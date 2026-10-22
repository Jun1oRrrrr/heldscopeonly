using EFT.NextObservedPlayer;

namespace HeldScopeOnly;

internal static class ObservedPlayerFirearmHandsControllerUpdateScopesPatch
{
	internal static void Postfix(ObservedPlayerFirearmHandsController __instance)
	{
		HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
		OpticRenderPolicy.ScanHierarchy(__instance._weaponHierarchy, "remote observed-player UpdateScopes");
	}
}
