using CRC;
using CRC3D3.KeyHints.Rows;
using CrcMove.Controller;
using HarmonyLib;

namespace CRC3D3.KeyHints.Game;

/// <summary>Game hooks that keep contexts "Field" and "Dance" up to date, event driven.</summary>
/// <remarks>
/// "Field" is read again after each change of its inputs: input layer switch (<c>GameInputLayer.IsInputLayerEnabled</c> setter),
/// active layer list (<c>InputManager.SetInputLayerActive</c>, <c>ClearActiveInputLayer</c>), player controller start and end.
/// "Dance" follows <c>DanceMain.OnSceneLoadCompleted</c> and <c>DanceMain.OnDestroy</c>.
/// </remarks>
internal static class ContextHooks
{
    /// <summary>Feature name in plugin guard.</summary>
    public const string Feature = "Contexts";

    private const int LogLimit = 50;

    private static int _logged;

    /// <summary>Raised once per real change of a context.</summary>
    public static event Action ContextsChanged;

    /// <summary>Gets current context state.</summary>
    public static ContextSet Contexts { get; } = new();

    /// <summary>Reads "Field" from game and applies it.</summary>
    public static void RefreshField() => Apply(FieldContext.Name, FieldContext.Read());

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameInputLayer), nameof(GameInputLayer.IsInputLayerEnabled), MethodType.Setter)]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.SetInputLayerActive))]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.ClearActiveInputLayer))]
    [HarmonyPatch(typeof(PlayerMoveController), "Awake")]
    [HarmonyPatch(typeof(PlayerMoveController), "OnDestroy")]
    private static void AfterFieldInputChange() => Plugin.Guard.Run(Feature, RefreshField);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DanceMain), "OnSceneLoadCompleted")]
    private static void AfterDanceLoaded(DanceMain __instance) =>
        Plugin.Guard.Run(Feature, () => Apply(DanceContext.Name, DanceContext.Loaded(__instance)));

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DanceMain), "OnDestroy")]
    private static void AfterDanceDestroyed(DanceMain __instance) =>
        Plugin.Guard.Run(Feature, () => Apply(DanceContext.Name, DanceContext.Destroyed(__instance)));

    private static void Apply(string name, bool on)
    {
        if (!Contexts.Set(name, on))
        {
            return;
        }

        if (_logged < LogLimit)
        {
            _logged++;
            Plugin.Log.LogInfo($"Key hints: context {name} {(on ? "on" : "off")}");
            if (_logged == LogLimit)
            {
                Plugin.Log.LogInfo("Key hints: context log stopped");
            }
        }

        ContextsChanged?.Invoke();
    }
}
