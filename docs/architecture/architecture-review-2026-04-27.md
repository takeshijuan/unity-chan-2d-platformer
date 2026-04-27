# Architecture Review Report — Coverage Mode

**Date**: 2026-04-27
**Mode**: `/architecture-review coverage`
**Engine**: Unity 6.3 LTS (6000.3.x)
**GDDs Reviewed**: 2（`game-concept.md`、`systems-index.md`）— system-specific GDD は未着手
**ADRs Reviewed**: 3（ADR-0001 / ADR-0002 / ADR-0003、いずれも `Proposed`）
**TR Registry**: 初回エントリ 26 件登録（`docs/architecture/tr-registry.yaml`）

---

## Traceability Matrix

| TR-ID | Source | System | Requirement（要約） | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-pillar-001 | concept Pillar 1 | Class Switch | R1/L1 1 ボタン即時、1 フレーム視覚同期 | ADR-0001 | ✅ |
| TR-pillar-002 | concept Pillar 1 / Core Mech 1 | Class Switch | 切替時 visual (color wash) + audio (SE) 報酬 | ADR-0001 (Tier 0 inline) + ADR-0003 (Tier 1) | ✅ |
| TR-pillar-003 | concept Pillar 3 | CC2D / Combat | hitstop + knockback + impact frame の重量感 | ADR-0002 | ⚠️ Partial — Combat ADR 未着手 |
| TR-pillar-004 | concept Pillar 4 | Class Switch | 4 職拡張がコード変更ゼロ | ADR-0001 | ✅ |
| TR-cc2d-001 | concept Tech / R-T1 | CC2D | 自作 Kinematic CC2D + Box2D v3 整合 | ADR-0002 | ✅ |
| TR-cc2d-002 | concept Core Mech 4 | Combo Input | 先行入力バッファ 4-6f / 空中キャンセル / 職業間受け渡し | — | ❌ GAP（ADR-0002 V9 で責務委譲明示） |
| TR-class-001 | concept R-T3 | Class Switch | 切替コスト ~0.4ms（≤0.8ms 予算） | ADR-0001 Performance | ✅ |
| TR-art-001 | concept R-T1 / Visual Identity | Art Pipeline | 案C Hybrid + 統一縁取り 2-3px | — | ❌ GAP |
| TR-save-001 | concept R-T5 | Save Data | schemaVersion + Newtonsoft.Json + .bak + Steam Cloud | — | ❌ GAP |
| TR-addr-001 | concept Tech | Scene & Addressables | Addressables 2.0 + Scene 動的ロード | — | ❌ GAP（ADR-0001/0003 が前提利用） |
| TR-cam-001 | concept Tech | Camera | Cinemachine 3 + 2D Confiner | — | ❌ GAP |
| TR-input-001 | concept Tech | Input | Input System 1.8+ / Action Rebinding / Steam Input | — | ❌ GAP |
| TR-anim-001 | concept Tech | Animation | 2D Skeletal Animation 10.x + PSD Importer + Unityちゃん PSB | ADR-0001（部分利用） | ⚠️ Partial — 採用根拠 ADR 別途必要 |
| TR-render-001 | concept Tech | Rendering | URP 2D Renderer 採用 | ADR-0003（前提利用） | ⚠️ Partial — 採用根拠 ADR 別途必要 |
| TR-engine-001 | concept Tech | Engine | Unity 6.3 LTS pin | — | ❌ GAP |
| TR-arch-A1 | systems-index A1 | Class Abilities | ClassAbilityData / IAbilityExecutor / AbilityContext 三分割 | ADR-0001（枠組み） | ⚠️ Partial — ADR-0004 Class Abilities 詳細待ち |
| TR-arch-A2 | systems-index A2 | CC2D | 双方向結合防止 + 意図ベース API | ADR-0002 | ✅ |
| TR-arch-A3 | systems-index A3 | Game State / Scene Mgr | 責務直交化（Loading 状態二重表現防止） | — | ❌ GAP |
| TR-arch-A4 | systems-index A4 | VFX | Core 層 IVFXPublisher pub/sub サービス | ADR-0003 | ✅ |
| TR-arch-A5 | systems-index A5 | Save Data | ISaveable + section-based JSON | — | ❌ GAP（TR-save-001 と同一 ADR） |
| TR-cd-CD1 | systems-index CD1 | Class Switch / CC2D | Tier 0 minimal feedback（color wash + SE + hitstop） | ADR-0001 + ADR-0002 | ✅ |
| TR-cd-CD2 | systems-index CD2 | Combat | Tier 0 inline hit feedback（dummy + hitstop + knockback） | ADR-0002（API のみ） | ⚠️ Partial — Combat ADR 未作成 |
| TR-cd-CD4 | systems-index CD4 | Class Abilities | Anti-Pillar 監視（アンロックツリー REJECT） | — | N/A — creative review process |
| TR-prod-001 | systems-index Localization | Localization | MVP day 1 から Strings キー参照 | ADR-0001/0002/0003 で個別言及 | ⚠️ Partial — Localization System ADR 未作成 |
| TR-prod-002 | systems-index Production | Save Data | MVP 最小スタブ前倒し | — | ❌ GAP（TR-save-001 と同一 ADR、最優先） |
| TR-perf-001 | technical-preferences | All | 60fps / 16.6ms frame budget | ADR-0001/0002/0003 個別予算 | ⚠️ Partial（横断 ADR は必須ではない） |

