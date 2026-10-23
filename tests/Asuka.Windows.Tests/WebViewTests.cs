using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Web.WebView2.WinForms;
using Xunit;
namespace Asuka.Windows.Tests;

public class WebViewTests
{
    [Fact] public async Task WebViewRendersARealLocalPage()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(35));
        var response = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync(cancellation.Token);
            using var stream = connection.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            while (await reader.ReadLineAsync(cancellation.Token) is { Length: > 0 }) { }
            byte[] body = Encoding.UTF8.GetBytes("<!doctype html><html><body><h1 id='test'>Asuka locale</h1></body></html>");
            byte[] headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headers, cancellation.Token);
            await stream.WriteAsync(body, cancellation.Token);
        });
        var completed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            using var form = new Form { Width = 400, Height = 300, ShowInTaskbar = false };
            using var view = new WebView2 { Dock = DockStyle.Fill, CreationProperties = new CoreWebView2CreationProperties {
                UserDataFolder = Path.Combine(Path.GetTempPath(), "Asuka-WebView-Test-" + Guid.NewGuid()), IsInPrivateModeEnabled = true,
                AdditionalBrowserArguments = "--no-proxy-server --disable-background-networking" } };
            form.Controls.Add(view);
            form.Shown += async (_, _) =>
            {
                try
                {
                    await view.EnsureCoreWebView2Async();
                    var loaded = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                    view.CoreWebView2.NavigationCompleted += async (_, e) =>
                    {
                        try
                        {
                            if (!e.IsSuccess) throw new InvalidOperationException("Navigation WebView2 : " + e.WebErrorStatus);
                            loaded.TrySetResult(await view.CoreWebView2.ExecuteScriptAsync("document.getElementById('test').textContent"));
                        }
                        catch (Exception ex) { loaded.TrySetException(ex); }
                    };
                    view.CoreWebView2.Navigate($"http://127.0.0.1:{port}/");
                    string text = await loaded.Task.WaitAsync(TimeSpan.FromSeconds(20));
                    completed.TrySetResult(text);
                }
                catch (Exception ex) { completed.TrySetException(ex); }
                finally { form.Close(); }
            };
            Application.Run(form);
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.Equal("\"Asuka locale\"", await completed.Task.WaitAsync(TimeSpan.FromSeconds(30)));
        await response;
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "Le thread de test WebView2 doit se terminer.");
    }
}
