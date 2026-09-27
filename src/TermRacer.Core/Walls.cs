namespace TermRacer.Core;

public static class Walls
{
    public static double? Resolve(ref CarState state, Track track, CarSpec spec, in TrackProjection projection)
    {
        var limit = track.WallOffset - spec.Width / 2;
        if (projection.Distance <= limit)
        {
            return null;
        }

        var outward = projection.Away;
        var normalSpeed = state.Velocity.Dot(outward);
        var velocity = state.Velocity;
        if (normalSpeed > 0)
        {
            var tangential = velocity - outward * normalSpeed;
            velocity = tangential * spec.WallFriction - outward * (normalSpeed * spec.WallRestitution);
        }

        state = state with { Position = projection.Point + outward * limit, Velocity = velocity };
        return Math.Max(0, normalSpeed);
    }
}
