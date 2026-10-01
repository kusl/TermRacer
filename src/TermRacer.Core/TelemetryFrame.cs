namespace TermRacer.Core;

public readonly record struct TelemetryFrame(
    double Time,
    CarState Car,
    ControlInput Input,
    bool Autopilot,
    bool OffTrack,
    bool Fault,
    double Odometer,
    int Segment);
