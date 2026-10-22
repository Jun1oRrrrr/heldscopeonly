# HeldScopeOnly

## Goal

Render picture-in-picture and scope-mask content only for the optic mounted on
the local player's held weapon.

AI, Fika remote players, hideout weapon-rack displays, and other non-local
optics must keep their normal external model, mount, tube, and lens glass while
their scope interior, reticle, collimator, and camera work are suppressed.

No Fika protocol change is required. Every graphical client must install the
same DLL locally. The Fika dedicated server and headless client do not need it.

## Target

- SPT version: `4.1.6`
- Runtime: BepInEx
- Plugin assembly: `HeldScopeOnly.1.9.2.dll`

## Installed Artifact

- Plugin version: `1.9.2`
- BepInEx GUID: `local.heldscopeonly`
- SHA256:
  `2CF2164B8EA546561F771B8317704B8076F31B9A73F0709995E5585C55EE73CE`
- File size: `59392` bytes

The repository contains the complete plugin source. The compiled DLL is
distributed as a release asset.

All `1.6`, `1.7`, `1.8`, `1.9.0`, and `1.9.1` builds are superseded and
should not be deployed.

## 1.9.2 Changes

- Registers every `StashItemModel.Init` call before any weapon-subclass filter,
  so all hideout-displayed optics use the same suppression path.
- Collects every optic below a registered stash model, including its prefab,
  and retries lenses whose renderer or interior materials bind late.
- Limits late-lens retries to four candidates per update with bounded attempt
  and frame windows, avoiding an unbounded hierarchy scan.
- Bypasses the normal material-state shortcut for hideout optics and reapplies
  opaque neutral lens materials when vanilla rebuilds the renderer materials.
- Keeps the `1.9.1` local aiming reset, remote/AI external-lens behavior, and
  dropped-weapon suppression intact.

- Clears the local optic aiming state immediately on both the frame-polling
  path and the `FirearmController.IsAiming` setter path.
- Fixes the grenade-to-primary-weapon sequence leaving the previous aiming
  state stuck and preventing the local scope interior from being restored.
- Detects a missing DynamicExternalResolution installation only once instead of
  retrying the restore hook on a fixed frame interval.
- Checks only the currently allowed local optic before each camera render.
  Previously tracked lenses are not traversed on every frame; destroyed-lens
  cleanup is spread across frames.
- Keeps external lens glass visible on AI, Fika remote players, dropped
  weapons, and hideout displays while suppressing every non-local scope
  interior.

## Runtime Rules

Local held scope:

- The scope interior renders only while the local player is aiming through the
  currently held optic.
- Scope mode changes use the vanilla visual selection path.
- Closing a scope restores DynamicExternalResolution.

AI and Fika remote players:

- External model, mount, tube, selected scope mode, and lens glass remain
  visible.
- Scope camera, picture-in-picture content, mask, reticle, and collimator
  rendering remain suppressed.
- Remote `OpticSight` lifecycle events do not replace the local player's shared
  camera or reticle state.

Hideout weapon rack:

- Every descendant optic is registered from `StashItemModel.Init`.
- Display optic interior and camera rendering remain suppressed while the
  external optic stays intact.
- Late lens bindings are retried only within a fixed-size candidate batch.

## Patches

1. `CameraManager.SetPlayer(Player)` postfix
   - Caches only the real local player.

2. `OpticCameraManager.OnOpticSightEnabled(OpticSight)` prefix
   - Accepts only the local held scope.
   - Rejects AI, remote-player, and hideout optics.

3. `OpticSight.LensFade(bool)` prefix
   - Forces non-local lens requests to `LensFade(false)` so the external lens is
     visible.
   - Continues to hide other unwanted optics that are not AI or remote-player
     held scopes.

4. `OpticSight.OnEnable()` prefix
   - For non-local optics, shows the external lens and skips the vanilla global
     optic publication.
   - Blocks hideout display optic enable behavior.

5. `OpticSight.OnDisable()` prefix
   - For non-local optics, keeps the external lens visible instead of fading it
     out.
   - Skips the vanilla global optic publication.
   - Blocks hideout display optic disable behavior.

