using System;

namespace HeldScopeOnly;

internal static class FirearmControllerSetIsAimingPatch
{
	internal static void Postfix(EFT.Player.FirearmController __instance, bool value)
	{
		try
		{
			HeldScopeOnlyPlugin.NotifyFirearmAimingChanged(__instance, value);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("FirearmController.IsAiming patch exception; continuing vanilla: " + ex);
		}
	}
}
