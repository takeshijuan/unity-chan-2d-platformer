# Architecture Review — 2026-04-29 (coverage mode)

**Date**: 2026-04-29
**Mode**: `/architecture-review coverage`
**Engine**: Unity 6.3 LTS (6000.3.x)
**GDDs Reviewed**: 2 — `design/gdd/game-concept.md`, `design/gdd/systems-index.md`
**ADRs Reviewed**: 9 — ADR-0001 〜 ADR-0009（全て Proposed）

> 前回 v2（2026-04-28）以降の差分: ADR-0007 (Combo Input Buffer) / ADR-0008 (Class Abilities System) / ADR-0009 (Combat System) の 3 件が起草され、5 件残っていた Feature/Core ギャップのうち 4 件を解消、1 件を ⚠️ → ✅ 昇格させた。

---

## Verdict: 🟢 PASS

MVP scope に対する architectural coverage を達成（94%）。残 2 件は MVP gate を阻害しない:

- **TR-camera-001**: ⚠️ Partial（ADR-0006 thin provisional 採用済、R1 spike で残部分 lock 予定）
- **TR-enemyai-001**: ❌ Gap（MVP では DummyEnemy / ADR-0009 で機能代替、VS 期に ADR-0010 起草想定）

Pre-Production gate 移行可能。Validation Gate 通過（全 9 ADR の Accepted 昇格）が次のマイルストーン。

---

## Coverage Summary

| Status | Count | % | Δ from v2 |
|---|---|---|---|
| ✅ Covered | **29** | **94%** | +6 |
| ⚠️ Partial | 1 | 3% | -2 |
| ❌ Gap | 1 | 3% | -4 |
| **Total registered** | **31** | 100% | 変更なし |

---

## TR-ID 状態遷移（v2 → 今回）

| TR-ID | Before | After | 反映 ADR | 根拠 |
|---|---|---|---|---|
| TR-combo-001 | ❌ | ✅ | ADR-0007 | BufferWindowSec = 0.083s (≒5f)、Ring Buffer + IComboBuffer |
| TR-combo-002 | ❌ | ✅ | ADR-0007 | Flush() 不呼出によるクラス跨ぎ保持、空中キャンセル仕様 |
| TR-abilities-001 | ❌ | ✅ | ADR-0008 | ClassAbilityData SO + IAbilityExecutor で 3 職分の能力定義可能 |
| TR-abilities-002 | ⚠️ | ✅ | ADR-0008 | AbilityContext DI による三分割完全実現（A1 申し送り解消） |
| TR-combat-001 | ⚠️ | ✅ | ADR-0009 | HitstopSec=0.04f / KnockbackImpulse=5f を ClassAbilityData に追記、Pillar 3 触覚 3 点完成 |
| TR-combat-002 | ❌ | ✅ | ADR-0009 | DummyEnemy : IDamageReceiver、HitConfirmed → CombatSystem thin mediator、AddForce(knockback) |

---

## Layer 別 Coverage

| Layer | Covered | % |
|---|---|---|
| Foundation（Input + Save） | 8/8 | **100%** ✅ |
| Core（CC2D + ClassSwitch + Camera + Combo + VFX） | 18/19 | 95% |
| Feature（Abilities + Combat + EnemyAI） | 5/6 | 83% |

---

## 残存ギャップ（2 件）

### ⚠️ TR-camera-001 — Cinemachine 3 + 2D Confiner（Partial）

**現状**: ADR-0006 thin provisional で以下が lock 済:
- Camera package: Cinemachine 3 (`com.unity.cinemachine`、Unity 6.3 LTS 同梱)
- Follow contract: `ICharacterMotor.Position` 経由
- MVP confiner: 単一 PolygonCollider2D + `CinemachineConfiner2D.BoundingShape2D` Inspector アサイン

**未確定（deferred to R1 spike + ADR-0006a）**: Body component 名 / Confiner method 名 / Damping / Foundation Singleton 適用 / ICameraDirector full surface / Camera Shake routing / TransformReadProxy / Pixel Perfect Reference / Crop Frame setup / 6 anchor shake profiles / performance budgets / forbidden patterns 残 3 件

**アクション**: R1 Editor spike（半日、Unity 6.3 LTS macOS Editor、12 verification items）→ 結果を `production/qa/evidence/r1-camera-cinemachine3-spike-result.md` に記録 → ADR-0006a で残 11 件 lock

---

### ❌ TR-enemyai-001 — Tier 0 1 ダミー / VS 3-4 archetype（Gap）

**現状**: MVP では ADR-0009 の `DummyEnemy : IDamageReceiver` が「1-hit で disable + knockback で吹き飛び」の Tier 0 機能要件を機能的に代替している。

**判定**: Pre-Production gate 阻害なし。AI 構造（state machine / behavior tree / perception）の architectural decision は VS 期（5-6 ヶ月）で十分。

**アクション**: VS 期に `/architecture-decision enemy-ai-system`（ADR-0010 候補）。MVP 期間中はギャップとして許容。

---

