# R5 Class Switch Spike — Validation Result

**ADR**: `docs/architecture/adr-0001-class-switch-architecture.md`
**Spike**: `prototypes/r5-class-switch-spike/`
**実施日**: YYYY-MM-DD（記入時に更新）
**実施者**: （ユーザ名）

## 環境情報

| 項目 | 値 |
|---|---|
| Unity Editor バージョン | 6000.3.x |
| `com.unity.2d.animation` | 13.0.4 |
| `com.unity.2d.psdimporter` | （実値記入） |
| `com.unity.render-pipelines.universal` | （実値記入） |
| `com.unity.inputsystem` | （実値記入） |
| OS | （Windows 11 / macOS 14 等） |
| GPU | （RTX 4060 / M2 Pro 等） |
| URP 2D Renderer | （Asset 名・パス） |
| Unityちゃん PSB | （ファイル名・スケルトン情報） |

## 条件 (a): Skeletal 描画

- 実施: ☐
- 結果: Pass / Fail
- 観察:
  - スワップ回数: （N 回）
  - ボーン崩れ: 発生 / なし
  - スクリーンショット: `evidence/screenshots/r5_a_skeletal_*.png`（任意）

## 条件 (b): 同フレーム sprite 更新

### 自動 resolve（`_explicitResolve: OFF`）

- ログサンプル（5 回分）:
```
[R5] frame=... sameFrame=... spriteChanged=... ...
[R5] frame=... ...
```
- `sameFrame=True` の比率: N/M

### 明示 resolve（`_explicitResolve: ON`）

- ログサンプル（5 回分）:
```
（記入）
```
- `sameFrame=True` の比率: N/M

### 結論

- 自動 resolve で同フレーム同期が **安定する / 不安定 / Fail**
- 本番採用: 自動 / 明示呼び出し
- ADR-0001 反映文案: 「`spriteLibraryAsset` 代入のみで同フレーム resolve、明示呼び出し不要」 / 「明示呼び出し必須」

## 条件 (c): 切替コスト

| 計測 | cold path（1 回目） | warm path（2-10 平均） | Budget |
|---|---|---|---|
| `R5.SwapSLA` | _ ms | _ ms | 0.8 ms（warm）/ 1.0 ms（cold） |
| `R5.ColorWash` | _ ms | _ ms | 参考値 |
| 合計（参考） | _ ms | _ ms | — |

- Profiler スクリーンショット: `evidence/screenshots/r5_c_profiler_*.png`
- Pass / Fail 判定:

## 条件 (d): API 文書化

### 確定事項

| API | 確定形 | 備考 |
|---|---|---|
| `SpriteLibrary.spriteLibraryAsset` | プロパティ | unity-specialist 公式 API 確認済 |
| `SpriteResolver.ResolveSpriteToSpriteRenderer()` | メソッド | 同上、ADR-0001 の `ResolveSpriteToRenderer()` は誤記、訂正対象 |
| 自動 resolve | （実測結果記入） | |

### ADR-0001 訂正案（Accepted 化と同時に反映）

1. `Engine Compatibility` セクション
   - `Knowledge Risk: HIGH` の文言は維持しつつ
   - `Post-Cutoff APIs Used` の `com.unity.2d.animation 10.x` → `13.0.4` 訂正
   - `Verification Required` を「✅ 検証済（YYYY-MM-DD、本 evidence 参照）」に置換
2. `Decision` 内の擬似コード:
   - `_spriteResolver.ResolveSpriteToRenderer()` → `_spriteResolver.ResolveSpriteToSpriteRenderer()`（または自動 resolve 採用なら削除）
3. `technical-preferences.md` の `Allowed Libraries / Addons`:
   - `com.unity.2d.animation` の注記に「Unity 6.3 LTS 同梱は 13.0.4」を追記

## 条件 (e): SpriteRenderer.color × SRP Batcher

- Frame Debugger スクリーンショット: `evidence/screenshots/r5_e_framedbg_*.png`
- batch break の有無: なし / あり（原因: ）
- Pass / Fail 判定:

## 全体判定

| 条件 | Pass / Fail |
|---|---|
| (a) Skeletal 描画 | ☐ |
| (b) 同フレーム sprite 更新 | ☐ |
| (c) 切替コスト ≤ 0.8 ms | ☐ |
| (d) API 文書化 | ☐ |
| (e) color × SRP Batcher | ☐ |

**ADR-0001 昇格可否**:
- 全 Pass → Accepted へ昇格、`/architecture-decision` 後続フローへ
- 1 つでも Fail → ADR-0001 を `Superseded by ADR-0001a` 候補化、Alternative 2 (Mecanim) ベースの新 ADR 起草を検討

## 学び / 副次的観察

（spike 中に発見した、ADR には書かれていないが将来関連する事項）

## 次のアクション

- [ ] ADR-0001 Status を Accepted に更新
- [ ] ADR-0001 内の API 仮表記を確定形に置換
- [ ] `technical-preferences.md` の package version 訂正
- [ ] `docs/registry/architecture.yaml` に R5 検証結果由来の stance 追加（必要なら）
- [ ] 後続: ADR-0002 起草着手
