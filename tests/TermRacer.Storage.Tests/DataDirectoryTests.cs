using Xunit;

namespace TermRacer.Storage.Tests;

public sealed class DataDirectoryTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "termracer-paths");
    private static readonly string Home = Path.Combine(Root, "home");
    private static readonly string Binary = Path.Combine(Root, "bin");
    private static readonly string LocalAppData = Path.Combine(Root, "appdata");

    private static DataLocation Resolve(Dictionary<string, string> environment, bool windows = false, bool portable = false) =>
        DataDirectory.Resolve(
            name => environment.GetValueOrDefault(name),
            Binary,
            windows,
            Home,
            LocalAppData,
            path => portable && path == Path.Combine(Binary, DataDirectory.PortableFolder));

    [Fact]
    public void OverrideVariableWins()
    {
        var chosen = Path.Combine(Root, "chosen");
        var location = Resolve(new() { [DataDirectory.OverrideVariable] = chosen, ["XDG_DATA_HOME"] = Path.Combine(Root, "xdg") }, portable: true);
        Assert.Equal(new DataLocation(chosen, DataSource.Override), location);
    }

    [Fact]
    public void PortableFolderNextToTheBinaryComesNext()
    {
        var location = Resolve(new() { ["XDG_DATA_HOME"] = Path.Combine(Root, "xdg") }, portable: true);
        Assert.Equal(new DataLocation(Path.Combine(Binary, DataDirectory.PortableFolder), DataSource.Portable), location);
    }

    [Fact]
    public void XdgDataHomeIsHonoured()
    {
        var xdg = Path.Combine(Root, "xdg");
        Assert.Equal(new DataLocation(Path.Combine(xdg, "termracer"), DataSource.Xdg), Resolve(new() { ["XDG_DATA_HOME"] = xdg }));
    }

    [Fact]
    public void RelativeXdgDataHomeIsIgnored() =>
        Assert.Equal(Path.Combine(Home, ".local", "share", "termracer"), Resolve(new() { ["XDG_DATA_HOME"] = "relative" }).Path);

    [Fact]
    public void UnixDefaultFollowsTheXdgFallback()
    {
        var other = Path.Combine(Root, "other-home");
        Assert.Equal(new DataLocation(Path.Combine(other, ".local", "share", "termracer"), DataSource.Default), Resolve(new() { ["HOME"] = other }));
    }

    [Fact]
    public void WindowsDefaultUsesLocalAppData() =>
        Assert.Equal(new DataLocation(Path.Combine(LocalAppData, "TermRacer"), DataSource.Default), Resolve([], windows: true));

    [Fact]
    public void HomeIsShortenedForDisplay()
    {
        Assert.Equal("~" + Path.DirectorySeparatorChar + "data", DataDirectory.Describe(Path.Combine(Home, "data"), Home));
        Assert.Equal(Path.Combine(Root, "elsewhere"), DataDirectory.Describe(Path.Combine(Root, "elsewhere"), Home));
        Assert.Equal(Path.Combine(Home + "x", "data"), DataDirectory.Describe(Path.Combine(Home + "x", "data"), Home));
    }
}