---

## Coverage Summary

- **Total requirements**: 26
- ✅ **Covered**: 8（ADR-0001 / ADR-0002 / ADR-0003）
- ⚠️ **Partial**: 9
- ❌ **Gap**: 8（`N/A` 1 件除く）

---

## Coverage Gaps（優先順）

### Foundation 層（MVP 前提、最優先）

1. ❌ **TR-engine-001** — Unity 6.3 LTS 採用根拠 ADR
   - Suggested: `/architecture-decision unity-engine-version-pin`
   - Engine Risk: MEDIUM

2. ⚠️ **TR-render-001** — URP 2D Renderer 採用根拠
   - Suggested: `/architecture-decision urp-2d-renderer-adoption`
   - Engine Risk: MEDIUM

3. ⚠️ **TR-anim-001** — 2D Skeletal Animation 採用根拠
   - Suggested: `/architecture-decision 2d-skeletal-animation-adoption`
   - Engine Risk: MEDIUM-HIGH

4. ❌ **TR-input-001** — Input System Architecture
   - Suggested: `/architecture-decision input-system-architecture`
   - Engine Risk: MEDIUM

5. ❌ **TR-save-001 / TR-arch-A5 / TR-prod-002** — Save Data System
   - Suggested: `/architecture-decision save-data-system`
   - Engine Risk: LOW-MEDIUM
   - **PR-SCOPE で MVP 前倒し対象、Foundation 9 MVP の 1 つ**

### Core 層

6. ❌ **TR-arch-A3** — Game State Machine
   - Suggested: `/architecture-decision game-state-machine`
   - Engine Risk: LOW

7. ❌ **TR-addr-001** — Scene & Addressables Manager
   - Suggested: `/architecture-decision scene-addressables-manager`
   - Engine Risk: MEDIUM

8. ❌ **TR-cam-001** — Camera System
   - Suggested: `/architecture-decision camera-system`
   - Engine Risk: MEDIUM

9. ❌ **TR-cc2d-002** — Combo Input Buffer
   - Suggested: `/architecture-decision combo-input-buffer`
   - Engine Risk: LOW
   - **ADR-0002 V9 が境界仕様をこの ADR に責務委譲済**

### Feature 層

10. ⚠️ **TR-arch-A1** — Class Abilities System 詳細（ADR-0004 として予約済）
    - 既存 3 ADR がすべて Forward Reference として待機

11. ⚠️ **TR-cd-CD2 / TR-pillar-003** — Combat System（ADR-0005 として予約済）

### Polish 層

12. ⚠️ **TR-prod-001** — Localization System
13. ❌ **TR-art-001** — Art Pipeline 案C Hybrid

---

## Verdict: **CONCERNS**

理由：
- ✅ 既存 3 ADR の品質は高く cover 範囲は内部一貫
- ⚠️ Foundation / Core 層に 8 件の完全未着手 ADR ギャップ。特に Save Data（MVP 前倒し対象）/ Input System / Game State Machine は Tier 0 MVP 9 システムに含まれ、story authoring 解禁前に必須
- ⚠️ ADR-0001/0002/0003 すべてが ADR-0004（Class Abilities）の Forward Reference に依存
- 🟢 Pre-production 段階としては正常な進捗。Pillar 1 / Pillar 3 の中核 ADR が先行確定したのは適切な順序

### Blocking Issues（PASS への必須項目）

- **TR-save-001** — Save Data ADR（MVP 前倒し制約）
- **TR-input-001** — Input System ADR（Foundation 起点）
- **TR-arch-A3** — Game State Machine ADR（A3 申し送り）

---

## Required ADRs（推奨着手順）

| 順 | Title | 採番候補 | 理由 |
|---|---|---|---|
| 1 | Save Data System | ADR-0004 | MVP 前倒し対象、7 依存 bottleneck |
| 2 | Input System Architecture | ADR-0005 | Foundation 起点、CC2D / Combo Input が依存 |
| 3 | Game State Machine | ADR-0006 | A3 申し送り、Scene & Addressables 直交化 |
| 4 | Combo Input Buffer | ADR-0007 | ADR-0002 V9 責務委譲済 |
| 5 | Class Abilities System | ADR-0008 | ADR-0001/0002/0003 Forward Reference 解消 |
| 6 | Camera System | ADR-0009 | MVP（単一シーン）に必要 |
| 7+ | Engine Pin / URP 2D / 2D Skeletal Animation 採用根拠 | ADR-0010+ | 既存 3 ADR の前提条件、後追い文書化可 |

---

## Handoff

1. **Immediate actions（最優先 3 件）**:
   - `/architecture-decision save-data-system`
   - `/architecture-decision input-system-architecture`
   - `/architecture-decision game-state-machine`
2. **Gate guidance**: Blocking 3 件解消 + Class Abilities ADR 着手で `/gate-check pre-production` 実行可能
3. **Rerun trigger**: 各 ADR 書込後に `/architecture-review` を再実行してカバレッジ向上を確認
