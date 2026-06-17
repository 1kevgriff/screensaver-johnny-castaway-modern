namespace JohnnyCastaway.ScreenSaver;

public sealed class SettingsForm : Form
{
    private readonly ISettingsStore _store;
    private readonly ComboBox _start = new() { DropDownStyle = ComboBoxStyle.DropDownList, Left = 120, Top = 16, Width = 100 };
    private readonly CheckBox _sound = new() { Text = "Sound effects", Left = 16, Top = 52 };

    public SettingsForm(ISettingsStore store)
    {
        _store = store;
        Text = "Screen Antics — Johnny Castaway";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(260, 140);
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Text = "Start of day:", Left = 16, Top = 19, Width = 100 });
        for (int h = 0; h < 24; h++) foreach (int m in new[] { 0, 30 })
            _start.Items.Add($"{h:D2}{m:D2}");
        Controls.Add(_start);
        Controls.Add(_sound);

        var ok = new Button { Text = "OK", Left = 60, Top = 92, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 150, Top = 92, DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => Save();
        Controls.Add(ok); Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;

        var s = _store.Load();
        _start.SelectedItem = $"{s.StartOfDayHHMM:D4}";
        if (_start.SelectedIndex < 0)
            _start.SelectedIndex = Math.Max(0, _start.Items.IndexOf($"{SaverSettings.Defaults.StartOfDayHHMM:D4}"));
        _sound.Checked = s.SoundEnabled;
    }

    private void Save()
    {
        int hhmm = int.Parse((string)_start.SelectedItem!);
        _store.Save(new SaverSettings(hhmm, _sound.Checked, 4));
    }
}
