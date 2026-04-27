# R5 Class Switch Spike

ADR-0001「Class Switch Architecture」の Validation Gate **R5** を実機検証する throw-away spike。

## 目的

`docs/architecture/adr-0001-class-switch-architecture.md` の Validation Gate セクション通過条件 (a)-(e) を、Unityちゃん公式 PSB + SpriteSkin + SpriteLibraryAsset ランタイムスワップ最小構成で実測する。

## 通過条件（ADR-0001 Validation Gate より）

- **(a)** Unityちゃん公式 PSB の SpriteSkin が `_spriteLibrary.spriteLibraryAsset = newSLA` 後に正しく描画される
- **(b)** `SpriteResolver` が同フレーム内に `SpriteRenderer.sprite` を更新する
- **(c)** Profiler 計測で切替コスト ≤ 0.8 ms（warm path 平均）
- **(d)** API 名 / 自動 resolve 有無を文書化し、ADR-0001 の API 仮表記を確定形に更新
- **(e)** `SpriteRenderer.color` 直接書込が URP 2D + SRP Batcher と干渉せず描画される

## ファイル

| ファイル | 用途 |
|---|---|
| `Scripts/R5ClassSwitchSpike.cs` | 切替・計測・ロギング担当の最小スクリプト |
| `SETUP.md` | Unity プロジェクト初期化〜シーン構築の手順 |
| `VALIDATION.md` | 通過条件 (a)-(e) の検証手順 + Pass/Fail 基準 |

## 実施担当

- ファイルセット整備: Claude Code（unity-specialist 検証情報を反映済み）
- Unity Editor 実機検証: ユーザ（プロジェクトリード）
- 結果保管: `production/qa/evidence/r5-class-switch-spike-result.md`

## throw-away 性

本 spike は ADR-0001 Accepted 化が目的。実装コードは本番採用しない（API 名検証 + 数値計測のみ）。検証完了後、`prototypes/r5-class-switch-spike/` はリポジトリ履歴として残すが、`Assets/Prototypes/R5/` への配置は削除して問題ない。

## API 名の根拠（unity-specialist 検証 2026-04-27）

| API | 確定形 | 出典 |
|---|---|---|
| `SpriteLibrary.spriteLibraryAsset` (set) | 確定 | https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/api/UnityEngine.U2D.Animation.SpriteLibrary.html |
| `SpriteResolver.ResolveSpriteToSpriteRenderer()` | 確定 | https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/api/UnityEngine.U2D.Animation.SpriteResolver.html |
| 自動 resolve（明示呼び出し不要） | 公式 Full Skin Swap サンプル準拠（同フレーム同期は要実測） | https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/ex-sprite-swap.html |

## 関連

- ADR-0001: `docs/architecture/adr-0001-class-switch-architecture.md`
- Engine Reference 追記: `docs/engine-reference/unity/modules/animation.md`（「2D Animation 13.x（Skeletal）」セクション）
- Active session: `production/session-state/active.md`
