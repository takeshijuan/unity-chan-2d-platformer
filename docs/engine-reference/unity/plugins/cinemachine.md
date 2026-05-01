# Unity 6.3 — Cinemachine

**Last verified:** 2026-02-13
**Status:** Production-Ready
**Package:** `com.unity.cinemachine` v3.0+ (Package Manager)

---

## Overview

**Cinemachine** is Unity's virtual camera system that enables professional, dynamic camera
behavior without manual scripting. It's the industry standard for Unity camera work.

**Use Cinemachine for:**
- 3rd person follow cameras
- Cutscenes and cinematics
- Camera blending and transitions
- Dynamic camera framing
- Screen shake and camera effects

**⚠️ Knowledge Gap:** Cinemachine 3.0 (Unity 6) is a major rewrite from 2.x.
Many API names and components changed.

---

## Installation

### Install via Package Manager

1. `Window > Package Manager`
2. Unity Registry > Search "Cinemachine"
3. Install `Cinemachine` (version 3.0+)

---

## Core Concepts

### 1. **Virtual Cameras**
- Define camera behavior (position, rotation, lens)
- Multiple virtual cameras can exist; only one is "live" at a time

### 2. **Cinemachine Brain**
- Component on main Camera
- Blends between virtual cameras
- Applies virtual camera settings to Unity Camera

### 3. **Priorit**ies**
- Virtual cameras have priority values
- Highest priority camera is active
- Blends smoothly when priority changes

---

## Basic Setup

### 1. Add Cinemachine Brain to Main Camera

```csharp
// Automatically added when creating first virtual camera
// Or manually: Add Component > Cinemachine Brain
```

### 2. Create Virtual Camera

`GameObject > Cinemachine > Cinemachine Camera`

This creates a **CinemachineCamera** GameObject with default settings.

---

## Virtual Camera Components

### CinemachineCamera (Unity 6 / Cinemachine 3.0+)

```csharp
using Unity.Cinemachine;

public class CameraController : MonoBehaviour {
    public CinemachineCamera virtualCamera;

    void Start() {
        // Set priority (higher = active)
        virtualCamera.Priority = 10;

        // Set follow target
        virtualCamera.Follow = playerTransform;

        // Set look-at target
        virtualCamera.LookAt = playerTransform;
    }
}
```

---

## Follow Modes (Body Component)

### 3rd Person Follow (Orbital Follow)

```csharp
// In Inspector:
// CinemachineCamera > Body > 3rd Person Follow

// Configure:
// - Shoulder Offset: (0.5, 0, 0) for over-shoulder
// - Camera Distance: 5.0
// - Vertical Damping: 0.5 (smooth up/down)
```

### Framing Transposer (Smooth Follow)

```csharp
// CinemachineCamera > Body > Position Composer

// Configure:
// - Screen Position: Center (0.5, 0.5)
// - Dead Zone: Don't move camera if target within zone
// - Damping: Smooth following
```

### Hard Lock (Exact Follow)

```csharp
// CinemachineCamera > Body > Hard Lock to Target
// Camera exactly matches target position (no offset or damping)
```

---

## Aim Modes (Aim Component)

### Composer (Frame Target)

```csharp
// CinemachineCamera > Aim > Composer

// Configure:
// - Tracked Object Offset: Aim at target's head instead of feet
// - Screen Position: Where target appears on screen
// - Dead Zone: Don't rotate if target within zone
```

### Look At Target

```csharp
// CinemachineCamera > Aim > Rotate With Follow Target
// Camera rotation matches target rotation (e.g., first-person)
```

---

## Blending Between Cameras

### Priority-Based Blending

```csharp
public CinemachineCamera normalCamera; // Priority: 10
public CinemachineCamera aimCamera;    // Priority: 5

void StartAiming() {
    // Set aim camera to higher priority
    aimCamera.Priority = 15; // Now active
    // Brain automatically blends from normalCamera to aimCamera
}

void StopAiming() {
    aimCamera.Priority = 5; // Back to normal
}
```

