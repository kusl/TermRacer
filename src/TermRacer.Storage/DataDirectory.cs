namespace TermRacer.Storage;

public static class DataDirectory
{
    public const string OverrideVariable = "TERMRACER_DATA_DIR";
    public const string PortableFolder = "termracer-data";
    public const string AppFolder = "termracer";

    public static DataLocation Resolve() => Resolve(
        Environment.GetEnvironmentVariable,
        AppContext.BaseDirectory,
        OperatingSystem.IsWindows(),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
        Directory.Exists);

    public static DataLocation Resolve(
        Func<string, string?> environment,
        string baseDirectory,
        bool windows,
        string userProfile,
        string localAppData,
        Func<string, bool> directoryExists)
    {
        if (environment(OverrideVariable) is { } chosen && !string.IsNullOrWhiteSpace(chosen))
        {
            return new DataLocation(Path.GetFullPath(chosen), DataSource.Override);
        }

        var portable = Path.Combine(baseDirectory, PortableFolder);
        if (directoryExists(portable))
        {
            return new DataLocation(portable, DataSource.Portable);
        }

        if (environment("XDG_DATA_HOME") is { } xdg && Path.IsPathFullyQualified(xdg))
        {
            return new DataLocation(Path.Combine(xdg, AppFolder), DataSource.Xdg);
        }

        if (windows && !string.IsNullOrEmpty(localAppData))
        {
            return new DataLocation(Path.Combine(localAppData, "TermRacer"), DataSource.Default);
        }

        var home = environment("HOME") is { Length: > 0 } variable ? variable : userProfile;
        return string.IsNullOrEmpty(home)
            ? new DataLocation(portable, DataSource.Portable)
            : new DataLocation(Path.Combine(home, ".local", "share", AppFolder), DataSource.Default);
    }

    public static string Describe(string path, string? home)
    {
        if (string.IsNullOrEmpty(home))
        {
            return path;
        }

        var trimmed = home.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (path == trimmed)
        {
            return "~";
        }

        return path.StartsWith(trimmed + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? "~" + path[trimmed.Length..] : path;
    }
}
