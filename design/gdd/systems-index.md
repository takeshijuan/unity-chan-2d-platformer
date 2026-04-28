# Systems Index: 職業オーブのレガシー / *Unity-chan and the Class Orbs*

> **Status**: Draft
> **Created**: 2026-04-27
> **Last Updated**: 2026-04-27
> **Source Concept**: [design/gdd/game-concept.md](game-concept.md)
> **Review Mode**: Full
> **Gates Passed**:
> - TD-SYSTEM-BOUNDARY: CONCERNS（5 項目、3 件は本文書「Architecture Notes」へ申し送り）
> - PR-SCOPE: UNREALISTIC → Option A（Producer 推奨を全面受け入れ）で解消、本文書「Production Constraints」へ反映
> - CD-SYSTEMS: CONCERNS（3 件、本文書「Creative Director Notes」へ申し送り）2026-04-27

---

## Overview

本作は 2D 横スクロール・メトロイドヴァニア。職業オーブを集めて剣士・弓士・魔法使いを瞬時に切替えるコンボアクションが核。Pillar 1「切替が、花になる」が本作のメカニカル中心。

メカニカル分解の結果、29 システムを 5 層（Foundation / Core / Feature / Presentation / Polish）に整理した。中核は **Class Switch System**（Pillar 1）と **CharacterController2D**（Pillar 3「歯ごたえ」）。**Save Data System** は 7 依存の最大 bottleneck。

PR-SCOPE レビューでオリジナル MVP（15 システム / 3-4 週間）が UNREALISTIC と判定され、Tier 0 を 9 システム / 6 週間に縮小、Save Data を MVP に前倒し、Localization 規律を MVP day 1 から強制する Option A を採用した。後続 Tier も +30-40% 後ろ倒しに調整した。

---

## Production Constraints

### Tier タイムライン（PR-SCOPE 修正後）

| Tier | システム数（累積） | タイムライン（累積） | 検証目的 |
|---|---|---|---|
| Tier 0 MVP | 9 | **6 週間** | 「切替コンボが面白いか」自己検証（Go/Pivot/Stop ゲート） |
| Tier 1 Vertical Slice | 21 | **5-6 ヶ月** | 外部テスター 5 名で「もう少し遊びたい」検証 |
| Tier 2a Steam Next Fest Demo | 29 | **8-10 ヶ月** | Steam Next Fest wishlist 獲得数で EA 発売判断 |
| Tier 2b Early Access 発売 | 29 | **12-15 ヶ月** | コミュニティフィードバックで Tier 3 方向決定 |
| Tier 3 Full Release | 29 (+ コンテンツ拡張) | **24-30 ヶ月** | エンディング・隠しエリア・真エンディング |

> **Note**: concept.md の元タイムライン（3-4 週 / 3-4 ヶ月 / 6-8 ヶ月 / 9-12 ヶ月 / 18-24 ヶ月）は PR-SCOPE で「ソロ MV 市場ベンチ比 2-3 倍速」と判定。本表が更新済みの公式タイムライン。

### MVP の重要制約（Option A 採用条件）

1. **MVP は 9 システムに限定**（VFX/Audio/Tilemap/Scene&Addressables/HUD/GameState/Health&Damage は VS へ送る）
2. **Save Data System は MVP に前倒し**（最小スタブ：`schemaVersion=1` + JSON I/O のみ。実体データは VS で拡張）
3. **Localization 規律は MVP day 1 から強制**（コードに生文字列禁止、`Strings.Combat.HitConfirm` 形式のキー参照のみ。実体テーブルは Alpha）
4. **Class Abilities の balance work は Tier 1 で自動化**（バランス検証スクリプト化必須、4 職拡張時に balance combinatorics が爆発するため）

---

## Creative Director Notes（CD-SYSTEMS 申し送り — 2026-04-27）

GDD authoring 時に必ず以下のピラー整合性確保を反映すること。CD レビューで「Tier 0 hypothesis spike の信頼性が、Pillar 1 検証手段が SpriteRenderer 単独に縮退すると棄損される」と指摘された。VFX/Audio System を前倒しせず、**該当ピラーシステムの GDD 内に self-contained な最低限フィードバックを埋め込む**方針で解消する。

