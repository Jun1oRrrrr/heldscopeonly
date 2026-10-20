using System;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticSightOnDisablePatch
{
	internal static bool Prefix(OpticSight __instance)
	{
		try
		{
			if (HeldScopeOnlyState.IsNonLocalPlayerOptic((Component)(object)__instance, out string reason))
			{
				HeldScopeOnlyState.ShowExternalOpticLens(__instance, reason);
				return false;
			}
			if (!HeldScopeOnlyState.IsStashOptic(__instance))
			{
				return true;
			}
			HeldScopeOnlyState.RecordStashDisableBlocked(__instance);
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("OpticSight.OnDisable filter exception; allowing vanilla: " + ex);
			return true;
		}
	}
}
