# ADR-0007: Combo Input Buffer（Ring Buffer + IComboBuffer + ScriptableObject Window）

## Status

**Proposed (Validation Gate: CB0-CB7)**

> ADR-0005 (Input System) の IInputEventStream が Proposed 段階のため、本 ADR も同様に
> Proposed。ADR-0005 の I0-I5 通過後に本 ADR の CB0-CB7 検証を実施し、両者同時に
> Accepted に昇格を目指す。story authoring の先行参照を可能にするため、registry への
> stance 前倒し登録を実施する（ADR-0005 と同方針）。

## Date

2026-04-29

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Input / Core |
| **Knowledge Risk** | LOW — 本 ADR の実装は純 C# のリングバッファ + Unity MonoBehaviour。engine-specific API (InputAction / Physics) に依存しない |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`docs/architecture/adr-0005-input-system-architecture.md`（§7 Combo Buffer integration、§9 Pause guarantee、R5/R10/R11）、`design/gdd/systems-index.md`（#9 Combo Input Buffer、#11 Class Abilities、A1 AbilityContext）、`docs/registry/architecture.yaml`（input_event_stream contract、direct_inputaction_subscribe forbidden、foundation_singleton_pattern）|
| **Post-Cutoff APIs Used** | なし — IInputEventStream 経由で InputEvent struct を受信するのみ |
| **Verification Required** | CB0: TryConsume 単一消費動作、CB1: Window 精度境界値、CB2: ClassSwitch 持ち越し、CB3: Pause 保持・Death flush、CB6: ring buffer overflow eviction、CB7: OnDisable Unsubscribe |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0005 Input System（IInputEventStream / InputEvent / (float)ctx.time — Proposed だが interface 確定済みのため並列進行可。**§9 Pause guarantee に依存**: InputService.SetActiveMap(None) が Gameplay map を無効化し、Pause 中は新規 InputEvent が buffer に届かないことを本 ADR が前提とする。ADR-0005 の Pause 実装変更は本 ADR に影響する）|
| **Enables** | ADR-0008 Class Abilities System（AbilityContext が IComboBuffer を注入、TryConsume("attack") で先行入力消費）、ADR-0001 Class Switch Architecture R5 revise（ClassStateMachine が職業間受け渡し時に ComboBuffer を参照）|
| **Blocks** | systems-index #11 Class Abilities の story authoring（IComboBuffer interface 確定前は AbilityContext が書けない） |
| **Ordering Note** | ADR-0005 I0/I1 spike と並列実装可。ADR-0005 Accepted 後に CB0-CB7 検証を続けて実施。ADR-0008 で ClassStateMachine / AbilityExecutor の具体的 ExecutionOrder 値を確定する（R11 参照）|

## Context

### Problem Statement

Pillar 3「歯ごたえ」が要求するコンボアクション体験（4-6f 先行入力 / 空中キャンセル / 職業間受け渡し）を実現するには、Input System（ADR-0005）から配信された InputEvent を一時的にバッファリングし、Class Abilities（#11）や CharacterController2D が「先行して入力されたアクション」を後から消費できる仕組みが必要である。

ADR-0005 は §7 で Combo Input Buffer の統合方法を指定したが、下記は未確定であった：
- `IComboBuffer` interface の shape（TryConsume / Flush の signature）
- バッファのデータ構造（ring buffer / LinkedList / queue）
- バッファウィンドウの設定方法（何フレーム分か、ScriptableObject か定数か）
- クラス切替時・Pause 時・死亡時のバッファ生存/クリア方針
- `IComboBuffer` の asmdef 配置（Game.Core か Game.Gameplay か）

これらが未定義では systems-index #11 Class Abilities の story authoring ができず、Tier 0 MVP が止まる。

### Constraints

