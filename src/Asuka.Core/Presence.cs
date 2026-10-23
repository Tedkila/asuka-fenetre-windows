using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Asuka.Core;

public sealed record Presence(
    [property: JsonPropertyName("appareil")] string Device,
    [property: JsonPropertyName("piece")] string Room,
    [property: JsonPropertyName("teddy")] bool Teddy,
    [property: JsonPropertyName("heure")] DateTimeOffset Time);

public sealed class PresenceClient(HttpClient client)
{
    public async Task<bool> SendAsync(Settings settings, bool teddy, string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        settings.Validate();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.Server), "/api/presence"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new Presence(settings.Device, settings.Room, teddy, DateTimeOffset.UtcNow));
            using var result = await client.SendAsync(request, ct);
            return result.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or FormatException) { return false; }
    }
}

public enum ViewMode { Sleep, Neutral, Personal }

// Pure state policy, shared by the application and tests. Time is supplied by the caller.
public sealed class PresencePolicy
{
    public ViewMode Mode { get; private set; } = ViewMode.Neutral;
    private DateTimeOffset lastPerson;
    private int matches;
    private DateTimeOffset manualUntil;
    public void ManualUnlock(DateTimeOffset now, int minutes = 3) { Mode = ViewMode.Personal; lastPerson = now; matches = 0; manualUntil = now.AddMinutes(minutes); }
    public void Lock() { Mode = ViewMode.Neutral; matches = 0; manualUntil = default; }
    public void Tick(Settings settings, DateTimeOffset now, bool face, bool teddy, bool movement, bool failed, bool fresh = true)
    {
        if (settings.IsSleeping(TimeOnly.FromDateTime(now.LocalDateTime))) { Mode = ViewMode.Sleep; matches = 0; manualUntil = default; return; }
        if (manualUntil > now) { Mode = ViewMode.Personal; return; }
        if (manualUntil != default) { Lock(); }
        if (settings.Guest || failed) { Lock(); return; }
        if (face)
        {
            lastPerson = now;
            if (!teddy) { Lock(); return; }
            if (fresh) matches++;
            if (matches >= 3) Mode = ViewMode.Personal;
            else if (Mode == ViewMode.Sleep) Mode = ViewMode.Neutral;
        }
        else
        {
            matches = 0;
            if (lastPerson == default) lastPerson = now;
            if (movement && Mode == ViewMode.Sleep) { Mode = ViewMode.Neutral; lastPerson = now; }
            else if (now - lastPerson >= TimeSpan.FromMinutes(settings.IdleMinutes)) Mode = ViewMode.Sleep;
        }
    }
}
