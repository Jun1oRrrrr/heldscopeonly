using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.CameraControl;
using HarmonyLib;
using UnityEngine;

namespace HeldScopeOnly;

internal static class ScopeMaskRendererPatchState
{
	private static FieldInfo _scopeModeInfosField;

	private static FieldInfo _currentModeIdField;

	private static FieldInfo _scopeModeOpticSightField;

	private static FieldInfo _scopeModeCollimatorSightField;

	private static FieldInfo _collimatorMaterialField;

	private static FieldInfo _activeCollimatorSightField;

	private static ScopePrefabCache _cachedModeCache;

	private static int _cachedModeId;

	private static bool _cachedModeValid;

	private static OpticSight _cachedModeOpticSight;

	private static CollimatorSight _cachedModeCollimatorSight;

	internal static float MaxCollimatorSqrMagnitude;

	internal static int ColorPropertyId;

	internal static Color CollimatorColor;

	internal static void Initialize()
	{
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		_scopeModeInfosField = (FieldInfo)(object)AccessTools.Field((Type)(object)typeof(ScopePrefabCache), "_scopeModeInfos");
		_currentModeIdField = (FieldInfo)(object)AccessTools.Field((Type)(object)typeof(ScopePrefabCache), "_currentModeId");
		if (_scopeModeInfosField == null || _currentModeIdField == null)
		{
			throw new MissingFieldException(typeof(ScopePrefabCache).FullName, "_scopeModeInfos");
		}
		Type elementType = _scopeModeInfosField.FieldType.GetElementType();
		if (elementType == null)
		{
			throw new MissingFieldException(typeof(ScopePrefabCache).FullName, "_scopeModeInfos element type");
		}
		_scopeModeOpticSightField = (FieldInfo)(object)AccessTools.Field((Type)(object)elementType, "OpticSight");
		_scopeModeCollimatorSightField = (FieldInfo)(object)AccessTools.Field((Type)(object)elementType, "CollimatorSight");
		if (_scopeModeOpticSightField == null || _scopeModeCollimatorSightField == null)
		{
			throw new MissingFieldException(elementType.FullName, "OpticSight/CollimatorSight");
		}
		_collimatorMaterialField = (FieldInfo)(object)AccessTools.Field((Type)(object)typeof(ScopeMaskRenderer), "_collimatorMaskMeterial");
		_activeCollimatorSightField = (FieldInfo)(object)AccessTools.Field((Type)(object)typeof(ScopeMaskRenderer), "_activeCollimatorSight");
		FieldInfo fieldInfo = (FieldInfo)(object)AccessTools.Field((Type)(object)typeof(ScopeMaskRenderer), "_maxCollimatorSqrMagnitude");
		FieldInfo fieldInfo2 = (FieldInfo)(object)AccessTools.Field((Type)(object)typeof(ScopeMaskRenderer), "_color");
		FieldInfo fieldInfo3 = FindCollimatorColorField();
		if (_collimatorMaterialField == null || _activeCollimatorSightField == null || fieldInfo == null || fieldInfo2 == null || fieldInfo3 == null)
		{
			throw new MissingFieldException(typeof(ScopeMaskRenderer).FullName, "scope-mask collimator fields");
		}
		MaxCollimatorSqrMagnitude = Convert.ToSingle(fieldInfo.GetValue(null));
		ColorPropertyId = Convert.ToInt32(fieldInfo2.GetValue(null));
		CollimatorColor = (Color)fieldInfo3.GetValue(null);
	}

	internal static Material CollimatorMaterial(ScopeMaskRenderer renderer)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		return (Material)_collimatorMaterialField.GetValue(renderer);
	}

	internal static CollimatorSight ActiveCollimatorSight(ScopeMaskRenderer renderer)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		return (CollimatorSight)_activeCollimatorSightField.GetValue(renderer);
	}

	internal static bool TryResolveScopeMode(ScopePrefabCache cache, out OpticSight opticSight, out CollimatorSight collimatorSight, out string reason)
	{
		opticSight = null;
		collimatorSight = null;
		reason = null;
		if ((Object)(object)cache == (Object)null)
		{
			reason = "local current scope has no prefab cache";
			return true;
		}
		int num;
		try
		{
			num = Convert.ToInt32(_currentModeIdField.GetValue(cache));
		}
		catch (Exception ex)
		{
			reason = "could not read local current scope mode: " + ex;
			return false;
		}
		if (_cachedModeValid && object.ReferenceEquals(_cachedModeCache, cache) && _cachedModeId == num)
		{
			opticSight = _cachedModeOpticSight;
			collimatorSight = _cachedModeCollimatorSight;
			reason = "local current scope mode resolved";
			return true;
		}
		if (!(_scopeModeInfosField.GetValue(cache) is Array array) || num < 0 || num >= array.Length)
		{
			reason = "local current scope mode is invalid";
			return false;
		}
		object value = array.GetValue(num);
		if (value == null)
		{
			reason = "local current scope mode data is unavailable";
			return false;
		}
		object value2 = _scopeModeOpticSightField.GetValue(value);
		opticSight = (OpticSight)((value2 is OpticSight) ? value2 : null);
		object value3 = _scopeModeCollimatorSightField.GetValue(value);
		collimatorSight = (CollimatorSight)((value3 is CollimatorSight) ? value3 : null);
		_cachedModeCache = cache;
		_cachedModeId = num;
		_cachedModeValid = true;
		_cachedModeOpticSight = opticSight;
		_cachedModeCollimatorSight = collimatorSight;
		reason = "local current scope mode resolved";
		return true;
	}

	private static FieldInfo FindCollimatorColorField()
	{
		List<FieldInfo> declaredFields = (List<FieldInfo>)(object)AccessTools.GetDeclaredFields((Type)(object)typeof(ScopeMaskRenderer));
		for (int i = 0; i < declaredFields.Count; i++)
		{
			FieldInfo fieldInfo = declaredFields[i];
			if (!(fieldInfo.FieldType != typeof(Color)) && !(fieldInfo.Name == "_activeCollimatorColor") && fieldInfo.Name.EndsWith("ollimatorColor", StringComparison.Ordinal))
			{
				return fieldInfo;
			}
		}
		return null;
	}
}
