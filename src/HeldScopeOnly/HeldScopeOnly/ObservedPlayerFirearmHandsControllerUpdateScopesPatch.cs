using EFT.NextObservedPlayer;

namespace HeldScopeOnly;

internal static class ObservedPlayerFirearmHandsControllerUpdateScopesPatch
{
	internal static void Prefix(ObservedPlayerFirearmHandsController __instance)
	{
		HeldScopeOnlyState.RegisterRemoteWeaponRoot(__instance._weaponHierarchy);
	}
}
