# ADR-0006: Camera System (Thin Provisional — Cinemachine 3 + ICharacterMotor.Position Follow + Single-Scene PolygonCollider2D Confiner, pending R1 spike)

## Status

**Proposed (Provisional, Validation Gate: C0-C1 — pending R1 spike + ADR-0002 V1 + Class-Switch prototype)**

> 本 ADR は **thin provisional** scope で起草された。`/autoplan` Phase 4 USER CHALLENGE で CEO + Eng 両モデル独立 dual voice review が 8 cross-phase systemic themes を flag した結果、ユーザは Option B（Locked Decisions 3 件 + Deferred Decisions 11 件 + R1 spike 前提）を採択。本 ADR は Cinemachine 3 採用 / ICharacterMotor.Position follow / 単一 PolygonCollider2D Confiner の 3 点のみを lock し、Body component 名 / Damping / Foundation Singleton 適用 / Pixel Perfect Reference Resolution / forbidden patterns / Look Ahead 等 11 件は **R1 spike 通過 + ADR-0002 V1 通過 + Class-Switch prototype 実行** 後の follow-up ADR で empirical data ベースで lock する。Validation Gate **C0（R1 spike completion）** が偽の場合、Cinemachine 3.x API surface の前提が崩壊し本 ADR を Superseded として再起草する。

## Date

2026-04-28

## Last Verified

2026-04-28

## Decision Makers

- Project Lead（ユーザ）— 最終決定権、`/autoplan` Phase 4 で Option B 採択
- `creative-director` 経由 CD-SYSTEMS Note — 但し camera craft（Look Ahead / Vertical Look / Dynamic Zoom）は本 ADR scope 外で follow-up ADR に defer
- `technical-director` 経由 TD-ADR gate — 但し本 ADR は thin provisional のため fully reviewed gate は follow-up ADR で
- `unity-specialist` — Cinemachine 3 / URP 2D 2D Renderer / Pixel Perfect Camera 3 者統合の R1 spike 結果に基づく review（spike 後）

## Summary

本作 `職業オーブのレガシー` の Camera System を **Cinemachine 3 + 単一 CinemachineConfiner2D + ICharacterMotor.Position follow** の 3 点で provisional 確定する。Tier 0 MVP の単一シーン playable scene を unblock する最小決定のみを lock し、Cinemachine Body component（PositionComposer 候補）/ Damping 値 / Foundation Singleton 適用 / Pixel Perfect Camera Reference Resolution（art-bible 384×216 vs game-concept.md 128×128 inconsistency 解消後）/ Look Ahead / Vertical Look / Dynamic Zoom / Camera Shake routing / CharacterFollowProxy vs ICharacterMotor.TransformReadProxy / forbidden patterns 4 件 / 6 anchor shake profiles / performance budgets は **deferred** とする。R1 Editor spike（半日）で Cinemachine 3.x API 名 12 箇所を検証し、`production/qa/evidence/r1-camera-cinemachine3-spike-result.md` に記録した結果に基づき follow-up ADR で完全仕様を lock する。`/autoplan` Phase 1-4 で発見された 8 cross-phase systemic themes（premature lock / Tier 1 under MVP / Foundation Singleton cargo-cult 可能性 / magic numbers / gates not enforceable / craft missing / proxy proliferation / shake API inflation）はすべて follow-up ADR の前提作業に組み込まれる。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Rendering（Cinemachine 3 + URP 2D + Pixel Perfect Camera） |
| **Knowledge Risk** | **MEDIUM-HIGH** — Cinemachine 3.x は Unity 6.0+ の新 API 系統で、2.x の `CinemachineVirtualCamera` / `FramingTransposer` 命名から大幅 rename された。LLM 訓練データは 2.x が主体。`PositionComposer` / `CinemachineCamera` / `CinemachineConfiner2D.InvalidateBoundingShapeCache` / `CinemachineImpulseSource.GenerateImpulse` overload / `CinemachinePixelPerfect` extension の 3.x 提供有無は engine-reference 未収録。R1 Editor spike で 12 箇所すべてを検証してから follow-up ADR で lock する |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`docs/engine-reference/unity/current-best-practices.md` line 24（"Cinemachine 3 2D Confiner"）、`docs/engine-reference/unity/modules/rendering.md`、`.claude/docs/technical-preferences.md`（`com.unity.cinemachine` 採用、URP 2D Renderer、PC + Steam Deck target）、`design/gdd/game-concept.md` line 239（Cinemachine 3 + 2D Confiner Extension + CinemachineCamera 新 API）、`design/gdd/systems-index.md` #7 Camera System（MVP Core 層、CharacterController2D 依存）、`docs/architecture/tr-registry.yaml`（TR-camera-001 / TR-camera-002）、`docs/architecture/adr-0002-character-controller-motor.md`（ICharacterMotor.Position read-only Vector2 contract）、`docs/architecture/adr-0003-vfx-system-boundary.md`（CinemachineBrain 直下 Animated Quad の Color Wash 階層前提）、`design/art/art-bible.md` line 656（基準解像度 384×216）/ line 658（Pixel Perfect Camera Integer Scale のみ）/ line 1410（前景キャラ 48×48）— 但し game-concept.md line 313-315 が 128×128 で不整合のため Pixel Perfect Reference は本 ADR で lock しない |
| **Post-Cutoff APIs Used** | （**lock せず deferred、R1 spike で検証**）想定: `Unity.Cinemachine.CinemachineCamera` / `CinemachineBrain` / `CinemachineConfiner2D` / `CinemachineImpulseSource` / `CinemachineImpulseListener` / Body component（`PositionComposer` 候補、旧 `FramingTransposer`）/ `UnityEngine.U2D.PixelPerfectCamera`。具体 API surface は R1 spike `production/qa/evidence/r1-camera-cinemachine3-spike-result.md` に記録 |
| **Verification Required** | **R1 Editor Spike（C0 prerequisite）— 半日 sandbox in Unity 6.3 LTS macOS Editor で 12 項目検証**: (1) `CinemachineCamera` クラス参照 + Inspector attach 動作、(2) `CinemachineBrain.UpdateMethod` 既定値（LateUpdate / SmartUpdate / FixedUpdate / ManualUpdate）、(3) `CinemachineCamera.Follow` プロパティ型確認、(4) Body component 命名（PositionComposer / Hard Look At / Tracked Dolly 等の 3.x 名）、(5) `CinemachineConfiner2D` extension 提供有無 + `BoundingShape2D` プロパティ型 + `InvalidateBoundingShapeCache()` メソッド名、(6) `CinemachineImpulseSource.GenerateImpulse()` overload（引数なし / `Vector3 velocity` / `float force`）、(7) `CinemachineImpulseListener` プロパティ名（Gain / Use2DDistance / ChannelMask / UseSignalSpaceOnly）、(8) `CinemachinePixelPerfect` extension の Cinemachine 3 提供有無（2.x sample のみだった可能性）、(9) URP 2D + Cinemachine 3 + Pixel Perfect Camera 三者統合動作 + Unity Issue Tracker 既知 stutter 状況、(10) `PixelPerfectCamera.refResolutionX/Y` プロパティ名 + Crop Frame enum 値、(11) `CinemachineBrain.UpdateMethod = LateUpdate` での Follow target 解決順序と `[DefaultExecutionOrder]` の関係、(12) `CinemachineBrain.OutputCamera` プロパティで実 camera transform を取得できる経路 |