6. `ScopeMaskRenderer.DrawOptic(CommandBuffer)` prefix
   - Draws only the local held optic.

7. `ScopeMaskRenderer.DrawCollimators(CommandBuffer, Camera)` prefix
   - Draws only the local held scope's collimator.

8. `ScopeMaskRenderer.DrawActiveCollimator(CommandBuffer)` prefix
   - Draws only the local held collimator.

9. `OpticRetrice.UpdateTransform(OpticSight)` prefix
   - Rejects non-local reticle transforms.

10. `OpticRetrice.SetOpticSight(OpticSight)` prefix
    - Rejects non-local optics before they replace the local shared reticle.

11. `StashItemModel.Init(GameObject)` postfix
    - Registers the model unconditionally and queues every descendant optic
      for forced interior suppression.
    - Retries optics whose lens renderer or materials are not ready during the
      initial initialization pass.

12. Remote weapon registration
    - Covers `ObservedPlayerView.SetWeaponRoot`, `UpdateWeaponRoot`, and
      `Initialization`.
    - Covers `ObservedPlayerFirearmHandsController.Initialize`,
      `UpdateTacticalComboVisualControllers`, and `UpdateScopes`.
    - Covers `Firearms.UpdateModsVisualControllers` and
      `BundleAnimationBones.ChangeHandsItem`.
    - A hierarchy fallback detects `ObservedPlayerView` when a rebuilt optic is
      not under a previously registered weapon root.

13. Local aiming and resolution restore
    - The frame fallback resets `_lastLocalOpticAiming` as soon as aiming ends.
    - `FirearmController.IsAiming` setter postfix schedules the authoritative
      close-scope restore and resets the aiming flag immediately.

## Deployment

Exit Escape From Tarkov and the launcher before replacing the DLL. BepInEx does
not reload a replaced plugin in an existing process.

Install `HeldScopeOnly.1.9.2.dll` through the BepInEx plugin loader on every
graphical Fika client that observes remote players or AI. Remove all older
`HeldScopeOnly` DLLs before installing the new build. Do not install it only
on one observer and expect another client's rendering to change.

The Fika headless client and dedicated server need no copy.

## Verification

After a complete restart, the BepInEx loader log and the plugin-owned
`HeldScopeOnly.log` must contain:

```text
HeldScopeOnly 1.9.2 loaded
```

When a hideout item model initializes, expect:

```text
stash-model-register=
stash-optic-pending=
stash-optic-retry=
stash-optic-enforced=
```

When a remote player or AI optic initializes, expect:

```text
fika-remote-register=
fika-remote-optic-found=
fika-remote-lens-suppressed=
```

When DynamicExternalResolution is not installed, expect one
`external-resolution-not-installed` line. The plugin must not keep retrying
that restore hook.

The following obsolete diagnostics must not appear from this build:

```text
fika-remote-lens-faded=
fika-remote-lens-hidden=
fika-remote-optic-disabled=
fika-remote-scope-mode-block=
```

Runtime acceptance:

1. A Fika observer sees the AI's full external optic, including frame, tube, and
   lens glass, with no scope interior.
2. Two Fika clients see each other's external optics normally, with no remote
   scope interior or reticle.
3. Scope mode changes update the remote external model without making the lens
   transparent.
4. The local player's held scope still renders normally through its interior.
5. Closing the local scope still restores DynamicExternalResolution.
6. The hideout weapon rack keeps its external optic visible and suppresses its
   scope interior. The log contains `stash-model-register=` and
   `stash-optic-enforced=` for the displayed weapon.

## Build

Build against the target game's managed assemblies and BepInEx libraries with
the included `build-1.9.2.rsp` response file. The response file is configured
for SPT `4.1.6`.

The expected compiler warning is only:

```text
warning CS2023: /noconfig is specified in a response file and is ignored
```

## Rollback

Use `HeldScopeOnly.1.9.1.dll` when rolling back. Replace the current plugin
assembly, remove the plugin-owned log, and restart the game completely.
