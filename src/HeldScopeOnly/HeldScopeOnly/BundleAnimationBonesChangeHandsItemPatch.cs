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
			if (!((Object)(object)itemObject == (Object)null) && !((Object)(object)itemObject.Hierarchy == (Object)null))
			{
				Transform transform = itemObject.Hierarchy.GetTransform((ECharacterWeaponBones)5);
				HeldScopeOnlyState.RegisterRemoteWeaponRoot(transform);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("BundleAnimationBones.ChangeHandsItem postfix exception: " + ex);
		}
	}
}
