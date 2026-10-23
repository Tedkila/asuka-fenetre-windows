using Asuka.Core;
using Xunit;
namespace Asuka.Tests;

public class PolicyTests
{
    private readonly DateTimeOffset now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    [Fact] public void UnknownAndMultipleFacesNeverRevealContent()
    {
        var p = new PresencePolicy(); var s = new Settings();
        for (int i = 0; i < 20; i++) p.Tick(s, now.AddSeconds(i), true, false, true, false);
        Assert.Equal(ViewMode.Neutral, p.Mode);
        for (int i = 0; i < 3; i++) p.Tick(s, now.AddSeconds(21 + i), true, true, true, false);
        Assert.Equal(ViewMode.Personal, p.Mode);
        p.Tick(s, now.AddSeconds(25), true, false, true, false);
        Assert.Equal(ViewMode.Neutral, p.Mode);
    }
    [Fact] public void ThreeDistinctMatchesThenAbsenceSleeps()
    {
        var p = new PresencePolicy(); var s = new Settings();
        p.Tick(s, now, true, true, false, false);
        for (int i = 0; i < 10; i++) p.Tick(s, now, true, true, false, false, fresh: false);
        Assert.Equal(ViewMode.Neutral, p.Mode);
        p.Tick(s, now.AddSeconds(1), true, true, false, false);
        p.Tick(s, now.AddSeconds(2), true, true, false, false);
        Assert.Equal(ViewMode.Personal, p.Mode);
        p.Tick(s, now.AddMinutes(4), false, false, false, false);
        Assert.Equal(ViewMode.Sleep, p.Mode);
        p.Tick(s, now.AddMinutes(4).AddSeconds(1), false, false, true, false);
        Assert.Equal(ViewMode.Neutral, p.Mode);
    }
    [Fact] public void GuestAndCameraFailureHideContent()
    {
        var s = new Settings(); var p = new PresencePolicy();
        for (int i = 0; i < 3; i++) p.Tick(s, now, true, true, false, false);
        p.Tick(s, now, false, false, false, true);
        Assert.Equal(ViewMode.Neutral, p.Mode);
        p.Tick(s with { Guest = true }, now, true, true, true, false);
        Assert.Equal(ViewMode.Neutral, p.Mode);
    }
    [Fact] public void PinFallbackWorksWithoutCameraAndExpires()
    {
        var p = new PresencePolicy(); var s = new Settings { Guest = true };
        p.ManualUnlock(now);
        p.Tick(s, now.AddSeconds(30), false, false, false, true);
        Assert.Equal(ViewMode.Personal, p.Mode);
        p.Tick(s, now.AddMinutes(3), false, false, false, true);
        Assert.Equal(ViewMode.Neutral, p.Mode);
    }
    [Theory]
    [InlineData(23, true)][InlineData(0, true)][InlineData(6, true)][InlineData(7, false)][InlineData(22, false)]
    public void OvernightSleep(int hour, bool sleeping) => Assert.Equal(sleeping, new Settings { SleepEnabled = true }.IsSleeping(new TimeOnly(hour, 0)));
    [Fact] public void SleepOverridesPin()
    {
        var p = new PresencePolicy(); p.ManualUnlock(now);
        p.Tick(new Settings { SleepEnabled = true, SleepStart = new(0, 0), SleepEnd = new(23, 59) }, now, true, true, true, false);
        Assert.Equal(ViewMode.Sleep, p.Mode);
    }
    [Theory]
    [InlineData("http://192.168.1.26:8765/", true)]
    [InlineData("https://10.0.0.2/", true)]
    [InlineData("http://172.31.1.2/", true)]
    [InlineData("http://localhost:1234/", true)]
    [InlineData("https://example.org/", false)]
    [InlineData("http://192.168.1.26.evil.com/", false)]
    [InlineData("http://192.168.1.26@evil.com/", false)]
    [InlineData("file:///C:/secret", false)]
    [InlineData("http://172.32.1.2/", false)]
    public void ServerRestrictedToLocalAddresses(string url, bool expected) => Assert.Equal(expected, LocalAddress.TryParse(url, out _));
    [Fact] public void DifferentPortsAndSchemesAreBlocked()
    {
        var origin = new Uri("http://192.168.1.26:8765/");
        Assert.True(LocalAddress.SameOrigin(origin, new Uri("http://192.168.1.26:8765/page")));
        Assert.False(LocalAddress.SameOrigin(origin, new Uri("http://192.168.1.26:8766/")));
        Assert.False(LocalAddress.SameOrigin(origin, new Uri("https://192.168.1.26:8765/")));
    }
    [Fact] public void PinHasSaltAndRejectsWrongCode()
    {
        string one = PinHash.Create("123456"), two = PinHash.Create("123456");
        Assert.NotEqual(one, two); Assert.True(PinHash.Verify("123456", one));
        Assert.False(PinHash.Verify("000000", one)); Assert.False(PinHash.Verify("123456", "corrupt"));
        Assert.Throws<ArgumentException>(() => PinHash.Create("1234"));
    }
}
