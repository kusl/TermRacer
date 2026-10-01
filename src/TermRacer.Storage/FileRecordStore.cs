using System.IO.Compression;
using System.Text;
using TermRacer.Core;

namespace TermRacer.Storage;

public sealed class FileRecordStore(string directory, string? display = null) : IRecordStore
{
    public const string LapsFile = "laps.tsv";
    public const string AutopilotFile = "autopilot.tsv";
    public const string ReplayFolder = "replays";
    public const string ReplayExtension = ".tsv.gz";

    private static readonly UTF8Encoding Utf8 = new(false);

    private bool logChecked;

    public string Root => directory;

    public string Location => display ?? directory;

    public string? Problem { get; private set; }

    private string LapsPath => Path.Combine(directory, LapsFile);

    private string AutopilotPath => Path.Combine(directory, AutopilotFile);

    private string ReplayDirectory => Path.Combine(directory, ReplayFolder);

    public static bool IsValidName(string name) =>
        name.Length is > 0 and <= 120 && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    public IReadOnlyList<LapRecord> LoadLaps() => Attempt(
        () =>
        {
            if (!File.Exists(LapsPath))
            {
                logChecked = true;
                return [];
            }

            var log = LapLogFormat.Parse(File.ReadLines(LapsPath, Utf8));
            if (!log.Current)
            {
                WriteAtomically(LapsPath, stream => WriteLines(stream, LapLogFormat.Lines(log.Records, log.Unreadable)));
            }

            logChecked = true;
            return log.Records;
        },
        []);

    public IReadOnlyCollection<string> ListReplays() => Attempt<IReadOnlyCollection<string>>(
        () => Directory.Exists(ReplayDirectory)
            ? [.. Directory.EnumerateFiles(ReplayDirectory, "*" + ReplayExtension)
                .Select(path => Path.GetFileName(path)[..^ReplayExtension.Length])
                .Where(IsValidName)]
            : [],
        []);

    public bool AppendLap(LapRecord record) => Attempt(
        () =>
        {
            if (!logChecked)
            {
                LoadLaps();
            }

            Directory.CreateDirectory(directory);
            var text = new StringBuilder();
            if (!File.Exists(LapsPath))
            {
                text.Append(LapLogFormat.Banner).Append('\n').Append(LapLogFormat.Header).Append('\n');
            }

            text.Append(LapLogFormat.Format(record)).Append('\n');
            File.AppendAllText(LapsPath, text.ToString(), Utf8);
            return true;
        },
        false);

    public bool SaveReplay(string name, LapRecord record, Replay replay) => IsValidName(name) && Attempt(
        () =>
        {
            WriteAtomically(ReplayPath(name), stream =>
            {
                using var gzip = new GZipStream(stream, CompressionLevel.Optimal);
                using var writer = new StreamWriter(gzip, Utf8);
                ReplayFormat.Write(writer, record, replay);
            });
            return true;
        },
        false);

    public Replay? LoadReplay(string name) => !IsValidName(name) ? null : Attempt(
        () =>
        {
            var path = ReplayPath(name);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(path);
                using var gzip = new GZipStream(stream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Utf8);
                return ReplayFormat.Read(reader);
            }
            catch (InvalidDataException)
            {
                return null;
            }
        },
        null);

    public void DeleteReplay(string name)
    {
        if (IsValidName(name))
        {
            Attempt(
                () =>
                {
                    File.Delete(ReplayPath(name));
                    return true;
                },
                false);
        }
    }

    public AutopilotModel? LoadAutopilot() => Attempt(
        () =>
        {
            if (!File.Exists(AutopilotPath))
            {
                return null;
            }

            using var reader = new StreamReader(AutopilotPath, Utf8);
            return AutopilotFormat.Read(reader);
        },
        null);

    public bool SaveAutopilot(AutopilotModel model) => Attempt(
        () =>
        {
            WriteAtomically(AutopilotPath, stream =>
            {
                using var writer = new StreamWriter(stream, Utf8);
                AutopilotFormat.Write(writer, model);
            });
            return true;
        },
        false);

    private static void WriteLines(Stream stream, IEnumerable<string> lines)
    {
        using var writer = new StreamWriter(stream, Utf8);
        foreach (var line in lines)
        {
            writer.Write(line);
            writer.Write('\n');
        }
    }

    private static void WriteAtomically(string path, Action<Stream> write)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            write(stream);
        }

        File.Move(temporary, path, overwrite: true);
    }

    private string ReplayPath(string name) => Path.Combine(ReplayDirectory, name + ReplayExtension);

    private T Attempt<T>(Func<T> action, T fallback)
    {
        try
        {
            return action();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Problem ??= exception.Message;
            return fallback;
        }
    }
}
