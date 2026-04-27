# R5 Spike — Validation Procedure

ADR-0001 Validation Gate の通過条件 (a)-(e) を実測する手順と Pass/Fail 基準。

> 結果の記入先: `production/qa/evidence/r5-class-switch-spike-result.md`

## 共通準備

- `SETUP.md` Step 1-9 完了済
- Unity Editor: Window → Analysis → **Profiler** を開く
- Window → Analysis → **Frame Debugger** を開く（条件 (e) 用）
- Console を Clear、`[R5]` ログを観測可能に

## 条件 (a): Unityちゃん PSB の SpriteSkin が SLA スワップ後も正しく描画

**手順**:
1. Play モード進入
2. Space キー押下 → SLA_B にスワップ
3. **目視**: ボーン変形が崩れていないか（手足・頭の位置、関節の捻じれ等）
4. もう一度 Space → SLA_A に戻る、再度目視
5. 連打 5 回 → 描画破綻が累積しないか

**Pass 基準**: ボーン変形に破綻なし、Sprite が SLA に応じて切り替わる

**Fail 時の対応**:
- スケルトン不一致 → SLA_A / SLA_B のボーン階層を Skinning Editor で照合、Copy/Paste で同期
- 描画ちらつき → SpriteSkin の `autoRebind: true` 確認

---

## 条件 (b): SpriteResolver が同フレームに SpriteRenderer.sprite を更新

**手順 1（自動 resolve 検証）**:
1. Inspector で `_explicitResolve: OFF` に設定
2. Play → Space 押下
3. Console の `[R5]` ログを確認:
   ```
   [R5] frame=N sameFrame=True spriteChanged=True from=... to=... explicitResolve=False
   ```

**Pass 基準**: `sameFrame=True` かつ `spriteChanged=True`

**手順 2（明示 resolve 検証）**:
1. Stop → `_explicitResolve: ON` に変更
2. Play → Space 押下
3. 同様にログ確認

**期待**: 両モードで `sameFrame=True spriteChanged=True`。差異が出れば `_explicitResolve: ON` を本番採用方針とし、ADR-0001 に明記。

**Fail 時の対応**:
- `sameFrame=False` の場合 → SpriteResolver の自動更新が次フレームに繰り越されている
- ADR-0001 を Superseded 候補にし、Mecanim ベース Alternative 2 へ Pivot 検討

---

## 条件 (c): 切替コスト ≤ 0.8 ms（warm path 平均）

**手順**:
1. Play モード進入
2. Profiler → CPU Usage モジュール → Hierarchy 表示
3. Space 連打 10 回（**1 回目は cold path、2-10 回目を warm path として平均**）
4. Profiler の検索ボックスに `R5.SwapSLA` を入力 → 該当フレームの実コスト記録
5. `R5.ColorWash` も同様に確認（参考値）
6. cold path（1 回目）と warm path（2-10 回目平均）を分けて記録

**Pass 基準**:
- warm path 平均 ≤ 0.8 ms
- cold path 単発 ≤ 1.0 ms

**Fail 時の対応**:
- テクスチャアトラス分散が原因の可能性 → SLA_A / SLA_B の Sprite を同一 Sprite Atlas に集約して再計測
- それでも超過 → ADR-0001 の Performance Budget を再評価

---

## 条件 (d): API 名 / 自動 resolve 有無の文書化

**手順**:
1. 条件 (b) の手順 1-2 結果を `production/qa/evidence/r5-class-switch-spike-result.md` に記入
2. 自動 resolve（`_explicitResolve: OFF`）で `sameFrame=True` が安定するか、明示呼び出しが必要かを判定
3. 結果を ADR-0001 の以下に反映（**本 spike では下書きのみ、本反映は Accepted 化と同時**）:
   - `Engine Compatibility` の `Verification Required` を「✅ 検証済（YYYY-MM-DD）」に
   - 擬似コード `_spriteResolver.ResolveSpriteToRenderer()` を `ResolveSpriteToSpriteRenderer()` に修正（unity-specialist 確認済の API 名）
   - パッケージバージョン `10.x` → `13.0.4` を訂正

**Pass 基準**: 上記 3 項目の更新文案が evidence ファイルに記載されている

---

## 条件 (e): SpriteRenderer.color × URP 2D × SRP Batcher 干渉なし

**手順**:
1. Frame Debugger を開く
2. Play → Space 押下（color wash 中の 0.15s 内に Pause）
3. Frame Debugger で SpriteRenderer の draw call を選択
4. **「Why this draw call can't be batched with the previous one?」** メッセージを確認
5. SpriteRenderer.color 変更前後で batch break が発生していないか比較
   - **比較ベース**: `_washSec = 0` にして color 変更を実質無効化したケース（Inspector 一時変更）vs `_washSec = 1.0` の継続変更ケース

**Pass 基準**:
- Frame Debugger に SpriteRenderer 由来の batch break が出ていない
- または出ていても、原因が color ではなく別要素（Material 違い等）

**Fail 時の対応**:
- color が原因で batch break が出る場合、SpriteRenderer.color 採用を再評価
- ただし unity-specialist 調査では公式記述上「per-object buffer 経由」で問題なしとなっており、Fail は想定外

---

## 全体判定

5 条件すべて Pass → ADR-0001 Status を Proposed → **Accepted** に更新可能。

| 条件 | Pass / Fail | 結果サマリ |
|---|---|---|
| (a) | ☐ | |
| (b) | ☐ | |
| (c) | ☐ | |
| (d) | ☐ | |
| (e) | ☐ | |

→ `production/qa/evidence/r5-class-switch-spike-result.md` に詳細記入
