using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public abstract class GenericScrolledTableHandler : IExportHandler
{
	private const int WAIT_FRAMES = 4;

	private const int ZERO_STEPS_MAX = 3;

	protected VisualElement _captureView;

	protected List<string> _captureHeaders;

	private HashSet<string> _seenKeys;

	private float _lastScrollY;

	private int _scrollAttempts;

	private int _maxScrollAttempts;

	private int _maxRows;

	private int _capturedRowCount;

	private int _zeroSteps;

	private bool _diagLogged;

	private int _captureWait;

	private bool _isComplete;

	private bool _outputStarted;

	private bool _outputFinalized;

	private StreamWriter _csvWriter;

	private StreamWriter _htmlWriter;

	private string _csvFile;

	private string _htmlFile;

	protected string FilePrefix { get; set; } = "export_";

	private sealed class TableCandidate
	{
		public VisualElement Headers;
		public VisualElement View;
		public bool Visible;
	}

	public virtual bool TryStartCapture(VisualElement root, out string errorMessage)
	{
		errorMessage = string.Empty;
		List<TableCandidate> candidates = FindTableCandidates(root);

		if (candidates.Count == 0)
		{
			errorMessage = "[FM26Export.GenericTable] No list table candidates were found in the UI.";
			return false;
		}

		// Prefer visible candidates, but keep hidden candidates as a fallback for compatibility.
		candidates.Sort((a, b) => b.Visible.CompareTo(a.Visible));

		List<string> rejected = new List<string>();

		foreach (TableCandidate candidate in candidates)
		{
			_captureView = candidate.View;
			_captureHeaders = ReadHeaders(candidate.Headers);

			if (!IsValidScreen(root, _captureHeaders))
			{
				rejected.Add(string.Join(", ", _captureHeaders));
				continue;
			}

			Plugin.Log.LogInfo($"[FM26Export] Accepted table candidate. visible={candidate.Visible} | Headers ({_captureHeaders.Count}): {string.Join(" | ", _captureHeaders)}");
			InitializeCaptureState();
			return true;
		}

		_captureView = null;
		_captureHeaders = null;
		errorMessage = "[FM26Export] Candidate tables were found, but none matched this handler.";
		if (rejected.Count > 0)
			errorMessage += " Rejected headers: " + string.Join(" || ", rejected);

		return false;
	}

	private void InitializeCaptureState()
	{
		_seenKeys = new HashSet<string>();
		_scrollAttempts = 0;
		_maxRows = PluginConfig.EffectiveMaxRowsToExport;
		_maxScrollAttempts = PluginConfig.GetMaxScrollAttemptsForRows(_maxRows);
		_capturedRowCount = 0;
		_zeroSteps = 0;
		_lastScrollY = -1f;
		_diagLogged = false;
		_isComplete = false;
		_outputStarted = false;
		_outputFinalized = false;
		_csvWriter = null;
		_htmlWriter = null;
		_csvFile = null;
		_htmlFile = null;

		ScrollView firstAncestorOfType = _captureView.GetFirstAncestorOfType<ScrollView>();
		if (firstAncestorOfType != null)
			firstAncestorOfType.scrollOffset = Vector2.zero;

		_captureWait = WAIT_FRAMES;
		Plugin.Log.LogInfo($"[FM26Export] List capture ({FilePrefix}) started. Row limit={_maxRows} rows | scroll safety={_maxScrollAttempts}.");
	}

	private List<TableCandidate> FindTableCandidates(VisualElement root)
	{
		List<TableCandidate> results = new List<TableCandidate>();
		HashSet<VisualElement> seenViews = new HashSet<VisualElement>();

		AddNamedContainerCandidate(root, "playertable", results, seenViews);
		AddNamedContainerCandidate(root, "client-object-viewer-table", results, seenViews);
		AddNamedContainerCandidate(root, "nonplayertable", results, seenViews);

		List<VisualElement> headers = new List<VisualElement>();
		FindAllByName(root, "column-headers", headers);

		foreach (VisualElement header in headers)
		{
			VisualElement view = FindNearbyView(header);
			AddCandidate(header, view, results, seenViews);
		}

		return results;
	}

	private void AddNamedContainerCandidate(VisualElement root, string name, List<TableCandidate> results, HashSet<VisualElement> seenViews)
	{
		VisualElement container = UIUtils.FindByName(root, name);
		if (container == null)
			return;

		VisualElement headers = UIUtils.FindByName(container, "column-headers");
		VisualElement view = UIUtils.FindByName(container, "View");
		AddCandidate(headers, view, results, seenViews);
	}

	private void AddCandidate(VisualElement headers, VisualElement view, List<TableCandidate> results, HashSet<VisualElement> seenViews)
	{
		if (headers == null || view == null || view.childCount == 0 || seenViews.Contains(view))
			return;

		seenViews.Add(view);
		results.Add(new TableCandidate
		{
			Headers = headers,
			View = view,
			Visible = IsVisibleForCapture(view)
		});
	}

	private VisualElement FindNearbyView(VisualElement headers)
	{
		VisualElement scope = headers?.parent;

		for (int depth = 0; scope != null && depth < 4; depth++, scope = scope.parent)
		{
			VisualElement view = UIUtils.FindByName(scope, "View");
			if (view != null)
				return view;
		}

		return null;
	}

	private List<string> ReadHeaders(VisualElement headersElement)
	{
		List<string> headers = new List<string>();

		for (int i = 1; i < headersElement.childCount; i++)
		{
			VisualElement el = headersElement.ElementAt(i);
			List<string> texts = new List<string>();
			UIUtils.CollectAllTexts(el, texts);

			string text = string.Empty;
			foreach (string item in texts)
			{
				string lower = item.ToLowerInvariant();
				if (!lower.Contains("sort") && !lower.Contains("orden") && !lower.Contains("order") && item.Length > text.Length)
					text = item;
			}

			if (string.IsNullOrWhiteSpace(text))
			{
				string tooltip = UIUtils.CollectFirstTooltip(el);
				string firstText = UIUtils.CollectFirstText(el);
				text = !string.IsNullOrWhiteSpace(tooltip) ? tooltip : firstText;
			}

			headers.Add(text != null ? UIUtils.Esc(text) : $"Col{i}");
		}

		if (headers.Count == 0)
			headers.Add("Data");

		return headers;
	}

	private bool IsVisibleForCapture(VisualElement element)
	{
		if (element == null)
			return false;

		try
		{
			for (VisualElement current = element; current != null; current = current.parent)
			{
				if (!current.visible || current.resolvedStyle.display == DisplayStyle.None)
					return false;
			}

			Rect bounds = element.worldBound;
			return bounds.width > 0f && bounds.height > 0f;
		}
		catch
		{
			return true;
		}
	}

	protected virtual bool ShouldCaptureRow(VisualElement row)
	{
		try
		{
			return row != null && row.ClassListContains("virtualised-list__item--selected");
		}
		catch
		{
			return false;
		}
	}

	protected abstract bool IsValidScreen(VisualElement root, List<string> headers);

	public bool CaptureStep()
	{
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Expected O, but got Unknown
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Expected O, but got Unknown
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		if (_isComplete)
		{
			return true;
		}
		if (_captureWait > 0)
		{
			_captureWait--;
			return false;
		}
		bool flag3 = default(bool);
		try
		{
			int num = 0;
			for (int i = 0; i < _captureView.childCount; i++)
			{
				VisualElement val = _captureView.ElementAt(i);
				if (!ShouldCaptureRow(val))
				{
					continue;
				}
				bool flag2 = !_diagLogged && _scrollAttempts == 0 && num == 0;
				List<string> list = ReadRow(val, flag2, _captureHeaders);
				if (flag2)
				{
					_diagLogged = true;
				}
				if (list.Count == 0)
				{
					continue;
				}
				string text = UIUtils.RowKey(list);
				if (string.IsNullOrEmpty(text) || _seenKeys.Contains(text))
				{
					continue;
				}
				_seenKeys.Add(text);
				if (!WriteCapturedRow(list))
				{
					_isComplete = true;
					return true;
				}
				num++;
				if (_capturedRowCount >= _maxRows)
				{
					ManualLogSource log = Plugin.Log;
					BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(52, 1, out flag3);
					if (flag3)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export] Configured row limit of ");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_maxRows);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" rows reached.");
					}
					log.LogWarning(val2);
					_isComplete = true;
					return true;
				}
			}
			ScrollView firstAncestorOfType = _captureView.GetFirstAncestorOfType<ScrollView>();
			float num2 = ((firstAncestorOfType != null) ? firstAncestorOfType.scrollOffset.y : 0f);
			_scrollAttempts++;
			bool flag4 = Math.Abs(num2 - _lastScrollY) < 0.5f && _lastScrollY >= 0f;
			if (num == 0)
			{
				_zeroSteps++;
			}
			else
			{
				_zeroSteps = 0;
			}
			bool flag5 = _zeroSteps >= 3;
			ManualLogSource log2 = Plugin.Log;
			BepInExInfoLogInterpolatedStringHandler val3 = new BepInExInfoLogInterpolatedStringHandler(58, 7, out flag3);
			if (flag3)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[FM26Export] Step ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_scrollAttempts);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(": +");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(num);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" | total=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_capturedRowCount);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" | scrollY=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<float>(num2, "F0");
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" | atEnd=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(flag4);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" | stall=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_zeroSteps);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("/");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(3);
			}
			log2.LogInfo(val3);
			if ((flag4 || _scrollAttempts >= _maxScrollAttempts) | flag5)
			{
				if (flag5 && !flag4)
				{
					Plugin.Log.LogWarning((object)"[FM26Export] Stopped because no new rows were found.");
				}
				if (_scrollAttempts >= _maxScrollAttempts)
				{
					ManualLogSource log3 = Plugin.Log;
					BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(58, 1, out flag3);
					if (flag3)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export] Stopped at the scroll safety limit (");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_maxScrollAttempts);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(").");
					}
					log3.LogWarning(val2);
				}
				_isComplete = true;
				return true;
			}
			_lastScrollY = num2;
			float num3;
			if (firstAncestorOfType != null)
			{
				Rect layout = ((VisualElement)firstAncestorOfType).layout;
				if (layout.height > 0f)
				{
					num3 = layout.height;
					goto IL_0369;
				}
			}
			num3 = 600f;
			goto IL_0369;
			IL_0369:
			float num4 = num3;
			if (firstAncestorOfType != null)
			{
				firstAncestorOfType.scrollOffset = new Vector2(0f, num2 + num4);
			}
			_captureWait = 4;
			return false;
		}
		catch (Exception ex)
		{
			ManualLogSource log4 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val4 = new BepInExErrorLogInterpolatedStringHandler(31, 1, out flag3);
			if (flag3)
			{
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[FM26Export] CaptureStep error: ");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(ex.Message);
			}
			log4.LogError(val4);
			_isComplete = true;
			return true;
		}
	}

	public void FinishCapture()
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			if (_capturedRowCount == 0)
			{
				CloseOutput(deleteFiles: true);
				ManualLogSource log = Plugin.Log;
				BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(31, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export] No data captured for ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FilePrefix);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
				}
				log.LogWarning(val);
				return;
			}
			CloseOutput(deleteFiles: false);
			ManualLogSource log2 = Plugin.Log;
			BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(28, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export] Export complete: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_capturedRowCount);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" rows.");
			}
			log2.LogInfo(val2);
			ManualLogSource log3 = Plugin.Log;
			val2 = new BepInExInfoLogInterpolatedStringHandler(27, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export] CSV saved to: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(_csvFile);
			}
			log3.LogInfo(val2);
			ManualLogSource log4 = Plugin.Log;
			val2 = new BepInExInfoLogInterpolatedStringHandler(28, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export] HTML saved to: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(_htmlFile);
			}
			log4.LogInfo(val2);
		}
		catch (Exception ex)
		{
			ManualLogSource log5 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val3 = new BepInExErrorLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[FM26Export] FinishCapture error: ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
			}
			log5.LogError(val3);
		}
	}

	public virtual void Cleanup()
	{
		CloseOutput(_outputStarted && !_outputFinalized);
		_captureView = null;
		if (_captureHeaders != null)
		{
			_captureHeaders.Clear();
		}
		if (_seenKeys != null)
		{
			_seenKeys.Clear();
		}
	}

	private bool WriteCapturedRow(List<string> row)
	{
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		try
		{
			if (row == null || row.Count == 0)
			{
				return true;
			}
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
				return true;
			}
			EnsureOutputStarted();
			_csvWriter.WriteLine(string.Join(";", row.ConvertAll<string>(UIUtils.Esc)));
			_htmlWriter.WriteLine("<tr>");
			foreach (string item2 in row)
			{
				_htmlWriter.WriteLine("\t<td>" + HtmlEsc(item2) + "</td>");
			}
			_htmlWriter.WriteLine("</tr>");
			_capturedRowCount++;
			return true;
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag2 = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(45, 1, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export] Error writing exported row: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val);
			CloseOutput(deleteFiles: true);
			return false;
		}
	}

	private void EnsureOutputStarted()
	{
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Expected O, but got Unknown
		if (_outputStarted)
		{
			return;
		}
		string text = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset");
		string text2 = Path.Combine(path, "Exports CSV");
		string text3 = Path.Combine(path, "Exports HTML");
		Directory.CreateDirectory(text2);
		Directory.CreateDirectory(text3);
		_csvFile = Path.Combine(text2, FilePrefix + text + ".csv");
		_htmlFile = Path.Combine(text3, FilePrefix + text + ".html");
		_csvWriter = new StreamWriter(_csvFile, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 65536);
		_htmlWriter = new StreamWriter(_htmlFile, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 65536);
		_csvWriter.WriteLine(string.Join(";", _captureHeaders));
		_htmlWriter.WriteLine("<html>");
		_htmlWriter.WriteLine("<head>");
		_htmlWriter.WriteLine("<meta charset=\"UTF-8\">");
		_htmlWriter.WriteLine("<style type =\"text/css\">");
		_htmlWriter.WriteLine("body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; }");
		_htmlWriter.WriteLine("th { padding: 5px; text-align: left; background-color: #EEEEEE; border: 1px solid #000000; font-weight: bold; }");
		_htmlWriter.WriteLine("td { padding: 4px; border: 1px solid #000000; }");
		_htmlWriter.WriteLine("table { border-collapse: collapse; width: 98%; margin: 20px auto; }");
		_htmlWriter.WriteLine("tr:nth-child(even) { background-color: #F9F9F9; }");
		_htmlWriter.WriteLine("</style>");
		_htmlWriter.WriteLine("</head>");
		_htmlWriter.WriteLine("<body>");
		_htmlWriter.WriteLine("<table border=\"1\">");
		_htmlWriter.WriteLine("<tr>");
		foreach (string captureHeader in _captureHeaders)
		{
			_htmlWriter.WriteLine("\t<th>" + HtmlEsc(captureHeader) + "</th>");
		}
		_htmlWriter.WriteLine("</tr>");
		_outputStarted = true;
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(55, 2, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export] Incremental output started: CSV=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(_csvFile);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" | HTML=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(_htmlFile);
		}
		log.LogInfo(val);
	}

	private void CloseOutput(bool deleteFiles)
	{
		try
		{
			if (_outputStarted && !_outputFinalized && _htmlWriter != null)
			{
				_htmlWriter.WriteLine("</table></body></html>");
			}
		}
		catch
		{
		}
		try
		{
			_csvWriter?.Dispose();
		}
		catch
		{
		}
		try
		{
			_htmlWriter?.Dispose();
		}
		catch
		{
		}
		_csvWriter = null;
		_htmlWriter = null;
		if (deleteFiles)
		{
			TryDelete(_csvFile);
			TryDelete(_htmlFile);
		}
		if (_outputStarted && !deleteFiles)
		{
			_outputFinalized = true;
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static string HtmlEsc(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&#39;");
	}

	private List<string> ReadRow(VisualElement row, bool diag, List<string> headers)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		List<string> list = new List<string>();
		if (row == null || row.childCount == 0)
		{
			return list;
		}
		VisualElement val = row.ElementAt(0);
		if (val.childCount == 1 && val.ElementAt(0).childCount > 1)
		{
			val = val.ElementAt(0);
		}
		bool flag = default(bool);
		for (int i = 1; i < val.childCount; i++)
		{
			VisualElement val2 = val.ElementAt(i);
			string text;
			if (i == 1)
			{
				List<string> list2 = new List<string>();
				UIUtils.CollectAllTexts(val2, list2);
				if (diag)
				{
					ManualLogSource log = Plugin.Log;
					BepInExInfoLogInterpolatedStringHandler val3 = new BepInExInfoLogInterpolatedStringHandler(29, 1, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[FM26Export] Cell[1] DIAG: ");
						((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(UIUtils.DiagCell(val2));
					}
					log.LogInfo(val3);
				}
				text = string.Empty;
				foreach (string item in list2)
				{
					if (item.Length > text.Length)
					{
						text = item;
					}
				}
			}
			else
			{
				text = UIUtils.CollectFirstText(val2) ?? string.Empty;
				if (string.IsNullOrEmpty(text))
				{
					string text2 = UIUtils.TryReadStars(val2);
					if (text2 != null)
					{
						text = text2;
					}
				}
			}
			list.Add(text);
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
}
