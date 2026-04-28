# ADR-0005: Input System Architecture (4 Action Maps + IInputEventStream + Rebinding via SettingsSaveable)

## Status

**Proposed (Validation Gate: I0-I5)**

> 本 ADR は「Validation Gate」セクション **I0-I5** の検証を通過するまで Accepted に昇格しない。特に **I0（IL2CPP build smoke + link.xml 機能検証）** と **I1（Action Map bleed-through 不在 — Tier 0 PR1 前 Spike 必須）** が偽の場合、Decision の根幹（Generated C# class IL2CPP 互換 + 4 Action Map 状態遷移 + ADR-0004 SettingsSaveable 経由永続化）が崩壊し、Alternative 再評価となる。なお本 ADR は **Proposed 段階でも `docs/registry/architecture.yaml` への architectural stance を前倒し追記**する（CC2D / Combo Input Buffer / Class Switch / Title Menu UI が `IInputService` / `IInputEventStream` interface を story authoring 時点で参照する必要があるため）。

## Date

2026-04-28

## Last Verified

2026-04-28

## Decision Makers

- Project Lead（ユーザ）— 最終決定権
- `creative-director` 経由 CD-SYSTEMS Note CD2（Combo Input Buffer 4-6f 先行入力の前提インフラ）
- `technical-director` 経由 TD-ADR gate — Foundation 層 routing と Game State Machine 境界の整合性
- `producer` 経由 PR-SCOPE — Tier 0 MVP に最小限の rebinding UI が含まれることの整合性
- `unity-specialist` — Unity 6.3 LTS Input System 1.8+ / Action Rebinding API / Steam Input 自動認識 / IL2CPP リフレクション挙動レビュー
- `accessibility-specialist` — d-pad/stick 全 UI navigation / hover-only 禁止 / Steam Deck Verified 要件レビュー

## Summary

本作 `職業オーブのレガシー` の Foundation 層において、Input System を **Unity Input System 1.8+ + Generated C# class + 4 Action Maps（Gameplay / UI / Pause / Dialogue）+ `IInputEventStream` Combo 連携 interface + Rebinding via ADR-0004 SettingsSaveable** で確定する。Game State Machine（ADR-0006 future）が `IInputService.SetActiveMap()` を呼んで状態遷移時に map を enable/disable し、Combo Input Buffer System（#9）は `IInputEventStream` から timestamp + phase + control id をストリームとして購読する。Rebinding は `InputActionAsset.SaveBindingOverridesAsJson()` を SettingsSaveable.Data["rebinding"] に verbatim 格納（ADR-0004 整合）。Steam Input は Tier 0 で Input System default 認識のみ、Steam Input 専用 API（glyph / Steam Deck layout）は Tier 2a Demo で別 ADR + Game.SteamCloud.asmdef 経由注入。UI Hover-only は Roslyn analyzer rule で build error 化（Steam Deck Verified 拘束）。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Input |
| **Knowledge Risk** | **MEDIUM-HIGH** — `com.unity.inputsystem` 1.8+ は安定 API、Unity 6.3 LTS で active default。**Steam Input auto-detect は config 依存**（Steam が `Gamepad` 互換エミュレーション有効時のみ Input System が `Gamepad.current` で認識、Steam Input API 優先設定では Virtual Gamepad / Action sets 経由になる）— "信頼できる仕様" ではなく Steam 設定 + Steam Deck native build / Proton build で挙動が分岐。Steam Input 専用 API（glyph / Steam Deck layout 取得）は engine-reference 未収録、Tier 2a で別途検証 |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`docs/engine-reference/unity/modules/input.md`（Input System 1.11+ 全機能、Action Maps / Rebinding / Auto-Switch 例）、`docs/engine-reference/unity/breaking-changes.md`（lines 38-50: Legacy Input deprecation, line 226: Input System 必須化）、`docs/engine-reference/unity/deprecated-apis.md`（lines 10-20: `Input.*` deprecation table）、`docs/engine-reference/unity/current-best-practices.md`（lines 124-145: Input System 推奨パターン）、`.claude/docs/technical-preferences.md` lines 13-23, 77（Input methods, Gamepad full, Action Rebinding 必須, Steam Input 対応, d-pad/stick 全 UI navigable, hover-only 禁止）、`design/gdd/game-concept.md` line 238、`design/gdd/systems-index.md` #1 Input System / #9 Combo Input Buffer、`docs/architecture/tr-registry.yaml`（TR-input-001/002/003）、`docs/architecture/adr-0004-save-data-system.md`（SettingsSaveable rebinding 形式、forbidden `playerprefs_for_save_data`） |
| **Post-Cutoff APIs Used** | `com.unity.inputsystem` 1.8+ の `InputActionAsset` / `InputAction` / `PerformInteractiveRebinding()` / `SaveBindingOverridesAsJson()` / `LoadBindingOverridesFromJson()`、`InputSystem.onDeviceChange` event、`InputControlScheme`、`InputBinding.MaskByGroup()`、`Gamepad.current` / `Keyboard.current` / `Mouse.current`、Steam Input は Input System 自動認識（`Gamepad` interface 経由）— Tier 2a で `Steamworks.NET ISteamInput` を別 asmdef で注入予定 |
| **Verification Required** | (0) **IL2CPP build smoke**（I0 Spike 必須、Tier 0 PR1 前段）— Generated C# class + link.xml で stripping 防止が機能、`PlayerControls` インスタンス化と Action callback 動作を確認、(1) **4 Action Map bleed-through Spike**（I1 必須、Tier 0 PR1 前段）— `InputActionMap.Disable()` 直後の同 frame 内で旧 map action の `performed` event が発火しないことを実機 / Editor 両方で実測（`InputAction.Reset()` は public API に存在しないため `Disable()` の挙動信頼可否を検証）、(2) `PerformInteractiveRebinding()` の `WithControlsExcluding("<Mouse>/position")` で UI navigation 用 binding を除外できるか、(3) **Steam Deck で Input System が Gamepad device として正しく detect するか**（Steam Input default 設定 = Gamepad 互換モードで `Gamepad.current` 認識、Steam Input API 優先設定での Virtual Gamepad 認識、native Linux build / Proton build 両方）、(4) `InputAction.CallbackContext.time`（`double` 型、`Time.realtimeSinceStartup` ベース）が `Time.unscaledTime` より frame 内 event 順序判定に高精度であることの確認、(5) IL2CPP で Generated C# class の reflection が正常動作するか（毎 PR の CI gate）、(6) `InputAction.GetBindingIndexForControl(InputControl)` の戻り値 -1 ケース（binding 不一致）の取扱い、(7) `InputSystem.actions = newAsset` で実行時に Action Asset を切替えると既存 binding override が保持されるか、(8) `SaveBindingOverridesAsJson()` を `IInputService` interface 経由で取得できる API 設計の妥当性 — **すべて I0 / I1 / I2 / I5 検証プロトタイプで実測必須** |

> **Note**: Knowledge Risk が MEDIUM のため、`com.unity.inputsystem` のメジャーバージョンが 2.x に上がった場合や Action Asset format が変更された場合、本 ADR を Superseded にし新 ADR を起こすこと。Steam Input 専用 API integration は Tier 2a Demo で **新 ADR**（ADR-XXXX Steam Integration）として起草、本 ADR は Tier 0 default detection のみ範囲。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None — Input System 本体（IInputService / IInputEventStream / 4 Action Maps / RebindingService）は ADR-0004 に直接依存しない。**SettingsInputSaveable コンポーネントのみ**が ADR-0004 の `saveable_contract` interface に依存（systems-index design order #1 Input → #2 Save の順序を保つため、Input 本体は Save と peer relationship） |
| **Coordinates with** | [ADR-0004 Save Data System](adr-0004-save-data-system.md)（`SettingsInputSaveable` のみ ADR-0004 ISaveable contract と forbidden `playerprefs_for_save_data` を遵守、rebinding 永続化は SettingsSaveable.Data["rebinding"] verbatim — ADR-0004 未受 Accept でも Input 本体は spike 可、SettingsInputSaveable のみ ADR-0004 Accepted 後）、[ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md)（class_switch_notification の producer として `ClassStateMachine.SwitchTo` を呼び出す）、[ADR-0002 CharacterController2D](adr-0002-character-controller-motor.md)（`motor_intent_command` は ability layer 経由、Input System は raw input 配信のみ） |
| **Requires (revise requests)** | **ADR-0001 R5 revise: `SwitchContext { PlayerInput, SystemRestore, NarrativeForced }` enum + `SwitchTo(ClassDefinition, SwitchContext)` overload**（ADR-0004 B2 と同一要請、Input System 経由 class switch input handler が `SwitchContext.PlayerInput` で発火し、Tier 0 inline color-wash + SE feedback を正しく駆動するため） |
| **Enables** | systems-index #6 CharacterController2D（Move / Jump / Dash 入力受信）、#9 Combo Input Buffer（IInputEventStream 購読、4-6f 先行入力 / 空中キャンセル / 職業間受け渡し）、#10 Class Switch System（R1/L1 + slot keys 入力 → SwitchTo）、#11 Class Abilities System（攻撃 / アビリティ入力 → AbilityExecutor）、#24 Title / Menu / Settings UI（UI Action Map + Rebinding UI）、#29 Accessibility Options（rebinding / d-pad navigation 拡張）、**ADR-0006 Game State Machine（`IInputService.SetActiveMap` で routing 駆動。本 ADR の `ActionMapId` enum 値（`None=0 / Gameplay=1 / UI=2 / Pause=4 / Dialogue=8`）は ADR-0006 が継承し redefine 不可、Tier 1 で Cinematic 等を追加する場合は値 16, 32... を append すること）**、**ADR-0006 が `IUIInputProxy` の最終所有権を持つ可能性あり**（ADR-0005 では `IInputEventStream` の UI subscriber spec のみ確定、`IUIInputProxy` 具体実装は ADR-0006 起草時に再確定）、ADR-XXXX Steam Integration（Tier 2a で Steam Input 専用 API 注入） |
| **Blocks** | systems-index MVP の上記 6 systems すべて（Input System 不在では何も操作できない、systems-index.md line 201 既述） |
| **Ordering Note** | systems-index.md Recommended Design Order **#1 Input System**（Foundation 層最優先）、ADR-0004 と並列で進行可（peer）。Production Constraint Option A により MVP に最小スタブ（4 Action Maps + 基本 binding + rebinding 最小実装）を含む。本 ADR Accepted 後、ADR-0006 Game State Machine が `IInputService.SetActiveMap` orchestration を担い、Pause / Dialogue 状態の入力 routing 責務が ADR-0006 完全所管となる |

