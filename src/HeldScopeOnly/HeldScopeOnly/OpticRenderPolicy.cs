using System;
using System.Collections.Generic;
using EFT.CameraControl;
using EFT.Interactive;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticRenderPolicy
{
	private sealed class LensSnapshot
	{
		internal Renderer Renderer;

		internal OpticSight Optic;

		internal Material[] SourceMaterials;

		internal Material[] SuppressedMaterials;

		internal int LastAllowed = -1;
	}

	private sealed class PendingStashOptic
	{
		internal OpticSight Optic;

		internal int InstanceId;

		internal string Reason;

		internal int FirstFrame;

		internal int NextFrame;

		internal int Attempts;
	}

	private const string CameraTextureProperty = "_CamTex";

	private const string SwitchToSightProperty = "_SwitchToSight";

	private static readonly Dictionary<Renderer, LensSnapshot> LensByRenderer = new Dictionary<Renderer, LensSnapshot>();

	private static readonly List<LensSnapshot> Snapshots = new List<LensSnapshot>();

	private static readonly List<PendingStashOptic> PendingStashOptics = new List<PendingStashOptic>();

	private static readonly HashSet<int> PendingStashOpticIds = new HashSet<int>();

	private const int StashRetryBatchLimit = 4;

	private const int StashRetryIntervalFrames = 2;

	private const int StashRetryAttemptLimit = 90;

	private const int StashRetryAgeLimitFrames = 240;

	private static Texture2D _opaqueNeutralTexture;

	private static bool _scanExceptionLogged;

	private static LensSnapshot _lastAllowedSnapshot;

	private static int _pruneCursor;

	private static int _lastPruneFrame = -1;

	internal static int PolicyChecks;

	internal static int PolicyAllowed;

	internal static int PolicyBlocked;

	internal static int LensesSuppressed;

	internal static int LensesRestored;

	internal static int HierarchyScans;

	internal static int OpticsVisited;

	internal static int FrameEnforcementPasses;

	internal static int FrameEnforcedOptics;

	internal static int CamTexRenderersTracked;

	internal static int CamTexRenderersSuppressed;

	internal static int CamTexRenderersAllowed;

	internal static int InteriorTexturePropertiesBlocked;

	internal static void ScanHierarchy(Transform root, string reason)
	{
		if (root == null)
		{
			return;
		}
		HierarchyScans++;
		try
		{
			OpticSight[] componentsInChildren = root.GetComponentsInChildren<OpticSight>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ApplyOpticState(componentsInChildren[i], reason);
			}
		}
		catch (Exception ex)
		{
			if (!_scanExceptionLogged)
			{
				_scanExceptionLogged = true;
				HeldScopeOnlyLog.WriteLine("optic hierarchy scan failed reason=\"" + reason + "\" exception=" + ex);
			}
		}
	}

	internal static void ScanLootItem(LootItem item, string reason)
	{
		if (!(item == null))
		{
			ScanHierarchy(item.transform, reason);
		}
	}

	internal static void RequestStashOptic(OpticSight opticSight, string reason)
	{
		if (opticSight == null)
		{
			return;
		}
		if (TrackOptic(opticSight, allowed: false, force: true))
		{
			HeldScopeOnlyState.RecordStashOpticEnforced(opticSight, reason);
			return;
		}
		int instanceID = opticSight.GetInstanceID();
		if (!PendingStashOpticIds.Add(instanceID))
		{
			for (int i = 0; i < PendingStashOptics.Count; i++)
			{
				if ((object)PendingStashOptics[i].Optic == opticSight)
				{
					PendingStashOptics[i].Reason = reason;
					break;
				}
			}
		}
		else
		{
			int frameCount = Time.frameCount;
			PendingStashOptics.Add(new PendingStashOptic
			{
				Optic = opticSight,
				InstanceId = instanceID,
				Reason = reason,
				FirstFrame = frameCount,
				NextFrame = frameCount + 1
			});
			HeldScopeOnlyState.RegisterStashOptic(opticSight, reason);
		}
	}

	internal static void ProcessPendingStashOptics()
	{
		if (PendingStashOptics.Count == 0)
		{
			return;
		}
		int num = Math.Min(4, PendingStashOptics.Count);
		int frameCount = Time.frameCount;
		while (num-- > 0 && PendingStashOptics.Count > 0)
		{
			PendingStashOptic pendingStashOptic = PendingStashOptics[0];
			PendingStashOptics.RemoveAt(0);
			if (pendingStashOptic == null)
			{
				continue;
			}
			if (pendingStashOptic.Optic == null)
			{
				PendingStashOpticIds.Remove(pendingStashOptic.InstanceId);
				continue;
			}
			if (!HeldScopeOnlyState.IsStashOptic(pendingStashOptic.Optic))
			{
				PendingStashOpticIds.Remove(pendingStashOptic.InstanceId);
				ApplyOpticState(pendingStashOptic.Optic, "stash optic left hideout hierarchy");
				continue;
			}
			if (frameCount < pendingStashOptic.NextFrame)
			{
				PendingStashOptics.Add(pendingStashOptic);
				continue;
			}
			pendingStashOptic.Attempts++;
			int num2 = frameCount - pendingStashOptic.FirstFrame;
			HeldScopeOnlyState.RecordStashOpticRetry(pendingStashOptic.Optic, pendingStashOptic.Attempts, num2, pendingStashOptic.Reason);
			if (TrackOptic(pendingStashOptic.Optic, allowed: false, force: true))
			{
				PendingStashOpticIds.Remove(pendingStashOptic.InstanceId);
				HeldScopeOnlyState.RecordStashOpticEnforced(pendingStashOptic.Optic, "late stash lens resolution");
			}
			else if (pendingStashOptic.Attempts >= 90 || num2 >= 240)
			{
				PendingStashOpticIds.Remove(pendingStashOptic.InstanceId);
				HeldScopeOnlyState.RecordStashOpticUnresolved(pendingStashOptic.Optic, pendingStashOptic.Attempts, num2, pendingStashOptic.Reason);
			}
			else
			{
				pendingStashOptic.NextFrame = frameCount + 2;
				PendingStashOptics.Add(pendingStashOptic);
			}
		}
	}

	internal static void EnforceBeforeCameraRender(Camera camera, string reason)
	{
		FrameEnforcementPasses++;
		if (Snapshots.Count == 0)
		{
			return;
		}
		LensSnapshot snapshot = null;
		if (TryGetAllowedLocalOptic(out var opticSight))
		{
			TryGetSnapshot(opticSight, out snapshot);
		}
		if (snapshot != _lastAllowedSnapshot)
		{
			LensSnapshot lastAllowedSnapshot = _lastAllowedSnapshot;
			_lastAllowedSnapshot = snapshot;
			if (lastAllowedSnapshot != null)
			{
				ApplySnapshot(lastAllowedSnapshot, allowed: false);
			}
			if (snapshot != null)
			{
				ApplySnapshot(snapshot, allowed: true);
			}
		}
		PruneDestroyedSnapshots();
	}

	internal static bool TryGetAllowedLocalOptic(out OpticSight opticSight)
	{
		PolicyChecks++;
		opticSight = null;
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var opticSight2, out var _, out var _))
		{
			PolicyBlocked++;
			return false;
		}
		if (opticSight2 == null)
		{
			PolicyBlocked++;
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var optic) || !aiming || !optic)
		{
			PolicyBlocked++;
			return false;
		}
		if (!opticSight2.isActiveAndEnabled || opticSight2.LensRenderer == null)
		{
			PolicyBlocked++;
			return false;
		}
		if (HeldScopeOnlyState.IsStashOptic(opticSight2))
		{
			PolicyBlocked++;
			return false;
		}
		opticSight = opticSight2;
		PolicyAllowed++;
		return true;
	}

	internal static bool TryGetAllowedLocalCollimator(out CollimatorSight collimatorSight)
	{
		collimatorSight = null;
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var _, out var collimatorSight2, out var _))
		{
			return false;
		}
		if (collimatorSight2 == null)
		{
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var _) || !aiming)
		{
			return false;
		}
		if (!collimatorSight2.isActiveAndEnabled || collimatorSight2.CollimatorMeshRenderer == null)
		{
			return false;
		}
		collimatorSight = collimatorSight2;
		return true;
	}

	internal static bool IsCurrentLocalOptic(OpticSight opticSight)
	{
		PolicyChecks++;
		if (opticSight == null)
		{
			PolicyBlocked++;
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var opticSight2, out var _, out var _) || (object)opticSight2 != opticSight)
		{
			PolicyBlocked++;
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var optic) || !aiming || !optic)
		{
			PolicyBlocked++;
			return false;
		}
		if (!opticSight.isActiveAndEnabled || opticSight.LensRenderer == null)
		{
			PolicyBlocked++;
			return false;
		}
		if (HeldScopeOnlyState.IsStashOptic(opticSight))
		{
			PolicyBlocked++;
			return false;
		}
		PolicyAllowed++;
		return true;
	}

	internal static bool IsCurrentLocalHeldOptic(OpticSight opticSight)
	{
		if (opticSight == null)
		{
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var opticSight2, out var _, out var _) || (object)opticSight2 != opticSight)
		{
			return false;
		}
		if (opticSight.LensRenderer == null)
		{
			return false;
		}
		if (HeldScopeOnlyState.IsStashOptic(opticSight))
		{
			return false;
		}
		return true;
	}

	internal static void ApplyOpticState(OpticSight opticSight, string reason)
	{
		if (!(opticSight == null))
		{
			if (HeldScopeOnlyState.IsStashOptic(opticSight))
			{
				RequestStashOptic(opticSight, reason);
			}
			else
			{
				TrackOptic(opticSight, IsCurrentLocalOptic(opticSight), force: false);
			}
		}
	}

	internal static void SuppressOptic(OpticSight opticSight, string reason, bool force = false)
	{
		if (!(opticSight == null))
		{
			if (force || HeldScopeOnlyState.IsStashOptic(opticSight))
			{
				RequestStashOptic(opticSight, reason);
			}
			else
			{
				TrackOptic(opticSight, allowed: false, force: false);
			}
		}
	}

	internal static void RestoreOptic(OpticSight opticSight, string reason)
	{
		if (!(opticSight == null))
		{
			if (HeldScopeOnlyState.IsStashOptic(opticSight))
			{
				RequestStashOptic(opticSight, reason);
			}
			else
			{
				TrackOptic(opticSight, IsCurrentLocalOptic(opticSight), force: false);
			}
		}
	}

	internal static void RestoreCurrentLocalOptic(string reason)
	{
		if (TryGetAllowedLocalOptic(out var opticSight) && TryGetSnapshot(opticSight, out var snapshot))
		{
			ApplySnapshot(snapshot, allowed: true);
		}
		else
		{
			HideCurrentLocalOptic(reason);
		}
	}

	internal static void HideCurrentLocalOptic(string reason)
	{
		if (HeldScopeOnlyState.TryGetLocalCurrentSights(out var opticSight, out var _, out var _) && opticSight != null && TryGetSnapshot(opticSight, out var snapshot))
		{
			ApplySnapshot(snapshot, allowed: false);
		}
	}

	internal static void ReleaseResources()
	{
		for (int num = Snapshots.Count - 1; num >= 0; num--)
		{
			LensSnapshot lensSnapshot = Snapshots[num];
			if (lensSnapshot != null)
			{
				RestoreSourceMaterials(lensSnapshot);
				DestroySnapshotMaterials(lensSnapshot);
			}
		}
		Snapshots.Clear();
		LensByRenderer.Clear();
		PendingStashOptics.Clear();
		PendingStashOpticIds.Clear();
		_lastAllowedSnapshot = null;
		_pruneCursor = 0;
		_lastPruneFrame = -1;
		if (_opaqueNeutralTexture != null)
		{
			DestroyObject(_opaqueNeutralTexture);
			_opaqueNeutralTexture = null;
		}
	}

	private static bool TrackOptic(OpticSight opticSight, bool allowed, bool force)
	{
		if (opticSight == null || opticSight.LensRenderer == null)
		{
			return false;
		}
		OpticsVisited++;
		Renderer lensRenderer = opticSight.LensRenderer;
		if (LensByRenderer.TryGetValue(lensRenderer, out var value))
		{
			value.Optic = opticSight;
			ApplySnapshot(value, allowed, force);
			return true;
		}
		Material[] sharedMaterials;
		try
		{
			sharedMaterials = lensRenderer.sharedMaterials;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("could not read optic lens materials: " + ex);
			return false;
		}
		if (sharedMaterials == null || sharedMaterials.Length == 0)
		{
			return false;
		}
		bool flag = false;
		foreach (Material material in sharedMaterials)
		{
			if (material != null && (material.HasProperty("_CamTex") || material.HasProperty("_SwitchToSight")))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		Material[] array = new Material[sharedMaterials.Length];
		for (int j = 0; j < sharedMaterials.Length; j++)
		{
			Material material2 = sharedMaterials[j];
			if (material2 != null && (material2.HasProperty("_CamTex") || material2.HasProperty("_SwitchToSight")))
			{
				Material material3 = new Material(material2);
				material3.name = material2.name + " [HeldScopeOnly suppressed lens]";
				array[j] = material3;
			}
			else
			{
				array[j] = material2;
			}
		}
		LensSnapshot lensSnapshot = new LensSnapshot
		{
			Renderer = lensRenderer,
			Optic = opticSight,
			SourceMaterials = sharedMaterials,
			SuppressedMaterials = array
		};
		LensByRenderer[lensRenderer] = lensSnapshot;
		Snapshots.Add(lensSnapshot);
		CamTexRenderersTracked++;
		ApplySnapshot(lensSnapshot, allowed, force);
		return true;
	}

	private static bool TryGetSnapshot(OpticSight opticSight, out LensSnapshot snapshot)
	{
		snapshot = null;
		if (opticSight == null || opticSight.LensRenderer == null)
		{
			return false;
		}
		return LensByRenderer.TryGetValue(opticSight.LensRenderer, out snapshot);
	}

	private static void ApplySnapshot(LensSnapshot snapshot, bool allowed, bool force = false)
	{
		if (snapshot == null || snapshot.Renderer == null)
		{
			return;
		}
		int num = (allowed ? 1 : 0);
		if (!force && snapshot.LastAllowed == num)
		{
			return;
		}
		try
		{
			if (allowed)
			{
				snapshot.Renderer.sharedMaterials = snapshot.SourceMaterials;
				LensesRestored++;
				CamTexRenderersAllowed++;
			}
			else
			{
				SetSuppressedLensOpaque(snapshot.SuppressedMaterials);
				snapshot.Renderer.sharedMaterials = snapshot.SuppressedMaterials;
				LensesSuppressed++;
				CamTexRenderersSuppressed++;
			}
			snapshot.LastAllowed = num;
			FrameEnforcedOptics++;
		}
		catch (Exception ex)
		{
			snapshot.LastAllowed = -1;
			HeldScopeOnlyLog.WriteLine("could not apply optic lens render state: " + ex);
		}
	}

	private static void SetSuppressedLensOpaque(Material[] materials)
	{
		if (materials == null)
		{
			return;
		}
		Texture2D opaqueNeutralTexture = GetOpaqueNeutralTexture();
		foreach (Material material in materials)
		{
			if (!(material == null))
			{
				if (material.HasProperty("_CamTex"))
				{
					material.SetTexture("_CamTex", opaqueNeutralTexture);
					InteriorTexturePropertiesBlocked++;
				}
				if (material.HasProperty("_SwitchToSight"))
				{
					material.SetFloat("_SwitchToSight", 0.97f);
				}
			}
		}
	}

	private static Texture2D GetOpaqueNeutralTexture()
	{
		if (_opaqueNeutralTexture != null)
		{
			return _opaqueNeutralTexture;
		}
		_opaqueNeutralTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
		_opaqueNeutralTexture.name = "HeldScopeOnly opaque neutral lens";
		_opaqueNeutralTexture.filterMode = FilterMode.Bilinear;
		_opaqueNeutralTexture.wrapMode = TextureWrapMode.Clamp;
		_opaqueNeutralTexture.hideFlags = HideFlags.HideAndDontSave;
		_opaqueNeutralTexture.SetPixel(0, 0, new Color(0.06f, 0.065f, 0.06f, 1f));
		_opaqueNeutralTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
		return _opaqueNeutralTexture;
	}

	private static void RemoveSnapshotAt(int index)
	{
		LensSnapshot lensSnapshot = Snapshots[index];
		Snapshots.RemoveAt(index);
		if (lensSnapshot != null)
		{
			if (lensSnapshot == _lastAllowedSnapshot)
			{
				_lastAllowedSnapshot = null;
			}
			if (lensSnapshot.Renderer != null)
			{
				LensByRenderer.Remove(lensSnapshot.Renderer);
				RestoreSourceMaterials(lensSnapshot);
			}
			DestroySnapshotMaterials(lensSnapshot);
		}
	}

	private static void PruneDestroyedSnapshots()
	{
		int frameCount = Time.frameCount;
		if (_lastPruneFrame == frameCount || Snapshots.Count == 0)
		{
			return;
		}
		_lastPruneFrame = frameCount;
		int num = Math.Min(4, Snapshots.Count);
		while (num-- > 0)
		{
			if (_pruneCursor >= Snapshots.Count)
			{
				_pruneCursor = 0;
			}
			LensSnapshot lensSnapshot = Snapshots[_pruneCursor];
			if (lensSnapshot == null || lensSnapshot.Renderer == null)
			{
				RemoveSnapshotAt(_pruneCursor);
				if (_pruneCursor >= Snapshots.Count)
				{
					_pruneCursor = 0;
				}
				break;
			}
			_pruneCursor++;
			if (_pruneCursor >= Snapshots.Count)
			{
				_pruneCursor = 0;
			}
		}
	}

	private static void RestoreSourceMaterials(LensSnapshot snapshot)
	{
		if (snapshot == null || snapshot.Renderer == null || snapshot.SourceMaterials == null)
		{
			return;
		}
		try
		{
			snapshot.Renderer.sharedMaterials = snapshot.SourceMaterials;
			snapshot.LastAllowed = 1;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("could not restore optic lens source materials: " + ex);
		}
	}

	private static void DestroySnapshotMaterials(LensSnapshot snapshot)
	{
		if (snapshot?.SuppressedMaterials == null || snapshot.SourceMaterials == null)
		{
			return;
		}
		for (int i = 0; i < snapshot.SuppressedMaterials.Length; i++)
		{
			Material material = snapshot.SuppressedMaterials[i];
			Material material2 = ((i < snapshot.SourceMaterials.Length) ? snapshot.SourceMaterials[i] : null);
			if (material != null && (object)material != material2)
			{
				DestroyObject(material);
			}
		}
	}

	private static void DestroyObject(UnityEngine.Object value)
	{
		if (!(value == null))
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(value);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(value);
			}
		}
	}
}
