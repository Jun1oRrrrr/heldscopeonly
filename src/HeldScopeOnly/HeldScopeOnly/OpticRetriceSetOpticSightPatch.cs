using System;
using EFT.CameraControl;

namespace HeldScopeOnly;

internal static class OpticRetriceSetOpticSightPatch
{
	internal static bool Prefix(OpticSight opticSight)
	{
		try
		{
			if (opticSight == null)
			{
				return true;
			}
			return HeldScopeOnlyState.ShouldApplyReticle(opticSight, "SetOpticSight");
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("OpticRetrice.SetOpticSight filter exception; blocking optic: " + ex);
			return false;
		}
	}
}
