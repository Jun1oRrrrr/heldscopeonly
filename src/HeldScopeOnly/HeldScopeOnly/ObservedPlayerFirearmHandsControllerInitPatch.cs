using System;
using EFT.NextObservedPlayer;

namespace HeldScopeOnly;

internal static class ObservedPlayerFirearmHandsControllerInitPatch
{
	internal static void Prefix(ObservedPlayerFirearmHandsController __instance)
	{
		try
		{
			HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("ObservedPlayerFirearmHandsController.Initialize prefix exception: " + ex);
		}
	}

	internal static void Postfix(ObservedPlayerFirearmHandsController __instance)
	{
		try
		{
			HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
			OpticRenderPolicy.ScanHierarchy(
				__instance._weaponHierarchy,
				"remote ObservedPlayerFirearmHandsController.Initialize");
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("ObservedPlayerFirearmHandsController.Initialize postfix exception: " + ex);
		}
	}
}
