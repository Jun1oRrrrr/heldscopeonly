using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.CameraControl;
using EFT.Hideout;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.NextObservedPlayer;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HeldScopeOnly;

[BepInPlugin("local.heldscopeonly", "Held Scope Only", "1.9.2")]
public sealed class HeldScopeOnlyPlugin : BaseUnityPlugin
{
	public const string Guid = "local.heldscopeonly";

	public const string Version = "1.9.2";

	internal static ManualLogSource Log;

	internal static HeldScopeOnlyPlugin Instance;

	private const string ExternalResolutionTypeName = "DynamicExternalResolution.DynamicExternalResolutionPatches";

	private const string ExternalResolutionRestoreMethodName = "SetResolutionDefault";

	private bool _lastLocalOpticAiming;

	private bool _externalResolutionUnavailableLogged;

	private bool _externalResolutionRestoreMethodLogged;

	private bool _externalResolutionUnavailable;

	private bool _externalResolutionRestorePending;

	private int _externalResolutionRestoreDueFrame = -1;

	private MethodInfo _externalResolutionRestoreMethod;

	private int _externalResolutionAimEnterCount;

	private int _externalResolutionRestoreCount;

	private int _externalResolutionRestoreFailures;

	private void OnEnable()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(HandleCameraPreCull));
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(HandleCameraPreCull));
	}

	private void OnDisable()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(HandleCameraPreCull));
	}

	private void HandleCameraPreCull(Camera camera)
	{
		try
		{
			OpticRenderPolicy.EnforceBeforeCameraRender(camera, "camera pre-cull strict non-local lens enforcement");
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("camera pre-cull optic enforcement exception: " + ex);
		}
	}

	private void Awake()
	{
		Log = Logger;
		Instance = this;
		HeldScopeOnlyLog.Initialize();
		try
		{
			Harmony harmony = new Harmony("local.heldscopeonly");
			MethodInfo methodInfo = AccessTools.Method(typeof(CameraManager), "SetPlayer", new Type[1] { typeof(Player) });
			MethodInfo methodInfo2 = AccessTools.Method(typeof(OpticCameraManager), "OnOpticSightEnabled", new Type[1] { typeof(OpticSight) });
			MethodInfo methodInfo3 = AccessTools.Method(typeof(OpticSight), "LensFade", new Type[1] { typeof(bool) });
			MethodInfo methodInfo4 = AccessTools.Method(typeof(OpticSight), "OnEnable");
			MethodInfo methodInfo5 = AccessTools.Method(typeof(OpticSight), "OnDisable");
			MethodInfo methodInfo6 = AccessTools.Method(typeof(ScopeMaskRenderer), "DrawOptic", new Type[1] { typeof(CommandBuffer) });
			MethodInfo methodInfo7 = AccessTools.Method(typeof(ScopeMaskRenderer), "DrawCollimators", new Type[2]
			{
				typeof(CommandBuffer),
				typeof(Camera)
			});
			MethodInfo methodInfo8 = AccessTools.Method(typeof(ScopeMaskRenderer), "DrawActiveCollimator", new Type[1] { typeof(CommandBuffer) });
			MethodInfo methodInfo9 = AccessTools.Method(typeof(OpticRetrice), "UpdateTransform", new Type[1] { typeof(OpticSight) });
			MethodInfo methodInfo10 = AccessTools.Method(typeof(OpticRetrice), "SetOpticSight", new Type[1] { typeof(OpticSight) });
			MethodInfo methodInfo11 = AccessTools.Method(typeof(StashItemModel), "Init", new Type[1] { typeof(GameObject) });
			MethodInfo methodInfo12 = AccessTools.Method(typeof(ObservedPlayerFirearmHandsController), "Initialize", new Type[2]
			{
				typeof(WeaponPrefab),
				typeof(Weapon)
			});
			MethodInfo methodInfo13 = AccessTools.Method(typeof(ObservedPlayerFirearmHandsController), "UpdateTacticalComboVisualControllers");
			MethodInfo methodInfo14 = AccessTools.Method(typeof(ObservedPlayerFirearmHandsController), "UpdateScopes");
			MethodInfo methodInfo15 = AccessTools.Method(typeof(Firearms), "UpdateModsVisualControllers");
			MethodInfo methodInfo16 = AccessTools.Method(typeof(ObservedPlayerView), "SetWeaponRoot", new Type[1] { typeof(Transform) });
			MethodInfo methodInfo17 = AccessTools.Method(typeof(ObservedPlayerView), "UpdateWeaponRoot");
			MethodInfo methodInfo18 = AccessTools.Method(typeof(ObservedPlayerView), "Initialization", new Type[1] { typeof(ObservedPlayerSpawnMessage) });
			MethodInfo methodInfo19 = AccessTools.Method(typeof(BundleAnimationBones), "ChangeHandsItem", new Type[1] { typeof(WeaponPrefab) });
			MethodInfo methodInfo20 = AccessTools.Method(typeof(SightModVisualControllers), "UpdateSightMode", new Type[1] { typeof(bool) });
			MethodInfo methodInfo21 = AccessTools.Method(typeof(ScopePrefabCache), "SetMode", new Type[1] { typeof(int) });
			MethodInfo methodInfo22 = AccessTools.Method(typeof(LootItem), "Init", new Type[7]
			{
				typeof(Item),
				typeof(string),
				typeof(GameWorld),
				typeof(bool),
				typeof(MongoID[]),
				typeof(string),
				typeof(bool)
			});
			MethodInfo methodInfo23 = AccessTools.Method(typeof(LootItem), "SetItemAndRigidbody", new Type[2]
			{
				typeof(Item),
				typeof(Rigidbody)
			});
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
			if (methodInfo15 == null)
			{
				throw new MissingMethodException(typeof(Firearms).FullName, "UpdateModsVisualControllers");
			}
			if (methodInfo16 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerView).FullName, "SetWeaponRoot");
			}
			if (methodInfo17 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerView).FullName, "UpdateWeaponRoot");
			}
			if (methodInfo18 == null)
			{
				throw new MissingMethodException(typeof(ObservedPlayerView).FullName, "Initialization");
			}
			if (methodInfo19 == null)
			{
				throw new MissingMethodException(typeof(BundleAnimationBones).FullName, "ChangeHandsItem");
			}
			if (methodInfo20 == null)
			{
				throw new MissingMethodException(typeof(SightModVisualControllers).FullName, "UpdateSightMode");
			}
			if (methodInfo21 == null)
			{
				throw new MissingMethodException(typeof(ScopePrefabCache).FullName, "SetMode");
			}
			if (methodInfo22 == null)
			{
				throw new MissingMethodException(typeof(LootItem).FullName, "Init");
			}
			if (methodInfo23 == null)
			{
				throw new MissingMethodException(typeof(LootItem).FullName, "SetItemAndRigidbody");
			}
			HeldScopeOnlyState.Initialize();
			ScopeMaskRendererPatchState.Initialize();
			harmony.Patch(methodInfo, null, new HarmonyMethod(typeof(CameraManagerSetPlayerPatch), "Postfix"));
			harmony.Patch(methodInfo2, new HarmonyMethod(typeof(OpticCameraManagerEnablePatch), "Prefix"));
			harmony.Patch(methodInfo3, null, new HarmonyMethod(typeof(OpticSightLensFadePatch), "Postfix"));
			harmony.Patch(methodInfo4, null, new HarmonyMethod(typeof(OpticSightOnEnablePatch), "Postfix"));
			harmony.Patch(methodInfo5, null, new HarmonyMethod(typeof(OpticSightOnDisablePatch), "Postfix"));
			harmony.Patch(methodInfo6, new HarmonyMethod(typeof(ScopeMaskRendererDrawOpticPatch), "Prefix"));
			harmony.Patch(methodInfo7, new HarmonyMethod(typeof(ScopeMaskRendererDrawCollimatorsPatch), "Prefix"));
			harmony.Patch(methodInfo8, new HarmonyMethod(typeof(ScopeMaskRendererDrawActiveCollimatorPatch), "Prefix"));
			harmony.Patch(methodInfo9, new HarmonyMethod(typeof(OpticRetriceUpdateTransformPatch), "Prefix"));
			harmony.Patch(methodInfo10, new HarmonyMethod(typeof(OpticRetriceSetOpticSightPatch), "Prefix"));
			harmony.Patch(methodInfo11, null, new HarmonyMethod(typeof(StashItemModelInitPatch), "Postfix"));
			harmony.Patch(methodInfo12, new HarmonyMethod(typeof(ObservedPlayerFirearmHandsControllerInitPatch), "Prefix"), new HarmonyMethod(typeof(ObservedPlayerFirearmHandsControllerInitPatch), "Postfix"));
			harmony.Patch(methodInfo13, null, new HarmonyMethod(typeof(ObservedPlayerFirearmHandsControllerTacticalUpdatePatch), "Postfix"));
			harmony.Patch(methodInfo14, null, new HarmonyMethod(typeof(ObservedPlayerFirearmHandsControllerUpdateScopesPatch), "Postfix"));
			harmony.Patch(methodInfo15, new HarmonyMethod(typeof(FirearmsUpdateModsVisualControllersPatch), "Prefix"), new HarmonyMethod(typeof(FirearmsUpdateModsVisualControllersPatch), "Postfix"));
			harmony.Patch(methodInfo16, null, new HarmonyMethod(typeof(ObservedPlayerViewSetWeaponRootPatch), "Postfix"));
			harmony.Patch(methodInfo17, null, new HarmonyMethod(typeof(ObservedPlayerViewUpdateWeaponRootPatch), "Postfix"));
			harmony.Patch(methodInfo18, null, new HarmonyMethod(typeof(ObservedPlayerViewInitializationPatch), "Postfix"));
			harmony.Patch(methodInfo19, null, new HarmonyMethod(typeof(BundleAnimationBonesChangeHandsItemPatch), "Postfix"));
			harmony.Patch(methodInfo20, null, new HarmonyMethod(typeof(SightModVisualControllersUpdateSightModePatch), "Postfix"));
			harmony.Patch(methodInfo21, null, new HarmonyMethod(typeof(ScopePrefabCacheSetModePatch), "Postfix"));
			harmony.Patch(methodInfo22, null, new HarmonyMethod(typeof(LootItemInitPatch), "Postfix"));
			harmony.Patch(methodInfo23, null, new HarmonyMethod(typeof(LootItemSetItemAndRigidbodyPatch), "Postfix"));
			PatchFirearmAimingSetter(harmony);
			string text = "HeldScopeOnly 1.9.2 loaded; only the local currently held optic may show its interior while aiming; every non-local/AI/Fika/dropped/hideout scope interior is blocked unconditionally with cloned opaque neutral textures and _SwitchToSight=0.97; every StashItemModel registers unconditionally, late-bound hideout lenses retry in a bounded batch, and hideout suppression bypasses the material-state short circuit.";
			Log.LogInfo(text);
			HeldScopeOnlyLog.WriteLine(text);
		}
		catch (Exception ex)
		{
			string text2 = "HeldScopeOnly failed to initialize; plugin inactive: " + ex;
			Log.LogError(text2);
			HeldScopeOnlyLog.WriteLine(text2);
		}
	}

	private void PatchFirearmAimingSetter(Harmony harmony)
	{
		try
		{
			Type type = AccessTools.TypeByName("EFT.Player+FirearmController");
			if (type == null)
			{
				type = AccessTools.TypeByName("EFT.Player.FirearmController");
			}
			if (type == null)
			{
				throw new MissingMemberException("EFT.Player", "FirearmController");
			}
			MethodInfo methodInfo = AccessTools.PropertySetter(type, "IsAiming");
			if (methodInfo == null)
			{
				throw new MissingMethodException(type.FullName, "set_IsAiming");
			}
			harmony.Patch(methodInfo, null, new HarmonyMethod(typeof(FirearmControllerSetIsAimingPatch), "Postfix"));
			string message = "FirearmController.IsAiming setter patched; close-scope restore trigger active.";
			HeldScopeOnlyLog.WriteLine(message);
		}
		catch (Exception ex)
		{
			string text = "FirearmController.IsAiming setter patch installation failed; falling back to frame polling: " + ex;
			Log.LogWarning(text);
			HeldScopeOnlyLog.WriteLine(text);
		}
	}

	private void Update()
	{
		OpticRenderPolicy.ProcessPendingStashOptics();
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var optic))
		{
			return;
		}
		if (aiming & optic)
		{
			if (!_lastLocalOpticAiming)
			{
				OpticRenderPolicy.RestoreCurrentLocalOptic("procedural-weapon-animation aim entered");
			}
			MarkLocalOpticAimEntered("procedural-weapon-animation");
		}
		else if (_lastLocalOpticAiming)
		{
			OpticRenderPolicy.HideCurrentLocalOptic("procedural-weapon-animation aim ended");
			_lastLocalOpticAiming = false;
			ScheduleExternalResolutionRestore("local-optic-aim-ended-update", 1);
		}
	}

	private void LateUpdate()
	{
		if (_externalResolutionRestorePending && Time.frameCount >= _externalResolutionRestoreDueFrame)
		{
			if (HeldScopeOnlyState.TryGetLocalFirearmAiming(out var aiming) & aiming)
			{
				CancelPendingExternalResolutionRestore("local-firearm-still-aiming");
			}
			else if (RestoreExternalResolution("local-firearm-aiming-ended"))
			{
				_externalResolutionRestorePending = false;
				_externalResolutionRestoreDueFrame = -1;
				_lastLocalOpticAiming = false;
			}
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
		if (!_externalResolutionUnavailable)
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
	}

	private void CancelPendingExternalResolutionRestore(string reason)
	{
		if (_externalResolutionRestorePending)
		{
			_externalResolutionRestorePending = false;
			_externalResolutionRestoreDueFrame = -1;
			HeldScopeOnlyLog.WriteLine("restore-cancelled reason=\"" + reason + "\"");
		}
	}

	private bool RestoreExternalResolution(string reason)
	{
		if (_externalResolutionUnavailable)
		{
			return true;
		}
		if (!ResolveExternalResolutionRestoreMethod())
		{
			return true;
		}
		HeldScopeOnlyLog.WriteLine("external-resolution-restore-trigger=1 reason=\"" + reason + "\" failures=" + _externalResolutionRestoreFailures);
		try
		{
			_externalResolutionRestoreMethod.Invoke(null, null);
			_externalResolutionRestoreCount++;
			_externalResolutionRestoreFailures = 0;
			HeldScopeOnlyLog.WriteLine("restore-applied external-resolution-restored=" + _externalResolutionRestoreCount + " reason=\"" + reason + "\"");
			return true;
		}
		catch (Exception ex)
		{
			_externalResolutionRestoreFailures++;
			if (_externalResolutionRestoreFailures >= 3)
			{
				_externalResolutionUnavailable = true;
				HeldScopeOnlyLog.WriteLine("external-resolution-disabled-after-failures=" + _externalResolutionRestoreFailures + " reason=\"" + reason + "\" exception=" + ex);
				return true;
			}
			HeldScopeOnlyLog.WriteLine("external-resolution-restore-failed=" + _externalResolutionRestoreFailures + " reason=\"" + reason + "\" exception=" + ex);
			return false;
		}
	}

	private bool ResolveExternalResolutionRestoreMethod()
	{
		if (_externalResolutionRestoreMethod != null)
		{
			return true;
		}
		if (_externalResolutionUnavailable)
		{
			return false;
		}
		try
		{
			Type type = AccessTools.TypeByName("DynamicExternalResolution.DynamicExternalResolutionPatches");
			if (type != null)
			{
				_externalResolutionRestoreMethod = AccessTools.Method(type, "SetResolutionDefault", Type.EmptyTypes);
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
				HeldScopeOnlyLog.WriteLine("restore-method-found=DynamicExternalResolution.DynamicExternalResolutionPatches.SetResolutionDefault");
			}
			return true;
		}
		_externalResolutionUnavailable = true;
		if (!_externalResolutionUnavailableLogged)
		{
			_externalResolutionUnavailableLogged = true;
			string text = "external-resolution-not-installed; restore hook permanently inactive after one probe";
			Log.LogWarning(text);
			HeldScopeOnlyLog.WriteLine(text);
		}
		return false;
	}

	private void HandleFirearmAimingChanged(Player.FirearmController controller, bool value)
	{
		if (controller == null)
		{
			return;
		}
		Player player = controller._player;
		if (!HeldScopeOnlyState.IsLocalPlayer(player))
		{
			return;
		}
		bool flag = HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var optic);
		string message = "setter-aiming=" + (value ? "true" : "false") + " is-local=true firearm-aiming=" + (controller.IsAiming ? "true" : "false") + " pwa-read=" + (flag ? "true" : "false") + " pwa-aiming=" + (aiming ? "true" : "false") + " optic=" + (optic ? "true" : "false") + " frame=" + Time.frameCount;
		HeldScopeOnlyLog.WriteLine(message);
		if (value)
		{
			CancelPendingExternalResolutionRestore("local-firearm-aiming-resumed");
			if (flag & aiming & optic)
			{
				OpticRenderPolicy.RestoreCurrentLocalOptic("FirearmController.IsAiming=true");
				MarkLocalOpticAimEntered("FirearmController.IsAiming");
			}
		}
		else
		{
			OpticRenderPolicy.HideCurrentLocalOptic("FirearmController.IsAiming=false");
			if (_lastLocalOpticAiming)
			{
				_lastLocalOpticAiming = false;
				ScheduleExternalResolutionRestore("FirearmController.IsAiming=false", 1);
			}
		}
	}

	internal static void NotifyFirearmAimingChanged(Player.FirearmController controller, bool value)
	{
		HeldScopeOnlyPlugin instance = Instance;
		if (instance == null)
		{
			return;
		}
		try
		{
			instance.HandleFirearmAimingChanged(controller, value);
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
		if ((object)Instance == this)
		{
			Instance = null;
		}
		HeldScopeOnlyLog.WriteLine("plugin destroyed; accepted=" + HeldScopeOnlyState.Accepted + " rejected=" + HeldScopeOnlyState.Rejected + " lensBlocked=" + HeldScopeOnlyState.LensBlocked + " stashBlocked=" + HeldScopeOnlyState.StashBlocked + " stashEnableBlocked=" + HeldScopeOnlyState.StashEnableBlocked + " stashDisableBlocked=" + HeldScopeOnlyState.StashDisableBlocked + " stashModelsRegistered=" + HeldScopeOnlyState.StashModelsRegistered + " stashOpticsQueued=" + HeldScopeOnlyState.StashOpticsQueued + " stashOpticsRetried=" + HeldScopeOnlyState.StashOpticsRetried + " stashOpticsEnforced=" + HeldScopeOnlyState.StashOpticsEnforced + " stashOpticsUnresolved=" + HeldScopeOnlyState.StashOpticsUnresolved + " drawOpticBlocked=" + HeldScopeOnlyState.DrawOpticBlocked + " drawCollimatorBlocked=" + HeldScopeOnlyState.DrawCollimatorBlocked + " drawActiveCollimatorBlocked=" + HeldScopeOnlyState.DrawActiveCollimatorBlocked + " reticleChecked=" + HeldScopeOnlyState.ReticleChecked + " reticleBlocked=" + HeldScopeOnlyState.ReticleBlocked + " remoteRegistered=" + HeldScopeOnlyState.RemoteRegistered + " remoteOpticsFound=" + HeldScopeOnlyState.RemoteOpticsFound + " remoteLensesShown=" + HeldScopeOnlyState.RemoteLensesShown + " remoteViewFallbacks=" + HeldScopeOnlyState.RemoteViewFallbacks + " policyChecks=" + OpticRenderPolicy.PolicyChecks + " policyAllowed=" + OpticRenderPolicy.PolicyAllowed + " policyBlocked=" + OpticRenderPolicy.PolicyBlocked + " lensesSuppressed=" + OpticRenderPolicy.LensesSuppressed + " lensesRestored=" + OpticRenderPolicy.LensesRestored + " hierarchyScans=" + OpticRenderPolicy.HierarchyScans + " opticsVisited=" + OpticRenderPolicy.OpticsVisited + " frameEnforcementPasses=" + OpticRenderPolicy.FrameEnforcementPasses + " frameEnforcedOptics=" + OpticRenderPolicy.FrameEnforcedOptics + " camTexRenderersTracked=" + OpticRenderPolicy.CamTexRenderersTracked + " camTexRenderersSuppressed=" + OpticRenderPolicy.CamTexRenderersSuppressed + " camTexRenderersAllowed=" + OpticRenderPolicy.CamTexRenderersAllowed + " interiorTexturePropertiesBlocked=" + OpticRenderPolicy.InteriorTexturePropertiesBlocked);
		OpticRenderPolicy.ReleaseResources();
	}
}
