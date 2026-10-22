using EFT.Interactive;

namespace HeldScopeOnly;

internal static class LootItemInitPatch
{
	internal static void Postfix(LootItem __instance)
	{
		OpticRenderPolicy.ScanLootItem(__instance, "ground LootItem.Init");
	}
}
