using System;
using EFT;

namespace HeldScopeOnly;

internal static class CameraManagerSetPlayerPatch
{
	internal static void Postfix(Player p)
	{
		try
		{
			HeldScopeOnlyState.RememberLocalPlayer(p);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("SetPlayer postfix exception; continuing vanilla: " + ex);
		}
	}
}