### CD1. Class Switch System GDD：「Tier 0 スパイク用ミニマル feedback」セクション必須

Class Switch System の GDD には Pillar 1「切替が、花になる」を Tier 0 単独で検証可能にする minimal-feedback サブセクションを置くこと。VFX System / Audio System 不在でも切替体験の satisfaction が成立する設計責任は Class Switch 自身が持つ：

- **Color wash**: Camera の `OnRenderImage` または直接 SpriteRenderer の color tint で 0.1-0.2s 放射状カラーオーバーレイ（Visual Identity Anchor Principle 3 を簡略実装）
- **SE**: Unity 内蔵の `AudioSource.PlayOneShot()` で切替 SE 1 種類のみ（Audio System の pool 化前段階）
- **Hitstop**: 30-50ms を CharacterController2D 側に持たせる（Combat System に依存させない）

これらは Tier 1 で VFX System / Audio System が完成したら refactor 移管するが、Tier 0 の hypothesis 検証期は Class Switch GDD 内 self-contained とする。

### CD2. Combat System GDD：「Tier 0 inline hit feedback の定義」セクション必須

Combat System の MVP scope を曖昧にしない：

- **Tier 0 では HP=1 の dummy enemy + hitstop + knockback impulse のみ**（HP/ダメージ計算なし、敵は 1 ヒットで死亡）
- **Pillar 3「歯ごたえ」の触覚は damage number ではなく hitstop + knockback + impact frame**。Tier 0 はこの 3 点に集中
- HP / Damage Calculation / 状態異常等は Tier 1（Health & Damage System 完成後）で追加

### CD3. Pillar 2 / Pillar 4 の検証時期を明示

- **Pillar 2「次の鍵が見える」は Tier 0 では検証不可**（Orb Acquisition / Gate & Lock / Map Tracking すべて VS）。**Tier 1 Go/Pivot/Stop ゲートに「Pillar 2 design test = 1 時間プレイで次の目標が常に頭にあるか」を追加**する
- **Pillar 4「全部でもっと深く」は Tier 0 では検証不可**（2 職のみ）。Tier 1 で 3 職実装まで判断保留

### CD4. Anti-Pillar 監視（Class Abilities GDD 着手時）

`systems-designer` が Class Abilities GDD で「アビリティアンロックツリー」「アビリティ強化スロット」を提案してきたら **Creative Director として REJECT**。Anti-Pillar 2「複雑なビルド/装備最適化」への drift 経路。

---

## Architecture Notes（TD-SYSTEM-BOUNDARY 申し送り）

GDD authoring 時に必ず以下のアーキテクチャ判断を反映すること。Class Abilities GDD と CharacterController2D GDD には特に重要。

### A1. Class Abilities System の God Object 化防止 → ADR-0001（枠組み確立済み）+ 将来の ADR-0004（Class Abilities System 詳細）

`ClassAbility` が ability データ + combat トリガー + movement 変更 + VFX cue + audio cue を全て握ると ScriptableObject が肥大化する。以下の 3 構造に分割せよ：

- **`ClassAbilityData`**（ScriptableObject）— 純データのみ：damage curve / hitbox shape / frame data / animation key / vfx key / sfx key
- **`IAbilityExecutor`**（interface）— 実行ロジック。MonoBehaviour に実装
- **`AbilityContext`**（注入オブジェクト）— `ICharacterMotor` / `IComboBuffer` / `IVFXPublisher` / `IAudioPublisher` を含む

### A2. CharacterController2D ↔ Class Abilities の双方向結合防止 → ADR-0002 で確定（Proposed, Validation Gate V1-V5）

`archer dash` や `mage hover` のような ability が CC2D の内部状態（velocity, gravity scale, grounded flag）を直接書き換える設計を禁止。**Solver の権威性は CC2D 側に残す**（1 フレーム 1 solve、Box2D v3 SyncTransforms タイミング保護、`Physics2D.SyncTransforms()` 明示呼出）。