> **Note**: 本 ADR の Knowledge Risk は MEDIUM-HIGH のため、`com.unity.cinemachine` のメジャーバージョンが 4.x に上がった場合や `CinemachineCamera` API surface が変更された場合、本 ADR を Superseded にし新 ADR を起こすこと。R1 spike で 12 項目のうち **3 件以上が unverified** だった場合、本 ADR の Status を Superseded → 再起草へ。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md) — 本 ADR の "Locked Decision 2"（Camera follow target は ICharacterMotor.Position）は ADR-0002 の `state_ownership.motor_position` interface に直接依存。**ADR-0002 V1 通過後**に本 ADR の C1 検証が可能（V1 = `Physics2D.SyncTransforms()` + Cast 検証 + ICharacterMotor.Position 確定形）|
| **Coordinates with** | [ADR-0003 VFX System Boundary](adr-0003-vfx-system-boundary.md)（CinemachineBrain 直下 Animated Quad の Color Wash 階層前提を維持、本 ADR では camera GameObject 階層に Sorting Layer `VFX_Overlay` の前提として記載のみ）、[ADR-0005 Input System Architecture](adr-0005-input-system-architecture.md)（`architectural_stances.foundation_singleton_pattern` を **camera で適用するか deferred** — Camera は scene composition で infrastructure ではない可能性、follow-up ADR で R1 spike 結果と DDOL+scene refs の架構整合性を再評価）|
| **Requires (revise requests)** | **None for provisional scope**。但し follow-up ADR では: (a) ADR-0002 への `ICharacterMotor.TransformReadProxy { get; }` 提案（CharacterFollowProxy 増殖防止のため motor 自身が owned proxy を持つ案、`/autoplan` Claude CEO Finding 5 + Eng F2 由来）、(b) ADR-0003 への Camera Shake routing hybrid 提案（motor event 直接 subscribe vs `IVFXPublisher.PlayCue` 経由の境界再評価、`/autoplan` Claude Eng F7 由来） |
| **Enables** | systems-index #7 Camera System provisional 化（Tier 0 MVP playable scene の最小 camera）。**Full enablement は follow-up ADR で**: 単一 CinemachineCamera + Confiner で MVP scene が可、boss arena 用 multi-vcam / Look Ahead / Vertical Look / Dynamic Zoom / Camera Shake は VS / Tier 1 |
| **Blocks** | **None for provisional scope**。Tier 0 MVP の他 8 systems は本 ADR の locked 3 件で unblock 可能。`/architecture-review` coverage 計測上は TR-camera-001 / TR-camera-002 を partial cover（API 詳細は spike 後に full cover）|
| **Ordering Note** | systems-index Recommended Design Order **#6 Camera System** (Core 層、MVP)。本 ADR provisional で起草、R1 spike + ADR-0002 V1 + Class-Switch prototype（ADR-0001 R5 spike）3 つすべて通過後、follow-up ADR（仮称 ADR-0006a Camera System Implementation）で完全仕様を lock。3 spikes は独立で並列実行可。`/architecture-review coverage` を新規セッションで先行実行し本 ADR provisional 反映を確認 |

## Context

### Problem Statement

本作のゲームプレイ核心（Pillar 1「切替が、花になる」/ Pillar 3「歯ごたえ」/ メトロイドヴァニア探索フィードバック）は **camera が motor の動きに 1 frame sync で追従し、職業切替時に Color Wash 表現の前提となる CinemachineBrain 階層を提供する**ことを Tier 0 MVP で要求する。`design/gdd/systems-index.md` は Camera System を #7 Core 層 MVP として、`design/gdd/game-concept.md` line 239 は Cinemachine 3 + 2D Confiner Extension + CinemachineCamera 新 API を技術選定として記載、`docs/architecture/tr-registry.yaml` は TR-camera-001 / TR-camera-002 を登録済。

しかし `/autoplan` Phase 1-4 review（CEO + Eng dual voices, 6/6 + 5/6 dimensions disagree）で **完全 ADR scope（51KB プラン、9 registry entries、4 forbidden patterns、完全 ICameraDirector interface、Foundation Singleton stance、Damping 値固定、Look Ahead 不採用）は systemic に premature** と判定された。具体的に:

