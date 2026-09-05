using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public class PlayerExportHandler : GenericScrolledTableHandler
    {
        public PlayerExportHandler()
        {
            FilePrefix = "person_"; // Default to generic, but user expects player/moneyball
        }

        public override bool TryStartCapture(VisualElement root, out string errorMessage)
        {
            bool ret = base.TryStartCapture(root, out errorMessage);
            if (ret)
            {
                // Verify if it's REALLY player export, or maybe it's the generic one
                // Use moneyball prefix to match old v4 behavior
                FilePrefix = "moneyball_export_";
            }
            return ret;
        }

        protected override bool IsValidScreen(VisualElement root, List<string> headers)
        {
            // If it reached here (the last generic handler checked), it wasn't intercepted by StaffExportHandler.
            // We assume it's moneyball (player view).
            return true;
        }
    }
}