`ICharacterMotor` interface のみを expose し、ability は意図ベース API のみ叩く（**ADR-0002 確定 spec**）：
- `RequestImpulse(Vector2 impulse)` — 1 フレーム加算
- `OverrideGravity(float multiplier, float durationSec)` — multiplier 後勝ち、durationSec は max 合成
- `LockHorizontalControl(float durationSec)` — 長い方優先で合成
- `ApplyHitstop(float durationSec)` — CD1 Tier 0 内蔵（30-50ms、`MotorTuning.HitstopDefaultSec = 0.04s`）
- `SetFacing(Facing direction)` — ability 由来の向き強制

詳細・合成規則・event 通知（`Landed` / `JumpStarted` / `WallTouched` / `HitstopApplied` / `StateChanged`）・performance budget（0.5 ms / FixedUpdate）は ADR-0002 を参照すること。

### A3. Game State Machine と Scene & Addressables Manager の責務直交化

- **Game State Machine** = プレイヤー視点のモード（Title / Playing / Paused / GameOver）
- **Scene & Addressables Manager** = シーン/アセットの物理状態（loading / loaded / unloading）

「Loading」状態を両者が二重に表現するリスクを避ける。GDD では「Loading 状態は Game State 側に持たせ、Scene Manager は async progress を publish するのみ」と明記。両者を一つに merge してはいけない（pause 中もシーン非ロード状態は維持される等、直交する状況がある）。

### A4. VFX System の層配置（Core 層）→ ADR-0003（VFX System Boundary + IVFXPublisher、Proposed / Validation Gate G1-G5 待ち）

VFX は Class Switch / Combat / Orb Acquisition / Boss / UI Notification すべてから publish される pub/sub サービス。**ゲームプレイの依存先ではなく依存元**として Core 層に配置済み。`IVFXPublisher.PlayCue(VfxCueDefinition, in VfxCueArgs)` を `Game.Core.asmdef` に置き、内部実装（`Game.VFX.asmdef` の `VFXPublisherService`）は Addressables から VFX prefab を per-cue object pool で引く。Particle backbone は Legacy ParticleSystem（VFX Graph は IVFXPublisher 抽象越しの将来オプション）、Color Wash は CinemachineBrain 直下の Animated Quad + `Sprite_ColorWash.shadergraph` を Sorting Layer `VFX_Overlay` で実装。

詳細・5 anchor cue（class_switch_orb_burst / hit_spark / dust_landed / hitstop_freeze_frame / orb_acquisition）・Tier 0 → Tier 1 移行 PR 分割・Validation Gate G1-G5 は ADR-0003 を参照すること。

### A5. Save Data System の論理分割（系統内）→ ADR-0004（VFX System Boundary + IVFXPublisher、Proposed / Validation Gate S1-S6 待ち）

7 依存の bottleneck だが System を split しない（schemaVersion migration 整合のため）。代わりに：
- `ISaveable` interface（library-agnostic POCO contract）+ section-based JSON（player / world / settings / meta 4 section）
- 論理分割は実装内部で達成
- Atomic write: `.tmp → fsync → File.Replace + .bak 1 世代`
- `ICloudSync` 抽象（Tier 0 `NullCloudSync` / Tier 2a `SteamCloudSync` 別 asmdef）
- `[RegisterSaveable]` attribute + reflection scan で auto-registration（cyclic asmdef 防止）