- **Cinemachine 3.x API は 2.x から大幅 rename**: `CinemachineVirtualCamera` → `CinemachineCamera`、`FramingTransposer` → `PositionComposer` 候補、Body component model 改訂。LLM 訓練データは 2.x 主体で 12 箇所の API 名が unverified。Editor spike なしに ADR で lock すると **policy un-wind コストが大きい**
- **ADR-0002 V1 未通過**: motor.Position の precision / timing / interpolation 仕様未確定で、camera の "1 frame sync" claim は physics clock (50Hz) と render clock (60+Hz) の boundary を未考慮（FixedUpdate 駆動 motor + LateUpdate 駆動 camera の skew）
- **art-bible vs game-concept.md 解像度 inconsistency**: art-bible L656 は 384×216 / L1410 は char 48×48 だが game-concept.md L313-315 は 128×128 / 96×96 / 64×64。Pixel Perfect Reference を camera ADR で lock するのは backwards で、上流 GDD 解消が前提
- **Class-Switch prototype 未実行**: 実際の camera feel（Damping「粘り」/ Look Ahead 距離 / Vertical Look 持続時間）は ref ゲーム比較 + 体感調整で決まる empirical 領域、紙上 magic numbers 固定不可

このため本 ADR は **thin provisional** で 3 件のみ lock、残 11 件は R1 spike + ADR-0002 V1 + Class-Switch prototype 通過後に follow-up ADR で empirical data ベースで lock する。

### Current State

- `.claude/docs/technical-preferences.md` で `com.unity.cinemachine` 採用、URP 2D Renderer、PC + Steam Deck target が確定済
- `design/gdd/game-concept.md` line 239 で Cinemachine 3 + 2D Confiner + CinemachineCamera 新 API が技術選定として記載
- `design/gdd/systems-index.md` #7 Camera System が Core 層 MVP（単一シーン）/ VS（multi-room）として記載、CharacterController2D + Scene & Addressables (VS) 依存
- `design/art/art-bible.md` で Pixel Perfect Camera Integer Scale のみ / Mip OFF / PPU=100 の規律が確定。基準解像度 L656 は 384×216（代替 360×224）、前景キャラ L1410 は 48×48
- `design/gdd/game-concept.md` line 313-315 は **art-bible と不整合の 128×128 / 96×96 / 64×64**（既存 GDD revision flag に追加要請）
- `docs/architecture/tr-registry.yaml` v3 で TR-camera-001（Cinemachine 3 + 2D Confiner + CinemachineCamera 新 API）、TR-camera-002（CharacterController2D 追従 / ICharacterMotor.Position 経由）の 2 件が登録済
- ADR-0002 が `state_ownership.motor_position` を read-only Vector2 で確定、`forbidden_patterns.external_motor_state_write` で外部 mutation 禁止
- ADR-0003 が CinemachineBrain 直下 Animated Quad の Color Wash 階層を確定、`forbidden_patterns.vfx_camera_onrenderimage` / `vfx_renderpipeline_compatibilitymode` を確定
- ADR-0005 が `architectural_stances.foundation_singleton_pattern` を Foundation 5 ADR 統一として確定（但し本 ADR Camera で適用するかは deferred）
- registry に Camera System 関連 stance なし（本 ADR で minimal stance を追加）
- `docs/architecture/architecture-review-2026-04-28.md` が ADR-0006 Camera System を Top ADR gap として識別、本 ADR で provisional 解消

### Constraints

- **Engine**: Unity 6.3 LTS / `com.unity.cinemachine` 3.x のみ採用、Cinemachine 2.x `CinemachineVirtualCamera` legacy API は禁止（engine-reference/deprecated-apis.md に追記）
- **Render Pipeline**: URP 2D Renderer のみ、Built-in RP / HDRP / URP Compatibility Mode 全面禁止（ADR-0003 forbidden_patterns 継承）
- **Motor contract**: ICharacterMotor.Position（ADR-0002 read-only Vector2）経由のみ、motor の Rigidbody2D / Transform 直接参照禁止
- **Color Wash 階層**: CinemachineBrain 直下 Animated Quad の Sorting Layer `VFX_Overlay` 前提を維持（ADR-0003 確定）
- **MVP scope**: 単一シーン、単一 PolygonCollider2D Confiner、boss arena framing / multi-vcam / room transition は VS 以降
- **Knowledge gap**: Cinemachine 3.x API 名 12 箇所 unverified、Editor spike なしに具体 component 名で ADR を lock しない

### Requirements

- **TR-camera-001**: Cinemachine 3 + 2D Confiner Extension、CinemachineCamera 新 API → Locked Decision 1 + 3 で部分カバー（具体 component 名は R1 spike 後）
- **TR-camera-002**: CharacterController2D に追従、ICharacterMotor.Position 経由 → Locked Decision 2 で完全カバー
- **MVP playable scene**: 単一シーンで player を follow し境界を遵守する camera → Locked Decision 1 + 2 + 3 の組合せで実現
- **Color Wash co-location**: ADR-0003 の CinemachineBrain 直下 Animated Quad 階層を破壊しない → Locked Decision 1 で前提継承
- **Steam Deck Verified path**: Tier 2a 申請対象 → 本 ADR では Pixel Perfect 設定を lock せず、art-bible vs game-concept.md inconsistency 解消 + R1 spike 結果に基づき follow-up ADR で lock

## Decision

### Locked Decisions（3 件のみ — 本 ADR で確定）

#### Decision 1 — Camera Package: Cinemachine 3

