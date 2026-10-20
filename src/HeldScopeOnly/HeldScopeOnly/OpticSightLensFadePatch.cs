using System;
using EFT;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticSightLensFadePatch
{
	internal static void Prefix(OpticSight __instance, ref bool isHide)
	{
		try
		{
			bool flag = true;
			string reason = "vanilla requested lens hide";
			Player owner = null;
			Player local = null;
			if (HeldScopeOnlyState.IsNonLocalPlayerOptic((Component)(object)__instance, out reason))
			{
				flag = true;
				isHide = false;
			}
			else if (!isHide)
			{
				flag = HeldScopeOnlyState.EvaluateOpticForLens(__instance, out reason, out owner, out local);
				if (!flag)
				{
					isHide = true;
				}
			}
			HeldScopeOnlyState.RecordLensFade(__instance, isHide, flag, reason, owner, local);
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("LensFade filter exception; preserving vanilla behavior: " + ex);
		}
	}
}
