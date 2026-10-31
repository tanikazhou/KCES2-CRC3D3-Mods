using BepInEx.Logging;

namespace KCES2.Fusebox.Tests.Support;

/// <summary>Real BepInEx <see cref="ManualLogSource"/> with listener that collects its output.</summary>
internal sealed class LogCapture : IDisposable
{
    public readonly ManualLogSource Source = new("test");
    public readonly List<(LogLevel Level, string Text)> Lines = [];

    public LogCapture()
    {
        Source.LogEvent += (_, e) => Lines.Add((e.Level, e.Data == null ? "" : e.Data.ToString()));
    }

    public IEnumerable<string> Of(LogLevel level) => Lines.Where(l => l.Level == level).Select(l => l.Text);

    public void Dispose() => Source.Dispose();
}
