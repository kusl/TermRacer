namespace TermRacer.Core;

public sealed class Track
{
    private readonly Vec2[] points;
    private readonly Vec2[] tangents;
    private readonly Vec2[] normals;
    private readonly double[] curvatures;

    private Track(TrackDefinition definition, Vec2[] points)
    {
        Definition = definition;
        this.points = points;
        var count = points.Length;
        tangents = new Vec2[count];
        normals = new Vec2[count];
        curvatures = new double[count];
        for (var i = 0; i < count; i++)
        {
            Length += points[i].DistanceTo(points[(i + 1) % count]);
        }

        Spacing = Length / count;
        for (var i = 0; i < count; i++)
        {
            var previous = points[(i - 1 + count) % count];
            var next = points[(i + 1) % count];
            var incoming = points[i] - previous;
            var outgoing = next - points[i];
            tangents[i] = (next - previous).Normalized();
            normals[i] = tangents[i].Perpendicular;
            curvatures[i] = Math.Atan2(incoming.Cross(outgoing), incoming.Dot(outgoing)) / (0.5 * (incoming.Length + outgoing.Length));
        }

        Gates = BuildGates();
        var startIndex = Wrap(-(int)Math.Round(definition.StartOffset / Spacing));
        StartPose = new Pose(points[startIndex], tangents[startIndex].Angle);
        var reach = WallOffset + 2;
        BoundsMin = new Vec2(points.Min(p => p.X) - reach, points.Min(p => p.Y) - reach);
        BoundsMax = new Vec2(points.Max(p => p.X) + reach, points.Max(p => p.Y) + reach);
    }

    public TrackDefinition Definition { get; }

    public string Name => Definition.Name;

    public int Count => points.Length;

    public double Length { get; }

    public double Spacing { get; }

    public double HalfWidth => Definition.HalfWidth;

    public double WallOffset => Definition.HalfWidth + Definition.Runoff;

    public double TrackLimit => Definition.HalfWidth + 0.6;

    public IReadOnlyList<Gate> Gates { get; }

    public Pose StartPose { get; }

    public Vec2 BoundsMin { get; }

    public Vec2 BoundsMax { get; }

    public int Wrap(int index) => (index % Count + Count) % Count;

    public Vec2 Point(int index) => points[Wrap(index)];

    public Vec2 Tangent(int index) => tangents[Wrap(index)];

    public Vec2 Normal(int index) => normals[Wrap(index)];

    public double Curvature(int index) => curvatures[Wrap(index)];

    public TrackProjection Project(Vec2 position)
    {
        var best = 0;
        var bestT = 0.0;
        var bestDistance = double.MaxValue;
        for (var i = 0; i < points.Length; i++)
        {
            var start = points[i];
            var end = points[i + 1 == points.Length ? 0 : i + 1];
            var t = Geometry.ClosestParameter(start, end, position);
            var distance = (position - Vec2.Lerp(start, end, t)).LengthSquared;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
                bestT = t;
            }
        }

        var from = points[best];
        var to = points[Wrap(best + 1)];
        var point = Vec2.Lerp(from, to, bestT);
        var tangent = (to - from).Normalized();
        var offset = position - point;
        var length = offset.Length;
        var side = offset.Dot(tangent.Perpendicular) >= 0 ? 1 : -1;
        var away = length > 1e-9 ? offset / length : tangent.Perpendicular * side;
        return new TrackProjection(best, bestT, point, tangent, away, length, length * side, (best + bestT) * Spacing);
    }

    public Surface SurfaceAt(in TrackProjection projection) => projection.Distance <= TrackLimit ? Surface.Tarmac : Surface.Grass;

    public Surface SurfaceAt(Vec2 position) => SurfaceAt(Project(position));

    public static Track Build(TrackDefinition definition)
    {
        var sections = definition.Sections;
        if (sections.Count == 0 || sections.Any(section => section.Length <= 0))
        {
            throw new ArgumentException("Track needs sections with positive length.", nameof(definition));
        }

        var starts = new (Vec2 Position, double Heading, double Distance)[sections.Count];
        var position = definition.Origin;
        var heading = definition.Heading;
        var total = 0.0;
        for (var i = 0; i < sections.Count; i++)
        {
            starts[i] = (position, heading, total);
            (position, heading) = Advance(position, heading, sections[i], sections[i].Length);
            total += sections[i].Length;
        }

        var closure = position - definition.Origin;
        if (closure.Length > 0.5 || Math.Abs(Geometry.NormalizeAngle(heading - definition.Heading)) > 1e-3)
        {
            throw new ArgumentException("Track sections do not form a closed loop.", nameof(definition));
        }

        var count = Math.Max(3, (int)Math.Round(total / definition.Spacing));
        var step = total / count;
        var points = new Vec2[count];
        var current = 0;
        for (var k = 0; k < count; k++)
        {
            var distance = k * step;
            while (current < starts.Length - 1 && starts[current + 1].Distance <= distance)
            {
                current++;
            }

            var (origin, direction, offset) = starts[current];
            var (point, _) = Advance(origin, direction, sections[current], distance - offset);
            points[k] = point - closure * (distance / total);
        }

        return new Track(definition, points);
    }

    private static (Vec2 Position, double Heading) Advance(Vec2 position, double heading, TrackSection section, double distance)
    {
        if (!section.IsArc)
        {
            return (position + Vec2.FromAngle(heading) * distance, heading);
        }

        var sign = Math.Sign(section.Degrees);
        var center = position + Vec2.FromAngle(heading).Perpendicular * (section.Radius * sign);
        var turned = heading + sign * distance / section.Radius;
        return (center - Vec2.FromAngle(turned).Perpendicular * (section.Radius * sign), turned);
    }

    private Gate[] BuildGates()
    {
        var count = Math.Max(1, Definition.Checkpoints);
        var reach = WallOffset + 1;
        var gates = new Gate[count];
        for (var k = 0; k < count; k++)
        {
            var index = (int)Math.Round((double)k * Count / count) % Count;
            gates[k] = new Gate(k, points[index] - normals[index] * reach, points[index] + normals[index] * reach, tangents[index]);
        }

        return gates;
    }
}
