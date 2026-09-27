using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using TermRacer.Core;
using TermRacer.Tui;

Application.MaximumIterationsPerSecond = 60;
using var app = Application.Create().Init(DriverRegistry.Names.ANSI);
if (app.Driver is { } driver)
{
    driver.Force16Colors = false;
}

using var view = new GameView(app, new Game());
app.Run(view);
