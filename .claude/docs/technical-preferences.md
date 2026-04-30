# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.3 LTS (6000.3.x)
- **Language**: C# (.NET)
- **Rendering**: Universal Render Pipeline (URP) 2D Renderer
- **Physics**: Unity 2D Physics (Box2D v3 integrated, multi-threaded) + カスタム Kinematic CharacterController2D

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC (Steam / Itch.io / Epic Games Store)
- **Input Methods**: Keyboard/Mouse, Gamepad
- **Primary Input**: Gamepad（アクション主体、職業切替コンボ操作が核）
- **Gamepad Support**: Full（Unity Input System + Action Rebinding UI 必須、Steam Input API 対応）
- **Touch Support**: None
- **Platform Notes**: 全UIはd-pad/スティックで操作可能であること。ホバー専用インタラクションは禁止。Steam Deck Verified 申請を Tier 2a Demo 時点で検討。

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerController`) — must be `partial` if used with SourceGenerators
- **Public fields/properties**: PascalCase (e.g., `MoveSpeed`)
- **Private fields**: `_camelCase` (e.g., `_moveSpeed`, `_isGrounded`)
- **Methods**: PascalCase (e.g., `TakeDamage()`, `SwitchClass()`)
- **Signals/Events**: C# `event` keyword + PascalCase + `EventHandler` suffix（例: `HealthChangedEventHandler`）、または UnityEvent のインスペクタ公開
- **Files**: PascalCase matching class (e.g., `PlayerController.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `PlayerController.prefab`, `Zone01_RuinedCastle.unity`)
- **Constants**: PascalCase (e.g., `MaxHealth`) or UPPER_SNAKE_CASE（定数コンテナクラス内）
- **Assembly Definitions**: `.asmdef` ファイル名もPascalCase (`Game.Core.asmdef`)

## Performance Budgets

- **Target Framerate**: 60 fps（PC標準）
- **Frame Budget**: 16.6 ms / frame
  - Update loop: ≤ 4 ms
  - Physics: ≤ 2 ms
  - Rendering: ≤ 6 ms
  - その他（UI、入力、GC等）: ≤ 4 ms
- **Draw Calls**: 300 以下（2D メトロイドヴァニア標準、SRP Batcher前提）
- **Memory Ceiling**: 1 GB RAM（Steam Deck 考慮）
- **VRAM**: 1.5 GB 以下（統合GPU PC対応）

## Testing

- **Framework**: Unity Test Framework (NUnit ベース、EditMode + PlayMode)
- **Minimum Coverage**: ゲームプレイシステムの単体テスト必須（切替ロジック、コンボ判定、セーブ互換性）
- **Required Tests**:
  - バランスフォーマル（ダメージ計算、コンボ倍率）
  - ゲームプレイシステム（職業切替 State Machine、Class-Swap の 1 フレーム同期）
  - セーブデータの schemaVersion マイグレーション（Low risk だが事故ると致命）
  - 入力バッファ（コンボ先行入力 4-6 フレーム）
- **CI**: GitHub Actions + `game-ci/unity-test-runner@v4`（ヘッドレス自動実行）

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- **`GameObject.Find()` / `FindObjectOfType()` / `GetComponent()` in `Update()`** — `Awake()` でキャッシュ必須
- **`SendMessage()` / `BroadcastMessage()`** — Rimote invoke は禁止、event/interface で明示的に
- **`PlayerPrefs` でゲームセーブデータを保存** — Steam Cloud 同期外、Windows Registry 依存で破損しやすい。`Application.persistentDataPath` + Newtonsoft.Json + Steam Remote Storage を使用
- **Magic numbers（ゲームバランス）** — ScriptableObject に配置、コードハードコード禁止
- **`Animator.Play()` で即時視覚切替を期待** — Animator は次フレーム反映。切替瞬間は `SpriteRenderer.sprite` の直接代入で 1 フレーム視覚遅延をゼロ化
- **URP Compatibility Mode の使用** — Unity 6.3 で削除済み、新規プロジェクトでは不要
- **`Physics.autoSyncTransforms`** — Unity 6.3 で非推奨、`Physics.SyncTransforms()` を明示呼び出し

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->

### Unity Official Packages（初期セットアップ時に入れる）
- `com.unity.render-pipelines.universal`（URP 2D）
- `com.unity.inputsystem`（Input System 1.8+、Action Rebinding）
- `com.unity.cinemachine` (Cinemachine 3、2D Confiner Extension)
- `com.unity.2d.animation`（2D Skeletal Animation, PSD Importer, Sprite Library, 2D IK）
- `com.unity.2d.tilemap.extras`（Rule Tile, Animated Tile）
- `com.unity.addressables`（2.0+、Scene 動的ロード）
- `com.unity.nuget.newtonsoft-json`（セーブデータシリアライゼーション）
- `com.unity.test-framework`（Unity Test Framework）

### 将来検討（実装開始時に追加）
<!-- Guardrail: 実装開始時まで追加しない -->
- Steamworks.NET（Tier 2a Demo 時、Steam Cloud / Achievements 統合開始時）

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->

- [ADR-0001](docs/architecture/adr-0001-class-switch-architecture.md): 職業切替アーキテクチャ（ScriptableObject + ClassStateMachine + SpriteLibrary）
- [ADR-0002](docs/architecture/adr-0002-character-controller-motor.md): Kinematic CharacterController2D 自作（Unity 2D 標準物理は不採用）
- [ADR-0003](docs/architecture/adr-0003-vfx-system-boundary.md): VFXシステム境界
- [ADR-0004](docs/architecture/adr-0004-save-data-system.md): セーブシステム（Newtonsoft.Json + schemaVersion + Steam Cloud）
- [ADR-0005](docs/architecture/adr-0005-input-system-architecture.md): Input System アーキテクチャ
- [ADR-0006](docs/architecture/adr-0006-camera-system.md): カメラシステム（Cinemachine 3 + 2D Confiner、Accepted Provisional / C0 PASS 2026-04-30）
- [ADR-0006a](docs/architecture/adr-0006a-camera-system-r1-findings.md): Camera System R1 Findings（Cinemachine 3 API 命名確定 + PixelPerfect 採用方針 + C1 protocol 拡張）
- [ADR-0007](docs/architecture/adr-0007-combo-input-buffer.md): Combo Input Buffer（Ring Buffer + IComboBuffer + ScriptableObject Window）
- [ADR-0008](docs/architecture/adr-0008-class-abilities-system.md): Class Abilities System（ClassAbilityData + AbilityContext + HitConfirmed event）
- [ADR-0009](docs/architecture/adr-0009-combat-system.md): Combat System（HitConfirmed → IDamageReceiver Thin Mediator）

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# レビューは primary でカバー)
- **Shader Specialist**: unity-shader-specialist (Shader Graph、HLSL、URP/HDRP マテリアル)
- **UI Specialist**: unity-ui-specialist (UI Toolkit UXML/USS、UGUI Canvas、ランタイム UI)
- **Additional Specialists**: unity-addressables-specialist (アセット読込、メモリ管理、コンテンツカタログ)
- **Routing Notes**: アーキテクチャと一般 C# コードレビューは primary を呼ぶ。UI 実装は unity-ui-specialist を呼ぶ。Addressables を使うアセット管理は unity-addressables-specialist を呼ぶ。unity-dots-specialist (ECS/Jobs/Burst) は本作では不使用（2D メトロイドヴァニアに DOTS はオーバーエンジニアリング）。

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (.uxml, .uss, Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| ScriptableObject assets (.asset) | unity-specialist |
| Addressables groups (.asset, AddressableAssetsData) | unity-addressables-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | unity-specialist |
