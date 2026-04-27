# Architecture Review Report

**Date**: 2026-04-27
**Engine**: Unity 6.3 LTS (6000.3.x)
**GDDs Reviewed**: 2 (`design/gdd/game-concept.md`, `design/gdd/systems-index.md`)
**ADRs Reviewed**: 2 (ADR-0001 Class Switch, ADR-0002 CharacterController2D + ICharacterMotor)
**Mode**: full
**Phase Skips**: Phase 3b RTM（`production/epics/`、`tests/` 未存在）、Phase 6 Architecture Document Coverage（`docs/architecture/architecture.md` 未存在）

---

## Executive Summary

両 ADR（ADR-0001 Class Switch、ADR-0002 CharacterController2D + ICharacterMotor）は方法論的に健全で、Knowledge Risk HIGH を明示し Validation Gate (R5 / V1-V5) で post-cutoff 不確実性を管理している。Box2D v3 マルチスレッド対応・`Physics2D.SyncTransforms()` 明示呼出・`Animator.Play()` 拒否 → SpriteLibrary 直 resolve など、Unity 6.3 LTS 固有の破壊的変更にも正しく追従。

ただし MVP 9 システムのうち ADR が存在するのは 2 システムのみで、残 7 システム（Input / Save Data / Combo Input Buffer / Camera / Class Abilities / Combat / Enemy AI）は ADR 未起草。これは事前生産フェーズとして想定範囲だが、Tier 0 prototype 着手前に Save Data / Class Abilities / VFX System の ADR 起草が必要。

加えて、ADR-0001 Summary に Hitstop ownership に関する記述不整合（State Ownership ambiguity）があり、unity-specialist consultation で 5 件の追加 anti-pattern が検出された。これらは Validation Gate 通過前の軽量修正で対処可能。

---

## Phase 2-3: Traceability Matrix

> 29 システムのうち MVP 9 システム + 主要 VS システムの「architectural-decision-implying」要件のみ抽出（39 件）。実装詳細・コンテンツ要件は除外。MVP 9 システム該当 25 件は `tr-registry.yaml` に登録。

### Foundation Layer（MVP）

| TR-ID | Source GDD | System | Requirement | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-input-001 | game-concept.md, systems-index.md | Input System | Unity Input System 1.8+（Action Rebinding UI / Steam Input） | — | ❌ GAP |
| TR-input-002 | game-concept.md | Input System | Gamepad full support / d-pad/スティック必須 | — | ❌ GAP |
| TR-input-003 | systems-index.md | Input System | コンボバッファ向け timestamp 配信 | — | ❌ GAP |
| TR-save-001 | game-concept.md R-T5 | Save Data | schemaVersion + マイグレーションチェーン | — | ❌ GAP |
| TR-save-002 | game-concept.md R-T5 | Save Data | `.bak` 1 世代バックアップ | — | ❌ GAP |
| TR-save-003 | systems-index.md A5 | Save Data | `ISaveable` interface + section-based JSON 論理分割 | — | ❌ GAP |
| TR-save-004 | game-concept.md | Save Data | Newtonsoft.Json + Steam Cloud (`ISteamRemoteStorage`) | — | ❌ GAP |
| TR-save-005 | systems-index.md PR-SCOPE Option A | Save Data | MVP 最小スタブ前倒し（schemaVersion=1 + JSON I/O） | — | ❌ GAP |
| TR-gamestate-001 | systems-index.md A3 | Game State Machine | Title / Playing / Paused / GameOver FSM（VS） | — | ❌ GAP |
| TR-gamestate-002 | systems-index.md A3 | Game State Machine | Scene Manager と直交（Loading 状態の二重表現禁止） | — | ❌ GAP |

### Core Layer（MVP + VS）

