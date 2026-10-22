using System;
using EFT;

namespace HeldScopeOnly;

internal static class FirearmsUpdateModsVisualControllersPatch
{
	internal static void Prefix(Firearms __instance)
	{
		try
		{
			Player player = __instance.Player;
			if (!(player == null) && !player.IsYourPlayer)
			{
				HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
				OpticRenderPolicy.ScanHierarchy(__instance._weaponHierarchy, "remote Firearms.UpdateModsVisualControllers prefix");
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
			if (!(player == null) && !player.IsYourPlayer)
			{
				HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
				OpticRenderPolicy.ScanHierarchy(__instance._weaponHierarchy, "remote Firearms.UpdateModsVisualControllers postfix");
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("Firearms.UpdateModsVisualControllers postfix exception: " + ex);
		}
	}
}
