# ADR-0003: VFX System Boundary + IVFXPublisher

## Status

**Proposed (Validation Gate: G1-G5)**

> 本 ADR は「Validation Gate」セクション G1-G5 の検証を通過するまで Accepted に昇格しない。特に G2（PlayCue ≤ 0.2ms / 0-byte GC）と G5（Steam Deck 1080p VFX rendering ≤ 1.5ms）が偽の場合、Decision の根幹（Per-cue object pool + Legacy ParticleSystem backbone）が崩壊し、Alternative 1（VFX Graph 採用）の再評価となる。なお本 ADR は **Proposed 段階でも `docs/registry/architecture.yaml` への architectural stance を前倒し追記**する（Tier 1 移行は ADR-0001 R5 + ADR-0002 V1-V5 通過後だが、interface contract と forbidden patterns は ADR-0004 / Combat / Audio 系 ADR が参照する必要があるため）。

## Date

2026-04-27

## Last Verified

2026-04-27

## Decision Makers

- Project Lead（ユーザ）— 最終決定権
- `creative-director` 経由 CD-SYSTEMS Note CD1 / CD2（Tier 0 minimal feedback の定義と Tier 1 移行先要件）
- `technical-director` 経由 TD-SYSTEM-BOUNDARY A4 + TD-ADR gate — Core 層境界・publish/subscribe 方向性の確定
- `producer` 経由 PR-SCOPE — Tier 1 移行 PR1/PR2 のスプリント割当整合性
- `unity-specialist` — Unity 6.3 LTS / URP 2D Renderer / Legacy ParticleSystem deprecation / RenderGraph API レビュー
- `art-director` — art-bible.md 既存記述（`Camera.OnRenderImage` / `Render Texture マスク`）の supersede 整合性レビュー

## Summary

本作 `職業オーブのレガシー` の Pillar 1「切替が、花になる」と Pillar 3「歯ごたえ」を支える VFX feedback の Core 層境界を、`IVFXPublisher` interface + `VfxCueDefinition` ScriptableObject + per-cue object pool で確定する。Legacy ParticleSystem を主バックボーンに採用し（VFX Graph は IVFXPublisher 抽象越しに将来移行可能）、Color Wash は CinemachineBrain 直下の Animated Quad + `Sprite_ColorWash.shadergraph` に Sorting Layer `VFX_Overlay` で実装、ADR-0001 Tier 0 inline color-wash + SE は ADR-0001 R5 + ADR-0002 V1-V5 両通過後に PR1（二重 feedback）→ PR2（inline 削除）で publisher へ移行する。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Rendering（URP 2D Renderer + 2D Lights）+ VFX（Legacy ParticleSystem）+ Core（Pub/Sub Service） |
| **Knowledge Risk** | **HIGH** — Unity 6.3 LTS（2025-12 リリース）は LLM 訓練データ cutoff（May 2025）以降。URP Compatibility Mode 削除・Legacy ParticleSystem deprecation・RenderGraph API 必須化など破壊的変更多数。`engine-reference/unity/modules/rendering.md` 収録範囲は限定的（2D Lights / VFX Graph 詳細未収録） |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`modules/rendering.md`（lines 19-33, 149-191）、`breaking-changes.md`（lines 111-113, 154-167, 209-213）、`deprecated-apis.md`（lines 89-93）、`current-best-practices.md`（lines 21, 217-239, 245-255）、`design/art/art-bible.md`（lines 1077-1079, 1571-1592）、`docs/architecture/adr-0001-class-switch-architecture.md`（Tier 1 Refactor Path）、`docs/architecture/adr-0002-character-controller-motor.md`（motor_event_notification） |
| **Post-Cutoff APIs Used** | `UnityEngine.Rendering.Universal` URP 2D Sorting Layer、`Cinemachine.CinemachineBrain`（Cinemachine 3）、`UnityEngine.AddressableAssets.AssetReferenceGameObject`（Addressables 2.0+）、`ParticleSystem.MainModule.stopAction = StopAction.Callback` + `OnParticleSystemStopped()` callback、`ShaderGraph` `Sprite_ColorWash`（custom）、`SortingLayer.layers` API |
| **Verification Required** | (1) `VFX_Overlay` Sorting Layer が `Default`(World) と UI Canvas Sorting Layer の間に挿入可能であることを Project Settings で確認、(2) `CinemachineBrain` の child Transform に貼ったクワッドが LateUpdate で camera follow されること（Cinemachine 3 Brain output transform への child 配置動作）、(3) `AssetReferenceGameObject.LoadAssetAsync<GameObject>()` が cold-miss 時に例外を投げず `Status == Failed` となること、(4) `ParticleSystem.MainModule.stopAction = StopAction.Callback` が all sub-emitter 含めて呼ばれること、(5) URP 2D Renderer 2D Lights が `VFX_Overlay` Sorting Layer の sprite に正しく作用すること — **すべて G1 / G3 / G5 検証プロトタイプで実測必須** |

