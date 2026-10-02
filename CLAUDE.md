# Blurred Lines VFX

## What This Project Is

A Unity project for **live show interactive visualizers** using depth cameras (ZED stereo camera, Kinect V2) and Unity's VFX Graph. The project contains a single scene, **blurred-lines-vfx**: real-time depth-based particle visualization performed live on stage.

The primary creative direction uses a **ZED stereo camera** to capture performers/subjects as 3D point clouds, rendered through VFX Graph. Kinect V2 is kept as an alternative input for the same scene.

## Unity Automation

For any Unity work, use the global `unity` agent. It handles scene inspection, GameObject editing, C# code, prefabs, assets, input system, testing, and UI automation. The unity-cli bridge listens on port **6404**.

## Tech Stack

- **Unity 6000.3** LTS
- **HDRP** v17.3.0 (High Definition Render Pipeline)
- **VFX Graph** v17.3.0 (GPU-driven particle system)
- **ZED SDK** v5.5.0 via `com.stereolabs.zed` (upstream `stereolabs/zed-unity#v5.5.0`; must match the installed ZED SDK). ZED classes live in `namespace sl`.
- **Kinect V2 SDK** via `Assets/Standard Assets/` managed wrappers + `Assets/Plugins/x86_64/KinectUnityAddin.dll` (needs the Kinect for Windows runtime, `Kinect20.dll`)
- **Keijiro Takahashi packages**: VFX graph assets, shader graph assets, noise shaders, procedural motion (Brownian/Cyclic motion used in blurred-lines-vfx), PCX point cloud

## Architecture Overview

### Data Flow: Depth Camera -> VFX Graph

```
Depth Camera (ZED or Kinect)
    |
    v
Bridge Script (ZEDPointCloudVFX.cs or KinectPointCloud.cs)
    |-- Position data -> RenderTexture (ARGBFloat)
    |-- Color data    -> RenderTexture (ARGB32)
    v
VFX Graph (.vfx asset)
    |-- Samples position texture -> sets particle positions
    |-- Samples color texture    -> sets particle colors
    |-- Custom subgraphs for clipping, DOF, etc.
    v
HDRP Rendering -> Screen
```

### Switching Between ZED and Kinect

Select **Director** and use the **Source** dropdown on `DepthCameraSelector` (ZED / Kinect). It enables the matching bridge component on `Depth VFX` and `Lines VFX` (each carries both `ZEDPointCloudVFX` and `KinectPointCloud`) and activates `ZED_Rig_Mono` only for ZED. The change applies immediately through a custom inspector (undoable, saved with the scene), and `Awake` re-applies it at runtime. Don't toggle the bridge components by hand; the selector will overwrite them.

## Key Directories

| Path | Purpose |
|------|---------|
| `Assets/Kinect/Core/` | Bridge scripts, camera selector, compute shader, flip shaders, render textures |
| `Assets/VFX/` | VFX Graph assets (`Depth.vfx`, `Lines.vfx`) and their subgraphs |
| `Assets/Scenes/blurred-lines-vfx.unity` | The only scene |
| `Assets/Scenes/blurred-lines-vfx/` | Volume profile (`Skater Volume Profile`) |
| `Assets/Standard Assets/` | Kinect V2 SDK managed wrappers (do not edit) |
| `Assets/Plugins/x86_64/` | Kinect native add-in (64-bit Windows only) |

## Core Scripts

### ZED Pipeline (`Assets/Kinect/Core/`)

- **ZEDPointCloudVFX.cs** - Bridges ZED SDK to VFX Graph. Gets `MEASURE.XYZ` (positions) and `VIEW.LEFT` (color) textures from ZED, blits through flip shaders to RenderTextures, binds to VFX Graph properties (`PositionMap`, `ColorMap`, `PointCount`). Handles camera disconnect/reconnect gracefully.
- **DepthCameraSelector.cs** (+ `Editor/DepthCameraSelectorEditor.cs`) - ZED/Kinect dropdown on the Director object.
- **ZEDCameraReset.cs** - Press R to reload the scene to recover from a ZED hiccup.
- **ZEDFlipPosition.shader** - Mirrors X coordinate for ZED -> Unity handedness conversion. Negates X position.
- **ZEDFlipColor.shader** - Mirrors color texture UV only, preserves color values.

