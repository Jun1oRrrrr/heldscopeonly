using System;
using EFT.CameraControl;

namespace HeldScopeOnly;

internal static class OpticSightOnEnablePatch
{
	internal static void Postfix(OpticSight __instance)
	{
		try
		{
			OpticRenderPolicy.ApplyOpticState(__instance, "OpticSight.OnEnable postfix");
			if (!OpticRenderPolicy.IsCurrentLocalOptic(__instance) && HeldScopeOnlyState.IsStashOptic(__instance))
			{
				HeldScopeOnlyState.RecordStashEnableBlocked(__instance);
			}
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("OpticSight.OnEnable postfix exception; suppressing lens: " + ex);
			OpticRenderPolicy.SuppressOptic(__instance, "OpticSight.OnEnable postfix exception");
		}
	}
}