> **Note**: Knowledge Risk が HIGH のため、Unity 6.3 → 6.4 等のマイナー昇格時に Legacy ParticleSystem が削除予告された場合、本 ADR を Superseded にし新 ADR（VFX Graph 移行）を起こすこと。`api_decisions.vfx_particle_backbone.revisit_trigger` を registry にも記録する。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 Class Switch Architecture（Tier 1 移行ターゲット — `class_switch_orb_burst` cue が ClassStateMachine.SwitchTo から発火される）、ADR-0002 CharacterController2D + ICharacterMotor（`motor_event_notification` 購読源 — `Landed` → `dust_landed` cue、`HitstopApplied` → `hitstop_freeze_frame` cue） |
| **Enables** | ADR-0004 Class Abilities（`AbilityContext.VFXPublisher` で本 ADR の `IVFXPublisher` を埋める）、ADR-0005 Combat（hit_spark / status feedback で IVFXPublisher 参照）、ADR-0006 Audio System（IAudioPublisher を本 ADR の pooling semantics で対称設計）、`design/gdd/vfx-system.md` GDD authoring |
| **Blocks** | None — VFX System GDD authoring と Tier 1 移行 PR の前提だが、現時点で immediate blocker なし |
| **Ordering Note** | systems-index.md Recommended Design Order 表で **Order #16 VFX System**（Architecture Note A4）。Tier 1 移行は ADR-0001 R5 + ADR-0002 V1-V5 の **両 Validation Gate** が Accepted になってから着手。先行実装すると gate 失敗時に migration work を捨てるリスク。technical-preferences.md ADR 候補リストに VFX 専用 ADR は未記載だが、active.md にて ADR-0003 として確定済み |

## Context

### Problem Statement

本作の Pillar 1「切替が、花になる」即時 1-frame 視覚報酬と Pillar 3「歯ごたえ」打撃時 hitstop + impact frame は、Tier 0 hypothesis spike では VFX System / Audio System / Combat System 不在のまま検証する必要があり（CD1 / CD2）、ClassStateMachine と CharacterController2D が **inline で minimal feedback を内包**する設計が ADR-0001 / ADR-0002 で確定している。

しかし combat / abilities / boss effects / orb acquisition / UI notification といった consumer system が増えるにつれ、inline feedback 構造は (a) VFX ロジックを gameplay ロジックに結合させる、(b) prefab pooling / async loading / sorting / 性能予算管理を分散させる、(c) `Camera.OnRenderImage` を前提にした art-bible.md の旧仕様（URP 6.3 で動作不可）を gameplay code に固着させる、(d) Tier 1 リファクタターゲットが具体化されないため story authoring がブロックされる。

加えて systems-index.md Architecture Note **A4** で「VFX は Core 層、`IVFXPublisher` を提供する pub/sub サービス」が確定済みだが、interface 詳細・particle backbone 選択・Color Wash 実装手法・Tier 0→Tier 1 migration sequencing が未定義。`docs/registry/architecture.yaml` には `vfx-system-future` placeholder が `class_switch_notification.consumers` と `motor_event_notification.consumers` に残存し、ADR-0004 / Combat 系 ADR が確定参照を要求している。

### Current State

- ADR-0001 ClassStateMachine が Tier 0 で `_currentSpriteRenderer.color` 経由の per-sprite tint（uniform）と `_audioSourceMinimal` 経由の `AudioSource.PlayOneShot` を inline 実装中。Tier 1 で `IVFXPublisher` / `IAudioPublisher` へ「リファクタする」と宣言済みだが、移行先 API / トリガー条件 / PR 分割が未定義
- ADR-0002 CharacterController2D が `motor_event_notification` event（`Landed` / `JumpStarted` / `WallTouched` / `HitstopApplied` / `StateChanged`）を発火するが、registry consumer に `vfx-system-future` placeholder が残る。Hitstop（30-50ms）は **Motor 自身の Solver-skip** で実装され、VFX は freeze-frame 視覚効果のみ subscribe する境界が確定（Hitstop solver-skip を VFX に動かしてはならない）
- art-bible.md は当時 Built-in RP / `Camera.OnRenderImage()` / 「Render Texture マスク」を前提に Color Wash を仕様化（line 1077-1079）。Unity 6.3 URP 2D Renderer で動作不可のため supersede 必須
- art-bible.md G-6 が「同時アクティブ ParticleSystem 上限: 20 個」と budget 化（line 1574）。engine-reference の deprecation note と矛盾するように見えるが、これは **count 予算であり API 選択ではない**。本 ADR で「backbone は Legacy ParticleSystem」と明記して矛盾解消する
- art-bible.md G-7 で `Sprite_ColorWash.shadergraph` が custom shader budget 5 種の 1 つとして既に枠取り済み（line 1592）。本 ADR の Color Wash 実装手法はこの shader を流用する

### Constraints

- **Engine**: Unity 6.3 LTS / URP 2D Renderer 専用（Built-in RP / `Camera.OnRenderImage` / URP Compatibility Mode は使用不可）
- **Engine deprecation**: Legacy ParticleSystem は deprecated だが 6.3 でも機能。VFX Graph は URP 2D Output Particle Sprite 統合に caveats（Sub-emitter / Trail Renderer 不足）
- **Performance**: PlayCue ≤ 0.2 ms / event、hot path 0-byte GC alloc、VFX 合計 ≤ 1.5ms（Steam Deck 1080p worst-case）
- **art-bible budget**: 同時アクティブ particle emitter 20 個、VFX 専用 draw call 60、texture memory 150 MB 以内
- **Determinism**: VFX は gameplay state を mutate しない（ADR-0002 の replay 安全性を保つため。replay 記録は input + cue 発火コマンドのみで particle 内部状態は記録しない）
- **HUD readability**: Color Wash は HUD Canvas に視覚干渉しない（art-bible.md line 1077-1079 — Conflict 2 両立解の維持）
- **Pillar 1**: 切替視覚同期 1 frame（`SpriteLibrary.spriteLibraryAsset` swap 後の即時 cue 発火）

