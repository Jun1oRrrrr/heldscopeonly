using System;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class ScopeMaskRendererDrawOpticPatch
{
	internal static bool Prefix(ScopeMaskRenderer __instance)
	{
		try
		{
			if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var opticSight, out var _, out var reason))
			{
				HeldScopeOnlyState.RecordDrawOpticBlock(reason);
				return false;
			}
			OpticSight currentOpticSight = __instance.CurrentOpticSight;
			if ((Object)(object)opticSight != (Object)null && object.ReferenceEquals(currentOpticSight, opticSight))
			{
				return true;
			}
			HeldScopeOnlyState.RecordDrawOpticBlock(((Object)(object)currentOpticSight == (Object)null) ? "no current optic to draw" : "current optic is not the local held scope");
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyState.RecordDrawOpticBlock("DrawOptic filter exception: " + ex);
			return false;
		}
	}
}
