using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public static class UIUtils
{
	public static string GameLang = "pt";

	public static string GetTrans(string key)
	{
		if (GameLang == "en")
		{
			switch (key)
			{
			case "Amarelo":
				return "Yellow";
			case "Vermelho":
				return "Red";
			case "Sub In":
				return "Sub In";
			case "Sub Out":
				return "Sub Out";
			case "Lesão":
				return "Injured";
			}
		}
		else if (GameLang == "es")
		{
			switch (key)
			{
			case "Amarelo":
				return "Amarilla";
			case "Vermelho":
				return "Roja";
			case "Sub In":
				return "Entra";
			case "Sub Out":
				return "Sale";
			case "Lesão":
				return "Lesión";
			}
		}
		else
		{
			if (key == "Sub In")
			{
				return "Entra";
			}
			if (key == "Sub Out")
			{
				return "Sai";
			}
		}
		return key;
	}

	public static string GetText(VisualElement el)
	{
		if (el == null)
		{
			return null;
		}
		try
		{
			TextElement val = ((Il2CppObjectBase)el).TryCast<TextElement>();
			if (val != null && !string.IsNullOrWhiteSpace(val.text))
			{
				return StripHtml(val.text.Trim());
			}
		}
		catch
		{
		}
		try
		{
			Label val2 = ((Il2CppObjectBase)el).TryCast<Label>();
			if (val2 != null && !string.IsNullOrWhiteSpace(((TextElement)val2).text))
			{
				return StripHtml(((TextElement)val2).text.Trim());
			}
		}
		catch
		{
		}
		try
		{
			string tooltip = el.tooltip;
			if (!string.IsNullOrWhiteSpace(tooltip))
			{
				return StripHtml(tooltip.Trim());
			}
		}
		catch
		{
		}
		return null;
	}

	public static string StripHtml(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return Regex.Replace(s, "<[^>]+>", string.Empty).Trim();
		}
		return s;
	}

	public static string CollectFirstText(VisualElement el, int d = 0)
	{
		if (el == null || d > 20)
		{
			return null;
		}
		string text = GetText(el);
		if (text != null)
		{
			return text;
		}
		for (int i = 0; i < el.childCount; i++)
		{
			string text2 = CollectFirstText(el.ElementAt(i), d + 1);
			if (text2 != null)
			{
				return text2;
			}
		}
		return null;
	}

	public static string CollectFirstTooltip(VisualElement el, int d = 0)
	{
		if (el == null || d > 20)
		{
			return null;
		}
		try
		{
			string tooltip = el.tooltip;
			if (!string.IsNullOrWhiteSpace(tooltip))
			{
				return StripHtml(tooltip.Trim());
			}
		}
		catch
		{
		}
		for (int i = 0; i < el.childCount; i++)
		{
			string text = CollectFirstTooltip(el.ElementAt(i), d + 1);
			if (text != null)
			{
				return text;
			}
		}
		return null;
	}

	public static string CollectAllTextsJoined(VisualElement el, int d = 0)
	{
		if (el == null || d > 20)
		{
			return "";
		}
		List<string> list = new List<string>();
		CollectAllTexts(el, list);
		return string.Join(" ", list).Trim();
	}

	public static void CollectAllTexts(VisualElement el, List<string> out_, int d = 0)
	{
		if (el == null || d > 20)
		{
			return;
		}
		string text = GetText(el);
		if (text != null && !out_.Contains(text))
		{
			out_.Add(text);
		}
		try
		{
			string tooltip = el.tooltip;
			if (!string.IsNullOrWhiteSpace(tooltip))
			{
				string item = StripHtml(tooltip.Trim());
				if (!out_.Contains(item))
				{
					out_.Add(item);
				}
			}
		}
		catch
		{
		}
		for (int i = 0; i < el.childCount; i++)
		{
			CollectAllTexts(el.ElementAt(i), out_, d + 1);
		}
	}

	public static string TryReadStars(VisualElement cell)
	{
		try
		{
			string tooltip = cell.tooltip;
			if (!string.IsNullOrEmpty(tooltip) && double.TryParse(tooltip, out var _))
			{
				return tooltip;
			}
		}
		catch
		{
		}
		int filled = 0;
		int half = 0;
		int total = 0;
		CountStars(cell, ref filled, ref half, ref total, 0);
		if (total == 0)
		{
			return null;
		}
		float num = (float)filled + (float)half * 0.5f;
		if (num <= 0f)
		{
			return string.Empty;
		}
		return num.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", ",");
	}

	private static void CountStars(VisualElement el, ref int filled, ref int half, ref int total, int d)
	{
		if (el == null || d > 12)
		{
			return;
		}
		try
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			for (int i = 0; i < el.classList.Count; i++)
			{
				string text = el.classList[i].ToLower();
				if (text.Contains("star") || text.Contains("ability") || text.Contains("rating"))
				{
					flag = true;
				}
				if (text.Contains("filled") || text.Contains("active") || text.Contains("full") || text.Contains("on"))
				{
					flag2 = true;
				}
				if (text.Contains("half"))
				{
					flag3 = true;
				}
			}
			if (flag && el.childCount == 0)
			{
				total++;
				if (flag3)
				{
					half++;
				}
				else if (flag2)
				{
					filled++;
				}
			}
		}
		catch
		{
		}
		for (int j = 0; j < el.childCount; j++)
		{
			CountStars(el.ElementAt(j), ref filled, ref half, ref total, d + 1);
		}
	}

	public static string RowKey(List<string> vals)
	{
		if (vals == null || vals.Count == 0)
		{
			return string.Empty;
		}
		return string.Join("|", vals);
	}

	public static VisualElement FindByName(VisualElement el, string name)
	{
		if (el == null)
		{
			return null;
		}
		if (el.name == name)
		{
			return el;
		}
		for (int i = 0; i < el.childCount; i++)
		{
			VisualElement val = FindByName(el.ElementAt(i), name);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	public static string Esc(string v)
	{
		if (string.IsNullOrEmpty(v))
		{
			return string.Empty;
		}
		v = v.Replace("\r", " ").Replace("\n", " ");
		string text = new string(new char[1] { '"' });
		if (v.Contains(";") || v.Contains(text))
		{
			v = text + v.Replace(text, text + text) + text;
		}
		return v;
	}

	public static string LerIconesComoTexto(VisualElement el, int d = 0)
	{
		if (el == null || d > 6)
		{
			return "";
		}
		List<string> list = new List<string>();
		try
		{
			for (int i = 0; i < el.classList.Count; i++)
			{
				string text = el.classList[i].ToLower();
				if (text.Contains("yellow"))
				{
					list.Add(GetTrans("Amarelo"));
				}
				if (text.Contains("red"))
				{
					list.Add(GetTrans("Vermelho"));
				}
				if (text.Contains("sub") && !text.Contains("subject"))
				{
					if (text.Contains("on") || text.Contains("in"))
					{
						list.Add(GetTrans("Sub In"));
					}
					else if (text.Contains("off") || text.Contains("out"))
					{
						list.Add(GetTrans("Sub Out"));
					}
					else
					{
						list.Add("Sub");
					}
				}
				if (text.Contains("injur"))
				{
					list.Add(GetTrans("Lesão"));
				}
				if (text.Contains("condition") || text.Contains("heart") || text.Contains("sharpness"))
				{
					list.Add("Coração");
				}
				if (text.Contains("fatigue") || text.Contains("tired"))
				{
					list.Add("Fadigado");
				}
			}
			string tooltip = el.tooltip;
			if (!string.IsNullOrWhiteSpace(tooltip) && list.Count > 0)
			{
				string text2 = StripHtml(tooltip);
				if (text2.Length < 40)
				{
					string text3 = list[list.Count - 1];
					if (text3 != GetTrans("Sub In") && text3 != GetTrans("Sub Out"))
					{
						list[list.Count - 1] = text2;
					}
				}
			}
		}
		catch
		{
		}
		for (int j = 0; j < el.childCount; j++)
		{
			string text4 = LerIconesComoTexto(el.ElementAt(j), d + 1);
			if (string.IsNullOrEmpty(text4))
			{
				continue;
			}
			string[] array = text4.Split(new string[1] { " | " }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string item in array)
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		return string.Join(" | ", list);
	}

	public static string DiagCell(VisualElement el, int d = 0)
	{
		if (el == null || d > 6)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			for (int i = 0; i < el.classList.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(el.classList[i]);
			}
		}
		catch
		{
		}
		string value = GetText(el) ?? string.Empty;
		StringBuilder stringBuilder2 = new StringBuilder();
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 5, stringBuilder3);
		handler.AppendFormatted(new string('-', d));
		handler.AppendFormatted(((object)el).GetType().Name);
		handler.AppendLiteral("[cls=");
		handler.AppendFormatted(stringBuilder);
		handler.AppendLiteral(",ch=");
		handler.AppendFormatted(el.childCount);
		handler.AppendLiteral(",txt=");
		handler.AppendFormatted(value);
		handler.AppendLiteral("] ");
		stringBuilder3.Append(ref handler);
		for (int j = 0; j < el.childCount; j++)
		{
			stringBuilder2.Append(DiagCell(el.ElementAt(j), d + 1));
		}
		return stringBuilder2.ToString();
	}
}