### Custom Blend Times

```csharp
// Create Custom Blends Asset:
// Assets > Create > Cinemachine > Cinemachine Blender Settings

// In Cinemachine Brain:
// - Custom Blends = your asset
// - Configure blend times per camera pair
```

---

## Camera Shake

### Impulse Source (Trigger Shake)

```csharp
using Unity.Cinemachine;

public class ExplosionShake : MonoBehaviour {
    public CinemachineImpulseSource impulseSource;

    void Explode() {
        // Trigger camera shake
        impulseSource.GenerateImpulse();
    }
}
```

### Impulse Listener (Receive Shake)

```csharp
// Add to CinemachineCamera:
// Add Component > CinemachineImpulseListener

// Impulse listener automatically receives shake from nearby Impulse Sources
```

---

## Freelook Camera (Third Person with Mouse Look)

### Cinemachine Free Look

```csharp
// GameObject > Cinemachine > Cinemachine Free Look

// Creates 3 rigs (Top, Middle, Bottom) that blend based on vertical input
// Configure:
// - Orbit Radius: Distance from target
// - Height Offset: Camera height at each rig
// - X/Y Axis: Mouse or joystick input
```

---

## State-Driven Camera (Anim ator-Based)

### Cinemachine State-Driven Camera

```csharp
// GameObject > Cinemachine > Cinemachine State-Driven Camera

// Configure:
// - Animated Target: Character with Animator
// - Layer: Animator layer to track
// - State: Assign camera per animation state (Idle, Run, Jump, etc.)

// Camera automatically switches based on animation state
```

---

## Dolly Tracks (Cutscenes)

### Cinemachine Dolly Track

```csharp
// 1. Create Spline: GameObject > Cinemachine > Cinemachine Spline

// 2. Create Dolly Camera:
//    GameObject > Cinemachine > Cinemachine Camera
//    Body > Spline Dolly
//    Assign Spline

// 3. Animate dolly position on spline (Timeline or script)
```

---

## Common Patterns

### Third-Person Follow Camera

```csharp
// CinemachineCamera
// - Follow: Player Transform
// - Body: 3rd Person Follow (shoulder offset, distance: 5)
// - Aim: Composer (frame player at center)
```

---

### Aiming Camera (Zoom In)

```csharp
// Normal Camera (Priority 10):
//   - Distance: 5.0

// Aim Camera (Priority 5):
//   - Distance: 2.0
//   - FOV: Narrower

// Script:
void StartAiming() {
    aimCamera.Priority = 15; // Blend to aim camera
}
```

---

### Cutscene Camera Sequence

```csharp
// Use Timeline:
// 1. Create Timeline (Assets > Create > Timeline)
// 2. Add Cinemachine Track
// 3. Add virtual cameras as clips
// 4. Timeline automatically blends between cameras
```

---

## Migration from Cinemachine 2.x (Unity 2021)

### API Changes (Unity 6 / Cinemachine 3.0)

```csharp
// ❌ OLD (Cinemachine 2.x):
CinemachineVirtualCamera vcam;
vcam.m_Follow = target;

// ✅ NEW (Cinemachine 3.0+):
CinemachineCamera vcam;
vcam.Follow = target; // Cleaner API
```

**Major Changes:**
- `CinemachineVirtualCamera` → `CinemachineCamera`
- `m_Follow`, `m_LookAt` → `Follow`, `LookAt` (no "m_" prefix)
- Components renamed for clarity
- Better performance

---

## Performance Tips

- Limit active virtual cameras (only activate when needed)
- Use lower-priority cameras instead of destroying/creating
- Disable virtual cameras when far from player

---

## Debugging

### Cinemachine Debug

```csharp
// Window > Analysis > Cinemachine Debugger
// Shows active camera, blend info, shot quality
```