- **GC Alloc**: バッファへの push / pop は 0 byte GC alloc（固定長配列必須、LinkedList 禁止）
- **Performance**: Update フェーズの Combo Buffer 処理 ≤ 0.1ms / frame（Update ≤ 4ms envelope の 1/40）
- **Interface 配置**: `IComboBuffer` は `Game.Core.asmdef` に配置（systems-index A1 AbilityContext が参照するため）
- **Input 経路**: IInputEventStream 経由のみ。`InputAction.performed` 直接 subscribe 禁止（forbidden_pattern）
- **Timestamp**: InputEvent.Timestamp = (float)ctx.time を使用（ADR-0005 M3 fix）
- **ScriptableObject**: バッファウィンドウ秒数は Magic number 禁止、ScriptableObject で設定
- **職業間受け渡し**: クラス切替時にバッファを flush してはいけない（受け渡しが機能しなくなる）
- **Foundation Singleton**: foundation_singleton_pattern stance に従う

### Requirements

- **R1**: `IComboBuffer` interface を `Game.Core.asmdef` に配置し、`AbilityContext` / `ClassStateMachine` が注入経由で参照できるようにする
- **R2**: バッファウィンドウを 4-6f の範囲で設定可能にする（`ComboInputBufferSettings` ScriptableObject）
- **R3**: `TryConsume(actionName)` — バッファ内の最新マッチを消費（consumed フラグ = true）し true を返す。なければ false。**Latest-first セマンティクス**: 同一アクション連打時、最も新しい Timestamp のエントリを消費する（キャンセル操作感 — 直前の意図を最優先）。FIFO ではない。
- **R4**: `Flush()` — 全エントリをクリア（死亡 / リスポーン / レベルリセット時に Game State Machine が呼出）。**Game State Machine のみが呼び出すこと**。IComboBuffer インターフェースに Flush() を残すが、Game State Machine 以外からの呼出しは設計違反。`#if DEVELOPMENT_BUILD` で呼び出し元クラス名を Debug.Log に出力する実装規約を設ける。
- **R5**: クラス切替時は Flush() を呼ばない（職業間受け渡し実現）
- **R6**: Pause 中（ADR-0005 §9: InputService.SetActiveMap(None) → Gameplay map 無効化）は新規 input を受けないが既存バッファを保持する。Resume 時に持ち越し可。Pause 実装の変更は本 ADR に影響する（ADR Dependencies 参照）。
- **R7**: `IInputEventStream.Subscribe` で必要な action のみ購読（attack / dash / jump / class_switch_*)
- **R8**: `ComboInputBuffer` は `Game.Gameplay.asmdef` に配置、`IComboBuffer` interface のみを Game.Core.asmdef に
- **R9**: Unit test: CB0-CB7 全 gate を EditMode / PlayMode で自動実施
- **R10**: 内部並列3配列（`_actionNames / _timestamps / _consumed`）はすべてのスロット操作（write / prune / flush）で3配列を同時更新すること。1配列だけ更新するとインデックスずれが発生する（実装上の invariant）。
- **R11**: ComboInputBuffer を消費するすべての MonoBehaviour（ClassStateMachine、AbilityExecutor 等）は `[DefaultExecutionOrder]` 値を ComboInputBuffer(-80) より後（> -80、推奨: -60 以降）に設定すること。同フレームでのクラス切替 + TryConsume のレース防止。具体値は ADR-0008 で確定する。

## Decision

Combo Input Buffer を **固定長 Ring Buffer（循環配列）+ IComboBuffer interface + ScriptableObject 設定** で実装する。

1. `IComboBuffer` interface（`TryConsume` / `Flush` のみ）を `Game.Core.asmdef` に配置（AbilityContext 注入先）
2. `ComboInputBufferSettings` ScriptableObject でウィンドウ秒数・対象 actions を設定
3. `ComboInputBuffer : MonoBehaviour, IComboBuffer` を `Game.Gameplay.asmdef` に配置
4. Awake で `IInputEventStream.Subscribe` を設定固有 actions にのみ張る（OnDisable で Unsubscribe — DDOL オブジェクトのためシーンアンロードでは OnDisable は呼ばれない。SetActive(false) でのみ発動。通常運用では DDOL のため OnDisable は呼ばれず、Subscribe は生存し続ける設計で正しい）
5. 内部は `string[]` / `float[]` / `bool[]` 3並列固定長配列（Capacity=16）+ write pointer + プルーニング（GC ゼロ）
6. `TryConsume` は最新（Timestamp 最大）の未消費マッチを consumed = true にして返す（Latest-first セマンティクス）
7. `Flush` は Array.Clear 3配列 + `_writeIdx = 0` リセット（allocation なし）
8. Foundation Singleton-like access（`IComboBuffer Instance` + `DontDestroyOnLoad`）

