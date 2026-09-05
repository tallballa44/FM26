using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public abstract class GenericScrolledTableHandler : IExportHandler
    {
        private const int WAIT_FRAMES    = 4;
        private const int MAX_SCROLL     = 500;
        private const int MAX_ROWS       = 5000;
        private const int ZERO_STEPS_MAX = 3;

        protected VisualElement _captureView;
        protected List<string>  _captureHeaders;
        protected List<List<string>> _capturedRows;
        private HashSet<string>    _seenKeys;
        private float _lastScrollY;
        private int   _scrollAttempts;
        private int   _zeroSteps;
        private bool  _diagLogged;
        
        private int _captureWait = 0;
        private bool _isComplete = false;

        protected string FilePrefix { get; set; } = "export_";

        public virtual bool TryStartCapture(VisualElement root, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            VisualElement targetHeaders = null;
            VisualElement targetView = null;

            // Preferência 1: IDs Conhecidos Estáveis (Player & Database)
            var pt = UIUtils.FindByName(root, "playertable") ?? UIUtils.FindByName(root, "client-object-viewer-table");
            if (pt != null)
            {
                targetHeaders = UIUtils.FindByName(pt, "column-headers");
                targetView = UIUtils.FindByName(pt, "View");
            }

            // Preferência 2: Busca Genérica (Staff Search e Tabelas Novas)
            if (targetHeaders == null || targetView == null)
            {
                var chList = new List<VisualElement>();
                FindAllByName(root, "column-headers", chList);
                foreach (var chEl in chList)
                {
                    var v = UIUtils.FindByName(chEl.parent, "View") 
                         ?? (chEl.parent?.parent != null ? UIUtils.FindByName(chEl.parent.parent, "View") : null)
                         ?? (chEl.parent?.parent?.parent != null ? UIUtils.FindByName(chEl.parent.parent.parent, "View") : null);

                    if (v != null && v.childCount > 0)
                    {
                        targetHeaders = chEl;
                        targetView = v;
                        break;
                    }
                }
            }

            if (targetHeaders == null || targetView == null) 
            {
                errorMessage = "[FM26Export.GenericTable] Nenhuma tabela de lista ('playertable' ou genérica) encontrada na UI."; 
                return false; 
            }
            
            _captureView = targetView;
            
            // Capture Headers
            _captureHeaders = new List<string>();
            for (int i = 1; i < targetHeaders.childCount; i++) // skip checkbox
            {
                var thEl = targetHeaders.ElementAt(i);
                var txts = new List<string>();
                UIUtils.CollectAllTexts(thEl, txts);

                string bestTxt = string.Empty;
                foreach (var tx in txts)
                {
                    // Ignore common sort tooltips that could override the real name
                    string tLow = tx.ToLowerInvariant();
                    if (tLow.Contains("sort") || tLow.Contains("orden") || tLow.Contains("order"))
                        continue;

                    if (tx.Length > bestTxt.Length) 
                        bestTxt = tx;
                }

                // Fallback Se tudo falhar e só sobrou o tooltip de sort ou abreviação
                if (string.IsNullOrWhiteSpace(bestTxt))
                {
                    var tip = UIUtils.CollectFirstTooltip(thEl);
                    var txt = UIUtils.CollectFirstText(thEl);
                    bestTxt = !string.IsNullOrWhiteSpace(tip) ? tip : txt;
                }

                _captureHeaders.Add(bestTxt != null ? UIUtils.Esc(bestTxt) : $"Col{i}");
            }
            if (_captureHeaders.Count == 0) _captureHeaders.Add("Dados");

            // Validar com a subclasse se essa tabela é realmente dela
            if (!IsValidScreen(root, _captureHeaders))
            {
                errorMessage = $"[FM26Export] Tabela encontrada, mas rejeitada pelo handler genérico/filho. Headers: {string.Join(", ", _captureHeaders)}";
                return false;
            }

            Plugin.Log.LogInfo($"[FM26Export] Headers ({_captureHeaders.Count}): {string.Join(" | ", _captureHeaders)}");

            _capturedRows   = new List<List<string>>();
            _seenKeys       = new HashSet<string>();
            _scrollAttempts = 0;
            _zeroSteps      = 0;
            _lastScrollY    = -1f;
            _diagLogged     = false;
            _isComplete     = false;
            
            var sv = _captureView.GetFirstAncestorOfType<ScrollView>();
            if (sv != null) sv.scrollOffset = Vector2.zero;

            _captureWait = WAIT_FRAMES;
            Plugin.Log.LogInfo($"[FM26Export] Captura de lista ({FilePrefix}) iniciada...");
            return true;
        }

        protected abstract bool IsValidScreen(VisualElement root, List<string> headers);

        public bool CaptureStep()
        {
            if (_isComplete) return true;
            if (_captureWait > 0) 
            { 
                _captureWait--; 
                return false; 
            }

            try
            {
                int newCount = 0;
                for (int i = 0; i < _captureView.childCount; i++)
                {
                    var row = _captureView.ElementAt(i);
                    bool sel = false;
                    try { sel = row.ClassListContains("virtualised-list__item--selected"); } catch { }
                    if (!sel) continue;

                    bool dodiag = !_diagLogged && _scrollAttempts == 0 && newCount == 0;
                    var vals = ReadRow(row, dodiag, _captureHeaders);
                    if (dodiag) _diagLogged = true;
                    if (vals.Count == 0) continue;

                    string key = UIUtils.RowKey(vals);
                    if (string.IsNullOrEmpty(key) || _seenKeys.Contains(key)) continue;
                    
                    _seenKeys.Add(key);
                    _capturedRows.Add(vals);
                    newCount++;

                    if (_capturedRows.Count >= MAX_ROWS)
                    {
                        Plugin.Log.LogWarning($"[FM26Export] Limite de {MAX_ROWS} linhas atingido.");
                        _isComplete = true; 
                        return true;
                    }
                }

                var sv = _captureView.GetFirstAncestorOfType<ScrollView>();
                float currentY = sv != null ? sv.scrollOffset.y : 0;
                _scrollAttempts++;
                bool atBottom = Math.Abs(currentY - _lastScrollY) < 0.5f && _lastScrollY >= 0;

                if (newCount == 0) _zeroSteps++; else _zeroSteps = 0;
                bool stalled = _zeroSteps >= ZERO_STEPS_MAX;

                Plugin.Log.LogInfo($"[FM26Export] Step {_scrollAttempts}: +{newCount} | total={_capturedRows.Count} | scrollY={currentY:F0} | fim={atBottom} | stall={_zeroSteps}/{ZERO_STEPS_MAX}");

                if (atBottom || _scrollAttempts >= MAX_SCROLL || stalled)
                {
                    if (stalled && !atBottom) Plugin.Log.LogWarning("[FM26Export] Parado por falta de novos dados.");
                    _isComplete = true;
                    return true;
                }

                _lastScrollY = currentY;
                float ph = sv != null && sv.layout.height > 0 ? sv.layout.height : 600f;
                if (sv != null) sv.scrollOffset = new Vector2(0, currentY + ph);
                _captureWait = WAIT_FRAMES;
                return false;
            }
            catch (Exception ex) 
            { 
                Plugin.Log.LogError($"[FM26Export] Erro CaptureStep: {ex.Message}"); 
                _isComplete = true;
                return true; 
            }
        }

        public void FinishCapture()
        {
            try
            {
                if (_capturedRows.Count == 0) { Plugin.Log.LogWarning($"[FM26Export] Nenhum dado para {FilePrefix}."); return; }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sports Interactive", "Football Manager 26", "FM26PlayerExport by vinteset");
                string csvDir = Path.Combine(baseDir, "Exports CSV");
                string htmlDir = Path.Combine(baseDir, "Exports HTML");

                Directory.CreateDirectory(csvDir);
                Directory.CreateDirectory(htmlDir);

                // CSV EXPORT
                var csv = new StringBuilder();
                csv.AppendLine(string.Join(";", _captureHeaders));
                foreach (var row in _capturedRows) csv.AppendLine(string.Join(";", row.ConvertAll(UIUtils.Esc)));
                
                string csvFile = Path.Combine(csvDir, $"{FilePrefix}{timestamp}.csv");
                File.WriteAllText(csvFile, csv.ToString(), Encoding.UTF8);

                // HTML EXPORT
                var html = new StringBuilder();
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset=\"UTF-8\">");
                html.AppendLine("<style type =\"text/css\">");
                html.AppendLine("body,td,th { font-family: Verdana, Arial, Helvetica, sans-serif; font-size: 12px; }");
                html.AppendLine("th { padding: 5px; text-align: left; background-color: #EEEEEE; border: 1px solid #000000; font-weight: bold; }");
                html.AppendLine("td { padding: 4px; border: 1px solid #000000; }");
                html.AppendLine("table { border-collapse: collapse; width: 98%; margin: 20px auto; }");
                html.AppendLine("tr:nth-child(even) { background-color: #F9F9F9; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<table border=\"1\">");

                // Headers
                html.AppendLine("<tr>");
                foreach (var header in _captureHeaders)
                {
                    html.AppendLine($"\t<th>{header}</th>");
                }
                html.AppendLine("</tr>");

                // Rows
                foreach (var row in _capturedRows)
                {
                    bool isEmpty = true;
                    foreach (var cell in row) if (!string.IsNullOrEmpty(cell)) { isEmpty = false; break; }
                    if (isEmpty) continue;

                    html.AppendLine("<tr>");
                    foreach (var cell in row)
                    {
                        html.AppendLine($"\t<td>{cell}</td>");
                    }
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table></body></html>");
                string htmlFile = Path.Combine(htmlDir, $"{FilePrefix}{timestamp}.html");
                File.WriteAllText(htmlFile, html.ToString(), Encoding.UTF8);

                Plugin.Log.LogInfo($"[FM26Export] ✅ {_capturedRows.Count} exportados.");
                Plugin.Log.LogInfo($"[FM26Export] CSV salvo em: {csvFile}");
                Plugin.Log.LogInfo($"[FM26Export] HTML salvo em: {htmlFile}");
            }
            catch (Exception ex) { Plugin.Log.LogError($"[FM26Export] Erro FinishCapture: {ex.Message}"); }
        }

        public virtual void Cleanup()
        {
            _captureView = null;
            if (_captureHeaders != null) _captureHeaders.Clear();
            if (_capturedRows != null) _capturedRows.Clear();
            if (_seenKeys != null) _seenKeys.Clear();
        }

        private List<string> ReadRow(VisualElement row, bool diag, List<string> headers)
        {
            var vals = new List<string>();
            if (row == null || row.childCount == 0) return vals;
            
            var sel = row.ElementAt(0);
            if (sel.childCount == 1 && sel.ElementAt(0).childCount > 1)
                sel = sel.ElementAt(0);

            for (int c = 1; c < sel.childCount; c++)
            {
                var cell = sel.ElementAt(c);
                string val;

                if (c == 1)
                {
                    var txts = new List<string>();
                    UIUtils.CollectAllTexts(cell, txts);
                    if (diag) Plugin.Log.LogInfo($"[FM26Export] Célula[1] DIAG: {UIUtils.DiagCell(cell)}");
                    val = string.Empty;
                    foreach (var tx in txts) if (tx.Length > val.Length) val = tx;
                }
                else
                {
                    val = UIUtils.CollectFirstText(cell) ?? string.Empty;
                    if (string.IsNullOrEmpty(val))
                    {
                        var stars = UIUtils.TryReadStars(cell);
                        if (stars != null) val = stars;
                    }
                }
                vals.Add(val);
            }
            return vals;
        }

        private void FindAllByName(VisualElement root, string name, List<VisualElement> results)
        {
            if (root == null) return;
            if (root.name == name) results.Add(root);
            for (int i = 0; i < root.childCount; i++)
            {
                FindAllByName(root.ElementAt(i), name, results);
            }
        }
    }
}
