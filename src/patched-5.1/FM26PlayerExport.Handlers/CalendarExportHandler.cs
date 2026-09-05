using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public class CalendarExportHandler : IExportHandler
{
	private VisualElement _calendarRoot;

	private List<string> _headers = new List<string>();

	private List<List<string>> _rows = new List<List<string>>();

	public bool TryStartCapture(VisualElement root, out string errorMessage)
	{
		errorMessage = string.Empty;
		_calendarRoot = UIUtils.FindByName(root, "fixtures_schedule");
		if (_calendarRoot == null)
		{
			_calendarRoot = UIUtils.FindByName(root, "Calendar");
		}
		if (_calendarRoot == null)
		{
			_calendarRoot = UIUtils.FindByName(root, "team_fixtures");
		}
		if (_calendarRoot == null)
		{
			errorMessage = "[FM26Export.Calendar] Calendar screen (fixtures_schedule, team_fixtures, or Calendar) was not found.";
			return false;
		}
		List<VisualElement> list = new List<VisualElement>();
		FindAllByName(_calendarRoot, "StreamedTable", list);
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
		if (val == null || val2 == null)
		{
			errorMessage = "[FM26Export.Calendar] No table was found on the Calendar screen.";
			return false;
		}
		VisualElement val4 = UIUtils.FindByName(val, "column-headers");
		List<int> list2 = new List<int>();
		_headers.Clear();
		if (val4 != null)
		{
			for (int i = 0; i < val4.childCount; i++)
			{
				string text = UIUtils.CollectFirstText(val4.ElementAt(i));
				if (string.IsNullOrEmpty(text))
				{
					text = UIUtils.CollectFirstTooltip(val4.ElementAt(i));
				}
				if (string.IsNullOrEmpty(text))
				{
					text = "Col" + i;
				}
				_headers.Add(UIUtils.Esc(text.Trim()));
				list2.Add(i);
			}
		}
		else
		{
			_headers.Add("Dados");
			list2.Add(0);
		}
		_rows.Clear();
		for (int j = 0; j < val2.childCount; j++)
		{
			VisualElement obj = val2.ElementAt(j);
			List<string> list3 = new List<string>();
			VisualElement val5 = obj;
			if (val5.childCount == 1 && val5.ElementAt(0).childCount > 1)
			{
				val5 = val5.ElementAt(0);
			}
			for (int k = 0; k < list2.Count; k++)
			{
				int num = list2[k];
				if (num >= val5.childCount)
				{
					break;
				}
				VisualElement el = val5.ElementAt(num);
				string text2 = UIUtils.CollectAllTextsJoined(el) ?? "";
				if (string.IsNullOrEmpty(text2))
				{
					string text3 = UIUtils.CollectFirstTooltip(el);
					if (!string.IsNullOrEmpty(text3))
					{
						text2 = text3;
					}
				}
				list3.Add(text2.Trim());
			}
			bool flag = true;
			foreach (string item2 in list3)
			{
				if (!string.IsNullOrWhiteSpace(item2.Replace("-", "")))
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				_rows.Add(list3);
			}
		}
		return true;
	}

	public bool CaptureStep()
	{
		return true;
	}

	public void FinishCapture()
	{
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Expected O, but got Unknown
		if (_rows.Count == 0)
		{
			return;
		}
		string text = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset"), "Exports HTML");
		Directory.CreateDirectory(text);
		string text2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string text3 = Path.Combine(text, "calendario_" + text2 + ".html");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<html><head><meta charset=\"UTF-8\">");
		stringBuilder.AppendLine("<style type =\"text/css\">body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; } h2 { font-size: 16px; margin-top: 25px; font-weight: bold; } table { border-collapse: collapse; width: 100%; border: 1px solid #000; } th { padding: 5px; background-color: #EEE; border: 1px solid #000; text-align: left; } td { padding: 4px; border: 1px solid #000; } </style>");
		stringBuilder.AppendLine("</head><body><h2>Summary: Calendar</h2><br><table><tr>");
		foreach (string header in _headers)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<th>");
			handler.AppendFormatted(header);
			handler.AppendLiteral("</th>");
			stringBuilder3.AppendLine(ref handler);
		}
		stringBuilder.AppendLine("</tr>");
		foreach (List<string> row in _rows)
		{
			stringBuilder.AppendLine("<tr>");
			foreach (string item in row)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
				handler.AppendLiteral("<td>");
				handler.AppendFormatted(item);
				handler.AppendLiteral("</td>");
				stringBuilder4.AppendLine(ref handler);
			}
			stringBuilder.AppendLine("</tr>");
		}
		stringBuilder.AppendLine("</table></body></html>");
		File.WriteAllText(text3, stringBuilder.ToString(), Encoding.UTF8);
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(58, 2, ref flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export.Calendar] Exported ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_rows.Count);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" calendar rows to ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text3);
		}
		log.LogInfo(val);
	}

	public void Cleanup()
	{
		_calendarRoot = null;
		if (_headers != null)
		{
			_headers.Clear();
		}
		if (_rows != null)
		{
			_rows.Clear();
		}
	}

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
}