`com.unity.cinemachine` (Unity 6.3 LTS 同梱版、3.x 系統) を採用する。Cinemachine 2.x の `CinemachineVirtualCamera` / `CinemachineBrain` 旧 API、Built-in RP / HDRP 統合 / URP Compatibility Mode 経由の camera 拡張は **forbidden**。Camera 拡張が必要な場合は Cinemachine 3 標準 component（CinemachineConfiner2D / CinemachineImpulseSource / CinemachineImpulseListener / Body components など）または Cinemachine Extension 経由のみ。

**Cinemachine 2.x legacy API 禁止の根拠**: Unity 6.3 LTS 同梱は Cinemachine 3.x、新規プロジェクトで 2.x 採用は逆走。`CinemachineVirtualCamera` は 3.x で deprecated shim（期間限定）。本 ADR では per-ADR forbidden_pattern として登録せず、`docs/engine-reference/unity/deprecated-apis.md` に Unity 6.3 LTS engine-wide deprecation 事実として記載する（`/autoplan` Claude Eng Finding 5 採用 — analyzer cost-benefit 不明、asmdef boundary + PR review で運用）。

#### Decision 2 — Follow Contract: ICharacterMotor.Position

Camera は ADR-0002 で確定した `ICharacterMotor.Position`（read-only Vector2 property、MovePosition + Physics2D.SyncTransforms() 適用後の値）を follow target source として使用する。`ICharacterMotor` を実装する MonoBehaviour（CharacterController2D）の Transform / Rigidbody2D を camera 経路から **直接参照禁止**（ADR-0002 forbidden_pattern `external_motor_state_write` の範囲を camera read 経路にも適用）。

**Cinemachine の Transform contract bridge 実装方式は本 ADR で lock しない（deferred）**: CinemachineCamera.Follow が Transform を要求するのに対し、ICharacterMotor.Position は readonly Vector2 で Transform を expose しない。Bridge 候補は (a) CharacterFollowProxy MonoBehaviour（service が proxy GameObject を生成、毎 LateUpdate で `transform.position = motor.Position`）、(b) ICharacterMotor.TransformReadProxy { get; } 提案（ADR-0002 への逆提案、motor 自身が owned proxy を持つ）、(c) 他案。R1 spike + ADR-0002 V1 通過 + 1-frame sync の physics clock vs render clock skew 検証後、follow-up ADR で empirical data ベースで決定する。

#### Decision 3 — MVP Confiner: 単一シーン PolygonCollider2D 直配置

MVP Tier 0 単一シーンには 1 つの `PolygonCollider2D`（IsTrigger=true）を直配置し、`CinemachineConfiner2D.BoundingShape2D` に Inspector アサインする。VS multi-room 対応（`SetConfiner(Collider2D)` API + `InvalidateBoundingShapeCache()` 呼出 / RoomBounds ScriptableObject 等）は本 ADR で lock せず、follow-up ADR で room transition 仕様確定後に extend する。

**MVP 単一シーンの根拠**: systems-index.md MVP scope は「単一シーン」確定（Production Constraint Option A）、9 systems / 6 weeks の中で multi-room は VS 以降。Cinemachine 3 `CinemachineConfiner2D.InvalidateBoundingShapeCache()` API 名は R1 spike 検証項目 #5。

### Deferred Decisions（11 件 — follow-up ADR で empirical data ベースで lock）

| # | Deferred decision | Defer to | Reason / Source |
|---|-------------------|----------|-----------------|
| D1 | Cinemachine Body component（PositionComposer vs Hard Look At vs custom） | R1 spike #4 | 3.x の Body component 命名と機能差は Editor で実機確認必須、Damping behavior は 2.x と異なる可能性 |
| D2 | Damping X/Y 値 | Designer playtest with class-switch prototype（ADR-0001 R5 spike 通過後） | "magic numbers" 規律違反、Hollow Knight 級「粘り」表現は ref ゲーム比較 + 体感調整必須 — `/autoplan` CEO + Eng 共通指摘 |
| D3 | Foundation Singleton stance for Camera | post-R1 spike re-evaluation | `/autoplan` Codex CEO Finding 3「cargo-cult」: Camera は scene composition で infrastructure (save/input) ではない可能性、Eng F2「DDOL+scene-local refs 矛盾」(scene unload で SerializeField null) — DDOL 不採用案を spike 結果と合わせて follow-up ADR で評価 |
| D4 | ICameraDirector full surface（FixedPoint / Composite / RequestShakeAt / CancelAllShakes / CameraMoved / Snapshot） | follow-up ADR | `/autoplan` Eng F4「API inflation without backing model」、F5「observability API が service.transform を sample（actual motion は CinemachineBrain.OutputCamera 由来）→ telemetry day-one garbage」。Minimal interface のみ本 ADR で確定、full surface は CinemachineBrain.OutputCamera.transform 経由再設計 |
| D5 | Camera Shake routing（motor event direct subscribe vs VFX cue 経由 vs hybrid） | ADR-0003 G1-G5 通過後 | IVFXPublisher.SubscribeMotorEvents 実装と並走、boss attack の VFX 主導 shake 用 hybrid 経路は ADR-0003 + ADR-0006 両 Accepted 後に再評価 |
| D6 | CharacterFollowProxy MonoBehaviour vs ICharacterMotor.TransformReadProxy 提案 | ADR-0002 への逆提案セッション | `/autoplan` Claude CEO Finding 5: AudioListener / Trail / Light2D / Targeting reticle で同要件発生、proxy 増殖防止のため motor 側 owned proxy 案を ADR-0002 で議論 |
| D7 | Pixel Perfect Camera Reference Resolution（384×216 vs 360×224 vs other） | art-bible L656 vs game-concept.md L313-315 GDD inconsistency 解消後 | art-bible 権威ソースが alternate 解像度提示、game-concept.md は 128×128 のまま — 上流で resolve せずに ADR で lock するのは backwards（`/autoplan` Codex CEO Finding 2） |
| D8 | Pixel Perfect Camera Crop Frame setup（Pillarbox+Letterbox / Stretch Fill / Filter Mode） | R1 spike #10 + Steam Deck native 1280×800 実機検証 | URP 2D 6.3 + Cinemachine 3 + Pillarbox+Letterbox の組合せ動作未検証、Unity Issue Tracker で stutter 既知 issue（`/autoplan` Claude Eng F6） |
| D9 | 6 CameraShakeProfile anchor SO assets（land soft / land heavy / hitstop micro / hitstop heavy / wall bump / class switch） | Designer + class-switch prototype validation | magnitude / duration 数値は ref ゲーム比較 + 体感調整、紙上固定不可 — `/autoplan` CEO + Eng 共通指摘 |
| D10 | Performance budgets（camera_lateupdate_per_frame / pixelperfect_rescale_per_frame） | R1 spike + Steam Deck 実機測定 | 0.01ms 単位の decomposition は実装前に invented、Steam Deck 1080p vs 1280×800 native の混在も解消必要（`/autoplan` Codex Eng F9） |
| D11 | Forbidden patterns 4 件のうち 3 件 | 個別判断 | `camera_directly_reads_motor_transform` のみ本 ADR で registry に追加 / `cinemachine_virtualcamera_legacy_api` は engine-reference/deprecated-apis.md / `direct_maincamera_transform_manipulation` + `camera_shake_via_transform_mutation` は asmdef boundary + PR review で運用、Roslyn analyzer は Tier 0 cost-benefit 不明（`/autoplan` Claude Eng Finding 5 採用） |

