using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers;

public class PlayerExportHandler : GenericScrolledTableHandler
{
    public PlayerExportHandler()
    {
        base.FilePrefix = "person_";
    }

    public override bool TryStartCapture(VisualElement root, out string errorMessage)
    {
        bool started = base.TryStartCapture(root, out errorMessage);
        if (started)
            base.FilePrefix = "moneyball_export_";

        return started;
    }

    protected override bool IsValidScreen(VisualElement root, List<string> headers)
    {
        if (LooksLikeStandings(headers) || LooksLikeStaff(headers))
            return false;

        for (VisualElement current = _captureView; current != null; current = current.parent)
        {
            if (string.IsNullOrEmpty(current.name))
                continue;

            string name = current.name.ToLowerInvariant();

            if (name.Contains("staff") || name.Contains("non_player") || name.Contains("nonplayer"))
                return false;

            if (name.Contains("playertable") || name.Contains("client-object-viewer-table"))
                return true;
        }

        // Preserve 5.1 compatibility for custom player views while blocking the
        // two false-positive signatures we have directly observed.
        return true;
    }

    private static bool LooksLikeStaff(List<string> headers)
    {
        return HasHeader(headers, "person") && HasHeader(headers, "preferred job");
    }

    private static bool LooksLikeStandings(List<string> headers)
    {
        return HasHeader(headers, "team") &&
               HasHeader(headers, "pld") &&
               HasHeader(headers, "w") &&
               HasHeader(headers, "d") &&
               HasHeader(headers, "l") &&
               HasHeader(headers, "pts");
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
