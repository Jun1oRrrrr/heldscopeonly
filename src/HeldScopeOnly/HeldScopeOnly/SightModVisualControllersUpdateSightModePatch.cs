namespace HeldScopeOnly;

internal static class SightModVisualControllersUpdateSightModePatch
{
	internal static void Postfix(SightModVisualControllers __instance)
	{
		HeldScopeOnlyState.InvalidateCurrentSightsCache();
		OpticRenderPolicy.ScanHierarchy(__instance.transform, "SightModVisualControllers.UpdateSightMode");
	}
}