| TR-ID | Source GDD | System | Requirement | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-cc2d-001 | game-concept.md, systems-index.md A2 | CharacterController2D | Kinematic Rigidbody2D + 自作 Cast Solver、ability に内部状態 write 不可 | ADR-0002 | ✅ |
| TR-cc2d-002 | systems-index.md A2 | CharacterController2D | `ICharacterMotor` interface のみ expose、意図ベース API のみ駆動 | ADR-0002 | ✅ |
| TR-cc2d-003 | game-concept.md Pillar 3 | CharacterController2D | コヨーテタイム / Jump Buffer / Wall Slide メトロイドヴァニア定番 | ADR-0002 (MotorTuning) | ✅ |
| TR-cc2d-004 | systems-index.md CD1 | CharacterController2D | Tier 0 Hitstop 30-50ms 内蔵 | ADR-0002 (`ApplyHitstop`) | ✅ |
| TR-cc2d-005 | game-concept.md R-T1, R-T4 | CharacterController2D | Box2D v3 マルチスレッド整合（明示 `Physics2D.SyncTransforms()`） | ADR-0002 | ✅ |
| TR-cc2d-006 | game-concept.md Retention | CharacterController2D | Determinism（リプレイ・スピランの土台） | ADR-0002 (fixedDeltaTime) | ✅ |
| TR-classswitch-001 | game-concept.md Pillar 1 | Class Switch | R1/L1 1 ボタン即時切替（クールダウン無し、戦闘中・空中 OK） | ADR-0001 | ✅ |
| TR-classswitch-002 | game-concept.md R-T3 | Class Switch | 入力受信から視覚反映が同フレーム内 | ADR-0001 (SpriteLibrary 直接 resolve) | ✅ |
| TR-classswitch-003 | game-concept.md Visual Identity P3 | Class Switch | Color Wash 0.1-0.2s 放射状フェード | ADR-0001 (`SpriteRenderer.color` + Coroutine) | ⚠️ Partial (AP-1 多重起動ガード未明示) |
| TR-classswitch-004 | game-concept.md Core Mech 1 | Class Switch | 切替自体が視覚/聴覚の報酬（SE） | ADR-0001 (`AudioSource.PlayOneShot`, Tier 0) | ✅ |
| TR-classswitch-005 | game-concept.md Pillar 4 | Class Switch | 4 職目追加がコード変更ゼロ（Tier 3） | ADR-0001 (SO + Inspector 配列) | ✅ |
| TR-classswitch-006 | systems-index.md A1 | Class Switch | ClassAbilityData (SO) + IAbilityExecutor + AbilityContext 三分割の枠組み | ADR-0001 (枠組み) + ADR-0004 (詳細未) | ⚠️ Partial |
| TR-camera-001 | game-concept.md | Camera | Cinemachine 3 + 2D Confiner Extension | — | ❌ GAP |
| TR-camera-002 | game-concept.md, systems-index.md | Camera | CharacterController2D follow（`ICharacterMotor.Position` 経由） | — | ❌ GAP |
| TR-combo-001 | game-concept.md Core Mech 4 | Combo Input Buffer | 先行入力バッファ 4-6 フレーム | — | ❌ GAP |
| TR-combo-002 | game-concept.md Core Mech 4 | Combo Input Buffer | 空中キャンセル / 職業間コンボ受け渡し | — | ❌ GAP |
| TR-scene-001 | game-concept.md | Scene & Addressables | Scene 単位の動的ロード（Addressables 2.0） | — | ❌ GAP |
| TR-scene-002 | game-concept.md | Scene & Addressables | AI 生成アセットのバリアント管理 | — | ❌ GAP |
| TR-scene-003 | systems-index.md A3 | Scene & Addressables | async progress publish のみ（Game State と直交） | — | ❌ GAP |
| TR-audio-001 | game-concept.md | Audio System | AudioSource + AudioMixer Snapshot 切替 | — | ❌ GAP |
| TR-audio-002 | game-concept.md | Audio System | SE プール化 / BGM ゾーン単位 | — | ❌ GAP |
| TR-audio-003 | systems-index.md (implicit) | Audio System | `IAudioPublisher` サービス（Tier 1 で ADR-0001 と統合） | — | ❌ GAP |
| TR-vfx-001 | systems-index.md A4 | VFX System | `IVFXPublisher` pub/sub サービス、Core 層、依存ゼロ | — | ❌ GAP |
| TR-vfx-002 | game-concept.md Visual Identity P3 | VFX System | 切替時 0.1-0.2s 放射状グラデーション | — | ❌ GAP |

### Feature Layer（MVP）

