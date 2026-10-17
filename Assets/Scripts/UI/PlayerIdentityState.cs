using System;
using UnityEngine;

/// <summary>
/// Local display ID only. This is not an account, authentication token or
/// globally unique identifier; it is stored in this device/browser only.
/// </summary>
public static class PlayerIdentityState
{
    private const string PrefsKey = "Guandu.PlayerDisplayId.v1";
    public const int MinLength = 3;
    public const int MaxLength = 18;

    // Session gate is separate from the locally remembered suggestion.
    // Unity SubsystemRegistration also runs when Play Mode skips domain reload.
    private static bool confirmedThisLaunch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void ResetForNewLaunch()
    {
        confirmedThisLaunch = false;
    }

    public static string CurrentId
    {
        get
        {
            string value = PlayerPrefs.GetString(PrefsKey, "");
            return IsValid(value) ? value : "";
        }
    }

    public static bool HasId => confirmedThisLaunch && !string.IsNullOrEmpty(CurrentId);

    public static bool IsValid(string value)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length < MinLength || value.Length > MaxLength) return false;
        foreach (char c in value)
        {
            if ((c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') || c == '_' || c == '-')
                continue;
            return false;
        }
        return true;
    }

    public static bool TrySave(string proposed, out string message)
    {
        string value = (proposed ?? "").Trim();
        if (!IsValid(value))
        {
            message = "请输入3–18位英文字母、数字、下划线或短横线。";
            return false;
        }
        PlayerPrefs.SetString(PrefsKey, value);
        PlayerPrefs.Save();
        confirmedThisLaunch = true;
        message = "";
        return true;
    }
}
