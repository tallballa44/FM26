using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace FM26PlayerExport;

[BepInPlugin("com.koda.fm26.playerexport", "FM26 Player Export", "5.1.0")]
public class Plugin : BasePlugin
{
	internal static ManualLogSource Log;

	private static ExportBehaviour _behaviour;

	public override void Load()
	{
		Log = ((BasePlugin)this).Log;
		Log.LogInfo((object)"[FM26Export v5.1] Carregado! Atalhos: [F9 ou Ctrl+P] = exportar | [F8] = re-escanear");
		PluginConfig.Init(((BasePlugin)this).Config, Log);
		_behaviour = ((BasePlugin)this).AddComponent<ExportBehaviour>();
	}

	public override bool Unload()
	{
		Log.LogInfo((object)"[FM26Export v5.1] Descarregando plugin...");
		if ((Object)(object)_behaviour != (Object)null)
		{
			Object.Destroy((Object)(object)_behaviour);
		}
		return ((BasePlugin)this).Unload();
	}
}