| TR-ID | Source GDD | System | Requirement | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-abilities-001 | game-concept.md Core Mech 2 | Class Abilities | 剣士 / 弓士 / 魔法使い / 4 職目（Tier 3） | — | ❌ GAP |
| TR-abilities-002 | systems-index.md A1 | Class Abilities | `ClassAbilityData` (SO) + `IAbilityExecutor` (MB) + `AbilityContext` (DI) 三分割 | ADR-0001 枠組み + ADR-0004 (planned) | ⚠️ Partial |
| TR-abilities-003 | systems-index.md A2 | Class Abilities | ability が motor 内部状態を書かず、意図 API のみ叩く | ADR-0002 (interface に setter なし) | ✅ |
| TR-abilities-004 | systems-index.md PR-SCOPE | Class Abilities | balance 検証スクリプト化（4 職拡張で combinatorics 爆発防止、Tier 1） | — | ❌ GAP |
| TR-combat-001 | game-concept.md Pillar 3 | Combat System | Hit 感（hitstop + knockback + impact frame の 3 点） | ADR-0002 (Hitstop) | ⚠️ Partial |
| TR-combat-002 | systems-index.md CD2 | Combat System | Tier 0: HP=1 dummy enemy + hitstop + knockback impulse のみ | — | ❌ GAP |
| TR-combat-003 | game-concept.md, systems-index.md | Combat System | HP / ダメージ計算 / 状態異常 = VS（Health & Damage System） | — | ❌ GAP |
| TR-enemyai-001 | game-concept.md MVP | Enemy AI | Tier 0: 1 ダミー / VS: 3-4 archetype | — | ❌ GAP |

### Cross-Cutting

| TR-ID | Source | Domain | Requirement | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-perf-001 | technical-preferences.md | Performance | 60 fps / 16.6 ms / Update ≤ 4ms / Physics ≤ 2ms / Rendering ≤ 6ms | ADR-0001 (0.8ms) + ADR-0002 (0.5ms) 部分割当 | ⚠️ Partial |
| TR-perf-002 | technical-preferences.md | Performance | Memory ≤ 1 GB RAM (Steam Deck) / VRAM ≤ 1.5 GB / Draw Calls ≤ 300 | — | ❌ GAP |
| TR-loc-001 | systems-index.md Localization Discipline | Localization | コード生文字列禁止、`Strings.[Cat].[Key]` 形式のみ（MVP day 1 から強制） | — | ❌ GAP（規律のみ明文化） |
| TR-steam-001 | game-concept.md | Steam Integration | Steamworks.NET + Cloud Save + Achievements（Alpha） | — | ❌ GAP |
| TR-a11y-001 | technical-preferences.md | Accessibility | 全 UI d-pad/スティック操作可能、ホバー専用禁止 | — | ❌ GAP |

### Coverage Summary

| Status | Count | % |
|---|---|---|
| ✅ Covered | 14 | 36% |
| ⚠️ Partial | 5 | 13% |
| ❌ Gap | 20 | 51% |
| **Total extracted** | **39** | 100% |

---

## Phase 4: Cross-ADR Conflict Detection

### Conflicts Detected

#### ⚠️ CONCERN-1: ADR-0001 Summary 記述不整合（State Ownership of Hitstop）

**Type**: Wording inconsistency / State ownership ambiguity

**ADR-0001 line 27 (Summary)**:
> "Tier 0 では VFX/Audio System 不在でも minimal feedback（color wash + SE + **hitstop**）を ClassStateMachine が自己内包し"

**ADR-0002 line 27, 60-61, 622-624（GDD Requirements Addressed table）**:
- Hitstop の権威は `ICharacterMotor.ApplyHitstop()` + `MotorTuning.HitstopDefaultSec = 0.04s`
- CD1 Tier 0 Hitstop owner = CharacterController2D（明示）

**Reality check**: ADR-0001 の Switch Sequence (lines 137-166) には `ApplyHitstop` 呼び出しが**存在しない**。Implementation Guidelines にも hitstop 言及なし。実装は ADR-0002 と整合する。

**Impact**: 実装には影響しないが、Tier 0 デバッグ時に実装者が「ClassStateMachine が hitstop を持つはず」と誤解するリスク。

**Resolution Options**:
1. （推奨）ADR-0001 Summary line 27 を `（color wash + SE）` に修正、`hitstop` 言及削除
2. 明示化版：`（color wash + SE + hitstop は ICharacterMotor.ApplyHitstop 経由）`

unity-specialist も同見解で Option 1 を推奨。

