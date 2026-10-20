using System;
using EFT.NextObservedPlayer;

namespace HeldScopeOnly;

internal static class ObservedPlayerFirearmHandsControllerTacticalUpdatePatch
{
	internal static void Postfix(ObservedPlayerFirearmHandsController __instance)
	{
		try
		{
			HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("ObservedPlayerFirearmHandsController tactical update postfix exception: " + ex);
		}
	}
}
