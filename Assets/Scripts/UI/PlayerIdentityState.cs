using System;
using UnityEngine;

/// <summary>
/// Short display name for this play session only. It is not an online
/// account or globally unique identifier, and is cleared at the next launch.
/// </summary>
public static class PlayerIdentityState
{
    public const int MinLength = 1;
    public const int MaxLength = 18;

    private static string currentId = string.Empty;
    private static bool confirmedThisLaunch;

    // Covers Unity Play Mode with domain reload disabled as well as builds.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void ResetForNewLaunch()
    {
        currentId = string.Empty;
        confirmedThisLaunch = false;
    }

    public static string CurrentId => confirmedThisLaunch ? currentId : string.Empty;
    public static bool HasId => confirmedThisLaunch && !string.IsNullOrEmpty(currentId);

    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length < MinLength || value.Length > MaxLength) return false;

        foreach (char c in value)
        {
            // Safe for TMP poster text while allowing Chinese IME, English,
            // punctuation and ordinary nicknames rather than ASCII only.
            if (char.IsControl(c) || c == '<' || c == '>')
                return false;
        }
        return true;
    }

    public static bool TrySave(string proposed, out string message)
    {
        string value = (proposed ?? string.Empty).Trim();
        if (!IsValid(value))
        {
            message = string.IsNullOrWhiteSpace(value)
                ? "请先输入名号。" : "请换一个较短的名号。";
            return false;
        }

        currentId = value;
        confirmedThisLaunch = true;
        message = string.Empty;
        return true;
    }
}