### Requirements

- **R1**: Foundation/Core 層に gameplay 依存を持たない `IVFXPublisher` service interface を `Game.Core.asmdef` で提供
- **R2**: 5 anchor cue を V1 で出荷: `class_switch_orb_burst` / `hit_spark` / `dust_landed` / `hitstop_freeze_frame` / `orb_acquisition`
- **R3**: 各 cue を per-cue object pool で管理（hard MaxConcurrent cap + OverflowPolicy）
- **R4**: Addressables async load、cold-miss は silent drop + debug log + counter（hot cue は `PreloadOnSceneLoad = true` を契約化）
- **R5**: ADR-0001 Tier 0 inline color-wash + SE を 2-PR で publisher に移行（PR1 二重 feedback、PR2 inline 削除）。Tier 1 移行は ADR-0001 R5 + ADR-0002 V1-V5 両通過後
- **R6**: ADR-0002 motor event を subscribe（`Landed` → dust、`HitstopApplied` → freeze flash）。Hitstop solver-skip 自体は Motor 所管（VFX 非介入）
- **R7**: Color Wash は `VFX_Overlay` Sorting Layer の screen-space Animated Quad、HUD Canvas より下に sorting で配置
- **R8**: VFX は ICharacterMotor 状態を write しない（ADR-0002 forbidden_patterns 継承）
- **R9**: VfxCueDefinition は ScriptableObject 駆動（enum / string key 不可）— デザイナーがコード編集なしで cue 追加可能

## Decision

VFX System を Core 層 publisher service として確定する。`IVFXPublisher` を `Game.Core.asmdef` に配置し、実装は `Game.VFX.asmdef` の `VFXPublisherService` MonoBehaviour（Scene root に install、autoload 不使用）が担う。Particle backbone は Legacy ParticleSystem のみとし、VFX Graph 移行は IVFXPublisher 抽象越しの将来オプションとする。Color Wash は CinemachineBrain 直下の Animated Quad + `Sprite_ColorWash.shadergraph`、Sorting Layer `VFX_Overlay` で実装。Tier 0 → Tier 1 migration は ADR-0001 R5 + ADR-0002 V1-V5 両通過後に PR1（並行 feedback）→ PR2（inline 削除）で実施。

### Architecture

```
┌──────────────────────────────────────────────────────────────┐
│ Game.Core.asmdef (Foundation, no gameplay deps)              │
│   ICharacterMotor             (ADR-0002)                     │
│   IVFXPublisher               (this ADR)                     │
│   VfxCueDefinition (SO)       (this ADR)                     │
│   VfxCueArgs (struct)         (this ADR)                     │
│   VfxHandle (struct)          (this ADR)                     │
│   VfxLayer / OverflowPolicy / AttachPolicy (enum, this ADR)  │
└──────────────────────────────────────────────────────────────┘
              ▲                       ▲
              │ implements             │ subscribes
┌──────────────────────────────┐  ┌──────────────────────────┐
│ Game.VFX.asmdef              │  │ Game.Gameplay.asmdef     │
│   VFXPublisherService        │  │   ClassStateMachine      │
│   PooledVfxInstance          │  │   CharacterController2D  │
│   ColorWashController        │  │   PlayerVFXBinder        │
│   VfxCueRegistry             │  │   (event → PlayCue)      │
│   NullVFXPublisher (Tier 0)  │  │                          │
└──────────────────────────────┘  └──────────────────────────┘
              │                              │
              │ Addressables async load      │ calls PlayCue
              ▼                              ▼
        ┌────────────────────────────────────────┐
        │ assets/vfx/cues/VfxCue_*.asset (SO)    │
        │ assets/shaders/Sprite_ColorWash.sg     │
        │ Sorting Layer "VFX_Overlay"            │
        │   (World < VFX_Overlay < UI Canvas)    │
        └────────────────────────────────────────┘
```

データフロー（class switch 例、Tier 1 後）:
```
Input (class slot 2 button)
  → ClassStateMachine.SwitchTo(slot 2)
      ├─ SpriteLibrary.spriteLibraryAsset = newSLA           (1 frame visual)
      ├─ AbilityExecutor.Configure(newClass.AbilitySet)
      └─ _vfxPublisher.PlayCue(_classSwitchCue, args)         (≤ 0.2ms)
            └─ VFXPublisherService.PlayCue
                  ├─ pool.Pop() or pool.PlaceFromPrefab()    (cache hit / cold miss)
                  ├─ instance.Transform.position = args.WorldPos
                  ├─ instance.ParticleSystem.Play()
                  └─ instance.ColorWashTrigger.Begin(args)
                        └─ Animated Quad shader _Progress 0→1 over 0.15s
```

### Key Interfaces

