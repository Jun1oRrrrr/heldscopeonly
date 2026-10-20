using EFT.NextObservedPlayer;

namespace HeldScopeOnly;

internal static class ObservedPlayerViewInitializationPatch
{
	internal static void Postfix(ObservedPlayerView __instance)
	{
		HeldScopeOnlyState.RegisterRemoteObservedPlayerView(__instance, "remote ObservedPlayerView.Initialization");
	}
}
