using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace FM26PlayerExport;

[BepInPlugin("com.koda.fm26.playerexport", "FM26 Player Export", "5.1.6")]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    private static ExportBehaviour _behaviour;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("[FM26Export Patch 0.6] Loaded. Hotkeys: F9 or Ctrl+P = export | F8 = UI diagnostics");
        PluginConfig.Init(base.Config, Log);
        _behaviour = AddComponent<ExportBehaviour>();
    }

    public override bool Unload()
    {
        Log.LogInfo("[FM26Export Patch 0.6] Unloading plugin...");
        if (_behaviour != null)
            Object.Destroy(_behaviour);

        return base.Unload();
    }
}
