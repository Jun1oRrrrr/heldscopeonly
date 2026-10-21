using System;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticRetriceSetOpticSightPatch
{
	internal static bool Prefix(OpticSight opticSight)
	{
		try
		{
			if ((Object)(object)opticSight == (Object)null)
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
