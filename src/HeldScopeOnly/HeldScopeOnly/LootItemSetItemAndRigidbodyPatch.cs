using EFT.Interactive;

namespace HeldScopeOnly;

internal static class LootItemSetItemAndRigidbodyPatch
{
	internal static void Postfix(LootItem __instance)
	{
		OpticRenderPolicy.ScanLootItem(
			__instance,
			"ground LootItem.SetItemAndRigidbody");
	}
}