### Kinect Pipeline (`Assets/Kinect/Core/`)

- **KinectPointCloud.cs** - Captures Kinect depth + color frames, dispatches compute shaders, outputs position/color RenderTextures (`KinectPointCloudMap`, `KinectColorMap`).
- **PointCloudBaker.compute** - Two kernels: `BakeDepth` (raw depth -> 3D positions) and `BakeColor` (depth-to-color space mapping).
- **Extensions.cs** - Reflection hack to load unmanaged memory into ComputeBuffers efficiently.

### Interactive Controls (`Assets/Kinect/Core/`)

- **VFXKeyboardControl.cs** - On Director. A list of bindings that nudge exposed VFX floats on every target graph exposing them (targets: Depth VFX + Lines VFX). Defaults: ←/→ near clip, Shift+←/→ far clip, scroll or Shift+↑/↓ focus distance (Depth only), ↑/↓ Lines Brightness Multiplier (Lines only, min 0), [/] Pulse Period (Depth only, min 0.5). Keeps near < far by `minClipGap`. Uses the legacy `Input` class. Replaced the per-object `VFXScrollControl`.

## VFX Graph Assets (`Assets/VFX/`)

| Asset | Notes |
|-------|-------|
| `Depth.vfx` | Depth particles with focus distance, saturation/brightness clipping, near/far planes, height limits |

**Focus pulse (Depth.vfx, "Focus" blackboard category):** every `Pulse Period` seconds focus moves from `Focus Distance` to `Focus Distance + Pulse Focus Offset` and back, following the `Pulse Shape` curve (time 0-1 = one period, value 0-1 = share of the offset). Chain: VFX Total Time / Pulse Period -> Fractional -> Sample Curve -> x Offset -> + Focus Distance -> `Depth Of Field In Local` block (Output context); `Blur Radius` drives that block's Radius. `Pulse Focus Offset = 0` disables the pulse. A sticky note in the graph explains the same.
| `Lines.vfx` | Line particle variant |

### Subgraphs (`Assets/VFX/Subgraphs/`)

- `ClipByDistance.vfxoperator` - Distance-based particle clipping (near/far)
- `ClipByColor.vfxoperator` - Color-based particle clipping
- `Depth Of Field In Local.vfxblock` - DOF block (Depth)
- `WebUV.vfxoperator` - UV manipulation (Lines)

## Important Technical Details

- **ZED vs Kinect depth**: ZED is a stereo camera with per-pixel confidence scores and filtering. Kinect V2 is time-of-flight with binary depth readings (value or 0). No confidence threshold on Kinect.
- **Coordinate flip**: ZED outputs left-handed coordinates; the flip shaders convert to Unity's coordinate system by negating X.
- **RenderTexture formats**: Position data uses `ARGBFloat` to preserve full XYZ precision. Color uses `ARGB32`.
- **Compute buffers**: The `Extensions.cs` reflection hack bypasses managed array copies for Kinect frame data - do not refactor without understanding the performance implications.
- **VFX Graph property names**: The bridge scripts bind textures by string name (`PositionMap`, `ColorMap`, `PointCount`). These must match exactly between C# and the VFX Graph assets.

## Working With This Project

- The camera must be physically connected for the scene to show particles.
- The project uses HDRP - all materials must be HDRP-compatible. Standard/URP shaders will render pink.
- `Assets/Standard Assets/` contains the Kinect SDK C# wrappers. These are upstream vendor code - do not modify.
- Assets removed in the 2026-10-01 cleanup (other demo scenes, Basic/Blocks/Lines VFX, Kinect mapped variant, skeleton tracker, mic FFT/audio-reactive scripts, skater PLY and Timeline, ZED 5.2.0 samples, 32-bit/UWP Kinect plugins) are backed up in `../Kinect-VFX-Graph_removed-assets-2026-10-01/`, preserving their original paths and `.meta` files.