```csharp
// Game.Core.asmdef
namespace Game.Core.VFX
{
    public interface IVFXPublisher
    {
        /// <summary>Fire-and-forget. Caller does not own lifetime.</summary>
        void PlayCue(VfxCueDefinition cue, in VfxCueArgs args);

        /// <summary>Persistent / cancellable cue (e.g., burning aura).</summary>
        VfxHandle SpawnCue(VfxCueDefinition cue, in VfxCueArgs args);

        /// <summary>Bind motor events to default cue mappings (Tier 1 wiring).</summary>
        void SubscribeMotorEvents(ICharacterMotor motor);
    }

    [CreateAssetMenu(menuName = "Game/VFX/Cue Definition", fileName = "VfxCue_")]
    public sealed class VfxCueDefinition : ScriptableObject
    {
        [SerializeField] private string _cueId;             // "class_switch_orb_burst" — debug only
        [SerializeField] private AssetReferenceGameObject _prefabRef;
        [SerializeField] private int _maxConcurrent = 3;
        [SerializeField] private float _defaultLifetimeSec = 1.5f;
        [SerializeField] private bool _preloadOnSceneLoad = false;
        [SerializeField] private VfxLayer _layer = VfxLayer.World;
        [SerializeField] private OverflowPolicy _overflow = OverflowPolicy.DropNewest;
        [SerializeField] private AttachPolicy _attachPolicy = AttachPolicy.WorldSpace;

        public string CueId => _cueId;
        public AssetReferenceGameObject PrefabRef => _prefabRef;
        public int MaxConcurrent => _maxConcurrent;
        public float DefaultLifetimeSec => _defaultLifetimeSec;
        public bool PreloadOnSceneLoad => _preloadOnSceneLoad;
        public VfxLayer Layer => _layer;
        public OverflowPolicy Overflow => _overflow;
        public AttachPolicy AttachPolicy => _attachPolicy;
    }

    public enum VfxLayer { World, Overlay, UI }
    public enum OverflowPolicy { DropOldest, DropNewest }
    public enum AttachPolicy { WorldSpace, AttachToTransform }

    public readonly struct VfxCueArgs
    {
        public readonly Vector2 WorldPos;
        public readonly Transform AttachTo;
        public readonly Vector2 Direction;
        public readonly Color TintOverride;
        public readonly float ScaleMult;

        public VfxCueArgs(
            Vector2 worldPos,
            Transform attachTo = null,
            Vector2 direction = default,
            Color tintOverride = default,
            float scaleMult = 1f)
        {
            WorldPos = worldPos;
            AttachTo = attachTo;
            Direction = direction == default ? Vector2.up : direction;
            TintOverride = tintOverride;
            ScaleMult = scaleMult <= 0f ? 1f : scaleMult;
        }
    }

    public readonly struct VfxHandle
    {
        public readonly int Id;
        public VfxHandle(int id) { Id = id; }
    }
}
```

**Anchor cue（V1 で出荷する 5 種、`assets/vfx/cues/` 配下）**

| CueId | Trigger | Backbone 構成 | Preload |
|-------|---------|---------------|---------|
| `class_switch_orb_burst` | ClassStateMachine.SwitchTo（Tier 1） | ParticleSystem（オーブ burst） + Animated Quad ColorWash（0.15s） | true |
| `hit_spark` | Combat hit confirm（ADR-0005 future） | ParticleSystem（短命 spark） | true |
| `dust_landed` | ICharacterMotor.Landed | ParticleSystem（地面ダスト） | true |
| `hitstop_freeze_frame` | ICharacterMotor.HitstopApplied | フルスクリーン flash quad（短時間） | true |
| `orb_acquisition` | Class slot unlock | ParticleSystem（ringlight burst）+ glint sprite anim | true |

### Implementation Guidelines

1. **`Game.VFX.asmdef` 新規作成**: `Game.Core.asmdef` に依存、`Game.Gameplay.asmdef` には依存しない（依存方向は publisher → core のみ）
2. **`VFXPublisherService` install**: Scene root に GameObject 1 つ、`DontDestroyOnLoad` で永続化。Awake で Addressables preload を非同期発火
3. **Per-cue pool**: `Dictionary<VfxCueDefinition, Stack<PooledVfxInstance>>` + `Dictionary<VfxCueDefinition, int>` で active count 追跡。`PoolStack<>` 抽象を作らず Unity 標準コレクションで十分
4. **Lifetime ownership**: 各 prefab の `ParticleSystem.MainModule.stopAction = StopAction.Callback`、`PooledVfxInstance.OnParticleSystemStopped()` で pool 返却。万一通知漏れがあれば `WaitForSeconds(_definition.DefaultLifetimeSec * 1.5f)` Watchdog コルーチンが pool に強制返却
5. **Cold-miss policy**: `AssetReferenceGameObject.LoadAssetAsync<GameObject>()` の `IsDone == false` 時は呼出を silent drop + `Debug.Log(LogType.Warning)`（ConditionalAttribute UNITY_EDITOR）+ `_coldMissCount[cue]++` インクリメント。同期ロード（`WaitForCompletion`）は **使用禁止**（forbidden_patterns 登録）
6. **Color Wash quad**: `CinemachineBrain` GameObject の child に Quad mesh を配置、material の shader は `Sprite_ColorWash.shadergraph`（properties: `_RadialCenter` Vector2 UV、`_Progress` 0..1、`_BaseColor` Color）。Quad の SortingLayer = `VFX_Overlay`、SortingOrder は HUD Canvas より低い値に固定。`ColorWashController.Begin(VfxCueArgs args)` が `Material.SetFloat("_Progress", 0)` から `1` まで 0.15s で animator または DOTween で driving
7. **Sorting Layer setup**: Project Settings → Tags & Layers → Sorting Layers に `VFX_Overlay` を `Default`(World) と UI Canvas Sorting Layer の間に挿入。Editor validation script が `SortingLayer.layers` を Awake で検証
8. **NullVFXPublisher**: Tier 0 環境で `IVFXPublisher` 注入の no-op default。`PlayCue` / `SpawnCue` / `SubscribeMotorEvents` 全てが空実装。Tier 1 では bind を本実装に切替えるだけで consumer code は不変
9. **Roslyn analyzer rule（Tier 1 PR2 with）**: Gameplay assembly（`Game.Gameplay.asmdef`）から `SpriteRenderer.color` を write する代入式を診断、ビルドエラー化。Tier 1 後の inline tint 再混入を防止

