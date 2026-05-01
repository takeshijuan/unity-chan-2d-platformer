## Prototype Report: Camera Cinemachine 3 R1 Spike

### Hypothesis

Cinemachine 3.x（com.unity.cinemachine, Unity 6.3 LTS 同梱版）の 12 API 項目は
ADR-0006 Camera System の仮定と一致し、3 Locked Decisions の前提が成立する。

### Approach

- Context7 + unity-specialist による事前ドキュメントリサーチで 8/12 項目を pre-verify（HIGH confidence）
- `R1CinemachineSpike.cs`（EditorWindow + MonoBehaviour 統合）で全 12 項目を自動検証
  - EditorWindow: reflection ベース API checks (#1-8, #10, #12) — Play Mode 不要
  - MonoBehaviour: runtime stutter detection (#9) + execution order recording (#11)
- 16 件の eng review fix を適用済み（deprecated API, null guard, contract verification, pipe escaping 等）
- 重み付けスコアリング導入: Critical (#8,#9,#11) = 3pt, Standard = 1pt, C0 PASS = 14/18+
- 工数: 事前準備 ~2h（Claude Code）、Editor 検証 ~1-2h（ユーザ見込み）

### Result

**Status: COMPLETED — C0 PASS (18/18)** ✅

Unity 6.3 LTS (6000.3.13f1) + Cinemachine 3.1.6 + URP 17.0.3 で Phase A (API Check) + Phase B (Runtime) 完了。

検証結果:
- **Phase A**: 10/10 OK (FAIL/RENAMED unverified なし)
- **Phase B Runtime**: stutterFrames=0/120, maxFollowDelta=0.0000, PASS=True
- **Critical 3 件**: #8 PixelPerfect = functional (NOT empty stub), #9 Triple Integration = PASS, #11 Execution Order = PASS

事前検証の修正:
1. **CinemachinePixelPerfect は functional だった** — declaredMethods=3、`[AddComponentMenu("")]` で Inspector メニュー隠蔽のみ。**Plan B 不要**（事前検証 MEDIUM confidence は誤り）
2. **UseSignalSpaceOnly → UseCameraSpace** — RENAMED 確定（HIGH confidence 通り）
3. **BoundingShape2D は field** — confirmed
4. **CinemachineBrain に `[DefaultExecutionOrder]` 属性なし** — CM3 は UpdateMethod enum + ExecuteAlways で timing 制御

Phase B の限界:
- FollowTarget が静止していたため、**static-state 同期検証**のみ。動的追従中の stutter は C1 (ADR-0002 V1 通過後) で検証要

### Metrics

- API 検証通過率: 8/12 pre-verified (HIGH), 2/12 partial, 2/12 runtime-only
- 重み付けスコア（事前推定）: 15-18/18（C0 PASS 見込み）
- Stutter frames: 未計測（Editor 実行待ち）
- Max follow delta: 未計測（Editor 実行待ち）
- Editor 検証所要時間: 未計測

### Recommendation: PROCEED — ADR-0006 Accepted 昇格

> **Creative Director Review (CD-PLAYTEST)**: CONCERNS (accepted) 2026-04-30
> **Editor verification**: C0 PASS 18/18 (2026-04-30)

12/12 OK、Critical 3 件すべて PASS。C0 gate 完全通過。ADR-0006 を Proposed (Provisional)
から **Accepted** に昇格可能。発見事項を反映した follow-up ADR (ADR-0006a) で:
- D8 (PixelPerfect): Plan B 不要、CinemachinePixelPerfect 採用方針を確定
- D2 (Look Ahead): Pillar 2 Design Test との接続を明記 (CD CONCERN B)
- C1 protocol: 動的追従テスト + render-space pixel snap 検証を追加

**CD CONCERNS (3 件、いずれも non-blocking):**
1. Plan B 構成でのピクセルスナップ・ジッター視覚確認を Editor 検証時に追加（Pillar 3 支援）
2. ADR-0006a で Deferred Decision D2（Look Ahead）に Pillar 2 Design Test との接続を明記
3. #9 stutter の pass/fail にプレイヤー体験基準（連続 2 フレーム以上の 16.6ms 超過 = 0 件）を追加

### If Proceeding

- ADR-0006 の RENAMED 項目を反映（UseSignalSpaceOnly → UseCameraSpace 等）
- CinemachinePixelPerfect が stub の場合、D8 deferred decision に Plan B を記録
- engine-reference/unity/plugins/cinemachine.md に R1 Spike Findings 追記済
- deprecated-apis.md に Cinemachine 2.x legacy API を追記
- C1 gate（ADR-0002 V1 通過後）で follow sync + pixel snap の完全検証
- ADR-0006a: D2 (Look Ahead) defer 理由に Pillar 2 Design Test との接続を追記（CD CONCERN B）
- ADR-0006a: Color Wash overlay と Cinemachine Body Damping の z-order 干渉検証を C1 検証リストに追加

### If Pivoting

- Cinemachine 3.x の FAIL 項目に基づき代替 API 経路を設計
- ADR-0006 を partial Superseded にし、FAIL 項目のみ再起草

### If Killing

- 3+ items FAIL の場合のみ。自作 camera controller を検討
- （Context7 事前検証からは発生確率 LOW）

### Lessons Learned

- Context7 による事前 API ドキュメント検証が有効。12 項目中 8 項目を Editor 前に HIGH confidence で確認でき、spike の焦点を残り 4 項目（#8 stub, #9 stutter, #10 props, #11 order）に絞れた
- Cinemachine 2.x → 3.x のリネームは広範囲。LLM 訓練データは 2.x が主体のため、engine-reference に 3.x mapping を記録する価値が高い
- autoplan dual voice review で 7-file → 3-file にスコープ削減。R5 spike パターン準拠が throwaway spike の適正サイズ
