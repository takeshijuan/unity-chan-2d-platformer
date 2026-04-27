---
name: Art Bible Section Progress
description: アートバイブル各セクションの完成状況と主要決定事項
type: project
---

Art Bible は `design/art/` 以下に格納予定。現時点ではドラフト状態。

**Section 1-7 確定済み主要決定事項:**
- 基準解像度 384×216px（代替 360×224px）、Pixel Perfect Camera + Integer Scale のみ
- スプライト 1-2px Deep Slate `#2C3E50` 縁取り
- 六角形 = 職業/オーブ、アーチ形 = 部屋接続
- World 7 色パレット + 職業色 4 色（Crimson/Emerald/Indigo/Solar Gold）+ Aurora Teal
- 前景キャラ 48×48px、ボス 128×96〜256×192px
- 敵 Tier 1/2/3 定義済み
- 前景タイル 32×32、中景 48×48、背景 64×64
- 視差スクロール 4 Layer
- アイコン 32×32、HUD オーブ 48/32px
- Art Pipeline 案 C Hybrid: Unity-chan 公式（UCL 2.0）+ AI 生成は差分のみ

**Section 8 Asset Standards — Art ドラフト完成（2026-04-26）:**
- A. ファイル形式: 全スプライト PNG-32、スプライトシート PNG + JSON metadata
- B. 命名規則: `[category]_[name]_[variant]_[size].[ext]`、AI 由来は `_ai` サフィックス
- C. テクスチャ解像度ティア: 原寸維持、Atlas は PoT
- D. LOD: 奥行きレイヤー別解像度差（Layer 0-4）
- E. Export Settings: Filter Mode = Point、Compression = None（要 TA 確認）、sRGB ON
- F. AI パイプライン: 後処理チェックリスト + ai-asset-registry.csv 台帳

**Section 8 保留事項（technical-artist との確認待ち）:**
- Compression: None の VRAM 試算（Steam Deck 1.5GB 制約）
- 視差背景ミップマップ要否
- Sprite Mesh Type Tight の適用範囲
- Atlas 分割戦略（ゾーン別 vs カテゴリ別）
- Draw Call 300 以下の Atlas 試算
- .meta カスタムタグ実装

**Why:** Art Bible はビジュアルの唯一のソースオブトゥルース。Section 8 が未確定だとアセット納品時のフォーマット不一致が発生するため最優先で作成。
**How to apply:** 次回会話では保留事項の TA 回答を統合してファイル書き込み承認を得る。