## Alternatives Considered

### Alternative 1: VFX Graph を主バックボーンに採用

- **Description**: engine-reference の deprecation alignment、GPU/compute 駆動 particle simulation
- **Pros**: Unity 公式の推奨後継、ハイエンド表現、SRP Batcher / GPU Instancing の自動活用、Burst+Jobs と親和
- **Cons**: URP 2D Output Particle Sprite 統合に caveats（Output Particle URP Lit/Unlit Sprite が必要、2D Lights / Sprite Mask 連携限定）、Sub-emitter on collision / Trail Renderer 2D 連携の機能不足、現スコープ（20 emitter / hit spark / dust）に対して GPU/compute は overkill、エディタ プレビュー workflow 学習コスト 1-2 週間
- **Estimated Effort**: 中（プロトタイプ検証 + チームスキル習得 2-3 週間）
- **Rejection Reason**: MVP/VS スコープに対する複雑性過剰。**IVFXPublisher 抽象により後日 cue 単位で swap 可能なので将来移行を阻害しない**。Unity 6.4 LTS で Legacy ParticleSystem 削除予告された時点で再評価する（`api_decisions.vfx_particle_backbone.revisit_trigger` に記録）

### Alternative 2: ハイブリッド（hero=VFX Graph、cheap=Legacy）

- **Description**: ボス爆発 / 衰退演出のみ VFX Graph、hit spark / dust は Legacy ParticleSystem
- **Pros**: 必要な箇所のみ高機能、cheap 部分は既存スキル流用
- **Cons**: pooling 戦略 2 系統、editor preview workflow 2 系統、debug メンタルモデル 2 系統、IVFXPublisher 抽象の利点（backbone 隠蔽）が消える、小規模チーム（Solo dev）で保守コスト過大
- **Estimated Effort**: 大（両系統の保守 = 1.5-2倍）
- **Rejection Reason**: publisher pattern を採用するなら backbone は **1 種類に統一して抽象化**するのが筋。ハイブリッドは publisher 抽象を機能させない

### Alternative 3: ScriptableRendererFeature + RenderGraph custom pass で Color Wash

- **Description**: URP 公式拡張点、最も柔軟。fullscreen blit shader を class-switch phase に挿入
- **Pros**: 任意の post-process が可能、Volume framework 連携可、Render Pipeline Asset で ON/OFF 制御
- **Cons**: RenderGraph は Unity 6 新規 API（HIGH 知識リスク領域）、学習コスト 1-2 週間、HUD マスクは結局 Sorting Layer / Render Texture で実装する（custom pass 単体では HUD 除外できない）、debug 困難
- **Estimated Effort**: 大（学習 + プロトタイプ + デバッグ 2-3 週間）
- **Rejection Reason**: MVP に対する input/output 比が悪い。Animated Quad アプローチで同等の視覚を ~1 日で実装でき、art-bible 既存 shader budget（`Sprite_ColorWash.shadergraph`）も活用できる

### Alternative 4: Volume framework + Color Adjustments override

- **Description**: 公式 post-processing パイプライン。Volume を class-tagged にして weight アニメ
- **Pros**: 最もシンプル、URP 標準
- **Cons**: Color Adjustments は **画面均一 tint** のみ提供、Pillar 1 アート意図「画面四隅からの放射状 Color Wash」（art-bible.md Section 1 Principle 3）を表現不能
- **Estimated Effort**: 小
- **Rejection Reason**: アート意図違反。アート要件「放射状」が機能要件として確定済み

### Alternative 5: Per-sprite tint のみ（Tier 0 踏襲、フルスクリーン放棄）

- **Description**: MaterialPropertyBlock で全可視 sprite を tint。Tier 0 のまま据え置き
- **Pros**: 追加 shader 不要、最も保守的
- **Cons**: 「世界が染まる」体験が平板、Pillar 1 報酬感低下、CD1 で「色波形が画面全体を満たす」と明示の意図と矛盾
- **Estimated Effort**: ゼロ（変更なし）
- **Rejection Reason**: Pillar 1 の決定的アート要件違反。Tier 1 でフルスクリーン化する方針が CD1 で確定済み

### Alternative 6: PlayCue の cue 識別を string addressable key に

- **Description**: `PlayCue("class_switch_orb_burst", args)` のように文字列キーで cue 指定
- **Cons**: typo がコンパイル通過、`Find References` 不能、refactor-rename 不能、デザイナーが string を間違えると runtime 失敗
- **Rejection Reason**: 設計事故源。型安全性違反

### Alternative 7: PlayCue の cue 識別を enum `VfxCueId` に

- **Description**: `PlayCue(VfxCueId.ClassSwitchOrbBurst, args)`
- **Cons**: cue 追加ごとにコード編集必要 → デザイナーが SO 作成だけで cue 追加できる workflow が壊れる
- **Rejection Reason**: ScriptableObject 駆動の data-driven design 違反（technical-preferences.md「Magic numbers（ゲームバランス）— ScriptableObject に配置、コードハードコード禁止」と同じ思想）

