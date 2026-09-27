namespace TermRacer.Core;

public sealed record CarSpec
{
    public static CarSpec Default { get; } = new();

    public double Length { get; init; } = 4.8;

    public double Width { get; init; } = 2.1;

    public double Wheelbase { get; init; } = 2.8;

    public double MaxSteerAngle { get; init; } = 0.6;

    public double SteerRate { get; init; } = 4.5;

    public double MaxAcceleration { get; init; } = 9;

    public double Power { get; init; } = 190;

    public double AeroDrag { get; init; } = 0.0018;

    public double RollingResistance { get; init; } = 0.3;

    public double BrakeDeceleration { get; init; } = 16;

    public double ReverseAcceleration { get; init; } = 5;

    public double MaxReverseSpeed { get; init; } = 9;

    public double TarmacGrip { get; init; } = 12.5;

    public double GrassGrip { get; init; } = 5.5;

    public double GrassDrag { get; init; } = 3.5;

    public double GrassPowerFactor { get; init; } = 0.55;

    public double WallRestitution { get; init; } = 0.3;

    public double WallFriction { get; init; } = 0.75;
}