### Other conflict checks: ✅ NO CONFLICT

| Check | Result |
|---|---|
| Data ownership conflict | ✅ なし — Class Switch (visual + audio cue) と CC2D (physical state + hitstop) は責務分離済 |
| Integration contract | ✅ ADR-0001 Forward Reference `ICharacterMotor` を ADR-0002 が確定（互換） |
| Performance budget | ✅ ADR-0001 (0.8ms / Update path) + ADR-0002 (0.5ms / FixedUpdate path) = フレーム予算 16.6ms 内 |
| Dependency cycle | ✅ 両者 Foundation 層、`Depends On: None`、循環なし |
| Architecture pattern | ✅ 両者とも SO + MB + interface パターンで一貫 |
| State management | ✅ Class Switch は `ClassChanged` event を publish のみ、CC2D は購読しない契約（ADR-0002 R6 / V6 で明文化） |

### ADR Dependency Order

#### Recommended Implementation Order

**Foundation (no dependencies)** — 並行可能:
1. **ADR-0001** Class Switch Architecture — `Status: Proposed (R5 gate)`
2. **ADR-0002** CharacterController2D + ICharacterMotor — `Status: Proposed (V1-V5 gate)`

> **重要**: ADR-0001 R5 spike と ADR-0002 V1-V5 spike は **同一プロトタイプセッションで合体実施**を ADR-0002 lines 580-582 が明示。R6（空中切替時 motor velocity 連続性）の検証機会を確保するため、独立実施は禁忌。両 ADR の Validation Gate 通過は同時イベント。

#### Pending（未起草、systems-index.md より優先順）

| Priority | ADR | Why |
|---|---|---|
| 3 | ADR-0003 (planned): VFX System Boundary + IVFXPublisher | Core 層、依存ゼロ、ADR-0001 の Tier 1 リファクタ target |
| 4 | ADR-0004 (planned): Class Abilities System 詳細 | ADR-0001/0002 を要 Accepted、`ClassAbilityData / IAbilityExecutor / AbilityContext` 三分割の最終契約 |
| 5 | ADR-0005 候補: Save Data System | MVP 最小スタブ前倒し済（PR-SCOPE Option A）、`ISaveable` + section-based JSON |
| 6 | ADR-0006 候補: Combo Input Buffer + Input System contract | ADR-0001 即時切替と ADR-0002 V9（`LockHorizontalControl` 中の Jump Buffer 消費規則）の境界仕様 |
| 7 | ADR-0007 候補: Camera System / Cinemachine 3 統合 | ADR-0002 `ICharacterMotor.Position` を follow target で参照 |
| 8 | ADR-0008 候補: Combat System Tier 0 minimal scope | systems-index.md CD2、ADR-0002 経由で Hitstop/Knockback 実現 |

**Unresolved dependencies**: ADR-0001 / ADR-0002 ともに Forward Reference の `IVFXPublisher` / `IAudioPublisher` / `AbilityContext` が未定義 ADR を指す。Tier 0 では参照を未定義のままでも実装可能（ADR-0001 が Tier 0 で `_audioSourceMinimal` 直叩きを許容）。Tier 1 リファクタ前に ADR-0003 / ADR-0004 起草必須。

**Cycle detection**: ✅ なし

---

## Phase 5: Engine Compatibility Audit

| Field | Result |
|---|---|
| Engine | Unity 6.3 LTS (6000.3.x) |
| ADRs with Engine Compatibility section | 2 / 2 ✅ |
| Version consistency | ✅ 両 ADR 一致 |
| Deprecated API references | ✅ なし — `Animator.Play()` は明示的 rejection、`Physics2D.autoSyncTransforms` は明示的 forbidden |
| Stale version references | ✅ なし |
| Post-cutoff API conflicts (between ADRs) | ✅ なし — ADR-0001 (2D Animation 10.x) と ADR-0002 (2D Physics Box2D v3) はドメイン非重複 |

### Engine Specialist Findings — unity-specialist

**Verdict**: **CONCERNS**

監査結果 A-G を確認。H（Summary 記述の hitstop 不整合）も確認、ADR-0001 Summary を `(color wash + SE)` に修正推奨。

#### Q1-Q5 Expert Responses (要約)

