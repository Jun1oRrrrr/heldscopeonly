using System;
using EFT.Hideout;
using UnityEngine;

namespace HeldScopeOnly;

internal static class StashItemModelInitPatch
{
	internal static void Postfix(StashItemModel __instance, GameObject prefab)
	{
		try
		{
			HeldScopeOnlyState.RegisterStashModel(__instance, prefab);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("StashItemModel.Init postfix exception: " + ex);
		}
	}
}
