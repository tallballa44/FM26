using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public class StaffExportHandler : GenericScrolledTableHandler
{
    public override bool TryStartCapture(VisualElement root, out string errorMessage)
    {
        if (base.TryStartCapture(root, out errorMessage))
        {
            base.FilePrefix = "staff_export_";
            return true;
        }

        return false;
    }

    protected override bool IsValidScreen(VisualElement root, List<string> headers)
    {
        if (_captureView == null)
            return false;

        for (VisualElement current = _captureView; current != null; current = current.parent)
        {
            if (string.IsNullOrEmpty(current.name))
                continue;

            string name = current.name.ToLowerInvariant();
            if (name.Contains("staff") || name.Contains("non_player") || name.Contains("nonplayer"))
                return true;
        }

        return HasHeader(headers, "person") && HasHeader(headers, "preferred job");
    }

    protected override bool ShouldCaptureRow(VisualElement row)
    {
        // Staff Search does not expose the same selected-row class used by player lists.
        // Export the rows rendered by the staff search table and let scrolling/deduplication
        // collect the full filtered result set.
        return row != null && row.childCount > 0;
    }

    private static bool HasHeader(List<string> headers, string expected)
    {
        if (headers == null)
            return false;

        foreach (string header in headers)
        {
            if (string.Equals((header ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