### R1 Spike Protocol（C0 prerequisite — Accepted 昇格 gate）

**Scope**: Unity 6.3 LTS macOS Editor（pre-production、Unity project 未初期化のため本 spike が Unity project 初期化の最初の作業の 1 つ）で半日 sandbox。

**Setup**:
1. 新規 Unity 6.3 LTS project（empty 2D template）
2. `com.unity.cinemachine` + `com.unity.render-pipelines.universal` + `com.unity.2d.tilemap.extras` を Package Manager から追加
3. URP 2D Renderer + 2D Renderer Asset 設定
4. Empty scene に Main Camera + CinemachineBrain + 1 CinemachineCamera + PolygonCollider2D + PixelPerfectCamera 配置

**12 Verification Items**:
1. `Unity.Cinemachine.CinemachineCamera` クラス参照 + Inspector attach 動作 → OK / 命名 X
2. `CinemachineBrain.UpdateMethod` enum 既定値（LateUpdate / SmartUpdate / FixedUpdate / ManualUpdate のどれか）
3. `CinemachineCamera.Follow` プロパティ型（`Transform` で確定確認）
4. Body component 命名 — `PositionComposer` の確認、または `FramingTransposer` 残存 / 新名 X
5. `CinemachineConfiner2D` extension 提供有無 + `BoundingShape2D` プロパティ型 + `InvalidateBoundingShapeCache()` メソッド名 + 公開アクセシビリティ
6. `CinemachineImpulseSource.GenerateImpulse()` overload — 引数なし / `Vector3 velocity` / `float force` / その他
7. `CinemachineImpulseListener` プロパティ名 — `Gain` / `Use2DDistance` / `ChannelMask` / `UseSignalSpaceOnly` の 3.x 名
8. `CinemachinePixelPerfect` extension の Cinemachine 3 提供有無（2.x sample のみだった可能性）
9. URP 2D + Cinemachine 3 + Pixel Perfect Camera 三者統合動作確認 + Unity Issue Tracker 既知 stutter Issue 検索ログ
10. `UnityEngine.U2D.PixelPerfectCamera.refResolutionX/Y` プロパティ名 + Crop Frame enum 値（None / Pillarbox / Letterbox / Windowbox / Stretch Fill）
11. `CinemachineBrain.UpdateMethod = LateUpdate` 設定下での Follow target 解決順序、`[DefaultExecutionOrder]` MonoBehaviour との関係（physics clock vs render clock 検証の前段）
12. `CinemachineBrain.OutputCamera` プロパティで実 camera transform を取得できる経路（observability API 再設計前提）

**Output Format** — `production/qa/evidence/r1-camera-cinemachine3-spike-result.md`:

```markdown
# R1 Spike Result — Camera + Cinemachine 3 API Verification

**Date**: YYYY-MM-DD
**Unity version**: 6.3.x
**com.unity.cinemachine version**: 3.x.x
**Status**: PASS / PARTIAL / FAIL

## Verification Results (12 items)

| # | Item | Status | Found name (if rename) | Notes |
|---|------|--------|------------------------|-------|
| 1 | CinemachineCamera | OK / X | — | Inspector screenshot: ... |
| ... | | | | |

## Unity Issue Tracker Search Log

- Cinemachine 3 + URP 2D PPC: [link / open issue summary]
- ...

## Conclusion

- 12 / 12 verified → ADR-0006 Accepted gate C0 PASS
- N / 12 mismatched → 代替名 / 不在対応案を follow-up ADR で
- 3+ / 12 unverified → ADR-0006 Superseded、再起草

## Screenshots / Inspector exports

- CinemachineCamera Inspector view
- PixelPerfectCamera Inspector view
- CinemachineConfiner2D Inspector view
- ...
```

### Validation Gates（C0-C1 のみ — provisional）

