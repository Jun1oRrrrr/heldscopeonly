# HeldScopeOnly

## Goal

Render picture-in-picture and scope-mask content only for the scope mode that
is mounted on the local player's held weapon.

The plugin rejects scope events from AI, dropped weapons, other players, Fika
remote players, and hideout weapon-rack display weapons. Fika does not need a
protocol change. Every graphical client must install the DLL locally.

## Target

- Game: `D:\EFT\SPT4.1.6`
- Game assembly:
  `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
- Runtime plugin:
  `BepInEx\plugins\HeldScopeOnly.dll`

## Installed artifact

- Plugin version: `1.7.0`
- BepInEx GUID: `local.heldscopeonly`
- SHA256:
  `97AEC4A13A0019BE4884B8448F5E96ED0A4202EBA7A34A89CFC883ED9837ECE7`

The experimental `1.8.0` build is preserved as `HeldScopeOnly.1.8.0.dll` and
`HeldScopeOnly.1.8.0.cs` but is not installed. Older known-good and
experimental builds are retained as `HeldScopeOnly.1.5.0.dll` and
`HeldScopeOnly.1.6.0.dll`.

Each graphical Fika client must install `1.7.0` locally. The dedicated server
and Fika headless client do not need this DLL. Installing it only on the
observer cannot suppress scope rendering that is performed in another client
process.

## 1.8.0 regression

Runtime testing on 2026-10-19 confirmed that remote-player scope suppression
worked, but the local player's own held scope rendered without an image. The
`1.8.0` build is therefore rolled back and must not be installed.

The likely regression area is the new ground-loot filter interacting with the
local held optic lifecycle. Do not resume `1.8.0` development from this build.
Reproduce the failure from the preserved source and add a local-held-scope
regression check before deployment.

## 1.8.0 changes (inactive)

- Added postfixes for `LootItem.Init(Item, string, GameWorld, bool, MongoID[],
  string, bool)` and `LootItem.SetItemAndRigidbody(Item, Rigidbody)`.
- Applies the ground-loot filter to normal dropped weapons, static loot, Fika
  or network `ObservedLootItem` instances, and pooled objects rebuilt through
  `SetItemAndRigidbody`.
- For a ground weapon or ground scope mod, forces `OpticSight.LensFade(true)`,
  disables `OpticSight` and `CollimatorSight`, and blocks
  `OpticSight.OnEnable/OnDisable` so a ground scope cannot publish the global
  optic state or enter the PiP path.
- Blocks ground optics in `OpticCameraManager.OnOpticSightEnabled`,
  `OpticRetrice.SetOpticSight/UpdateTransform`, `ScopeMaskRenderer.DrawOptic`,
  and the collimator draw paths.
- Added `ground-loot-hidden=`, `ground-optic-found=`,
  `ground-lens-hidden=`, `ground-optic-disabled=`, and
  `ground-optic-enable-block=` diagnostics.
- Was intended to preserve the local held-scope path and the 1.7.0 Fika
  remote-player suppression. Runtime testing disproved that assumption for the
  local held scope.

## 1.7.0 changes

- Added remote-player registration patches for
  `ObservedPlayerView.SetWeaponRoot`, `ObservedPlayerView.UpdateWeaponRoot`,
  `ObservedPlayerView.Initialization`, and
  `BundleAnimationBones.ChangeHandsItem`.
- Added both prefix and postfix handling to
  `Firearms.UpdateModsVisualControllers`, so remote weapon roots are captured
  and remote optics are hidden without depending on Fika prefix ordering.
- Added a hierarchy fallback that resolves `ObservedPlayerView` from an optic
  transform when the registered remote weapon root is not hit first.
- Hides all remote `ScopePrefabCache` mode objects, disables remote
  `OpticSight` instances, and forces remote lens fade before their scope
  renderers can draw.
- Added `fika-remote-register`, `fika-remote-view-fallback`,
  `fika-remote-optic-found`, `fika-remote-lens-hidden`,
  `fika-remote-optic-disabled`, `fika-remote-sight-controller-block`, and
  `fika-remote-scope-mode-block` diagnostics.
- Keeps the local held scope as the only enabled scope path.

## 1.5.0 changes

- Added a prefix for `OpticRetrice.SetOpticSight(OpticSight)`.
  - Allows the null cleanup path.
  - Allows only the local held scope to set the shared reticle renderer.
  - Keeps remote, AI, and hideout optics from changing `_canUpdate`,
    `sharedMesh`, or the shared reticle GameObject state.
- Added a prefix for `OpticRetrice.UpdateTransform(OpticSight)`.
  - Allows the local held scope to update reticle position, rotation, and
    scale.
  - Rejects every other scope once the local current scope is known.
- Added `reticle-check=` and `reticle-block=` diagnostics with the scope
  instance, owner, local player, and block reason.
- Kept fail-open behavior when local player or local current-scope state is
  not readable yet, so local scope initialization is not disabled.
- Fast-path reference comparison now happens before component-parent lookup,
  avoiding two `GetComponentInParent` calls for every local reticle update.

## 1.4.0 changes

- Removed the per-draw `ProceduralWeaponAnimation.CurrentScope` access that
  threw `ArgumentOutOfRangeException` when the held weapon had no valid scope.
  The new path checks `ScopeAimTransforms` and `AimIndex` before indexing.
- Replaced the repeated `GetScopeModeInfo` reflection call with direct reads
  from `_scopeModeInfos`, `_currentModeId`, `OpticSight`, and
  `CollimatorSight`.
- Added a per-frame local-scope cache and a per-mode optic/collimator cache so
  the three draw paths do not resolve the same state repeatedly.
- Reduced high-frequency draw-block logging while retaining final counters in
  the plugin-destroyed summary.

## Patches

1. `CameraManager.SetPlayer(Player)` postfix
   - Caches only the real local player.
   - Confirms the candidate against `GamePlayerOwner.MyPlayer`.

2. `OpticCameraManager.OnOpticSightEnabled(OpticSight)` prefix
   - Rejects hideout weapon-rack optics.
   - Rejects optics owned by AI or another player.
   - Accepts only the local player's current scope mode.

3. `OpticSight.LensFade(bool)` prefix
   - Forces `LensFade(true)` for unwanted optics.
   - Leaves an explicit hide request alone.

4. `OpticSight.OnEnable()` prefix
   - Skips the complete vanilla enable path for weapon-rack and ground-loot
     optics.
   - This prevents a ground scope from publishing the global optic event or
     entering the PiP path.

5. `OpticSight.OnDisable()` prefix
   - Skips the vanilla disable path for weapon-rack and ground-loot optics.
   - This prevents a display or ground scope from clearing the local held
     scope.

6. `ScopeMaskRenderer.DrawOptic(CommandBuffer)` prefix
   - Draws only when the renderer's current optic is the local held optic.

7. `ScopeMaskRenderer.DrawCollimators(CommandBuffer, Camera)` prefix
   - Replaces the vanilla global `_collimatorSights` loop.
   - Draws only the local held scope's current collimator.

8. `ScopeMaskRenderer.DrawActiveCollimator(CommandBuffer)` prefix
   - Draws only when the active collimator is the local held collimator.

9. `OpticRetrice.UpdateTransform(OpticSight)` prefix
   - Rejects reticle transform updates from remote players, AI, dropped
     weapons, and hideout display weapons.

10. `OpticRetrice.SetOpticSight(OpticSight)` prefix
    - Rejects remote or non-local scopes before they can replace the shared
      reticle renderer. The null cleanup call remains allowed.

11. `StashItemModel.Init(GameObject)` postfix
   - Handles `StashWeaponItemModel` and
     `CircleOfCultistsStashWeaponItemModel`.
   - Calls `LensFade(true)` for each display optic.
   - Disables the display `OpticSight` component after initialization.

12. `Firearms.UpdateModsVisualControllers()` prefix and postfix
    - Registers each remote player's weapon hierarchy.
    - Hides remote scope mode objects and remote `OpticSight` components after
      Fika rebuilds the held item.

13. Remote `ObservedPlayerView` postfixes
    - Registers the player transform, `WeaponRoot.Original`, and rebuilt
      weapon roots.

14. `BundleAnimationBones.ChangeHandsItem(WeaponPrefab)` postfix
    - Registers the remote weapon root immediately after a hands-item rebuild
      and hides its optics.

15. `LootItem.Init(...)` postfix
    - Covers normal drops, static loot, and Fika/network `ObservedLootItem`
      creation.
    - Hides and disables every ground `OpticSight` and `CollimatorSight` after
      initialization.

16. `LootItem.SetItemAndRigidbody(Item, Rigidbody)` postfix
    - Reapplies the ground filter when an object-pooled loot item is rebound
      to a new item.
    - Prevents a reused loot hierarchy from retaining an enabled optic from a
      previous item.

`OnOpticSightDisabled` is intentionally not used as the primary filter because
vanilla cleanup still needs to clear cameras and render textures.

## Critical restart requirement

BepInEx loads plugins only when the game process starts. Replacing the DLL
while the game or launcher is running has no effect on that process.

After deployment:

1. Exit Escape From Tarkov.
2. Exit the launcher completely.
3. Start a fresh game process.
4. Enter the hideout and inspect the weapon rack.

The previous runtime log was written by an older build; it cannot prove
whether the current `1.8.0` build was loaded.

## Verification after restart

`BepInEx\LogOutput.log` must contain the plugin-load line, and the plugin-owned
log `HeldScopeOnly.log` (in the game root) must contain:

```text
HeldScopeOnly 1.8.0 loaded
```

When the hideout weapon rack is initialized, `HeldScopeOnly.log` should contain
one or more of:

```text
stash-lens-hidden=
stash-optic-enable-block=
stash-optic-disable-block=
```

During normal scope drawing, the log may also contain diagnostics such as:

```text
reticle-check=
reticle-block=
draw-optic-block=
draw-collimator-block=
draw-active-collimator-block=
```

When another Fika client exposes a non-local scope, the expected diagnostics
include:

```text
fika-remote-register=
fika-remote-view-fallback=
fika-remote-optic-found=
fika-remote-lens-hidden=
```

One or more of these should also appear after Fika rebuilds a remote weapon:

```text
fika-remote-optic-disabled=
fika-remote-sight-controller-block=
fika-remote-scope-mode-block=
```

After dropping a weapon, the ground weapon should remain visible, but its
scope lens, PiP image, reticle, and collimator must not render. The log should
contain one or more of:

```text
ground-loot-hidden=
ground-optic-found=
ground-lens-hidden=
ground-optic-disabled=
ground-optic-enable-block=
```

The same ground optic must not then produce an allowed reticle update:

```text
reticle-check=... ALLOW
```

For a ground optic, a blocked diagnostic is expected instead:

```text
reticle-block=... reason="reticle optic belongs to a ground loot weapon"
```

Ground-loot regression steps:

1. Enter a raid with a scoped weapon and confirm the held scope renders
   normally.
2. Drop the weapon, move a few meters away, look at it, approach it, and look
   through the scope model again.
3. Confirm the weapon body and lens glass remain visible while the scope image,
   reticle, and collimator behavior are absent.
4. Drop a second scoped weapon after picking the first one back up. This
   checks pooled `LootItem` reuse and `SetItemAndRigidbody`.
5. Equip one of the dropped weapons again and confirm the local held scope
   renders normally after it leaves the `LootItem` hierarchy.
6. Repeat the drop/observe sequence while connected to Fika and verify that
   each graphical client reports its own `ground-*` diagnostics. A client
   without `1.8.0` can still render the scope in its own process.

For AI or a hideout weapon, the expected reticle diagnostic is:

```text
reticle-block=... entry=SetOpticSight|UpdateTransform
reason="optic belongs to a different player"
```

or, when no owner is exposed:

```text
reticle-block=... entry=SetOpticSight|UpdateTransform
reason="optic has no Player owner and is not the local current optic"
```

The log should not contain a new `ArgumentOutOfRangeException` from
`HeldScopeOnlyState.TryGetCurrentSights`. That exception was expected only in
the old `1.3.0` implementation.

Quick check:

```powershell
Select-String `
  -Path 'D:\EFT\SPT4.1.6\BepInEx\LogOutput.log' `
  -Pattern 'HeldScopeOnly|local.heldscopeonly'