### Architecture Diagram

```
┌────────────────────────────────────────┐
│ Game.Core.asmdef                       │
│   IComboBuffer               (ADR-0007)│
│   IInputEventStream          (ADR-0005)│
│   InputEvent (struct)        (ADR-0005)│
│   AbilityContext             (ADR-0008)│ ← IComboBuffer を注入
└────────────────────────────────────────┘
              ▲ implements
┌───────────────────────────────┐
│ Game.Gameplay.asmdef          │
│   ComboInputBuffer            │
│     ├─ _actionNames: string[16]
│     ├─ _timestamps:  float[16]
│     ├─ _consumed:    bool[16] │
│     ├─ _writeIdx: int         │
│     ├─ Subscribe(IInputEventStream)
│     └─ Update(): プルーニング  │
└───────────────────────────────┘
              │ subscribes
              ▼
┌───────────────────────────────┐
│ Game.Input.asmdef             │
│   InputService                │
│   └─ IInputEventStream        │
└───────────────────────────────┘

消費者（consumers of IComboBuffer）:
  AbilityExecutor (Game.Gameplay.asmdef) [DefaultExecutionOrder > -80]
    └─ AbilityContext._comboBuffer.TryConsume("attack")
  ClassStateMachine (Game.Gameplay.asmdef) [DefaultExecutionOrder > -80]
    └─ 職業間受け渡し確認: _comboBuffer.TryConsume("class_switch_right")
```

データフロー例（先行入力 — ジャンプ中の Attack）:

```
[Player] Attack ボタン押下（ジャンプ中フレーム 3）
    ↓
InputService → InputEventStream → InputEvent {ActionName="attack", Timestamp=T}
    ↓
ComboInputBuffer.OnInputReceived(evt)
  → _actionNames[_writeIdx] = "attack"
  → _timestamps[_writeIdx]  = T
  → _consumed[_writeIdx]    = false
  → _writeIdx = (_writeIdx + 1) % 16
    ↓
[Player] 着地（フレーム 7 = T + 0.067s ≒ 4f 後）
    ↓
CharacterController2D.Landed event → AbilityExecutor.OnLanded()
    ↓
AbilityExecutor: _comboBuffer.TryConsume("attack") → true（window 内）
    ↓
SwordAbility.Execute() 発動
```

### Key Interfaces

```csharp
// Game.Core.asmdef
namespace Game.Core.Input
{
    /// <summary>
    /// 先行入力バッファへのアクセス抽象。
    /// AbilityContext / ClassStateMachine が IComboBuffer 経由で先行入力を消費する。
    ///
    /// 設計ノート:
    /// - HasBuffered は存在しない。消費前の確認は TryConsume の bool 戻り値で代替する。
    ///   これにより「確認後に別 consumer が先に消費する」構造的 race を防ぐ。
    /// - null actionName はコントラクト違反。呼び出し元で検証すること。
    /// - ActionName は Unity Input System 生成 C# クラスの string 定数を使用すること（typo 防止）。
    /// </summary>
    public interface IComboBuffer
    {
        /// <summary>
        /// window 内の最新（最大 Timestamp）の未消費エントリを消費し true を返す。なければ false。
        /// Latest-first セマンティクス: キャンセル操作感 — 直前の意図を最優先。
        /// FIFO ではない。同アクション連打時、最後に押した入力が採用される。
        /// 1 回の呼出で最大 1 エントリを消費する。
        /// </summary>
        bool TryConsume(string actionName);

        /// <summary>
        /// 全エントリをクリアする。
        /// 呼出タイミング: 死亡 / リスポーン / レベルリセット — Game State Machine のみ責任を持つ。
        /// クラス切替時は呼ばない（職業間受け渡しのため）。
        /// Game State Machine 以外からの呼出しは設計違反。#if DEVELOPMENT_BUILD でログ出力すること。
        /// </summary>
        void Flush();
    }
}
```

