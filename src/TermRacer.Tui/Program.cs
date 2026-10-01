using System.Reflection;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using TermRacer.Core;
using TermRacer.Storage;
using TermRacer.Tui;

var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";
if (args is ["--version"] or ["-v"])
{
    Console.WriteLine($"termracer {version}");
    return;
}

var location = DataDirectory.Resolve();
var home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var game = new Game(new FileRecordStore(location.Path, DataDirectory.Describe(location.Path, home)));

Application.MaximumIterationsPerSecond = 60;
using var app = Application.Create().Init(DriverRegistry.Names.ANSI);
if (app.Driver is { } driver)
{
    driver.Force16Colors = false;
}

using var view = new GameView(app, game, $"v{version}");
app.Run(view);
