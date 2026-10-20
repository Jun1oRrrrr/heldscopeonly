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
		Type type = (Type)(object)AccessTools.TypeByName("EFT.Animations.ProceduralWeaponAnimation");
		if (type == null)
		{
			throw new MissingMethodException("EFT.Animations.ProceduralWeaponAnimation", "type");
		}
		_scopeAimTransformsField = (FieldInfo)(object)AccessTools.Field((Type)(object)type, "ScopeAimTransforms");
		_aimIndexProperty = (PropertyInfo)(object)AccessTools.Property((Type)(object)type, "AimIndex");
		_isAimingProperty = (PropertyInfo)(object)AccessTools.Property((Type)(object)type, "IsAiming");
		Type nestedType = type.GetNestedType("SightNBone", BindingFlags.Public | BindingFlags.NonPublic);
		if (_scopeAimTransformsField == null || _aimIndexProperty == null || _isAimingProperty == null || nestedType == null)
		{
			throw new MissingMemberException(type.FullName, "ScopeAimTransforms/AimIndex/IsAiming/SightNBone");
		}
		_sightNBoneIsOpticProperty = (PropertyInfo)(object)AccessTools.Property((Type)(object)nestedType, "IsOptic");
		_sightNBoneScopePrefabCacheField = (FieldInfo)(object)AccessTools.Field((Type)(object)nestedType, "ScopePrefabCache");
		if (_sightNBoneIsOpticProperty == null || _sightNBoneScopePrefabCacheField == null)
		{
			throw new MissingMemberException(nestedType.FullName, "IsOptic/ScopePrefabCache");
		}
	}

	internal static void RememberLocalPlayer(Player player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}
		Player gameLocalPlayer = GetGameLocalPlayer();
		if ((Object)(object)gameLocalPlayer != (Object)null)
		{
			if (!object.ReferenceEquals(player, gameLocalPlayer))
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
		if (!object.ReferenceEquals(_localPlayer, player))
		{
			_localPlayer = player;
			InvalidateCurrentSightsCache();
			string text = "cached local player: " + DescribePlayer(player);
			HeldScopeOnlyPlugin.Log.LogInfo((object)text);
			HeldScopeOnlyLog.WriteLine(text);
		}
	}

	internal static void RegisterRemoteWeaponRoot(Transform root)
	{
		if ((Object)(object)root == (Object)null)
		{
			return;
		}
		for (int num = _remoteWeaponRoots.Count - 1; num >= 0; num--)
		{
			Transform val = _remoteWeaponRoots[num];
			if ((Object)(object)val == (Object)null)
			{
				_remoteWeaponRoots.RemoveAt(num);
			}
			else if (object.ReferenceEquals(val, root))
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
	}

	internal static void RegisterRemoteObservedPlayerView(ObservedPlayerView view, string reason)
	{
		if ((Object)(object)view == (Object)null)
		{
			return;
		}
		try
		{
			RegisterRemoteWeaponRoot(((Component)view).transform);
			if (view.WeaponRoot != null)
			{
				RegisterRemoteWeaponRoot(view.WeaponRoot.Original);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("remote ObservedPlayerView registration exception: " + ex);
		}
	}

	private static void RecordRemoteViewFallback(ObservedPlayerView view, Transform evidence)
	{
		if ((Object)(object)view == (Object)null)
		{
			return;
		}
		int instanceID = ((Object)view).GetInstanceID();
		if (_remoteViewFallbackSeen.Add(instanceID))
		{
			RemoteViewFallbacks++;
			if (RemoteViewFallbacks <= 40 || RemoteViewFallbacks % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("fika-remote-view-fallback=" + RemoteViewFallbacks + " view=" + DescribeComponent((Component)(object)view) + " evidence=" + DescribeTransform(evidence));
			}
		}
	}

	internal static bool IsRemoteScopeTransform(Transform transform)
	{
		if ((Object)(object)transform == (Object)null)
		{
			return false;
		}
		for (int num = _remoteWeaponRoots.Count - 1; num >= 0; num--)
		{
			Transform val = _remoteWeaponRoots[num];
			if ((Object)(object)val == (Object)null)
			{
				_remoteWeaponRoots.RemoveAt(num);
			}
			else if (object.ReferenceEquals(val, transform) || transform.IsChildOf(val))
			{
				return true;
			}
		}
		ObservedPlayerView componentInParent = ((Component)transform).GetComponentInParent<ObservedPlayerView>(true);
		if ((Object)(object)componentInParent != (Object)null)
		{
			RegisterRemoteObservedPlayerView(componentInParent, "remote scope hierarchy fallback");
			RecordRemoteViewFallback(componentInParent, transform);
			return true;
		}
		return false;
	}

	internal static bool IsRemoteScopeComponent(Component component)
	{
		return (Object)(object)component != (Object)null && IsRemoteScopeTransform(component.transform);
	}

	internal static bool IsNonLocalPlayerOptic(Component component, out string reason)
	{
		reason = null;
		if ((Object)(object)component == (Object)null)
		{
			return false;
		}
		if (IsRemoteScopeComponent(component))
		{
			reason = "optic belongs to a registered observed player hierarchy";
			return true;
		}
		Player owner = component.GetComponentInParent<Player>();
		if ((Object)(object)owner == (Object)null)
		{
			reason = "optic has no Player owner";
			return false;
		}
		Player local = ResolveLocalPlayer();
		if ((Object)(object)local != (Object)null)
		{
			if (object.ReferenceEquals(owner, local))
			{
				reason = "optic belongs to the local player";
				return false;
			}
			reason = "optic belongs to a non-local player";
			return true;
		}
		if (!owner.IsYourPlayer)
		{
			reason = "optic belongs to a non-local player before local registration";
			return true;
		}
		reason = "local player is not known yet";
		return false;
	}

	internal static void ShowExternalOpticLens(OpticSight opticSight, string reason)
	{
		if ((Object)(object)opticSight == (Object)null)
		{
			return;
		}
		int instanceID = ((Object)opticSight).GetInstanceID();
		if (_remoteOpticsSeen.Add(instanceID))
		{
			RemoteOpticsFound++;
			if (RemoteOpticsFound <= 40 || RemoteOpticsFound % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("fika-remote-optic-found=" + RemoteOpticsFound + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
			}
		}
		if ((Object)(object)opticSight.LensRenderer == (Object)null)
		{
			return;
		}
		opticSight.LensFade(false);
		if (_remoteLensesShownSeen.Add(instanceID))
		{
			RemoteLensesShown++;
			if (RemoteLensesShown <= 40 || RemoteLensesShown % 20 == 0)
			{
				HeldScopeOnlyLog.WriteLine("fika-remote-lens-shown=" + RemoteLensesShown + " reason=\"" + reason + "\" optic=" + DescribeOptic(opticSight));
			}
		}
	}

	internal static bool ShouldEnable(OpticSight opticSight)
	{
		string reason;
		Player owner;
		Player local;
		bool flag = EvaluateOptic(opticSight, out reason, out owner, out local);
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
		local = null;
		if ((Object)(object)opticSight == (Object)null)
		{
			reason = "null optic event; preserving vanilla behavior";
			return true;
		}
		if (IsRemoteScopeComponent((Component)(object)opticSight))
		{
			reason = "optic belongs to a remote observed player";
			return false;
		}
		if (IsStashOptic(opticSight))
		{
			reason = "optic belongs to a hideout display weapon";
			return false;
		}
		local = ResolveLocalPlayer();
		if ((Object)(object)local == (Object)null)
		{
			reason = "local player is not known yet; fail open";
			return true;
		}
		owner = ((Component)opticSight).GetComponentInParent<Player>();
		bool flag = (Object)(object)owner != (Object)null && object.ReferenceEquals(owner, local);
		if ((Object)(object)owner != (Object)null && !flag)
		{
			reason = "optic belongs to a different player";
			return false;
		}
		if (!TryGetCurrentOptic(local, out var opticSight2))
		{
			reason = "could not read local current scope; fail open";
			return true;
		}
		if (object.ReferenceEquals(opticSight2, opticSight))
		{
			reason = "optic is the local player's current scope mode";
			return true;
		}
		if (flag && (Object)(object)opticSight2 == (Object)null)
		{
			reason = "local scope state is not ready; fail open for local owner";
			return true;
		}
		reason = (((Object)(object)owner == (Object)null) ? "optic has no Player owner and is not the current local optic" : "local owner optic is not the current scope mode");
		return false;
	}

	internal static bool IsStashOptic(OpticSight opticSight)
	{
		if ((Object)(object)opticSight == (Object)null)
		{
			return false;
		}
		StashItemModel componentInParent = ((Component)opticSight).GetComponentInParent<StashItemModel>(true);
		return componentInParent is StashWeaponItemModel || componentInParent is CircleOfCultistsStashWeaponItemModel;
	}

	internal static bool ShouldApplyReticle(OpticSight opticSight, string entryPoint)
	{
		string reason;
		Player owner;
		Player local;
		bool flag = EvaluateReticle(opticSight, out reason, out owner, out local);
		RecordReticleCheck(entryPoint, flag, reason, opticSight, owner, local);
		return flag;
	}

	private static bool EvaluateReticle(OpticSight opticSight, out string reason, out Player owner, out Player local)
	{
		owner = null;
		local = null;
		if ((Object)(object)opticSight == (Object)null)
		{
			reason = "null reticle optic; fail open";
			return true;
		}
		if (IsRemoteScopeComponent((Component)(object)opticSight))
		{
			reason = "reticle optic belongs to a remote observed player";
			return false;
		}
		local = ResolveLocalPlayer();
		if ((Object)(object)local == (Object)null)
		{
			reason = "local player is not known yet; fail open";
			return true;
		}
		OpticSight opticSight2;
		CollimatorSight collimatorSight;
		bool flag = TryGetLocalCurrentSights(out opticSight2, out collimatorSight, out reason);
		if (flag && (Object)(object)opticSight2 != (Object)null && object.ReferenceEquals(opticSight2, opticSight))
		{
			reason = "optic is the local player's current scope";
			return true;
		}
		if (IsStashOptic(opticSight))
		{
			reason = "optic belongs to a hideout display weapon";
			return false;
		}
		owner = ((Component)opticSight).GetComponentInParent<Player>();
		if ((Object)(object)owner != (Object)null && !object.ReferenceEquals(owner, local))
		{
			reason = "optic belongs to a different player";
			return false;
		}
		if (!flag)
		{
			reason = "could not read local current scope; fail open: " + reason;
			return true;
		}
		if ((Object)(object)opticSight2 == (Object)null)
		{
			reason = "local held weapon has no active optic; fail open";
			return true;
		}
		reason = (((Object)(object)owner == (Object)null) ? "optic has no Player owner and is not the local current optic" : "local owner optic is not the local current optic");
		return false;
	}

	private static Player ResolveLocalPlayer()
	{
		Player gameLocalPlayer = GetGameLocalPlayer();
		if ((Object)(object)gameLocalPlayer != (Object)null)
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
				Type type = (Type)(object)AccessTools.TypeByName("EFT.GamePlayerOwner");
				if (type != null)
				{
					_gamePlayerOwnerMyPlayer = (PropertyInfo)(object)AccessTools.Property((Type)(object)type, "MyPlayer");
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
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("could not read GamePlayerOwner.MyPlayer: " + ex);
			return null;
		}
	}

	internal static bool TryGetLocalCurrentSights(out OpticSight opticSight, out CollimatorSight collimatorSight, out string reason)
	{
		Player val = ResolveLocalPlayer();
		if ((Object)(object)val == (Object)null)
		{
			opticSight = null;
			collimatorSight = null;
			reason = "local player is not known";
			return false;
		}
		int frameCount = Time.frameCount;
		if (_currentSightsCacheValid && _currentSightsCacheFrame == frameCount && object.ReferenceEquals(_currentSightsCachePlayer, val))
		{
			opticSight = _currentSightsCacheOptic;
			collimatorSight = _currentSightsCacheCollimator;
			reason = _currentSightsCacheReason;
			return _currentSightsCacheResult;
		}
		bool flag = TryGetCurrentSights(val, out opticSight, out collimatorSight, out reason);
		_currentSightsCacheValid = true;
		_currentSightsCacheFrame = frameCount;
		_currentSightsCachePlayer = val;
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
		Player val = ResolveLocalPlayer();
		if ((Object)(object)val == (Object)null)
		{
			return true;
		}
		try
		{
			ProceduralWeaponAnimation proceduralWeaponAnimation = val.ProceduralWeaponAnimation;
			if ((Object)(object)proceduralWeaponAnimation == (Object)null)
			{
				return true;
			}
			aiming = Convert.ToBoolean(_isAimingProperty.GetValue(proceduralWeaponAnimation, null));
			if (!aiming)
			{
				return true;
			}
			if (!(_scopeAimTransformsField.GetValue(proceduralWeaponAnimation) is IList { Count: not 0 } list))
			{
				return true;
			}
			int num = Convert.ToInt32(_aimIndexProperty.GetValue(proceduralWeaponAnimation, null));
			if (num < 0)
			{
				return true;
			}
			object obj = list[num % list.Count];
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
				HeldScopeOnlyLog.WriteLine("local-optic-aim-read-error=" + _localOpticAimReadErrors + " player=" + DescribePlayer(val) + " exception=" + ex);
			}
			return false;
		}
	}

	internal static bool IsLocalPlayer(Player player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return false;
		}
		Player val = ResolveLocalPlayer();
		if ((Object)(object)val != (Object)null)
		{
			return object.ReferenceEquals(player, val);
		}
		return player.IsYourPlayer;
	}

	internal static bool TryGetLocalFirearmAiming(out bool aiming)
	{
		aiming = false;
		Player val = ResolveLocalPlayer();
		if ((Object)(object)val == (Object)null)
		{
			return true;
		}
		try
		{
			Player.FirearmController firearmController = val.HandsController as Player.FirearmController;
			if ((Object)(object)firearmController == (Object)null)
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
			if ((Object)(object)proceduralWeaponAnimation == (Object)null)
			{
				reason = "local player has no procedural weapon animation";
				return true;
			}
			if (!(_scopeAimTransformsField.GetValue(proceduralWeaponAnimation) is IList { Count: not 0 } list))
			{
				reason = "local held weapon has no active scope";
				return true;
			}
			int num = Convert.ToInt32(_aimIndexProperty.GetValue(proceduralWeaponAnimation, null));
			if (num < 0)
			{
				reason = "local held weapon has no active aim index";
				return true;
			}
			object obj = list[num % list.Count];
			if (obj == null)
			{
				reason = "local held weapon has no active scope";
				return true;
			}
			object value = _sightNBoneScopePrefabCacheField.GetValue(obj);
			ScopePrefabCache val = (ScopePrefabCache)((value is ScopePrefabCache) ? value : null);
			if ((Object)(object)val == (Object)null)
			{
				reason = "local current scope has no prefab cache";
				return true;
			}
			return ScopeMaskRendererPatchState.TryResolveScopeMode(val, out opticSight, out collimatorSight, out reason);
		}
		catch (Exception ex)
		{
			reason = "could not read local current scope: " + ex;
			return false;
		}
	}

	private static void InvalidateCurrentSightsCache()
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
		if ((Object)(object)transform == (Object)null)
		{
			return "<null>";
		}
		return ((Object)transform).name + "#" + ((Object)transform).GetInstanceID();
	}

	private static string DescribeComponent(Component component)
	{
		if ((Object)(object)component == (Object)null)
		{
			return "<null>";
		}
		return ((object)component).GetType().Name + "#" + ((Object)component).GetInstanceID() + "(" + ((Object)component).name + ")";
	}

	private static string DescribeOptic(OpticSight opticSight)
	{
		if ((Object)(object)opticSight == (Object)null)
		{
			return "<null>";
		}
		return ((Object)opticSight).name + "#" + ((Object)opticSight).GetInstanceID();
	}

	private static string DescribePlayer(Player player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return "<null>";
		}
		return ((Object)player).name + "#" + ((Object)player).GetInstanceID();
	}

	private static string DescribeStash(StashItemModel stashModel)
	{
		if ((Object)(object)stashModel == (Object)null)
		{
			return "<null>";
		}
		return ((object)stashModel).GetType().Name + "#" + ((Object)stashModel).GetInstanceID();
	}
}