---

## Sources
- https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html
- https://learn.unity.com/tutorial/cinemachine

---

## R1 Spike Findings (2026-04-30)

> **Source**: ADR-0006 R1 spike Editor verification (Unity 6.3 LTS 6000.3.13f1 + Cinemachine 3.1.6 + URP 17.0.3) で確定した API 命名 / 公開形式。
> Evidence: [`production/qa/evidence/r1-camera-cinemachine3-spike-result.md`](../../../../production/qa/evidence/r1-camera-cinemachine3-spike-result.md)
> ADR: [ADR-0006a Camera System R1 Findings](../../../architecture/adr-0006a-camera-system-r1-findings.md)

### Cinemachine 3 vs 2.x — Namespace + 命名 mapping

| Cinemachine 2.x | Cinemachine 3.x (確定) | 公開形式 | Notes |
|-----------------|------------------------|----------|-------|
| `Cinemachine.CinemachineVirtualCamera` | `Unity.Cinemachine.CinemachineCamera` | sealed class | Namespace `Unity.Cinemachine` 必須 |
| `Cinemachine.CinemachineBrain` | `Unity.Cinemachine.CinemachineBrain` | class | Same |
| `CinemachineFramingTransposer` (Body) | `CinemachinePositionComposer` | class | Body component 改名 |
| `CinemachineConfiner2D.m_BoundingShape2D` | `CinemachineConfiner2D.BoundingShape2D` | **field** (not property) | `Collider2D` 型、direct assign 可 |
| `CinemachineConfiner2D.InvalidateCache()` | `CinemachineConfiner2D.InvalidateBoundingShapeCache()` | method | Parameterless |
| `CinemachineImpulseSource.GenerateImpulse()` | 7 overloads | methods | 3 legacy + 3 new naming + 1 no-arg, see below |
| `CinemachineImpulseListener.UseSignalSpaceOnly` | `CinemachineImpulseListener.UseCameraSpace` | property | **RENAMED** + **semantic NOT empirically equivalent** — `UseSignalSpaceOnly=true` (2.x) vs `UseCameraSpace=true` (3.x) likely have different (possibly inverted) meanings. **Do NOT mechanically rename**; treat as a manual port that requires re-evaluating intent against Cinemachine 3 ImpulseListener docs |
| `CinemachineBrain.m_UpdateMethod` (private serialized) | `CinemachineBrain.UpdateMethod` | **field** (not property) | enum `UpdateMethods` |
| `vcam.LookAt`, `vcam.Follow` (m_ prefixed in 2.x) | `cam.LookAt`, `cam.Follow` | properties | `Transform` type, no m_ prefix |
| `CinemachineBrain.OutputCamera` | Same | property | `Camera` type |

### CinemachineImpulseSource — 7 GenerateImpulse Overloads

```csharp
// New naming (Cinemachine 3 推奨)
GenerateImpulseAt(Vector3 position, Vector3 velocity);
GenerateImpulseWithVelocity(Vector3 velocity);
GenerateImpulseWithForce(float force);

// Legacy (deprecated shim, 期間限定)
GenerateImpulse(Vector3 velocity);                      // → use GenerateImpulseWithVelocity
GenerateImpulse(float force);                           // → use GenerateImpulseWithForce
GenerateImpulseAtPositionWithVelocity(Vector3 pos, Vector3 vel);  // legacy alias

// No-arg (uses DefaultVelocity from Inspector)
GenerateImpulse();
```

**推奨**: Cinemachine 3 では `GenerateImpulseWithVelocity` / `GenerateImpulseWithForce` / `GenerateImpulseAt` の 3 つの new naming を使用。Legacy 3 overloads はマイグレーション完了後に warning 化される可能性あり。

### CinemachineBrain.UpdateMethod — enum 値

```csharp
public enum UpdateMethods
{
    FixedUpdate = 0,
    LateUpdate = 1,
    SmartUpdate = 2,   // ← Cinemachine 3 default
    ManualUpdate = 3
}
```