### Alternative 8: cold-miss で `WaitForCompletion` 同期ロード

- **Description**: Addressables async ロードが未完了なら blocking 待機して必ず発火
- **Cons**: 初回 10-50ms の frame hitch、Pillar 1 の即時フィードバック（1 frame sync）を破壊、ボス遭遇時の最初のヒットが固まる
- **Rejection Reason**: Pillar 1 違反

### Alternative 9: cold-miss で queue → load 完了後 fire

- **Description**: ロード未完なら enqueue、完了後に再生
- **Cons**: 古いゲームイベントに対する stale な visual feedback、入力同期破綻、playtest で「何が起きたか分からない」フィードバック
- **Rejection Reason**: feel への悪影響大

### Alternative 10: 単一グローバル GameObject pool

- **Description**: VFX 全体で 1 つの `Stack<GameObject>` プール
- **Cons**: cue ごとに prefab root 構造が異なる、reset コスト高（component 設定全部書き戻し必要）、結局 typed bucket で実装することになる
- **Rejection Reason**: per-cue pool と運用上等価で API のみ汚染

### Alternative 11: Tier 1 移行を単一 PR で実施

- **Description**: ClassStateMachine.SwitchTo の inline tint 削除と publisher 注入を 1 PR で
- **Cons**: playtest A/B 不能、rollback path なし、ADR-0001 R5 の「もう一回切替えたい」体感失敗時に切戻し不可、Color Wash 視覚仕様が uniform tint → radial gradient に変わる **アップグレード** であるため一度に置換すると差分が観測しにくい
- **Rejection Reason**: 検証不能リスク。playtest 結果で立替判断可能な PR1（並行 feedback）→ PR2（cleanup）の分割を採用

## Consequences

### Positive

- gameplay system が VFX rendering と疎結合になる（ClassStateMachine / CharacterController2D は cue がどう描画されるかを知らない）
- pooling 中央化で予測可能なメモリ確保 / GC-free hot path
- デザイナーが SO asset 作成のみで新 cue 追加（コード編集不要）
- engine deprecation 移行は backbone 層 1 ファイル swap で済む（consumer code は不変）— Unity 6.4 で Legacy PS 削除されても VFXPublisherService 内部の `ParticleSystem.Play()` 呼出を `VFXGraph.SendEvent()` に置換するだけ
- Color Wash の HUD マスクは Unity 標準 Sorting Layer で済み、render-pass の脆弱な exclusion 不要
- ADR-0002 の replay 安全性が保たれる（VFX は gameplay state を mutate しない）
- ADR-0001 Tier 1 リファクタターゲットが具体化、story authoring 解禁

### Negative

- Tier 1 移行に PR が 2 本必要（double-feedback PR1 → cleanup PR2）。MVP scope 圧迫
- Legacy ParticleSystem deprecation により 2-3 年スパンで再評価コスト（Unity 6.4 アナウンス時要注意）
- art-bible.md の更新必須（`Camera.OnRenderImage()` 言及は無効、`Render Texture マスク` 表現は supersede）
- ADR-0001 Tier 0 の per-sprite tint は **refactor ターゲットではなく upgrade 対象**（uniform tint → radial gradient）。視覚変化が観測される
- editor 「Play in Scene View」プレビューボタンは ~1 時間の editor scripting 追加負担（option、必須ではない）
- 5 anchor cue 分の prefab + ParticleSystem セットアップ作業が ~1-2 日（テクニカルアーティスト工数）

### Neutral

- VFX Graph 採用は将来オプションとして残存（IVFXPublisher 抽象越しに置換可能）
- Tier 1 移行タイミングが ADR-0001 / ADR-0002 の Validation Gate 通過に従属するため、本 ADR Accepted と同時には着手できない（待ち状態を許容）

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| **R-A**: Legacy ParticleSystem が将来 Unity LTS で削除され migration が間に合わない | LOW | HIGH | IVFXPublisher 抽象で backbone swap を 1 ファイルに局所化。Unity 6.4 / 7.0 release notes を継続監視（`api_decisions.vfx_particle_backbone.revisit_trigger` に明記） |
| **R-B**: Cold-miss silent drop で gameplay-feel regression が見えなくなる | MEDIUM | MEDIUM | profiler counter で cold-miss 回数を常時記録。CI smoke test で「playable scene 5 anchor cue で cold-miss = 0」をアサート。`PreloadOnSceneLoad = true` を hot cue 全てに適用 |
| **R-C**: Color Wash quad の Sorting Layer 設定誤りで HUD 上に被る | MEDIUM | HIGH | Editor validation script で `VFX_Overlay` の SortingLayer.value < UI Canvas SortingLayer.value を Awake で assert（Unity Test Framework EditMode）。PR review チェックリスト追加 |
| **R-D**: `OnParticleSystemStopped()` 通知漏れで pool が枯渇 | LOW | MEDIUM | Watchdog コルーチン（`DefaultLifetimeSec * 1.5` で強制 pool 返却）。`StopAction.Callback` の Sub-emitter 含む全 emitter 動作を G1 検証で実測 |
| **R-E**: Tier 1 移行で feel が劣化（ADR-0001 R5 の「もう一回切替えたい」反応喪失） | MEDIUM | HIGH | PR1 で double-feedback（inline tint と publisher cue を並行発火）、playtest 通過後にのみ PR2 cleanup。NullVFXPublisher で Tier 0 互換も保つ |
| **R-F**: VfxCueArgs の `Direction == default` 判定が `Vector2.zero` と `Vector2(0,0) を引数指定` を区別不能 | LOW | LOW | `VfxCueArgs` を 0 = unspecified の規約とし、`Direction == default ? Vector2.up : direction` で正常化。ドキュメントに明記 |
| **R-G**: GC.Alloc が VfxCueArgs 以外の経路（closure, boxing, debug log）で発生 | MEDIUM | MEDIUM | `Debug.Log` を `[Conditional("UNITY_EDITOR")]` 化、profiler でホットパスの GC.Alloc を G2 で実測 |

