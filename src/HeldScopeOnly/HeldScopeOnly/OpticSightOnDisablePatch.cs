using System;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticSightOnDisablePatch
{
	internal static void Postfix(OpticSight __instance)
	{
		try
		{
			OpticRenderPolicy.SuppressOptic(
				__instance,
				"OpticSight.OnDisable postfix");
			if (HeldScopeOnlyState.IsStashOptic(__instance))
			{
				HeldScopeOnlyState.RecordStashDisableBlocked(__instance);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine(
				"OpticSight.OnDisable postfix exception; suppressing lens: " + ex);
			OpticRenderPolicy.SuppressOptic(
				__instance,
				"OpticSight.OnDisable postfix exception");
		}
	}
}
