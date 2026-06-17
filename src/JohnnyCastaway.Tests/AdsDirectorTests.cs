using JohnnyCastaway.Ads;
using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class AdsDirectorTests
{
    sealed class FakeRenderer : IClipRenderer
    {
        public readonly List<(string, int)> Calls = new();
        public IReadOnlyList<RenderedFrame> Render(string ttm, int seq)
        {
            Calls.Add((ttm, seq));
            using var s = SKSurface.Create(new SKImageInfo(2, 2));
            return new[] { new RenderedFrame(s.Snapshot(), 1, Array.Empty<int>()) };
        }
    }

    static Instr I(string op, params int[] args) => new(op, args, null);

    // env 1 -> "A.TTM". Vignette: play seq 1; when finished, play seq 2.
    static AdsScript Chain() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("chain", new[]
        {
            I("INIT"),
            I("IF_NOT_PLAYED", 1, 1), I("ADD_SEQ", 1, 1, 0, 1), I("END_IF"),
            I("IF_FINISHED", 1, 1),   I("ADD_SEQ", 1, 2, 0, 1), I("END_IF"),
        }) });

    [Fact]
    public void SequencesChainedClipsInOrder()
    {
        var dir = new AdsDirector(Chain(), new FakeRenderer(), new Random(1));
        var plan = dir.PlanSegment(0);
        Assert.Equal(new[] { 1, 2 }, plan.Select(c => c.SeqNum).ToArray());
        Assert.All(plan, c => Assert.Equal("A.TTM", c.TtmFile));
    }

    // RANDOM picks exactly one of the weighted ADD_SEQs.
    static AdsScript RandomPick() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("rand", new[]
        {
            I("RANDOM_START"),
            I("ADD_SEQ", 1, 10, 0, 5),
            I("ADD_SEQ", 1, 11, 0, 5),
            I("RANDOM_END"),
        }) });

    [Fact]
    public void RandomPicksOneWeightedClip()
    {
        var dir = new AdsDirector(RandomPick(), new FakeRenderer(), new Random(42));
        var plan = dir.PlanSegment(0);
        Assert.Single(plan);
        Assert.Contains(plan[0].SeqNum, new[] { 10, 11 });
    }

    // 1. RandomFiresOnce: a RANDOM block of two ADD_SEQs yields exactly ONE clip total.
    // On the second pass through PlanSegment's loop the group is exhausted (one candidate
    // has been played) so ChooseRandomAdd returns null and the loop terminates.
    static AdsScript RandomOnlyBlock() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("rand-once", new[]
        {
            I("RANDOM_START"),
            I("ADD_SEQ", 1, 20, 0, 1),
            I("ADD_SEQ", 1, 21, 0, 1),
            I("RANDOM_END"),
        }) });

    [Fact]
    public void RandomFiresOnce()
    {
        var dir = new AdsDirector(RandomOnlyBlock(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        // Exactly one clip: the group must not re-fire on the second pass.
        Assert.Single(plan);
        Assert.Contains(plan[0].SeqNum, new[] { 20, 21 });
    }

    // 2. ElseBranchRunsWhenConditionFalse:
    // IF_PLAYED(1,1) ADD_SEQ(1,5) ELSE ADD_SEQ(1,6) END_IF
    // (1,1) has NOT been played → condition is false → ELSE body runs → seq 6 is planned.
    static AdsScript IfElseScript() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("ifelse", new[]
        {
            I("IF_PLAYED", 1, 1),
            I("ADD_SEQ", 1, 5, 0, 1),
            I("ELSE"),
            I("ADD_SEQ", 1, 6, 0, 1),
            I("END_IF"),
        }) });

    [Fact]
    public void ElseBranchRunsWhenConditionFalse()
    {
        var dir = new AdsDirector(IfElseScript(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        // First (and only) planned clip must come from the ELSE body.
        Assert.NotEmpty(plan);
        Assert.Equal(6, plan[0].SeqNum);
        Assert.DoesNotContain(plan, c => c.SeqNum == 5);
    }

    // 3a. AndChain: IF_NOT_PLAYED(1,1) AND IF_NOT_PLAYED(1,2) ADD_SEQ(1,7) END_IF
    // Both sides true on a fresh state → seq 7 is planned.
    static AdsScript AndChainScript() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("and-chain", new[]
        {
            I("IF_NOT_PLAYED", 1, 1),
            I("AND"),
            I("IF_NOT_PLAYED", 1, 2),
            I("ADD_SEQ", 1, 7, 0, 1),
            I("END_IF"),
        }) });

    [Fact]
    public void AndChainFires_WhenBothSidesTrue()
    {
        var dir = new AdsDirector(AndChainScript(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        Assert.NotEmpty(plan);
        Assert.Contains(plan, c => c.SeqNum == 7);
    }

    // 3b. OrChain: IF_PLAYED(1,1) OR IF_NOT_PLAYED(1,2) ADD_SEQ(1,8) END_IF
    // Left side is false (1,1 not played), right side is true (1,2 not played) → OR fires → seq 8.
    static AdsScript OrChainScript() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("or-chain", new[]
        {
            I("IF_PLAYED", 1, 1),
            I("OR"),
            I("IF_NOT_PLAYED", 1, 2),
            I("ADD_SEQ", 1, 8, 0, 1),
            I("END_IF"),
        }) });

    [Fact]
    public void OrChainFires_WhenOneSideTrue()
    {
        var dir = new AdsDirector(OrChainScript(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        Assert.NotEmpty(plan);
        Assert.Contains(plan, c => c.SeqNum == 8);
    }

    // 4. NestedIf: outer IF_NOT_PLAYED(1,1) containing inner IF_NOT_PLAYED(1,2) ADD_SEQ(1,9).
    // Both not played → both conditions true → seq 9 is planned.
    static AdsScript NestedIfScript() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("nested-if", new[]
        {
            I("IF_NOT_PLAYED", 1, 1),
                I("IF_NOT_PLAYED", 1, 2),
                    I("ADD_SEQ", 1, 9, 0, 1),
                I("END_IF"),
            I("END_IF"),
        }) });

    [Fact]
    public void NestedIf_InnerAddSeqFires_WhenBothConditionsTrue()
    {
        var dir = new AdsDirector(NestedIfScript(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        Assert.NotEmpty(plan);
        Assert.Contains(plan, c => c.SeqNum == 9);
    }

    // 4b. NestedIf suppressed: outer condition false → inner ADD_SEQ must NOT fire.
    static AdsScript NestedIfOuterFalseScript() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("nested-if-outer-false", new[]
        {
            I("IF_PLAYED", 1, 1),        // false: (1,1) not played
                I("IF_NOT_PLAYED", 1, 2),
                    I("ADD_SEQ", 1, 9, 0, 1),
                I("END_IF"),
            I("END_IF"),
        }) });

    [Fact]
    public void NestedIf_InnerAddSeqSuppressed_WhenOuterConditionFalse()
    {
        var dir = new AdsDirector(NestedIfOuterFalseScript(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        Assert.Empty(plan);
    }

    // 5. MultiAddSeqBlockEmitsAllInOrder
    // IF_NOT_PLAYED(1,1) { ADD_SEQ(1,1); ADD_SEQ(1,18) }  IF_FINISHED(1,18) { ADD_SEQ(1,10) }
    // env 1 → "A.TTM".
    // Expected plan seq order: [1, 18, 10].
    // On pass 1: both ADD_SEQ(1,1) and ADD_SEQ(1,18) must be collected together (both are in the
    // active body of IF_NOT_PLAYED), marked played, appended in order.
    // On pass 2: IF_NOT_PLAYED(1,1) is now false → skip; IF_FINISHED(1,18) is true (played==finished)
    // → ADD_SEQ(1,10) fires.
    // On pass 3: nothing new → done.
    // THIS TEST IS THE REGRESSION LOCK — it MUST FAIL before the batch-emission fix.
    static AdsScript MultiAddSeqBlock() => new(
        new Dictionary<string, string> { ["1"] = "A.TTM" },
        new[] { new AdsSegment("multi-add", new[]
        {
            I("IF_NOT_PLAYED", 1, 1),
            I("ADD_SEQ", 1, 1, 0, 1),
            I("ADD_SEQ", 1, 18, 0, 1),
            I("END_IF"),
            I("IF_FINISHED", 1, 18),
            I("ADD_SEQ", 1, 10, 0, 1),
            I("END_IF"),
        }) });

    [Fact]
    public void MultiAddSeqBlockEmitsAllInOrder()
    {
        var dir = new AdsDirector(MultiAddSeqBlock(), new FakeRenderer(), new Random(0));
        var plan = dir.PlanSegment(0);
        Assert.Equal(new[] { 1, 18, 10 }, plan.Select(c => c.SeqNum).ToArray());
        Assert.All(plan, c => Assert.Equal("A.TTM", c.TtmFile));
    }

    // 6. RealFishingSegmentChainsMultipleClips
    // Load the real bundle and verify the FISHING.ADS segment 0 produces a meaningful plan.
    static string Repo => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));

    [Fact]
    public void RealFishingSegmentChainsMultipleClips()
    {
        var bundle = ContentBundle.Load(Path.Combine(Repo, "content"));
        var fishing = bundle.Ads["FISHING.ADS"];
        var dir = new AdsDirector(fishing, new FakeRenderer(), new Random(7));
        var plan = dir.PlanSegment(0);
        Assert.True(plan.Count >= 4,
            $"Expected at least 4 clips in FISHING seg 0, got {plan.Count}: [{string.Join(", ", plan.Select(c => $"seq{c.SeqNum}"))}]");
        Assert.Equal("MJFISH.TTM", plan[0].TtmFile);
    }
}