```csharp
// Game.Gameplay.asmdef
namespace Game.Gameplay.Input
{
    [DefaultExecutionOrder(-80)]  // InputService (-90) の直後。consumers は > -80 に設定すること (R11)
    public sealed class ComboInputBuffer : MonoBehaviour, IComboBuffer
    {
        // ─── Dependencies ────────────────────────────────────────────────
        [SerializeField] private ComboInputBufferSettings _settings;
        private IInputEventStream _inputStream;

        // ─── Singleton access (foundation_singleton_pattern) ─────────────
        public static IComboBuffer Instance { get; private set; }

        // 最大バッファウィンドウ 6f × 60fps × 安全率 ≒ 12 → 2の冪乗で 16。
        // SO 化しない理由: 配列サイズ変更にはコード変更が必要なため ScriptableObject での設定は偽の柔軟性。
        private const int Capacity = 16;

        // 並列3配列: すべてのスロット操作で3配列を同時更新すること (R10)。
        // null actionName = empty slot（書込前 / プルーニング後 / Flush 後）。
        private readonly string[] _actionNames = new string[Capacity];
        private readonly float[]  _timestamps  = new float[Capacity];
        private readonly bool[]   _consumed    = new bool[Capacity];
        private int _writeIdx;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _inputStream = InputService.Instance;
            SubscribeActions();
        }

        private void SubscribeActions()
        {
            foreach (var action in _settings.BufferedActionNames)
                _inputStream.Subscribe(action, OnInputReceived);
        }

        // DontDestroyOnLoad オブジェクトのためシーンアンロードでは OnDisable は呼ばれない。
        // SetActive(false) でのみ Unsubscribe が発動する。通常運用では Subscribe は生存し続ける。
        private void OnDisable()
        {
            foreach (var action in _settings.BufferedActionNames)
                _inputStream.Unsubscribe(action, OnInputReceived);
        }

        private void OnInputReceived(InputEvent evt)
        {
            // ring buffer に書込。Capacity 超過時は最古エントリを上書きする（silent eviction）。
            // 3配列を同時更新すること (R10)。
            _actionNames[_writeIdx] = evt.ActionName;
            _timestamps[_writeIdx]  = evt.Timestamp;
            _consumed[_writeIdx]    = false;
            _writeIdx = (_writeIdx + 1) % Capacity;
        }

        private void Update()
        {
            // window を過ぎたエントリをプルーニング (R10: 3配列同時更新)。
            // Time.unscaledTime を使用: Time.timeScale=0 でも時計が進み、window が正常に機能する。
            float cutoff = Time.unscaledTime - _settings.BufferWindowSec;
            for (int i = 0; i < Capacity; i++)
            {
                if (_actionNames[i] != null && _timestamps[i] < cutoff)
                {
                    _actionNames[i] = null;
                    _timestamps[i]  = 0f;
                    _consumed[i]    = false;
                }
            }
        }

        // ─── IComboBuffer ─────────────────────────────────────────────

        public bool TryConsume(string actionName)
        {
            // Latest-first: 最新（Timestamp 最大）の未消費マッチを探す (R3)
            float cutoff  = Time.unscaledTime - _settings.BufferWindowSec;
            float best    = float.MinValue;
            int   bestIdx = -1;
            for (int i = 0; i < Capacity; i++)
            {
                if (_actionNames[i] == actionName && !_consumed[i]
                    && _timestamps[i] >= cutoff && _timestamps[i] > best)
                {
                    best    = _timestamps[i];
                    bestIdx = i;
                }
            }
            if (bestIdx < 0) return false;
            _consumed[bestIdx] = true;
            return true;
        }

        public void Flush()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[ComboBuffer] Flush called by: {new System.Diagnostics.StackFrame(1).GetMethod()?.DeclaringType?.Name ?? "Unknown"} at {Time.unscaledTime:F3}");
#endif
            // R10: 3配列を同時クリア
            System.Array.Clear(_actionNames, 0, Capacity);
            System.Array.Clear(_timestamps,  0, Capacity);
            System.Array.Clear(_consumed,    0, Capacity);
            _writeIdx = 0;
        }
    }
}
```

