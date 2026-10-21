using System;
using System.Collections.Generic;
using EFT.CameraControl;
using UnityEngine;

namespace HeldScopeOnly;

internal static class OpticRenderPolicy
{
	private const string CameraTextureProperty = "_CamTex";

	private const string SwitchToSightProperty = "_SwitchToSight";

	private sealed class LensSnapshot
	{
		internal Renderer Renderer;

		internal OpticSight Optic;

		internal Material[] SourceMaterials;

		internal Material[] SuppressedMaterials;

		internal int LastAllowed = -1;
	}

	private static readonly Dictionary<Renderer, LensSnapshot> LensByRenderer = new Dictionary<Renderer, LensSnapshot>();

	private static readonly List<LensSnapshot> Snapshots = new List<LensSnapshot>();

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
		if ((Object)(object)root == (Object)null)
		{
			return;
		}
		HierarchyScans++;
		try
		{
			OpticSight[] optics = ((Component)root).GetComponentsInChildren<OpticSight>(true);
			for (int i = 0; i < optics.Length; i++)
			{
				TrackOptic(optics[i]);
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

	internal static void ScanLootItem(EFT.Interactive.LootItem item, string reason)
	{
		if ((Object)(object)item == (Object)null)
		{
			return;
		}
		ScanHierarchy(((Component)item).transform, reason);
	}

	internal static void EnforceBeforeCameraRender(Camera camera, string reason)
	{
		FrameEnforcementPasses++;
		if (Snapshots.Count == 0)
		{
			return;
		}

		LensSnapshot desiredSnapshot = null;
		if (TryGetAllowedLocalOptic(out var allowedOptic))
		{
			TryGetSnapshot(allowedOptic, out desiredSnapshot);
		}

		if (!object.ReferenceEquals(desiredSnapshot, _lastAllowedSnapshot))
		{
			LensSnapshot previousSnapshot = _lastAllowedSnapshot;
			_lastAllowedSnapshot = desiredSnapshot;
			if (previousSnapshot != null)
			{
				ApplySnapshot(previousSnapshot, allowed: false);
			}
			if (desiredSnapshot != null)
			{
				ApplySnapshot(desiredSnapshot, allowed: true);
			}
		}

		PruneDestroyedSnapshots();
	}

	internal static bool TryGetAllowedLocalOptic(out OpticSight opticSight)
	{
		PolicyChecks++;
		opticSight = null;
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var currentOptic, out var _, out var _))
		{
			PolicyBlocked++;
			return false;
		}
		if ((Object)(object)currentOptic == (Object)null)
		{
			PolicyBlocked++;
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var opticMode) || !aiming || !opticMode)
		{
			PolicyBlocked++;
			return false;
		}
		if (!((Behaviour)currentOptic).isActiveAndEnabled || (Object)(object)currentOptic.LensRenderer == (Object)null)
		{
			PolicyBlocked++;
			return false;
		}
		opticSight = currentOptic;
		PolicyAllowed++;
		return true;
	}

	internal static bool TryGetAllowedLocalCollimator(out CollimatorSight collimatorSight)
	{
		collimatorSight = null;
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var _, out var currentCollimator, out var _))
		{
			return false;
		}
		if ((Object)(object)currentCollimator == (Object)null)
		{
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var _) || !aiming)
		{
			return false;
		}
		if (!((Behaviour)currentCollimator).isActiveAndEnabled || (Object)(object)currentCollimator.CollimatorMeshRenderer == (Object)null)
		{
			return false;
		}
		collimatorSight = currentCollimator;
		return true;
	}

	internal static bool IsCurrentLocalOptic(OpticSight opticSight)
	{
		PolicyChecks++;
		if ((Object)(object)opticSight == (Object)null)
		{
			PolicyBlocked++;
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var currentOptic, out var _, out var _) || !object.ReferenceEquals(currentOptic, opticSight))
		{
			PolicyBlocked++;
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalOpticAiming(out var aiming, out var opticMode) || !aiming || !opticMode)
		{
			PolicyBlocked++;
			return false;
		}
		if (!((Behaviour)opticSight).isActiveAndEnabled || (Object)(object)opticSight.LensRenderer == (Object)null)
		{
			PolicyBlocked++;
			return false;
		}
		PolicyAllowed++;
		return true;
	}

	internal static bool IsCurrentLocalHeldOptic(OpticSight opticSight)
	{
		if ((Object)(object)opticSight == (Object)null)
		{
			return false;
		}
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var currentOptic, out var _, out var _) || !object.ReferenceEquals(currentOptic, opticSight))
		{
			return false;
		}
		if ((Object)(object)opticSight.LensRenderer == (Object)null)
		{
			return false;
		}
		return true;
	}

	internal static void ApplyOpticState(OpticSight opticSight, string reason)
	{
		TrackOptic(opticSight);
		if ((Object)(object)opticSight == (Object)null)
		{
			return;
		}
		bool allowed = IsCurrentLocalOptic(opticSight);
		if (!TryGetSnapshot(opticSight, out var snapshot))
		{
			return;
		}
		ApplySnapshot(snapshot, allowed);
	}

	internal static void SuppressOptic(OpticSight opticSight, string reason)
	{
		TrackOptic(opticSight);
		if (TryGetSnapshot(opticSight, out var snapshot))
		{
			ApplySnapshot(snapshot, allowed: false);
		}
	}

	internal static void RestoreOptic(OpticSight opticSight, string reason)
	{
		TrackOptic(opticSight);
		if (TryGetSnapshot(opticSight, out var snapshot))
		{
			ApplySnapshot(snapshot, IsCurrentLocalOptic(opticSight));
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
		if (!HeldScopeOnlyState.TryGetLocalCurrentSights(out var opticSight, out var _, out var _))
		{
			return;
		}
		if ((Object)(object)opticSight != (Object)null && TryGetSnapshot(opticSight, out var snapshot))
		{
			ApplySnapshot(snapshot, allowed: false);
		}
	}

	internal static void ReleaseResources()
	{
		for (int i = Snapshots.Count - 1; i >= 0; i--)
		{
			LensSnapshot snapshot = Snapshots[i];
			if (snapshot != null)
			{
				RestoreSourceMaterials(snapshot);
				DestroySnapshotMaterials(snapshot);
			}
		}
		Snapshots.Clear();
		LensByRenderer.Clear();
		_lastAllowedSnapshot = null;
		_pruneCursor = 0;
		_lastPruneFrame = -1;
		if ((Object)(object)_opaqueNeutralTexture != (Object)null)
		{
			DestroyObject(_opaqueNeutralTexture);
			_opaqueNeutralTexture = null;
		}
	}

	private static void TrackOptic(OpticSight opticSight)
	{
		if ((Object)(object)opticSight == (Object)null || (Object)(object)opticSight.LensRenderer == (Object)null)
		{
			return;
		}
		OpticsVisited++;
		Renderer renderer = opticSight.LensRenderer;
		if (LensByRenderer.TryGetValue(renderer, out var existing))
		{
			existing.Optic = opticSight;
			ApplySnapshot(existing, IsCurrentLocalOptic(opticSight));
			return;
		}
		Material[] sourceMaterials;
		try
		{
			sourceMaterials = renderer.sharedMaterials;
		}
		catch (Exception ex)
		{
			HeldScopeOnlyLog.WriteLine("could not read optic lens materials: " + ex);
			return;
		}
		if (sourceMaterials == null || sourceMaterials.Length == 0)
		{
			return;
		}
		bool hasInteriorProperty = false;
		for (int i = 0; i < sourceMaterials.Length; i++)
		{
			Material material = sourceMaterials[i];
			if ((Object)(object)material != (Object)null && (material.HasProperty(CameraTextureProperty) || material.HasProperty(SwitchToSightProperty)))
			{
				hasInteriorProperty = true;
				break;
			}
		}
		if (!hasInteriorProperty)
		{
			return;
		}
		Material[] suppressedMaterials = new Material[sourceMaterials.Length];
		for (int j = 0; j < sourceMaterials.Length; j++)
		{
			Material source = sourceMaterials[j];
			if ((Object)(object)source != (Object)null && (source.HasProperty(CameraTextureProperty) || source.HasProperty(SwitchToSightProperty)))
			{
				Material clone = new Material(source);
				clone.name = source.name + " [HeldScopeOnly suppressed lens]";
				suppressedMaterials[j] = clone;
			}
			else
			{
				suppressedMaterials[j] = source;
			}
		}
		LensSnapshot snapshot = new LensSnapshot
		{
			Renderer = renderer,
			Optic = opticSight,
			SourceMaterials = sourceMaterials,
			SuppressedMaterials = suppressedMaterials
		};
		LensByRenderer[renderer] = snapshot;
		Snapshots.Add(snapshot);
		CamTexRenderersTracked++;
		ApplySnapshot(snapshot, IsCurrentLocalOptic(opticSight));
	}

	private static bool TryGetSnapshot(OpticSight opticSight, out LensSnapshot snapshot)
	{
		snapshot = null;
		if ((Object)(object)opticSight == (Object)null || (Object)(object)opticSight.LensRenderer == (Object)null)
		{
			return false;
		}
		return LensByRenderer.TryGetValue(opticSight.LensRenderer, out snapshot);
	}

	private static void ApplySnapshot(LensSnapshot snapshot, bool allowed)
	{
		if (snapshot == null || (Object)(object)snapshot.Renderer == (Object)null)
		{
			return;
		}
		int desired = (allowed ? 1 : 0);
		if (snapshot.LastAllowed == desired)
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
			snapshot.LastAllowed = desired;
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
		Texture2D texture = GetOpaqueNeutralTexture();
		for (int i = 0; i < materials.Length; i++)
		{
			Material material = materials[i];
			if ((Object)(object)material == (Object)null)
			{
				continue;
			}
			if (material.HasProperty(CameraTextureProperty))
			{
				material.SetTexture(CameraTextureProperty, (Texture)(object)texture);
				InteriorTexturePropertiesBlocked++;
			}
			if (material.HasProperty(SwitchToSightProperty))
			{
				material.SetFloat(SwitchToSightProperty, 0.97f);
			}
		}
	}

	private static Texture2D GetOpaqueNeutralTexture()
	{
		if ((Object)(object)_opaqueNeutralTexture != (Object)null)
		{
			return _opaqueNeutralTexture;
		}
		_opaqueNeutralTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		_opaqueNeutralTexture.name = "HeldScopeOnly opaque neutral lens";
		_opaqueNeutralTexture.filterMode = FilterMode.Bilinear;
		_opaqueNeutralTexture.wrapMode = TextureWrapMode.Clamp;
		_opaqueNeutralTexture.hideFlags = HideFlags.HideAndDontSave;
		_opaqueNeutralTexture.SetPixel(0, 0, new Color(0.06f, 0.065f, 0.06f, 1f));
		_opaqueNeutralTexture.Apply(false, false);
		return _opaqueNeutralTexture;
	}

	private static void RemoveSnapshotAt(int index)
	{
		LensSnapshot snapshot = Snapshots[index];
		Snapshots.RemoveAt(index);
		if (snapshot == null)
		{
			return;
		}
		if (object.ReferenceEquals(snapshot, _lastAllowedSnapshot))
		{
			_lastAllowedSnapshot = null;
		}
		if ((Object)(object)snapshot.Renderer != (Object)null)
		{
			LensByRenderer.Remove(snapshot.Renderer);
			RestoreSourceMaterials(snapshot);
		}
		DestroySnapshotMaterials(snapshot);
	}

	private static void PruneDestroyedSnapshots()
	{
		int frameCount = Time.frameCount;
		if (_lastPruneFrame == frameCount || Snapshots.Count == 0)
		{
			return;
		}
		_lastPruneFrame = frameCount;
		int checksRemaining = Math.Min(4, Snapshots.Count);
		while (checksRemaining-- > 0)
		{
			if (_pruneCursor >= Snapshots.Count)
			{
				_pruneCursor = 0;
			}
			LensSnapshot snapshot = Snapshots[_pruneCursor];
			if (snapshot == null || (Object)(object)snapshot.Renderer == (Object)null)
			{
				RemoveSnapshotAt(_pruneCursor);
				if (_pruneCursor >= Snapshots.Count)
				{
					_pruneCursor = 0;
				}
				return;
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
		if (snapshot == null || (Object)(object)snapshot.Renderer == (Object)null || snapshot.SourceMaterials == null)
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
			Material suppressed = snapshot.SuppressedMaterials[i];
			Material source = (i < snapshot.SourceMaterials.Length) ? snapshot.SourceMaterials[i] : null;
			if ((Object)(object)suppressed != (Object)null && !object.ReferenceEquals(suppressed, source))
			{
				DestroyObject(suppressed);
			}
		}
	}

	private static void DestroyObject(Object value)
	{
		if ((Object)(object)value == (Object)null)
		{
			return;
		}
		if (Application.isPlaying)
		{
			Object.Destroy(value);
		}
		else
		{
			Object.DestroyImmediate(value);
		}
	}
}
