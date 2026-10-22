using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Animations;
using EFT.CameraControl;
using EFT.Hideout;
using EFT.NextObservedPlayer;
using HarmonyLib;
using UnityEngine;

namespace HeldScopeOnly;

internal static class HeldScopeOnlyState
{
	private static Player _localPlayer;

	private static PropertyInfo _gamePlayerOwnerMyPlayer;

	private static bool _gamePlayerOwnerMyPlayerResolved;

	private static FieldInfo _scopeAimTransformsField;

	private static PropertyInfo _aimIndexProperty;

	private static PropertyInfo _isAimingProperty;

	private static PropertyInfo _sightNBoneIsOpticProperty;

	private static FieldInfo _sightNBoneScopePrefabCacheField;

	private static readonly List<Transform> _remoteWeaponRoots = new List<Transform>();

	private static readonly HashSet<int> _remoteOpticsSeen = new HashSet<int>();

	private static readonly HashSet<int> _remoteLensesShownSeen = new HashSet<int>();

	private static readonly HashSet<int> _remoteViewFallbackSeen = new HashSet<int>();

	private static readonly List<Transform> _stashRoots = new List<Transform>();

	private static readonly List<int> _stashRootInstanceIds = new List<int>();

	private static readonly HashSet<int> _stashRootIds = new HashSet<int>();

	private static bool _currentSightsCacheValid;

	private static int _currentSightsCacheFrame = -1;

	private static Player _currentSightsCachePlayer;

	private static bool _currentSightsCacheResult;

	private static int _localOpticAimReadErrors;

	private static OpticSight _currentSightsCacheOptic;

	private static CollimatorSight _currentSightsCacheCollimator;

	private static string _currentSightsCacheReason;

	internal static int Accepted;

	internal static int Rejected;

	internal static int LensBlocked;

	internal static int LensChecked;

	internal static int StashBlocked;

	internal static int StashEnableBlocked;

	internal static int StashDisableBlocked;

	internal static int StashModelsRegistered;

	internal static int StashOpticsQueued;

	internal static int StashOpticsRetried;

	internal static int StashOpticsEnforced;

	internal static int StashOpticsUnresolved;

	internal static int DrawOpticBlocked;

	internal static int DrawCollimatorAllowed;

	internal static int DrawCollimatorBlocked;

	internal static int DrawActiveCollimatorBlocked;

	internal static int ReticleChecked;

	internal static int ReticleBlocked;

	internal static int RemoteRegistered;

	internal static int RemoteOpticsFound;

	internal static int RemoteLensesShown;

	internal static int RemoteViewFallbacks;

	internal static void Initialize()
	{
		Type type = AccessTools.TypeByName("EFT.Animations.ProceduralWeaponAnimation");
		if (type == null)
		{
			throw new MissingMethodException("EFT.Animations.ProceduralWeaponAnimation", "type");
		}
		_scopeAimTransformsField = AccessTools.Field(type, "ScopeAimTransforms");
		_aimIndexProperty = AccessTools.Property(type, "AimIndex");
		_isAimingProperty = AccessTools.Property(type, "IsAiming");
		Type nestedType = type.GetNestedType("SightNBone", BindingFlags.Public | BindingFlags.NonPublic);
		if (_scopeAimTransformsField == null || _aimIndexProperty == null || _isAimingProperty == null || nestedType == null)
		{
			throw new MissingMemberException(type.FullName, "ScopeAimTransforms/AimIndex/IsAiming/SightNBone");
		}
		_sightNBoneIsOpticProperty = AccessTools.Property(nestedType, "IsOptic");
		_sightNBoneScopePrefabCacheField = AccessTools.Field(nestedType, "ScopePrefabCache");
		if (_sightNBoneIsOpticProperty == null || _sightNBoneScopePrefabCacheField == null)
		{
			throw new MissingMemberException(nestedType.FullName, "IsOptic/ScopePrefabCache");
		}
	}

