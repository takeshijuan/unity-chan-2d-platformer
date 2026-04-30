# ADR-0006a: Camera System — R1 Spike Findings (Follow-up to ADR-0006)

## Status

**Accepted**

> 本 ADR は [ADR-0006 Camera System](adr-0006-camera-system.md) の C0 gate (R1 spike) で得られた empirical findings を反映する **follow-up ADR**。ADR-0006 の 3 Locked Decisions は維持しつつ、Cinemachine 3.1.6 実機検証で確定した API 命名 / 採用方針 / C1 protocol 拡張を本 ADR で lock する。ADR-0006 自体の Deferred Decisions D1-D11 のうち本 ADR で touch するのは **D8 関連 (CinemachinePixelPerfect 採用方針)** と **D2 関連 (Look Ahead と Pillar 2 接続)** のみ。残り 9 件は引き続き defer。

## Date

2026-04-30

## Last Verified

2026-04-30 — R1 spike Editor 検証完了 (Unity 6.3 LTS 6000.3.13f1 + Cinemachine 3.1.6 + URP 17.0.3)

## Decision Makers

- takeshi (project lead, Editor verification)
- Claude Code (autoplan + ADR drafting)
- Creative Director gate (CD-PLAYTEST: CONCERNS accepted 2026-04-30)

## Summary

R1 spike (C0 PASS 18/18 weighted score) で 12 verification items 全 OK + 4 件の **discovered findings**（事前 Context7 推測の補正含む）を確定。Cinemachine 3 の API 命名 4 件、CinemachinePixelPerfect 採用方針、`CinemachineBrain` execution order 制御方式、C1 protocol 拡張（動的追従 + render-space pixel snap）を本 ADR で lock。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine version** | Unity 6.3 LTS (6000.3.13f1) — verified 2026-04-30 |
| **Cinemachine package** | `com.unity.cinemachine` 3.1.6 — verified 2026-04-30 |
| **URP package** | `com.unity.render-pipelines.universal` 17.0.3 (PixelPerfectCamera bundled) |
| **Knowledge Risk** | **LOW** (R1 spike empirical data 取得済) |
| **Verification Required** | C1 (動的追従 + pixel snap) — ADR-0002 V1 通過後 |

> **Note**: R1 spike で `CinemachinePixelPerfect` の事前 Context7 推測 (MEDIUM confidence "empty stub") は **誤り** と判明。本 ADR の Decision 2 で採用方針確定。

## ADR Dependencies

| Relation | ADR | Reason |
|----------|-----|--------|
| **Follow-up to** | [ADR-0006](adr-0006-camera-system.md) | C0 gate R1 spike findings 反映、ADR-0006 3 Locked Decisions は維持 |
| **Depends On** | [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md) | C1 protocol（動的追従検証）は ADR-0002 V1 通過後に実施 |
| **Cross-checks** | [ADR-0003 VFX System Boundary](adr-0003-vfx-system-boundary.md) | Color Wash overlay (CinemachineBrain 直下 Animated Quad) と Body Damping の z-order 干渉検証は C1 scope に追加 (CD-PLAYTEST CONCERN B) |

## Context

### R1 Spike Result Summary

ADR-0006 C0 gate (R1 spike completion) を Unity 6.3 LTS Editor で実施。12 verification items 全 OK、weighted score **18/18** で PASS。Evidence: [`production/qa/evidence/r1-camera-cinemachine3-spike-result.md`](../../production/qa/evidence/r1-camera-cinemachine3-spike-result.md). Spike artifacts: [`prototypes/camera-cinemachine3-r1-spike/`](../../prototypes/camera-cinemachine3-r1-spike/).

### Findings Requiring Lock

R1 spike で得られた発見のうち、本 follow-up ADR で lock すべき項目:

1. **CinemachinePixelPerfect は functional** — 事前検証 MEDIUM confidence の "empty stub" 推測は **誤り**。`declaredMethods=3` で実装あり。`[AddComponentMenu("")]` で Inspector "Add Component" メニュー隠しのみ。**Plan B（PixelPerfectCamera 単体運用）は不要**
2. **API 命名 4 件確定**:
   - `CinemachineImpulseListener.UseCameraSpace` — 旧 `UseSignalSpaceOnly` から RENAMED 確定 (#7)
   - `CinemachineConfiner2D.BoundingShape2D` — **field** 公開（property ではない）、型 `Collider2D` (#5)
   - `CinemachineImpulseSource` — 7 GenerateImpulse overloads (3 legacy + 3 new naming + 1 no-arg) (#6)
   - `CinemachineBrain.UpdateMethod` — **field** 公開（property ではない）、enum 順 `FixedUpdate=0, LateUpdate=1, SmartUpdate=2, ManualUpdate=3` (#2)
3. **`CinemachineBrain` に `[DefaultExecutionOrder]` 属性なし** — Cinemachine 3 は execution timing を `UpdateMethod` enum + `[ExecuteAlways]` で制御。属性ベースの順序固定は不要 (#11)
4. **CD-PLAYTEST CONCERN 反映**:
   - **A (Plan B ピクセルスナップ・ジッター視覚確認)**: **dissolved** — CONCERN A は「Plan B 構成での」視覚確認を要求していたが、Plan B 自体が不要 (CinemachinePixelPerfect functional) と判明。視覚的 pixel snap jitter は **render-space** の話で R1 spike scope 外。本件は新規 C1-C protocol（render-space pixel snap jitter）で別アプローチで検証する
   - **B (Look Ahead と Pillar 2 接続)**: Decision 5 で D2 deferred decision の defer 理由に Pillar 2 Design Test 接続を追記
   - **C (#9 stutter のプレイヤー体験基準)**: C1-B protocol に「連続 2 フレーム 16.6ms 超過 = 0 件」基準を追加。**重要**: R1 spike Phase B の `stutterFrames=0/120` は static target での Y-position-delta ベース計測で、**dynamic stutter は実測していない**。C1-B が真の dynamic stutter 検証となる

### Phase B 検証の限界

R1 spike Phase B は **static-state 検証** のみ。FollowTarget が静止していたため `followDelta=0.0000` は「同期している」ではなく「両方静止していた」を示す。動的追従中の stutter / 1-frame sync / render-space pixel snap jitter は本 ADR の C1 protocol で検証する。

## Decision

### Locked Decision 1 — Cinemachine 3 API 命名確定

R1 spike (Phase A reflection check) で確定した API 命名を本 ADR で lock。今後の実装コード / 設計ドキュメントは以下の名前を使用:

| API | 確定形 | 公開形式 | Source |
|-----|--------|----------|--------|
| `Unity.Cinemachine.CinemachineCamera` | sealed class | type | R1 #1 |
| `CinemachineCamera.Follow` | `Transform` | property | R1 #3 |
| `CinemachinePositionComposer` | Body component | type | R1 #4 |
| `CinemachineConfiner2D.BoundingShape2D` | `Collider2D` | **field** (NOT property) | R1 #5 |
| `CinemachineConfiner2D.InvalidateBoundingShapeCache()` | parameterless | method | R1 #5 |
| `CinemachineImpulseSource.GenerateImpulse(...)` | 7 overloads | methods | R1 #6 |
| `CinemachineImpulseListener.UseCameraSpace` | `bool` | property | R1 #7 (RENAMED from `UseSignalSpaceOnly`) |
| `CinemachineImpulseListener.Gain / Use2DDistance / ChannelMask` | properties | properties | R1 #7 |
| `CinemachineBrain.UpdateMethod` | `UpdateMethods` enum | **field** (NOT property) | R1 #2 |
| `CinemachineBrain.OutputCamera` | `Camera` | property | R1 #12 |

**Forbidden**:
- `UseSignalSpaceOnly` プロパティ参照 — 旧 Cinemachine 2.x 命名、3.x で削除済
- `CinemachineConfiner2D.BoundingShape2D` を property 経由でアクセスしようとする呼出（field 直接代入のみ）
- `CinemachineBrain.UpdateMethod` を property 経由で reflection 取得しようとする呼出（field 経由 / `m_UpdateMethod` private field 経由 / `GetField("UpdateMethod")` 経由のみ）

### Locked Decision 2 — CinemachinePixelPerfect 採用方針

`CinemachinePixelPerfect` extension は **functional** と確認、採用する。ただし `[AddComponentMenu("")]` で Inspector "Add Component" メニューから隠蔽されているため、以下の追加経路のみ使用:

- **コード経由**: `gameObject.AddComponent<Unity.Cinemachine.CinemachinePixelPerfect>()` で動的追加
- **プレハブ経由**: 既存 prefab に手動でドラッグ追加 → prefab 化して再利用

**Forbidden**:
- Inspector "Add Component" メニューから手動追加を期待する設計 / ドキュメント / レビュー要求（メニューに出ない）

**Plan B 廃棄**: ADR-0006 起草時に検討された「PixelPerfectCamera 単体で `cropFrameX/Y` 直接制御する Plan B」は **採用しない**（事前検証の誤りに基づく代替案だったため）。

### Locked Decision 3 — CinemachineBrain Execution Timing

`CinemachineBrain.UpdateMethod = SmartUpdate` (Cinemachine 3 default) を採用、本 ADR で固定する。

- **理由 1**: SmartUpdate は physics object に対して FixedUpdate、static target に対して LateUpdate を自動選択。本作の camera target は ICharacterMotor.Position（MovePosition + SyncTransforms 後の値）で、physics 経路を通る後の値なので SmartUpdate の自動選択が適合
- **理由 2**: `CinemachineBrain` には `[DefaultExecutionOrder]` 属性が **付与されていない**（R1 #11 確認）。属性ベースの execution order 固定は不要、UpdateMethod enum で十分
- **理由 3**: ADR-0002 V1 の Physics2D.SyncTransforms() 検証結果に応じて UpdateMethod を変更する余地を C1 で残す（C1 PASS = SmartUpdate 維持、FAIL = LateUpdate fallback）

### Decision 4 — C1 Validation Gate Protocol 拡張

ADR-0006 の C1 (Provisional Follow Basic) を以下に拡張、ADR-0002 V1 通過後に実施:

#### C1-A: Dynamic Follow Sync（既存）
- ICharacterMotor.Position drives camera follow within 1 render frame
- 30/60/120Hz render × 50Hz physics matrix で sync 検証
- `Time.timeScale=0.1` slow-mo + hitstop+dash 組合せで 1-frame divergence ≤ 0.005 unit p99 (PlayMode test)

#### C1-B: Dynamic Stutter（**追加**、CD CONCERN C 反映）
- FollowTarget を sin curve / linear motion で連続 600 フレーム移動
- Stutter 定義: **連続 2 フレーム以上の dt > 16.6ms (60fps frame budget) 超過 = 0 件**
- `followDelta` の p99 ≤ 0.005 unit、stutter event > 0 件で C1 FAIL
- Macbook (Editor) と Steam Deck native（C2 で対応）両方で計測

#### C1-C: Render-Space Pixel Snap Jitter（**追加**、CD CONCERN C 反映）
- PixelPerfectCamera + CinemachinePixelPerfect 統合下でキャラクター走行中のサブピクセル・ジッターを Frame Debugger / RenderDoc キャプチャで観察
- 連続 120 フレームで pixel-snap discontinuity > 1px 発生件数 = 0 件で PASS
- Pillar 3「可愛い」視覚基盤の前提検証

#### C1-D: Color Wash z-order Cross-check（**追加**、CD CONCERN B 関連、ADR-0003 連携）
- CinemachineBrain 直下 Animated Quad (ADR-0003 Color Wash) と Cinemachine Body Damping 適用中の z-order 順序確認
- Color Wash overlay が Damping 中の camera transform 確定前に rendering されないこと（順序逆転で flicker 発生リスク）
- ADR-0003 G1-G5 と並走で実施

### Decision 5 — D2 Deferred Decision の defer 理由補強（CD CONCERN B 反映）

ADR-0006 D2 (Damping X/Y 値) の defer 理由に **Pillar 2「一歩ごとに、次の鍵が見える」Design Test との接続** を明記する。

> **D2 拡張 defer 理由**: Damping X/Y 値の選定は Look Ahead 機能（ADR-0006 D2 / future Look Ahead deferred decision）と一体で評価する必要がある。Pillar 2 Design Test「1 時間プレイして次の目標が思い浮かばない瞬間があれば失格」をクリアするには、メトロイドヴァニアにおいてプレイヤーがダッシュ方向の先にある環境ヒント（ゲートの色、隙間の光、遠景の構造物）を「ちらりと見る」瞬間が必要。カメラが完全にキャラクター中心で先読みなしの場合、Pillar 2 Design Test に引っかかるリスクがある。Damping 値選定 + Look Ahead 仕様策定は class-switch prototype (ADR-0001 R5 通過後) で Pillar 2 Design Test の **直接的な入力** として実施する。

### NOT in scope（本 ADR で扱わない）

- ADR-0006 Deferred Decisions D1, D3, D4, D5, D6, D7, D8 (Crop Frame setup), D9, D10, D11 — 引き続き defer
- Cinemachine 3 → 4.x マイグレーション — 4.x release が明らかになった時点で別 ADR
- 動的追従の **実機計測** — C1 で実施（本 ADR は protocol 定義のみ）

## Alternatives Considered

### Alternative 1: ADR-0006 を直接編集して findings を反映

- **Description**: Status を Accepted に変更し、本 ADR の Decision 1-5 を ADR-0006 内に追記
- **Pros**: ファイル数が増えない、参照が一箇所に集約
- **Cons**: ADR-0006 の "Provisional" → "Final" の歴史的経緯が消える、Decision の追加と同時に Deferred Decisions の状態変化が混在し可読性低下、別 follow-up が必要になった時 (ADR-0006b) の incremental authoring が辛い
- **Why rejected**: ADR は immutable な意思決定記録であるべき。R1 spike の発見は ADR-0006 の前提を補強するもので、ADR-0006 の Decisions 自体は変更不要。**follow-up ADR が ADR pattern として正しい**

### Alternative 2: findings を ADR-0006 の更新 + 個別 deferred decision の lock として分散

- **Description**: API 命名は engine-reference に、CinemachinePixelPerfect は D8 を解除して D8 lock、execution timing は D11 (forbidden patterns) に追加、C1 protocol 拡張は ADR-0006 の Validation Gates セクション直接編集
- **Pros**: 各情報を最も近い場所に配置
- **Cons**: 5 ファイル横断編集で trace 困難、CD CONCERN A/B/C の反映が散逸、後で「R1 spike findings は何だったか」が再構築不能
- **Why rejected**: traceability が壊れる。R1 spike → 本 ADR → ADR-0006 / engine-reference / registry へ propagate という chain を維持

### Alternative 3 (採択): Single follow-up ADR (ADR-0006a) で全 findings を lock

- **Description**: 本 ADR
- **Pros**: R1 spike findings の単一参照点、CD CONCERN 3 件の対応が一箇所で確認可能、ADR-0006 + ADR-0006a の組合せで完全な camera system architecture を表現
- **Cons**: ADR ファイル数 +1
- **Why selected**: ADR pattern の正道、traceability 保持、incremental authoring 容易（ADR-0006b が必要になっても増分追加可能）

## Consequences

### Positive

- **API 命名の不確実性除去**: 4 件の API 名 / 公開形式 lock により、今後のコード実装で reflection / property/field 取り違えバグ予防
- **CinemachinePixelPerfect 採用方針確定**: Plan B 廃棄で D8 関連の決定空間が縮小、Pixel Perfect 統合戦略が単純化
- **C1 protocol 完成度向上**: CD CONCERN A/B/C 全反映で player experience 観点が技術検証に組込まれた
- **ADR-0006 Provisional → Provisional + R1 PASS 状態の明示**: 本 ADR が ADR-0006 の補強であることが履歴上明確
- **Follow-up ADR pattern の確立**: 将来の R5 / V1-V5 spike findings も同形式で incremental に追加可能

### Negative

- **ADR ファイル数 +1**: camera system 関連 ADR が 2 ファイルに分割（ただし ADR pattern では正しい運用）
- **C1 protocol 拡張で C1 工数増**: C1-B/C/D 追加で C1 検証時間が概算 2-3h → 5-7h に増加（ただし ADR-0006 Provisional のまま long-term 運用するリスクと比較すれば許容）

### Risks

- **R1**: C1 protocol 実施時に SmartUpdate UpdateMethod が ADR-0002 V1 結果と整合しない（例: Physics2D.SyncTransforms タイミング issue）→ Decision 3 を変更し LateUpdate 採用 → 本 ADR の partial Superseded。Mitigation: C1-A で UpdateMethod の最終 lock を実施、本 ADR の Decision 3 は "default 推奨" 扱い
- **R2**: Cinemachine 3.1.6 → 3.2.x 以降のマイナーアップデートで Decision 1 の API 名 / 公開形式が変更（特に `UpdateMethod` field → property 化など）→ engine-reference / 本 ADR 部分更新。Mitigation: Cinemachine 更新時の `tools/ci/unity-compile-check.sh` で reflection-based 検証を再走させる定期メンテプロトコル（年 1 回 or major Unity update 時）
- **R3**: `CinemachinePixelPerfect` の hidden component menu 仕様が将来 Unity アップデートで普通の menu component に戻る → 採用方針 (Decision 2) の "コード/プレハブ経由のみ" は overconstraint になり緩和必要。Mitigation: 影響軽微 (Inspector メニュー追加経路を許可するだけ)、3.x major release 時の point of review

## Performance Implications

- 本 ADR は protocol / API 命名の確定のみ、ランタイム性能には直接影響なし
- C1-B (Dynamic Stutter) で Steam Deck 1280×800 native の baseline measurement を取得 → ADR-0006 D10 (Performance budgets) の数値根拠として転用予定

## Migration Plan

### 即時反映 (Tier 0 MVP 進行中)

1. **engine-reference 更新** (本 ADR Decision 1 と連動):
   - `docs/engine-reference/unity/plugins/cinemachine.md` 既存ファイルに **R1 Spike Findings (2026-04-30)** セクションを追記 + 古い記述で誤りのある箇所 (FramingTransposer 等の旧名) を修正
   - `docs/engine-reference/unity/deprecated-apis.md` 追記 — `UseSignalSpaceOnly`, `CinemachineVirtualCamera`, `CinemachineFramingTransposer`, `CinemachineBrain.m_UpdateMethod` (private serialized field) など Cinemachine 2.x legacy API
2. **registry 更新** (`docs/registry/architecture.yaml`):
   - api_decisions に `cinemachine_camera_class`, `cinemachine_brain_update_method`, `cinemachine_position_composer`, `cinemachine_confiner2d_bounding_shape`, `cinemachine_impulse_listener_use_camera_space`, `cinemachine_pixel_perfect_extension`, `cinemachine_brain_output_camera` を append
   - forbidden_patterns に `cinemachine_use_signal_space_only_property_reference`, `cinemachine_pixel_perfect_inspector_menu_expectation` を append
3. **ADR-0006 Status 更新**: 完了済 (Status を Accepted (Provisional, C0 PASS) に変更)

### Tier 0 → Tier 1 移行時 (ADR-0002 V1 通過後)

4. **C1 protocol 実施**: C1-A/B/C/D を順次実行、結果を `production/qa/evidence/c1-camera-validation-result.md` に記録
5. **C1 PASS 後**: ADR-0006 の Status を最終的に `Accepted (Final)` に昇格、Deferred Decisions を順次 lock する follow-up ADR (ADR-0006b 以降) を起草

## Validation Criteria

- [x] R1 spike C0 PASS (18/18) 達成 — 本 ADR の前提
- [x] engine-reference/unity/plugins/cinemachine.md に R1 Spike Findings セクション追記（2026-04-30）
- [x] engine-reference/unity/deprecated-apis.md に Cinemachine 2.x → 3.x mapping 9 件確定（2026-04-30）
- [x] registry/architecture.yaml に Decision 1 関連 api_decisions 8 件 + forbidden_patterns 3 件 追記（2026-04-30）
- [ ] ADR-0002 V1 通過後、C1-A/B/C/D 実施完了
- [ ] C1 結果に応じて Decision 3 (UpdateMethod) の最終 lock 確認

## GDD Requirements Addressed

| TR-ID | Requirement | How addressed |
|-------|-------------|---------------|
| (TR-CAM-001 to be assigned by `/architecture-review`) | カメラは 1 ボタン即時切替時に追従する (Pillar 1 支援) | Decision 3 で UpdateMethod = SmartUpdate を default lock、ICharacterMotor.Position follow と整合 |
| (TR-CAM-002 to be assigned) | 環境ヒントが画面内に表示される (Pillar 2 支援) | Decision 5 で D2 Look Ahead と Pillar 2 Design Test の接続を明記、C1 protocol で検証準備 |
| (TR-CAM-003 to be assigned) | ピクセルアートが安定して描画される (Pillar 3 支援) | Decision 2 で CinemachinePixelPerfect 採用確定、C1-C で render-space pixel snap jitter 検証 |

## Related

- [ADR-0006 Camera System](adr-0006-camera-system.md) — Parent ADR
- [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md) — V1 通過後に C1 連動
- [ADR-0003 VFX System Boundary](adr-0003-vfx-system-boundary.md) — C1-D で Color Wash z-order 連携
- R1 Spike: [`prototypes/camera-cinemachine3-r1-spike/`](../../prototypes/camera-cinemachine3-r1-spike/)
- R1 Evidence: [`production/qa/evidence/r1-camera-cinemachine3-spike-result.md`](../../production/qa/evidence/r1-camera-cinemachine3-spike-result.md)
- R1 Report: [`prototypes/camera-cinemachine3-r1-spike/REPORT.md`](../../prototypes/camera-cinemachine3-r1-spike/REPORT.md)
