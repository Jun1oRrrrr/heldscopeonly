using System;
using EFT.NextObservedPlayer;
using UnityEngine;

namespace HeldScopeOnly;

internal static class BundleAnimationBonesChangeHandsItemPatch
{
	internal static void Postfix(ObservedPlayerView ____observedPlayerView, WeaponPrefab itemObject)
	{
		try
		{
			HeldScopeOnlyState.RegisterRemoteObservedPlayerView(____observedPlayerView, "remote BundleAnimationBones.ChangeHandsItem");
			if (!(itemObject == null) && !(itemObject.Hierarchy == null))
			{
				Transform transform = itemObject.Hierarchy.GetTransform(ECharacterWeaponBones.Weapon_root);
				HeldScopeOnlyState.RegisterRemoteWeaponRoot(transform);
				OpticRenderPolicy.ScanHierarchy(transform, "remote BundleAnimationBones.ChangeHandsItem");
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("BundleAnimationBones.ChangeHandsItem postfix exception: " + ex);
		}
	}
}