- **C0 — R1 Spike Completion**: 半日 Editor sandbox で 12 verification items を実行、`production/qa/evidence/r1-camera-cinemachine3-spike-result.md` を書込。OK / Rename / 不在 / 代替案 のいずれかで全 12 項目が結論済。3 項目以上 unverified なら本 ADR を Superseded 化
- **C1 — Provisional Follow Basic**（ADR-0002 V1 通過後）: motor.Position drives camera follow within 1 render frame、Brain.UpdateMethod を architecture で pin（spike #2 結果ベース）、30/60/120Hz render × 50Hz physics matrix で sync 検証、`Time.timeScale=0.1` slow-mo + hitstop+dash 組合せで 1-frame divergence ≤ 0.005 unit p99（PlayMode test）

> **C2-C5 は本 ADR では gate 化せず、follow-up ADR で定義**: stutter / Confiner bounds / Camera Shake / Steam Deck performance はすべて Deferred Decisions D1-D11 の依存先で、本 provisional ADR では検証不要。

## Alternatives Considered

> **Note**: 詳細な alternative 評価（10 件）は `/autoplan` Phase 1-4 review および `/Users/takeshi/.claude/plans/camera-system-streamed-aho.md` の "PRELIMINARY DRAFT (superseded)" セクションに保管。ここでは本 ADR の thin provisional scope に直接関係する 3 件のみ要約。

### Alternative 1: 完全 ADR scope（51KB プラン全体を本 ADR で lock）

- **Description**: 9 registry entries + 4 forbidden patterns + 完全 ICameraDirector interface + Foundation Singleton + 384×216 Pixel Perfect + Single vcam + 6 anchor shake profiles + Look Ahead 不採用 を本 ADR で確定
- **Pros**: スプリント計画とタイムラインを維持、coverage 71% → 80%+ 一気に引上げ、ADR バッチ処理で効率
- **Cons**: `/autoplan` Phase 1-4 で CEO 6/6 + Eng 5/6 dimensions 全て disagree、両モデル独立 collapse は systemic signal、実装着手で NRE / camera jitter / dead telemetry 1-2 weeks unplanned rework、motor V1 未通過 + 12 Cinemachine 3.x API 未検証で C5 untestable → Status Proposed→Accepted deadlock
- **Rejection Reason**: USER CHALLENGE で Option A 却下、systemic premature lock リスクが high

### Alternative 2: Eng critical 4 件のみ修復して scope 維持（architectural rewrite）

- **Description**: 1-frame sync (Brain.UpdateMethod pin + motor V1 interpolation + 30/60/120Hz×50Hz matrix) / DDOL+scene refs 統一 / Observability API を Brain.OutputCamera 経由 / Lifecycle idempotent 化、の 4 領域を完全 rewrite して 51KB scope を維持
- **Pros**: 51KB 成果 retain、スプリント計画維持
- **Cons**: CEO 8 cross-phase themes（Tier 1 under MVP / craft missing / Foundation Singleton cargo-cult 等）未解決、6 ヶ月後 regret retain、+4-6h rewrite、Pixel Perfect+Cinemachine 3 stutter は Editor spike なしで Damping X=0.2 動作不明
- **Rejection Reason**: B (thin provisional + R1 spike) より Pareto inferior、CEO strategic 8 themes が未解決

### Alternative 3: ADR を完全保留、ADR-0002 V1 + class-switch prototype 後に再起草

- **Description**: 全象 archive、ADR-0002 V1 + ADR-0001 R5 spike 通過後に「実際に camera が何を follow し、どんな Damping が「粘る」体験を生むか」を実証してから ADR-0006 起草
- **Pros**: discovery before policy 完全遵守
- **Cons**: MVP 9 systems #6 ADR 未作成で coverage 71% → 改善せず、Pre-Production gate 通過しにくい、systems-index Camera #7 Not Started のまま、+1-2 weeks スケジューリング
- **Rejection Reason**: thin provisional + R1 spike を spike 並列で進行（B 採用）が Pareto superior、ADR-0006 provisional があれば coverage は partial cover、follow-up ADR で full lock

## Consequences

### Positive

- **Cinemachine 3 採用と motor follow contract が安全に lock**: 2 件は既存 ADR + 既知技術で論理的に堅実、unwind コスト 0
- **MVP 単一 PolygonCollider2D Confiner は Inspector 直書きで完結**: 1 操作で実装可、coverage 計測で TR-camera-001 / 002 partial cover
- **R1 spike 並列実行可**: ADR-0001 R5 + ADR-0002 V1 + R1 Camera spike の 3 spikes 独立、~1 week で 3 つすべて完了見込み
- **systemic premature lock 回避**: `/autoplan` 8 cross-phase themes すべて follow-up ADR の前提作業に組み込み、6 ヶ月後 regret 排除
- **Camera 固有問題の早期発見**: Cinemachine 3.x stutter 既知 issue / DDOL+scene refs 矛盾 / observability API wrong object など 11 deferred 領域を Editor spike + prototype で実証ベースで解決
- **art-bible vs game-concept.md inconsistency 上流解消の trigger**: Pixel Perfect Reference を本 ADR で lock せず deferred にしたことで game-concept.md L313-315 修正 を強制

### Negative

- **MVP playable scene 完全動作には 11 件の follow-up 決定が必要**: thin provisional のみでは Damping 値 / Pixel Perfect 設定 / shake profiles / Look Ahead 等が未確定、follow-up ADR まで Designer / 実装者の判断保留領域大
- **ADR file 量増加**: 本 ADR + follow-up ADR の 2 ファイル運用、registry append-only ルールで follow-up ADR が 9+4 件 append → registry 肥大化
- **scheduling cost +1 week**: R1 spike + ADR-0002 V1 spike + ADR-0001 R5 spike の 3 spikes すべて通過 + follow-up ADR 起草で +1 week、original 51KB plan 採用比
- **Validation Gate C2-C5 が本 ADR では未定義**: stutter / Confiner / Shake / Steam Deck performance の検証は follow-up ADR まで先送り、Tier 2a Demo 直前の検証集中リスク
- **CharacterFollowProxy 未確定で他システム待機**: Audio / Light2D / Trail Renderer 等が motor 追従要件で proxy 待ち、ADR-0002 への TransformReadProxy 提案セッション必要

