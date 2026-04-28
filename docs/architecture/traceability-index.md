# Architecture Traceability Index

**Last Updated**: 2026-04-28
**Engine**: Unity 6.3 LTS (6000.3.x)
**Source Reviews**:
- [architecture-review-2026-04-28.md](architecture-review-2026-04-28.md) — coverage mode (latest)
- [architecture-review-2026-04-27.md](architecture-review-2026-04-27.md) — initial full review

---

## Coverage Summary

| Status | Count | % |
|---|---|---|
| ✅ Covered | 22 | 71% |
| ⚠️ Partial | 2 | 6% |
| ❌ Gap | 7 | 23% |
| **Total registered** | **31** | 100% |

> 抽出範囲: 29 システムのうち MVP 9 システム + 主要 VS システムの「architectural-decision-implying」要件のみ。実装詳細・コンテンツ要件は除外。31 件全件が `tr-registry.yaml` に登録済（2026-04-28 で +6 件: TR-save-001/002, TR-classswitch-005/006, TR-vfx-001/002）。残 deferred 15 件は VS / Polish / Cross-cutting で GDD authoring 時に追加登録予定。

---

## Full Matrix

### Foundation Layer（MVP）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-input-001 | game-concept.md | Input System | Unity Input System 1.8+（Action Rebinding / Steam Input） | ADR-0005 | ✅ |
| TR-input-002 | game-concept.md | Input System | Gamepad full support / d-pad/スティック必須 | ADR-0005 | ✅ |
| TR-input-003 | systems-index.md | Input System | コンボバッファ向け timestamp 配信 | ADR-0005 | ✅ |
| TR-save-001 | game-concept.md | Save Data | schemaVersion + マイグレーションチェーン | ADR-0004 | ✅ |
| TR-save-002 | game-concept.md | Save Data | `.bak` 1 世代バックアップ | ADR-0004 | ✅ |
| TR-save-003 | systems-index.md A5 | Save Data | `ISaveable` + section-based JSON | ADR-0004 | ✅ |
| TR-save-004 | game-concept.md | Save Data | Newtonsoft.Json + Steam Cloud | ADR-0004 | ✅ |
| TR-save-005 | systems-index.md PR-SCOPE | Save Data | MVP 最小スタブ前倒し | ADR-0004 | ✅ |

### Core Layer（MVP + VS）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-cc2d-001 | systems-index.md A2 | CharacterController2D | Kinematic + 自作 Cast Solver | ADR-0002 | ✅ |
| TR-cc2d-002 | systems-index.md A2 | CharacterController2D | `ICharacterMotor` interface のみ expose | ADR-0002 | ✅ |
| TR-cc2d-003 | game-concept.md Pillar 3 | CharacterController2D | Coyote / Jump Buffer / Wall Slide | ADR-0002 | ✅ |
| TR-cc2d-004 | systems-index.md CD1 | CharacterController2D | Tier 0 Hitstop 30-50ms 内蔵 | ADR-0002 | ✅ |
| TR-cc2d-005 | game-concept.md R-T1, R-T4 | CharacterController2D | Box2D v3 + 明示 SyncTransforms | ADR-0002 | ✅ |
| TR-classswitch-001 | game-concept.md Pillar 1 | Class Switch | R1/L1 1ボタン即時切替 | ADR-0001 | ✅ |
| TR-classswitch-002 | game-concept.md R-T3 | Class Switch | 1 フレーム視覚同期 | ADR-0001 | ✅ |
| TR-classswitch-003 | game-concept.md Visual P3 | Class Switch | Color Wash 0.1-0.2s | ADR-0001 + ADR-0003 | ✅ |
| TR-classswitch-004 | game-concept.md Core Mech 1 | Class Switch | 切替 SE | ADR-0001 | ✅ |
| TR-classswitch-005 | game-concept.md Pillar 4 | Class Switch | 4 職目追加コードゼロ | ADR-0001 | ✅ |
| TR-classswitch-006 | systems-index.md A1 | Class Switch | 三分割の枠組み | ADR-0001 | ✅ |
| TR-camera-001 | game-concept.md | Camera | Cinemachine 3 + 2D Confiner | — | ❌ |
| TR-camera-002 | game-concept.md | Camera | `ICharacterMotor.Position` follow | — | ❌ |
| TR-combo-001 | game-concept.md Core Mech 4 | Combo Input Buffer | 先行入力 4-6 フレーム | — | ❌ |
| TR-combo-002 | game-concept.md Core Mech 4 | Combo Input Buffer | 空中キャンセル | — | ❌ |
| TR-vfx-001 | systems-index.md A4 | VFX System | `IVFXPublisher` pub/sub | ADR-0003 | ✅ |
| TR-vfx-002 | game-concept.md Visual P3 | VFX System | 切替放射状グラデーション | ADR-0003 | ✅ |