	internal static void RememberLocalPlayer(Player player)
	{
		if (player == null)
		{
			return;
		}
		Player gameLocalPlayer = GetGameLocalPlayer();
		if (gameLocalPlayer != null)
		{
			if ((object)player != gameLocalPlayer)
			{
				HeldScopeOnlyLog.WriteLine("ignored non-local CameraManager.SetPlayer: " + DescribePlayer(player) + " local=" + DescribePlayer(gameLocalPlayer));
				return;
			}
		}
		else if (!player.IsYourPlayer)
		{
			HeldScopeOnlyLog.WriteLine("ignored SetPlayer without local marker: " + DescribePlayer(player));
			return;
		}
		if ((object)_localPlayer != player)
		{
			_localPlayer = player;
			InvalidateCurrentSightsCache();
			string text = "cached local player: " + DescribePlayer(player);
			HeldScopeOnlyPlugin.Log.LogInfo(text);
			HeldScopeOnlyLog.WriteLine(text);
			OpticRenderPolicy.ScanHierarchy(((Component)player).transform, "local player registration");
		}
	}

	internal static void RegisterRemoteWeaponRoot(Transform root)
	{
		if (root == null)
		{
			return;
		}
		for (int num = _remoteWeaponRoots.Count - 1; num >= 0; num--)
		{
			Transform transform = _remoteWeaponRoots[num];
			if (transform == null)
			{
				_remoteWeaponRoots.RemoveAt(num);
			}
			else if ((object)transform == root)
			{
				return;
			}
		}
		_remoteWeaponRoots.Add(root);
		RemoteRegistered++;
		if (RemoteRegistered <= 40 || RemoteRegistered % 20 == 0)
		{
			HeldScopeOnlyLog.WriteLine("fika-remote-register=" + RemoteRegistered + " root=" + DescribeTransform(root));
		}
		OpticRenderPolicy.ScanHierarchy(root, "remote weapon root registration");
	}

	internal static void ScanLocalPlayerHierarchy(string reason)
	{
		Player player = ResolveLocalPlayer();
		if (!(player == null))
		{
			OpticRenderPolicy.ScanHierarchy(((Component)player).transform, reason);
		}
	}

	internal static void RegisterRemoteObservedPlayerView(ObservedPlayerView view, string reason)
	{
		if (view == null)
		{
			return;
		}
		try
		{
			RegisterRemoteWeaponRoot(view.transform);
			if (view.WeaponRoot != null)
			{
				RegisterRemoteWeaponRoot(view.WeaponRoot.Original);
			}
			OpticRenderPolicy.ScanHierarchy(view.transform, "remote ObservedPlayerView registration: " + reason);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("remote ObservedPlayerView registration exception: " + ex);
		}
	}

	private static void RecordRemoteViewFallback(ObservedPlayerView view, Transform evidence)
	{
		if (view == null)
		{
			return;
		}
		int instanceID = view.GetInstanceID();
		if (_remoteViewFallbackSeen.Add(instanceID))
		{
			RemoteViewFallbacks++;
			if (RemoteViewFallbacks <= 40 || RemoteViewFallbacks % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("fika-remote-view-fallback=" + RemoteViewFallbacks + " view=" + DescribeComponent(view) + " evidence=" + DescribeTransform(evidence));
			}
		}
	}

	internal static bool IsRemoteScopeTransform(Transform transform)
	{
		if (transform == null)
		{
			return false;
		}
		for (int num = _remoteWeaponRoots.Count - 1; num >= 0; num--)
		{
			Transform transform2 = _remoteWeaponRoots[num];
			if (transform2 == null)
			{
				_remoteWeaponRoots.RemoveAt(num);
			}
			else if ((object)transform2 == transform || transform.IsChildOf(transform2))
			{
				return true;
			}
		}
		ObservedPlayerView componentInParent = transform.GetComponentInParent<ObservedPlayerView>(includeInactive: true);
		if (componentInParent != null)
		{
			RegisterRemoteObservedPlayerView(componentInParent, "remote scope hierarchy fallback");
			RecordRemoteViewFallback(componentInParent, transform);
			return true;
		}
		return false;
	}

	internal static bool IsRemoteScopeComponent(Component component)
	{
		return component != null && IsRemoteScopeTransform(component.transform);
	}

