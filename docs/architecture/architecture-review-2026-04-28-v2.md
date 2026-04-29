# Architecture Review Report — 2026-04-28 (v2)

**Mode**: `coverage`（traceability only）
**Engine**: Unity 6.3 LTS (6000.3.x)
**GDDs Reviewed**: 2 (`game-concept.md`, `systems-index.md`)
**ADRs Reviewed**: 6 (ADR-0001 〜 ADR-0006、全 Status `Proposed`)
**TR Registry**: 31 件（v3、変更なし）
**Predecessor**: `architecture-review-2026-04-28.md`（同日朝、ADR-0005 まで反映）

---

## Executive Summary

ADR-0006 Camera System（thin provisional）の Proposed 書込を反映した **同日 2 回目** の coverage 再計測。

- **登録済 31 TR の 74% (23 件) が ✅ フルカバー**（前回 71% → +1）
- TR-camera-002 が ❌ → ✅、TR-camera-001 が ❌ → ⚠️ に昇格（thin provisional の 3 lock 効果）
- Gap 残 5 件はすべて Core 層の Combo / Feature 層の Abilities / Combat / Enemy AI
- Verdict: 🟡 **CONCERNS** 継続。Pre-Production gate に向け **ADR-0007 (Combo) + ADR-0008 (Abilities)** が次優先

---

## Phase 1 — Inputs Loaded

| Source | Count | Notes |
|---|---|---|
| GDDs | 2 | `game-concept.md`, `systems-index.md` |
| ADRs | 6 | ADR-0001..0006、全 Status `Proposed` |
| Engine reference | Unity 6.3 LTS | VERSION.md, breaking-changes, deprecated-apis（Cinemachine 3.x section 追加済） |
| TR registry (pre-review) | 31 entries | v3 (2026-04-28 朝) |
| `consistency-failures.md` | none | 未作成（履歴なし） |

---

## Phase 2/3 — Traceability Matrix

完全マトリクスは `traceability-index.md` を参照。サマリ:

### Coverage Δ（前回比）

| Status | 朝 (ADR-0005まで) | 今回 (ADR-0006 反映) | Δ |
|---|---|---|---|
| ✅ Covered | 22 (71%) | **23 (74%)** | +1 |
| ⚠️ Partial | 2 (6%) | **3 (10%)** | +1 |
| ❌ Gap | 7 (23%) | **5 (16%)** | -2 |

### TR-ID 状態遷移（ADR-0006 thin provisional 効果）

| TR-ID | 朝 | 現在 | 根拠 |
|---|---|---|---|
| TR-camera-001 | ❌ | ⚠️ | ADR-0006 Locked Decision 1（Cinemachine 3 採用）+ Decision 3（Confiner2D 採用）、Body component / Confiner method 名は R1 spike #4-5 で確定 |
| TR-camera-002 | ❌ | ✅ | ADR-0006 Locked Decision 2（`ICharacterMotor.Position` read-only Vector2 follow contract、bridge 方式は R1 spike + ADR-0002 V1 後に確定） |

### Coverage Gaps（残 5 件、登録済 TR）

| TR-ID | System | Layer | 提案 ADR | Engine Risk |
|---|---|---|---|---|
| TR-combo-001 | Combo | Core | ADR-0007 (Combo Input Buffer) | MEDIUM (timestamp 同期) |
| TR-combo-002 | Combo | Core | ADR-0007 | MEDIUM |
| TR-abilities-001 | Class Abilities | Feature | ADR-0008 | LOW |
| TR-combat-002 | Combat | Feature | ADR-0009 | LOW |
| TR-enemyai-001 | Enemy AI | Feature | VS 期 (Low priority) | LOW |

### Partial（⚠️ 3 件）

| TR-ID | 現状 | 必要なフォローアップ |
|---|---|---|
| **TR-camera-001** | ADR-0006 thin provisional で package + Confiner2D 採用 lock、Body component / Confiner method 名は deferred | C0 (R1 Editor spike) 通過後の follow-up ADR-0006a で full lock |
| TR-abilities-002 | ADR-0001 が Class Switch 側からのみ三分割保証、ability 側 Data/Executor/Context 細目なし | ADR-0008 で詳細化 |
| TR-combat-001 | ADR-0002 が hitstop solver-skip 保証、knockback/impact frame 未仕様 | ADR-0009 で knockback impulse + impact frame 規定 |

---

## Phase 4 — Cross-ADR Conflicts（coverage モード簡易確認）

ADR-0001..0006 の `ADR Dependencies` フィールドと state ownership を簡易チェック:

- **データ所有権競合**: なし。ADR-0006 は `state_ownership.motor_position.referenced_by` への append のみ、新規 ownership 主張なし
- **依存サイクル**: なし。Topo order: ADR-0005 → ADR-0004 → ADR-0001/0002 → ADR-0003 → ADR-0006
  - ADR-0006 (Camera) は ADR-0002（read-only Position）+ ADR-0003（CinemachineBrain 階層）を参照
