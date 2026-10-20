using System;
using EFT.CameraControl;

namespace HeldScopeOnly;

internal static class OpticRetriceUpdateTransformPatch
{
	internal static bool Prefix(OpticSight opticSight)
	{
		try
		{
			return HeldScopeOnlyState.ShouldApplyReticle(opticSight, "UpdateTransform");
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("OpticRetrice.UpdateTransform filter exception; allowing vanilla: " + ex);
			return true;
		}
	}
}
