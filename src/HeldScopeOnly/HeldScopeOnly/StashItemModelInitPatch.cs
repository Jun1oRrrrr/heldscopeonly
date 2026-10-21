using System;
using EFT.CameraControl;
using EFT.Hideout;
using UnityEngine;

namespace HeldScopeOnly;

internal static class StashItemModelInitPatch
{
	internal static void Postfix(StashItemModel __instance, GameObject prefab)
	{
		try
		{
			if (!(__instance is StashWeaponItemModel) && !(__instance is CircleOfCultistsStashWeaponItemModel))
			{
				return;
			}
			OpticRenderPolicy.ScanHierarchy(
				((Component)__instance).transform,
				"hideout weapon display StashItemModel.Init");
			if ((Object)(object)prefab == (Object)null)
			{
				return;
			}
			OpticSight[] componentsInChildren = prefab.GetComponentsInChildren<OpticSight>(true);
			foreach (OpticSight val in componentsInChildren)
			{
				if (!((Object)(object)val == (Object)null))
				{
					OpticRenderPolicy.ApplyOpticState(
						val,
						"hideout weapon display StashItemModel.Init");
					HeldScopeOnlyState.StashBlocked++;
					HeldScopeOnlyState.RecordStashLensHidden(val, __instance);
				}
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("StashItemModel.Init postfix exception: " + ex);
		}
	}
}
