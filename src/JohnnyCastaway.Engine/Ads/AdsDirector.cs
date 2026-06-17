using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;

namespace JohnnyCastaway.Ads;

public interface IClipRenderer
{
    IReadOnlyList<RenderedFrame> Render(string ttmFile, int seqNum);
}

public readonly record struct ClipRef(string TtmFile, int SeqNum, string Label);

public sealed class AdsDirector(AdsScript ads, IClipRenderer renderer, Random rng)
{
    private readonly AdsScript _ads = ads;
    private readonly IClipRenderer _renderer = renderer;
    private readonly Random _rng = rng;

    public IReadOnlyList<ClipRef> PlanSegment(int segmentIndex, int maxClips = 64)
    {
        var seg = _ads.Segments[segmentIndex].Instructions;
        var played = new HashSet<(int, int)>();
        var plan = new List<ClipRef>();

        while (plan.Count < maxClips)
        {
            var batch = CollectBatch(seg, played);
            if (batch.Count == 0) break;

            foreach (var (env, seq) in batch)
            {
                if (plan.Count >= maxClips) break;
                played.Add((env, seq));
                if (!_ads.Res.TryGetValue(env.ToString(), out var ttm))
                    continue;   // no Res mapping → skip (do not emit "?" clip)
                plan.Add(new ClipRef(ttm, seq, _ads.Segments[segmentIndex].Label));
            }
        }
        return plan;
    }

    // Walk the segment once and collect ALL not-yet-played ADD_SEQ targets that are reachable in
    // the current pass (i.e. inside active/non-skipped IF bodies), in document order.
    // A RANDOM_START..END group contributes at most ONE weighted pick (fire-once semantics).
    // Returns an empty list when nothing new is reachable — signals end of vignette.
    private List<(int env, int seq)> CollectBatch(IReadOnlyList<Instr> seg, HashSet<(int, int)> played)
    {
        var batch = new List<(int env, int seq)>();
        int i = 0;
        while (i < seg.Count)
        {
            var ins = seg[i];
            switch (ins.Op)
            {
                case "IF_NOT_PLAYED" or "IF_PLAYED" or "IF_FINISHED" or "IF_RUNNING"
                  or "IF_NOT_RUNNING" or "IF_PAUSED" or "IF_NOT_PAUSED":
                    bool cond = EvalCondition(seg, ref i, played);
                    // i now points at first body instruction (or ELSE/END_IF if body is empty)
                    if (!cond) SkipToElseOrEndIf(seg, ref i, takeElse: true);
                    // if cond is true, fall through — i already points at body; loop processes it next
                    break;
                case "ELSE":
                    SkipToEndIf(seg, ref i); break;       // condition was true; skip else body
                case "END_IF":
                    i++; break;
                case "RANDOM_START":
                {
                    var pick = ChooseRandomAdd(seg, ref i, played);
                    if (pick is not null) batch.Add(pick.Value);
                    break;
                }
                case "ADD_SEQ":
                {
                    var key = (ins.Args[0], ins.Args[1]);
                    if (!played.Contains(key)) batch.Add(key);
                    i++; break;
                }
                default:
                    i++; break;
            }
        }
        return batch;
    }

    // Evaluates the condition starting at seg[i]; supports AND/OR chains.
    // Leaves i at the first instruction AFTER the condition chain (i.e., first body instruction or ELSE/END_IF).
    private bool EvalCondition(IReadOnlyList<Instr> seg, ref int i, HashSet<(int, int)> played)
    {
        bool result = EvalSingle(seg[i], played);
        i++;
        while (i < seg.Count && (seg[i].Op == "AND" || seg[i].Op == "OR"))
        {
            string logic = seg[i].Op; i++;
            if (i >= seg.Count) break;          // malformed: chain ends with no RHS operand
            bool rhs = EvalSingle(seg[i], played); i++;
            result = logic == "AND" ? (result && rhs) : (result || rhs);
        }
        return result;
    }

    private static bool EvalSingle(Instr c, HashSet<(int, int)> played)
    {
        var key = c.Args.Length >= 2 ? (c.Args[0], c.Args[1]) : (0, 0);
        bool isPlayed = played.Contains(key);
        return c.Op switch
        {
            "IF_NOT_PLAYED" => !isPlayed,
            "IF_PLAYED" or "IF_FINISHED" => isPlayed,   // clips run to completion -> played==finished
            "IF_RUNNING" => false,
            "IF_NOT_RUNNING" => true,
            "IF_PAUSED" => false,
            "IF_NOT_PAUSED" => true,
            _ => false,
        };
    }

    // Collects all ADD_SEQs in a RANDOM_START..RANDOM_END block, picks one by weight.
    // If any candidate was already played the group is exhausted; skip it and return null.
    private (int, int)? ChooseRandomAdd(IReadOnlyList<Instr> seg, ref int i, HashSet<(int, int)> played)
    {
        i++; // past RANDOM_START
        var opts = new List<((int, int) key, int weight)>();
        bool anyPlayed = false;
        while (i < seg.Count && seg[i].Op != "RANDOM_END")
        {
            if (seg[i].Op == "ADD_SEQ")
            {
                var key = (seg[i].Args[0], seg[i].Args[1]);
                int w = seg[i].Args.Length >= 4 ? Math.Max(1, seg[i].Args[3]) : 1;
                if (played.Contains(key)) anyPlayed = true;
                else opts.Add((key, w));
            }
            i++;
        }
        if (i < seg.Count) i++; // past RANDOM_END
        // If any candidate was already played, the group has fired — don't pick again.
        if (anyPlayed || opts.Count == 0) return null;
        int total = opts.Sum(o => o.weight);
        int roll = _rng.Next(total);
        foreach (var o in opts) { if (roll < o.weight) return o.key; roll -= o.weight; }
        return opts[^1].key;
    }

    private static void SkipToElseOrEndIf(IReadOnlyList<Instr> seg, ref int i, bool takeElse)
    {
        int depth = 0;
        while (i < seg.Count)
        {
            string op = seg[i].Op;
            if (op is "IF_NOT_PLAYED" or "IF_PLAYED" or "IF_FINISHED" or "IF_RUNNING"
                  or "IF_NOT_RUNNING" or "IF_PAUSED" or "IF_NOT_PAUSED") depth++;
            else if (op == "END_IF") { if (depth == 0) { i++; return; } depth--; }
            else if (op == "ELSE" && depth == 0 && takeElse) { i++; return; }
            i++;
        }
    }

    private static void SkipToEndIf(IReadOnlyList<Instr> seg, ref int i)
    {
        int depth = 0;
        while (i < seg.Count)
        {
            string op = seg[i].Op;
            if (op is "IF_NOT_PLAYED" or "IF_PLAYED" or "IF_FINISHED" or "IF_RUNNING"
                  or "IF_NOT_RUNNING" or "IF_PAUSED" or "IF_NOT_PAUSED") depth++;
            else if (op == "END_IF") { if (depth == 0) { i++; return; } depth--; }
            i++;
        }
    }
}
