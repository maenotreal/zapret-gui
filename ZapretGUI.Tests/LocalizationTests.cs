using System.Reflection;
using ZapretGUI.Localization;

namespace ZapretGUI.Tests;

public class LocalizationTests
{
    [Fact]
    public void AllStringProperties_Russian_AreNonEmpty()
    {
        L.Lang = Language.Russian;
        AssertAllNonEmpty();
    }

    [Fact]
    public void AllStringProperties_English_AreNonEmpty()
    {
        L.Lang = Language.English;
        try { AssertAllNonEmpty(); }
        finally { L.Lang = Language.Russian; }
    }

    static void AssertAllNonEmpty()
    {
        var props = typeof(L)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0);

        foreach (var prop in props)
        {
            var value = (string?)prop.GetValue(null);
            Assert.False(string.IsNullOrWhiteSpace(value),
                $"L.{prop.Name} is empty or null in {L.Lang}");
        }
    }

    [Fact]
    public void Changed_FiresOnSwitch_NotOnSameValue()
    {
        L.Lang = Language.Russian;
        int count = 0;
        Action handler = () => count++;
        L.Changed += handler;
        try
        {
            L.Lang = Language.English;
            Assert.Equal(1, count);

            L.Lang = Language.Russian;
            Assert.Equal(2, count);

            L.Lang = Language.Russian; // same value — must not fire
            Assert.Equal(2, count);
        }
        finally
        {
            L.Changed -= handler;
            L.Lang = Language.Russian;
        }
    }

    [Fact]
    public void LangToggle_AlternatesBetweenLanguages()
    {
        L.Lang = Language.Russian;
        Assert.Equal("EN", L.LangToggle);

        L.Lang = Language.English;
        Assert.Equal("RU", L.LangToggle);

        L.Lang = Language.Russian;
    }

    [Fact]
    public void ParameterizedMethods_ReturnNonEmpty()
    {
        L.Lang = Language.Russian;
        Assert.False(string.IsNullOrWhiteSpace(L.LogDirOk("C:\\zapret")));
        Assert.False(string.IsNullOrWhiteSpace(L.LogDirInvalid("C:\\bad")));
        Assert.False(string.IsNullOrWhiteSpace(L.LogInstalling("general")));
        Assert.False(string.IsNullOrWhiteSpace(L.DiagProxyWarn("127.0.0.1:8080")));
        Assert.False(string.IsNullOrWhiteSpace(L.DiagCflErr("GoodbyeDPI")));
        Assert.False(string.IsNullOrWhiteSpace(L.TestRunning("Standard", 3)));
        Assert.False(string.IsNullOrWhiteSpace(L.TestBestConfig("general (ALT2)")));
    }
}
