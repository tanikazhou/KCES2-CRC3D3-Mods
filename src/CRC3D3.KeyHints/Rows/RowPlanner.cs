using BepInEx.Configuration;
using CRC3D3.KeyHints.Discovery;
using UnityEngine;

namespace CRC3D3.KeyHints.Rows;

/// <summary>Chooses and orders shown key hint rows.</summary>
internal static class RowPlanner
{
    /// <summary>Label of toggle key row.</summary>
    public const string ToggleLabel = "Hide hints";

    /// <remarks>Reused between plans: indexes of kept hints, sorted in place.</remarks>
    private static readonly List<int> _kept = [];

    /// <summary>Fills rows with hints that can show now, sorted by order, then discovery order.</summary>
    /// <param name="hints">Found hints.</param>
    /// <param name="states">Values of this read, same index as <paramref name="hints"/>.</param>
    /// <param name="contexts">Context state.</param>
    /// <param name="toggleKey">Key that hides hints; its row comes last when any row shows.</param>
    /// <param name="rows">List that gets rows; cleared first.</param>
    /// <remarks>Hint shows only when active, with key and with one allowed context. Main thread only.</remarks>
    public static void Plan(IReadOnlyList<FoundHint> hints, IReadOnlyList<HintState> states, ContextSet contexts, KeyboardShortcut toggleKey,
        List<RowText> rows)
    {
        rows.Clear();
        _kept.Clear();
        for (int i = 0; i < hints.Count; i++)
        {
            HintState s = states[i];
            if (s.Active && s.Key.MainKey != KeyCode.None && contexts.AllowsAny(hints[i].Contexts))
            {
                Insert(i, hints, states);
            }
        }

        foreach (int i in _kept)
        {
            rows.Add(new RowText(KeyText.Of(states[i].Key), states[i].Label));
        }

        if (rows.Count > 0 && toggleKey.MainKey != KeyCode.None)
        {
            rows.Add(new RowText(KeyText.Of(toggleKey), ToggleLabel));
        }
    }

    /// <summary>Inserts hint index after all kept hints that sort before or equal to it.</summary>
    private static void Insert(int index, IReadOnlyList<FoundHint> hints, IReadOnlyList<HintState> states)
    {
        int at = _kept.Count;
        while (at > 0 && Before(index, _kept[at - 1], hints, states))
        {
            at--;
        }

        _kept.Insert(at, index);
    }

    private static bool Before(int a, int b, IReadOnlyList<FoundHint> hints, IReadOnlyList<HintState> states) =>
        states[a].Order != states[b].Order ? states[a].Order < states[b].Order : hints[a].Index < hints[b].Index;
}