- **Q1 (SpriteLibraryAsset runtime swap with SpriteSkin/PSB)**: API 名は私の知識外、R5 gate 必須。SLA スワップ後の SpriteSkin ボーン再計算が核心リスク。
- **Q2 (Rigidbody2D.linearVelocity rename)**: Unity 6.0 で改名された蓋然性高、警告/エラーのどちらかは V1-b gate で確認必須。
- **Q3 (MovePosition → SyncTransforms → Cast)**: 「同 FixedUpdate 内の次 Cast が更新後位置を見る」保証は reference docs 未明文化。V2 gate 適切。
- **Q4 (MaterialPropertyBlock vs SpriteRenderer.color in URP 2D)**: ADR-0001 採用案 (`SpriteRenderer.color` 直書き) は **正しい判断**。MPB は SRP Batcher 環境で意図せずバッチ分断の既知問題あり。HDR color wash には注意（SpriteRenderer はクランプ）。
- **Q5 (Box2D v3 Cast 結果ソート順)**: 距離昇順ソートの保証が reference docs 未明文化。ADR-0002 ソルバーが `_castResults[0]` を最近傍と暗黙仮定する箇所あり — **明示的に最小 distance 選択ロジックを使うよう Implementation Guidelines に追記推奨**。

#### Additional Anti-Patterns Found

| # | Severity | ADR | Issue | Recommendation |
|---|---|---|---|---|
| **AP-1** | **Medium** | ADR-0001 | `_colorWashCoroutine` 多重起動ガード未明示 — 連打時 color 競合リスク | Implementation Guidelines に「`StartCoroutine` 前に `if (_colorWashCoroutine != null) StopCoroutine(_colorWashCoroutine)`」必須化 |
| AP-2 | Low | ADR-0001 | Coroutine alloc — Unity 6.0+ `Awaitable` が Tier 0 から使用可能 | Note 追加（blocking ではない） |
| **AP-3** | **Medium** | ADR-0002 | `Debug.Assert` は Release Build でも条件評価が残る — Steam Deck Verified ビルド Profiler 計測時の影響を Validation Criteria に未記載 | Validation Criteria に「Release Build Profiler で Awake Assert 計上を確認」追加 |
| AP-4 | Low | ADR-0002 | `Facing` enum 名と `Facing` プロパティ名が同一 — `using static` でコンパイラ警告誘発の可能性 | `enum FacingDirection` rename か、プロパティを `FacingDir` に rename |
| AP-5 | Low | ADR-0001 | `UnityEvent<ClassDefinition>` が Tier 1 で Addressables 移行時に `AssetReferenceT<ClassDefinition>` と競合の可能性 | Tier 1 リファクタ計画に「UnityEvent → AssetReferenceT 移行コスト評価」追記 |

---

## Phase 5b: GDD Revision Flags

| GDD | Assumption | Reality (from ADR/engine-reference) | Action |
|---|---|---|---|
| `design/gdd/game-concept.md` line 236 | "Recommended Engine: **Unity 6 LTS (6000.0.x)**" | プロジェクト pin は Unity **6.3 LTS (6000.3.x)** (`docs/engine-reference/unity/VERSION.md`、ADR-0001/0002 ともに 6.3 明示) | Revise GDD to "Unity 6.3 LTS (6000.3.x)" |
| `design/gdd/game-concept.md` line 333 (R-T3) | "ScriptableObject + SpriteLibrary + VFX プール化で **~0.4ms** で解決可能" | ADR-0001 Performance Implications: 切替コスト **0.7-0.8 ms**（cold path 1.0 ms 許容、ability reconfig 含む） | GDD 数値を ADR と整合（0.8ms）するか、原典記述を維持しつつ ADR への Forward Reference を追加 |

> 上記 2 件はいずれも軽微（version 文字列と数値見積）。アーキテクチャ判断は変更不要。

---

## Phase 7: Verdict

# 🟡 CONCERNS

### 根拠

