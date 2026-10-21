using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HeldScopeOnly;

internal static class ScopeMaskRendererDrawCollimatorsPatch
{
	internal static bool Prefix(ScopeMaskRenderer __instance, CommandBuffer buffer, Camera currentCamera)
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!OpticRenderPolicy.TryGetAllowedLocalCollimator(out var collimatorSight))
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("local held collimator is not aiming or not active");
				return false;
			}
			if (buffer == null || (Object)(object)currentCamera == (Object)null)
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("draw buffer or camera is unavailable");
				return false;
			}
			Material val = ScopeMaskRendererPatchState.CollimatorMaterial(__instance);
			if ((Object)(object)val == (Object)null)
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("collimator mask material is unavailable");
				return false;
			}
			Vector3 position = ((Component)currentCamera).transform.position;
			Vector3 position2 = ((Component)collimatorSight.CollimatorMeshRenderer).transform.position;
			Vector3 val2 = position - position2;
			if (val2.sqrMagnitude > ScopeMaskRendererPatchState.MaxCollimatorSqrMagnitude)
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("local current collimator is outside vanilla draw distance");
				return false;
			}
			val.SetColor(ScopeMaskRendererPatchState.ColorPropertyId, ScopeMaskRendererPatchState.CollimatorColor);
			buffer.DrawRenderer((Renderer)(object)collimatorSight.CollimatorMeshRenderer, val);
			HeldScopeOnlyState.DrawCollimatorAllowed++;
			return false;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyState.RecordDrawCollimatorBlock("DrawCollimators filter exception: " + ex);
			return false;
		}
	}
}
