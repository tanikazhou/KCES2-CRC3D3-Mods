#nullable enable
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace KCES2.Fusebox;

/// <summary>Simple scene-ready signal, raised X frames after <see cref="SceneManager.sceneLoaded"/>.</summary>
/// <remarks>
/// <para>Unity raises <see cref="SceneManager.sceneLoaded"/> before scene objects of KCES2 and its games finish their setup,
/// so features wait some frames, e.g. EditAssist 2 frames.</para>
/// <para>Plugin calls <see cref="Tick"/> once per frame from its Update and <see cref="Stop"/> from its OnDestroy.</para>
/// </remarks>
/// <example>
/// <code>
/// _sceneReady = new SceneReadyDelay(2);
///
/// private void Update()
/// {
///     if (_sceneReady.Tick()) Guard.SceneReadyAll();
///     Guard.TickAll();
/// }
/// </code>
/// </example>
internal sealed class SceneReadyDelay
{
    private readonly int _frames;
    private int _countdown = -1;

    /// <summary>Initializes delay and starts listening for scene loads.</summary>
    /// <param name="frames">Calls of <see cref="Tick"/> to skip after scene load; scene ready comes on next call.</param>
    public SceneReadyDelay(int frames)
    {
        _frames = frames;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Counts one frame.</summary>
    /// <returns><see langword="true"/> in frame when scene is ready; otherwise, <see langword="false"/>.</returns>
    public bool Tick() => _countdown >= 0 && _countdown-- == 0;

    /// <summary>Stops listening for scene loads.</summary>
    public void Stop() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _countdown = _frames;
}