- **Default**: `SmartUpdate` — physics object には FixedUpdate、static target には LateUpdate を自動選択
- **Reflection 取得**: `field` 経由のみ。`brainType.GetField("UpdateMethod", BindingFlags.Public | BindingFlags.Instance)` で取得可能。`GetProperty` は **null** を返す
- **`[DefaultExecutionOrder]` 属性**: `CinemachineBrain` には付与されていない。Execution timing は `UpdateMethod` enum + `[ExecuteAlways]` で制御

### CinemachinePixelPerfect Extension

| 項目 | 値 |
|------|------|
| クラス | `Unity.Cinemachine.CinemachinePixelPerfect` |
| 状態 | **functional** (declaredMethods=3) |
| `[AddComponentMenu("")]` | 付与（**Inspector "Add Component" メニューから隠蔽**） |
| 推奨追加方法 | コード経由 `gameObject.AddComponent<CinemachinePixelPerfect>()` または既存 prefab に手動ドラッグ → prefab 化 |
| Pixel Perfect Camera (URP bundled) との関係 | URP 17.0.3 同梱の `UnityEngine.U2D.PixelPerfectCamera` と組合せて使用 |

> **Note**: ADR-0006 起草時の Context7 事前検証 (MEDIUM confidence "empty stub") は **誤り**。Plan B (PixelPerfectCamera 単体運用) は不要。詳細は [ADR-0006a Decision 2](../../../architecture/adr-0006a-camera-system-r1-findings.md#locked-decision-2--cinemachinepixelperfect-採用方針)。

### CinemachineImpulseListener — Property 一覧

R1 #7 で確認した Cinemachine 3.1.6 の `CinemachineImpulseListener` public members:

```csharp
public class CinemachineImpulseListener : ...
{
    public float Gain;                  // shake 強度倍率
    public bool Use2DDistance;          // 2D distance attenuation
    public int ChannelMask;             // ImpulseSource ChannelMask とマッチング
    public bool UseCameraSpace;         // ★ RENAMED from `UseSignalSpaceOnly` (Cinemachine 2.x)
    // ... (他の members)
}
```

**Forbidden**: `UseSignalSpaceOnly` プロパティ参照 — 旧 Cinemachine 2.x 命名、3.x で削除済（R1 spike で `members.Contains("UseSignalSpaceOnly") = False` を確認）

### URP 2D + Cinemachine 3 + PixelPerfectCamera 三者統合

R1 #9 (Phase B static-state verification) の結果:
- Brain + PixelPerfectCamera + UniversalRP 共存可能
- 静止状態で stutterFrames=0/120
- **動的検証は ADR-0006 C1 protocol scope (ADR-0002 V1 通過後)**

### PixelPerfectCamera (URP bundled) — Property 一覧

```csharp
// UnityEngine.U2D.PixelPerfectCamera (URP 17.0.3 bundled)
// 元 com.unity.2d.pixel-perfect package が URP に統合済
public class PixelPerfectCamera : MonoBehaviour
{
    public CropFrame cropFrame;        // enum: None/Pillarbox/Letterbox/Windowbox/StretchFill
    public GridSnapping gridSnapping;  // enum
    public float orthographicSize;
    public int assetsPPU;
    public int refResolutionX;          // ★ refResolutionX/Y (NOT referenceResolutionX/Y)
    public int refResolutionY;
    public bool upscaleRT;
    public bool pixelSnapping;
    public bool cropFrameX;
    public bool cropFrameY;
    public bool stretchFill;
    public int pixelRatio;
    public bool requiresUpscalePass;
    // ...
}
```

**Forbidden**: `referenceResolutionX/Y` プロパティ参照（旧 com.unity.2d.pixel-perfect 命名の可能性、URP bundled 版では `refResolutionX/Y` のみ）