	internal static void RegisterStashModel(StashItemModel stashModel, GameObject prefab)
	{
		if (!(stashModel == null))
		{
			Transform transform = stashModel.transform;
			bool flag = false;
			int instanceID = transform.GetInstanceID();
			if (_stashRootIds.Add(instanceID))
			{
				_stashRoots.Add(transform);
				_stashRootInstanceIds.Add(instanceID);
				flag = true;
			}
			StashModelsRegistered++;
			HashSet<int> seenOptics = new HashSet<int>();
			int optics = 0;
			int boundLenses = 0;
			int unboundLenses = 0;
			CollectStashOptics(transform, seenOptics, ref optics, ref boundLenses, ref unboundLenses);
			if (prefab != null)
			{
				CollectStashOptics(prefab.transform, seenOptics, ref optics, ref boundLenses, ref unboundLenses);
			}
			if (StashModelsRegistered <= 80 || StashModelsRegistered % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("stash-model-register=" + StashModelsRegistered + " type=" + stashModel.GetType().Name + " root=" + DescribeTransform(transform) + " new-root=" + (flag ? "true" : "false") + " optics=" + optics + " lens-bound=" + boundLenses + " lens-unbound=" + unboundLenses + " prefab=" + DescribeTransform((prefab != null) ? prefab.transform : null));
			}
		}
	}

