using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.CameraControl;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.NextObservedPlayer;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HeldScopeOnly;

[BepInPlugin("local.heldscopeonly", "Held Scope Only", "1.7.4")]
public sealed class HeldScopeOnlyPlugin : BaseUnityPlugin
{
	public const string Guid = "local.heldscopeonly";

	public const string Version = "1.7.4";

	internal static ManualLogSource Log;

	internal static HeldScopeOnlyPlugin Instance;

	private const string ExternalResolutionTypeName = "DynamicExternalResolution.DynamicExternalResolutionPatches";

	private const string ExternalResolutionRestoreMethodName = "SetResolutionDefault";

	private bool _lastLocalOpticAiming;

	private bool _externalResolutionUnavailableLogged;

	private bool _externalResolutionRestoreMethodLogged;

	private bool _externalResolutionRestorePending;

	private int _externalResolutionRestoreDueFrame = -1;

	private MethodInfo _externalResolutionRestoreMethod;

	private int _externalResolutionAimEnterCount;

	private int _externalResolutionRestoreCount;

	private int _externalResolutionRestoreFailures;

	private int _nextExternalResolutionRestoreFrame;

	private int _nextExternalResolutionResolveFrame;

	private void Awake()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_072b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0738: Expected O, but got Unknown
		//IL_074b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0759: Expected O, but got Unknown
		//IL_076c: Unknown result type (might be due to invalid IL or missing references)
		//IL_077a: Expected O, but got Unknown
		//IL_078e: Unknown result type (might be due to invalid IL or missing references)
		//IL_079c: Expected O, but got Unknown
		//IL_07b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07be: Expected O, but got Unknown
		//IL_07d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e0: Expected O, but got Unknown
		//IL_07f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0802: Expected O, but got Unknown
		//IL_0816: Unknown result type (might be due to invalid IL or missing references)
		//IL_0824: Expected O, but got Unknown
		//IL_0838: Unknown result type (might be due to invalid IL or missing references)
		//IL_0846: Expected O, but got Unknown
		//IL_085a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0868: Expected O, but got Unknown
		//IL_087d: Unknown result type (might be due to invalid IL or missing references)
		//IL_088a: Expected O, but got Unknown
		//IL_089e: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c0: Expected O, but got Unknown
		//IL_08c0: Expected O, but got Unknown
		//IL_08d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e2: Expected O, but got Unknown
		//IL_08f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0904: Expected O, but got Unknown
		//IL_0918: Unknown result type (might be due to invalid IL or missing references)
		//IL_0926: Expected O, but got Unknown
		//IL_093a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Expected O, but got Unknown
		//IL_095c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0971: Unknown result type (might be due to invalid IL or missing references)
		//IL_097e: Expected O, but got Unknown
		//IL_097e: Expected O, but got Unknown
		//IL_0993: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a0: Expected O, but got Unknown
		//IL_09b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c2: Expected O, but got Unknown
		//IL_09d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e4: Expected O, but got Unknown
		//IL_09f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a06: Expected O, but got Unknown
		Log = Logger;
		Instance = this;
		HeldScopeOnlyLog.Initialize();
		try
		{
			Harmony val = new Harmony("local.heldscopeonly");
			MethodInfo methodInfo = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(CameraManager), "SetPlayer", (Type[])(object)new Type[1] { typeof(Player) }, (Type[])null);
			MethodInfo methodInfo2 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(OpticCameraManager), "OnOpticSightEnabled", (Type[])(object)new Type[1] { typeof(OpticSight) }, (Type[])null);
			MethodInfo methodInfo3 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(OpticSight), "LensFade", (Type[])(object)new Type[1] { typeof(bool) }, (Type[])null);
			MethodInfo methodInfo4 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(OpticSight), "OnEnable", (Type[])null, (Type[])null);
			MethodInfo methodInfo5 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(OpticSight), "OnDisable", (Type[])null, (Type[])null);
			MethodInfo methodInfo6 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ScopeMaskRenderer), "DrawOptic", (Type[])(object)new Type[1] { typeof(CommandBuffer) }, (Type[])null);
			MethodInfo methodInfo7 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ScopeMaskRenderer), "DrawCollimators", (Type[])(object)new Type[2]
			{
				typeof(CommandBuffer),
				typeof(Camera)
			}, (Type[])null);
			MethodInfo methodInfo8 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ScopeMaskRenderer), "DrawActiveCollimator", (Type[])(object)new Type[1] { typeof(CommandBuffer) }, (Type[])null);
			MethodInfo methodInfo9 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(OpticRetrice), "UpdateTransform", (Type[])(object)new Type[1] { typeof(OpticSight) }, (Type[])null);
			MethodInfo methodInfo10 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(OpticRetrice), "SetOpticSight", (Type[])(object)new Type[1] { typeof(OpticSight) }, (Type[])null);
			MethodInfo methodInfo11 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(StashItemModel), "Init", (Type[])(object)new Type[1] { typeof(GameObject) }, (Type[])null);
			MethodInfo methodInfo12 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ObservedPlayerFirearmHandsController), "Initialize", (Type[])(object)new Type[2]
			{
				typeof(WeaponPrefab),
				typeof(Weapon)
			}, (Type[])null);
			MethodInfo methodInfo13 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ObservedPlayerFirearmHandsController), "UpdateTacticalComboVisualControllers", (Type[])null, (Type[])null);
			MethodInfo methodInfo14 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ObservedPlayerFirearmHandsController), "UpdateScopes", (Type[])null, (Type[])null);
			MethodInfo methodInfo17 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(Firearms), "UpdateModsVisualControllers", (Type[])null, (Type[])null);
			MethodInfo methodInfo18 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ObservedPlayerView), "SetWeaponRoot", (Type[])(object)new Type[1] { typeof(Transform) }, (Type[])null);
			MethodInfo methodInfo19 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ObservedPlayerView), "UpdateWeaponRoot", (Type[])null, (Type[])null);
			MethodInfo methodInfo20 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(ObservedPlayerView), "Initialization", (Type[])(object)new Type[1] { typeof(ObservedPlayerSpawnMessage) }, (Type[])null);
			MethodInfo methodInfo21 = (MethodInfo)(object)AccessTools.Method((Type)(object)typeof(BundleAnimationBones), "ChangeHandsItem", (Type[])(object)new Type[1] { typeof(WeaponPrefab) }, (Type[])null);
			if (methodInfo == null)
			{
				throw new MissingMethodException(typeof(CameraManager).FullName, "SetPlayer");
			}
			if (methodInfo2 == null)
			{
				throw new MissingMethodException(typeof(OpticCameraManager).FullName, "OnOpticSightEnabled");
			}
			if (methodInfo3 == null)
			{
				throw new MissingMethodException(typeof(OpticSight).FullName, "LensFade");
			}
			if (methodInfo4 == null)
			{
				throw new MissingMethodException(typeof(OpticSight).FullName, "OnEnable");
			}
			if (methodInfo5 == null)
			{
				throw new MissingMethodException(typeof(OpticSight).FullName, "OnDisable");
			}
			if (methodInfo6 == null)
			{
				throw new MissingMethodException(typeof(ScopeMaskRenderer).FullName, "DrawOptic");
			}
			if (methodInfo7 == null)
			{
				throw new MissingMethodException(typeof(ScopeMaskRenderer).FullName, "DrawCollimators");
			}
			if (methodInfo8 == null)
			{
				throw new MissingMethodException(typeof(ScopeMaskRenderer).FullName, "DrawActiveCollimator");
			}
			if (methodInfo9 == null)
			{
				throw new MissingMethodException(typeof(OpticRetrice).FullName, "UpdateTransform");
			}
			if (methodInfo10 == null)
			{
				throw new MissingMethodException(typeof(OpticRetrice).FullName, "SetOpticSight");
			}
			if (methodInfo11 == null)
			{
				throw new MissingMethodException(typeof(StashItemModel).FullName, "Init");
			}
			if (methodInfo12 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerFirearmHandsController).FullName, "Initialize");
			}
			if (methodInfo13 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerFirearmHandsController).FullName, "UpdateTacticalComboVisualControllers");
			}
			if (methodInfo14 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerFirearmHandsController).FullName, "UpdateScopes");
			}
			if (methodInfo17 == null)
			{
				throw new MissingMethodException(typeof(Firearms).FullName, "UpdateModsVisualControllers");
			}
			if (methodInfo18 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerView).FullName, "SetWeaponRoot");
			}
			if (methodInfo19 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerView).FullName, "UpdateWeaponRoot");
			}
			if (methodInfo20 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerView).FullName, "Initialization");
			}
			if (methodInfo21 == null)
			{
				throw new MissingMethodException(typeof(BundleAnimationBones).FullName, "ChangeHandsItem");
			}
			HeldScopeOnlyState.Initialize();
			ScopeMaskRendererPatchState.Initialize();
			val.Patch((MethodBase)(object)methodInfo, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(CameraManagerSetPlayerPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo2, new HarmonyMethod((Type)(object)typeof(OpticCameraManagerEnablePatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo3, new HarmonyMethod((Type)(object)typeof(OpticSightLensFadePatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo4, new HarmonyMethod((Type)(object)typeof(OpticSightOnEnablePatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo5, new HarmonyMethod((Type)(object)typeof(OpticSightOnDisablePatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo6, new HarmonyMethod((Type)(object)typeof(ScopeMaskRendererDrawOpticPatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo7, new HarmonyMethod((Type)(object)typeof(ScopeMaskRendererDrawCollimatorsPatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo8, new HarmonyMethod((Type)(object)typeof(ScopeMaskRendererDrawActiveCollimatorPatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo9, new HarmonyMethod((Type)(object)typeof(OpticRetriceUpdateTransformPatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo10, new HarmonyMethod((Type)(object)typeof(OpticRetriceSetOpticSightPatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo11, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(StashItemModelInitPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo12, new HarmonyMethod((Type)(object)typeof(ObservedPlayerFirearmHandsControllerInitPatch), "Prefix", (Type[])null), new HarmonyMethod((Type)(object)typeof(ObservedPlayerFirearmHandsControllerInitPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo13, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(ObservedPlayerFirearmHandsControllerTacticalUpdatePatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo14, new HarmonyMethod((Type)(object)typeof(ObservedPlayerFirearmHandsControllerUpdateScopesPatch), "Prefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo17, new HarmonyMethod((Type)(object)typeof(FirearmsUpdateModsVisualControllersPatch), "Prefix", (Type[])null), new HarmonyMethod((Type)(object)typeof(FirearmsUpdateModsVisualControllersPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo18, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(ObservedPlayerViewSetWeaponRootPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo19, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(ObservedPlayerViewUpdateWeaponRootPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo20, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(ObservedPlayerViewInitializationPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.Patch((MethodBase)(object)methodInfo21, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(BundleAnimationBonesChangeHandsItemPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			PatchFirearmAimingSetter(val);
			string text = "HeldScopeOnly 1.7.4 loaded; CameraManager.SetPlayer and OpticCameraManager.OnOpticSightEnabled and OpticSight.LensFade and OpticSight.OnEnable/OnDisable and ScopeMaskRenderer draw paths and OpticRetrice.UpdateTransform/SetOpticSight and StashItemModel.Init and observed-player registration and Firearms.UpdateModsVisualControllers patched; remote/AI external lenses remain visible; visual scope-mode selection runs vanilla; local FirearmController.IsAiming setter monitoring enabled.";
			Log.LogInfo((object)text);
			HeldScopeOnlyLog.WriteLine(text);
		}
		catch (Exception ex)
		{
			string text2 = "HeldScopeOnly failed to initialize; plugin inactive: " + ex;
			Log.LogError((object)text2);
			HeldScopeOnlyLog.WriteLine(text2);
		}
	}

	private void PatchFirearmAimingSetter(Harmony harmony)
	{
		try
		{
			Type type = (Type)(object)AccessTools.TypeByName("EFT.Player+FirearmController");
			if (type == null)
			{
				type = (Type)(object)AccessTools.TypeByName("EFT.Player.FirearmController");
			}
			if (type == null)
			{
				throw new MissingMemberException("EFT.Player", "FirearmController");
			}
			MethodInfo methodInfo = (MethodInfo)(object)AccessTools.PropertySetter((Type)(object)type, "IsAiming");
			if (methodInfo == null)
			{
				throw new MissingMethodException(type.FullName, "set_IsAiming");
			}
			harmony.Patch((MethodBase)(object)methodInfo, (HarmonyMethod)null, new HarmonyMethod((Type)(object)typeof(FirearmControllerSetIsAimingPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			string text = "FirearmController.IsAiming setter patched; close-scope restore trigger active.";
			HeldScopeOnlyLog.WriteLine(text);
		}
		catch (Exception ex)
		{
			string text = "FirearmController.IsAiming setter patch installation failed; falling back to frame polling: " + ex;
			Log.LogWarning((object)text);
			HeldScopeOnlyLog.WriteLine(text);
		}
	}

	private void Update()
	{
		bool aiming;
		bool optic;
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out aiming, out optic))
		{
			return;
		}
		bool flag = aiming && optic;
		if (flag)
		{
			MarkLocalOpticAimEntered("procedural-weapon-animation");
			return;
		}
		if (!_lastLocalOpticAiming)
		{
			return;
		}
		ScheduleExternalResolutionRestore("local-optic-aim-ended-update", 1);
	}

	private void LateUpdate()
	{
		if (!_externalResolutionRestorePending || Time.frameCount < _externalResolutionRestoreDueFrame || Time.frameCount < _nextExternalResolutionRestoreFrame)
		{
			return;
		}
		if (HeldScopeOnlyState.TryGetLocalFirearmAiming(out bool aiming) && aiming)
		{
			CancelPendingExternalResolutionRestore("local-firearm-still-aiming");
			return;
		}
		if (RestoreExternalResolution("local-firearm-aiming-ended"))
		{
			_externalResolutionRestorePending = false;
			_externalResolutionRestoreDueFrame = -1;
			_lastLocalOpticAiming = false;
		}
	}

	private void MarkLocalOpticAimEntered(string source)
	{
		if (!_lastLocalOpticAiming)
		{
			_externalResolutionAimEnterCount++;
			HeldScopeOnlyLog.WriteLine("external-resolution-aim-enter=" + _externalResolutionAimEnterCount + " source=\"" + source + "\"");
		}
		_lastLocalOpticAiming = true;
	}

	private void ScheduleExternalResolutionRestore(string reason, int delayFrames)
	{
		int num = Time.frameCount + Math.Max(0, delayFrames);
		if (_externalResolutionRestorePending)
		{
			_externalResolutionRestoreDueFrame = Math.Min(_externalResolutionRestoreDueFrame, num);
			return;
		}
		_externalResolutionRestorePending = true;
		_externalResolutionRestoreDueFrame = num;
		HeldScopeOnlyLog.WriteLine("restore-deferred-frame=" + num + " reason=\"" + reason + "\"");
	}

	private void CancelPendingExternalResolutionRestore(string reason)
	{
		if (!_externalResolutionRestorePending)
		{
			return;
		}
		_externalResolutionRestorePending = false;
		_externalResolutionRestoreDueFrame = -1;
		HeldScopeOnlyLog.WriteLine("restore-cancelled reason=\"" + reason + "\"");
	}

	private bool RestoreExternalResolution(string reason)
	{
		HeldScopeOnlyLog.WriteLine("external-resolution-restore-trigger=1 reason=\"" + reason + "\" failures=" + _externalResolutionRestoreFailures);
		if (!ResolveExternalResolutionRestoreMethod())
		{
			_nextExternalResolutionRestoreFrame = Time.frameCount + 60;
			HeldScopeOnlyLog.WriteLine("restore-failed reason=\"" + reason + "\" cause=method-unavailable retryFrame=" + _nextExternalResolutionRestoreFrame);
			return false;
		}
		try
		{
			_externalResolutionRestoreMethod.Invoke(null, null);
			_externalResolutionRestoreCount++;
			HeldScopeOnlyLog.WriteLine("restore-applied external-resolution-restored=" + _externalResolutionRestoreCount + " reason=\"" + reason + "\"");
			return true;
		}
		catch (Exception ex)
		{
			_externalResolutionRestoreFailures++;
			_nextExternalResolutionRestoreFrame = Time.frameCount + 30;
			HeldScopeOnlyLog.WriteLine("external-resolution-restore-failed=" + _externalResolutionRestoreFailures + " reason=\"" + reason + "\" retryFrame=" + _nextExternalResolutionRestoreFrame + " exception=" + ex);
			return false;
		}
	}

	private bool ResolveExternalResolutionRestoreMethod()
	{
		if (_externalResolutionRestoreMethod != null)
		{
			return true;
		}
		if (Time.frameCount < _nextExternalResolutionResolveFrame)
		{
			return false;
		}
		try
		{
			Type type = (Type)(object)AccessTools.TypeByName(ExternalResolutionTypeName);
			if (type != null)
			{
				_externalResolutionRestoreMethod = (MethodInfo)(object)AccessTools.Method((Type)(object)type, ExternalResolutionRestoreMethodName, Type.EmptyTypes, (Type[])null);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("external-resolution-resolve-failed: " + ex);
		}
		if (_externalResolutionRestoreMethod != null)
		{
			if (!_externalResolutionRestoreMethodLogged)
			{
				_externalResolutionRestoreMethodLogged = true;
				HeldScopeOnlyLog.WriteLine("restore-method-found=" + ExternalResolutionTypeName + "." + ExternalResolutionRestoreMethodName);
			}
			return true;
		}
		_nextExternalResolutionResolveFrame = Time.frameCount + 60;
		if (!_externalResolutionUnavailableLogged)
		{
			_externalResolutionUnavailableLogged = true;
			string text = "external-resolution-not-installed; restore hook inactive";
			Log.LogWarning((object)text);
			HeldScopeOnlyLog.WriteLine(text);
		}
		return false;
	}

	private void HandleFirearmAimingChanged(EFT.Player.FirearmController controller, bool value)
	{
		if ((Object)(object)controller == (Object)null)
		{
			return;
		}
		Player player = controller._player;
		if (!HeldScopeOnlyState.IsLocalPlayer(player))
		{
			return;
		}
		bool aiming;
		bool optic;
		bool flag = HeldScopeOnlyState.TryGetLocalOpticAiming(out aiming, out optic);
		string text = "setter-aiming=" + (value ? "true" : "false") + " is-local=true firearm-aiming=" + (controller.IsAiming ? "true" : "false") + " pwa-read=" + (flag ? "true" : "false") + " pwa-aiming=" + (aiming ? "true" : "false") + " optic=" + (optic ? "true" : "false") + " frame=" + Time.frameCount;
		HeldScopeOnlyLog.WriteLine(text);
		if (value)
		{
			CancelPendingExternalResolutionRestore("local-firearm-aiming-resumed");
			if (flag && aiming && optic)
			{
				MarkLocalOpticAimEntered("FirearmController.IsAiming");
			}
			return;
		}
		if (_lastLocalOpticAiming)
		{
			ScheduleExternalResolutionRestore("FirearmController.IsAiming=false", 1);
		}
	}

	internal static void NotifyFirearmAimingChanged(EFT.Player.FirearmController controller, bool value)
	{
		HeldScopeOnlyPlugin heldScopeOnlyPlugin = Instance;
		if (heldScopeOnlyPlugin == null)
		{
			return;
		}
		try
		{
			heldScopeOnlyPlugin.HandleFirearmAimingChanged(controller, value);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("FirearmController.IsAiming postfix exception; continuing vanilla: " + ex);
		}
	}

	private void OnDestroy()
	{
		if (_lastLocalOpticAiming)
		{
			_lastLocalOpticAiming = false;
			RestoreExternalResolution("plugin-destroyed");
		}
		if (object.ReferenceEquals(Instance, this))
		{
			Instance = null;
		}
		HeldScopeOnlyLog.WriteLine("plugin destroyed; accepted=" + HeldScopeOnlyState.Accepted + " rejected=" + HeldScopeOnlyState.Rejected + " lensBlocked=" + HeldScopeOnlyState.LensBlocked + " stashBlocked=" + HeldScopeOnlyState.StashBlocked + " stashEnableBlocked=" + HeldScopeOnlyState.StashEnableBlocked + " stashDisableBlocked=" + HeldScopeOnlyState.StashDisableBlocked + " drawOpticBlocked=" + HeldScopeOnlyState.DrawOpticBlocked + " drawCollimatorBlocked=" + HeldScopeOnlyState.DrawCollimatorBlocked + " drawActiveCollimatorBlocked=" + HeldScopeOnlyState.DrawActiveCollimatorBlocked + " reticleChecked=" + HeldScopeOnlyState.ReticleChecked + " reticleBlocked=" + HeldScopeOnlyState.ReticleBlocked + " remoteRegistered=" + HeldScopeOnlyState.RemoteRegistered + " remoteOpticsFound=" + HeldScopeOnlyState.RemoteOpticsFound + " remoteLensesShown=" + HeldScopeOnlyState.RemoteLensesShown + " remoteViewFallbacks=" + HeldScopeOnlyState.RemoteViewFallbacks);
	}
}