- **PASS 不可**: 39 抽出要件のうち 51% (20件) が ❌ GAP — MVP 9 システムのうち ADR があるのは 2 システムのみ。Foundation/Core 層に未起草 ADR が複数（Save Data, Game State, Camera, Combo Input, VFX, Audio）。
- **FAIL 不可**: 抽出要件のうち architectural-decision-implying な要件は ADR-0001/0002 の Validation Gate で適切に管理されている。両 ADR 自体に blocking conflict なし。GAP は事前生産フェーズとして想定範囲内。
- **CONCERNS 該当**:
  1. ADR-0001 Summary 記述不整合（hitstop ownership wording）— 軽量修正で解決可能
  2. unity-specialist 提示の AP-1 / AP-3（Medium severity）は Tier 0 prototype で実害化するリスクあり、Validation Gate 通過前に対処推奨
  3. 残 ADR の起草ペース（systems-index.md "Next Steps" の ADR-0003 / ADR-0004 起草未着手）

### Blocking Issues

なし（CONCERNS 判定のため blocking なし）。ただし以下は Tier 0 prototype 着手前に対応推奨：

1. **ADR-0001 Summary 修正** (CONCERN-1) — line 27 から `+ hitstop` を削除または明示化
2. **ADR-0001 Implementation Guidelines に AP-1 追記** — ColorWashCoroutine 多重起動ガード必須化
3. **ADR-0002 Implementation Guidelines に Q5 補強追記** — Cast 結果の最小 distance 選択を明示
4. **ADR-0002 Validation Criteria に AP-3 追記** — Release Build Profiler Assert 確認

### Required ADRs（Pre-Production gate に向けて、優先順）

| Priority | ADR | Why |
|---|---|---|
| **High** | ADR-0003: VFX System Boundary + IVFXPublisher | ADR-0001 の Tier 1 リファクタ target、systems-index.md A4 で Core 層配置確定済 |
| **High** | ADR-0004: Class Abilities System 詳細 | ADR-0001/0002 を要 Accepted、`ClassAbilityData / IAbilityExecutor / AbilityContext` 三分割の最終契約 |
| **High** | ADR-0005 (Save Data System) | MVP 最小スタブ前倒し済、`ISaveable` interface + section-based JSON |
| Medium | ADR-0006 (Combo Input Buffer + Input System contract) | ADR-0001 即時切替と ADR-0002 V9 の境界仕様 |
| Medium | ADR-0007 (Camera System / Cinemachine 3 統合) | ADR-0002 `ICharacterMotor.Position` follow target |
| Low | ADR-0008 (Combat System Tier 0 minimal scope) | systems-index.md CD2、ADR-0002 経由で Hitstop/Knockback |

---

## Phase 9: Handoff

### Immediate Actions（推奨）

1. **ADR-0001 Summary 修正 + AP-1 追記** — 別セッションで `/architecture-decision adr-0001` 開きパッチ
2. **ADR-0002 Implementation Guidelines + Validation Criteria 追記** — Q5 / AP-3 反映
3. **ADR-0003 VFX System Boundary 起草** — `/architecture-decision vfx-system-boundary`
4. **ADR-0004 Class Abilities System 詳細起草** — ADR-0001/0002 Validation Gate 通過後（or 並行可）
5. **ADR-0005 Save Data System 起草** — Foundation 層、ADR-0001/0002 と並行可

### Gate Guidance

- 全 blocking issue 解消後、`/gate-check pre-production` を実行して Pre-Production フェーズへ遷移可否を判定
- Tier 0 prototype 着手前に最低でも ADR-0003 / ADR-0004 / ADR-0005 の 3 件起草が望ましい

### Rerun Trigger

- 各 ADR 新規起草後、`/architecture-review` を再実行して Coverage 改善を確認
- ADR-0001 R5 spike + ADR-0002 V1-V5 spike 通過後、両 ADR を Accepted に昇格させて再 review
- GDD 改訂（Phase 5b 反映）後、`/architecture-review single-gdd design/gdd/game-concept.md` で部分再 review

---

## References

- `design/gdd/game-concept.md`
- `design/gdd/systems-index.md`
- `docs/architecture/adr-0001-class-switch-architecture.md`
- `docs/architecture/adr-0002-character-controller-motor.md`
- `docs/architecture/tr-registry.yaml`
- `docs/engine-reference/unity/VERSION.md`
- `docs/engine-reference/unity/breaking-changes.md`
- `docs/engine-reference/unity/deprecated-apis.md`
- `docs/engine-reference/unity/current-best-practices.md`
- `docs/engine-reference/unity/modules/{physics,animation,input}.md`
- `.claude/docs/technical-preferences.md`

**Engine Specialist**: unity-specialist (consulted Phase 5)
