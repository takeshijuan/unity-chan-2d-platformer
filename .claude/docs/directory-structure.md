# Directory Structure

Unity 6.3 LTS プロジェクトとして初期化済み（2026-04-30）。Unity 標準（`Assets/` `Packages/` `ProjectSettings/`）を採用し、テンプレート由来の engine-agnostic ディレクトリ（`design/` `docs/` `production/` 等）と共存。

```text
/
├── CLAUDE.md                    # Master configuration
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
│
├── Assets/                      # Unity asset root（必須・固定名）
│   ├── _Project/                # ゲーム本体実装（先頭 `_` で Project ビューの最上段にソート）
│   │   ├── Scripts/             # C# ソース（asmdef 単位で分割）
│   │   │   ├── Game.Core/       # interface / data 層（IDamageReceiver, ICharacterMotor 等）
│   │   │   ├── Game.Gameplay/   # MonoBehaviour 実装層
│   │   │   └── Game.Editor/     # Editor 専用拡張（Inspector / カスタムツール）
│   │   ├── Art/                 # アート（スプライト、アニメ、マテリアル、シェーダ）
│   │   ├── Audio/               # 音楽 / SFX
│   │   └── Prefabs/             # プレハブ
│   ├── Scenes/                  # シーン（テンプレート初期 SampleScene.unity を含む）
│   ├── Settings/                # URP / Renderer2D / Lit2DSceneTemplate 等の SO アセット
│   ├── Tests/                   # Unity Test Framework
│   │   ├── EditMode/            # EditMode テスト（Game.Tests.EditMode.asmdef）
│   │   └── PlayMode/            # PlayMode テスト（Game.Tests.PlayMode.asmdef）
│   ├── ThirdParty/              # 外部資産（例: Unityちゃん公式素材 UCL 2.0）
│   ├── DefaultVolumeProfile.asset       # テンプレート由来
│   ├── InputSystem_Actions.inputactions # テンプレート由来（後で ADR-0005 設計に再構成）
│   └── UniversalRenderPipelineGlobalSettings.asset # テンプレート由来
│
├── Packages/                    # UPM マニフェスト（manifest.json + packages-lock.json）
├── ProjectSettings/             # Unity プロジェクト設定（22 ファイル + ProjectVersion.txt）
│
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation
│   ├── architecture/            # ADR（adr-0001〜0009 配置済）
│   ├── engine-reference/unity/  # Unity 6.3 LTS API スナップショット（version-pinned）
│   └── registry/                # architecture.yaml / tr-registry.yaml
├── tools/                       # Build / pipeline / validation scripts
│   └── ci/                      # Local validation (compile check 等)
│       ├── unity-compile-check.sh  # Unity local compile validation (詳細は tools/ci/README.md)
│       └── README.md            # Script catalog + usage + exit codes
├── prototypes/                  # 独立 spike プロジェクト（例: r5-class-switch-spike/）
├── production/                  # Sprint / milestone / QA evidence
│   ├── session-state/           # active.md（tracked、worktree 間ハンドオフ用）
│   └── session-logs/            # Session 監査ログ（gitignored）
│
├── src/                         # **DEPRECATED** — Unity 移行前の placeholder。新規コードは Assets/_Project/Scripts/ へ
│
└── CCGS Skill Testing Framework/ # CCGS テンプレートのテストフレームワーク（ゲーム本体に非関連）
```

## 重要な配置規約

### Unity が認識する固定名（変更禁止）
- `Assets/` — Unity プロジェクトの全アセット（コード含む）のルート
- `Packages/` — UPM マニフェスト
- `ProjectSettings/` — エディタ設定 + `ProjectVersion.txt`（Unity 6000.3.13f1 ピン留め）
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `*.csproj`, `*.sln` — Unity 自動生成、`.gitignore` 済み

### 命名・配置規約（[technical-preferences.md](.claude/docs/technical-preferences.md) に従う）
- **C# ソース**: `Assets/_Project/Scripts/<asmdef-name>/` 配下に PascalCase ファイル名
- **シーン**: `Assets/Scenes/` または `Assets/_Project/Scenes/`、PascalCase（例: `Zone01_RuinedCastle.unity`）
- **プレハブ**: `Assets/_Project/Prefabs/`、PascalCase（例: `PlayerController.prefab`）
- **ScriptableObject 設定**: `Assets/_Project/Settings/` または領域別フォルダ（命名規約は systems-index.md 参照）
- **外部資産（Unityちゃん 公式素材等）**: `Assets/ThirdParty/<vendor-name>/` でライセンス境界を明示

### asmdef 階層
- `Game.Core` — UnityEngine 参照のみ。interface / POCO / data class 専用。他 asmdef へ依存禁止
- `Game.Gameplay` — `Game.Core` + Unity package（InputSystem / URP / 2D Animation / SpriteShape）に依存
- `Game.Editor` — Editor プラットフォーム限定、`Game.Core` + `Game.Gameplay` に依存
- `Game.Tests.EditMode` / `Game.Tests.PlayMode` — `nunit.framework.dll` precompiled 参照、`UNITY_INCLUDE_TESTS` define constraint

すべての asmdef で `autoReferenced: false` を採用し、依存関係を明示化（隠れた相互参照を防止）。

## File Routing

ファイル種別ごとの specialist 振り分けは [technical-preferences.md](.claude/docs/technical-preferences.md) → `Engine Specialists` → `File Extension Routing` を参照。
