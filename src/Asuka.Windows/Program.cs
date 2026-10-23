namespace Asuka.Windows;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var single = new Mutex(true, @"Local\FenetreAsuka-" + Environment.UserName, out bool first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        try { Application.Run(new MainWindow(new Storage())); }
        catch (Exception)
        {
            MessageBox.Show("Impossible d’ouvrir Fenêtre Asuka. Le profil est peut-être illisible. Consultez le guide de récupération dans docs. Aucune donnée n’a été effacée.", "Fenêtre Asuka", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