Get-Content `
  -Path 'D:\EFT\SPT4.1.6\HeldScopeOnly.log' `
  -Tail 120
```

If the log does not say `1.8.0`, the new DLL was not loaded by that process.
Verify this line independently on every graphical client before judging
whether the remote-scope suppression worked.

## Build

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' `
  /nologo /target:library `
  /out:'D:\EFT\SPT4.1.6\BepInEx\plugins\HeldScopeOnly.dll' `
  /r:'D:\EFT\SPT4.1.6\BepInEx\core\0Harmony.dll' `
  /r:'D:\EFT\SPT4.1.6\BepInEx\core\BepInEx.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\netstandard.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\UnityEngine.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\UnityEngine.CoreModule.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\UnityEngine.PhysicsModule.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\DissonanceVoip.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\ItemTemplate.Types.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\ItemComponent.Types.dll' `
  /r:'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll' `
  /r:'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Core.dll' `
  'D:\EFT\SPT4.1.6\backups\held_scope_only_20261016\HeldScopeOnly.cs'
```

## Static verification

```powershell
& 'D:\EFT\SPT4.1.6\backups\held_scope_only_20261016\VerifyHeldScopeOnly.1.8.0.exe' `
  'D:\EFT\SPT4.1.6\EscapeFromTarkov_Data\Managed' `
  'D:\EFT\SPT4.1.6\BepInEx\plugins\HeldScopeOnly.dll'
```

Expected result:

```text
VERIFY PASS
```

The verifier confirms the target methods and fields exist and that the built
plugin contains the expected patch entry points and diagnostic strings. It
also rejects a build that calls `ProceduralWeaponAnimation::get_CurrentScope`
or `MethodInfo::Invoke`.

## Rollback

Delete:

```text
D:\EFT\SPT4.1.6\BepInEx\plugins\HeldScopeOnly.dll
D:\EFT\SPT4.1.6\HeldScopeOnly.log
```

Then restart the game.

To restore the previously installed build, replace the installed DLL with
`HeldScopeOnly.1.7.0.dll` from this directory before restarting.
