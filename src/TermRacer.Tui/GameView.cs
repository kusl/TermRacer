using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TermRacer.Core;
using TermRacer.Rendering;
using TuiAttribute = Terminal.Gui.Drawing.Attribute;

namespace TermRacer.Tui;

internal sealed class GameView : Runnable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(15);

    private readonly Game game;
    private readonly SceneRenderer renderer;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly List<CellRun> runs = [];
    private CellBuffer front = new(0, 0);
    private CellBuffer back = new(0, 0);
    private bool fullRedraw = true;

    public GameView(IApplication app, Game game)
    {
        this.game = game;
        renderer = new SceneRenderer(game.Track);
        Title = "TermRacer";
        app.ScreenChanged += (_, _) => fullRedraw = true;
        if (app.Driver is { } driver)
        {
            driver.ClearedContents += (_, _) => fullRedraw = true;
        }

        app.AddTimeout(TickInterval, Tick);
    }

    protected override bool OnClearingViewport() => true;

    protected override bool OnDrawingText(DrawContext? context) => true;

    protected override bool OnDrawingContent(DrawContext? context)
    {
        var width = Viewport.Width;
        var height = Viewport.Height;
        if (width <= 0 || height <= 0)
        {
            return true;
        }

        if (back.Width != width || back.Height != height)
        {
            back.Resize(width, height);
            front.Resize(width, height);
            fullRedraw = true;
        }

        renderer.Render(game, back);
        if (fullRedraw)
        {
            FrameDiff.Full(back, runs);
        }
        else
        {
            FrameDiff.Compute(front, back, runs);
        }

        fullRedraw = false;
        WriteRuns();
        (front, back) = (back, front);
        return true;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (KeyMap.Resolve(key) is not { } gameKey)
        {
            return false;
        }

        if (key.EventType == KeyEventType.Repeat && gameKey is GameKey.Confirm or GameKey.ToggleAutopilot)
        {
            return true;
        }

        game.Press(gameKey);
        if (game.ExitRequested)
        {
            RequestStop();
        }

        return true;
    }

    protected override bool OnKeyUp(Key key)
    {
        if (KeyMap.Resolve(key) is not { } gameKey)
        {
            return false;
        }

        game.Release(gameKey);
        return true;
    }

    private bool Tick()
    {
        var elapsed = stopwatch.Elapsed.TotalSeconds;
        stopwatch.Restart();
        game.Paused = !SceneRenderer.Fits(Viewport.Width, Viewport.Height);
        game.Advance(elapsed);
        if (game.ExitRequested)
        {
            RequestStop();
            return false;
        }

        SetNeedsDraw();
        return true;
    }

    private void WriteRuns()
    {
        TuiAttribute? current = null;
        foreach (var run in runs)
        {
            Move(run.Column, run.Row);
            for (var x = run.Column; x < run.Column + run.Length; x++)
            {
                var cell = back[x, run.Row];
                var attribute = TerminalColors.ToAttribute(cell);
                if (current != attribute)
                {
                    SetAttribute(attribute);
                    current = attribute;
                }

                AddRune(cell.Glyph);
            }
        }
    }
}
