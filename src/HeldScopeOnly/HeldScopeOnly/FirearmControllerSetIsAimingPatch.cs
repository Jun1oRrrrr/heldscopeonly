using System;
using EFT;

namespace HeldScopeOnly;

internal static class FirearmControllerSetIsAimingPatch
{
	internal static void Postfix(Player.FirearmController __instance, bool value)
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
