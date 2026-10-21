using System;
using UnityEngine;

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
			CollimatorSight val = ScopeMaskRendererPatchState.ActiveCollimatorSight(__instance);
			if (object.ReferenceEquals(val, collimatorSight))
			{
				return true;
			}
			HeldScopeOnlyState.RecordDrawActiveCollimatorBlock(((Object)(object)val == (Object)null) ? "no active collimator to draw" : "active collimator is not the local held scope");
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyState.RecordDrawActiveCollimatorBlock("DrawActiveCollimator filter exception: " + ex);
			return false;
		}
	}
}
