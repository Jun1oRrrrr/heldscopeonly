using System;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticSightOnEnablePatch
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
			HeldScopeOnlyState.RecordStashEnableBlocked(__instance);
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("OpticSight.OnEnable filter exception; allowing vanilla: " + ex);
			return true;
		}
	}
}
