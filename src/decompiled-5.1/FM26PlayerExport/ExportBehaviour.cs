using System;
using System.Collections.Generic;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using FM26PlayerExport.Handlers;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

namespace FM26PlayerExport;

public class ExportBehaviour(IntPtr ptr) : MonoBehaviour(ptr)
{
	private int _frame;

	private IExportHandler _currentHandler;

	private List<IExportHandler> _availableHandlers = new List<IExportHandler>();

	private bool _isQuitting;

	private void Start()
	{
		_availableHandlers.Add(new MatchStatsExportHandler());
		_availableHandlers.Add(new StaffExportHandler());
		_availableHandlers.Add(new CalendarExportHandler());
		_availableHandlers.Add(new PlayerExportHandler());
	}

	private void OnDestroy()
	{
		LimparRefs();
	}

	private void OnApplicationQuit()
	{
		LimparRefs();
	}

	private void OnDisable()
	{
		LimparRefs();
	}

	private void LimparRefs()
	{
		_isQuitting = true;
		if (_availableHandlers != null)
		{
			foreach (IExportHandler availableHandler in _availableHandlers)
			{
				try
				{
					availableHandler.Cleanup();
				}
				catch
				{
				}
			}
			_availableHandlers.Clear();
		}
		_currentHandler = null;
		try
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		catch
		{
		}
	}

	private void Update()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		if (_isQuitting)
		{
			return;
		}
		try
		{
			_frame++;
			if (Keyboard.current == null)
			{
				return;
			}
			if (_currentHandler == null)
			{
				bool num = (((ButtonControl)Keyboard.current.leftCtrlKey).isPressed || ((ButtonControl)Keyboard.current.rightCtrlKey).isPressed) && ((ButtonControl)Keyboard.current.pKey).wasPressedThisFrame;
				bool wasPressedThisFrame = ((ButtonControl)Keyboard.current.f9Key).wasPressedThisFrame;
				if (num | wasPressedThisFrame)
				{
					ManualLogSource log = Plugin.Log;
					bool flag = default(bool);
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(46, 1, ref flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export] Iniciando exportação via atalho: ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(wasPressedThisFrame ? "F9" : "Ctrl+P");
					}
					log.LogInfo(val);
					StartCapture();
				}
			}
			else if (_currentHandler.CaptureStep())
			{
				_currentHandler.FinishCapture();
				try
				{
					_currentHandler.Cleanup();
				}
				catch
				{
				}
				_currentHandler = null;
				try
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
					return;
				}
				catch
				{
					return;
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private VisualElement GetMainRoot()
	{
		try
		{
			Il2CppArrayBase<UIDocument> val = Object.FindObjectsOfType<UIDocument>();
			if (val == null)
			{
				return null;
			}
			foreach (UIDocument item in val)
			{
				if ((Object)(object)item != (Object)null && item.rootVisualElement != null && item.rootVisualElement.name == "PanelManager-container")
				{
					return item.rootVisualElement;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private void StartCapture()
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		bool flag2 = default(bool);
		try
		{
			VisualElement mainRoot = GetMainRoot();
			if (mainRoot == null)
			{
				Plugin.Log.LogError((object)"[FM26Export] Sem UIDocument (Painel principal não encontrado).");
				return;
			}
			bool flag = false;
			foreach (IExportHandler availableHandler in _availableHandlers)
			{
				if (availableHandler.TryStartCapture(mainRoot, out var errorMessage))
				{
					ManualLogSource log = Plugin.Log;
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(29, 1, ref flag2);
					if (flag2)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FM26Export] Usando handler: ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(availableHandler.GetType().Name);
					}
					log.LogInfo(val);
					_currentHandler = availableHandler;
					flag = true;
					break;
				}
				if (!string.IsNullOrEmpty(errorMessage))
				{
					Plugin.Log.LogInfo((object)errorMessage);
				}
			}
			if (!flag)
			{
				Plugin.Log.LogWarning((object)"[FM26Export] Nenhuma tela suportada para exportação encontrada no momento.");
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(38, 1, ref flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FM26Export] Erro ao iniciar captura: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log2.LogError(val2);
		}
	}
}
