using BepInEx.Logging;

namespace ThronefallMP;

public static class Ext
{
    public static bool LogErrorFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowError{section}", true).Value;
    }
    
    public static void LogErrorFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowError{section}", true);
        if (value.Value)
        {
            source.LogError($"{section}> " + obj);
        }
    }

    public static bool LogWarningFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowWarning{section}", true).Value;
    }
    
    public static void LogWarningFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowWarning{section}", true);
        if (value.Value)
        {
            source.LogWarning($"{section}> " + obj);
        }
    }

    public static bool LogInfoFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowInfo{section}", true).Value;
    }
    
    public static void LogInfoFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowInfo{section}", true);
        if (value.Value)
        {
            source.LogInfo($"{section}> " + obj);
        }
    }

    public static bool LogDebugFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowDebug{section}", true).Value;
    }
    
    public static void LogDebugFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowDebug{section}", true);
        if (value.Value)
        {
            source.LogDebug(obj);
        }
    }
}