```csharp
// Game.Gameplay.asmdef（ScriptableObject）
namespace Game.Gameplay.Input
{
    [CreateAssetMenu(menuName = "Game/Combat/Combo Input Buffer Settings")]
    public sealed class ComboInputBufferSettings : ScriptableObject
    {
        [Tooltip("バッファウィンドウ秒数（4f ≒ 0.067s、6f ≒ 0.100s at 60fps）")]
        [SerializeField, Range(0.05f, 0.20f)]
        public float BufferWindowSec = 0.083f;  // ~5f at 60fps（デフォルト中間値）

        [Tooltip("バッファリングする ActionName リスト（Unity Input System 生成 C# クラスの string 定数を使用すること）")]
        [SerializeField]
        public string[] BufferedActionNames = new[]
        {
            "attack", "dash", "jump",
            "class_switch_left", "class_switch_right",
            "class_slot_1", "class_slot_2", "class_slot_3"
        };
    }
}
```

## Alternatives Considered

### Alternative 1: LinkedList + per-entry TTL（GC alloc あり）

- **Description**: `LinkedList<BufferedInput>` でエントリを追加、期限切れを LinkedList.Remove() で削除
- **Pros**: 可変長、ancient entry 削除が O(1)
- **Cons**: `new LinkedListNode<T>` が GC alloc を生成。hot path（InputEvent dispatch per frame）で許容できない
- **Rejection Reason**: 0 byte GC alloc 制約違反

### Alternative 2: InputAction.WasPerformedThisFrame() per-frame polling

- **Description**: AbilityExecutor が毎 Update で `_controls.Gameplay.Attack.WasPerformedThisFrame()` を呼ぶ
- **Pros**: バッファ不要、最シンプル
- **Cons**: `direct_inputaction_subscribe` forbidden pattern 違反、先行入力 window（4-6f）を実現できない（WasPerformedThisFrame は当該 frame のみ true）、職業間受け渡し不能
- **Rejection Reason**: forbidden pattern 違反 + 先行入力機能不達

### Alternative 3: InputAction.history を使用

- **Description**: Unity Input System の `InputAction.history` で過去の input を query
- **Pros**: engine built-in、実装コスト低
- **Cons**: 公開 API 保証が薄い、IInputEventStream 抽象を bypass する、history サイズ設定が per-action で統一困難
- **Rejection Reason**: IInputEventStream 抽象 bypass + API 安定性懸念

### Alternative 4: Global Action Event Bus（全 action を broadcast、フィルタなし）

- **Description**: `IInputEventStream.InputReceived` global event を購読、全 InputEvent を受信
- **Pros**: Subscribe 1 回で済む
- **Cons**: UI map / Pause map の input もバッファに混入するリスク、capacity を無駄に消費
- **Rejection Reason**: アクション限定購読の方が意図明確で安全

### Alternative 5: HasBuffered + TryConsume の 2-step API

- **Description**: `HasBuffered(actionName)` で確認後に `TryConsume(actionName)` で消費
- **Pros**: 「消費前に確認したい」ユースケース（ClassStateMachine の HUD 表示など）に対応できる
- **Cons**: HasBuffered と TryConsume の間に別 consumer が消費した場合、HasBuffered の結果と TryConsume の結果が食い違う論理的 race が発生する。これを「1 action = 1 consumer 規律」で防ぐのはコード上強制できない。
- **Rejection Reason**: 構造的 race リスク。ClassStateMachine の確認ユースケースも TryConsume の bool 戻り値で代替可能（必要なら ADR-0008 で HasBuffered を再追加できる）

