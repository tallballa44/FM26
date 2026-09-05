using System;
using System.Collections.Generic;
using BepInEx.Logging;
using FM26PlayerExport.Handlers;
using UnityEngine.UIElements;

namespace FM26PlayerExport;

internal static class TableDiagnostics
{
    internal static void Scan(VisualElement root, ManualLogSource log)
    {
        if (root == null)
        {
            log.LogWarning("[FM26Export.Scan] Scan skipped because the UI root is null.");
            return;
        }

        log.LogInfo("[FM26Export.Scan] UI diagnostics started.");

        var headerElements = new List<VisualElement>();
        FindAllByName(root, "column-headers", headerElements);

        var seenViews = new HashSet<VisualElement>();
        int candidateNumber = 0;

        foreach (var headersElement in headerElements)
        {
            var view = FindNearbyView(headersElement);
            if (view == null || view.childCount == 0 || seenViews.Contains(view))
                continue;

            seenViews.Add(view);
            candidateNumber++;

            var headers = ReadHeaders(headersElement);
            int selectedRows = CountSelectedRows(view);
            int renderedRows = view.childCount;
            bool visible = IsElementVisible(view);
            string container = FindContainerName(headersElement);
            string classification = Classify(headers);

            log.LogInfo($"[FM26Export.Scan] Candidate {candidateNumber}");
            log.LogInfo($"[FM26Export.Scan]   Container: {container}");
            log.LogInfo($"[FM26Export.Scan]   View: {SafeName(view)} | visible={visible} | renderedRows={renderedRows} | selectedRows={selectedRows}");
            log.LogInfo($"[FM26Export.Scan]   Headers ({headers.Count}): {string.Join(" | ", headers)}");
            log.LogInfo($"[FM26Export.Scan]   Classification: {classification}");
        }

        if (candidateNumber == 0)
        {
            log.LogWarning("[FM26Export.Scan] No candidate list tables were found.");
        }
        else
        {
            log.LogInfo($"[FM26Export.Scan] Scan complete. Candidates found: {candidateNumber}.");
            log.LogInfo("[FM26Export.Scan] Diagnostic scan does not change export routing in Patch 0.1.");
        }
    }

    private static VisualElement FindNearbyView(VisualElement headers)
    {
        VisualElement scope = headers?.parent;

        for (int depth = 0; scope != null && depth < 4; depth++, scope = scope.parent)
        {
            var view = UIUtils.FindByName(scope, "View");
            if (view != null)
                return view;
        }

        return null;
    }

    private static List<string> ReadHeaders(VisualElement headersElement)
    {
        var headers = new List<string>();
        if (headersElement == null)
            return headers;

        for (int i = 1; i < headersElement.childCount; i++)
        {
            var headerCell = headersElement.ElementAt(i);
            var texts = new List<string>();
            UIUtils.CollectAllTexts(headerCell, texts);

            string best = string.Empty;

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string lower = text.ToLowerInvariant();
                if (lower.Contains("sort") || lower.Contains("orden") || lower.Contains("order"))
                    continue;

                if (text.Length > best.Length)
                    best = text;
            }

            if (string.IsNullOrWhiteSpace(best))
            {
                var tooltip = UIUtils.CollectFirstTooltip(headerCell);
                var text = UIUtils.CollectFirstText(headerCell);
                best = !string.IsNullOrWhiteSpace(tooltip) ? tooltip : text;
            }

            headers.Add(best ?? $"Col{i}");
        }

        if (headers.Count == 0)
            headers.Add("Data");

        return headers;
    }

    private static int CountSelectedRows(VisualElement view)
    {
        int selected = 0;

        for (int i = 0; i < view.childCount; i++)
        {
            try
            {
                if (view.ElementAt(i).ClassListContains("virtualised-list__item--selected"))
                    selected++;
            }
            catch { }
        }

        return selected;
    }

    private static bool IsElementVisible(VisualElement element)
    {
        if (element == null)
            return false;

        try
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (!current.visible)
                    return false;

                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            }

            var bounds = element.worldBound;
            return bounds.width > 0f && bounds.height > 0f;
        }
        catch
        {
            return true;
        }
    }

    private static string FindContainerName(VisualElement element)
    {
        string fallback = string.Empty;

        for (var current = element; current != null; current = current.parent)
        {
            if (string.IsNullOrWhiteSpace(current.name))
                continue;

            if (string.IsNullOrEmpty(fallback))
                fallback = current.name;

            string lower = current.name.ToLowerInvariant();
            if (lower.Contains("playertable") ||
                lower.Contains("client-object-viewer-table") ||
                lower.Contains("staff") ||
                lower.Contains("non_player") ||
                lower.Contains("streamedtable"))
            {
                return current.name;
            }
        }

        return string.IsNullOrEmpty(fallback) ? "(unnamed)" : fallback;
    }

    private static string Classify(List<string> headers)
    {
        bool hasPerson = ContainsHeader(headers, "person");
        bool hasPreferredJob = ContainsHeader(headers, "preferred job");
        bool hasTeam = ContainsHeader(headers, "team");
        bool hasPld = ContainsHeader(headers, "pld");
        bool hasW = ContainsHeader(headers, "w");
        bool hasD = ContainsHeader(headers, "d");
        bool hasL = ContainsHeader(headers, "l");
        bool hasPts = ContainsHeader(headers, "pts");

        if (hasPerson && hasPreferredJob)
            return "Likely Staff";

        if (hasTeam && hasPld && hasW && hasD && hasL && hasPts)
            return "Standings / unrelated";

        bool hasAge = ContainsHeader(headers, "age");
        bool hasPosition = ContainsHeader(headers, "position") || ContainsHeader(headers, "pos");
        bool hasValue = ContainsHeader(headers, "transfer value") || ContainsHeader(headers, "value");

        if ((hasAge && hasPosition) || (hasPosition && hasValue))
            return "Likely Player";

        return "Unknown";
    }

    private static bool ContainsHeader(List<string> headers, string expected)
    {
        foreach (var header in headers)
        {
            if (string.Equals((header ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string SafeName(VisualElement element)
    {
        if (element == null)
            return "(null)";

        return string.IsNullOrWhiteSpace(element.name) ? "(unnamed View)" : element.name;
    }

    private static void FindAllByName(VisualElement root, string name, List<VisualElement> results)
    {
        if (root == null)
            return;

        if (root.name == name)
            results.Add(root);

        for (int i = 0; i < root.childCount; i++)
            FindAllByName(root.ElementAt(i), name, results);
    }
}
