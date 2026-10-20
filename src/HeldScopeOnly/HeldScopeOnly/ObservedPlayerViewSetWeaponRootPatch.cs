using EFT.NextObservedPlayer;

namespace HeldScopeOnly;

internal static class ObservedPlayerViewSetWeaponRootPatch
{
	internal static void Postfix(ObservedPlayerView __instance)
	{
		HeldScopeOnlyState.RegisterRemoteObservedPlayerView(__instance, "remote ObservedPlayerView.SetWeaponRoot");
	}
}
