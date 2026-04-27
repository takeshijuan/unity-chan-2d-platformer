# R5 Spike — Setup Guide

Unity Editor 上での実機検証準備手順。`R5ClassSwitchSpike.cs` を動作させて `VALIDATION.md` の通過条件 (a)-(e) を計測できる状態を作る。

## 前提

- Unity Hub インストール済み
- **Unity 6.3 LTS (6000.3.x)** インストール済み
- Unityちゃん公式 PSB 素材入手（License: UCL 2.0、要確認）— https://unity-chan.com/contents/license/
- Git LFS（PSB ファイルが大きい場合）

## Step 1: Unity プロジェクト作成

1. Unity Hub → New Project → **2D Core** テンプレート
2. 場所: 本リポジトリ直下（`Assets/`, `Packages/`, `ProjectSettings/` がリポジトリルートに作成される）
3. Unity バージョン: **6000.3.x**（VERSION.md 準拠）

> **Note**: `Assets/`, `Library/`, `Logs/`, `Temp/` 等は `.gitignore` 検討。本 spike は `prototypes/` 配下を git 追跡する。

## Step 2: パッケージインストール（Window → Package Manager）

| パッケージ | バージョン | 必須/任意 |
|---|---|---|
| `com.unity.render-pipelines.universal` | Unity 6.3 同梱版 | 必須（URP 2D Renderer） |
| `com.unity.2d.animation` | **13.0.4** | 必須（SpriteSkin / SpriteLibrary / SpriteResolver） |
| `com.unity.2d.psdimporter` | Unity 6.3 同梱版 | 必須（Unityちゃん PSB インポート） |
| `com.unity.inputsystem` | 1.8+ | 必須（Keyboard / Gamepad 入力） |

> Project Settings → Player → Active Input Handling を **「Input System Package (New)」** に設定。Unity 再起動。

## Step 3: URP 2D Renderer 設定

1. Project ウィンドウで右クリック → Create → Rendering → URP Asset (with 2D Renderer)
2. 生成された Renderer 2D Asset を Project Settings → Graphics → Scriptable Render Pipeline Settings に割当
3. Project Settings → Quality の各 Quality Level にも同 Asset を割当

## Step 4: Unityちゃん公式 PSB インポート

1. `Assets/UnityChan/` ディレクトリ作成
2. Unityちゃん公式 PSB ファイル（Skeletal リグ済）を配置
3. Inspector で PSB ファイルを選択 → PSD Importer で:
   - **Use as Rig**: Character Rig（Skeletal）
   - **Sprite Mode**: Multiple
   - **Pivot Alignment**: Custom or Bottom（プロジェクト方針に合わせ）
4. Apply → Skinning Editor を開いてボーン階層を確認
5. **重要**: 後続の Step 5 で作る 2 つの SLA は **同一スケルトン** を共有する必要あり（公式必須要件、`SpriteSwapIntro.html` 参照）

## Step 5: SpriteLibraryAsset を 2 つ作成

1. Project 右クリック → Create → 2D → **Sprite Library Asset**
2. ファイル名: `SLA_A.spriteLib`（剣士相当）
3. もう 1 つ作成: `SLA_B.spriteLib`（弓士相当）
4. 両 SLA に **同一カテゴリ + 同一ラベル名** で Sprite を割当（中身の Sprite だけ違う / スケルトンは同じ）
   - 例: Category `Body`, Labels `Idle`, `Run`
5. SLA_A の Sprite はオリジナル PSB のもの、SLA_B は色味やバリエーション違いの Sprite（spike 段階では PSB 内の同 Sprite を流用しても可、(a) 検証の目的次第）

> **検証目的の考慮**: SLA_A と SLA_B が **完全に同じ Sprite** だと (a) の「描画変化」確認が困難。色違いテクスチャでもよいので、視覚的に区別可能な状態を推奨。

## Step 6: シーン構築

1. `Assets/_R5Spike/Scenes/R5Spike.unity` 新規作成
2. Hierarchy に Player GameObject 配置:
   - 既存 Unityちゃん Skeletal Prefab をシーンにドラッグ（SpriteRenderer + SpriteSkin が既に付いている想定）
   - SpriteSkin の **autoRebind: true** に設定
   - 同 GameObject に以下を Add Component:
     - `SpriteLibrary`
     - `SpriteResolver`
     - `R5ClassSwitchSpike`（後述 Step 7）
3. Camera を URP 2D Renderer 対応に設定（Camera Inspector → Renderer → 作成した URP 2D Renderer）

## Step 7: R5ClassSwitchSpike スクリプト配置

1. `Assets/_R5Spike/Scripts/` ディレクトリ作成
2. `prototypes/r5-class-switch-spike/Scripts/R5ClassSwitchSpike.cs` を上記にコピー
3. Unity がコンパイル → 警告/エラー無いことを確認

## Step 8: Inspector アサイン

R5ClassSwitchSpike コンポーネントの各フィールド:

| フィールド | アサイン値 |
|---|---|
| `_spriteLibrary` | 同 GameObject の SpriteLibrary |
| `_spriteResolver` | 同 GameObject の SpriteResolver |
| `_spriteRenderer` | 同 GameObject の SpriteRenderer |
| `_slaA` | `SLA_A.spriteLib` |
| `_slaB` | `SLA_B.spriteLib` |
| `_washA` | `#E63946`（赤系、剣士相当） |
| `_washB` | `#4DC8EB`（水色系、弓士相当） |
| `_washSec` | `0.15` |
| `_explicitResolve` | OFF（最初は自動 resolve 検証） |

SpriteResolver の Inspector で **Category** と **Label** を `Body` / `Idle` 等に設定（SLA に登録した値と一致させる）。

## Step 9: 動作確認 → VALIDATION.md へ

Play モード進入 → Space キー or Gamepad B（XInput）/ ✕（DualShock）押下 → 切替動作確認。

詳細な検証手順は `VALIDATION.md` 参照。