## Performance Implications

| Metric | Before（Tier 0 inline） | Expected After（Tier 1 publisher） | Budget |
|--------|------------------------|-------------------------------------|--------|
| CPU PlayCue (per call) | N/A | 0.05-0.15ms | ≤ 0.2ms |
| CPU VFX total (worst frame) | ~0.3ms（per-sprite tint） | ~0.5ms（class-switch + 5 hit-sparks + 2 dust + 1 hitstop）+ Color Wash quad 0.1-0.3ms | ≤ 1.5ms |
| Memory (pool pre-alloc) | 0 | ~100 GameObject pre-pooled at startup | ≤ 30 MB |
| Load Time (scene load) | 0 | +500ms（5 anchor cue PreloadOnSceneLoad） | ≤ 2s 増分 |
| GC Alloc (PlayCue) | N/A | 0 byte / call（VfxCueArgs is readonly struct） | 0 byte |
| Network | N/A | N/A | N/A |

**Steam Deck 1080p VFX rendering**: G5 で `≤ 1.5 ms`（worst-case = 20 active emitter / boss scene）を実測検証。Unity Profiler を Steam Deck 実機接続で測定。

## Migration Plan

```
[現在] ── ADR-0001 (R5 待ち) ── ADR-0002 (V1-V5 待ち)
            │                       │
            └──── 両 gate Accepted ──┘
                       │
                       ▼
            ADR-0003 G1-G5 検証プロトタイプ
                       │
                       ▼
         PR1: IVFXPublisher 導入 + 二重 feedback
              ・IVFXPublisher / VfxCueDefinition / 5 anchor cue 実装
              ・ClassStateMachine に IVFXPublisher 注入（NullVFXPublisher デフォルト）
              ・PlayerVFXBinder で motor event subscribe
              ・inline tint と publisher cue が **並行発火**
                       │
                       ▼
            playtest 1 サイクル（5 名以上）
              ・「もう一回切替えたい」体感維持を確認
              ・cold-miss counter = 0 を確認
              ・HUD 視認性維持を確認
                       │
                       ▼
         PR2: inline 削除 cleanup
              ・ClassStateMachine から `_currentSpriteRenderer.color` 書込み削除
              ・`_audioSourceMinimal` field 削除（Audio System ADR と協調）
              ・CharacterController2D から inline impact flash 削除
              ・Roslyn analyzer rule で `SpriteRenderer.color` 書込みを
                gameplay assembly から禁止
```

**Step-by-step**:

1. ADR-0003 Accepted（G1-G5 通過）
2. ADR-0001 R5 Validation Gate 通過、ADR-0002 V1-V5 Validation Gate 通過（並行可）
3. **PR1（Tier 1 移行 — 二重 feedback）**:
   - `Game.VFX.asmdef` 作成、`IVFXPublisher` / `VfxCueDefinition` 実装
   - 5 anchor cue prefab + SO asset 作成
   - `Sprite_ColorWash.shadergraph` 実装
   - Sorting Layer `VFX_Overlay` 設定
   - `VFXPublisherService` を Scene root に install
   - `ClassStateMachine` に `IVFXPublisher` 注入（DI コンストラクタ or `[SerializeField]`）、`NullVFXPublisher` をデフォルト bind
   - `PlayerVFXBinder` を Player prefab に attach、motor event を subscribe
   - **inline tint コルーチン と publisher cue が並行発火** することを確認
4. **Playtest（PR1 後）**: 5 名以上の playtester に「もう一回切替えたい」反応を verify
5. **PR2（Tier 1 cleanup）**:
   - `ClassStateMachine` から `_currentSpriteRenderer.color` 書込みコルーチン削除
   - `_audioSourceMinimal` field 削除
   - `CharacterController2D` から inline impact flash 削除
   - Roslyn analyzer rule 追加（`SpriteRenderer.color` 書込み禁止 — gameplay assembly のみ）
6. ADR-0003 Status を `Accepted` に昇格、`docs/registry/architecture.yaml` の `vfx-system-future` placeholder を `vfx-publisher-system` 確定参照に置換完了報告

**Rollback plan**:

- **PR1 で feel 劣化が確認された場合**: `NullVFXPublisher` を本番 bind に戻し、publisher cue を無効化。inline tint だけが動作する Tier 0 状態に即時復帰可能（コード削除 0 行、設定 1 箇所変更）
- **PR2 後に visual regression が判明した場合**: `git revert` で PR2 のみ revert、PR1 の double-feedback 状態に戻る。さらに必要なら PR1 も revert で完全 Tier 0 状態へ
- **G2（hot path 性能）が達成不能の場合**: pool size を増やす、Addressables preload を全 cue 強制 true、debug log を完全削除して再測定。それでも未達なら ADR Status を `Superseded` にし VFX Graph 移行 ADR-0003' を起こす

