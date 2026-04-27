# Architecture Traceability Index

**Last Updated**: 2026-04-27
**Engine**: Unity 6.3 LTS (6000.3.x)
**Source Review**: [architecture-review-2026-04-27.md](architecture-review-2026-04-27.md)

---

## Coverage Summary

| Status | Count | % |
|---|---|---|
| ✅ Covered | 14 | 36% |
| ⚠️ Partial | 5 | 13% |
| ❌ Gap | 20 | 51% |
| **Total extracted** | **39** | 100% |

> 抽出範囲: 29 システムのうち MVP 9 システム + 主要 VS システムの「architectural-decision-implying」要件のみ。実装詳細・コンテンツ要件は除外。MVP 9 システム該当 25 件は `tr-registry.yaml` に登録（残 14 件は VS / Polish / Cross-cutting で GDD authoring 時に追加登録予定）。

---

## Full Matrix

### Foundation Layer（MVP）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-input-001 | game-concept.md | Input System | Unity Input System 1.8+（Action Rebinding / Steam Input） | — | ❌ |
| TR-input-002 | game-concept.md | Input System | Gamepad full support / d-pad/スティック必須 | — | ❌ |
| TR-input-003 | systems-index.md | Input System | コンボバッファ向け timestamp 配信 | — | ❌ |
| TR-save-001 | game-concept.md | Save Data | schemaVersion + マイグレーションチェーン | — | ❌ |
| TR-save-002 | game-concept.md | Save Data | `.bak` 1 世代バックアップ | — | ❌ |
| TR-save-003 | systems-index.md A5 | Save Data | `ISaveable` + section-based JSON | — | ❌ |
| TR-save-004 | game-concept.md | Save Data | Newtonsoft.Json + Steam Cloud | — | ❌ |
| TR-save-005 | systems-index.md PR-SCOPE | Save Data | MVP 最小スタブ前倒し | — | ❌ |
| TR-gamestate-001 | systems-index.md A3 | Game State Machine | Title/Playing/Paused/GameOver FSM | — | ❌ |
| TR-gamestate-002 | systems-index.md A3 | Game State Machine | Scene Manager と直交 | — | ❌ |

### Core Layer（MVP + VS）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-cc2d-001 | systems-index.md A2 | CharacterController2D | Kinematic + 自作 Cast Solver | ADR-0002 | ✅ |
| TR-cc2d-002 | systems-index.md A2 | CharacterController2D | `ICharacterMotor` interface のみ expose | ADR-0002 | ✅ |
| TR-cc2d-003 | game-concept.md Pillar 3 | CharacterController2D | Coyote / Jump Buffer / Wall Slide | ADR-0002 | ✅ |
| TR-cc2d-004 | systems-index.md CD1 | CharacterController2D | Tier 0 Hitstop 30-50ms 内蔵 | ADR-0002 | ✅ |
| TR-cc2d-005 | game-concept.md R-T1, R-T4 | CharacterController2D | Box2D v3 + 明示 SyncTransforms | ADR-0002 | ✅ |
| TR-cc2d-006 | game-concept.md Retention | CharacterController2D | Determinism（リプレイ・スピラン） | ADR-0002 | ✅ |
| TR-classswitch-001 | game-concept.md Pillar 1 | Class Switch | R1/L1 1ボタン即時切替 | ADR-0001 | ✅ |
| TR-classswitch-002 | game-concept.md R-T3 | Class Switch | 1 フレーム視覚同期 | ADR-0001 | ✅ |
| TR-classswitch-003 | game-concept.md Visual P3 | Class Switch | Color Wash 0.1-0.2s | ADR-0001 | ⚠️ AP-1 |
| TR-classswitch-004 | game-concept.md Core Mech 1 | Class Switch | 切替 SE | ADR-0001 | ✅ |
| TR-classswitch-005 | game-concept.md Pillar 4 | Class Switch | 4 職目追加コードゼロ | ADR-0001 | ✅ |
| TR-classswitch-006 | systems-index.md A1 | Class Switch | 三分割の枠組み | ADR-0001 + ADR-0004 | ⚠️ |
| TR-camera-001 | game-concept.md | Camera | Cinemachine 3 + 2D Confiner | — | ❌ |
| TR-camera-002 | game-concept.md | Camera | `ICharacterMotor.Position` follow | — | ❌ |
| TR-combo-001 | game-concept.md Core Mech 4 | Combo Input Buffer | 先行入力 4-6 フレーム | — | ❌ |
| TR-combo-002 | game-concept.md Core Mech 4 | Combo Input Buffer | 空中キャンセル | — | ❌ |
| TR-scene-001 | game-concept.md | Scene & Addressables | Scene 単位動的ロード | — | ❌ |
| TR-scene-002 | game-concept.md | Scene & Addressables | AI 生成バリアント管理 | — | ❌ |
| TR-scene-003 | systems-index.md A3 | Scene & Addressables | async progress publish | — | ❌ |
| TR-audio-001 | game-concept.md | Audio System | AudioMixer Snapshot 切替 | — | ❌ |
| TR-audio-002 | game-concept.md | Audio System | SE プール化 / BGM ゾーン単位 | — | ❌ |
| TR-audio-003 | systems-index.md (implicit) | Audio System | `IAudioPublisher` サービス | — | ❌ |
| TR-vfx-001 | systems-index.md A4 | VFX System | `IVFXPublisher` pub/sub | — | ❌ |
| TR-vfx-002 | game-concept.md Visual P3 | VFX System | 切替放射状グラデーション | — | ❌ |

