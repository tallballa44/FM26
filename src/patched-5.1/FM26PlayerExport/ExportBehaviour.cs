using System;
using System.Collections.Generic;
using FM26PlayerExport.Handlers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26PlayerExport;

public class ExportBehaviour : MonoBehaviour
{
    private int _frame;
    private IExportHandler _currentHandler;
    private readonly List<IExportHandler> _availableHandlers = new();
    private bool _isQuitting;

    public ExportBehaviour(IntPtr ptr) : base(ptr) { }

    private void Start()
    {
        _availableHandlers.Add(new MatchStatsExportHandler());
        _availableHandlers.Add(new StaffExportHandler());
        _availableHandlers.Add(new CalendarExportHandler());
        _availableHandlers.Add(new PlayerExportHandler());
    }

    private void OnDestroy() => CleanupReferences();
    private void OnApplicationQuit() => CleanupReferences();
    private void OnDisable() => CleanupReferences();

    private void CleanupReferences()
    {
        _isQuitting = true;

        foreach (var handler in _availableHandlers)
        {
            try { handler.Cleanup(); }
            catch { }
        }

        _availableHandlers.Clear();
        _currentHandler = null;

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        catch { }
    }

    private void Update()
    {
        if (_isQuitting)
            return;

        try
        {
            _frame++;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (_currentHandler == null)
            {
                if (keyboard.f8Key.wasPressedThisFrame)
                {
                    RunDiagnostics();
                    return;
                }

                bool ctrlP =
                    (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
                    keyboard.pKey.wasPressedThisFrame;

                bool f9 = keyboard.f9Key.wasPressedThisFrame;

                if (ctrlP || f9)
                {
                    Plugin.Log.LogInfo($"[FM26Export] Starting export via hotkey: {(f9 ? "F9" : "Ctrl+P")}");
                    StartCapture();
                }
            }
            else if (_currentHandler.CaptureStep())
            {
                _currentHandler.FinishCapture();

                try { _currentHandler.Cleanup(); }
                catch { }

                _currentHandler = null;

                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch { }
            }
        }
        catch
        {
            // Preserve 5.1 behavior during Unity/FM teardown.
        }
    }

    private VisualElement GetMainRoot()
    {
        try
        {
            var all = FindObjectsOfType<UIDocument>();
            if (all == null)
                return null;

            foreach (var doc in all)
            {
                if (doc != null &&
                    doc.rootVisualElement != null &&
                    doc.rootVisualElement.name == "PanelManager-container")
                {
                    return doc.rootVisualElement;
                }
            }
        }
        catch { }

        return null;
    }

    private void RunDiagnostics()
    {
        var root = GetMainRoot();
        if (root == null)
        {
            Plugin.Log.LogError("[FM26Export.Scan] Main PanelManager UIDocument was not found.");
            return;
        }

        TableDiagnostics.Scan(root, Plugin.Log);
    }

    private void StartCapture()
    {
        try
        {
            var root = GetMainRoot();
            if (root == null)
            {
                Plugin.Log.LogError("[FM26Export] Main PanelManager UIDocument was not found.");
                return;
            }

            bool handled = false;

            foreach (var handler in _availableHandlers)
            {
                if (handler.TryStartCapture(root, out string errorMessage))
                {
                    Plugin.Log.LogInfo($"[FM26Export] Using handler: {handler.GetType().Name}");
                    _currentHandler = handler;
                    handled = true;
                    break;
                }

                if (!string.IsNullOrEmpty(errorMessage))
                    Plugin.Log.LogInfo(errorMessage);
            }

            if (!handled)
                Plugin.Log.LogWarning("[FM26Export] No supported export screen was found.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[FM26Export] Error starting capture: {ex.Message}");
        }
    }
}
