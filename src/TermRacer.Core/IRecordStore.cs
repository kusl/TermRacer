namespace TermRacer.Core;

public interface IRecordStore
{
    string Location { get; }

    string? Problem { get; }

    IReadOnlyList<LapRecord> LoadLaps();

    IReadOnlyCollection<string> ListReplays();

    bool AppendLap(LapRecord record);

    bool SaveReplay(string name, LapRecord record, Replay replay);

    Replay? LoadReplay(string name);

    void DeleteReplay(string name);

    AutopilotModel? LoadAutopilot();

    bool SaveAutopilot(AutopilotModel model);
}