### Feature Layer（MVP）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-abilities-001 | game-concept.md Core Mech 2 | Class Abilities | 剣士/弓士/魔法使い/4 職目 | — | ❌ |
| TR-abilities-002 | systems-index.md A1 | Class Abilities | 三分割（Data/Executor/Context） | ADR-0001 + ADR-0004 | ⚠️ |
| TR-abilities-003 | systems-index.md A2 | Class Abilities | 意図 API のみ叩く | ADR-0002 | ✅ |
| TR-abilities-004 | systems-index.md PR-SCOPE | Class Abilities | balance 検証スクリプト化 | — | ❌ |
| TR-combat-001 | game-concept.md Pillar 3 | Combat | hitstop + knockback + impact frame | ADR-0002 部分 | ⚠️ |
| TR-combat-002 | systems-index.md CD2 | Combat | Tier 0 HP=1 dummy + 3 点 | — | ❌ |
| TR-combat-003 | game-concept.md | Combat | HP/ダメージ計算（VS） | — | ❌ |
| TR-enemyai-001 | game-concept.md MVP | Enemy AI | Tier 0: 1 ダミー / VS: 3-4 archetype | — | ❌ |

### Cross-Cutting

| TR-ID | Source | Domain | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-perf-001 | technical-preferences.md | Performance | 60 fps / 16.6 ms / Update ≤ 4ms / Physics ≤ 2ms | ADR-0001 (0.8ms) + ADR-0002 (0.5ms) | ⚠️ |
| TR-perf-002 | technical-preferences.md | Performance | Memory ≤ 1GB / VRAM ≤ 1.5GB / Draw Calls ≤ 300 | — | ❌ |
| TR-loc-001 | systems-index.md | Localization | 規律 MVP day 1 から、`Strings.[Cat].[Key]` | — | ❌ |
| TR-steam-001 | game-concept.md | Steam Integration | Steamworks.NET + Cloud + Achievements | — | ❌ |
| TR-a11y-001 | technical-preferences.md | Accessibility | d-pad/スティック操作可能、ホバー禁止 | — | ❌ |

---

## Known Gaps（提案 ADR 別）

### High Priority

#### ADR-0003 (planned): VFX System Boundary + IVFXPublisher
覆う TR-IDs: TR-vfx-001, TR-vfx-002, TR-classswitch-003 (Tier 1 リファクタ target)
基準: systems-index.md A4 で Core 層配置確定済
Suggested skill: `/architecture-decision vfx-system-boundary`

#### ADR-0004 (planned): Class Abilities System 詳細
覆う TR-IDs: TR-abilities-001, TR-abilities-002, TR-abilities-004, TR-classswitch-006
依存: ADR-0001 / ADR-0002 を要 Accepted（or 並行起草）
Suggested skill: `/architecture-decision class-abilities-system`

#### ADR-0005 候補: Save Data System
覆う TR-IDs: TR-save-001 〜 TR-save-005
基準: MVP 最小スタブ前倒し済（PR-SCOPE Option A）
Suggested skill: `/architecture-decision save-data-system`

### Medium Priority

#### ADR-0006 候補: Combo Input Buffer + Input System contract
覆う TR-IDs: TR-input-001, TR-input-002, TR-input-003, TR-combo-001, TR-combo-002
依存: ADR-0002 V9 (`LockHorizontalControl` 中の Jump Buffer 消費規則) を引用
Suggested skill: `/architecture-decision combo-input-buffer`

#### ADR-0007 候補: Camera System / Cinemachine 3 統合
覆う TR-IDs: TR-camera-001, TR-camera-002
依存: ADR-0002 `ICharacterMotor.Position` follow target 経由
Suggested skill: `/architecture-decision camera-system`

### Low Priority（VS 期で再評価可能）

#### ADR-0008 候補: Combat System Tier 0 minimal scope
覆う TR-IDs: TR-combat-001, TR-combat-002, TR-combat-003
依存: ADR-0002 経由で Hitstop/Knockback、systems-index.md CD2
Suggested skill: `/architecture-decision combat-system`

#### ADR-0009+ 候補: Game State Machine / Scene & Addressables / Audio System / Enemy AI
基準: VS 期（5-6 ヶ月）以降の起草で十分

---

## Superseded Requirements

なし（初回 review、TR Registry 初期化）

---

## Conflicts and Anti-Patterns Surfaced

### CONCERN-1: ADR-0001 Summary State Ownership of Hitstop
- **Type**: Wording inconsistency
- **Resolution**: ADR-0001 line 27 を `（color wash + SE）` に修正

### unity-specialist Anti-Patterns

| # | Severity | ADR | Issue |
|---|---|---|---|
| AP-1 | **Medium** | ADR-0001 | `_colorWashCoroutine` 多重起動ガード未明示 |
| AP-2 | Low | ADR-0001 | `Awaitable` (Unity 6.0+) 利用検討 |
| AP-3 | **Medium** | ADR-0002 | `Debug.Assert` Release Build 評価 — Validation Criteria に未記載 |
| AP-4 | Low | ADR-0002 | `Facing` enum 名と property 名が同一 |
| AP-5 | Low | ADR-0001 | `UnityEvent<ClassDefinition>` と `AssetReferenceT` の Tier 1 移行 |

### GDD Revision Flags

| GDD | Issue |
|---|---|
| game-concept.md line 236 | "Unity 6 LTS (6000.0.x)" → "Unity 6.3 LTS (6000.3.x)" |
| game-concept.md line 333 (R-T3) | "~0.4ms" → ADR-0001 の 0.7-0.8ms と整合 |

---

## Verdict History

| Date | Verdict | Coverage | Notes |
|---|---|---|---|
| 2026-04-27 | 🟡 CONCERNS | 36% Covered / 13% Partial / 51% Gap (39 reqs total) | Initial review. ADR-0001/0002 共に Validation Gate 待ち、Pre-Production 前に ADR-0003/0004/0005 起草要 |