- **Performance budget**: ADR-0006 は budget 値を deferred D10 へ送り未確定。R1 spike + Steam Deck 実機測定後に follow-up ADR で lock 予定
- **Status Proposed 連鎖**: 全 6 件が Proposed。ADR-0006 は **C0 (R1 spike) + C1 (ADR-0002 V1 後 1-frame sync 30/60/120Hz×50Hz physics matrix)** の 2 段階 gate

詳細 conflict 検出は `/architecture-review consistency` を別途実行推奨。

---

## Phase 5 — Engine Compatibility（coverage モード簡易確認）

すべての ADR で `Engine Compatibility` セクションが存在。Post-Cutoff API 宣言:

| ADR | Post-Cutoff API |
|---|---|
| ADR-0001 | `AssetReferenceT<ClassDefinition>` (Addressables 2.0) |
| ADR-0002 | `Physics2D.SyncTransforms()` 明示呼出 (Unity 6.3 Box2D v3) |
| ADR-0003 | `Sprite_ColorWash.shadergraph` (URP 2D Renderer Unified Render Graph) |
| ADR-0004 | Newtonsoft.Json for Unity（公式パッケージ）, Steamworks.NET ISteamRemoteStorage |
| ADR-0005 | Input System 1.8+ Action Rebinding UI |
| **ADR-0006** | **Cinemachine 3 (`com.unity.cinemachine`、Unity 6.3 LTS 同梱)、CinemachineConfiner2D Extension、CinemachineCamera 新 API** |

deprecated-apis.md と照合: 該当なし。ADR-0006 が deprecated-apis.md に Cinemachine 3.x 移行 section を新規追加済（CinemachineVirtualCamera / FramingTransposer / 旧 CinemachineBrain API 列挙）。詳細は `/architecture-review engine` を別途実行推奨。

---

## Phase 6 — Architecture Document Coverage

`docs/architecture/architecture.md` は未作成。`/create-architecture` で生成予定。本レビュー対象外。

---

## Verdict: 🟡 **CONCERNS**（継続、改善傾向）

| 評価軸 | 結果 |
|---|---|
| 登録済 TR カバー率 | **74%**（朝 71% → +1 件昇格） |
| Foundation 層 | ✅ 100% カバー |
| Core 層 | ⚠️ Camera partial（thin provisional）/ Combo gap、それ以外 ✅ |
| Feature 層 | ❌ Abilities + Combat 未着手、Enemy AI は VS 期 |
| Cross-ADR 競合 | なし |
| Engine 互換性 | OK（全 ADR `Engine Compatibility` 完備、deprecated-apis Cinemachine 3.x 反映済） |

### Pre-Production Gate に必要な追加 ADR（最低限）

1. **ADR-0007 Combo Input Buffer**（ADR-0002 V9 + ADR-0005 timestamp 配信の橋渡し、Core 層最後の gap）
2. **ADR-0008 Class Abilities System**（ADR-0001 Accepted 後、TR-abilities-001/002 解消）
3. ADR-0006a Camera System Implementation（R1 spike + ADR-0002 V1 通過後、deferred 11 件 lock）

ADR-0009 Combat Tier 0 と Enemy AI は VS 期入りまでに整えれば十分。

---

## Required Next Actions

1. **R1 + R5 + V1 の 3 spikes 並列実行**（独立、~1 week 想定）
   - R1: Camera Editor spike（半日、12 verification items）
   - R5: Class Switch spike（prototype template main 取込済）
   - V1: CharacterController2D spike（Cast + SyncTransforms + tunneling + Teleport API）
2. **ADR-0007 Combo Input Buffer 起草**（spike 待ち不要、独立 Core gap）
3. **GDD Revision Flags 解消**: game-concept.md L236（Unity 6 LTS → 6.3 LTS）/ L333（R-T3 0.4ms → 0.7-0.8ms）/ **L313-315（解像度 inconsistency、art-bible.md L656/L1410 と矛盾、ADR-0006 で新規検出）**
4. 次回 `/architecture-review full` 実行時に conflict + engine 完全監査

### Reflexion Log

`docs/consistency-failures.md` は未作成のため、新規 CONFLICT エントリの記録なし。今回の review でも 🔴 CONFLICT は検出されなかった。

---

## Files Updated by This Review

- `docs/architecture/traceability-index.md` (Coverage Summary 22→23 / 2→3 / 7→5、Camera 2 行 ✅/⚠️ 化、Verdict History 行追加)
- `docs/architecture/architecture-review-2026-04-28-v2.md` (this file)
- `docs/architecture/tr-registry.yaml` (last_updated note のみ、新規 TR なし)
- `production/session-state/active.md` (Session Extract 追記)
