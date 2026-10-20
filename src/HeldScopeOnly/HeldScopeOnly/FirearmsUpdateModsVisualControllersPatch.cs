using System;
using EFT;
using UnityEngine;

namespace HeldScopeOnly;

internal static class FirearmsUpdateModsVisualControllersPatch
{
	internal static void Prefix(Firearms __instance)
	{
		try
		{
			Player player = __instance.Player;
			if (!((Object)(object)player == (Object)null) && !player.IsYourPlayer)
			{
				HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("Firearms.UpdateModsVisualControllers prefix exception: " + ex);
		}
	}

	internal static void Postfix(Firearms __instance)
	{
		try
		{
			Player player = __instance.Player;
			if (!((Object)(object)player == (Object)null) && !player.IsYourPlayer)
			{
				HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("Firearms.UpdateModsVisualControllers postfix exception: " + ex);
		}
	}
}