## Consequences

### Positive

- `IComboBuffer` interface（TryConsume / Flush）が `Game.Core.asmdef` に配置されることで、AbilityContext を通じた疎結合な先行入力消費が実現
- `HasBuffered` を排除したシンプルな interface により、構造的 race が不可能になる
- 固定長 ring buffer（16 entries）で GC alloc ゼロ、Update フェーズ ≤ 0.1ms
- ScriptableObject 設定によりデザイナーがコード変更なしにウィンドウ調整可能
- 職業間受け渡しが構造的に実現（Flush() の呼び出しタイミングを Game State Machine に集約）
- ADR-0005 の `direct_inputaction_subscribe` forbidden pattern を遵守

### Negative

- `HasBuffered` を排除したため、「消費せずに確認だけ」したい将来のユースケースには TryConsume の bool 戻り値で対応するか、ADR-0008 で interface を拡張する必要がある
- ring buffer capacity (16) はコード定数。変更には配列宣言のコード変更が必要（ScriptableObject では設定不可 — これは意図的な選択。SO 化は偽の柔軟性）。
- Pause 中のバッファ保持は ADR-0005 §9 の SetActiveMap 実装に依存する。ADR-0005 の Pause 実装変更時は本 ADR も影響を受ける（ADR Dependencies に明記済み）
- `Flush()` が IComboBuffer インターフェースに露出しているため、Game State Machine 以外のコードも呼び出せる。設計違反の検出は `#if DEVELOPMENT_BUILD` ログに依存する。

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| ring buffer capacity 不足（16 が少ない） | LOW | MEDIUM | CB6 で overflow eviction を検証。超過時は const int Capacity = 16 をコード変更で対処 |
| 同フレーム cross-class TryConsume race（C1） | LOW | HIGH | R11: consumers の ExecutionOrder > -80 を強制。ADR-0008 で具体値を確定する |
| 並列3配列のインデックスずれ | LOW | HIGH | R10: 3配列同時更新を invariant として文書化。実装レビューで確認 |
| Pause 中に Gameplay input が誤って蓄積 | LOW | LOW | ADR-0005 §9 が保証。ADR-0005 変更時は本 ADR も再検討 |
| Flush() 誤用（GSM 以外から呼出） | LOW | MEDIUM | #if DEVELOPMENT_BUILD ログで呼び出し元を記録。registry forbidden_pattern に登録済み |
| window 秒数の float 誤差 | LOW | LOW | >= 比較で境界安全。2時間超プレイで 1ms 誤差が発生しうるが実用上無影響 |

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/systems-index.md` | #9 Combo Input Buffer | 4-6f 先行入力 / 空中キャンセル / 職業間受け渡し | BufferWindowSec = 0.083s (≒5f)、Flush() 不呼出によるクラス跨ぎ保持 |
| `design/gdd/systems-index.md` | #11 Class Abilities System (A1) | AbilityContext が IComboBuffer を含む | IComboBuffer を Game.Core.asmdef に配置、AbilityContext に注入 |
| `docs/architecture/adr-0005-input-system-architecture.md` | §7 Combo Buffer integration | IInputEventStream.Subscribe + Unsubscribe pair | ComboInputBuffer.Awake/OnDisable で pair 実装 |
| `.claude/docs/technical-preferences.md` | Magic numbers 禁止 | バランス値は ScriptableObject | ComboInputBufferSettings.BufferWindowSec をチューニング可能 |

## Performance Implications

| Metric | Value | Budget |
|--------|-------|--------|
| OnInputReceived（hot path） | O(1)、0 byte GC alloc | 0 byte / event |
| Update（プルーニング） | O(16) = O(1) | ≤ 0.05 ms / frame |
| TryConsume | O(16) = O(1) | ≤ 0.02 ms / call |
| Memory（ring buffer） | 16 × (string ref 8B + float 4B + bool 1B) ≒ 208 byte + string header | < 1 KB |

## Migration Plan

```
[現在] ADR-0005 Input System (Proposed, I0-I5 Spike 待ち)
            │ IInputEventStream 確定済み
            ▼
   ADR-0007 CB0-CB1 EditMode Unit Tests（ring buffer boundary, window accuracy）
            │
            ▼
   Tier 0 PR3（ADR-0005 Migration Plan 参照）
        ・ComboInputBuffer 実装（IComboBuffer in Game.Core.asmdef）
        ・ComboInputBufferSettings SO 作成 + prefab に SerializeField
        ・CB0-CB7 全 gate を CI PlayMode test に追加
        ・ADR-0005 PR3 の ComboBuffer integration と同 PR で実施
            │
            ▼
   ADR-0008 Class Abilities（IComboBuffer を AbilityContext に注入、ExecutionOrder 値確定）
