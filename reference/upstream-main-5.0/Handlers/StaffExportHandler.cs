using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public class StaffExportHandler : GenericScrolledTableHandler
    {
        public override bool TryStartCapture(VisualElement root, out string errorMessage)
        {
            if (base.TryStartCapture(root, out errorMessage))
            {
                FilePrefix = "staff_export_";
                return true;
            }
            return false;
        }

        protected override bool IsValidScreen(VisualElement root, List<string> headers)
        {
            if (_captureView == null) return false;
            var p = _captureView;
            while (p != null)
            {
                if (!string.IsNullOrEmpty(p.name))
                {
                    string nm = p.name.ToLower();
                    if (nm.Contains("staff") || nm.Contains("non_player")) return true;
                }
                p = p.parent;
            }
            return false;
        }
    }
}
