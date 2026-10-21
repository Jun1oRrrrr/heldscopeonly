using System;
using EFT.CameraControl;

namespace HeldScopeOnly;

internal static class OpticCameraManagerEnablePatch
{
	internal static bool Prefix(OpticSight opticSight)
	{
		try
		{
			return HeldScopeOnlyState.ShouldEnable(opticSight);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("filter exception; blocking optic: " + ex);
			return false;
		}
	}
}
