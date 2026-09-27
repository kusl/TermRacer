namespace TermRacer.Rendering;

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Hex(uint value) => new((byte)(value >> 16), (byte)(value >> 8), (byte)value);

    public static Rgb Lerp(Rgb from, Rgb to, double t) =>
        new(Channel(from.R + (to.R - from.R) * t), Channel(from.G + (to.G - from.G) * t), Channel(from.B + (to.B - from.B) * t));

    public Rgb Scale(double factor) => new(Channel(R * factor), Channel(G * factor), Channel(B * factor));

    private static byte Channel(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
