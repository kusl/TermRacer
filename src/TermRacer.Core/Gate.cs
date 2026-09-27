namespace TermRacer.Core;

public readonly record struct Gate(int Index, Vec2 A, Vec2 B, Vec2 Direction)
{
    public Vec2 Center => (A + B) / 2;
}
