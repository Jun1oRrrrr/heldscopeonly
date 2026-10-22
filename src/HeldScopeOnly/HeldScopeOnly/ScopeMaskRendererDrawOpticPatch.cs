using System;
using EFT.CameraControl;

namespace HeldScopeOnly;

internal static class ScopeMaskRendererDrawOpticPatch
{
	internal static bool Prefix(ScopeMaskRenderer __instance)
	{
		try
		{
			if (!OpticRenderPolicy.TryGetAllowedLocalOptic(out var opticSight))
			{
				HeldScopeOnlyState.RecordDrawOpticBlock("local held optic is not aiming or not active");
				return false;
			}
			OpticSight currentOpticSight = __instance.CurrentOpticSight;
			if ((object)currentOpticSight == opticSight)
			{
				return true;
			}
			HeldScopeOnlyState.RecordDrawOpticBlock((currentOpticSight == null) ? "no current optic to draw" : "current optic is not the local held scope");
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyState.RecordDrawOpticBlock("DrawOptic filter exception: " + ex);
			return false;
		}
	}
}
