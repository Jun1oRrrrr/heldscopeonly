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
- Plugin assembly: `HeldScopeOnly.dll`

## Installed Artifact

- Plugin version: `1.7.4`
- BepInEx GUID: `local.heldscopeonly`
- Release: GitHub Releases, tag `v1.7.4`
- SHA256:
  `F28F79884ABDB6FB2D531AED17B4BCF2A1BD88F5B1E76CA79860893EBBCFB132`

The repository contains the complete plugin source. The compiled DLL is
distributed only as a release asset.

The `1.7.3` build is superseded and should not be deployed. The previously
installed `1.7.2` build is preserved for rollback.

## 1.7.4 Changes

- Fixes Fika clients seeing only the AI scope mount or frame.
- Fixes AI and remote-player scope lenses becoming transparent after a weapon,
  scope-mode, or observed-player visual rebuild.
- Keeps vanilla scope-mode selection and external scope geometry visible on
  remote players and AI.
- Forces non-local `OpticSight.LensFade` calls to show the external lens.
- Re-shows the external lens in both remote `OpticSight.OnEnable` and
  `OpticSight.OnDisable`; neither event publishes the global optic state.
- Keeps `OpticCameraManager`, `ScopeMaskRenderer`, `OpticRetrice`, and
  collimator filtering active, so remote/AI scope interiors still do not
  render.
- Leaves the local held-scope path unchanged, including local scope mode
  selection.
- Leaves the `1.7.2` close-scope DynamicExternalResolution restore logic
  unchanged.

## Runtime Rules

Local held scope:

- Full internal and external rendering remains enabled.
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

- Display optic lens and interior rendering remain suppressed.

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
    - Fades and disables weapon-rack display optics.

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
    - `FirearmController.IsAiming` setter postfix schedules the authoritative
      close-scope restore.
    - The frame fallback remains active if the setter patch cannot be installed.

## Deployment

Exit Escape From Tarkov and the launcher before replacing the DLL. BepInEx does
not reload a replaced plugin in an existing process.

Install `HeldScopeOnly.dll` through the BepInEx plugin loader on every graphical
Fika client that observes remote players or AI. Do not install it only on one
observer and expect another client's rendering to change.

The Fika headless client and dedicated server need no copy.

## Verification

After a complete restart, the BepInEx loader log and the plugin-owned
`HeldScopeOnly.log` must contain:

```text
HeldScopeOnly 1.7.4 loaded
```

When a remote player or AI optic initializes, expect:

```text
fika-remote-register=
fika-remote-optic-found=
fika-remote-lens-shown=
```

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
6. The hideout weapon rack still suppresses its scope interior.

## Build

Build against the target game's managed assemblies and BepInEx libraries with
the included `build-1.7.4.rsp` response file. The response file is configured
for SPT `4.1.6`.

The expected compiler warning is only:

```text
warning CS2023: /noconfig is specified in a response file and is ignored
```

## Rollback

Use the previously installed `1.7.2` DLL when rolling back. Replace the current
plugin assembly, remove the plugin-owned log, and restart the game completely.
