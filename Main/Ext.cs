using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace ThronefallMP;

public static class Ext
{
    public static bool LogErrorFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowError{section}", true).Value;
    }
    
    public static void LogErrorFiltered(this ManualLogSource source, string section, BepInExInfoLogInterpolatedStringHandler handler)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowError{section}", true);
        if (value.Value)
        {
            source.LogError(handler);
        }
    }
    
    public static void LogErrorFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowError{section}", true);
        if (value.Value)
        {
            source.LogError(obj);
        }
    }

    public static bool LogWarningFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowWarning{section}", true).Value;
    }
    
    public static void LogWarningFiltered(this ManualLogSource source, string section, BepInExInfoLogInterpolatedStringHandler handler)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowWarning{section}", true);
        if (value.Value)
        {
            source.LogWarning(handler);
        }
    }
    
    public static void LogWarningFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowWarning{section}", true);
        if (value.Value)
        {
            source.LogWarning(obj);
        }
    }

    public static bool LogInfoFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowInfo{section}", true).Value;
    }
    
    public static void LogInfoFiltered(this ManualLogSource source, string section, BepInExInfoLogInterpolatedStringHandler handler)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowInfo{section}", true);
        if (value.Value)
        {
            source.LogInfo(handler);
        }
    }
    
    public static void LogInfoFiltered(this ManualLogSource source, string section, object obj)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowInfo{section}", true);
        if (value.Value)
        {
            source.LogInfo(obj);
        }
    }

    public static bool LogDebugFiltered(string section)
    {
        return Plugin.Instance.Config.Bind("Debug", $"ShowDebug{section}", true).Value;
    }
    
    public static void LogDebugFiltered(this ManualLogSource source, string section, BepInExInfoLogInterpolatedStringHandler handler)
    {
        var value = Plugin.Instance.Config.Bind("Debug", $"ShowDebug{section}", true);
        if (value.Value)
        {
            source.LogDebug(handler);
        }
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