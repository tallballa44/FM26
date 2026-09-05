using System;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace FM26PlayerExport;

public static class PluginConfig
{
    public const int DefaultMaxRowsToExport = 5000;
    public const int SafeHardMaxRowsToExport = 10000;

    private const int DefaultMaxScrollAttempts = 500;
    private const int SafeHardMaxScrollAttempts = 20000;

    public static ConfigEntry<int> MaxRowsToExport;
    public static int EffectiveMaxRowsToExport { get; private set; } = DefaultMaxRowsToExport;

    public static void Init(ConfigFile config, ManualLogSource log)
    {
        MaxRowsToExport = config.Bind(
            "Export",
            "MaxRowsToExport",
            DefaultMaxRowsToExport,
            new ConfigDescription(
                "Maximum number of selected rows exported by list handlers. FM26 player lists currently present only the first 10000 rows in the UI, so higher values are clamped for safety.",
                new AcceptableValueRange<int>(1, SafeHardMaxRowsToExport)));

        Refresh(log);
    }

    public static int GetMaxScrollAttemptsForRows(int maxRows)
    {
        int attempts = (maxRows / 5) + 100;

        if (attempts < DefaultMaxScrollAttempts)
            return DefaultMaxScrollAttempts;

        if (attempts > SafeHardMaxScrollAttempts)
            return SafeHardMaxScrollAttempts;

        return attempts;
    }

    private static void Refresh(ManualLogSource log)
    {
        int requested = MaxRowsToExport?.Value ?? DefaultMaxRowsToExport;
        int effective = Math.Max(1, Math.Min(requested, SafeHardMaxRowsToExport));

        if (MaxRowsToExport != null && requested != effective)
        {
            MaxRowsToExport.Value = effective;
            log?.LogWarning($"[FM26Export.CONFIG] MaxRowsToExport was outside the safe range and was adjusted to {effective}.");
        }

        EffectiveMaxRowsToExport = effective;
        log?.LogInfo($"[FM26Export.CONFIG] MaxRowsToExport={EffectiveMaxRowsToExport}.");
    }
}