詳細・ISaveable / SaveSection / SaveDocument / ICloudSync 完全仕様・5 anchor save scenarios・S1-S6 Validation Gate・Tier 0 PR1-3 配備計画・ADR-0001 R5 / ADR-0002 V1 への revise 要請（`SwitchContext` / `Teleport`）は ADR-0004 を参照すること。

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|---|---|---|---|---|---|
| 1 | Input System | Core | MVP | Not Started | — | — |
| 2 | Game State Machine | Core | Vertical Slice | Not Started | — | — |
| 3 | Save Data System | Persistence | MVP（最小スタブ） / VS（full） | Not Started | — | — |
| 4 | Scene & Addressables Manager | Core | Vertical Slice | Not Started | — | Game State Machine |
| 5 | Audio System | Audio | Vertical Slice | Not Started | — | Game State Machine |
| 6 | CharacterController2D | Core | MVP | Not Started | — | Input System |
| 7 | Camera System | Core | MVP（単一シーン） / VS（multi-room） | Provisional (ADR-0006, C0-C1 + R1 spike pending) | — | CharacterController2D (ADR-0002), Scene & Addressables (VS) |
| 8 | Tilemap & World Geometry | Gameplay | Vertical Slice | Not Started | — | Scene & Addressables Manager |
| 9 | Combo Input Buffer | Gameplay | MVP | Not Started | — | Input System |
| 10 | Class Switch System | Gameplay | MVP | Not Started | — | Input System, Game State Machine |
| 11 | Class Abilities System | Gameplay | MVP（2 職） / VS（3 職） / Tier 3（4 職） | Not Started | — | Class Switch, Combo Input Buffer, CharacterController2D |
| 12 | Combat System | Gameplay | MVP（最小） / VS（full） | Not Started | — | Class Abilities, CharacterController2D |
| 13 | Health & Damage System | Gameplay | Vertical Slice | Not Started | — | Combat System |
| 14 | Enemy AI System | Gameplay | MVP（1 ダミー） / VS（3-4 archetype） | Not Started | — | CharacterController2D, Combat / H&D, Tilemap (VS) |
| 15 | Boss AI System | Gameplay | Vertical Slice | Not Started | — | Enemy AI, Health & Damage |
| 16 | VFX System | Tech Art | Vertical Slice | Not Started | — | （依存ゼロ — `IVFXPublisher` サービスを提供） |
| 17 | Orb Acquisition & Class Unlock | Progression | Vertical Slice | Not Started | — | Save Data, Class Switch, IVFXPublisher |
| 18 | Gate & Lock System | World | Vertical Slice | Not Started | — | Save Data, Class Abilities |
| 19 | Shortcut System | World | Alpha | Not Started | — | Save Data, Tilemap |
| 20 | Save Point / Bench System | World | Vertical Slice | Not Started | — | Save Data, Health & Damage |
| 21 | Map Tracking System | Progression | Alpha | Not Started | — | Save Data, Scene & Addressables |
| 22 | HUD | UI | Vertical Slice | Not Started | — | Health & Damage, Class Switch |
| 23 | Map & Inventory Screen | UI | Alpha | Not Started | — | Map Tracking, Orb Acquisition |
| 24 | Title / Menu / Settings UI | UI | Alpha | Not Started | — | Save Data, Input, Game State |
| 25 | Bench Interaction UI | UI | Alpha | Not Started | — | Save Point / Bench |
| 26 | Notification & Tutorial UI | UI | Vertical Slice | Not Started | — | Orb Acquisition |
| 27 | Localization System | Meta | Alpha（実装） / **MVP（規律）** | Not Started | — | Save Data, all UI |
| 28 | Steam Integration | Meta | Alpha | Not Started | — | Save Data |
| 29 | Accessibility Options | Meta | Alpha | Not Started | — | Settings UI, Localization, Input |

---

## Categories

| Category | Description | Systems |
|---|---|---|
| **Core** | Foundation infrastructure everything depends on | Input / Game State / Scene & Addressables / Audio / CC2D / Camera |
| **Gameplay** | Mechanics that make the game fun | Tilemap / Combo Input / Class Switch / Class Abilities / Combat / Health & Damage / Enemy AI / Boss AI |
| **Progression** | How the player grows over time | Orb Acquisition & Class Unlock / Map Tracking |
| **Persistence** | Save state and continuity | Save Data System |
| **World** | Spatial / structural systems | Gate & Lock / Shortcut / Save Point / Bench |
| **UI** | Player-facing displays | HUD / Map & Inventory / Title-Menu-Settings / Bench Interaction / Notification & Tutorial |
| **Audio** | Sound and music | Audio System |
| **Tech Art** | Visual effects and rendering customization | VFX System |
| **Meta** | Outside the core game loop | Localization / Steam Integration / Accessibility Options |

> 本作で不採用カテゴリ：**Narrative**（拠点 NPC 対話 / Dialogue System）、**Economy**（crafting / shops）。Anti-Pillars に従い、フレーバーテキストは Orb Acquisition + Notification UI で吸収する。

---

