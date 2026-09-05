using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public class MatchStatsExportHandler : IExportHandler
{
	private class ScrapedTab
	{
		public string TabName;

		public List<string> Headers;

		public List<List<string>> Rows;
	}

	private List<ScrapedTab> _accumulatedTabs = new List<ScrapedTab>();

	private string _matchContext = "";

	private VisualElement _matchStatsRoot;

	private int _currentTabIdx;

	private float _nextStepTime;

	private float _timeoutTime;

	private string _lastTableHash = "";

	private string[] _tabNames = new string[6] { "KeyStatistics", "Passing", "Attacking", "Defending", "Goalkeeping", "SetPieces" };

	private List<VisualElement> _tabElements = new List<VisualElement>();

	private string _contextHome = "";

	private string _contextAway = "";

	private bool IsElementVisible(VisualElement el)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		if (el == null)
		{
			return false;
		}
		try
		{
			if ((int)el.resolvedStyle.display == 1)
			{
				return false;
			}
		}
		catch
		{
		}
		if (el.parent != null && el.parent.name != "PanelManager-container")
		{
			return IsElementVisible(el.parent);
		}
		return true;
	}

	public bool TryStartCapture(VisualElement root, out string errorMessage)
	{
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		errorMessage = string.Empty;
		_matchStatsRoot = UIUtils.FindByName(root, "MatchStatsStandAlone");
		if (_matchStatsRoot == null)
		{
			return false;
		}
		_tabElements.Clear();
		VisualElement matchStatsRoot = _matchStatsRoot;
		object obj;
		if (matchStatsRoot == null)
		{
			obj = null;
		}
		else
		{
			IPanel panel = matchStatsRoot.panel;
			obj = ((panel != null) ? panel.visualTree : null);
		}
		VisualElement el = (VisualElement)obj;
		string[] tabNames = _tabNames;
		foreach (string name in tabNames)
		{
			VisualElement val = UIUtils.FindByName(el, name);
			if (val != null)
			{
				_tabElements.Add(val);
			}
		}
		if (_tabElements.Count < 2)
		{
			errorMessage = "[FM26Export.MatchStats] Abas (KeyStatistics, Passing...) não foram encontradas na memória da UI da Partida.";
			return false;
		}
		_accumulatedTabs.Clear();
		_currentTabIdx = 0;
		_nextStepTime = 0f;
		_lastTableHash = "";
		_contextHome = "";
		_contextAway = "";
		_matchContext = ObterContextoDaPartida();
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(72, 1, ref flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export.MatchStats] Macro automática iniciada! Abas encontradas: ");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_tabElements.Count);
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("/6.");
		}
		log.LogInfo(val2);
		SafeClick(_tabElements[0]);
		_nextStepTime = Time.unscaledTime + 1f;
		_timeoutTime = Time.unscaledTime + 5f;
		return true;
	}

	public bool CaptureStep()
	{
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Expected O, but got Unknown
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Expected O, but got Unknown
		if (Time.unscaledTime < _nextStepTime)
		{
			return false;
		}
		if (_currentTabIdx >= _tabElements.Count)
		{
			return true;
		}
		VisualElement matchStatsRoot = _matchStatsRoot;
		object obj;
		if (matchStatsRoot == null)
		{
			obj = null;
		}
		else
		{
			IPanel panel = matchStatsRoot.panel;
			obj = ((panel != null) ? panel.visualTree : null);
		}
		VisualElement root = (VisualElement)obj;
		List<VisualElement> list = new List<VisualElement>();
		FindAllByName(root, "StreamedTable", list);
		VisualElement val = null;
		VisualElement val2 = null;
		foreach (VisualElement item in list)
		{
			if (IsElementVisible(item))
			{
				VisualElement val3 = UIUtils.FindByName(item, "View");
				if (val3 != null && val3.childCount > 0)
				{
					val = item;
					val2 = val3;
					break;
				}
			}
		}
		bool flag = default(bool);
		if (val != null && val2 != null)
		{
			List<string> list2 = new List<string>();
			List<int> list3 = new List<int>();
			VisualElement val4 = UIUtils.FindByName(val, "column-headers");
			if (val4 != null)
			{
				for (int i = 0; i < val4.childCount; i++)
				{
					string text = UIUtils.CollectFirstTooltip(val4.ElementAt(i));
					string text2 = UIUtils.CollectFirstText(val4.ElementAt(i));
					string text3 = ((!string.IsNullOrWhiteSpace(text)) ? text : text2);
					string text4 = ((text3 != null) ? text3.ToLowerInvariant() : "");
					if (!text4.Contains("condi") && !text4.Contains("cora") && !text4.Contains("fit") && !text4.Contains("ção"))
					{
						string text5 = ((text3 != null) ? UIUtils.Esc(text3.Trim()) : $"Col{i}");
						if (!text5.ToLower().Contains("condi") && !text5.ToLower().Contains("condition"))
						{
							list2.Add(text5);
							list3.Add(i);
						}
					}
				}
			}
			if (list2.Count == 0)
			{
				list2.Add("Dados");
				list3.Add(0);
			}
			if (list2.Count > 0)
			{
				string text6 = string.Join(" ", list2).ToLowerInvariant();
				if (text6.Contains("name") || text6.Contains("time") || text6.Contains("distance") || text6.Contains("rating") || text6.Contains("goals"))
				{
					UIUtils.GameLang = "en";
				}
				else if (text6.Contains("nombre") || text6.Contains("pases") || text6.Contains("goles") || text6.Contains("asistencias") || text6.Contains("calificación"))
				{
					UIUtils.GameLang = "es";
				}
				else
				{
					UIUtils.GameLang = "pt";
				}
			}
			string text7 = UIUtils.CollectFirstText(_tabElements[_currentTabIdx]);
			if (string.IsNullOrEmpty(text7))
			{
				text7 = "Aba " + _currentTabIdx;
			}
			ScrapedTab scrapedTab = new ScrapedTab
			{
				TabName = text7,
				Headers = list2,
				Rows = new List<List<string>>()
			};
			for (int j = 0; j < scrapedTab.Headers.Count; j++)
			{
				string text8 = scrapedTab.Headers[j];
				switch (text8)
				{
				case "Desarmes Conseguidos":
				case "Tackles Won":
				case "Cabeceamentos Concluídos":
				case "Cabeceamentos Concluidos":
				case "Headers Won":
					if (!text8.StartsWith("%"))
					{
						scrapedTab.Headers[j] = "% " + text8;
					}
					break;
				}
			}
			for (int k = 0; k < val2.childCount; k++)
			{
				VisualElement row = val2.ElementAt(k);
				List<string> list4 = ReadMatchRow(row, scrapedTab.Headers, list3, k);
				if (list4.Count > 0)
				{
					scrapedTab.Rows.Add(list4);
				}
			}
			string text9 = ((list2.Count > 0) ? string.Join("|", list2) : "");
			if (scrapedTab.Rows.Count > 0)
			{
				text9 += string.Join("|", scrapedTab.Rows[0]);
			}
			if (text9 == _lastTableHash && Time.unscaledTime < _timeoutTime && _currentTabIdx > 0)
			{
				_nextStepTime = Time.unscaledTime + 0.3f;
				return false;
			}
			_lastTableHash = text9;
			_accumulatedTabs.Add(scrapedTab);
			ManualLogSource log = Plugin.Log;
			BepInExInfoLogInterpolatedStringHandler val5 = new BepInExInfoLogInterpolatedStringHandler(45, 4, ref flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("[FM26Export.MatchStats] Lidas ");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<int>(scrapedTab.Rows.Count);
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral(" linhas de ");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<string>(scrapedTab.TabName);
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral(" [");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<int>(_currentTabIdx + 1);
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("/");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<int>(_tabElements.Count);
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("]");
			}
			log.LogInfo(val5);
		}
		else
		{
			ManualLogSource log2 = Plugin.Log;
			BepInExWarningLogInterpolatedStringHandler val6 = new BepInExWarningLogInterpolatedStringHandler(69, 1, ref flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("[FM26Export.MatchStats] StreamedTable visível não encontrada na aba ");
				((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(_tabElements[_currentTabIdx].name);
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral(".");
			}
			log2.LogWarning(val6);
		}
		_currentTabIdx++;
		if (_currentTabIdx < _tabElements.Count)
		{
			SafeClick(_tabElements[_currentTabIdx]);
			_nextStepTime = Time.unscaledTime + 0.8f;
			_timeoutTime = Time.unscaledTime + 5f;
			return false;
		}
		return true;
	}

	private void SafeClick(VisualElement el)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		if (el == null)
		{
			return;
		}
		try
		{
			((Focusable)el).Focus();
			PointerDownEvent pooled = PointerEventBase<PointerDownEvent>.GetPooled(new Event
			{
				type = (EventType)0
			});
			((EventBase)pooled).target = ((Il2CppObjectBase)el).Cast<IEventHandler>();
			((CallbackEventHandler)el).SendEvent((EventBase)(object)pooled);
			PointerUpEvent pooled2 = PointerEventBase<PointerUpEvent>.GetPooled(new Event
			{
				type = (EventType)1
			});
			((EventBase)pooled2).target = ((Il2CppObjectBase)el).Cast<IEventHandler>();
			((CallbackEventHandler)el).SendEvent((EventBase)(object)pooled2);
			NavigationSubmitEvent pooled3 = NavigationEventBase<NavigationSubmitEvent>.GetPooled((EventModifiers)0);
			((EventBase)pooled3).target = ((Il2CppObjectBase)el).Cast<IEventHandler>();
			((CallbackEventHandler)el).SendEvent((EventBase)(object)pooled3);
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(30, 1, ref flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export] Erro safe_click: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val);
		}
	}

	public void FinishCapture()
	{
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Expected O, but got Unknown
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Expected O, but got Unknown
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Expected O, but got Unknown
		bool flag2 = default(bool);
		try
		{
			if (_accumulatedTabs.Count == 0)
			{
				return;
			}
			string text = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset"), "Exports HTML");
			Directory.CreateDirectory(text);
			string value = (string.IsNullOrEmpty(_matchContext) ? "partida_dados" : string.Join("_", _matchContext.Split(Path.GetInvalidFileNameChars())));
			string value2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string text2 = Path.Combine(text, $"match_stats_{value}_{value2}.html");
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<html>");
			stringBuilder.AppendLine("<head>");
			stringBuilder.AppendLine("<meta charset=\"UTF-8\">");
			stringBuilder.AppendLine("<style type =\"text/css\">");
			stringBuilder.AppendLine("body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; }");
			stringBuilder.AppendLine("h2 { font-size: 16px; margin-top: 25px; font-weight: bold; }");
			stringBuilder.AppendLine("h3 { font-size: 14px; margin-top: 15px; border-bottom: 2px solid #000; padding-bottom: 4px; }");
			stringBuilder.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 30px; }");
			stringBuilder.AppendLine("th { padding: 5px; text-align: left; background-color: #EEEEEE; border: 1px solid #000000; font-weight: bold; }");
			stringBuilder.AppendLine("td { padding: 4px; border: 1px solid #000000; }");
			stringBuilder.AppendLine("</style>");
			stringBuilder.AppendLine("</head>");
			stringBuilder.AppendLine("<body>");
			string value3 = (string.IsNullOrEmpty(_matchContext) ? "Estatísticas Unificadas" : ("Resumo: " + _matchContext));
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<h2>");
			handler.AppendFormatted(value3);
			handler.AppendLiteral("</h2>");
			stringBuilder3.AppendLine(ref handler);
			int num = 0;
			foreach (ScrapedTab accumulatedTab in _accumulatedTabs)
			{
				if (accumulatedTab.Rows.Count == 0)
				{
					continue;
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
				handler.AppendLiteral("<h3>");
				handler.AppendFormatted(accumulatedTab.TabName);
				handler.AppendLiteral("</h3>");
				stringBuilder4.AppendLine(ref handler);
				stringBuilder.AppendLine("<table>");
				stringBuilder.AppendLine("<tr>");
				foreach (string header in accumulatedTab.Headers)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
					handler.AppendLiteral("\t<th>");
					handler.AppendFormatted(header);
					handler.AppendLiteral("</th>");
					stringBuilder5.AppendLine(ref handler);
				}
				stringBuilder.AppendLine("</tr>");
				foreach (List<string> row in accumulatedTab.Rows)
				{
					bool flag = true;
					foreach (string item in row)
					{
						if (!string.IsNullOrEmpty(item))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
					stringBuilder.AppendLine("<tr>");
					foreach (string item2 in row)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder6 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
						handler.AppendLiteral("\t<td>");
						handler.AppendFormatted(item2);
						handler.AppendLiteral("</td>");
						stringBuilder6.AppendLine(ref handler);
					}
					stringBuilder.AppendLine("</tr>");
					num++;
				}
				stringBuilder.AppendLine("</table>");
			}
			stringBuilder.AppendLine("</body></html>");
			File.WriteAllText(text2, stringBuilder.ToString(), Encoding.UTF8);
			ManualLogSource log = Plugin.Log;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(76, 2, ref flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export.MatchStats] Dossiê gravado: (");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_accumulatedTabs.Count);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/6) abas lidas, ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" linhas exportadas.");
			}
			log.LogInfo(val);
			ManualLogSource log2 = Plugin.Log;
			val = new BepInExInfoLogInterpolatedStringHandler(42, 1, ref flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export.MatchStats] Arquivo salvo em: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
			}
			log2.LogInfo(val);
		}
		catch (Exception ex)
		{
			ManualLogSource log3 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(42, 1, ref flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export.MatchStats] Erro Export HTML: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log3.LogError(val2);
		}
	}

	public void Cleanup()
	{
		_matchStatsRoot = null;
		if (_tabElements != null)
		{
			_tabElements.Clear();
		}
		if (_accumulatedTabs != null)
		{
			_accumulatedTabs.Clear();
		}
	}

	private List<string> ReadMatchRow(VisualElement row, List<string> actualHeaders, List<int> validIndices, int rowIdx)
	{
		List<string> list = new List<string>();
		if (row == null || row.childCount == 0)
		{
			return list;
		}
		VisualElement val = row;
		if (val.childCount == 1 && val.ElementAt(0).childCount > 1)
		{
			val = val.ElementAt(0);
		}
		for (int i = 0; i < validIndices.Count; i++)
		{
			int num = validIndices[i];
			if (num >= val.childCount)
			{
				break;
			}
			VisualElement val2 = val.ElementAt(num);
			string text = ((i < actualHeaders.Count) ? actualHeaders[i].ToLower() : "");
			string text2 = "";
			switch (text)
			{
			default:
				if (!text.StartsWith("min") && !text.StartsWith("tem") && !text.StartsWith("tie"))
				{
					text2 = UIUtils.CollectAllTextsJoined(val2) ?? string.Empty;
					if (text2.StartsWith("- ") && text2.Length > 2)
					{
						text2 = text2.Substring(2).Trim();
					}
					if (text2.EndsWith(" -") && text2.Length > 2)
					{
						text2 = text2.Substring(0, text2.Length - 2).Trim();
					}
					if (string.IsNullOrEmpty(text2) || text2 == "-")
					{
						string text3 = UIUtils.TryReadStars(val2);
						if (text3 != null)
						{
							text2 = text3;
						}
					}
					break;
				}
				goto case "min";
			case "min":
			case "min.":
			case "time":
			{
				string text4 = UIUtils.LerIconesComoTexto(val2);
				string text5 = UIUtils.CollectAllTextsJoined(val2);
				if (text4.Contains(UIUtils.GetTrans("Sub In")) || text4.Contains(UIUtils.GetTrans("Sub Out")) || text4.Contains("Entra") || text4.Contains("Sai") || text4.Contains("Sub"))
				{
					string newValue = ((rowIdx < 11) ? UIUtils.GetTrans("Sub Out") : UIUtils.GetTrans("Sub In"));
					if (text4.Contains(UIUtils.GetTrans("Sub In")))
					{
						text4 = text4.Replace(UIUtils.GetTrans("Sub In"), newValue);
					}
					else if (text4.Contains(UIUtils.GetTrans("Sub Out")))
					{
						text4 = text4.Replace(UIUtils.GetTrans("Sub Out"), newValue);
					}
					else if (text4.Contains("Entra"))
					{
						text4 = text4.Replace("Entra", newValue);
					}
					else if (text4.Contains("Sai"))
					{
						text4 = text4.Replace("Sai", newValue);
					}
					else if (text4.Contains("Sub"))
					{
						text4 = text4.Replace("Sub", newValue);
					}
				}
				string text6 = "";
				if (string.IsNullOrEmpty(text5) || string.IsNullOrEmpty(text4))
				{
					text6 = (string.IsNullOrEmpty(text5) ? text4 : text5);
				}
				else
				{
					bool flag = true;
					string[] array = text5.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string value in array)
					{
						if (!text4.Contains(value))
						{
							flag = false;
							break;
						}
					}
					text6 = ((!flag) ? (text5 + " (" + text4 + ")") : text4);
				}
				text6 = text6.Replace("Coração", "").Replace("Fadigado", "").Replace("  ", " ")
					.Trim();
				text2 = text6;
				break;
			}
			}
			if (!string.IsNullOrEmpty(text2) && text2 != "-" && (text.Contains("desarmes") || text.Contains("cabece") || text.Contains("passe")) && double.TryParse(text2.Replace(",", ".").Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var _) && !text2.Contains("%"))
			{
				text2 += "%";
			}
			list.Add(text2);
			if (list.Count >= actualHeaders.Count)
			{
				break;
			}
		}
		int num2 = actualHeaders.FindIndex(delegate(string h)
		{
			string text7 = h.ToLower();
			switch (text7)
			{
			default:
				if (!text7.StartsWith("min") && !text7.StartsWith("tem"))
				{
					return text7.StartsWith("tie");
				}
				break;
			case "min":
			case "min.":
			case "time":
				break;
			}
			return true;
		});
		if (num2 >= 0 && num2 < list.Count)
		{
			bool flag2 = false;
			int num3 = actualHeaders.FindIndex((string h) => h.ToLower().StartsWith("dist"));
			if (num3 >= 0 && num3 < list.Count && !string.IsNullOrEmpty(list[num3]))
			{
				if (list[num3] != "0,0 km" && list[num3] != "0 km" && list[num3] != "-")
				{
					flag2 = true;
				}
			}
			else
			{
				int num4 = actualHeaders.FindIndex((string h) => h.ToLower().Contains("pass"));
				if (num4 >= 0 && num4 < list.Count && !string.IsNullOrEmpty(list[num4]) && list[num4] != "0" && list[num4] != "-")
				{
					flag2 = true;
				}
			}
			if (flag2 && string.IsNullOrEmpty(list[num2].Replace("-", "").Trim()))
			{
				list[num2] = "90";
			}
			else if (list[num2] == "-")
			{
				list[num2] = "";
			}
		}
		return list;
	}

	private void FindAllByName(VisualElement root, string name, List<VisualElement> results)
	{
		if (root != null)
		{
			if (root.name == name)
			{
				results.Add(root);
			}
			for (int i = 0; i < root.childCount; i++)
			{
				FindAllByName(root.ElementAt(i), name, results);
			}
		}
	}

	private string ObterContextoDaPartida()
	{
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Expected O, but got Unknown
		if (_matchStatsRoot == null)
		{
			return "Partida";
		}
		if (!string.IsNullOrEmpty(_contextHome) && !string.IsNullOrEmpty(_contextAway))
		{
			return _contextHome + " vs " + _contextAway;
		}
		string text = "Casa";
		string text2 = "Fora";
		string text3 = "";
		try
		{
			VisualElement matchStatsRoot = _matchStatsRoot;
			object el;
			if (matchStatsRoot == null)
			{
				el = null;
			}
			else
			{
				IPanel panel = matchStatsRoot.panel;
				el = ((panel != null) ? panel.visualTree : null);
			}
			VisualElement val = UIUtils.FindByName((VisualElement)el, "screen_title");
			if (val != null)
			{
				string text4 = UIUtils.CollectAllTextsJoined(val);
				if (!string.IsNullOrEmpty(text4))
				{
					string[] array = new string[3] { " vs ", " x ", " - " };
					foreach (string text5 in array)
					{
						if (!text4.Contains(text5))
						{
							continue;
						}
						string[] array2 = text4.Split(new string[1] { text5 }, StringSplitOptions.None);
						if (array2.Length == 2)
						{
							text = array2[0].Trim();
							text2 = array2[1].Trim();
							if (text.Contains(":"))
							{
								text = text.Substring(text.IndexOf(":") + 1).Trim();
							}
							break;
						}
					}
				}
			}
			if (text == "Casa" || text == "Estatísticas da Partida")
			{
				VisualElement val2 = UIUtils.FindByName(_matchStatsRoot, "HomeTeamBadge");
				if (val2 != null)
				{
					string text6 = UIUtils.CollectFirstTooltip(val2);
					if (string.IsNullOrEmpty(text6))
					{
						text6 = UIUtils.CollectAllTextsJoined(val2);
					}
					if (!string.IsNullOrEmpty(text6))
					{
						text = text6.Trim();
					}
				}
			}
			if (text2 == "Fora" || string.IsNullOrEmpty(text2))
			{
				VisualElement val3 = UIUtils.FindByName(_matchStatsRoot, "AwayTeamBadge");
				if (val3 != null)
				{
					string text7 = UIUtils.CollectFirstTooltip(val3);
					if (string.IsNullOrEmpty(text7))
					{
						text7 = UIUtils.CollectAllTextsJoined(val3);
					}
					if (!string.IsNullOrEmpty(text7))
					{
						text2 = text7.Trim();
					}
				}
			}
			VisualElement val4 = UIUtils.FindByName(_matchStatsRoot, "Teams frame");
			if (val4 != null)
			{
				List<string> list = new List<string>();
				UIUtils.CollectAllTexts(val4, list);
				string text8 = string.Join(" ", list).Replace("  ", " ").Trim();
				ManualLogSource log = Plugin.Log;
				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val5 = new BepInExInfoLogInterpolatedStringHandler(37, 1, ref flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("[FM26Export] Teams frame raw text: '");
					((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<string>(text8);
					((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("'");
				}
				log.LogInfo(val5);
				Match match = Regex.Match(text8, "(\\d+)\\s*[-xX]\\s*(\\d+)");
				if (match.Success)
				{
					text3 = " " + match.Groups[1].Value + "x" + match.Groups[2].Value;
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("Erro parser contexto: " + ex.Message));
		}
		_contextHome = text;
		_contextAway = text2;
		return text + text3 + " vs " + text2;
	}
}
