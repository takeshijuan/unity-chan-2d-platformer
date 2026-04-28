# Architecture Review Report — 2026-04-28

**Mode**: `coverage`（traceability only）
**Engine**: Unity 6.3 LTS (6000.3.x)
**GDDs Reviewed**: 2 (`game-concept.md`, `systems-index.md`)
**ADRs Reviewed**: 5 (ADR-0001 〜 ADR-0005)
**TR Registry**: 31 件（前回 25 件 + 今回 +6 件）

---

## Executive Summary

前回（2026-04-27）から ADR-0003 (VFX System Boundary)、ADR-0004 (Save Data System)、ADR-0005 (Input System Architecture) が追加され、Foundation 層が **100% フルカバー** に到達。Core 層も Camera / Combo を残すのみ。

- **登録済 31 TR の 71% (22 件) が ✅ フルカバー**（前回 36% → 71%）
- Gap 残 7 件はすべて Core / Feature 層、Pre-Production gate に向けて Camera + Combo + Class Abilities + Combat ADR の起草が必要
- すべての Status `Proposed` の ADR は Validation Gate 通過後に Accepted へ

---

## Phase 1 — Inputs Loaded

| Source | Count | Notes |
|---|---|---|
| GDDs | 2 | `game-concept.md`, `systems-index.md` |
| ADRs | 5 | ADR-0001..0005、すべて Status `Proposed` |
| Engine reference | Unity 6.3 LTS | VERSION.md, breaking-changes, deprecated-apis |
| TR registry (pre-review) | 25 entries | v2 (2026-04-27) |
| `consistency-failures.md` | none | 未作成（履歴なし） |

---

## Phase 2/3 — Traceability Matrix

完全マトリクスは `traceability-index.md` を参照。サマリ:

### Coverage Δ（前回比）

| Status | 2026-04-27 (39 件中) | 2026-04-28 (31 登録中) | Δ |
|---|---|---|---|
| ✅ Covered | 14 (36%) | **22 (71%)** | +8 |
| ⚠️ Partial | 5 (13%) | **2 (6%)** | -3 |
| ❌ Gap | 20 (51%) | **7 (23%)** | -13 (うち 14 件は登録外へ移動 / 残 deferred) |

### 新規完全カバーされた TR-ID（+8 件）

| TR-ID | 移行元 | 移行先 ADR |
|---|---|---|
| TR-input-001/002/003 | ❌ | ADR-0005 |
| TR-save-003/004/005 | ❌ | ADR-0004 |
| TR-classswitch-003 | ⚠️ AP-1 | ADR-0001 + ADR-0003 |
| TR-classswitch-005/006 | 未登録 | ADR-0001（新規登録 + 即フルカバー） |
| TR-save-001/002 | 未登録 | ADR-0004（新規登録 + 即フルカバー） |
| TR-vfx-001/002 | 未登録 | ADR-0003（新規登録 + 即フルカバー） |

### Coverage Gaps（残 7 件、登録済 TR）

| TR-ID | System | Layer | 提案 ADR | Engine Risk |
|---|---|---|---|---|
| TR-camera-001 | Camera | Core | ADR-0006 (Camera System) | LOW (Cinemachine 3 既知) |
| TR-camera-002 | Camera | Core | ADR-0006 | LOW |
| TR-combo-001 | Combo | Core | ADR-0007 (Combo Input Buffer) | MEDIUM (timestamp 同期) |
| TR-combo-002 | Combo | Core | ADR-0007 | MEDIUM |
| TR-abilities-001 | Class Abilities | Feature | ADR-0008 | LOW |
| TR-combat-002 | Combat | Feature | ADR-0009 | LOW |
| TR-enemyai-001 | Enemy AI | Feature | VS 期 (Low priority) | LOW |

### Partial（⚠️ 2 件）

| TR-ID | 現状 | 必要なフォローアップ |
|---|---|---|
| TR-abilities-002 | ADR-0001 が Class Switch 側からのみ三分割を保証、ability 側の Data/Executor/Context 細目なし | ADR-0008 で詳細化 |
| TR-combat-001 | ADR-0002 が hitstop solver-skip を保証、knockback/impact frame は未仕様 | ADR-0009 で knockback impulse + impact frame 規定 |

---