## Validation Gates Outstanding（全 9 ADR）

| ADR | Gate | 内容 |
|-----|------|------|
| ADR-0001 | R5 | Class Switch Spike（spike template main 取込済、実行待ち） |
| ADR-0002 | V1-V5 | Cast / SyncTransforms / tunneling / Teleport API |
| ADR-0003 | G1-G5 | Anchor cue / Pool hot path / Color Wash sorting / Cold-miss telemetry / Steam Deck |
| ADR-0004 | S1-S6 | Save round-trip / Crash safety / Migration / Forbidden enforcement / Steam Deck I/O / IL2CPP CI |
| ADR-0005 | I0-I5 | IL2CPP smoke / Action Map bleed-through / Rebinding / Combo timestamp / Roslyn / Steam Deck |
| ADR-0006 | C0-C1 | R1 Editor spike + ADR-0002 V1 後の 1-frame sync 30/60/120Hz × 50Hz physics matrix |
| ADR-0007 | CB0-CB7 | ADR-0005 I0-I5 通過後に連動検証 |
| ADR-0008 | CA0-CA8 | ADR-0001/0002/0003/0007 全通過後に連動検証 |
| ADR-0009 | CS0-CS5 | ADR-0008 CA0-CA8 通過後に連動検証 |

**実装可能順（topological）**:
1. **Foundation**（独立）: ADR-0004 (Save) / ADR-0005 (Input)
2. **Core**: ADR-0001 (ClassSwitch) ← R5 / ADR-0002 (CC2D) ← V1-V5 / ADR-0003 (VFX) ← G1-G5
3. **Composition**: ADR-0006 (Camera) ← ADR-0002 V1 / ADR-0007 (Combo) ← ADR-0005 I0-I5
4. **Feature**: ADR-0008 (Abilities) ← ADR-0001/0002/0003/0007 / ADR-0009 (Combat) ← ADR-0008

---

## TR Registry Update

新規 TR-ID 追加なし。既存 31 件のステータス遷移のみ。

将来登録候補（ADR-0009 の `IDamageReceiver` 確定により先取り contract 提供済）:
- `TR-health-001`: HealthComponent + IDamageReceiver 実装（VS 期 #13 Health & Damage System GDD 起草時）

---

## GDD Revision Flags（前回からの繰越）

| GDD | Issue | Status |
|---|---|---|
| game-concept.md L236 | "Unity 6 LTS (6000.0.x)" → "Unity 6.3 LTS" | 🟡 open |
| game-concept.md L333 (R-T3) | "~0.4ms" → ADR-0001 0.7-0.8ms | 🟡 open |
| game-concept.md L313-315 | 解像度 inconsistency (game-concept 128/96/64 vs art-bible 384×216 / 48×48 / 32-64) | 🟡 open |

これらは coverage mode の対象外（前回 v2 から繰越）。次回 `full` mode 実行時または GDD 修正セッションで処理。

---

## Conflicts and Anti-Patterns

本 review（coverage mode）では新規 conflict 検出なし。前回 v2 の AP-1 〜 AP-5 のうち以下が解消方向:

- **AP-1（ColorWashCoroutine 多重起動ガード）**: ADR-0003 IVFXPublisher 移行で Tier 1 解消予定 — 引き続き open
- **AP-2 / AP-4 / AP-5**: 引き続き open（次回 full review で再評価）
- **AP-3（Debug.Assert Release Build）**: ADR-0002 V2 Validation Gate 待ち

---

## Recommended Next Actions

### 即時（並列実行可、~1 week）
1. **R1 Camera Editor Spike** 実行 → ADR-0006 C0 解消
2. **R5 Class Switch Spike** 実行 → ADR-0001 R5 解消（template main 取込済）
3. **V1 CharacterController2D Spike** 実行 → ADR-0002 V1 解消（Teleport API 追加含む）

### 中期（spike 通過後）
4. **ADR-0006a Camera System Implementation** 起草（deferred 11 件を empirical data ベースで lock）
5. ADR-0001/0002/0003/0004/0005 を Accepted へ昇格
6. ADR-0007/0008/0009 の連動 Validation Gate 検証（依存 ADR Accepted 後）

### Pre-Production gate へ
7. 全 9 ADR Accepted 後 → `/gate-check pre-production`

---

## Verdict History

| Date | Verdict | Coverage | Notes |
|---|---|---|---|
| 2026-04-27 | 🟡 CONCERNS | 36% Covered / 13% Partial / 51% Gap (39 reqs) | Initial review |
| 2026-04-28 | 🟡 CONCERNS | 71% Covered / 6% Partial / 23% Gap (31 registered) | ADR-0003/0004/0005 確定後 |
| 2026-04-28 (v2) | 🟡 CONCERNS | 74% Covered / 10% Partial / 16% Gap (31 registered) | ADR-0006 thin provisional 反映 |
| **2026-04-29** | **🟢 PASS** | **94% Covered / 3% Partial / 3% Gap (31 registered)** | **ADR-0007/0008/0009 反映、MVP coverage 達成** |
