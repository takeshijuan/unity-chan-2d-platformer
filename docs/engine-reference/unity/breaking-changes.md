# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-04-23

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android/iOS)

---

## Unity 6.3 LTS 固有の変更（本プロジェクト重要）

### 🔴 URP Compatibility Mode 完全削除（Unity 6.0 で deprecated → 6.3 で削除）

```csharp
// ❌ Unity 6.3 で使用不可
var settings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
settings.enableRenderCompatibilityMode = true;  // 読み取り専用、常に false を返す

// ✅ RenderGraph ベースの新 API を使用
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
    // 新しい RenderGraph パターン
}
```

**影響**: カスタム ScriptableRenderFeature / ScriptableRenderPass を書く場合は全て RenderGraph API に移行必須。本プロジェクトはカスタム Render Feature を基本使わない予定だが、将来の VFX / Post Processing 実装時に注意。

### 🟡 Physics.autoSyncTransforms 非推奨

```csharp
// ❌ Unity 6.3 で deprecated（6.5 で削除予定）
Physics.autoSyncTransforms = true;  // 設定すること自体は可能だが警告

// ✅ 必要なタイミングで明示的に呼ぶ
Physics.SyncTransforms();
```

**影響**: 2D メトロイドヴァニアの物理では、Rigidbody2D の Transform 変更後に衝突判定が必要な場合、`Physics2D.SyncTransforms()` を明示呼び出す必要あり。Kinematic CharacterController2D の実装時に注意。

### 🟢 2D Physics: Box2D v3 統合（新機能）

Unity 6.3 で内部物理エンジンが Box2D v3 に刷新。マルチスレッド対応、決定論性強化。

**影響**: 本プロジェクト（2D メトロイドヴァニア、将来的にスピードラン文化想定）にとって **プラス**。リプレイ機能や入力記録ベースのテストで挙動の一貫性が期待できる。

```csharp
// Unity 6.3 の新 low-level 2D Physics API（将来使うかもしれない）
using UnityEngine.LowLevelPhysics2D;
// 詳細は公式ドキュメント参照
```

### 🟡 UI Toolkit USS パーサー厳格化

Unity 6.3 の USS パーサーは従来見過ごされていた無効な USS を検出するようになった。

**影響**: UGUI を使うなら影響なし。UI Toolkit を採用する場合、既存の USS ファイルで新エラーが出る可能性（本プロジェクトは UI 実装をこれから開始するので事前に認識しておく）。

### 🔴 Multiplay Hosting 廃止（本プロジェクトに影響なし）

Unity Multiplay Hosting サービスは 2026-03-31 で終了。本プロジェクトはシングルプレイなので関係なし。

### 🟢 Render Graph Viewer プレイヤービルド対応（新機能）

プレイヤービルド（実機）に接続してリアルタイムで Render Graph を可視化できる。

**影響**: Steam Deck 等実機パフォーマンス検証時に有用。

### 🟢 2D + 3D 同一シーン混在レンダリング対応（新機能）

URP 2D Renderer が 3D 要素（MeshRenderer/SkinnedMeshRenderer）と同一シーンで動作。3D 要素が 2D ライト/Sprite Mask と相互作用可能。

**影響**: 本プロジェクトは 2D 主体だが、将来的に背景の 3D 演出・ボス戦での 3D エフェクト等の選択肢が開く。

---

## 本プロジェクト影響度マトリクス

| 破壊的変更 | 本プロジェクトへの影響 | 対応 |
|---|---|---|
| URP Compatibility Mode 削除 | Low（新規プロジェクトなので） | 使わないルールに | 
| Physics.autoSyncTransforms | Medium | CharacterController2D 実装時にSyncTransforms明示呼び出し |
| Box2D v3 統合 | High（プラス） | 活用する、リプレイ/スピードラン対応の土台に |
| UI Toolkit USS 厳格化 | Medium | UI 実装開始時に事前確認 |
| Entities/DOTS 刷新 | **なし**（本作では DOTS 不使用） | 該当しない |
| Input System 必須化 | High（プラス） | 最初から Input System のみで実装 |
| RenderGraph API 移行 | Low（カスタム RenderPass 書く場合のみ） | 必要になったら対応 |
| Addressables 例外スロー | Medium | try/catch 必須、セーブ/ロード周りで要注意 |

---

**Sources:**
- Upgrade Guide: [https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html](https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html)
- What's New: [https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html)
- Unity 2022 → 6.0 migration: https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- Entities 1.3 upgrade: https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html