## Phase 4 — Cross-ADR Conflicts（coverage モード簡易確認）

ADR-0001..0005 の `ADR Dependencies` フィールドと state ownership を簡易チェック:

- **データ所有権競合**: なし。state_ownership table（registry）で重複所有なし
- **依存サイクル**: なし。Topo order: ADR-0005 → ADR-0004 → ADR-0001/0002 → ADR-0003
  - ADR-0005 (Input) は最 Foundation
  - ADR-0004 (Save Data) は Input と独立
  - ADR-0003 (VFX) は ADR-0001/0002 の event/notification を subscribe
- **Performance budget**: ADR-0001 (0.7-0.8ms) + ADR-0002 (0.5ms) + ADR-0003 (0.2ms/event, 1.5ms/frame total) = フレーム budget 16.6ms 以内
- **Status Proposed 連鎖**: 全 5 件が Proposed のため、Validation Gate G1-G5 を順次通過させる必要あり（依存先 ADR が Proposed のままだと、依存元の Accepted は不可）

詳細 conflict 検出は `/architecture-review consistency` を別途実行推奨。

---

## Phase 5 — Engine Compatibility（coverage モード簡易確認）

すべての ADR で `Engine Compatibility` セクションが存在。Post-Cutoff API は次の通り宣言済:

| ADR | Post-Cutoff API |
|---|---|
| ADR-0001 | `AssetReferenceT<ClassDefinition>` (Addressables 2.0) |
| ADR-0002 | `Physics2D.SyncTransforms()` 明示呼出 (Unity 6.3 Box2D v3) |
| ADR-0003 | `Sprite_ColorWash.shadergraph` (URP 2D Renderer Unified Render Graph) |
| ADR-0004 | Newtonsoft.Json for Unity（公式パッケージ）, Steamworks.NET ISteamRemoteStorage |
| ADR-0005 | Input System 1.8+ Action Rebinding UI |

deprecated-apis.md と照合: 該当なし。詳細は `/architecture-review engine` を別途実行推奨。

---

## Phase 6 — Architecture Document Coverage

`docs/architecture/architecture.md` は未作成。`/create-architecture` で生成予定。本レビュー対象外。

---

## Verdict: 🟡 **CONCERNS**

| 評価軸 | 結果 |
|---|---|
| 登録済 TR カバー率 | 71%（前回 36% → 大幅改善） |
| Foundation 層 | ✅ 100% カバー（Input + Save + 部分 Core） |
| Core 層 | ⚠️ Camera + Combo + VFX のうち VFX のみカバー |
| Feature 層 | ❌ Class Abilities + Combat 未着手、Enemy AI は VS 期 |
| Cross-ADR 競合 | なし |
| Engine 互換性 | OK（全 ADR `Engine Compatibility` 完備） |

### Pre-Production Gate に必要な追加 ADR（最低限）

1. **ADR-0006 Camera System**（依存軽い、ADR-0002 を引用するだけ）
2. **ADR-0007 Combo Input Buffer**（ADR-0002 V9 + ADR-0005 timestamp 配信の橋渡し）
3. **ADR-0008 Class Abilities System**（ADR-0001 Accepted 後）

ADR-0009 Combat Tier 0 と Enemy AI は VS 期入りまでに整えれば十分。

---

## Required Next Actions

1. ADR-0006 〜 ADR-0008 の起草（優先度順）
2. Status `Proposed` ADR-0001..0005 の Validation Gate 通過 → Accepted 昇格
3. GDD Revision Flags（game-concept.md line 236, 333）の解消
4. 次回 `/architecture-review full` 実行時に conflict + engine 完全監査

### Reflexion Log

`docs/consistency-failures.md` は未作成のため、新規 CONFLICT エントリの記録なし。今回の review でも 🔴 CONFLICT は検出されなかった。

---

## Files Updated by This Review

- `docs/architecture/tr-registry.yaml` v2 → v3 (+6 entries: TR-save-001/002, TR-classswitch-005/006, TR-vfx-001/002)
- `docs/architecture/traceability-index.md` (Coverage Summary, Full Matrix, Verdict History 更新)
- `docs/architecture/architecture-review-2026-04-28.md` (this file)
- `production/session-state/active.md` (Session Extract 追記)