### Risks

- **R1**: R1 spike で 3 項目以上 unverified → 本 ADR Superseded、Cinemachine 3.x が 4.x に上る前提変動。Mitigation: 12 項目すべて Editor で実機確認、Unity Issue Tracker で 6.0+ open issue 検索ログ保管
- **R2**: ADR-0002 V1 が Physics2D.SyncTransforms() / Cast 検証で fail → motor.Position contract 変更 → C1 検証不能。Mitigation: ADR-0002 V1 spike を最優先で並列実行、本 ADR は ADR-0002 V1 通過まで Provisional のまま
- **R3**: Class-Switch prototype（ADR-0001 R5）で「camera が固い」体感判定 → Look Ahead / Vertical Look 緊急追加で follow-up ADR scope 膨張。Mitigation: Look Ahead 等の Tier 1 expansion 候補を本 ADR D2 / D9 で明示的に defer、prototype で要求が surface したら follow-up ADR scope を expand
- **R4**: registry append-only ルールで follow-up ADR が 9+4 件 append → 1 つのシステムに対する複数 ADR 参照で referenced_by 肥大化。Mitigation: follow-up ADR で本 ADR を Superseded ではなく「provisional → full implementation の relationship」で追記、registry entry の `adr` field を follow-up ADR に update（既存 ADR の `referenced_by` は履歴として保持）
- **R5**: art-bible vs game-concept.md inconsistency が次回 `/architecture-review` で解消されない → Pixel Perfect Reference D7 の defer 先消失、follow-up ADR で arbitrary 解像度 lock リスク。Mitigation: 本 ADR の Open Questions に明示記録、`/architecture-review` 次回実行時に GDD revision flag として最優先処理

## Performance Implications

> **Provisional scope のため performance budget は本 ADR で lock しない（D10 deferred）**。

R1 spike 完了時に measurement protocol を定義（IL2CPP build / target device / worst-case scene / capture length / p95-p99 threshold）、Steam Deck 実機（1280×800 native）+ PC 1080p で実測してから follow-up ADR で `camera_lateupdate_per_frame` / `pixelperfect_rescale_per_frame` budgets を registry に append する。

参考値（hypothesis、実測前）:
- LateUpdate camera total: 0.2-0.4ms / frame（Cinemachine 3 標準構成、Steam Deck 想定）
- PixelPerfect rescale: 0.05-0.1ms / frame（Rendering envelope 1/60）

これらは fan fiction（Codex Eng F9 指摘）であり、実装前 lock しない。

## Migration Plan

### Tier 0 MVP（本 ADR provisional で実現）

1. R1 Camera spike + ADR-0002 V1 spike + ADR-0001 R5 spike を並列実行（~1 week、3 spikes 独立）
2. R1 spike 結果を `production/qa/evidence/r1-camera-cinemachine3-spike-result.md` に書込、12 verification items の OK / Rename / 不在 / 代替案 を記録
3. 3 spikes すべて通過後、follow-up ADR（仮称 ADR-0006a Camera System Implementation）を起草、deferred 11 件を empirical data ベースで lock
4. Tier 0 MVP scene は本 ADR provisional 3 件 lock のみで playable: Cinemachine 3 + 1 CinemachineCamera + Inspector アサインの PolygonCollider2D Confiner + ICharacterMotor.Position bridge（具体実装は follow-up ADR で）

### Tier 1 Vertical Slice

- follow-up ADR で確定された Damping / Look Ahead / Vertical Look / Camera Shake routing / Foundation Singleton 適用方式 / 6 anchor shake profiles を実装
- multi-room 対応 Confiner 切替: `SetConfiner(Collider2D)` + `InvalidateBoundingShapeCache()` を Scene & Addressables Manager 連携で呼出（boss arena は MVP 単一シーン scope だが、Tier 1 で複数 room 対応）

### Tier 2a Steam Next Fest Demo

- Steam Deck native 1280×800 build で C5 実機測定（performance budget validation）
- art-bible vs game-concept.md GDD inconsistency が解消済前提で Pixel Perfect Reference Resolution を follow-up ADR で lock
- Steam Deck Verified 申請: Pixel Perfect Integer Scale + Crop Frame Pillarbox+Letterbox 動作確認

### Tier 2b Early Access / Tier 3 Full Release

- multi-vcam Priority 切替（boss arena / cutscene 専用 vcam）を follow-up ADR で extend
- dynamic OrthographicSize / Smart Damping curve / VFX cue 経由 hybrid shake routing を Tier 1+ で empirical data ベースで extend

## Validation Criteria

### C0: R1 Spike Completion（Accepted 昇格 gate prerequisite）

- 半日 Editor sandbox 実行、`production/qa/evidence/r1-camera-cinemachine3-spike-result.md` 書込
- 12 verification items すべてが OK / Rename / 不在 / 代替案 のいずれかで結論済
- Unity Issue Tracker 検索ログ保管
- Inspector screenshot 3-4 枚（CinemachineCamera / PixelPerfectCamera / CinemachineConfiner2D / 統合 scene）
- **PASS basis**: 9 / 12 以上 OK、3 項目以下 mismatch / 不在 → follow-up ADR で代替案で進行可
- **FAIL basis**: 3 項目以上 unverified（Editor で結論不能、外部 ref 不在）→ 本 ADR Superseded、Cinemachine 3.x → 4.x 移行 or 自作 camera を再評価

