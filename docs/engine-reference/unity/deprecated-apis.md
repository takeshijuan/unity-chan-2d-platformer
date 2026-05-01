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

## Cinemachine (Unity 6.3 / Cinemachine 3.1.6) — R1 Spike 確定 (2026-04-30)

| Deprecated (2.x) | Replacement (3.x 確定) | Notes |
|------------------|------------------------|-------|
| `Cinemachine.CinemachineVirtualCamera` | `Unity.Cinemachine.CinemachineCamera` (sealed) | Namespace + rename。2.x 互換 shim は期間限定 |
| `CinemachineFramingTransposer` (Body) | `CinemachinePositionComposer` | Body component 改名 (R1 #4 確定) |
| `CinemachineBrain.m_UpdateMethod` 私有フィールド reflection 経路 | `CinemachineBrain.UpdateMethod` (**public field**, NOT property) | enum `UpdateMethods` { FixedUpdate=0, LateUpdate=1, SmartUpdate=2, ManualUpdate=3 }、default = SmartUpdate (R1 #2 公開フィールドを確定確認)。`m_UpdateMethod` は Unity の internal serialization 命名で、3.x で削除されたかは empirical 未確認 — **公開 API として参照しない**（あったとしても internal、shim 期待禁止） |
| `CinemachineImpulseListener.UseSignalSpaceOnly` | `CinemachineImpulseListener.UseCameraSpace` | RENAMED (R1 #7 確定、`UseSignalSpaceOnly` は 3.x で削除済) |
| `CinemachineConfiner2D.m_BoundingShape2D` (private) | `CinemachineConfiner2D.BoundingShape2D` (**public field**, NOT property) | `Collider2D` 型 (R1 #5 確定) |
| `CinemachineConfiner2D.InvalidateCache()` | `CinemachineConfiner2D.InvalidateBoundingShapeCache()` | parameterless (R1 #5 確定) |
| `vcam.m_Follow`, `vcam.m_LookAt` (m_ prefix) | `cam.Follow`, `cam.LookAt` (no prefix) | Property 化、`Transform` 型 (R1 #3 確定) |
| `[DefaultExecutionOrder]` 属性 で CinemachineBrain order 固定 | UpdateMethod enum + `[ExecuteAlways]` で制御 | `CinemachineBrain` には DefaultExecutionOrder 属性が **付与されていない** (R1 #11 確定) |
| `UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera.referenceResolutionX/Y` | `UnityEngine.U2D.PixelPerfectCamera.refResolutionX/Y` | URP 17.0.3 同梱、命名は `refResolutionX/Y` (NOT `referenceResolutionX/Y`、R1 #10 確定) |

### 補足: CinemachinePixelPerfect Extension は隠蔽されている (functional)

| 項目 | 値 |
|------|------|
| クラス | `Unity.Cinemachine.CinemachinePixelPerfect` (functional, declaredMethods=3) |
| `[AddComponentMenu("")]` | 付与 — Inspector "Add Component" メニューから **隠蔽** |
| 推奨追加方法 | コード経由 `gameObject.AddComponent<CinemachinePixelPerfect>()` または既存 prefab に手動ドラッグ |

> **Forbidden**: Inspector "Add Component" メニューから `CinemachinePixelPerfect` を選ぶ運用に依存する設計。`/architecture-decision adr-0006a` Decision 2 で確定。

> **Cinemachine 3.x 移行注意**: 上記の 2.x → 3.x mapping は ADR-0006 R1 spike (Unity 6.3 LTS 6000.3.13f1 + Cinemachine 3.1.6) で empirical 確定済。LLM 訓練データが 2.x 主体のため、コード生成時は本表を必ず参照すること。詳細仕様は [`docs/engine-reference/unity/plugins/cinemachine.md` の R1 Spike Findings セクション](plugins/cinemachine.md#r1-spike-findings-2026-04-30) と [ADR-0006a](../../architecture/adr-0006a-camera-system-r1-findings.md)。

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
