# Unity 6.3 LTS — Deprecated APIs

**Last verified:** 2026-04-23

Quick lookup table for deprecated APIs and their replacements.
Format: **Don't use X** → **Use Y instead**

---

## Input

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Input.GetKey()` | `Keyboard.current[Key.X].isPressed` | New Input System |
| `Input.GetKeyDown()` | `Keyboard.current[Key.X].wasPressedThisFrame` | New Input System |
| `Input.GetMouseButton()` | `Mouse.current.leftButton.isPressed` | New Input System |
| `Input.GetAxis()` | `InputAction` callbacks | New Input System |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` | New Input System |

**Migration:** Install `com.unity.inputsystem` package.

---

## UI

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Canvas` (UGUI) | `UIDocument` (UI Toolkit) | UI Toolkit is now production-ready |
| `Text` component | `TextMeshPro` or UI Toolkit `Label` | Better rendering, fewer draw calls |
| `Image` component | UI Toolkit `VisualElement` with background | More flexible styling |

**Migration:** UGUI still works, but UI Toolkit is recommended for new projects.

---

## DOTS/Entities

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `ComponentSystem` | `ISystem` (unmanaged) | Entities 1.0+ complete rewrite |
| `JobComponentSystem` | `ISystem` with `IJobEntity` | Burst-compatible |
| `GameObjectEntity` | Pure ECS workflow | No GameObject conversion |
| `EntityManager.CreateEntity()` (old signature) | `EntityManager.CreateEntity(EntityArchetype)` | Explicit archetype |
| `ComponentDataFromEntity<T>` | `ComponentLookup<T>` | Entities 1.0+ rename |

**Migration:** See Entities package migration guide. Major refactor required.

---

## Rendering

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CommandBuffer.DrawMesh()` | RenderGraph API | URP/HDRP render passes |
| `OnPreRender()` / `OnPostRender()` | `RenderPipelineManager` callbacks | SRP compatibility |
| `Camera.SetReplacementShader()` | Custom render pass | Not supported in SRP |

---

## Physics

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Physics.RaycastAll()` | `Physics.RaycastNonAlloc()` | Avoid GC allocations |
| `Rigidbody.velocity` (direct write) | `Rigidbody.AddForce()` | Better physics stability |
| **`Physics.autoSyncTransforms`** (Unity 6.3) | **`Physics.SyncTransforms()` を明示呼び出し** | 6.3 で deprecated、6.5 で削除予定 |
| **`Physics2D.autoSyncTransforms`** (Unity 6.3) | **`Physics2D.SyncTransforms()` を明示呼び出し** | 2D 版も同様 |

---

## Asset Loading

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Resources.Load()` | Addressables | Better memory control, async loading |
| Synchronous asset loading | `Addressables.LoadAssetAsync()` | Non-blocking |

---

## Animation

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Animation component | Animator Controller | Mecanim system |
| `Animation.Play()` | `Animator.Play()` | State machine control |

---

## Particles

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Particle System | Visual Effect Graph | GPU-accelerated, more performant |

---

## Scripting

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |

---

## Platform-Specific

### WebGL
| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| WebGL 1.0 | WebGL 2.0 or WebGPU | Unity 6+ defaults to WebGPU |

---

## Rendering (Unity 6.3 追加)

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| **URP Compatibility Mode** | RenderGraph API | Unity 6.3 で**完全削除**、`RenderGraphSettings.enableRenderCompatibilityMode` は読み取り専用 |
| `CommandBuffer` ベースの旧 Custom RenderPass | `RasterRenderPass` / RenderGraph | Unity 6 から段階的移行、6.3 で完全必須化 |

## Cinemachine (Unity 6.3 / Cinemachine 3.x)

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Cinemachine.CinemachineVirtualCamera` | `Unity.Cinemachine.CinemachineCamera` | Cinemachine 3.x で **rename + namespace 変更**。2.x 互換 shim は期間限定。新規プロジェクトは 3.x 系統のみ。`docs/architecture/adr-0006-camera-system.md` Locked Decision 1 で確定 |
| `CinemachineFramingTransposer` (Body component) | **要 Editor 確認**（`PositionComposer` 候補、ADR-0006 R1 spike #4 で検証） | Cinemachine 3.x で Body component 体系が改訂、命名と機能差は 2.x と異なる可能性 |
| Cinemachine 2.x 旧 `CinemachineBrain` API（`m_UpdateMethod` 等の private field アクセス） | Cinemachine 3.x `CinemachineBrain.UpdateMethod` 公開プロパティ | **要 Editor 確認**（既定値 LateUpdate / SmartUpdate / FixedUpdate / ManualUpdate のどれか、ADR-0006 R1 spike #2 で検証） |

> **Cinemachine 3.x 移行注意**: ADR-0006 Camera System (provisional) で `com.unity.cinemachine` 3.x 系統採用が確定。LLM 訓練データは 2.x 主体のため、API 名 12 箇所は ADR-0006 R1 spike (`production/qa/evidence/r1-camera-cinemachine3-spike-result.md`) で実機検証してから follow-up ADR で具体仕様を lock。

---

## Quick Migration Patterns

### Input Example
```csharp
// ❌ Deprecated
if (Input.GetKeyDown(KeyCode.Space)) {
    Jump();
}

// ✅ New Input System
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) {
    Jump();
}
```

### Asset Loading Example
```csharp
// ❌ Deprecated
var prefab = Resources.Load<GameObject>("Enemies/Goblin");

// ✅ Addressables
var handle = Addressables.LoadAssetAsync<GameObject>("Enemies/Goblin");
await handle.Task;
var prefab = handle.Result;
```

### UI Example
```csharp
// ❌ Deprecated (UGUI)
GetComponent<Text>().text = "Score: 100";

// ✅ TextMeshPro
GetComponent<TextMeshProUGUI>().text = "Score: 100";

// ✅ UI Toolkit
rootVisualElement.Q<Label>("score-label").text = "Score: 100";
```

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/deprecated-features.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Migration.html
