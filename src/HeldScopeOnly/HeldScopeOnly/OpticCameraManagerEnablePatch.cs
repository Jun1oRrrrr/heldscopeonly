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
			HeldScopeOnlyLog.WriteLine("filter exception; allowing vanilla behavior: " + ex);
			return true;
		}
	}
}
