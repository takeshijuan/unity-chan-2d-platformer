# Active Session State

**Last Updated**: 2026-04-27
**Branch**: feature/unruffled-mendeleev-a70cb4

## Current Task

**Skill**: `/architecture-decision` — **完了（ADR-0001）**
**Status**: ADR-0001 Class Switch Architecture を Proposed (Validation Gate: R5) で書込済、registry 6 件追記済
**Review Mode**: full

### 直前完了タスク（このセッション内）

- `/map-systems` Phase 1-7 完了（systems-index.md 作成、29 システム索引）
- `/architecture-decision` Step 0-7 完了（ADR-0001、Validation Gate R5 待ち）
- `docs/registry/architecture.yaml` 6 件追記（state / interface / budget / api / forbidden ×2）

## Progress Checklist

- [x] Phase 1: ゲームコンセプト読込（`design/gdd/game-concept.md`）
- [x] Phase 2: システム列挙 29 — ユーザ承認
- [x] Phase 3: 依存マッピング → TD-SYSTEM-BOUNDARY: **CONCERNS** → Architecture Notes A1-A5 として記録
- [x] Phase 4: 優先度割当 → PR-SCOPE: **UNREALISTIC** → Option A（Producer 推奨を全面受け入れ）で解消
- [x] Phase 5: systems-index.md 作成 → CD-SYSTEMS: **CONCERNS** → Creative Director Notes CD1-CD4 として記録
- [ ] Phase 7: 次のアクション選択（GDD 着手 / プロトタイプ / 追加レビュー）

## Files Created / Modified

- `design/gdd/systems-index.md` — 新規作成（29 システム、4 階層、3 ゲート結果反映済み）
- `production/session-state/active.md` — 本ファイル

## Key Decisions

### Scope（Option A 全面採用）

- **MVP**: 9 システム / 6 週間（concept.md の元 15 システム / 3-4 週から縮小）
- **VS**: 21 システム / 5-6 ヶ月（cumulative）
- **Alpha**: 29 システム / 8-10 ヶ月 → 12-15 ヶ月（cumulative）
- **Full Vision**: 29 システム + コンテンツ拡張のみ / 24-30 ヶ月（cumulative）
- **Save Data System** は MVP に前倒し（最小 stub）
- **Localization 規律** は MVP day 1 から（コードに生文字列禁止）

### Architecture（GDD 作成時に必須反映）

- A1: Class Abilities = `ClassAbilityData`（SO） + `IAbilityExecutor`（MB） + `AbilityContext`（注入）→ ADR-004
- A2: CharacterController2D は `ICharacterMotor` interface のみ expose → ADR-006
- A3: Game State Machine と Scene & Addressables Manager の責務直交化
- A4: VFX System は Core 層、`IVFXPublisher` 提供 → ADR-008（新設候補）
- A5: Save Data System は `ISaveable` + section-based JSON で論理分割

### Pillar 整合（GDD 作成時に必須反映）

- CD1: Class Switch GDD に Tier 0 ミニマル feedback セクション
- CD2: Combat GDD に Tier 0 inline hit feedback セクション
- CD3: Tier 1 Go/Pivot/Stop ゲートに Pillar 2 / Pillar 4 design test を追加
- CD4: Class Abilities GDD で「アビリティツリー / 強化スロット」が提案されたら CD として REJECT

## Open Questions

- 4 職目の選択（Tier 2b コミュニティフィードバック後 — 既存 Open Question Q1）
- UCL 2.0 衣装改変可否（Unity Japan 照会 — 既存 Open Question Q2）

## Next Action（次セッション選択肢）

### 優先 ADR（残り 2 件）

1. **ADR-0002 CharacterController2D + ICharacterMotor**（高優先）
   - Architecture Note A2 を確定
   - ADR-0001 Forward References の `ICharacterMotor` 契約を埋める
   - Class Abilities GDD・Combat GDD authoring の前提

2. **ADR-0003 VFX System Boundary + IVFXPublisher**（中優先）
   - Architecture Note A4 を確定
   - Tier 1 で ClassStateMachine の minimal feedback がリファクタされる移行先
   - VFX System GDD authoring の前提

### Validation Gate R5 検証プロトタイプ（ADR-0001 を Accepted に昇格させるため必要）

- 範囲: Unityちゃん公式 PSB 1 体 + SpriteSkin + SpriteLibrary swap 最小テスト（≈30 行）
- 通過条件 5 項目（ADR-0001 Validation Gate セクション参照）
- 担当・成果物保管: `production/qa/evidence/`

### GDD Authoring 開始可能タイミング

- Class Switch System GDD: ADR-0001 Accepted 後
- Class Abilities System GDD: ADR-0001 Accepted + ADR-0004 起草 後
- CharacterController2D System GDD: ADR-0002 Accepted 後
- Combat System GDD: ADR-0002 + ADR-0004 Accepted 後

### technical-preferences.md ADR Log 同期タスク（持ち越し）

- ADR-0001 Accepted 後、`technical-preferences.md` の "Architecture Decisions Log" を更新（実ファイル番号への同期）

## Session Extract — /architecture-review 2026-04-27

- Verdict: 🟡 CONCERNS
- Requirements: 39 total — 14 covered (36%), 5 partial (13%), 20 gaps (51%)
- New TR-IDs registered: 25（MVP 9 systems 分のみ）
- GDD revision flags: design/gdd/game-concept.md（Unity version line 236, R-T3 line 333）
- Top ADR gaps: ADR-0003 (VFX System Boundary), ADR-0004 (Class Abilities), ADR-0005 (Save Data System)
- unity-specialist anti-patterns: AP-1 ColorWashCoroutine 多重起動ガード (Medium), AP-3 Debug.Assert Release Build 評価 (Medium), AP-4/AP-5 Low
- ADR-0001 Summary CONCERN-1: line 27 hitstop 記述要修正（State Ownership ambiguity）→ **2026-04-27 同セッションで適用済**
- Report: docs/architecture/architecture-review-2026-04-27.md
- Traceability index: docs/architecture/traceability-index.md
- TR Registry updated: docs/architecture/tr-registry.yaml (version 2)

### Patches applied (2026-04-27 same session, D-H)

- **D**: ADR-0001 Summary line 27 — `（color wash + SE）` 化 + Hitstop は ADR-0002 ICharacterMotor.ApplyHitstop 経由を明記（CONCERN-1 解消）
- **E**: ADR-0001 Implementation Guidelines #8 追加 — ColorWashCoroutine 多重起動ガード必須化（AP-1）
- **F**: ADR-0002 Implementation Guidelines #14 追加 — Cast 結果は `_castResults[0]` 暗黙仮定禁止、明示的に最小 distance 選択（Q5 補強）
- **G**: ADR-0002 Validation Criteria — Release Build Profiler `Debug.Assert` 計上確認 checkbox 追加（AP-3）
- **H**: game-concept.md 3 箇所 revision — line 236 "Unity 6 LTS (6000.0.x)" → "Unity 6.3 LTS (6000.3.x)"、line 245 / line 333 R-T3 で "~0.4ms" → "0.7-0.8ms（ADR-0001 Performance Implications 参照）"

### Outstanding tasks（次セッション）

- ADR-0001 R5 spike + ADR-0002 V1-V5 spike 合体プロトタイプ実施（両 ADR Accepted 化の必須条件）
- ADR-0003 (VFX System Boundary) / ADR-0004 (Class Abilities) / ADR-0005 (Save Data) 起草（pre-production gate に向けて High priority）
- 任意の AP-2 / AP-4 / AP-5（Low severity、Tier 1 リファクタ計画への取り込み）