## Priority Tiers

| Tier | 定義 | Target Milestone | Design Urgency |
|---|---|---|---|
| **MVP** | Tier 0 で「切替コンボが面白いか」を 6 週間で自己検証する spike プロトタイプ | First Playable Prototype | Design FIRST |
| **Vertical Slice** | Tier 1 で外部テスター 5 名向け、コアループ完成＋1 ボス＋3 職 | 5-6 ヶ月 累積 | Design SECOND |
| **Alpha** | Tier 2a Demo + Tier 2b Early Access、Steam リリース対応 | 8-10 → 12-15 ヶ月 累積 | Design THIRD |
| **Full Vision** | Tier 3 Full Release、4 職目・4 ゾーン目・隠しエリア・真エンディング（コンテンツ拡張のみ、新システムなし） | 24-30 ヶ月 累積 | Design as needed |

---

## Dependency Map

### Foundation Layer（依存ゼロ）

1. **Input System** → ADR-0005（Proposed / Validation Gate I0-I5 待ち）— Unity Input System 1.8+ の `IInputService` / `IInputEventStream` 抽象、4 Action Maps（Gameplay / UI / Pause / Dialogue）、Action Rebinding via ADR-0004 SettingsInputSaveable、Steam Input Tier 0 default detection / Tier 2a 専用 API、Combo Input Buffer (#9) 向け `(float)ctx.time` timestamp 配信、d-pad/stick 全 UI navigation Roslyn analyzer 拘束。詳細・I0/I1 spike・実装 PR 計画・Foundation Singleton stance は ADR-0005 を参照
2. **Game State Machine** — プレイヤー視点のモード FSM。Scene Manager と直交（A3 参照）
3. **Save Data System** — `schemaVersion` + JSON I/O + `.bak` + Steam Cloud。`ISaveable` interface で論理分割（A5 参照）

### Core Layer（Foundation のみに依存）

4. **Scene & Addressables Manager** ← Game State Machine
5. **Audio System** ← Game State Machine（AudioMixer Snapshot を State 連動）
6. **CharacterController2D** ← Input System（自作 Kinematic、`ICharacterMotor` interface 提供 — A2 参照）
7. **Camera System** ← CharacterController2D（MVP 時）/ + Scene & Addressables（VS 以降）
9. **Combo Input Buffer** ← Input System
10. **Class Switch System** ← Input System、Game State Machine（SpriteLibrary 即時切替、ClassStateMachine）
16. **VFX System** ← なし（`IVFXPublisher` サービスを提供 — A4 参照）

### Feature Layer（Core 上に乗るゲームメカニクス）

8. **Tilemap & World Geometry** ← Scene & Addressables Manager
11. **Class Abilities System** ← Class Switch、Combo Input、CharacterController2D（Data + Executor + Context — A1 参照）
12. **Combat System** ← Class Abilities、CharacterController2D
13. **Health & Damage System** ← Combat
14. **Enemy AI System** ← CharacterController2D、Health & Damage、Tilemap
15. **Boss AI System** ← Enemy AI、Health & Damage
17. **Orb Acquisition & Class Unlock** ← Save Data、Class Switch、`IVFXPublisher`
18. **Gate & Lock System** ← Save Data、Class Abilities
19. **Shortcut System** ← Save Data、Tilemap
20. **Save Point / Bench System** ← Save Data、Health & Damage
21. **Map Tracking System** ← Save Data、Scene & Addressables

### Presentation Layer（UI）

22. **HUD** ← Health & Damage、Class Switch
23. **Map & Inventory Screen** ← Map Tracking、Orb Acquisition
24. **Title / Menu / Settings UI** ← Save Data、Input、Game State
25. **Bench Interaction UI** ← Save Point / Bench
26. **Notification & Tutorial UI** ← Orb Acquisition

### Polish Layer（メタ・後乗せ）

27. **Localization System** ← Save Data、全 UI コンシューマ ※規律は MVP day 1 から強制
28. **Steam Integration** ← Save Data
29. **Accessibility Options** ← Title/Menu/Settings UI、Localization、Input

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent | Effort |
|---|---|---|---|---|---|
| **MVP（9 システム）** | | | | | |
| 1 | Input System | MVP | Foundation | game-designer + unity-specialist | M |
| 2 | Save Data System | MVP（最小スタブ） | Foundation | game-designer + unity-specialist | L |
| 3 | CharacterController2D | MVP | Core | game-designer + unity-specialist | L |
| 4 | Combo Input Buffer | MVP | Core | game-designer | M |
| 5 | Class Switch System | MVP | Core | game-designer + unity-specialist | L |
| 6 | Camera System | MVP（単一シーン） | Core | game-designer + unity-specialist | M |
| 7 | Class Abilities System | MVP（2 職） | Feature | game-designer + systems-designer | L |
| 8 | Combat System | MVP（最小） | Feature | game-designer + systems-designer | M |
| 9 | Enemy AI System | MVP（1 ダミー） | Feature | game-designer + ai-programmer | M |
| **VS（+12 システム）** | | | | | |
| 10 | Game State Machine | VS | Foundation | game-designer | S |
| 11 | Scene & Addressables Manager | VS | Core | unity-addressables-specialist | M |
| 12 | Audio System | VS | Core | audio-director + sound-designer | M |
| 13 | VFX System | VS | Core | technical-artist + unity-shader-specialist | M |
| 14 | Tilemap & World Geometry | VS | Feature | level-designer + unity-specialist | M |
| 15 | Health & Damage System | VS | Feature | systems-designer | S |
| 16 | Boss AI System | VS | Feature | game-designer + ai-programmer | L |
| 17 | Orb Acquisition & Class Unlock | VS | Feature | game-designer + narrative-director | M |
| 18 | Gate & Lock System | VS | Feature | game-designer + level-designer | M |
| 19 | Save Point / Bench System | VS | Feature | game-designer | M |
| 20 | HUD | VS | Presentation | ux-designer + unity-ui-specialist | M |
| 21 | Notification & Tutorial UI | VS | Presentation | ux-designer + unity-ui-specialist | M |
| **Alpha（+8 システム）** | | | | | |
| 22 | Localization System | Alpha（実装） | Polish | localization-lead | M |
| 23 | Map Tracking System | Alpha | Feature | game-designer | M |
| 24 | Shortcut System | Alpha | Feature | level-designer | S |
| 25 | Title / Menu / Settings UI | Alpha | Presentation | ux-designer + unity-ui-specialist | L |
| 26 | Bench Interaction UI | Alpha | Presentation | ux-designer + unity-ui-specialist | S |
| 27 | Map & Inventory Screen | Alpha | Presentation | ux-designer + unity-ui-specialist | M |
| 28 | Steam Integration | Alpha | Polish | unity-specialist + release-manager | M |
| 29 | Accessibility Options | Alpha | Polish | accessibility-specialist + ux-designer | M |

> Effort: **S** = 1 セッション / **M** = 2-3 セッション / **L** = 4+ セッション

---

## Circular Dependencies

**循環は検出されませんでした。** 以下のペアはイベント駆動で一方向化：

- **Class Switch → VFX System**：Class Switch が `OnSwitched(orbId)` を publish、VFX が subscribe して花演出再生。Class Switch は VFX の存在を知らない（VFX 不在でもゲームロジックは成立）
- **Orb Acquisition → Notification UI**：Orb 取得時に `OnOrbAcquired(orbData)` を publish、Notification UI が subscribe。一方向

---

## High-Risk Systems

| System | Risk Type | Description | Mitigation |
|---|---|---|---|
| **Save Data System** | Architectural | 7 依存の最大 bottleneck。schema 互換性事故は致命的（セーブ全消し） | 最小スタブを MVP に前倒し / `ISaveable` interface + section-based JSON / `.bak` 1 世代 / migration chain で対応 |
| **Class Switch System** | Design | Pillar 1 中核、1f 同期失敗は本作の体験を破壊 | concept R-T3 で「ScriptableObject + SpriteLibrary + VFX プール化で ~0.4ms」と検証済。MVP プロトタイプで再確認 |
| **CharacterController2D** | Technical | 完全自作 Kinematic、Box2D v3 マルチスレッド化との整合 | concept R-T1 / R-T4 で対応方針あり。MVP で挙動精度を最優先で詰める |
| **Class Abilities System** | Design | 4 職拡張時に balance combinatorics が爆発（2→6, 3→12, 4→24 ペアワイズ） | Tier 1 で**バランス検証スクリプト化（自動）必須**。手動チェックを許容しない |
| **Boss AI System** | Scope | 1 体 = L-effort 単独で 4-6 週間級。Tier 3 まで × 4 体 = 16-24 週間相当 | 各 Tier で「ボス vs 中ボス（精鋭エリート）」の選択肢を持つ。優雅な scope 削減のため |
| **Tilemap / World 制作** | Hidden Cost | 30 部屋 × 平均 2-3 時間/部屋 = 60-90 時間（システム外） | Tilemap GDD に「コンテンツ予算ライン」を別途記載。Tier 移行時のレビューで進捗管理 |
| **Pillar 2 / Pillar 4 検証 timing** | Validation | Pillar 2「次の鍵が見える」と Pillar 4「全部でもっと深く」は Tier 0 では構造的に検証不可（前者は探索系システム不在、後者は 2 職のみ） | Tier 1 Go/Pivot/Stop ゲートに **Pillar 2 design test（1 時間プレイで次の目標が常に頭にあるか）** と **Pillar 4 design test（3 職目の追加で組合せの遊びが爆発するか）** を必須項目として追加。Tier 0 完了時に「これらが成立しない」と誤判定しない |

---

## Localization Discipline（MVP day 1 から強制）

Localization System の実装本体は Alpha だが、以下の規律は **MVP day 1 から守る**こと。後付け改修コストが高いため：

- **コードに生文字列を書かない** — 全プレイヤー向け文字列は `Strings.[Category].[Key]` 形式のキー参照のみ
- **キー命名規則** — `Strings.Combat.HitConfirm` / `Strings.UI.Title` / `Strings.Tutorial.FirstMove` 等
- **暫定実装** — MVP/VS 中は静的辞書（C# `Dictionary<string, string>`）でキー → JP 文字列。Alpha で String Table へ移行
- **デバッグログは規律外** — `Debug.Log()` は英語生文字列 OK
- **ScriptableObject の content フィールドも対象** — フレーバーテキスト等は Localization キーで保持し、Localization System が解決

---

## Production Risks Forwarded from PR-SCOPE

PR-SCOPE が指摘した hidden cost を以下に明記。各 Tier の進捗レビュー時に確認すること：

1. **マップ制作工数**：30 部屋 × 平均 2-3 時間/部屋 ≈ 60-90 時間（システム外）
2. **VFX 花演出アセット**：職業切替 × 3 職 × 2-3 バリエーション ≈ 9 アセット最低、各 1-2 時間
3. **Boss AI ×4 の Tier 3 累計**：単純加算で 16-24 週間相当

---

## Progress Tracker

| Metric | Count |
|---|---|
| Total systems identified | 29 |
| Design docs started | 0 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 0 / 9 |
| Vertical Slice systems designed | 0 / 21 |
| Alpha systems designed | 0 / 29 |

---

## Next Steps

- [ ] Spawn `creative-director` via Task using gate **CD-SYSTEMS** to validate this systems set against the game pillars before GDD authoring begins
- [ ] Run `/design-system input-system` (or `/map-systems next`) to start authoring the first GDD
- [ ] Update `production/session-state/active.md` with the index creation milestone
- [x] ADR-0001 Class Switch Architecture written (2026-04-27, Status: Proposed (Validation Gate: R5))
- [ ] Consider creating **ADR-0002 CharacterController2D + ICharacterMotor** at `/architecture-decision` time（Architecture Note A2 参照）
- [ ] Consider creating **ADR-0003 VFX System Boundary + IVFXPublisher** at `/architecture-decision` time（Architecture Note A4 参照）
- [ ] Prototype the highest-risk system early — `/prototype class-switch` is the canonical Tier 0 spike
- [ ] After 5-6 MVP GDDs are authored, run `/review-all-gdds` for cross-document consistency
- [ ] Run `/gate-check pre-production` when all 9 MVP GDDs are designed and reviewed