## Validation Criteria

Validation Gate G1-G5 全通過で `Proposed` → `Accepted` に昇格する。

- [ ] **G1 — Anchor Cue Coverage**: 5 anchor cue（class_switch_orb_burst / hit_spark / dust_landed / hitstop_freeze_frame / orb_acquisition）が `IVFXPublisher.PlayCue` 経由で正しく再生される。Editor scene で manual test。各 cue について「PlayCue → 視覚出現 → DefaultLifetimeSec 後に pool 返却」のサイクルを目視確認
- [ ] **G2 — Pool Hot Path Profile**: Unity Profiler EditMode で `PlayCue` を 1000 回 / 60s 連続発火、p99 ≤ 0.2ms、GC alloc = 0 byte / call、20 emitter 同時発火 scene で 60 fps 維持
- [ ] **G3 — Color Wash Sorting**: Class switch 中の Pause-frame screenshot で Color Wash quad が HUD Canvas より下に配置されている（HUD readable）。screenshot を `production/qa/evidence/adr-0003-g3-colorwash-sort.png` に保管
- [ ] **G4 — Cold-miss Telemetry**: 5 分間プレイで preload 済 anchor cue の `VFXPublisherService.ColdMissCount[cue]` が 全 cue で 0
- [ ] **G5 — Steam Deck Performance**: 1080p Steam Deck 実機で 20 emitter scene の Profiler 接続実行、VFX rendering ≤ 1.5ms（worst-case）。実機測定 evidence を `production/qa/evidence/adr-0003-g5-steamdeck-profile.json` に保管

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|---------------------------|
| `design/gdd/game-concept.md` | Pillar 1（切替が、花になる） | 「切替自体が視覚/聴覚の報酬」即時 1-frame 報酬 | `class_switch_orb_burst` cue + Animated Quad Color Wash で実現。`IVFXPublisher.PlayCue` の hot path budget ≤ 0.2ms により 1-frame sync を保証 |
| `design/gdd/systems-index.md` | Architecture Note A4 (#16 VFX System) | Core 層 / `IVFXPublisher` 提供 / zero deps / pub/sub サービス | 本 ADR が `IVFXPublisher` を `Game.Core.asmdef` で確定。consumer は signal/event 経由でのみ subscribe |
| `design/gdd/systems-index.md` | Creative Director Note CD1 | Tier 0 minimal feedback (color-wash 0.1-0.2s + SE) | Tier 0 inline は ADR-0001 ClassStateMachine 所管を維持。本 ADR は Tier 1 移行先（IVFXPublisher）を確定し、Tier 0 → Tier 1 migration の PR 分割を規定 |
| `design/gdd/systems-index.md` | Creative Director Note CD2 | Combat hit feedback (hitstop + knockback + impact frame) | `hit_spark` + `hitstop_freeze_frame` cue が `motor_event_notification.HitstopApplied` 経由で発火。**hitstop solver-skip 自体は Motor 所管（VFX 不介入）** を本 ADR で再確認 |
| `design/art/art-bible.md` | Section 1 Principle 3 | 「画面四隅からの放射状 Color Wash」 | Animated Quad + `Sprite_ColorWash.shadergraph`(`_RadialCenter`/`_Progress`) で放射状グラデーションを実装。art-bible.md 旧仕様（`Camera.OnRenderImage` / Render Texture マスク）は本 ADR で supersede |
| `design/art/art-bible.md` | G-6 / G-7 Performance budgets | 20 ParticleSystem / 60 draw calls / 150 MB texture / custom shader 5 種（含 Sprite_ColorWash） | Per-cue MaxConcurrent で aggregate 強制、`VFXPublisherService` で常時計測。`Sprite_ColorWash.shadergraph` を本 ADR の Color Wash 実装に流用 |
| `design/art/art-bible.md` | Conflict 2 両立解 | HUD Canvas は Color Wash の影響を受けない | Sorting Layer `VFX_Overlay` < UI Canvas Sorting Layer の sorting 順で除外 |

## Related

- **Depends on**: [ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md)（Tier 1 リファクタターゲット）
- **Depends on**: [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md)（motor_event_notification 購読源）
- **Enables**: ADR-0004 Class Abilities（将来）— ability cue で IVFXPublisher 参照
- **Enables**: ADR-0005 Combat（将来）— hit / status feedback で IVFXPublisher 参照
- **Enables**: ADR-0006 Audio System（将来）— `IAudioPublisher` を本 ADR の pooling semantics で対称設計
- **Supersedes (partial)**: `design/art/art-bible.md` lines 1077-1079 の `Render Texture マスク` 表現（Sorting Layer アプローチに置換）、line 1574 の「ParticleSystem 上限 20 個」表現（backbone 明記化）
- **Engine reference**: `docs/engine-reference/unity/breaking-changes.md`（Legacy PS deprecation, URP Compatibility Mode 削除）、`deprecated-apis.md`（Legacy ParticleSystem）、`current-best-practices.md`（URP 2D / Burst+Jobs）、`modules/rendering.md`（RenderGraph / Volume / SRP Batcher）
- **Implementation files (post-Accepted)**: `src/core/vfx/IVFXPublisher.cs`、`src/core/vfx/VfxCueDefinition.cs`、`src/vfx/VFXPublisherService.cs`、`assets/shaders/Sprite_ColorWash.shadergraph`、`assets/vfx/cues/VfxCue_*.asset` × 5
