using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

internal static class StaffSelectionDetector
{
    internal static bool IsSelected(VisualElement row)
    {
        return FindStrictSelectionMarker(row, null, 0);
    }

    internal static string DescribeSelectionMarkers(VisualElement row)
    {
        var markers = new List<string>();
        FindStrictSelectionMarker(row, markers, 0);
        return markers.Count == 0 ? "(none)" : string.Join(" | ", markers);
    }

    internal static string DescribeRowState(VisualElement row)
    {
        if (row == null)
            return "(null row)";

        var parts = new List<string>();

        try
        {
            parts.Add($"rowName={Safe(row.name)}");
            parts.Add($"viewDataKey={Safe(row.viewDataKey)}");
            Color bg = row.resolvedStyle.backgroundColor;
            parts.Add($"rowBg={ColorText(bg)}");
        }
        catch { }

        var interestingClasses = new List<string>();
        var backgrounds = new List<string>();
        CollectDiagnostics(row, interestingClasses, backgrounds, 0);

        parts.Add("classes=" + (interestingClasses.Count == 0 ? "(none)" : string.Join(",", interestingClasses)));
        parts.Add("backgrounds=" + (backgrounds.Count == 0 ? "(none)" : string.Join(",", backgrounds)));
        parts.Add("strictSelected=" + IsSelected(row));

        return string.Join(" | ", parts);
    }

    private static bool FindStrictSelectionMarker(VisualElement element, List<string> markers, int depth)
    {
        if (element == null || depth > 10)
            return false;

        bool selected = false;

        try
        {
            for (int i = 0; i < element.classList.Count; i++)
            {
                string cls = element.classList[i] ?? string.Empty;
                string lower = cls.ToLowerInvariant();

                bool selectedClass =
                    lower == "virtualised-list__item--selected" ||
                    lower == "selected" ||
                    lower == "is-selected" ||
                    lower.EndsWith("--selected") ||
                    lower.EndsWith("__selected") ||
                    lower.EndsWith("-selected") ||
                    lower.EndsWith("_selected");

                if (selectedClass)
                {
                    selected = true;
                    if (markers != null && markers.Count < 8)
                        markers.Add($"class:{cls}");
                }
            }
        }
        catch { }

        try
        {
            Toggle toggle = ((Il2CppObjectBase)element).TryCast<Toggle>();
            if (toggle != null && toggle.value)
            {
                selected = true;
                if (markers != null && markers.Count < 8)
                    markers.Add("toggle:true");
            }
        }
        catch { }

        for (int i = 0; i < element.childCount; i++)
        {
            if (FindStrictSelectionMarker(element.ElementAt(i), markers, depth + 1))
                selected = true;
        }

        return selected;
    }

    private static void CollectDiagnostics(
        VisualElement element,
        List<string> interestingClasses,
        List<string> backgrounds,
        int depth)
    {
        if (element == null || depth > 6)
            return;

        try
        {
            for (int i = 0; i < element.classList.Count && interestingClasses.Count < 20; i++)
            {
                string cls = element.classList[i] ?? string.Empty;
                string lower = cls.ToLowerInvariant();

                if (lower.Contains("select") ||
                    lower.Contains("check") ||
                    lower.Contains("active") ||
                    lower.Contains("focus") ||
                    lower.Contains("highlight") ||
                    lower.Contains("current"))
                {
                    string item = $"{depth}:{cls}";
                    if (!interestingClasses.Contains(item))
                        interestingClasses.Add(item);
                }
            }
        }
        catch { }

        try
        {
            Color bg = element.resolvedStyle.backgroundColor;
            if (bg.a > 0.001f && backgrounds.Count < 12)
            {
                string item = $"{depth}:{ColorText(bg)}";
                if (!backgrounds.Contains(item))
                    backgrounds.Add(item);
            }
        }
        catch { }

        try
        {
            Toggle toggle = ((Il2CppObjectBase)element).TryCast<Toggle>();
            if (toggle != null && interestingClasses.Count < 20)
                interestingClasses.Add($"{depth}:toggle={toggle.value}");
        }
        catch { }

        for (int i = 0; i < element.childCount; i++)
            CollectDiagnostics(element.ElementAt(i), interestingClasses, backgrounds, depth + 1);
    }

    private static string ColorText(Color c)
    {
        return $"{c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2}";
    }

    private static string Safe(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(empty)" : value;
    }
}
