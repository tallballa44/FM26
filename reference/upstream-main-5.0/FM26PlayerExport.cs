using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using FM26PlayerExport.Handlers;

namespace FM26PlayerExport
{
    [BepInPlugin("com.koda.fm26.playerexport", "FM26 Player Export", "5.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        private static ExportBehaviour _behaviour;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("[FM26Export v5] Carregado! Atalhos: [F9 ou Ctrl+P] = exportar | [F8] = re-escanear");
            _behaviour = AddComponent<ExportBehaviour>();
        }

        public override bool Unload()
        {
            Log.LogInfo("[FM26Export v5] Descarregando plugin...");
            if (_behaviour != null) UnityEngine.Object.Destroy(_behaviour);
            return base.Unload();
        }
    }

    public class ExportBehaviour : MonoBehaviour
    {
        private int _frame = 0;
        private IExportHandler _currentHandler;
        private List<IExportHandler> _availableHandlers = new List<IExportHandler>();

        public ExportBehaviour(IntPtr ptr) : base(ptr) { }

        private void Start()
        {
            _availableHandlers.Add(new MatchStatsExportHandler());
            _availableHandlers.Add(new StaffExportHandler());
            _availableHandlers.Add(new CalendarExportHandler());
            _availableHandlers.Add(new PlayerExportHandler());
        }

        private bool _isQuitting = false;

        private void OnDestroy() { LimparRefs(); }
        private void OnApplicationQuit() { LimparRefs(); }
        private void OnDisable() { LimparRefs(); }

        private void LimparRefs()
        {
            _isQuitting = true;
            if (_availableHandlers != null)
            {
                foreach (var h in _availableHandlers)
                {
                    try { h.Cleanup(); } catch { }
                }
                _availableHandlers.Clear();
            }
            _currentHandler = null;

            try 
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            } 
            catch { }
        }

        private void Update()
        {
            if (_isQuitting) return;
            try 
            {
                _frame++;
                if (Keyboard.current == null) return;
                
                if (_currentHandler == null)
                {
                    bool ctrlP = (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) && Keyboard.current.pKey.wasPressedThisFrame;
                    bool f9 = Keyboard.current.f9Key.wasPressedThisFrame;
                    
                    if (ctrlP || f9)
                    {
                        Plugin.Log.LogInfo($"[FM26Export] Iniciando exportação via atalho: {(f9 ? "F9" : "Ctrl+P")}");
                        StartCapture();
                    }
                }
                else
                {
                    bool isComplete = _currentHandler.CaptureStep();
                    if (isComplete)
                    {
                        _currentHandler.FinishCapture();
                        
                        // LIMPEZA IMEDIATA: Assim que a exportação termina, devemos soltar
                        // instantaneamente as referências ao UI Elements (C++) para evitar 
                        // ObjectReferenceException nativos do Unity em caso de fechamento do jogo!
                        try { _currentHandler.Cleanup(); } catch {}
                        _currentHandler = null;

                        try { System.GC.Collect(); System.GC.WaitForPendingFinalizers(); } catch {}
                    }
                }
            }
            catch (Exception)
            {
                // Silently catch exceptions during teardown
            }
        }

        private VisualElement GetMainRoot()
        {
            try {
                var all = FindObjectsOfType<UIDocument>();
                if (all == null) return null;
                foreach (var doc in all) {
                    if (doc != null && doc.rootVisualElement != null && doc.rootVisualElement.name == "PanelManager-container")
                        return doc.rootVisualElement;
                }
            } catch { }
            return null;
        }

        private void StartCapture()
        {
            try
            {
                VisualElement root = GetMainRoot();
                if (root == null) { 
                    Plugin.Log.LogError("[FM26Export] Sem UIDocument (Painel principal não encontrado)."); 
                    return; 
                }

                bool handled = false;
                foreach (var handler in _availableHandlers)
                {
                    if (handler.TryStartCapture(root, out string errorMessage))
                    {
                        Plugin.Log.LogInfo($"[FM26Export] Usando handler: {handler.GetType().Name}");
                        _currentHandler = handler;
                        handled = true;
                        break;
                    }
                    else if (!string.IsNullOrEmpty(errorMessage))
                    {
                        Plugin.Log.LogInfo(errorMessage);
                    }
                }

                if (!handled)
                {
                    Plugin.Log.LogWarning("[FM26Export] Nenhuma tela suportada para exportação encontrada no momento.");
                }
            }
            catch (Exception ex) 
            { 
                Plugin.Log.LogError($"[FM26Export] Erro ao iniciar captura: {ex.Message}"); 
            }
        }
    }
}