	internal static void RegisterStashOptic(OpticSight opticSight, string reason)
	{
		if (!(opticSight == null))
		{
			StashOpticsQueued++;
			if (StashOpticsQueued <= 80 || StashOpticsQueued % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("stash-optic-pending=" + StashOpticsQueued + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight) + " lens=" + ((opticSight.LensRenderer == null) ? "<unbound>" : opticSight.LensRenderer.name));
			}
		}
	}

	internal static void RecordStashOpticRetry(OpticSight opticSight, int attempt, int ageFrames, string reason)
	{
		StashOpticsRetried++;
		if (StashOpticsRetried <= 40 || StashOpticsRetried % 50 == 0)
		{
			HeldScopeOnlyLog.WriteLine("stash-optic-retry=" + StashOpticsRetried + " attempt=" + attempt + " ageFrames=" + ageFrames + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
		}
	}

	internal static void RecordStashOpticEnforced(OpticSight opticSight, string reason)
	{
		StashOpticsEnforced++;
		StashBlocked++;
		if (StashOpticsEnforced <= 40 || StashOpticsEnforced % 20 == 0)
		{
			HeldScopeOnlyLog.WriteLine("stash-optic-enforced=" + StashOpticsEnforced + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
		}
		RecordStashLensHidden(opticSight, opticSight.GetComponentInParent<StashItemModel>(includeInactive: true));
	}

	internal static void RecordStashOpticUnresolved(OpticSight opticSight, int attempts, int ageFrames, string reason)
	{
		StashOpticsUnresolved++;
		HeldScopeOnlyLog.WriteLine("stash-optic-unresolved=" + StashOpticsUnresolved + " attempts=" + attempts + " ageFrames=" + ageFrames + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
	}

	private static void CollectStashOptics(Transform root, HashSet<int> seenOptics, ref int optics, ref int boundLenses, ref int unboundLenses)
	{
		if (root == null)
		{
			return;
		}
		OpticSight[] componentsInChildren = root.GetComponentsInChildren<OpticSight>(includeInactive: true);
		OpticSight[] array = componentsInChildren;
		foreach (OpticSight opticSight in array)
		{
			if (!(opticSight == null) && seenOptics.Add(opticSight.GetInstanceID()))
			{
				optics++;
				if (opticSight.LensRenderer == null)
				{
					unboundLenses++;
				}
				else
				{
					boundLenses++;
				}
				OpticRenderPolicy.RequestStashOptic(opticSight, "StashItemModel.Init hierarchy scan");
			}
		}
	}

	internal static bool IsNonLocalPlayerOptic(Component component, out string reason)
	{
		reason = null;
		if (component == null)
		{
			return false;
		}
		if (IsRemoteScopeComponent(component))
		{
			reason = "optic belongs to a registered observed player hierarchy";
			return true;
		}
		Player componentInParent = component.GetComponentInParent<Player>();
		if (componentInParent == null)
		{
			reason = "optic has no local Player owner and is treated as non-local";
			return true;
		}
		Player player = ResolveLocalPlayer();
		if (player != null)
		{
			if ((object)componentInParent == player)
			{
				reason = "optic belongs to the local player";
				return false;
			}
			reason = "optic belongs to a non-local player";
			return true;
		}
		if (!componentInParent.IsYourPlayer)
		{
			reason = "optic belongs to a non-local player before local registration";
			return true;
		}
		reason = "local player is not known yet";
		return false;
	}

	internal static void ShowExternalOpticLens(OpticSight opticSight, string reason)
	{
		if (opticSight == null)
		{
			return;
		}
		int instanceID = opticSight.GetInstanceID();
		if (_remoteOpticsSeen.Add(instanceID))
		{
			RemoteOpticsFound++;
			if (RemoteOpticsFound <= 40 || RemoteOpticsFound % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("fika-remote-optic-found=" + RemoteOpticsFound + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
			}
		}
		if (opticSight.LensRenderer == null)
		{
			return;
		}
		OpticRenderPolicy.SuppressOptic(opticSight, "external lens suppression: " + reason);
		if (_remoteLensesShownSeen.Add(instanceID))
		{
			RemoteLensesShown++;
			if (RemoteLensesShown <= 40 || RemoteLensesShown % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("fika-remote-lens-suppressed=" + RemoteLensesShown + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
			}
		}
	}

	internal static bool ShouldEnable(OpticSight opticSight)
	{
		bool flag = EvaluateOptic(opticSight, out var reason, out var owner, out var local);
		Record(flag, reason, opticSight, owner, local);
		return flag;
	}

	internal static bool EvaluateOpticForLens(OpticSight opticSight, out string reason, out Player owner, out Player local)
	{
		return EvaluateOptic(opticSight, out reason, out owner, out local);
	}

	private static bool EvaluateOptic(OpticSight opticSight, out string reason, out Player owner, out Player local)
	{
		owner = null;
		local = ResolveLocalPlayer();
		if (opticSight == null)
		{
			reason = "null optic event; fail closed";
			return false;
		}
		if (OpticRenderPolicy.IsCurrentLocalHeldOptic(opticSight))
		{
			reason = "optic is the local player's current held scope";
			return true;
		}
		if (IsRemoteScopeComponent(opticSight))
		{
			reason = "optic belongs to a remote observed player";
			return false;
		}
		if (IsStashOptic(opticSight))
		{
			reason = "optic belongs to a hideout display weapon";
			return false;
		}
		owner = opticSight.GetComponentInParent<Player>();
		if (owner != null)
		{
			reason = "optic belongs to a different player";
			return false;
		}
		reason = "optic is not the current local scope";
		return false;
	}

	internal static bool IsStashOptic(OpticSight opticSight)
	{
		if (opticSight == null)
		{
			return false;
		}
		StashItemModel componentInParent = opticSight.GetComponentInParent<StashItemModel>(includeInactive: true);
		if (componentInParent != null)
		{
			return true;
		}
		Transform transform = opticSight.transform;
		for (int num = _stashRoots.Count - 1; num >= 0; num--)
		{
			Transform transform2 = _stashRoots[num];
			if (transform2 == null)
			{
				_stashRootIds.Remove(_stashRootInstanceIds[num]);
				_stashRootInstanceIds.RemoveAt(num);
				_stashRoots.RemoveAt(num);
			}
			else if ((object)transform == transform2 || transform.IsChildOf(transform2))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool ShouldApplyReticle(OpticSight opticSight, string entryPoint)
	{
		bool flag = EvaluateReticle(opticSight, out var reason, out var owner, out var local);
		RecordReticleCheck(entryPoint, flag, reason, opticSight, owner, local);
		return flag;
	}

	private static bool EvaluateReticle(OpticSight opticSight, out string reason, out Player owner, out Player local)
	{
		owner = null;
		local = ResolveLocalPlayer();
		if (opticSight == null)
		{
			reason = "null reticle optic; fail closed";
			return false;
		}
		if (OpticRenderPolicy.IsCurrentLocalOptic(opticSight))
		{
			reason = "optic is the local player's current scope";
			return true;
		}
		if (IsRemoteScopeComponent(opticSight))
		{
			reason = "reticle optic belongs to a remote observed player";
			return false;
		}
		if (IsStashOptic(opticSight))
		{
			reason = "optic belongs to a hideout display weapon";
			return false;
		}
		owner = opticSight.GetComponentInParent<Player>();
		if (owner != null)
		{
			reason = "optic belongs to a different player";
			return false;
		}
		reason = "optic is not the current local optic";
		return false;
	}

	private static Player ResolveLocalPlayer()
	{
		Player gameLocalPlayer = GetGameLocalPlayer();
		if (gameLocalPlayer != null)
		{
			_localPlayer = gameLocalPlayer;
			return gameLocalPlayer;
		}
		return _localPlayer;
	}

	private static Player GetGameLocalPlayer()
	{
		if (!_gamePlayerOwnerMyPlayerResolved)
		{
			_gamePlayerOwnerMyPlayerResolved = true;
			try
			{
				Type type = AccessTools.TypeByName("EFT.GamePlayerOwner");
				if (type != null)
				{
					_gamePlayerOwnerMyPlayer = AccessTools.Property(type, "MyPlayer");
				}
			}
			catch (Exception ex)
			{
				HeldScopeOnlyLog.WriteLine("could not resolve GamePlayerOwner.MyPlayer; using CameraManager.SetPlayer cache only: " + ex);
			}
		}
		if (_gamePlayerOwnerMyPlayer == null)
		{
			return null;
		}
		try
		{
			object value = _gamePlayerOwnerMyPlayer.GetValue(null, null);
			return (Player)((value is Player) ? value : null);
		}
		catch (Exception ex2)
		{
			HeldScopeOnlyLog.WriteLine("could not read GamePlayerOwner.MyPlayer: " + ex2);
			return null;
		}
	}

	internal static bool TryGetLocalCurrentSights(out OpticSight opticSight, out CollimatorSight collimatorSight, out string reason)
	{
		Player player = ResolveLocalPlayer();
		if (player == null)
		{
			opticSight = null;
			collimatorSight = null;
			reason = "local player is not known";
			return false;
		}
		int frameCount = Time.frameCount;
		if (_currentSightsCacheValid && _currentSightsCacheFrame == frameCount && (object)_currentSightsCachePlayer == player)
		{
			opticSight = _currentSightsCacheOptic;
			collimatorSight = _currentSightsCacheCollimator;
			reason = _currentSightsCacheReason;
			return _currentSightsCacheResult;
		}
		bool flag = TryGetCurrentSights(player, out opticSight, out collimatorSight, out reason);
		_currentSightsCacheValid = true;
		_currentSightsCacheFrame = frameCount;
		_currentSightsCachePlayer = player;
		_currentSightsCacheResult = flag;
		_currentSightsCacheOptic = opticSight;
		_currentSightsCacheCollimator = collimatorSight;
		_currentSightsCacheReason = reason;
		return flag;
	}

	internal static bool TryGetLocalOpticAiming(out bool aiming, out bool optic)
	{
		aiming = false;
		optic = false;
		Player player = ResolveLocalPlayer();
		if (player == null)
		{
			return true;
		}
		try
		{
			ProceduralWeaponAnimation proceduralWeaponAnimation = player.ProceduralWeaponAnimation;
			if (proceduralWeaponAnimation == null)
			{
				return true;
			}
			aiming = Convert.ToBoolean(_isAimingProperty.GetValue(proceduralWeaponAnimation, null));
			if (!aiming)
			{
				return true;
			}
			if (!(_scopeAimTransformsField.GetValue(proceduralWeaponAnimation) is IList { Count: >0 } list))
			{
				return true;
			}
			int num = Convert.ToInt32(_aimIndexProperty.GetValue(proceduralWeaponAnimation, null));
			if (num < 0 || num >= list.Count)
			{
				return true;
			}
			object obj = list[num];
			if (obj == null)
			{
				return true;
			}
			optic = Convert.ToBoolean(_sightNBoneIsOpticProperty.GetValue(obj, null));
			return true;
		}
		catch (Exception ex)
		{
			_localOpticAimReadErrors++;
			if (_localOpticAimReadErrors <= 8 || _localOpticAimReadErrors % 250 == 0)
			{
				HeldScopeOnlyLog.WriteLine("local-optic-aim-read-error=" + _localOpticAimReadErrors + " player=" + DescribePlayer(player) + " exception=" + ex);
			}
			return false;
		}
	}

	internal static bool IsLocalPlayer(Player player)
	{
		if (player == null)
		{
			return false;
		}
		Player player2 = ResolveLocalPlayer();
		if (player2 != null)
		{
			return (object)player == player2;
		}
		return player.IsYourPlayer;
	}

	internal static bool TryGetLocalFirearmAiming(out bool aiming)
	{
		aiming = false;
		Player player = ResolveLocalPlayer();
		if (player == null)
		{
			return true;
		}
		try
		{
			Player.FirearmController firearmController = player.HandsController as Player.FirearmController;
			if (firearmController == null)
			{
				return true;
			}
			aiming = firearmController.IsAiming;
			return true;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("could not read local FirearmController.IsAiming: " + ex);
			return false;
		}
	}

	private static bool TryGetCurrentOptic(Player player, out OpticSight opticSight)
	{
		CollimatorSight collimatorSight;
		string reason;
		return TryGetCurrentSights(player, out opticSight, out collimatorSight, out reason);
	}

	private static bool TryGetCurrentSights(Player player, out OpticSight opticSight, out CollimatorSight collimatorSight, out string reason)
	{
		opticSight = null;
		collimatorSight = null;
		reason = null;
		try
		{
			ProceduralWeaponAnimation proceduralWeaponAnimation = player.ProceduralWeaponAnimation;
			if (proceduralWeaponAnimation == null)
			{
				reason = "local player has no procedural weapon animation";
				return true;
			}
			if (!(_scopeAimTransformsField.GetValue(proceduralWeaponAnimation) is IList { Count: >0 } list))
			{
				reason = "local held weapon has no active scope";
				return true;
			}
			int num = Convert.ToInt32(_aimIndexProperty.GetValue(proceduralWeaponAnimation, null));
			if (num < 0 || num >= list.Count)
			{
				reason = "local held weapon has no active aim index";
				return true;
			}
			object obj = list[num];
			if (obj == null)
			{
				reason = "local held weapon has no active scope";
				return true;
			}
			object value = _sightNBoneScopePrefabCacheField.GetValue(obj);
			ScopePrefabCache scopePrefabCache = (ScopePrefabCache)((value is ScopePrefabCache) ? value : null);
			if (scopePrefabCache == null)
			{
				reason = "local current scope has no prefab cache";
				return true;
			}
			return ScopeMaskRendererPatchState.TryResolveScopeMode(scopePrefabCache, out opticSight, out collimatorSight, out reason);
		}
		catch (Exception ex)
		{
			reason = "could not read local current scope: " + ex;
			return false;
		}
	}

	internal static void InvalidateCurrentSightsCache()
	{
		_currentSightsCacheValid = false;
		_currentSightsCacheFrame = -1;
		_currentSightsCachePlayer = null;
		_currentSightsCacheResult = false;
		_currentSightsCacheOptic = null;
		_currentSightsCacheCollimator = null;
		_currentSightsCacheReason = null;
	}

	private static void RecordReticleCheck(string entryPoint, bool allowed, string reason, OpticSight opticSight, Player owner, Player local)
	{
		ReticleChecked++;
		if (!allowed)
		{
			ReticleBlocked++;
		}
		if (ReticleChecked <= 160 || ReticleChecked % 250 == 0)
		{
			HeldScopeOnlyLog.WriteLine("reticle-check=" + ReticleChecked + " entry=" + entryPoint + " result=" + (allowed ? "ALLOW" : "BLOCK") + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight) + " owner=" + DescribePlayer(owner) + " local=" + DescribePlayer(local) + " totals=blocked:" + ReticleBlocked);
		}
		if (!allowed && (ReticleBlocked <= 160 || ReticleBlocked % 250 == 0))
		{
			HeldScopeOnlyLog.WriteLine("reticle-block=" + ReticleBlocked + " entry=" + entryPoint + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight) + " owner=" + DescribePlayer(owner) + " local=" + DescribePlayer(local));
		}
	}

	private static void Record(bool accepted, string reason, OpticSight opticSight, Player owner, Player local)
	{
		if (accepted)
		{
			Accepted++;
		}
		else
		{
			Rejected++;
		}
		int num = Accepted + Rejected;
		if (num <= 120 || num % 250 == 0)
		{
			HeldScopeOnlyLog.WriteLine("event=" + num + " result=" + (accepted ? "ACCEPT" : "REJECT") + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight) + " owner=" + DescribePlayer(owner) + " local=" + DescribePlayer(local) + " totals=accept:" + Accepted + "/reject:" + Rejected);
		}
	}

	internal static void RecordLensFade(OpticSight opticSight, bool isHide, bool permitted, string reason, Player owner, Player local)
	{
		LensChecked++;
		if (!permitted)
		{
			LensBlocked++;
		}
		if (LensChecked <= 120 || LensChecked % 250 == 0)
		{
			HeldScopeOnlyLog.WriteLine("lens-call=" + LensChecked + " result=" + (permitted ? "ALLOW" : "BLOCK") + " isHide=" + isHide + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight) + " owner=" + DescribePlayer(owner) + " local=" + DescribePlayer(local) + " totals=blocked:" + LensBlocked);
		}
	}

	internal static void RecordDrawOpticBlock(string reason)
	{
		DrawOpticBlocked++;
		if (DrawOpticBlocked <= 3 || DrawOpticBlocked % 10000 == 0)
		{
			HeldScopeOnlyLog.WriteLine("draw-optic-block=" + DrawOpticBlocked + " reason=\"" + reason + "\"");
		}
	}

	internal static void RecordDrawCollimatorBlock(string reason)
	{
		DrawCollimatorBlocked++;
		if (DrawCollimatorBlocked <= 3 || DrawCollimatorBlocked % 10000 == 0)
		{
			HeldScopeOnlyLog.WriteLine("draw-collimator-block=" + DrawCollimatorBlocked + " reason=\"" + reason + "\" totals=allowed:" + DrawCollimatorAllowed);
		}
	}

	internal static void RecordDrawActiveCollimatorBlock(string reason)
	{
		DrawActiveCollimatorBlocked++;
		if (DrawActiveCollimatorBlocked <= 3 || DrawActiveCollimatorBlocked % 10000 == 0)
		{
			HeldScopeOnlyLog.WriteLine("draw-active-collimator-block=" + DrawActiveCollimatorBlocked + " reason=\"" + reason + "\"");
		}
	}

	internal static void RecordStashLensHidden(OpticSight opticSight, StashItemModel stashModel)
	{
		if (StashBlocked <= 120 || StashBlocked % 250 == 0)
		{
			HeldScopeOnlyLog.WriteLine("stash-lens-hidden=" + StashBlocked + " optic=" + DescribeOptic(opticSight) + " stash=" + DescribeStash(stashModel));
		}
	}

	internal static void RecordStashEnableBlocked(OpticSight opticSight)
	{
		StashEnableBlocked++;
		if (StashEnableBlocked <= 20 || StashEnableBlocked % 250 == 0)
		{
			HeldScopeOnlyLog.WriteLine("stash-optic-enable-block=" + StashEnableBlocked + " optic=" + DescribeOptic(opticSight));
		}
	}

	internal static void RecordStashDisableBlocked(OpticSight opticSight)
	{
		StashDisableBlocked++;
		if (StashDisableBlocked <= 20 || StashDisableBlocked % 250 == 0)
		{
			HeldScopeOnlyLog.WriteLine("stash-optic-disable-block=" + StashDisableBlocked + " optic=" + DescribeOptic(opticSight));
		}
	}

	private static string DescribeTransform(Transform transform)
	{
		if (transform == null)
		{
			return "<null>";
		}
		return transform.name + "#" + transform.GetInstanceID();
	}

	private static string DescribeComponent(Component component)
	{
		if (component == null)
		{
			return "<null>";
		}
		return component.GetType().Name + "#" + component.GetInstanceID() + "(" + component.name + ")";
	}

	private static string DescribeOptic(OpticSight opticSight)
	{
		if (opticSight == null)
		{
			return "<null>";
		}
		return opticSight.name + "#" + opticSight.GetInstanceID();
	}

	private static string DescribePlayer(Player player)
	{
		if (player == null)
		{
			return "<null>";
		}
		return player.name + "#" + player.GetInstanceID();
	}

	private static string DescribeStash(StashItemModel stashModel)
	{
		if (stashModel == null)
		{
			return "<null>";
		}
		return stashModel.GetType().Name + "#" + stashModel.GetInstanceID();
	}
}
