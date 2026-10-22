namespace HeldScopeOnly;

internal static class ScopePrefabCacheSetModePatch
{
	internal static void Postfix(ScopePrefabCache __instance)
	{
		HeldScopeOnlyState.InvalidateCurrentSightsCache();
		OpticRenderPolicy.ScanHierarchy(__instance.transform, "ScopePrefabCache.SetMode");
	}
}
