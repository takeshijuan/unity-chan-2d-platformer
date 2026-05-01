# R1 Camera Cinemachine 3 Spike

ADR-0006「Camera System」の Validation Gate **C0** を実機検証する throw-away spike。

## 目的

`docs/architecture/adr-0006-camera-system.md` の R1 Spike Protocol 12 verification items を、
Unity 6.3 LTS macOS Editor で実測する。Cinemachine 3.x API の命名・存在・型を確認し、
ADR-0006 の 3 Locked Decisions の前提が成立するかを判定する。

## 通過条件（ADR-0006 C0 gate）

12 verification items すべてが OK / RENAMED / 不在 / 代替案 のいずれかで結論済。

**重み付けスコアリング:**
- Critical items (#8 PixelPerfect, #9 Triple Integration, #11 Execution Order): 各 3 点
- Standard items (#1-7, #10, #12): 各 1 点
- 合計: 最大 18 点。**C0 PASS = 14/18 以上** かつ **Critical 全 3 件が FAIL でない**

## セットアップ

### 完了済み（Claude Code 側、2026-04-30）

- [x] **Step 1**: Unity 6.3 LTS (6000.3.13f1) プロジェクト本体を取込（origin/main fast-forward）
- [x] **Step 2**: `com.unity.cinemachine` 3.1.6 + `com.unity.render-pipelines.universal` 17.0.3 (PixelPerfectCamera 同梱) インストール済み
- [x] **Step 3**: `R1CinemachineSpike.cs` を `Assets/_R1Spike/Scripts/` に配置（**Editor/ サブフォルダではない** — MonoBehaviour 部が runtime で必要なため）
- [x] **Step 4**: `tools/ci/unity-compile-check.sh` で **PASS** 確認済み

### ユーザ側（Unity Editor 操作）

5. **Unity Hub** で本リポジトリ worktree (`/Users/takeshi/projects/unity-chan-2d-platformer/.claude/worktrees/zen-dirac-0fab62`) を開く
   - 初回は Library/ 生成で 5-15 分の import を待つ
   - 新規追加された `_R1Spike/` の .meta ファイルが自動生成される

6. **Phase A — API Check (Play Mode 不要)**:
   1. メニュー → Window → R1 Spike → Cinemachine 3 API Check
   2. 「Run All Checks」ボタン押下 → 10 項目の結果が EditorWindow に表示
   3. 「Copy to Clipboard」→ `production/qa/evidence/r1-camera-cinemachine3-spike-result.md` の "Verification Results" 表に貼付

7. **Phase B — Runtime Check (Play Mode 必須)**:
   `Assets/_R1Spike/Scenes/R1CameraSpike.unity` を新規作成し、以下 5 GameObject を配置:

   | GameObject | Component / 設定 |
   |---|---|
   | Main Camera | Camera (既定) + CinemachineBrain (既定) + PixelPerfectCamera (Assets PPU=100, RefRes=384×216) |
   | CinemachineCamera | CinemachineCamera (Follow=FollowTarget) + Body=CinemachinePositionComposer (既定) + CinemachineConfiner2D (BoundingShape2D=Confiner) + CinemachineImpulseListener (既定) |
   | FollowTarget | 空 GameObject @ (0,0,0) |
   | Confiner | PolygonCollider2D (IsTrigger=true, 矩形 -20,-10 to 20,10) |
   | ImpulseSource | CinemachineImpulseSource (既定) + **R1CinemachineSpike** (Inspector: `_brain`=Main Camera, `_followTarget`=FollowTarget Transform, `_sampleFrames`=120) |

   1. Play Mode 進入
   2. Console に `[R1]` プレフィックスのログが 120 フレーム出力される
   3. Scene View で FollowTarget をドラッグ → camera 追従を観察
   4. Console 出力をコピー → evidence file の "Editor-Only Verification Details" セクションに貼付
   5. 5 つの Inspector スクリーンショットを撮影 (CinemachineCamera / CinemachineBrain / PixelPerfectCamera / CinemachineConfiner2D / EditorWindow output)

8. **Phase C — Issue Tracker 検索** (Unity Issue Tracker):
   - Query 1: `"Cinemachine 3" "Pixel Perfect"`
   - Query 2: `"CinemachineConfiner2D" "URP 2D"`
   - Query 3: `"CinemachineBrain" "UpdateMethod"`
   - 結果を evidence file の "Unity Issue Tracker Search Log" に記録

9. **C0 gate 判定**:
   - 重み付きスコア計算: Critical (#8,#9,#11) × 3pt + Standard × 1pt = max 18
   - **PASS = 14/18+ かつ Critical 全 FAIL でない**
   - PASS なら ADR-0006 を Accepted 昇格、FAIL なら部分 Superseded

## PixelPerfect Plan B

CinemachinePixelPerfect が empty stub (Context7 事前検証: [AddComponentMenu("")]) の場合:
- **Plan B**: CinemachinePixelPerfect を使わず、PixelPerfectCamera コンポーネント単体で
  Cinemachine 3 との統合を検証。`PixelPerfectCamera.cropFrameX/Y` を直接制御して
  pixel snap を実現する。
- **Impact**: ADR-0006 D8 deferred decision で CinemachinePixelPerfect 非使用を記録。
  Camera update 後に PixelPerfectCamera が snap 補正を行う順序依存の可能性を C1 で検証。

## Item #9/#11 の制限事項

- **#9 (Triple Integration)**: transform-space の stutter 検出のみ。render-space の pixel snap
  jitter は本 spike scope 外。stutter 閾値 ≤3/120 は conservative 設定。
- **#11 (Execution Order)**: ADR-0002 V1 未通過のため、1-frame sync の完全検証は C1 scope。
  本 spike では `DefaultExecutionOrder` attribute 値の記録 + Brain.UpdateMethod 確認のみ。

## ファイル

| ファイル | 用途 |
|---|---|
| `Scripts/R1CinemachineSpike.cs` | EditorWindow (reflection API checks) + MonoBehaviour (runtime checks) |

## 実施担当

- ファイルセット整備: Claude Code（Context7 Cinemachine 3.1 docs ベース事前検証済み）
- Unity Editor 実機検証: ユーザ（プロジェクトリード）
- 結果保管: `production/qa/evidence/r1-camera-cinemachine3-spike-result.md`

## throw-away 性

本 spike は ADR-0006 C0 gate 通過が目的。実装コードは本番採用しない
（API 名検証 + 統合動作計測のみ）。

## API 名の根拠（Context7 Cinemachine 3.1 docs + unity-specialist 検証 2026-04-29）

| API | 事前検証結果 | 信頼度 |
|---|---|---|
| `Unity.Cinemachine.CinemachineCamera` | 存在確認 (sealed) | HIGH |
| `CinemachineBrain.UpdateMethods` enum | SmartUpdate default | MEDIUM |
| `CinemachineCamera.Follow` → `Transform` | 確認 | HIGH |
| `CinemachinePositionComposer` (Body) | 存在確認 | HIGH |
| `CinemachineConfiner2D.BoundingShape2D` (field) | `Collider2D` 型 | HIGH |
| `CinemachineConfiner2D.InvalidateBoundingShapeCache()` | 存在確認 | HIGH |
| `GenerateImpulse()` 7 overloads | 3 legacy + 3 new + 1 no-arg | HIGH |
| `CinemachineImpulseListener.UseCameraSpace` | `UseSignalSpaceOnly` から RENAMED | HIGH |
| `CinemachinePixelPerfect` | **empty stub** ([AddComponentMenu("")]) | MEDIUM |
| `CinemachineBrain.OutputCamera` → `Camera` | 確認 | HIGH |

## 関連

- ADR-0006: `docs/architecture/adr-0006-camera-system.md`
- R5 Spike (参考パターン): `prototypes/r5-class-switch-spike/`
