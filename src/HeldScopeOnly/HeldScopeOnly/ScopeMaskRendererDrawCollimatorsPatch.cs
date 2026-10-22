using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HeldScopeOnly;

internal static class ScopeMaskRendererDrawCollimatorsPatch
{
	internal static bool Prefix(ScopeMaskRenderer __instance, CommandBuffer buffer, Camera currentCamera)
	{
		try
		{
			if (!OpticRenderPolicy.TryGetAllowedLocalCollimator(out var collimatorSight))
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("local held collimator is not aiming or not active");
				return false;
			}
			if (buffer == null || currentCamera == null)
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("draw buffer or camera is unavailable");
				return false;
			}
			Material material = ScopeMaskRendererPatchState.CollimatorMaterial(__instance);
			if (material == null)
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("collimator mask material is unavailable");
				return false;
			}
			Vector3 position = currentCamera.transform.position;
			Vector3 position2 = collimatorSight.CollimatorMeshRenderer.transform.position;
			if ((position - position2).sqrMagnitude > ScopeMaskRendererPatchState.MaxCollimatorSqrMagnitude)
			{
				HeldScopeOnlyState.RecordDrawCollimatorBlock("local current collimator is outside vanilla draw distance");
				return false;
			}
			material.SetColor(ScopeMaskRendererPatchState.ColorPropertyId, ScopeMaskRendererPatchState.CollimatorColor);
			buffer.DrawRenderer(collimatorSight.CollimatorMeshRenderer, material);
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
