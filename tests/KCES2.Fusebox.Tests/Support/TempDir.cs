namespace KCES2.Fusebox.Tests.Support;

internal sealed class TempDir : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "kces2tests_" + Guid.NewGuid().ToString("N"));

    public TempDir()
    {
        Directory.CreateDirectory(_path);
    }

    /// <summary>Makes file unreadable (Unix mode 000).</summary>
    /// <remarks>On Windows or as root, file stays readable, so test is skipped.</remarks>
    public static void MakeUnreadable(string p)
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            Xunit.Assert.Skip("needs Unix file modes and a user that is not root");
        }
        else
        {
            System.IO.File.SetUnixFileMode(p, UnixFileMode.None);
        }
    }

    public string File(string name) => Path.Combine(_path, name);

    public string Write(string name, string text)
    {
        string p = File(name);
        Directory.CreateDirectory(Path.GetDirectoryName(p) ?? throw new InvalidOperationException());
        System.IO.File.WriteAllText(p, text, new System.Text.UTF8Encoding(false));
        return p;
    }

    public string WriteBytes(string name, byte[] bytes)
    {
        string p = File(name);
        System.IO.File.WriteAllBytes(p, bytes);
        return p;
    }

    public void Dispose()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                foreach (string f in Directory.GetFiles(_path, "*", SearchOption.AllDirectories))
                {
                    System.IO.File.SetUnixFileMode(f, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
            }

            Directory.Delete(_path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
