using System;
using EFT;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticSightLensFadePatch
{
	internal static void Postfix(OpticSight __instance, bool isHide)
	{
		try
		{
			bool permitted = OpticRenderPolicy.IsCurrentLocalOptic(__instance);
			OpticRenderPolicy.ApplyOpticState(
				__instance,
				"OpticSight.LensFade postfix");
			HeldScopeOnlyState.RecordLensFade(
				__instance,
				isHide,
				permitted,
				permitted ? "current local optic while aiming" : "not the current local aiming optic",
				owner: null,
				local: null);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine(
				"LensFade postfix exception; suppressing lens: " + ex);
			OpticRenderPolicy.SuppressOptic(
				__instance,
				"OpticSight.LensFade postfix exception");
		}
	}
}
