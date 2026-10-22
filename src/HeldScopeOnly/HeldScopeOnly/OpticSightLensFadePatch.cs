using System;
using EFT.CameraControl;

namespace HeldScopeOnly;

internal static class OpticSightLensFadePatch
{
	internal static void Postfix(OpticSight __instance, bool isHide)
	{
		try
		{
			bool flag = OpticRenderPolicy.IsCurrentLocalOptic(__instance);
			OpticRenderPolicy.ApplyOpticState(__instance, "OpticSight.LensFade postfix");
			HeldScopeOnlyState.RecordLensFade(__instance, isHide, flag, flag ? "current local optic while aiming" : "not the current local aiming optic", null, null);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("LensFade postfix exception; suppressing lens: " + ex);
			OpticRenderPolicy.SuppressOptic(__instance, "OpticSight.LensFade postfix exception");
		}
	}
}
