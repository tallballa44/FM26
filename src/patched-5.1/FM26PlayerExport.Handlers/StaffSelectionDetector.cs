using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

internal static class StaffSelectionDetector
{
    internal static bool IsSelected(VisualElement row)
    {
        return FindSelectionMarker(row, null, 0);
    }

    internal static string DescribeSelectionMarkers(VisualElement row)
    {
        var markers = new List<string>();
        FindSelectionMarker(row, markers, 0);
        return markers.Count == 0 ? "(none)" : string.Join(" | ", markers);
    }

    private static bool FindSelectionMarker(VisualElement element, List<string> markers, int depth)
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
                    (lower.Contains("selected") && !lower.Contains("unselected")) ||
                    (lower.Contains("checked") && !lower.Contains("unchecked"));

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
            if (FindSelectionMarker(element.ElementAt(i), markers, depth + 1))
                selected = true;
        }

        return selected;
    }
}
