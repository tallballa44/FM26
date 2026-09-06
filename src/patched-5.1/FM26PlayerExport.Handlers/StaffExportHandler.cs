using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public class StaffExportHandler : GenericScrolledTableHandler
{
    public override bool TryStartCapture(VisualElement root, out string errorMessage)
    {
        base.FilePrefix = "staff_export_";
        return base.TryStartCapture(root, out errorMessage);
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

        bool hasPerson = HasHeader(headers, "person");
        bool hasPreferredJob = HasHeader(headers, "preferred job");
        bool hasJob = HasHeader(headers, "job");
        bool hasStaffAttribute =
            HasHeader(headers, "judging staff ability") ||
            HasHeader(headers, "tactical knowledge") ||
            HasHeader(headers, "working with youngsters");

        return hasPerson && (hasPreferredJob || (hasJob && hasStaffAttribute));
    }

    protected override bool ShouldCaptureRow(VisualElement row)
    {
        return StaffSelectionDetector.IsSelected(row);
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
