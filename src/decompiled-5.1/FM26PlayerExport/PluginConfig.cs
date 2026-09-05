using System;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace FM26PlayerExport;

public static class PluginConfig
{
	public const int DefaultMaxRowsToExport = 5000;

	public const int SafeHardMaxRowsToExport = 10000;

	private const int DefaultMaxScrollAttempts = 500;

	private const int SafeHardMaxScrollAttempts = 20000;

	public static ConfigEntry<int> MaxRowsToExport;

	public static int EffectiveMaxRowsToExport { get; private set; } = 5000;

	public static void Init(ConfigFile config, ManualLogSource log)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		MaxRowsToExport = config.Bind<int>("Export", "MaxRowsToExport", 5000, new ConfigDescription("Maximum number of selected rows exported by list handlers. FM26 player lists currently present only the first 10000 rows in the UI, so higher values are clamped for safety.", (AcceptableValueBase)(object)new AcceptableValueRange<int>(1, 10000), Array.Empty<object>()));
		Refresh(log);
	}

	public static int GetMaxScrollAttemptsForRows(int maxRows)
	{
		int num = maxRows / 5 + 100;
		if (num < 500)
		{
			return 500;
		}
		if (num > 20000)
		{
			return 20000;
		}
		return num;
	}

	private static void Refresh(ManualLogSource log)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		int num = ((MaxRowsToExport != null) ? MaxRowsToExport.Value : 5000);
		int num2 = Math.Max(1, Math.Min(num, 10000));
		bool flag = default(bool);
		ManualLogSource val;
		if (MaxRowsToExport != null && num != num2)
		{
			MaxRowsToExport.Value = num2;
			val = log;
			if (val != null)
			{
				BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(77, 1, ref flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export.CONFIG] MaxRowsToExport fora do intervalo seguro; ajustado para ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(num2);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
				}
				val.LogWarning(val2);
			}
		}
		EffectiveMaxRowsToExport = num2;
		val = log;
		if (val != null)
		{
			BepInExInfoLogInterpolatedStringHandler val3 = new BepInExInfoLogInterpolatedStringHandler(37, 1, ref flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[FM26Export.CONFIG] MaxRowsToExport=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(EffectiveMaxRowsToExport);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(".");
			}
			val.LogInfo(val3);
		}
	}
}
