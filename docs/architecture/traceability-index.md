# Architecture Traceability Index

**Last Updated**: 2026-04-29
**Engine**: Unity 6.3 LTS (6000.3.x)
**Source Reviews**:
- [architecture-review-2026-04-29.md](architecture-review-2026-04-29.md) — coverage mode (latest, ADR-0007/0008/0009 反映、🟢 PASS)
- [architecture-review-2026-04-28-v2.md](architecture-review-2026-04-28-v2.md) — coverage mode (ADR-0006 反映)
- [architecture-review-2026-04-28.md](architecture-review-2026-04-28.md) — coverage mode (ADR-0005 まで)
- [architecture-review-2026-04-27.md](architecture-review-2026-04-27.md) — initial full review

---

## Coverage Summary

| Status | Count | % |
|---|---|---|
| ✅ Covered | 29 | 94% |
| ⚠️ Partial | 1 | 3% |
| ❌ Gap | 1 | 3% |
| **Total registered** | **31** | 100% |

> 抽出範囲: 29 システムのうち MVP 9 システム + 主要 VS システムの「architectural-decision-implying」要件のみ。実装詳細・コンテンツ要件は除外。31 件全件が `tr-registry.yaml` に登録済。残 deferred 15 件は VS / Polish / Cross-cutting で GDD authoring 時に追加登録予定。2026-04-29: ADR-0007/0008/0009 反映で TR-combo-001/002 / TR-abilities-001 / TR-combat-002 ❌→✅、TR-abilities-002 / TR-combat-001 ⚠️→✅ 昇格。MVP architectural coverage 達成（🟢 PASS）。

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
| TR-camera-001 | game-concept.md | Camera | Cinemachine 3 + 2D Confiner | ADR-0006 部分 | ⚠️ |
| TR-camera-002 | game-concept.md | Camera | `ICharacterMotor.Position` follow | ADR-0006 | ✅ |
| TR-combo-001 | game-concept.md Core Mech 4 | Combo Input Buffer | 先行入力 4-6 フレーム | ADR-0007 | ✅ |
| TR-combo-002 | game-concept.md Core Mech 4 | Combo Input Buffer | 空中キャンセル / 職業跨ぎ | ADR-0007 | ✅ |
| TR-vfx-001 | systems-index.md A4 | VFX System | `IVFXPublisher` pub/sub | ADR-0003 | ✅ |
| TR-vfx-002 | game-concept.md Visual P3 | VFX System | 切替放射状グラデーション | ADR-0003 | ✅ |

### Feature Layer（MVP）

| TR-ID | GDD | System | Requirement | ADR | Status |
|---|---|---|---|---|---|
| TR-abilities-001 | game-concept.md Core Mech 2 | Class Abilities | 剣士/弓士/魔法使い/4 職目 | ADR-0008 | ✅ |
| TR-abilities-002 | systems-index.md A1 | Class Abilities | 三分割（Data/Executor/Context） | ADR-0001 + ADR-0008 | ✅ |
| TR-abilities-003 | systems-index.md A2 | Class Abilities | 意図 API のみ叩く | ADR-0002 + ADR-0008 | ✅ |
| TR-combat-001 | game-concept.md Pillar 3 | Combat | hitstop + knockback + impact frame | ADR-0002 + ADR-0009 | ✅ |
| TR-combat-002 | systems-index.md CD2 | Combat | Tier 0 HP=1 dummy + 3 点 | ADR-0009 | ✅ |
| TR-enemyai-001 | game-concept.md MVP | Enemy AI | Tier 0: 1 ダミー / VS: 3-4 archetype | — | ❌ |

---

## Known Gaps（提案 ADR 別）

### High Priority（Pre-Production gate を阻むもの）

#### ADR-0006 Camera System（thin provisional 起草済 / 2026-04-28）
覆う TR-IDs: TR-camera-002 ✅ full / TR-camera-001 ⚠️ partial
Status: Proposed (Validation Gate C0-C1)
- C0: R1 Editor spike（半日、12 verification items）
- C1: ADR-0002 V1 通過後、1-frame sync 30/60/120Hz × 50Hz physics matrix
Follow-up: ADR-0006a で deferred 11 件（Body component / Damping / Foundation Singleton 適用 / ICameraDirector full surface / Camera Shake routing / TransformReadProxy / Pixel Perfect / Crop Frame / 6 anchor shake profiles / performance budgets / forbidden patterns 残 3 件）を R1 + R5 + V1 spike empirical data ベースで lock

#### ADR-0007 Combo Input Buffer ✅ 起草完了（2026-04-29 反映）
覆う TR-IDs: TR-combo-001 ✅ / TR-combo-002 ✅
Status: Proposed (Validation Gate CB0-CB7、ADR-0005 I0-I5 通過後に連動検証)

#### ADR-0008 Class Abilities System ✅ 起草完了（2026-04-29 反映）
覆う TR-IDs: TR-abilities-001 ✅ / TR-abilities-002 ✅
Status: Proposed (Validation Gate CA0-CA8、ADR-0001/0002/0003/0007 全通過後に連動検証)

#### ADR-0009 Combat System ✅ 起草完了（2026-04-29 反映）
覆う TR-IDs: TR-combat-001 ✅ / TR-combat-002 ✅
Status: Proposed (Validation Gate CS0-CS5、ADR-0008 CA0-CA8 通過後に連動検証)
副次: `IDamageReceiver` を `Game.Core` に確定 → 将来の TR-health-001（VS 期 #13 GDD）に contract 提供済

### Low Priority（VS 期で再評価可能）

#### ADR-0010+ 候補: Enemy AI / Game State Machine / Scene & Addressables / Audio System / Health & Damage System
基準: VS 期（5-6 ヶ月）以降の起草で十分。MVP では DummyEnemy (ADR-0009) で TR-enemyai-001 を機能代替。

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
| 2026-04-28 | 🟡 CONCERNS | 71% Covered / 6% Partial / 23% Gap (31 registered) | ADR-0003/0004/0005 確定後の coverage 再集計、TR registry +6 件 |
| 2026-04-28 (v2) | 🟡 CONCERNS | 74% Covered / 10% Partial / 16% Gap (31 registered) | ADR-0006 thin provisional 反映、TR-camera-002 ✅ / TR-camera-001 ⚠️、registry 変更なし |
| **2026-04-29** | **🟢 PASS** | **94% Covered / 3% Partial / 3% Gap (31 registered)** | ADR-0007/0008/0009 反映、TR-combo-001/002 + TR-abilities-001 + TR-combat-002 ❌→✅、TR-abilities-002 + TR-combat-001 ⚠️→✅。残 ⚠️ TR-camera-001（R1 spike 待ち）/ ❌ TR-enemyai-001（VS 期 defer、MVP は DummyEnemy で代替）。MVP architectural coverage 達成 |