### Feature Layer（MVP）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-abilities-001 | game-concept.md Core Mech 2 | Class Abilities | 剣士/弓士/魔法使い/4 職目 | — | ❌ |
| TR-abilities-002 | systems-index.md A1 | Class Abilities | 三分割（Data/Executor/Context） | ADR-0001 部分 | ⚠️ |
| TR-abilities-003 | systems-index.md A2 | Class Abilities | 意図 API のみ叩く | ADR-0002 | ✅ |
| TR-combat-001 | game-concept.md Pillar 3 | Combat | hitstop + knockback + impact frame | ADR-0002 部分 | ⚠️ |
| TR-combat-002 | systems-index.md CD2 | Combat | Tier 0 HP=1 dummy + 3 点 | — | ❌ |
| TR-enemyai-001 | game-concept.md MVP | Enemy AI | Tier 0: 1 ダミー / VS: 3-4 archetype | — | ❌ |

---

## Known Gaps（提案 ADR 別）

### High Priority（Pre-Production gate を阻むもの）

#### ADR-0006 候補: Camera System / Cinemachine 3 統合
覆う TR-IDs: TR-camera-001, TR-camera-002
依存: ADR-0002 `ICharacterMotor.Position` follow target 経由
Suggested skill: `/architecture-decision camera-system`

#### ADR-0007 候補: Combo Input Buffer
覆う TR-IDs: TR-combo-001, TR-combo-002
依存: ADR-0002 V9 (`LockHorizontalControl` 中の Jump Buffer 消費規則) + ADR-0005 timestamp 配信を引用
Suggested skill: `/architecture-decision combo-input-buffer`

#### ADR-0008 候補: Class Abilities System 詳細
覆う TR-IDs: TR-abilities-001, TR-abilities-002（partial → full に昇格）
依存: ADR-0001 Accepted 後
Suggested skill: `/architecture-decision class-abilities-system`

### Medium Priority

#### ADR-0009 候補: Combat System Tier 0 minimal scope
覆う TR-IDs: TR-combat-001（partial → full）, TR-combat-002
依存: ADR-0002 経由で Hitstop/Knockback、systems-index.md CD2
Suggested skill: `/architecture-decision combat-system-tier0`

### Low Priority（VS 期で再評価可能）

#### ADR-0010+ 候補: Enemy AI / Game State Machine / Scene & Addressables / Audio System
基準: VS 期（5-6 ヶ月）以降の起草で十分

---

## ADR Number Allocation Note

2026-04-27 の予約番号と現状の食い違い:

| 予約 (2026-04-27) | 実装 (2026-04-28) |
|---|---|
| ADR-0003: VFX System Boundary | ✅ ADR-0003 として確定 |
| ADR-0004: Class Abilities System | ❌ ADR-0004 は **Save Data System** に再割当（Foundation 優先） |
| ADR-0005: Save Data System | ❌ ADR-0005 は **Input System Architecture** に再割当 |
| Class Abilities System | → ADR-0008 で予定 |
| Camera System | → ADR-0006 で予定 |
| Combo Input Buffer | → ADR-0007 で予定 |

---

## Superseded Requirements

なし（registry append のみ、deprecation なし）

---

## Conflicts and Anti-Patterns Surfaced

### 2026-04-27 Initial Review (resolved status)

| # | Severity | ADR | Issue | Status (2026-04-28) |
|---|---|---|---|---|
| CONCERN-1 | Low | ADR-0001 | Summary Hitstop 表記 | 2026-04-27 で `（color wash + SE）` に修正済 |
| AP-1 | Medium | ADR-0001 | `_colorWashCoroutine` 多重起動ガード | ADR-0003 IVFXPublisher 移行で Tier 1 解消予定 |
| AP-2 | Low | ADR-0001 | `Awaitable` (Unity 6.0+) 利用検討 | 引き続き open |
| AP-3 | Medium | ADR-0002 | `Debug.Assert` Release Build 評価 | Validation Gate G2 で確認待ち |
| AP-4 | Low | ADR-0002 | `Facing` enum 名と property 名 | 引き続き open |
| AP-5 | Low | ADR-0001 | `UnityEvent<ClassDefinition>` Tier 1 移行 | ADR-0003 と整合済 |

### GDD Revision Flags（2026-04-27 検出）

| GDD | Issue | Status |
|---|---|---|
| game-concept.md line 236 | "Unity 6 LTS (6000.0.x)" → "Unity 6.3 LTS" | open（systems index 未反映） |
| game-concept.md line 333 (R-T3) | "~0.4ms" → ADR-0001 0.7-0.8ms | open |

---

## Verdict History

| Date | Verdict | Coverage | Notes |
|---|---|---|---|
| 2026-04-27 | 🟡 CONCERNS | 36% Covered / 13% Partial / 51% Gap (39 reqs total) | Initial review |
| 2026-04-28 | 🟡 CONCERNS | **71% Covered / 6% Partial / 23% Gap (31 registered)** | ADR-0003/0004/0005 確定後の coverage 再集計、TR registry +6 件 |
