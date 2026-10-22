using System;

namespace HeldScopeOnly;

internal static class ScopeMaskRendererDrawActiveCollimatorPatch
{
	internal static bool Prefix(ScopeMaskRenderer __instance)
	{
		try
		{
			if (!OpticRenderPolicy.TryGetAllowedLocalCollimator(out var collimatorSight))
			{
				HeldScopeOnlyState.RecordDrawActiveCollimatorBlock("local held collimator is not aiming or not active");
				return false;
			}
			CollimatorSight collimatorSight2 = ScopeMaskRendererPatchState.ActiveCollimatorSight(__instance);
			if ((object)collimatorSight2 == collimatorSight)
			{
				return true;
			}
			HeldScopeOnlyState.RecordDrawActiveCollimatorBlock((collimatorSight2 == null) ? "no active collimator to draw" : "active collimator is not the local held scope");
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyState.RecordDrawActiveCollimatorBlock("DrawActiveCollimator filter exception: " + ex);
			return false;
		}
	}
}