## Context

### Problem Statement

本作のゲームプレイ核心（Pillar 1「切替が、花になる」R1/L1 1 ボタン即時切替、Pillar 3「歯ごたえ」コンボ 4-6f 先行入力 / 空中キャンセル / 職業間受け渡し）は **入力 routing と timestamp 配信の正確さ**に依存する。systems-index.md は Input System を **#1 Foundation 層最優先**と位置づけ、CC2D / Combo Input Buffer / Class Switch / Class Abilities / Title Menu UI / Accessibility Options の 6 system が直接依存する。

加えて、technical-preferences.md は「Action Rebinding UI 必須」「Gamepad full」「全 UI が d-pad / スティックで操作可能」「ホバー専用インタラクションは禁止」「Steam Deck Verified 申請を Tier 2a で検討」と規定し、これらは Input System Architecture が **Foundation 段階で**満たすべき制約。Legacy Input Manager は Unity 6.3 で deprecated（engine-reference/breaking-changes.md line 38）、`com.unity.inputsystem` 1.8+ が公式採用。

しかし、interface 詳細（`IInputService` / `IInputEventStream` shape）/ Action Map 分割粒度 / Combo Input Buffer (#9) との境界 / rebinding UI 永続化経路（ADR-0004 SettingsSaveable との整合）/ Steam Input 統合タイミング（Tier 0 / Tier 2a）/ 4 Action Map 状態遷移時の bleed-through 防止 / hover-only 禁止の実装手段が未定義。`docs/registry/architecture.yaml` に Input System 関連 stance は未登録のため、systems-index #6/#9/#10/#11/#24/#29 の story authoring がブロックされる。

特に critical な矛盾点として、`docs/engine-reference/unity/modules/input.md` lines 269-275 の "Save/Load Bindings" 例は **`PlayerPrefs.SetString("InputBindings", rebinds)`** を使うが、これは ADR-0004 で **forbidden_pattern `playerprefs_for_save_data`** として登録済。本 ADR で engine-reference 例を supersede し、SettingsSaveable 経由永続化を確定する必要がある。

### Current State

- `.claude/docs/technical-preferences.md` line 19-23, 77 で Input System 1.8+ / Gamepad full / Action Rebinding UI 必須 / Steam Input 対応 / d-pad/stick 全 UI navigable / hover-only 禁止 / Steam Deck Verified Tier 2a が確定済
- `design/gdd/game-concept.md` line 238 で Input System 1.8+（Steam Input 対応、Action Rebinding UI）が技術選定として記載
- `design/gdd/systems-index.md` #1 Input System (Foundation, MVP) — "Unity Input System の薄い service ラッパ（Action Rebinding / Steam Input / コンボバッファ向け timestamp 配信）" と方針記載（line 201）
- `docs/architecture/tr-registry.yaml` v2 で TR-input-001 (Input System 1.8+ / Action Rebinding UI / Steam Input)、TR-input-002 (Gamepad full / d-pad/stick 全 UI)、TR-input-003 (コンボバッファ用 timestamp 配信) の 3 件が登録済
- ADR-0004 が `saveable_contract` interface + SettingsSaveable.Data["rebinding"] = SaveBindingOverridesAsJson() verbatim 形式を確定
- `docs/architecture/architecture-review-2026-04-27.md` で Input System Architecture を Foundation Blocking gap として識別、本 ADR で解消
- registry に Input System 関連 stance なし（本 ADR で確定参照を追加）

### Constraints

- **Engine**: Unity 6.3 LTS / `com.unity.inputsystem` 1.8+ のみ採用、Legacy Input Manager は deprecated（forbidden）
- **Library**: `com.unity.inputsystem` 1.8+ 公式 package のみ、Rewired / 商用 plugin 不採用
- **Persistence**: rebinding 永続化は ADR-0004 SettingsSaveable 経由のみ、PlayerPrefs 全面禁止（ADR-0004 forbidden_pattern 継承）
- **Performance (Input poll)**: `Update` フェーズの input poll + event dispatch ≤ 0.3 ms / frame、`Update ≤4ms` envelope の 1/13
- **Determinism**: Input timestamp は `Time.unscaledTime`（pause 中も停止しない wall clock 系）を使用、Game State Machine pause 中の Combo Buffer flush 動作を Combo Buffer 側で制御
- **Steam Deck Verified 要件**: 全 UI は gamepad-only で操作完結、mouse hover を要求しない、起動時に gamepad 自動検出
- **Localization**: Input glyph (button icon) は Tier 2a で Steam Input glyph API 経由、MVP では generic Xbox glyph 静的画像
- **IL2CPP**: Generated C# class + Input Action Asset の reflection が IL2CPP ビルドで動作必須、link.xml 配備
- **Save Data 整合**: Input rebinding format は `InputActionAsset.SaveBindingOverridesAsJson()` 文字列 verbatim、Schema 変更は Input System version pin で抑制

### Requirements

- **R1**: Foundation/Core 層に library-agnostic な `IInputService` / `IInputEventStream` interface を `Game.Core.asmdef` で提供（Unity Input System への直接結合を gameplay code から隔離）
- **R2**: 4 Action Maps を 1 Action Asset (`Assets/Input/PlayerControls.inputactions`) に集約: **Gameplay / UI / Pause / Dialogue**
- **R3**: Game State Machine（ADR-0006）から `IInputService.SetActiveMap(MapName)` で map enable/disable を駆動、状態遷移時に bleed-through なし
- **R4**: Generated C# class（"Generate C# Class" workflow）で型安全 Action access、magic string 禁止
- **R5**: `IInputEventStream` で Combo Input Buffer (#9) に timestamp + phase + control id を配信
- **R6**: Rebinding は `PerformInteractiveRebinding()` で実施、永続化は ADR-0004 SettingsSaveable.Data["rebinding"] 経由（PlayerPrefs 禁止）
- **R7**: Default bindings: Keyboard&Mouse + Gamepad の 2 control schemes、Move / Jump / Attack / Dash / ClassSwitchLeft / ClassSwitchRight / ClassSlot1-3 / Pause / Interact / UI Navigate / UI Submit / UI Cancel
- **R8**: Steam Input は Tier 0 で Input System default detection（`Gamepad` device として認識）のみ、Steam Input glyph / Steam Deck layout API は Tier 2a で別 ADR
- **R9**: 全 UI component は d-pad/stick navigable、`IPointerEnterHandler` 単独使用は Roslyn analyzer rule で build error 化（Steam Deck Verified 拘束）
- **R10**: `Time.unscaledTime` ベースの timestamp、`InputAction.performed` 時に Combo Buffer 向け event を IInputEventStream に push
- **R11**: Pause 中の Combo Buffer flush 動作は Combo Buffer 側責務、Input System は raw event 配信のみ
- **R12**: IL2CPP リンクストリッピング対策の `Assets/link.xml` を本 ADR 配備時に追加

## Decision

Input System を以下の構造で確定する：

1. **`IInputService` interface** + **`IInputEventStream` interface** を `Game.Core.asmdef` に配置
2. **`IUIInputProxy` interface**（UI navigation 用 d-pad/stick wrapper）を `Game.Core.asmdef` に配置
3. **`Assets/Input/PlayerControls.inputactions` Action Asset**（4 Action Maps、Generated C# class 有効化）
4. **`InputService` MonoBehaviour**（Scene root install、`DontDestroyOnLoad`、`[DefaultExecutionOrder(-90)]` で SaveDataService の -100 直後に Awake）を `Game.Input.asmdef` に配置
5. **`InputEventStream` 実装** が Generated `PlayerControls` の各 InputAction の started/performed/canceled を subscribe し、`InputEvent` struct を Action<> event で配信
6. **`SettingsInputSaveable : ISaveable`**（SectionId = "settings"、ADR-0004 SettingsSaveable と共存 — 同 SectionId 内の subkey で分離）を `Game.Input.asmdef` に配置、`[RegisterSaveable(loadOrder=10)]` で auto-register
7. **`RebindingService` MonoBehaviour**（`PerformInteractiveRebinding()` wrapper、conflict detection、cancel handling）を `Game.Input.asmdef` に配置、Title Menu UI が UI 経由で呼出
8. **4 Action Maps**: 
   - **Gameplay**: Move / Jump / Attack / Dash / ClassSwitchLeft / ClassSwitchRight / ClassSlot1 / ClassSlot2 / ClassSlot3 / Pause / Interact
   - **UI**: Navigate / Submit / Cancel / TabLeft / TabRight / Pause (close)
   - **Pause**: Resume / SaveQuit / OpenSettings / Navigate
   - **Dialogue**: Advance / Skip / SpeedUp (hold)
9. **Game State Machine 駆動 routing**（ADR-0006 が orchestrator）: 
   - Title 状態 → UI map only
   - Playing → Gameplay (+ UI overlay for HUD navigation if needed)
   - Paused → Pause + UI
   - Dialogue → Dialogue (+ UI for skip menu)
   - SettingsScreen → UI + Rebinding-specific input bypass
10. **`Game.SteamInput.asmdef` 別 assembly**（Tier 2a で追加、Game.Core / Game.Input 参照、Define Constraint `STEAM_INTEGRATION`）に Steam Input glyph / Steam Deck layout 統合を配置。MVP build には含まれない
11. **Hover-only 禁止**: `IPointerEnterHandler` を実装するクラスは必ず `ISelectHandler` または `ISubmitHandler` も実装することを Roslyn analyzer で強制、PR1 で配備
12. **Forbidden pattern strictness**: Legacy `UnityEngine.Input.*` API（GetKey / GetAxis 等）の使用、PlayerPrefs での binding 永続化、magic string action name（PlayerInput Send Messages mode）、`InputAction.performed` 直接 subscribe（gameplay code は `IInputEventStream` 経由のみ）を Roslyn analyzer / registry forbidden_patterns で全面禁止

13. **Foundation Singleton-like access pattern**（H4 — TD-ADR finding 解消）: ADR-0001 ClassStateMachine / ADR-0002 CharacterController2D / ADR-0003 VFXPublisherService / ADR-0004 SaveDataService / 本 ADR-0005 InputService **すべて MVP 段階では MonoBehaviour + DontDestroyOnLoad + Static `Instance` プロパティ + interface 提供** の Singleton-like access pattern を統一採用する。テスト容易性は interface 経由 mock injection で部分的に確保し、Tier 1 で DI コンテナ（VContainer / Zenject 候補）導入を再評価する。registry に `architectural_stance: foundation_singleton_pattern` として明示登録。本 ADR の `InputService.Instance` は当該 stance に従う

14. **`SettingsInputSaveable` の interface-only 結合**（B1 fix — unity-specialist BLOCKING 解消）: `SettingsInputSaveable` は **`InputService` 具象クラスを直接参照しない**。`IInputService.SaveBindings(SaveSection)` / `RestoreBindings(SaveSection)` を経由し、Save / Restore ロジックは `InputService` が internal で `_controls.SaveBindingOverridesAsJson()` / `LoadBindingOverridesFromJson()` を呼ぶ。これにより `SettingsInputSaveable` の unit test は `IInputService` mock のみで完結する

### Architecture

```
┌────────────────────────────────────────────────────────────────┐
│ Game.Core.asmdef (Foundation, no library deps)                 │
│   IInputService               (this ADR)                       │
│   IInputEventStream           (this ADR)                       │
│   InputEvent (struct)         (this ADR)                       │
│   InputPhase enum             (this ADR)                       │
│   ActionMapId enum            (this ADR)                       │
│   IUIInputProxy               (this ADR)                       │
│   ISaveable                   (ADR-0004)                       │
└────────────────────────────────────────────────────────────────┘
              ▲                            ▲
              │ implements                  │ subscribes
┌──────────────────────────────────┐  ┌──────────────────────────┐
│ Game.Input.asmdef                │  │ Game.Gameplay.asmdef     │
│   InputService                   │  │   ClassStateMachine      │
│     ├─ wraps PlayerControls.cs   │  │     (R1/L1 → SwitchTo)   │
│     │  (Generated C# class)      │  │   CharacterController2D  │
│     │  + PlayerControls.inputact │  │     (Move / Jump 等)     │
│     └─ SetActiveMap(ActionMapId) │  │   AbilityExecutor        │
│   InputEventStream               │  │     (Attack / Dash)      │
│     ├─ subscribe InputAction     │  │   ComboInputBuffer       │
│     │   started/performed/cancel │  │     (subscribes IInput-  │
│     └─ push InputEvent to        │  │      EventStream で      │
│        Action<InputEvent>        │  │      4-6f buffer 管理)   │
│   RebindingService               │  └──────────────────────────┘
│     └─ PerformInteractiveRebind  │
│        + conflict detection      │  ┌──────────────────────────┐
│   SettingsInputSaveable          │  │ Game.UI.asmdef           │
│     [RegisterSaveable(10)]       │  │   TitleMenu / Pause /    │
│     SectionId="settings"         │  │   Settings Screen        │
│     Data["rebinding"] =          │  │     (UI map subscribe)   │
│       SaveBindingOverridesAsJson │  │   RebindingUI            │
└──────────────────────────────────┘  │     (RebindingService    │
                                      │      呼出)               │
                                      └──────────────────────────┘
              │
              │ Action Asset references
              ▼
        ┌───────────────────────────────────────┐
        │ Assets/Input/                         │
        │   PlayerControls.inputactions         │
        │   PlayerControls.cs (Generated)       │
        │     Action Maps:                      │
        │       Gameplay (11 actions)           │
        │       UI (6 actions)                  │
        │       Pause (4 actions)               │
        │       Dialogue (3 actions)            │
        │     Control Schemes:                  │
        │       KeyboardMouse / Gamepad         │
        └───────────────────────────────────────┘

[Tier 2a 以降 — 別 asmdef]
┌─────────────────────────────────────────────────┐
│ Game.SteamInput.asmdef                          │
│   define constraint: STEAM_INTEGRATION          │
│   SteamInputGlyphProvider : IInputGlyphProvider │
│   SteamDeckLayoutDetector                       │
│   (Steamworks.NET ISteamInput)                  │
└─────────────────────────────────────────────────┘
              │ injected at startup (DI / SerializeField)
              ▼
        InputService._glyphProvider
```

データフロー（Class Switch 入力 例）:
```
[Input device] R1 button pressed
    ↓
PlayerControls.Gameplay.ClassSwitchRight.performed callback
    ↓
InputService が InputEvent {Action="class_switch_right",
                          Phase=Performed,
                          Timestamp=Time.unscaledTime,
                          Value=1f, Device=Gamepad} を生成
    ↓
InputEventStream.OnInputReceived event 発火
    ↓
ComboInputBuffer.OnInputReceived(evt) — buffer に push、4-6f 内の連携を判定
    ↓
ClassStateMachine subscribes 直接 (gameplay specific) →
  ClassStateMachine.SwitchTo(nextClass, SwitchContext.PlayerInput)  (ADR-0001)
    ↓
class_switch_notification event 発火、VFX / Audio System が consume (ADR-0003)
```

データフロー（Game State Machine 駆動 routing 例）:
```
[Game State] Playing → Paused 遷移（Pause 入力 or app focus loss）
    ↓
GameStateMachine.RequestTransition(Paused) (ADR-0006 future)
    ↓
GameStateMachine.OnExitPlaying:
    _inputService.SetActiveMap(ActionMapId.None)  // disable Gameplay 一旦
    ↓
GameStateMachine.OnEnterPaused:
    _inputService.SetActiveMap(ActionMapId.Pause | ActionMapId.UI)
    ↓
InputService.SetActiveMap:
    _playerControls.Gameplay.Disable()
    _playerControls.UI.Enable()
    _playerControls.Pause.Enable()
    _playerControls.Dialogue.Disable()
    ↓
Player input は Pause / UI map にのみ流れる、bleed-through 不在
```

### Key Interfaces

```csharp
// Game.Core.asmdef
namespace Game.Core.Input
{
    [System.Flags]   // M1 fix — bit flags ToString() 可読化
    public enum ActionMapId { None = 0, Gameplay = 1, UI = 2, Pause = 4, Dialogue = 8 }
                                // bit flags、複数 map 同時 enable 可能（Pause + UI 等）
                                // 値は ADR-0006 が継承、redefine 禁止。Tier 1 で
                                // Cinematic = 16 等を append のみ可能

    public enum InputPhase { Started, Performed, Canceled }

    public readonly struct InputEvent
    {
        public readonly string ActionName;     // "attack" / "class_switch_left"
        public readonly InputPhase Phase;
        public readonly float Timestamp;        // Time.unscaledTime
        public readonly float Value;            // for analog actions (0-1 trigger 等)
        public readonly Vector2 Vector2Value;   // for Vector2 actions (Move / Look 等)
        public readonly InputDeviceKind Device; // Keyboard / Mouse / Gamepad
        public readonly int BindingIndex;       // primary / alt binding index

        public InputEvent(string actionName, InputPhase phase, float timestamp,
                          float value, Vector2 vector2Value, InputDeviceKind device, int bindingIndex)
        { /* ... */ }
    }

    public enum InputDeviceKind { Unknown, Keyboard, Mouse, Gamepad }

    public interface IInputService
    {
        ActionMapId ActiveMaps { get; }
        InputDeviceKind LastUsedDevice { get; }

        /// <summary>Replace active map flags. Maps not in flags are disabled.</summary>
        void SetActiveMap(ActionMapId maps);

        /// <summary>Add specified maps to active set without disabling others.</summary>
        void EnableMap(ActionMapId map);

        /// <summary>Remove specified maps from active set.</summary>
        void DisableMap(ActionMapId map);

        /// <summary>Fired when last-used input device changes (Gamepad ↔ Keyboard).
        /// UI subscribes to swap glyphs.</summary>
        event System.Action<InputDeviceKind> ActiveDeviceChanged;

        /// <summary>(B1 fix) Persist current binding overrides into a SaveSection
        /// (ADR-0004 SettingsSaveable contract). Internal: calls
        /// _controls.SaveBindingOverridesAsJson() and stores verbatim under "rebinding" key.
        /// SettingsInputSaveable calls this — never accesses InputService concrete instance.</summary>
        void SaveBindings(Game.Core.Persistence.SaveSection section);

        /// <summary>(B1 fix) Restore binding overrides from a SaveSection.
        /// Internal: calls _controls.LoadBindingOverridesFromJson(section.Get<string>("rebinding")).
        /// Safe to call before _controls initialization (no-op + log warning if not ready).</summary>
        void RestoreBindings(Game.Core.Persistence.SaveSection section);
    }

    public interface IInputEventStream
    {
        /// <summary>Global input event stream. Combo Input Buffer subscribes here.</summary>
        event System.Action<InputEvent> InputReceived;

        /// <summary>Subscribe to specific action only (optional convenience).</summary>
        void Subscribe(string actionName, System.Action<InputEvent> handler);
        void Unsubscribe(string actionName, System.Action<InputEvent> handler);
    }

    public interface IUIInputProxy
    {
        /// <summary>Submit / Cancel / Navigate イベント (UI 用、d-pad/stick 統一抽象).</summary>
        event System.Action OnSubmit;
        event System.Action OnCancel;
        event System.Action<Vector2> OnNavigate;
        event System.Action OnTabLeft;
        event System.Action OnTabRight;
    }
}
```

```csharp
// Game.Input.asmdef
namespace Game.Input
{
    [DefaultExecutionOrder(-90)]
    public sealed class InputService : MonoBehaviour, IInputService, IInputEventStream
    {
        private PlayerControls _controls;             // Generated C# class
        private InputDeviceKind _lastUsedDevice;
        public ActionMapId ActiveMaps { get; private set; }
        public InputDeviceKind LastUsedDevice => _lastUsedDevice;

        public event Action<InputDeviceKind> ActiveDeviceChanged;
        public event Action<InputEvent> InputReceived;

        private void Awake()
        {
            _controls = new PlayerControls();
            HookActionCallbacks();
            InputSystem.onEvent += OnRawInputEvent; // device tracking 用
        }

        private void HookActionCallbacks()
        {
            // Gameplay map
            _controls.Gameplay.Move.performed += ctx => Push("move", InputPhase.Performed, ctx);
            _controls.Gameplay.Jump.performed += ctx => Push("jump", InputPhase.Performed, ctx);
            _controls.Gameplay.Attack.performed += ctx => Push("attack", InputPhase.Performed, ctx);
            _controls.Gameplay.Dash.performed += ctx => Push("dash", InputPhase.Performed, ctx);
            _controls.Gameplay.ClassSwitchLeft.performed += ctx =>
                Push("class_switch_left", InputPhase.Performed, ctx);
            _controls.Gameplay.ClassSwitchRight.performed += ctx =>
                Push("class_switch_right", InputPhase.Performed, ctx);
            // ... 他の actions も同様

            // UI map
            _controls.UI.Navigate.performed += ctx => Push("ui_navigate", InputPhase.Performed, ctx);
            _controls.UI.Submit.performed += ctx => Push("ui_submit", InputPhase.Performed, ctx);
            _controls.UI.Cancel.performed += ctx => Push("ui_cancel", InputPhase.Performed, ctx);
            // ...
        }

        private void Push(string actionName, InputPhase phase, InputAction.CallbackContext ctx)
        {
            // M2 fix: GetBindingIndexForControl returns -1 if control not in bindings.
            // Default to 0 (primary binding) for unknown controls.
            int bindingIdx = ctx.action.GetBindingIndexForControl(ctx.control);
            if (bindingIdx < 0) bindingIdx = 0;

            var evt = new InputEvent(
                actionName: actionName,
                phase: phase,
                // M3 fix: ctx.time is double, Time.realtimeSinceStartup-based, accurate per-event.
                // Use this instead of Time.unscaledTime which is frame-start-only and identical for
                // all events in same Update.
                timestamp: (float)ctx.time,
                value: ctx.ReadValueAsButton() ? 1f : ctx.ReadValue<float>(),
                vector2Value: ctx.valueType == typeof(Vector2) ? ctx.ReadValue<Vector2>() : Vector2.zero,
                device: DetectDeviceKind(ctx.control.device),
                bindingIndex: bindingIdx
            );
            InputReceived?.Invoke(evt);
        }

        // B1 fix: SaveBindings / RestoreBindings allow SettingsInputSaveable to use
        // IInputService interface only, no concrete InputService dependency.
        public void SaveBindings(Game.Core.Persistence.SaveSection section)
        {
            string rebindJson = _controls.SaveBindingOverridesAsJson();
            section.Set("rebinding", rebindJson);
        }

        public void RestoreBindings(Game.Core.Persistence.SaveSection section)
        {
            // H3 fix: handle null _controls (Awake order race).
            // SettingsInputSaveable.Restore can be called via SaveDataService.Start()
            // (after all Awake completes), but defensive null check anyway.
            if (_controls == null)
            {
                Debug.LogWarning("[Input] RestoreBindings called before InputService.Awake completed. Deferred restore needed.");
                return;
            }
            string rebindJson = section.Get<string>("rebinding", "");
            if (!string.IsNullOrEmpty(rebindJson))
                _controls.LoadBindingOverridesFromJson(rebindJson);
        }

        public void SetActiveMap(ActionMapId maps)
        {
            // 4 Action Maps を bit flags で一括切替、bleed-through 防止
            if ((maps & ActionMapId.Gameplay) != 0) _controls.Gameplay.Enable(); else _controls.Gameplay.Disable();
            if ((maps & ActionMapId.UI) != 0) _controls.UI.Enable(); else _controls.UI.Disable();
            if ((maps & ActionMapId.Pause) != 0) _controls.Pause.Enable(); else _controls.Pause.Disable();
            if ((maps & ActionMapId.Dialogue) != 0) _controls.Dialogue.Enable(); else _controls.Dialogue.Disable();
            ActiveMaps = maps;
        }
    }

    [RegisterSaveable(loadOrder: 10)]
    public sealed class SettingsInputSaveable : ISaveable
    {
        // B1 fix: receive IInputService via constructor, not concrete singleton access
        private readonly IInputService _inputService;

        // SaveDataService の reflection scan は parameterless ctor を期待。
        // 現実装では reflection scan が IInputService を locator から resolve（ServiceLocator.Get<IInputService>()）
        // または partial Activator.CreateInstance + post-construction Initialize(IInputService) 経路。
        // ADR-0004 の [RegisterSaveable] スキャン仕様と整合する Initialize injection を採用。
        public SettingsInputSaveable() { _inputService = ServiceLocator.Get<IInputService>(); }

        public string SectionId => "settings";
        public int SchemaVersion => 1;

        public SaveSection Capture()
        {
            var section = new SaveSection { SectionId = SectionId, SchemaVersion = SchemaVersion };
            _inputService.SaveBindings(section);   // B1 fix: interface 経由のみ
            return section;
        }

        public void Restore(SaveSection section)
        {
            _inputService.RestoreBindings(section); // B1 fix: interface 経由のみ
            // H3 fix: RestoreBindings 内部で null check 済み、Awake 順序 race を吸収
        }
    }
}
```

### Implementation Guidelines

1. **`Game.Input.asmdef` 新規作成**: `Game.Core.asmdef` + `com.unity.inputsystem` に依存。`Game.Gameplay.asmdef` には依存しない（依存方向は publisher → core のみ）
2. **`InputService` install**: Scene root に GameObject 1 つ、`DontDestroyOnLoad`、`[DefaultExecutionOrder(-90)]` で SaveDataService(-100) の直後 Awake。Static `Instance` プロパティで Singleton access（DI を使わない MVP 簡易化）
3. **Action Asset workflow**: `Assets/Input/PlayerControls.inputactions` で 4 Action Maps + 2 Control Schemes（KeyboardMouse / Gamepad）を定義。Inspector で "Generate C# Class" ✅ + "Class Name = PlayerControls" + "Namespace = Game.Input.Generated"。Generated `PlayerControls.cs` は `Assets/Input/PlayerControls.cs` に出力（`.gitignore` で除外せず commit 対象）
4. **Action 命名規律**: snake_case で actionName を InputEvent.ActionName に統一（"class_switch_left" / "ui_navigate" 等）。Action Asset 内の Display Name は CamelCase でも可（UI 表示用）、event 用は ActionName property 経由で snake_case 取得
5. **Generated C# class の更新**: Action 追加 / 削除時は Action Asset を編集 → Inspector で "Apply" → Generated `PlayerControls.cs` 再生成。**Generated file は手動編集禁止**、必要な拡張は partial class で別ファイルに分離
6. **`IInputEventStream` の subscriber 規律**: gameplay code から `InputAction.performed` を直接 subscribe **してはいけない**（forbidden）。`IInputEventStream.InputReceived` event 経由のみ、または `IInputEventStream.Subscribe(actionName, handler)` で specific action 購読。Roslyn analyzer で gameplay assembly 内の `InputAction.performed +=` を検出 → build error 化
7. **Combo Input Buffer integration**: `ComboInputBuffer.Awake` で `_inputEventStream.Subscribe("attack", OnAttack); _inputEventStream.Subscribe("dash", OnDash); ...` で必要 actions のみ購読。`OnDisable` で必ず Unsubscribe（leak 防止）。timestamp 単位は `Time.unscaledTime`、buffer 4-6f は `Time.unscaledDeltaTime` 累積で算出
8. **Class Switch input integration**: `ClassStateMachine` は `IInputEventStream.Subscribe("class_switch_left", ...)` / `Subscribe("class_switch_right", ...)` / `Subscribe("class_slot_1", ...)` 等で購読、`SwitchTo(targetClass, SwitchContext.PlayerInput)` を呼び出す（ADR-0001 R5 revise の SwitchContext を使用）
9. **UI input integration**: Title / Pause / Settings Screen UI は `IUIInputProxy` を購読。生 InputAction 直接 subscribe は禁止、`IUIInputProxy.OnSubmit` / `OnCancel` / `OnNavigate` event のみ使用。Unity UI EventSystem の `IPointerEnterHandler` を実装するクラスは **必ず** `ISelectHandler` または `ISubmitHandler` も実装すること（hover-only 禁止 — Roslyn analyzer で強制）
10. **Rebinding flow**:
    ```csharp
    public sealed class RebindingService : MonoBehaviour
    {
        public IEnumerator StartInteractiveRebind(InputAction action, int bindingIndex,
                                                   Action<RebindingResult> onComplete)
        {
            // Disable all maps during rebind
            var prevMaps = InputService.Instance.ActiveMaps;
            InputService.Instance.SetActiveMap(ActionMapId.None);

            var op = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(operation =>
                {
                    var result = ResolveResult(operation);
                    operation.Dispose();
                    InputService.Instance.SetActiveMap(prevMaps);  // restore
                    onComplete?.Invoke(result);
                })
                .Start();

            while (!op.completed) yield return null;
        }
    }
    ```
11. **Rebinding 永続化**: `RebindingService.OnComplete` で `SaveDataService.Instance.SaveAllAsync(currentSlot)` を呼出（ADR-0004 経由）。`SettingsInputSaveable` が `Capture()` 時に `SaveBindingOverridesAsJson()` を Data["rebinding"] に格納。**PlayerPrefs 直接書込は forbidden**
12. **Steam Input default detection**: Tier 0 では `InputSystem` がデフォルトで Steam Input を `Gamepad` device として認識する仕様を信頼。明示的 `Steamworks.NET ISteamInput` 連携は Tier 2a で `Game.SteamInput.asmdef` 別 asmdef + Define Constraint `STEAM_INTEGRATION` で配備、本 ADR では interface のみ（`IInputGlyphProvider`）を Game.Core に置き、MVP build では `DefaultInputGlyphProvider`（Xbox glyph 静的）を bind
13. **IL2CPP 対応**（H2 fix — link.xml 完全化）: `Assets/link.xml` に以下を追加:
    ```xml
    <linker>
      <assembly fullname="Unity.InputSystem" preserve="all"/>
      <assembly fullname="Unity.InputSystem.ForUI" preserve="all"/>  <!-- H2 追加: EventSystem integration -->
      <assembly fullname="Game.Core" preserve="all"/>                <!-- H2 追加: interface stripping 防止 -->
      <assembly fullname="Game.Input" preserve="all"/>
      <type fullname="Game.Input.Generated.PlayerControls" preserve="all"/>
    </linker>
    ```
    `Game.Core` は IInputService / InputEvent struct / ActionMapId enum 等の interface 層を含み、IL2CPP で reflection 経由 access される可能性があるため `preserve="all"` で安全側。Tier 1 でプロファイル後に絞込み
14. **Hover-only 禁止 Roslyn rule**: PR1 で実装、`IPointerEnterHandler` を implements する class は必ず `ISelectHandler` または `ISubmitHandler` も implements すること、違反は build error。例外は `[GamepadAccessibilityExempt]` attribute で明示的に opt-out（Tooltip / drag handle / scroll indicator / context menu hover preview 等の hover が必然な component 専用、L2 fix — 旧記述 `[ToolPipShowOnHover]` typo を修正、より一般化した命名へ）
15. **UI Navigation test**: Tier 0 PR2 で PlayMode test 実装、全 UI screens（TitleMenu / PauseMenu / SettingsScreen 等）に対し d-pad のみで全要素到達可能 + interact 可能を assert
16. **Forbidden pattern Roslyn rules**（PR1 で実装）:
    - `UnityEngine.Input.GetKey/GetKeyDown/GetMouseButton/GetAxis` (Legacy Input class)
    - `PlayerPrefs.SetXxx` for binding storage（ADR-0004 ban の Input domain 拡張）
    - `InputAction.performed +=` を gameplay assembly から（IInputEventStream 経由のみ許可）
    - Magic string action name in `PlayerInput.SendMessages` mode
    - `IPointerEnterHandler` 単独実装（ISelectHandler / ISubmitHandler 必須）
17. **Pause 中の Combo Buffer 動作**: `Time.timeScale = 0` で pause しても `(float)ctx.time`（M3 fix — 旧 `Time.unscaledTime` から変更）は進む。Combo Buffer は pause 中に新規 input を受けないが、buffer 内の既存 input は Resume 時に持ち越し。Combo Buffer 側責務（本 ADR は `ctx.time` 配信のみ）

18. **非 Steam 配布 fallback path**（L3 fix）: `STEAM_INTEGRATION` Define なしビルド（itch.io DRM-free / Epic Games Store / 直接配布）でも `Gamepad.current` での native HID detection が機能することを Tier 0 PR1 smoke test に含む。Steam Input 不在時は generic gamepad（Xbox Wireless / DualSense / 8BitDo 等）の HID 認識経由で動作。Steam Deck の native Linux build 時も同様

19. **`.inputactions` stale 検出 CI**（M9 fix）: `.inputactions` ファイル変更を含む PR で `.cs` Generated file が同 PR 内で更新されていることを GitHub Actions `paths-filter` で check。stale 検出時は `[input-system]` label を自動付与し、PR description で `Apply` 操作の必要性を notify。Tier 1 で Unity headless ライセンス取得後に再生成 + diff ベースの厳密検証へ移行検討

20. **`SaveBindingOverridesAsJson` golden file 検証**（L1 fix）: CI に `tests/fixtures/input/binding_overrides_v1.json` を凍結保管し、`com.unity.inputsystem` バージョン更新 PR で diff を検出。format breaking change を即時検知して ADR-0004 schema migration chain（v1→v2）への分岐を可能にする

## Alternatives Considered

### Alternative 1: PlayerInput component + Send Messages mode

- **Description**: GameObject に `PlayerInput` component を attach、Behavior = "Send Messages"。`OnMove(InputValue)` / `OnJump(InputValue)` 等の magic-string method を player class に実装
- **Pros**: 設定簡単、Inspector workflow、初心者向け
- **Cons**: **magic string で refactor 不安全**（method 名 typo がコンパイル通過、runtime fail）、`Find References` 不能、IL2CPP で reflection-based dispatch 必要 + パフォーマンス hit
- **Rejection Reason**: Refactor safety 違反、本作のような長期メンテナンスプロジェクトに不適

### Alternative 2: PlayerInput component + Invoke C# Events mode

- **Description**: PlayerInput component の Behavior = "Invoke C# Events"、Inspector で各 Action にハンドラを bind
- **Pros**: 型安全（Generated C# class より弱いが magic string よりマシ）、Inspector visual workflow
- **Cons**: Inspector binding に依存（prefab 破損 / merge conflict 時に再bind 必要）、Action 追加時に Inspector 再作業、Generated C# class より型安全性低い
- **Rejection Reason**: prefab merge conflict 耐性低、Generated C# class workflow が技術選定済（technical-preferences）

### Alternative 3: Rewired (商用 plugin)

- **Description**: Rewired の高機能 input library（複数 player 同時対応、device hot-swap、controller 詳細認識）
- **Pros**: Steam Input 自動対応、device 認識精度高、複数 player 対応
- **Cons**: **商用ライセンス必須**（$50+/seat）、Unity Input System との二重実装になる、本作は single player でオーバースペック、technical-preferences 未規定
- **Rejection Reason**: 商用ライセンス追加コスト + scope オーバーキル + technical-preferences 違反

### Alternative 4: 2 Action Maps（Gameplay / UI のみ）

- **Description**: Pause / Dialogue も UI map 内に統合、map 数最小化
- **Pros**: 最もシンプル、map switching 機構簡素
- **Cons**: Pause 中に Gameplay action（攻撃 / ジャンプ）を block する責務が UI map 内のロジックに分散、Dialogue 中の "skip" / "advance" を UI navigate と区別できない（UI Submit は dialogue advance と menu submit 両方の意味になる、context-dependent dispatch が必要）
- **Rejection Reason**: state-aware routing が UI map 内に押し込まれて Game State Machine の責任分割が崩れる

### Alternative 5: 3 Action Maps（Gameplay / UI / Cinematic = Pause + Dialogue 統合）

- **Description**: Pause と Dialogue を Cinematic map に統合（pause 中の dialogue 進行はないと仮定）
- **Pros**: map 数中庸、Tier 1 で boss cutscene / dialogue scenes 拡張時に再利用可能
- **Cons**: Pause UI と Dialogue UI で必要な action が異なる（Pause = Resume / SaveQuit / OpenSettings、Dialogue = Advance / Skip / SpeedUp）、Cinematic 内に両方詰めると action 数膨張
- **Rejection Reason**: Pause と Dialogue は UX が決定的に違う、separate map が clarity 高い

### Alternative 6: Per-scene Action Maps（TitleMenu / GameplayScene / SettingsScreen）

- **Description**: Scene 単位で専用 Action Map、SceneManager 切替で map 切替
- **Pros**: Scene と input map の対応明示
- **Cons**: state（Pause）と Scene の交差で map 数爆発（GameplayScene-Playing / GameplayScene-Paused / GameplayScene-Dialogue / TitleMenu-MainMenu / TitleMenu-Settings ...）、Scene 横断 state は map に表現できない
- **Rejection Reason**: state machine と Scene loading の混在は ADR-0006 で禁止される予定（systems-index A3）

### Alternative 7: Tier 0 から Steamworks.NET ISteamInput 直接統合

- **Description**: MVP build から Steamworks.NET 依存を含め、Steam Input 専用 API（Action sets / Glyph / Steam Deck layout 取得）を Tier 0 で配備
- **Pros**: Steam Deck Verified 早期取得可能、Steam Input カスタマイザを Tier 0 から提供
- **Cons**: ADR-0004 と同じトレードオフ — MVP build に Steamworks.NET 必須、CI ヘッドレスでテスト不可、Steam SDK 初期化コスト、Steam パートナー登録手続き加速必要、playtest 配布時に Steam ログイン要件
- **Rejection Reason**: MVP scope 圧迫 + テストインフラ複雑化。Tier 0 default Steam Input detection（Input System の Gamepad device 経由）で十分、専用 API は Tier 2a で別 ADR + 別 asmdef

### Alternative 8: Tier 2a まで Steam Input 一切考慮せず（Generic Gamepad のみ）

- **Description**: Tier 0/1 は Xbox / DualSense / generic gamepad のみ、Steam Deck テストは認識されるオフ
- **Pros**: scope 最小、Steam SDK 不要
- **Cons**: Steam Deck プレイテスト不可能、Steam Deck Verified 申請を Tier 2a で検討する technical-preferences に違反（早期検証が利かない）
- **Rejection Reason**: Steam Deck プレイテスト不可は MVP playtest フィードバック収集の致命傷

### Alternative 9: Combo Buffer が `InputAction.performed` を直接購読

- **Description**: Combo Input Buffer が InputActions を直接 reference、`_controls.Gameplay.Attack.performed += OnAttack;`
- **Pros**: 中間層なし、最高速
- **Cons**: Combo Buffer が Input System に **タイトに結合**、Combo Buffer の unit test に Input System mock が必要、Action 名変更で Combo Buffer code 修正必須、A1 様の God Object 化リスク（input + combo 判定 + 職業データを 1 system が抱える）
- **Rejection Reason**: Game.Input.asmdef と Game.Gameplay.asmdef の境界が崩れる、`IInputEventStream` 抽象で同等性能 + 疎結合実現

### Alternative 10: Input System が Combo Buffer 機能も内蔵、#9 廃止

- **Description**: `InputAction.history` パターンで Input System 単体でコンボバッファを実装、systems-index #9 を廃止
- **Pros**: system 数削減
- **Cons**: Input System に combo 値判定 / 4 職業 × N combo データ / 先行入力 4-6f / 職業間受け渡しを抱え込ませる → A1 類似の God Object 化、Class Switch / Class Abilities ADR の「ClassAbilityData(SO) + IAbilityExecutor + AbilityContext 三分割」(A1) 思想と矛盾
- **Rejection Reason**: systems-index A1 architecture note 違反

### Alternative 11: Hover-only 規制を PlayMode test のみで実施（analyzer なし）

- **Description**: Roslyn analyzer 実装せず、PlayMode test で全 UI を gamepad-only navigation 検証
- **Pros**: 実装コスト最小
- **Cons**: PR review + test カバレッジに依存、IPointerEnterHandler だけ実装した component が release build に混入するリスク、CI fail まで気づかない
- **Rejection Reason**: Steam Deck Verified 拘束、機械的強制が必要

### Alternative 12: ADR-0005 では規定のみ、実装ストラテジは UX spec / Title Menu UI ADR で

- **Description**: 本 ADR は Input System scope のみ、hover-only 禁止 / UI navigation 等の規律は別 ADR
- **Cons**: registry forbidden_patterns に分散、story authoring 時に複数 ADR 参照必要、Roslyn analyzer 実装オーナーシップ曖昧
- **Rejection Reason**: 規律 + 実装手段を 1 ADR にまとめる方が enforcement が明示的

## Consequences

### Positive

- gameplay code は Unity Input System を直接知らない（`IInputService` / `IInputEventStream` 経由のみ）、Input System version up や別 input library 移行時のリファクタが asmdef 境界に局所化
- 4 Action Maps で Game State Machine の responsibility と Input routing が直交、bleed-through の機械的防止
- Generated C# class で magic string なし、refactor 安全
- ADR-0004 SettingsSaveable 経由 rebinding 永続化で PlayerPrefs forbidden 違反ゼロ、Steam Cloud 同期も同 path で自動化
- Steam Input default detection で Tier 0 から Steam Deck プレイテスト可能、専用 API は Tier 2a で interface 変更ゼロで拡張
- Roslyn analyzer rule で hover-only / Legacy Input / PlayerPrefs binding / `InputAction.performed` 直接 subscribe を機械的に block、forbidden 違反が release build に混入するリスクゼロ
- `IUIInputProxy` で UI 側が Input System から疎結合、UI library 変更（UGUI → UI Toolkit）時の影響局所化

### Negative

- Generated C# class の Action 追加 / 削除で再生成 + commit 必要（手動 Apply 操作）、CI で生成 file が stale でないか check 必要（M9 fix の paths-filter で部分軽減）
- 4 Action Maps の同期メンテナンス（Action 追加時に Map 配置を熟慮、bleed-through テスト追加必要）
- Roslyn analyzer 4 rules（Legacy Input / PlayerPrefs binding / InputAction direct subscribe / hover-only with `[GamepadAccessibilityExempt]` 例外）の実装 + 保守コスト
- IL2CPP link.xml 拡張（Unity.InputSystem + Unity.InputSystem.ForUI + Game.Core + Game.Input + Generated PlayerControls）の保守コスト
- **Foundation Singleton-like access pattern の明示的容認**（H4 fix）: ADR-0001/0002/0003/0004/0005 すべて MonoBehaviour + DontDestroyOnLoad + Static Instance + interface 提供。Tier 1 で DI コンテナ統一リファクタを再評価予定、それまで test mock injection は interface 経由のみで部分的に対応
- Steam Input glyph integration が Tier 2a までないため、Tier 0/1 は generic Xbox glyph 表示固定（Steam Deck users が混乱する可能性）
- I0 / I1 spike を Tier 0 PR1 前段に追加（M6/M7 fix）— 着手タイミングが少し遅延（≈ 1-2 日）するが、初回 IL2CPP build / bleed-through 実測の早期化により後段リスクを大幅軽減
- ADR-0001 R5 revise（SwitchContext enum）依存（ADR-0004 と同要請）— 拒否時は R-K fallback path

### Neutral

- 4 Action Map の bit flags ActionMapId enum で複数 map 同時 enable 可（Pause + UI 等）。Tier 1 で必要なら Cinematic flag 追加で 5 map に拡張可能
- IInputService API は Singleton + interface 両提供（`InputService.Instance` 経由 access も `IInputService` inject 経由 access も両方サポート）

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| **R-A**: Generated C# class が IL2CPP build で reflection ストリッピングにより動作不能 | **MEDIUM** | HIGH | `Assets/link.xml` に Game.Input + Game.Core + Unity.InputSystem + Unity.InputSystem.ForUI + Generated `PlayerControls` 型を preserve（H2 fix）、**I0 Validation Gate（Tier 0 PR1 前 Spike）で初回 IL2CPP build smoke + Action callback 動作を実測**、毎 PR の CI で IL2CPP build smoke test を gate（ADR-0004 S6 と同 CI infra で実施）。M6 fix: 初回 IL2CPP build 通過まで MEDIUM、通過後 LOW に再評価 |
| **R-B**: Action Asset の Action 名変更で Generated C# class が breaking change → gameplay code 大量修正 | MEDIUM | MEDIUM | Tier 0 PR1 で Action 命名（snake_case + Display Name CamelCase）を確定し、以降の rename を禁ずる process control。Action 名変更時は migration ADR を起こす |
| **R-C**: 4 Action Map 状態遷移時の input bleed-through（map 切替の 1 frame 内に旧 map の performed event が触発） | **MEDIUM** | HIGH | **I1 Validation Gate を Tier 0 PR1 前段の Spike に格上げ（M7 fix）** — `InputActionMap.Disable()` の挙動信頼性を実機 + Editor 両方で実測（H1 fix: 旧記述の `InputAction.Reset()` 呼出は **public API 不在**のため削除）。Spike 結果次第で SetActiveMap 内に追加対策（Disable 後 1 frame の event drop 等）を Implementation Guideline に反映 |
| **R-D**: Combo Input Buffer の `IInputEventStream.Subscribe` / `Unsubscribe` の pair 不一致で memory leak | MEDIUM | LOW | `[RegisterSaveable]` 風の attribute による auto-subscribe 検討（Tier 1）、MVP では `OnEnable` / `OnDisable` pair を Roslyn analyzer で強制 |
| **R-E**: Rebinding UI で player が Pause action を unmappable にして escape 不能になる | LOW | HIGH | Pause action（Esc / Start）を `WithControlsExcluding` で reserve、Pause action は rebinding 不可とする UI 制約。Reset to Defaults ボタンを Settings UI に配備（必須） |
| **R-F**: Steam Deck native ビルドで Input System が Steam Input を認識しない | **MEDIUM** | HIGH | I5 Validation Gate に Steam Deck 実機 detection test 含む（M4 fix: **Steam Input default config = Gamepad 互換モード**で動作することを実測、Steam Input API 優先設定では Virtual Gamepad 経路）、Steam Input 認識不可時は `Steamworks.NET ISteamInput` 直接統合への fallback path を Tier 2a で用意 |
| **R-G**: `SaveBindingOverridesAsJson` format が Input System 1.8 → 1.9 / 2.0 で breaking change | LOW | HIGH | `com.unity.inputsystem` バージョンを `1.8.x` で pin（Unity 6.3 LTS 同梱版）、ADR-0004 schema migration chain 経由で format 移行（v1→v2 で rebinding clear + reset to defaults 強制も許容）。**L1 fix: CI golden file（`tests/fixtures/input/binding_overrides_v1.json`）で format hash diff を毎 PR で検出**、Input System upgrade 時の breaking change を即時警告 |
| **R-H**: hover-only Roslyn rule の false positive（Tooltip / drag handle / scroll indicator / context menu hover preview 等） | MEDIUM | LOW | `[GamepadAccessibilityExempt]` attribute（L2 fix: 旧 typo `[ToolPipShowOnHover]` を一般化命名へ修正）で analyzer rule から例外指定。例外対象は (a) Tooltip（focus 時に同情報表示で gamepad UI 不要）、(b) drag handle（gamepad では list 並べ替え UI 別経路）、(c) scroll indicator（hover で fade-in、gamepad では常時表示）、(d) context menu hover preview（gamepad では右スティック等で preview 切替）。例外申請は PR review 必須 |
| **R-I**: Legacy Input forbidden Roslyn rule が 3rd party asset（Asset Store import）に false positive を起こす | MEDIUM | LOW | analyzer rule は `Game.*` assembly のみに適用、`Assets/Plugins/`「ThirdParty/」配下は除外。CI で除外 path を明示 |
| **R-J**: `InputAction.performed` の同 frame 多重発火（同じ frame 内で複数の binding が trigger）で Combo Buffer に重複 event が流れる | LOW | MEDIUM | `InputEvent.BindingIndex` で binding 識別、Combo Buffer 側で de-dup（同一 actionName + 同一 frame は最初の event のみ採用）。I3 で検証。**M3 fix: timestamp は `(float)ctx.time`（CallbackContext.time、`double`/`Time.realtimeSinceStartup` ベース）採用で frame 内 event 順序判定に高精度** |
| **R-K**: ADR-0001 R5 revise の `SwitchContext` 追加が拒否された場合 | LOW | MEDIUM | ADR-0004 R-M と同 fallback: `ClassStateMachine.SetClassWithoutFeedback(ClassDefinition)` parallel API（drift 源、ADR-0001 owner との事前合意必須） |
| **R-L**: Foundation Singleton stance が将来 DI 統一リファクタで再評価された際の本 ADR 影響 | LOW | LOW | Tier 1 で 5 ADR 全体の DI 統一を検討、本 ADR は interface (`IInputService`) を分離済みのため refactor は internal 実装のみ、外部 consumer code 変更ゼロ |
| **R-M**: Generated C# class が `.inputactions` 編集後の "Apply" 操作忘れで stale | MEDIUM | MEDIUM | M9 fix: GitHub Actions `paths-filter` で `.inputactions` 変更時に `.cs` 同時更新を check、stale 検出で PR description に notify。Tier 1 で Unity headless ライセンス取得後に diff ベース厳密検証 |

## Performance Implications

| Metric | Tier 0 (MVP) | Tier 1 (VS) | Budget |
|--------|---------------|-------------|--------|
| Input poll (Update phase) | ~0.05 ms | ~0.10 ms | ≤ 0.3 ms / frame |
| InputEvent dispatch (per event) | ~0.02 ms | ~0.02 ms | ≤ 0.05 ms / event |
| SetActiveMap (state transition) | ~0.05 ms | ~0.05 ms | ≤ 0.20 ms / transition |
| Rebinding (interactive) | I/O-blocking N/A | I/O-blocking N/A | non-frame-budget (UI flow) |
| Memory (PlayerControls instance) | ~50 KB | ~80 KB | ≤ 1 MB |
| GC Alloc per InputEvent | 0 byte (struct) | 0 byte | 0 byte / event |

**Steam Deck performance**: I5 で `≤ 0.3 ms / frame input total` を実機検証（boss scene + 60 fps 維持）。

## Migration Plan

```
[現在] ADR-0004 Save Data System (Proposed/S1-S6) — peer relationship
            │
            ▼
   ADR-0005 I0 Spike（IL2CPP build smoke + link.xml 機能確認）
            │
            ▼
   ADR-0005 I1 Spike（Action Map bleed-through 実測）
            │
            ▼
   Tier 0 PR1（Foundation + Roslyn analyzer 配備）
        ・Game.Input.asmdef 作成、com.unity.inputsystem 1.8+ pin
        ・PlayerControls.inputactions Action Asset (4 maps + 2 schemes) 作成
        ・"Generate C# Class" で PlayerControls.cs 生成
        ・IInputService / IInputEventStream / IUIInputProxy / InputEvent / ActionMapId 定義
        ・InputService MonoBehaviour 実装 (DefaultExecutionOrder=-90)
        ・Assets/link.xml 拡張
        ・Roslyn analyzer 4 rules: Legacy Input ban / PlayerPrefs binding ban /
          InputAction direct subscribe ban / hover-only ban
        ・SettingsInputSaveable [RegisterSaveable(loadOrder=10)] 実装
        ・Unit test: SetActiveMap 状態遷移、bleed-through 不在、InputEvent dispatch
            │
            ▼
   Tier 0 PR2（UI integration + RebindingService）
        ・IUIInputProxy 実装
        ・RebindingService 実装（PerformInteractiveRebinding wrapper）
        ・Title Menu / Pause Menu / Settings Screen に UI map subscribe 配備
        ・PlayMode test: 全 UI screens の d-pad navigation 完備性 assert（I4）
        ・rebinding round-trip test: 変更 → save → quit → relaunch → load → 復元（I2）
            │
            ▼
   Tier 0 PR3（Combo Buffer integration + CI gate）
        ・Combo Input Buffer (#9) 実装、IInputEventStream 購読
        ・GitHub Actions IL2CPP build + I1-I3 round-trip test を CI gate 化（S6 と統合）
        ・5 anchor input scenarios smoke test（state transition / rebind / combo timestamp / d-pad nav / Steam Deck detection）
            │
            ▼
   Tier 2a Demo
        ・Game.SteamInput.asmdef 追加、Steamworks.NET 注入（ADR-XXXX Steam Integration）
        ・SteamInputGlyphProvider : IInputGlyphProvider 実装
        ・SteamDeckLayoutDetector 実装、Steam Deck native UI optimization
        ・Steam Deck Verified 申請準備
```

**Step-by-step**:

1. **前提（部分）**: ADR-0001 R5 revise（`SwitchContext` enum）コミット必須。**ADR-0004 Save Data System Accepted は SettingsInputSaveable のみ要求** — Input System 本体（IInputService / IInputEventStream / RebindingService）の Spike + PR1 は ADR-0004 と並列進行可（M5 fix）
2. **I0 Spike**: IL2CPP build smoke + link.xml 機能確認（M6 fix）
3. **I1 Spike**: Action Map bleed-through 実測、結果に応じて SetActiveMap 内追加対策確定（M7 fix）
4. ADR-0005 Accepted（I0-I5 通過、ADR-0004 Accepted で I2 解禁）
5. **Tier 0 PR1** (Foundation + analyzer): Game.Input.asmdef + PlayerControls Action Asset + InputService（IInputService.SaveBindings/RestoreBindings 含む）+ 4 Roslyn rules + link.xml（H2 拡張）+ Foundation Singleton ServiceLocator 配備
6. **Tier 0 PR2** (UI integration + Rebinding + SettingsInputSaveable): IUIInputProxy（or ADR-0006 確定後 defer）+ RebindingService + Title/Pause/Settings UI subscribers + SettingsInputSaveable [RegisterSaveable(10)]（**ADR-0004 Accepted 後**）+ d-pad navigation PlayMode test
7. **Tier 0 PR3** (Combo + CI): Combo Input Buffer (#9) integration + IL2CPP CI gate（ADR-0004 S6 と統合）+ 5 anchor smoke tests + non-Steam build path 検証（L3）
8. ADR-0005 Status を `Accepted` に昇格、registry の interfaces / api_decisions / forbidden_patterns / architectural_stance を確定参照化

**Rollback plan**:

- **I1 (Action Map bleed-through) 失敗の場合**: `InputAction.Reset()` を `Disable()` 後に呼ぶ自前 wrapper を InputService 内に追加。それでも未達なら 4 maps を 2 maps に縮退（Alt 4 へ下方修正）、ADR Status を Superseded
- **I2 (rebinding round-trip) 失敗の場合**: `SaveBindingOverridesAsJson` format の Input System version 違いが原因なら version pin 強化、format issue なら ADR-0004 schema migration chain で v2 へ移行
- **I5 (Steam Deck detection) 失敗の場合**: Tier 2a を待たず Steamworks.NET ISteamInput 統合を Tier 1 に前倒し（Alt 7 に近い形）

## Validation Criteria

Validation Gate **I0-I5** 全通過で `Proposed` → `Accepted` に昇格する。**I0 と I1 は Tier 0 PR1 前段の Spike として実施**（ADR-0001 R5 と同パターン）、I2-I5 は Tier 0 PR1-3 完了時に実施。

- [ ] **I0 — IL2CPP Build Smoke**（**Tier 0 PR1 前 Spike 必須**、M6 fix）: 最小限の `Game.Input.asmdef` + Generated `PlayerControls` + link.xml で IL2CPP build を実行し、Action callback が runtime で正常発火することを実機（Windows / macOS / Steam Deck native Linux いずれか 1）で実測。**`production/qa/evidence/adr-0005-i0-il2cpp-spike.md` に build ログ + 動作確認 screenshot を保管**。R-A の LOW 化条件
- [ ] **I1 — Action Map Bleed-through Spike**（**Tier 0 PR1 前 Spike 必須**、M7 fix、H1 fix）: `_controls.Gameplay.Disable() → _controls.Pause.Enable()` の同 frame 内で Gameplay map の `performed` event が **発火しない**ことを EditMode test + PlayMode test で assert。`SetActiveMap` 呼出後の 5 frame は inactive map の event が 0 件であることを verify。**`InputActionMap.Disable()` の挙動信頼性を Unity 6.3 で実測** — 失敗時は SetActiveMap 内に Disable 後 1 frame 待ち / event queue flush 等の追加対策を Implementation Guideline §SetActiveMap に反映。`production/qa/evidence/adr-0005-i1-bleed-through-spike.md` に保管
- [ ] **I2 — Rebinding Round-trip via IInputService.SaveBindings/RestoreBindings (B1 fix)**: PlayMode test で (1) Action `attack` を Z key から X key に rebind、(2) `SaveDataService.SaveAllAsync(0)` 呼出、(3) Application restart simulation（PlayMode で SettingsInputSaveable.Capture / Restore round-trip）、(4) Action `attack` の binding が X key であることを verify。**`IInputService.SaveBindings(section)` / `RestoreBindings(section)` interface 経由のみ実施を assert**（`InputService.Instance` 具象 access が unit test で不要であることを確認）
- [ ] **I3 — Combo Buffer Timestamp Accuracy (M3 fix)**: `IInputEventStream.Subscribe("attack", handler)` で受信した `InputEvent.Timestamp` が `(float)ctx.time` ベースで frame 内 event 順序判定可能であることを assert（同一 frame で複数 action 発火時、`Timestamp` 値が異なる順序で並ぶ）。`Timestamp` と `Time.unscaledTime` の差は `Time.unscaledDeltaTime` 以内であることを verify。Pause 中（Time.timeScale=0）でも `ctx.time` が進むことを verify
- [ ] **I4 — Roslyn Analyzer Enforcement**: CI build で以下のコード混入時に build error が発生することを verify:
  - `UnityEngine.Input.GetKey(KeyCode.Space)` (Legacy Input)
  - `PlayerPrefs.SetString("rebinding", json)` (PlayerPrefs binding)
  - `_controls.Gameplay.Attack.performed += OnAttack` in Game.Gameplay.asmdef (Direct subscribe)
  - `class Foo : IPointerEnterHandler { ... }` without `ISelectHandler` or `ISubmitHandler` and without `[GamepadAccessibilityExempt]` attribute (Hover-only without exempt)
- [ ] **I5 — Steam Deck Physical Verification (M4 fix)**: Steam Deck 実機（1080p、SteamOS native build）で (1) **Steam Input default config = Gamepad 互換モード**で `Gamepad.current != null` 認識、(2) Steam Input API 優先設定でも Virtual Gamepad 経路で `Gamepad.current != null` 認識、(3) **DRM-free build（`STEAM_INTEGRATION` Define なし）でも `Gamepad.current` で native HID detection が機能**（L3 fix）、(4) 全 UI screens（TitleMenu / PauseMenu / SettingsScreen）の d-pad / left-stick navigation で全要素到達 + interact 可能、(5) Input poll total ≤ 0.3 ms / frame（Profiler 実機接続）。`production/qa/evidence/adr-0005-i5-steamdeck-input.json` に Profiler ログ + screenshot 保管

## GDD Requirements Addressed

> TR-IDs は `docs/architecture/tr-registry.yaml` v2 に準拠。

| GDD Document | System | Requirement (TR-ID) | How This ADR Satisfies It |
|-------------|--------|---------------------|---------------------------|
| `design/gdd/game-concept.md` | Technical Considerations | Input System 1.8+（Steam Input 対応、Action Rebinding UI）（TR-input-001） | `com.unity.inputsystem` 1.8+ + Generated C# class + `PerformInteractiveRebinding` + Steam Input default detection で完全充足 |
| `design/gdd/game-concept.md` / technical-preferences.md | Input methods | Gamepad full / Keyboard&Mouse / 全 UI は d-pad/スティック で操作可能（TR-input-002） | 2 Control Schemes（KeyboardMouse / Gamepad）+ 4 Action Maps + IUIInputProxy + Hover-only Roslyn analyzer で完全充足 |
| `design/gdd/systems-index.md` | #9 Combo Input Buffer | コンボバッファ向け timestamp 配信、4-6f 先行入力 / 空中キャンセル / 職業間受け渡し（TR-input-003） | `IInputEventStream` で `InputEvent.Timestamp = Time.unscaledTime` を配信、Combo Input Buffer は subscriber として 4-6f buffer 管理 |
| `design/gdd/systems-index.md` | #1 Input System | Unity Input System の薄い service ラッパ（Action Rebinding / Steam Input / コンボバッファ向け timestamp 配信） | 本 ADR が IInputService + IInputEventStream + RebindingService + SettingsInputSaveable で確定 |
| `design/gdd/systems-index.md` | #10 Class Switch dependency on Input | R1/L1 1 ボタン即時切替（Pillar 1） | Gameplay map に `class_switch_left` / `class_switch_right` / `class_slot_1/2/3` actions、ClassStateMachine が `IInputEventStream.Subscribe` で購読 |
| `.claude/docs/technical-preferences.md` | Forbidden: hover-only / Steam Deck Verified | 全 UI が d-pad/stick navigable、ホバー専用禁止 | Roslyn analyzer rule + I4 Validation Gate + I5 Steam Deck 実機検証で完全充足 |
| `docs/architecture/adr-0004-save-data-system.md` | SettingsSaveable rebinding 形式 | `InputActionAsset.SaveBindingOverridesAsJson()` verbatim | SettingsInputSaveable.Capture が `_controls.SaveBindingOverridesAsJson()` を Data["rebinding"] に格納、ADR-0004 forbidden `playerprefs_for_save_data` 完全遵守 |

## Related

- **Coordinates with**: [ADR-0004 Save Data System](adr-0004-save-data-system.md)（M5 fix: peer relationship — SettingsInputSaveable のみが ADR-0004 ISaveable contract に依存、Input 本体は独立に Spike / PR1 進行可）、[ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md)（class_switch_notification の producer として ClassStateMachine.SwitchTo を呼び出す、`SwitchContext.PlayerInput` 使用 — ADR-0001 R5 revise 要請）、[ADR-0002 CharacterController2D](adr-0002-character-controller-motor.md)（motor_intent_command は ability layer 経由）、[ADR-0003 VFX System Boundary](adr-0003-vfx-system-boundary.md)（IVFXPublisher は Input から非依存）
- **Requires revise**: ADR-0001 R5 revise — `SwitchContext` enum + `SwitchTo(ClassDefinition, SwitchContext)` overload（ADR-0004 B2 と同一要請、本 ADR では Class Switch input handler が `SwitchTo(class, SwitchContext.PlayerInput)` で発火）
- **Enables**: ADR-0006 Game State Machine（IInputService.SetActiveMap で routing 駆動、Pause / Dialogue 状態の Action Map 切替を Game State 側が orchestration、ActionMapId enum 値継承）、ADR-0007 Class Abilities（IInputEventStream + Combo Input Buffer 経由で attack / dash / ability input を受信）
- **Enables (Tier 2a)**: ADR-XXXX Steam Integration（`SteamInputGlyphProvider : IInputGlyphProvider` を Game.SteamInput.asmdef 別 assembly で注入）
- **Engine reference**: `docs/engine-reference/unity/modules/input.md`（Input System 1.11+ 全 API、ただし lines 269-275 の `PlayerPrefs.SetString` 例は本 ADR で supersede）、`docs/engine-reference/unity/breaking-changes.md` lines 38-50（Legacy Input deprecation）、`docs/engine-reference/unity/deprecated-apis.md` lines 10-20（Input class deprecation table）
- **Architectural stance (new)**: registry に `architectural_stance: foundation_singleton_pattern` として追記要請（H4 fix） — ADR-0001 から ADR-0005 まで MVP 段階で MonoBehaviour + DontDestroyOnLoad + Static `Instance` + interface 提供を統一採用、Tier 1 で DI 統一リファクタを再評価
- **Implementation files (post-Accepted)**: `Assets/Input/PlayerControls.inputactions`、`Assets/Input/PlayerControls.cs`（Generated）、`src/core/input/IInputService.cs`（SaveBindings/RestoreBindings 含む）、`src/core/input/IInputEventStream.cs`、`src/core/input/IUIInputProxy.cs`（ADR-0006 確定後最終所有権検討）、`src/core/input/InputEvent.cs`、`src/core/input/ActionMapId.cs`、`src/core/GamepadAccessibilityExemptAttribute.cs`、`src/core/ServiceLocator.cs`（Foundation 5 ADR 共有）、`src/input/InputService.cs`、`src/input/InputEventStream.cs`、`src/input/RebindingService.cs`、`src/input/SettingsInputSaveable.cs`、`src/input/UIInputProxy.cs`、`Assets/link.xml`（拡張）、`tools/analyzer/InputForbiddenPatternAnalyzer.cs`、`tests/fixtures/input/binding_overrides_v1.json`（L1 golden file）