### C1: Provisional Follow Basic（ADR-0002 V1 通過後）

- ADR-0002 V1（Physics2D.SyncTransforms() + Cast 検証）通過確認
- PlayMode test: motor.Position drives camera follow within 1 render frame
- Brain.UpdateMethod を architecture で pin（R1 spike #2 結果ベース）
- 30 / 60 / 120Hz render × 50Hz physics matrix で sync 検証
- `Time.timeScale = 0.1` slow-mo + hitstop+dash 組合せで 1-frame divergence ≤ 0.005 unit p99
- **PASS basis**: 全 4 matrix combo で 1-frame sync 達成、`Vector2.Distance(camera.transform.position, motor.Position) ≤ 0.005` over 1000 sample frames
- **FAIL basis**: physics clock vs render clock skew が >0.005 unit → Brain.UpdateMethod 変更 / motor.Position interpolation 追加 / proxy 方式変更 を follow-up ADR で評価

> **C2-C5 は本 ADR では未定義、follow-up ADR で定義**

## GDD Requirements Addressed

| GDD / TR | Requirement | How addressed |
|----------|-------------|---------------|
| TR-camera-001 | Cinemachine 3 + 2D Confiner Extension、CinemachineCamera 新 API | Locked Decision 1（Cinemachine 3 採用）+ Decision 3（CinemachineConfiner2D 採用、Body component と Confiner method 名は R1 spike #4-5 で確定） — **partial cover until R1** |
| TR-camera-002 | CharacterController2D に追従、ICharacterMotor.Position 経由（直接 Transform 参照しない） | Locked Decision 2（ICharacterMotor.Position read-only Vector2 contract、bridge 方式は R1 spike + ADR-0002 V1 後に確定） — **full cover** |
| systems-index #7 Camera System (MVP 単一シーン) | 単一シーンで follow + 境界制約 | Locked 3 件すべてで MVP 単一シーンを unblock — **full cover for provisional scope** |
| game-concept.md line 239 | Cinemachine 3（2D Confiner Extension、CinemachineCamera 新 API） | Decision 1, 3 — **partial cover** |
| game-concept.md line 313-315（128×128 / 96×96 / 64×64） | art-bible.md L656/L1410 (384×216 / 48×48) と inconsistent | **本 ADR で lock せず deferred D7**、game-concept.md revision flag に追加（既存 L236 / L333 リスト + L313-315） |
| art-bible.md L656 | 基準解像度 384×216 px（代替 360×224 px） | **本 ADR で lock せず deferred D7**、game-concept.md L313-315 解消後に follow-up ADR で lock |
| art-bible.md L658 / L1434 | Pixel Perfect Camera で Integer Scale のみ | **本 ADR で具体設定 lock せず deferred D8**、R1 spike + Crop Frame enum 確認後に follow-up ADR で Pillarbox+Letterbox / Stretch Fill OFF 等を lock |
| art-bible.md Section 1 Principle 3 | Color Wash 配置（CinemachineBrain 直下） | Locked Decision 1 で前提継承（Cinemachine 3 採用 + ADR-0003 階層） — **structural fit confirmed** |

## Related

- [ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md) — R5 spike 通過後の class-switch prototype が本 ADR D2 / D9 の Damping / shake profile 値の empirical 確定 trigger
- [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md) — V1 通過が本 ADR C1 検証の前提、TransformReadProxy 提案逆流 (D6) の議論先
- [ADR-0003 VFX System Boundary + IVFXPublisher](adr-0003-vfx-system-boundary.md) — CinemachineBrain 階層前提継承、Camera Shake routing hybrid (D5) の coordinate
- [ADR-0005 Input System Architecture](adr-0005-input-system-architecture.md) — Foundation Singleton stance reference（本 ADR では Camera 適用を deferred D3）
- `docs/architecture/architecture-review-2026-04-28.md` — 本 ADR を Top gap として識別、provisional 反映後 coverage 再計測対象
- `/Users/takeshi/.claude/plans/camera-system-streamed-aho.md` — `/autoplan` Phase 1-4 review 完全記録、PRELIMINARY DRAFT として 51KB 原案保管
- `production/qa/evidence/r1-camera-cinemachine3-spike-result.md`（**書込予定** — R1 spike 完了時に作成）
- `docs/engine-reference/unity/deprecated-apis.md`（CinemachineVirtualCamera Cinemachine 2.x legacy 追記予定）

## Open Questions

1. **R1 spike 結果次第**: Cinemachine 3.x で `CinemachinePixelPerfect` extension が提供されない場合、PositionComposer Damping + Pixel Perfect snap stutter は別アプローチで解決必要（Unity Issue Tracker 既知 issue）
2. **Brain.UpdateMethod 既定値**: LateUpdate / SmartUpdate のどちらが Cinemachine 3.x default で、physics clock vs render clock skew にどう影響するか（C1 検証で実測）
3. **ICharacterMotor.TransformReadProxy 提案の ADR-0002 受入可否**: motor 自身が proxy GameObject を owned で持つ案、ADR-0002 author session で議論
4. **art-bible L656 vs game-concept.md L313-315 inconsistency**: art-bible 384×216 + char 48×48 を権威ソースとするか、game-concept.md 128×128 を art-bible に反映するか — **次回 `/architecture-review` で GDD revision flag として最優先処理**
5. **boss arena の MVP scope**: systems-index.md は MVP scope に「敵 2-3 種、Tier 0 で 1 ダミー」を含むが boss は VS 以降。Tier 0 で boss 不在なら multi-vcam Priority 切替も VS で OK だが、systems-index.md MVP 9 systems の line 256 で「Boss 1 体」を VS 含むため Tier 1 entry 時点で multi-vcam 必要 — follow-up ADR で確定
