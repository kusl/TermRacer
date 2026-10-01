namespace TermRacer.Storage.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("termracer-").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
        }
    }
}