```

## Validation Criteria

Validation Gate **CB0-CB7** 全通過で `Proposed` → `Accepted` に昇格。

- [ ] **CB0 — TryConsume 消費動作**: EditMode test で同一 action を 2 回 TryConsume → 1 回目 true、2 回目 false（同じエントリを 2 度消費しない）
- [ ] **CB1 — Window 精度（境界値）**: Timestamp = T で push、`Time.unscaledTime = T + window - 0.001s` で TryConsume → true、`T + window + 0.001s` → false。`>=` 比較で cutoff = unscaledTime - window
- [ ] **CB2 — 職業間受け渡し**: "attack" を push 後 ClassStateMachine.SwitchTo() 呼出（Flush 呼ばない）→ 次クラスの AbilityExecutor が TryConsume("attack") = true で取得できること
- [ ] **CB3 — Pause 保持 / Death flush**: Pause 中（InputService.SetActiveMap(ActionMapId.None) 後）は新規 push なし。Flush() 呼出後は全 TryConsume → false
- [ ] **CB4 — パフォーマンス**: Profiler EditMode で TryConsume 1000 回 / frame → p99 ≤ 0.1ms total。GC.Alloc = 0 byte を verify
- [ ] **CB5 — 先行入力統合（PlayMode）**: CharacterController2D.Airborne 中に "attack" push → Landed event 後に AbilityExecutor.TryConsume("attack") = true で ability 発動。ウィンドウ外（BufferWindowSec + 0.1s 後）では false
- [ ] **CB6 — Ring Buffer Overflow**: 17 連打（Capacity=16 超過）→ 最古エントリが上書きされ TryConsume が既上書きエントリに対して false を返す（eviction されたエントリは消費不可）
- [ ] **CB7 — OnDisable Unsubscribe**: ComboInputBuffer コンポーネントを SetActive(false) → Unsubscribe が完了し、その後の InputEvent が buffer に書き込まれないことを確認

## Related

- **Depends on**: [ADR-0005 Input System](adr-0005-input-system-architecture.md)（IInputEventStream / InputEvent / (float)ctx.time / §9 Pause guarantee）
- **Enables**: ADR-0008 Class Abilities System（AbilityContext.IComboBuffer、ExecutionOrder 値確定）
- **Referenced by**: [ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md)（職業間受け渡し）
- **Engine reference**: [docs/engine-reference/unity/VERSION.md](../../docs/engine-reference/unity/VERSION.md)
- **Implementation files (post-Accepted)**:
  - `src/core/input/IComboBuffer.cs`（IComboBuffer interface）
  - `src/gameplay/input/ComboInputBuffer.cs`（MonoBehaviour 実装）
  - `assets/data/input/ComboInputBufferSettings.asset`（ScriptableObject instance）
  - `src/gameplay/input/ComboInputBufferSettings.cs`（ScriptableObject 定義）
  - `tests/unit/input/ComboInputBufferTests.cs`（CB0-CB7 EditMode tests）
