using System.Runtime.InteropServices;
using Asuka.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Asuka.Windows;

internal sealed class MainWindow : Form
{
    private readonly Storage storage;
    private UserData data;
    private readonly PresencePolicy policy = new();
    private readonly Panel surface = new() { Dock = DockStyle.Fill, BackColor = Color.Black };
    private readonly Label message = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = Color.Black, Font = new Font("Segoe UI", 22) };
    private readonly Label indicator = new() { AutoSize = true, ForeColor = Color.LightGreen, Margin = new Padding(15) };
    private readonly FlowLayoutPanel bar = new() { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(20, 20, 20), AutoScroll = true };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 350 };
    private readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false }) { Timeout = TimeSpan.FromSeconds(3) };
    private readonly CancellationTokenSource lifetime = new();
    private CameraWorker? camera;
    private WebView2? web;
    private bool changing, busy, closing, closed, sending, initialized;
    private ViewMode? displayed;
    private DateTimeOffset nextPresence, blockedUntil, navigationStarted, lastObservation;
    private int failedCodes;
    private bool loading;
    private readonly Button retry;
    private readonly Button guest;

    public MainWindow(Storage storage)
    {
        this.storage = storage; data = storage.Load();
        Text = "Fenêtre Asuka"; WindowState = FormWindowState.Maximized; FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(800, 500); BackColor = Color.Black;
        surface.Controls.Add(message); Controls.Add(surface); Controls.Add(bar);
        AddButton("C’est moi", async () => { if (Authenticate()) { policy.ManualUnlock(DateTimeOffset.Now, data.Settings.IdleMinutes); displayed = null; await Render(); } });
        AddButton("Réglages", Configure);
        guest = AddButton("Mode invité", ToggleGuest);
        AddButton("Verrouiller", async () => { policy.Lock(); displayed = null; await Render(); });
        AddButton("Quitter", () => { Close(); return Task.CompletedTask; });
        bar.Controls.Add(indicator);
        retry = new Button { Text = "Réessayer", Dock = DockStyle.Bottom, Height = 56, Visible = false };
        retry.Click += async (_, _) => { if (policy.Mode == ViewMode.Personal) _ = OpenPage(); };
        surface.Controls.Add(retry); retry.BringToFront();
        Shown += async (_, _) => await Initialize();
        timer.Tick += async (_, _) => await Tick();
        FormClosing += OnClosingAsync;
    }
    private Button AddButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 42, Margin = new Padding(6), BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
        button.Click += async (_, _) =>
        {
            if (busy) return;
            busy = true;
            try { await action(); }
            catch (Exception) { MessageBox.Show(this, "L’opération n’a pas abouti. Vérifiez les autorisations Windows et réessayez.", Text); }
            finally { busy = false; }
        };
        bar.Controls.Add(button); return button;
    }
    private async Task Initialize()
    {
        if (data.Pin.Length == 0)
        {
            MessageBox.Show(this, "Bienvenue. Créez votre code personnel pour les réglages et l’enregistrement du visage. Aucune photo n’est conservée ni envoyée. La webcam surveillera localement avec un voyant visible. Le mode invité la coupe.\n\nLa reconnaissance peut être trompée par une photo : utilisez une session Windows personnelle.", Text);
            var code = Dialogs.Code(this, "Créer mon code", true);
            if (code is null) { Close(); return; }
            data = data with { Pin = PinHash.Create(code) }; storage.Save(data);
        }
        initialized = true;
        await ReconcileCamera();
        timer.Start(); await Render();
    }
    private bool Authenticate()
    {
        if (DateTimeOffset.UtcNow < blockedUntil) { MessageBox.Show(this, "Trop d’essais. Attendez 30 secondes."); return false; }
        string? code = Dialogs.Code(this, "Code personnel");
        if (code is null) return false;
        if (PinHash.Verify(code, data.Pin)) { failedCodes = 0; return true; }
        if (++failedCodes >= 5) { blockedUntil = DateTimeOffset.UtcNow.AddSeconds(30); failedCodes = 0; }
        MessageBox.Show(this, "Code incorrect."); return false;
    }
    private async Task Configure()
    {
        if (!Authenticate()) return;
        policy.Lock(); displayed = null; await Render();
        using var menu = new Form { Text = "Réglages protégés", Width = 520, Height = 490, StartPosition = FormStartPosition.CenterParent };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20) };
        menu.Controls.Add(actions);
        void Action(string text, Func<Task> execute)
        {
            var button = new Button { Text = text, Width = 455, Height = 50 };
            actions.Controls.Add(button);
            button.Click += async (_, _) =>
            {
                actions.Enabled = false;
                try { await execute(); }
                catch (Exception) { MessageBox.Show(menu, "Opération impossible. Vérifiez la webcam, les modèles et les autorisations Windows."); }
                finally { actions.Enabled = true; }
            };
        }
        Action("Adresse, présence, veille et vie privée", async () =>
        {
            using var settings = new SettingsDialog(data.Settings, data.Token);
            if (settings.ShowDialog(menu) != DialogResult.OK) return;
            var replacement = data with { Settings = settings.Result, Token = settings.TokenResult };
            Storage.AutoStart(replacement.Settings.AutoStart);
            storage.Save(replacement); data = replacement;
            await StopCamera(); await ReconcileCamera();
        });
        Action("Enregistrer / remplacer mon visage", async () =>
        {
            if (data.Settings.Guest || data.Settings.IsSleeping(TimeOnly.FromDateTime(DateTime.Now)))
            { MessageBox.Show(menu, "Désactivez le mode invité ou attendez la fin des heures de sommeil."); return; }
            if (camera is null || !camera.Latest.Running) { MessageBox.Show(menu, "La webcam n’est pas disponible. Réessayez la webcam."); return; }
            if (MessageBox.Show(menu, "Teddy doit être seul, face à la webcam, bien éclairé, pendant quelques secondes. Seule une signature chiffrée sera conservée. Continuer ?", "Enregistrer mon visage", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            indicator.Text = "● Regardez la webcam — enregistrement en cours…";
            try
            {
                var signature = await camera.EnrollAsync();
                var replacement = data with { Face = signature };
                storage.Save(replacement);
                if (data.Face is not null) Array.Clear(data.Face);
                data = replacement;
                await StopCamera(); await ReconcileCamera();
                MessageBox.Show(menu, "Visage enregistré. Aucune image n’a été conservée.");
            }
            catch (TimeoutException) { MessageBox.Show(menu, "Visage non enregistré : restez seul, face à la caméra, avec un bon éclairage, puis réessayez."); }
        });
        Action("Effacer mon visage", async () =>
        {
            if (MessageBox.Show(menu, "Effacer définitivement la signature faciale ?", Text, MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            var replacement = data with { Face = null }; storage.Save(replacement);
            if (data.Face is not null) Array.Clear(data.Face);
            data = replacement; policy.Lock(); await StopCamera(); await ReconcileCamera();
            MessageBox.Show(menu, "Signature effacée.");
        });
        Action("Changer mon code", () =>
        {
            var code = Dialogs.Code(menu, "Nouveau code", true);
            if (code is not null) { var replacement = data with { Pin = PinHash.Create(code) }; storage.Save(replacement); data = replacement; }
            return Task.CompletedTask;
        });
        Action("Réessayer webcam", async () => { await StopCamera(); await ReconcileCamera(); });
        Action("Fermer les réglages", () => { menu.Close(); return Task.CompletedTask; });
        changing = true;
        try { menu.ShowDialog(this); }
        finally { changing = false; policy.Lock(); displayed = null; await Render(); }
    }
    private async Task ToggleGuest()
    {
        if (data.Settings.Guest && !Authenticate()) return;
        var replacement = data with { Settings = data.Settings with { Guest = !data.Settings.Guest } };
        // Stop capture before reporting guest mode as active.
        policy.Lock(); displayed = null; await Render(); await StopCamera();
        storage.Save(replacement); data = replacement;
        await ReconcileCamera(); displayed = null; await Render();
    }
    private async Task StopCamera()
    {
        var old = camera; camera = null;
        if (old is not null) await old.DisposeAsync();
    }
    private async Task ReconcileCamera()
    {
        bool off = data.Settings.Guest || data.Settings.IsSleeping(TimeOnly.FromDateTime(DateTime.Now));
        if (off)
        {
            if (camera is not null) { policy.Lock(); displayed = null; await Render(); }
            await StopCamera();
        }
        else camera ??= new CameraWorker(data.Settings, data.Face?.ToArray());
    }
    private async Task Tick()
    {
        if (!initialized || changing || closed) return;
        changing = true;
        try
        {
            await ReconcileCamera();
            var observation = camera?.Latest;
            bool failed = observation is null || !observation.Running || DateTimeOffset.UtcNow - observation.At > TimeSpan.FromSeconds(4);
            indicator.Text = data.Settings.Guest ? "Webcam coupée — invité" : data.Settings.IsSleeping(TimeOnly.FromDateTime(DateTime.Now)) ? "Webcam coupée — sommeil" : observation?.Status ?? "Webcam coupée";
            if (failed && observation?.Running == true && !data.Settings.Guest) indicator.Text = "Webcam sans réponse — accès automatique coupé";
            indicator.ForeColor = failed ? Color.Gold : Color.LightGreen;
            guest.Text = data.Settings.Guest ? "Quitter mode invité" : "Mode invité";
            policy.Tick(data.Settings, DateTimeOffset.Now, observation?.Face == true, observation?.Teddy == true, observation?.Motion == true, failed, observation?.At != lastObservation);
            lastObservation = observation?.At ?? default;
            await Render();
            if (loading && DateTimeOffset.UtcNow - navigationStarted > TimeSpan.FromSeconds(12)) PageError();
            bool prevent = data.Settings.KeepAwakePlugged && SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
            SetThreadExecutionState(prevent ? 0x80000001u : 0x80000000u);
            if (!sending && DateTimeOffset.UtcNow >= nextPresence)
            {
                nextPresence = DateTimeOffset.UtcNow.AddSeconds(30);
                _ = SendPresence();
            }
        }
        finally { changing = false; }
    }
    private async Task SendPresence()
    {
        sending = true;
        try { await new PresenceClient(http).SendAsync(data.Settings, policy.Mode == ViewMode.Personal, data.Token, lifetime.Token); }
        finally { sending = false; }
    }
    private Task Render()
    {
        if (displayed == policy.Mode) return Task.CompletedTask;
        displayed = policy.Mode;
        DestroyWeb(); retry.Visible = false;
        if (policy.Mode == ViewMode.Personal) _ = OpenPage();
        else { message.Visible = true; message.Text = policy.Mode == ViewMode.Sleep ? "" : "Bonjour !"; }
        return Task.CompletedTask;
    }
    private void DestroyWeb()
    {
        loading = false;
        var old = web; web = null;
        if (old is not null) { surface.Controls.Remove(old); old.Dispose(); }
    }
    private async Task OpenPage()
    {
        DestroyWeb(); retry.Visible = false;
        message.Visible = true; message.Text = "Connexion à Asuka…";
        var candidate = new WebView2 { Dock = DockStyle.Fill, Visible = false,
            CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = Path.Combine(Storage.DirectoryPath, "WebView"), IsInPrivateModeEnabled = true,
                AdditionalBrowserArguments = "--no-proxy-server --disable-background-networking --disable-sync --disable-breakpad --disable-features=msEdgeShoppingAssistant,msEdgeSidebarV2" } };
        web = candidate; surface.Controls.Add(candidate);
        try
        {
            await candidate.EnsureCoreWebView2Async();
            if (web != candidate || policy.Mode != ViewMode.Personal) return;
            var core = candidate.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false; core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false; core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false; core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsWebMessageEnabled = false;
            var origin = new Uri(data.Settings.Server);
            bool Allowed(string address) => Uri.TryCreate(address, UriKind.Absolute, out var uri) && LocalAddress.SameOrigin(origin, uri);
            core.NavigationStarting += (_, e) => { if (!Allowed(e.Uri)) e.Cancel = true; };
            core.FrameNavigationStarting += (_, e) => { if (!Allowed(e.Uri)) e.Cancel = true; };
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.ServerCertificateErrorDetected += (_, e) => e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) =>
            {
                if (!Allowed(e.Request.Uri)) e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
            };
            core.NavigationCompleted += (_, e) =>
            {
                if (web != candidate || policy.Mode != ViewMode.Personal) return;
                loading = false;
                if (!e.IsSuccess || e.HttpStatusCode >= 400) { PageError(); return; }
                message.Visible = false; retry.Visible = false; candidate.Visible = true; candidate.BringToFront();
            };
            core.ProcessFailed += (_, _) => { if (web == candidate) PageError(); };
            loading = true; navigationStarted = DateTimeOffset.UtcNow;
            core.Navigate(data.Settings.Server);
        }
        catch (Exception)
        {
            if (web != candidate) return;
            DestroyWeb(); message.Visible = true;
            message.Text = "Asuka n’est pas disponible.\nVérifiez le serveur et le runtime Microsoft Edge WebView2.\nConsultez le guide d’installation."; retry.Visible = true;
        }
    }
    private void PageError()
    {
        DestroyWeb(); message.Visible = true;
        message.Text = "Asuka n'est joignable qu'à la maison\n\nVérifie le Wi-Fi de la maison"; retry.Visible = true;
    }
    private async void OnClosingAsync(object? sender, FormClosingEventArgs e)
    {
        if (closed) return;
        e.Cancel = true;
        if (closing) return;
        closing = true; timer.Stop(); changing = true;
        await lifetime.CancelAsync(); DestroyWeb();
        await StopCamera();
        SetThreadExecutionState(0x80000000u);
        if (data.Face is not null) Array.Clear(data.Face);
        closed = true; Close(); http.Dispose(); lifetime.Dispose(); timer.Dispose();
    }
    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
}
