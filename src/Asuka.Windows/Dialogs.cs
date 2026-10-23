using Asuka.Core;

namespace Asuka.Windows;

internal static class Dialogs
{
    public static string? Code(IWin32Window owner, string title, bool confirm = false)
    {
        using var f = new Form { Text = title, Width = 440, Height = confirm ? 260 : 210, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var label = new Label { Text = confirm ? "Choisissez un code personnel (6 à 32 chiffres)." : "Entrez votre code personnel.", Dock = DockStyle.Top, Height = 40 };
        var first = new TextBox { UseSystemPasswordChar = true, MaxLength = 32, Dock = DockStyle.Top };
        var second = new TextBox { UseSystemPasswordChar = true, MaxLength = 32, Dock = DockStyle.Top, PlaceholderText = "Confirmez le code" };
        var ok = new Button { Text = "Valider", Dock = DockStyle.Bottom, Height = 42 };
        f.Controls.Add(first); f.Controls.Add(label);
        if (confirm) { second.Top = 90; second.Dock = DockStyle.None; second.Width = 400; second.Left = 10; f.Controls.Add(second); second.BringToFront(); }
        f.Controls.Add(ok); f.AcceptButton = ok;
        ok.Click += (_, _) =>
        {
            if (confirm && (first.Text != second.Text || first.Text.Length < 6 || !first.Text.All(char.IsAsciiDigit)))
            { MessageBox.Show(f, "Deux codes identiques, de 6 à 32 chiffres, sont nécessaires."); return; }
            f.DialogResult = DialogResult.OK;
        };
        return f.ShowDialog(owner) == DialogResult.OK ? first.Text : null;
    }
}

internal sealed class SettingsDialog : Form
{
    public Settings Result { get; private set; }
    public string TokenResult { get; private set; }
    private readonly TableLayoutPanel rows = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(16) };
    public SettingsDialog(Settings settings, string token)
    {
        Result = settings; TokenResult = token;
        Text = "Réglages — Fenêtre Asuka"; Width = 730; Height = 780;
        StartPosition = FormStartPosition.CenterParent;
        var server = TextInput("Adresse du serveur (IPv4 privée)", settings.Server);
        var device = TextInput("Nom de cet appareil", settings.Device);
        var room = TextInput("Pièce", settings.Room);
        var secret = TextInput("Jeton présence (vide = désactivée)", token); secret.UseSystemPasswordChar = true;
        var idle = Number("Veille après absence (minutes)", settings.IdleMinutes, 1, 60, 0);
        var threshold = Number("Seuil visage (0,50 conseillé)", (decimal)settings.Threshold, 0.363m, 0.95m, 3);
        var motion = Number("Seuil mouvement (0,03 conseillé)", (decimal)settings.MotionThreshold, 0.001m, 0.5m, 3);
        var camera = Number("Webcam (0 = première)", settings.Camera, 0, 10, 0);
        var guest = Check("Mode invité : webcam coupée", settings.Guest);
        var auto = Check("Démarrer avec ma session Windows", settings.AutoStart);
        var awake = Check("Empêcher la veille Windows, uniquement branché", settings.KeepAwakePlugged);
        var sleep = Check("Heures de sommeil (webcam coupée)", settings.SleepEnabled);
        var start = TextInput("Sommeil de (HH:mm)", settings.SleepStart.ToString("HH:mm"));
        var end = TextInput("à (HH:mm)", settings.SleepEnd.ToString("HH:mm"));
        var notice = new Label { AutoSize = true, MaximumSize = new Size(620, 0), Text = "Aucune photo ni vidéo n’est conservée ou envoyée. Le code, le jeton et la signature sont protégés par votre compte Windows (DPAPI). La reconnaissance ne résiste pas nécessairement à une photo : ce n’est pas une protection d’accès forte." };
        Add("Vie privée", notice);
        var save = new Button { Text = "Enregistrer", Dock = DockStyle.Bottom, Height = 48 };
        Controls.Add(rows); Controls.Add(save);
        save.Click += (_, _) =>
        {
            try
            {
                Result = settings with { Server = server.Text.Trim(), Device = device.Text.Trim(), Room = room.Text.Trim(), IdleMinutes = (int)idle.Value,
                    Threshold = (double)threshold.Value, MotionThreshold = (double)motion.Value, Camera = (int)camera.Value,
                    Guest = guest.Checked, AutoStart = auto.Checked, KeepAwakePlugged = awake.Checked, SleepEnabled = sleep.Checked,
                    SleepStart = TimeOnly.ParseExact(start.Text.Trim(), "HH:mm"), SleepEnd = TimeOnly.ParseExact(end.Text.Trim(), "HH:mm") };
                Result.Validate(); TokenResult = secret.Text.Trim();
                if (TokenResult.Contains('\r') || TokenResult.Contains('\n')) throw new ArgumentException("Jeton invalide.");
                DialogResult = DialogResult.OK;
            }
            catch (Exception e) when (e is ArgumentException or FormatException) { MessageBox.Show(this, e.Message, "Réglages invalides"); }
        };
    }
    private void Add(string text, Control control)
    {
        int row = rows.RowCount++;
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rows.Controls.Add(new Label { Text = text, AutoSize = true, MaximumSize = new Size(270, 0), Margin = new Padding(4, 12, 4, 12) }, 0, row);
        control.Margin = new Padding(4, 10, 4, 10); control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        rows.Controls.Add(control, 1, row);
    }
    private TextBox TextInput(string label, string value) { var c = new TextBox { Text = value, Width = 360 }; Add(label, c); return c; }
    private NumericUpDown Number(string label, decimal value, decimal min, decimal max, int decimals)
    { var c = new NumericUpDown { Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, Increment = decimals > 0 ? 0.01m : 1 }; Add(label, c); return c; }
    private CheckBox Check(string label, bool value) { var c = new CheckBox { Checked = value, AutoSize = true }; Add(label, c); return c; }
}
