# ADR-0008: Class Abilities System（AbilityContext DI + ClassAbilityData SO + Poll-based AbilityExecutor）

## Status

**Proposed (Validation Gate: CA0-CA8)**

> ADR-0001 (ClassStateMachine) / ADR-0002 (ICharacterMotor) / ADR-0003 (IVFXPublisher) /
> ADR-0007 (IComboBuffer) が全て Proposed のため、本 ADR も Proposed。これら依存 ADR の
> 検証 gate 通過後に本 ADR の CA0-CA8 を実施し、全依存 ADR と同時に Accepted 昇格を目指す。
> story authoring の先行参照を可能にするため、registry への stance 前倒し登録を実施する。

## Date

2026-04-29

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Scripting / Core |
| **Knowledge Risk** | LOW — 本 ADR の実装は純 C# + MonoBehaviour。engine-specific API は `Physics2D.OverlapBoxNonAlloc`（hitbox 検出）と `Animator.SetTrigger`（animation coupling）のみ。両 API は Unity 2020 以降 stable であり Unity 6.3 での変更なし |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`docs/architecture/adr-0001-class-switch-architecture.md`（ClassDefinition / AbilityExecutor.Configure forward ref）、`docs/architecture/adr-0002-character-controller-motor.md`（ICharacterMotor interface）、`docs/architecture/adr-0003-vfx-system-boundary.md`（IVFXPublisher / VfxCueDefinition / VfxCueArgs）、`docs/architecture/adr-0007-combo-input-buffer.md`（IComboBuffer.TryConsume / ExecutionOrder R11）、`design/gdd/systems-index.md`（#11 Class Abilities、A1 三分割、A2 双方向結合禁止、CD4 Anti-Pillar guard） |
| **Post-Cutoff APIs Used** | なし — Physics2D.OverlapBoxNonAlloc / Animator.SetTrigger / ScriptableObject はすべて stable API |
| **Verification Required** | CA4: Physics2D.OverlapBoxNonAlloc が hitbox active frames のみで呼ばれること / CA6: ExecutionOrder チェーン（ComboInputBuffer -80 → AbilityExecutor -60）の順序保証 / CA8: HitConfirmed 発火後に Configure() しても HitData は取り消されないこと |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (ClassStateMachine — `Configure(ClassAbilityData[])` forward ref を本 ADR が実現)、ADR-0002 (ICharacterMotor — `AbilityContext.Motor` フィールド + `MotorState` enum + `Facing` enum)、ADR-0003 (IVFXPublisher + VfxCueDefinition + VfxCueArgs — `AbilityContext.VFX` フィールド)、ADR-0007 (IComboBuffer — `AbilityContext.ComboBuffer` / TryConsume poll / ExecutionOrder R11 > -80)。全て Proposed だが interface 確定済みのため並列進行可 |
| **Enables** | Combat System ADR（`HitConfirmed event + HitData struct`  経由で IDamageReceiver bridge を確立。ADR-0008 Accepted 後に Combat System の story authoring が可能）、Gate & Lock ADR (#18、`HasCapability(AbilityCapability)` で ability-gated ドア解放判定） |
| **Blocks** | `design/gdd/class-abilities-system.md` GDD authoring（ClassAbilityData / AbilityContext フィールド定義前は authoring 不能）、`design/gdd/combat-system.md` story authoring（HitConfirmed event signature 確定前は Combat System の damage pipeline が設計できない） |
| **Ordering Note** | ADR-0001 R5 spike（SpriteLibrary 実機確認）と並列実装可。ADR-0007 の CB0-CB5 spike 前でも IComboBuffer interface が確定済みのため本 ADR のドラフトは進行可。Combat System ADR は本 ADR Accepted 後に着手する。ADR-0005 §9 Pause guarantee（Gameplay map 無効化 → ComboBuffer に新規 push なし）に間接依存 |

## Context

### Problem Statement

Pillar 3「歯ごたえ」が要求するコンボアクション体験（ground slash → air slash → cross-class cancel）を実現するには、Class Switch System（ADR-0001）が依頼する `AbilityExecutor.Configure(ClassAbilityData[])` の具体的な実装と、先行入力バッファ（ADR-0007 IComboBuffer）を消費して ability を発動するロジックが必要である。

ADR-0001 は §Key Interfaces で `AbilityContext` の存在を forward reference したが下記を未確定とした：
- `AbilityContext` の具体フィールド（ICharacterMotor / IComboBuffer / IVFXPublisher / IAudioPublisher の注入方法）
- `ClassAbilityData ScriptableObject` のフィールド構造（hitbox frame data / combo chain 参照 / motor impulse 定義）
- `IAbilityExecutor` interface の shape（Configure の引数型 + Initialize の注入パターン）
- Combo chain の定義方法（attack1 → attack2 の連鎖を SO でどう表現するか）
- ヒットボックス発動タイミング（animation event 駆動 vs. frame data + OverlapBoxNonAlloc poll）
- `ExecutionOrder` 値の確定（ADR-0007 R11: > -80 必須）
- Gate & Lock (#18) のための能力問い合わせ API

これらが未定義では Combat System (#12) と Gate & Lock (#18) の story authoring が進められず、Tier 0 MVP が止まる。

### Constraints

- **GC Alloc**: AbilityExecutor.Update() の hot path（毎フレーム TryConsume + OverlapBoxNonAlloc）は 0 byte GC alloc 目標。`Physics2D.OverlapBoxNonAlloc` + `HashSet<Collider2D>` の Clear() を使用
- **Performance**: AbilityExecutor per-frame ≤ 0.2ms（Update ≤ 4ms envelope の 1/20）
- **ExecutionOrder**: AbilityExecutor は `[DefaultExecutionOrder(-60)]` — ADR-0007 R11「consumers は > -80 (ComboInputBuffer)」を満たし、InputAction callback での Configure 呼出し後に TryConsume poll が走る
- **Interface 配置**: `IAbilityExecutor` / `AbilityContext` / `ClassAbilityData` / `AbilityCapability` / `IAudioPublisher` / `AudioCueDefinition` / `AudioCueArgs` / `HitData` はすべて `Game.Core.asmdef` に配置。`AbilityExecutor` / `NullAudioPublisher` は `Game.Gameplay.asmdef`
- **状態制約チェック優先**: `IsStateValid(ability.StateRequirement) && TryConsume(ability.ActionName)` の短絡評価で、state 不一致の場合は TryConsume を呼ばない（バッファエントリを無駄に消費しない）
- **Ability キャンセル on class switch**: `Configure()` 呼出し時に実行中 ability は即座にキャンセル（ComboNext チェーン中でもキャンセル）
- **God Object 禁止 (A1)**: ClassAbilityData は純データのみ。ロジックは AbilityExecutor が持つ
- **Anti-Pillar guard (CD4)**: ClassAbilityData にアビリティアンロックツリーや強化スロットを追加してはいけない（本 ADR 範囲外）
- **능력空間制約**: ClassAbilityData の現フィールド集合がカバーする能力空間（移動 Impulse / 重力 Override / 水平制御 Lock / Hitbox detection / Combo chain）の範囲外の能力（テレポート / グラビティ反転 / 時間操作等）を Tier 3 クラス4で導入する場合は本 ADR の改訂が前提

### Requirements

- **R1**: `AbilityContext` を `Game.Core.asmdef` に配置し、`IAbilityExecutor.Initialize(AbilityContext)` で注入
- **R2**: `ClassAbilityData (ScriptableObject)` を `Game.Core.asmdef` に配置（`ClassDefinition.Abilities: ClassAbilityData[]` が参照するため）
- **R3**: `IAbilityExecutor` interface を `Game.Core.asmdef` に配置（`ClassStateMachine._abilityExecutor: IAbilityExecutor` 型参照のため）
- **R4**: `AbilityExecutor (MonoBehaviour)` を `Game.Gameplay.asmdef` に配置し `[DefaultExecutionOrder(-60)]` を付与
- **R5**: `PollForNewAbility()` で状態チェックを TryConsume より前に置く（短絡評価でバッファ無駄消費を防止）
- **R6**: `ClassAbilityData.ComboNext: ClassAbilityData` による SO チェーンでコンボを定義（MVP 範囲は最大 2 深度）。`ComboNext != _currentAbility` ガードで循環参照ループを防止（Unityオブジェクト比較は `==` を使用、`is null` 禁止）
- **R7**: `Physics2D.OverlapBoxNonAlloc` を hitbox active frames（`HitboxActiveStartFrame ≤ _currentFrame ≤ HitboxActiveEndFrame`）のみ実行（非 active frames はスキップ）
- **R8**: `IAudioPublisher` は `PlayCue(AudioCueDefinition, AudioCueArgs)` 形式で `Game.Core.asmdef` に定義。`NullAudioPublisher` で Tier 0 実装（PlayCue = no-op）。Tier 1 で Audio System ADR が実装を差し替える
- **R9**: `AbilityExecutor.HitConfirmed: event Action<HitData>` を公開し、Combat System が subscribe して damage pipeline を実行（Tier 0 では subscriber なし）
- **R10**: `Configure()` が呼ばれたとき実行中の ability を即座にキャンセルし `_currentAbility = null; _currentFrame = 0;` にリセット。**HitConfirmed 発火後に Configure() された場合、当該 HitData は確定**（取り消されない）。Combat System は「受け取った HitConfirmed は取り消されない」前提で実装する
- **R11**: `_abilities[]` 配列は最も状態特異的（状態制約が厳しい）ability を先頭に配置する（例: Grounded 限定攻撃 → Airborne 限定攻撃 → Any 汎用攻撃の順）。`ClassDefinition.Abilities` のインスペクタ配置規律として明記
- **R12**: `_hitThisExecution: HashSet<Collider2D>` で per-execution 重複ヒット防止（同一 ability 実行内で同一ターゲットを最大 1 回のみ HitConfirmed）。`StartAbility()` で Clear()
- **R13**: `AbilityCapability` flags enum を `Game.Core.asmdef` に配置し `IAbilityExecutor.HasCapability(AbilityCapability)` で能力問い合わせ。Gate & Lock は ActionName 文字列ではなく AbilityCapability で判定する
- **R14**: `Configure()` 呼出し時に `_animatorHashCache[]` を更新し `Animator.StringToHash(ability.AnimatorTriggerKey)` を事前計算キャッシュ（Animator.SetTrigger(int hash) 経由で呼出し）

## Decision

Class Abilities System を **Monolithic poll-based AbilityExecutor + ClassAbilityData SO + AbilityContext DI** で実装する。

1. `AbilityContext` plain class を `Game.Core.asmdef` に配置（4 interface の DI container）
2. `IAudioPublisher` interface を `Game.Core.asmdef` に定義。`PlayCue(AudioCueDefinition, AudioCueArgs)` 形式で ADR-0003 IVFXPublisher と対称化
3. `AudioCueDefinition : ScriptableObject` を `Game.Core.asmdef` に配置（Tier 0 ではフィールド空。Audio System ADR が内容を追加）
4. `AbilityCapability` flags enum を `Game.Core.asmdef` に配置
5. `ClassAbilityData : ScriptableObject` を `Game.Core.asmdef` に配置（純データ: hitbox frame data / combo chain ref / motor impulse / capability flags / VFX-SFX cue / animation hash）
6. `IAbilityExecutor` interface を `Game.Core.asmdef` に配置（`Initialize` + `Configure` + `HasCapability` の 3 メソッド）
7. `HitData` readonly struct を `Game.Core.asmdef` に配置（Combat System bridge payload。`SourceAbility` 参照で Combat System が逆引き不要）
8. `AbilityExecutor : MonoBehaviour, IAbilityExecutor` を `Game.Gameplay.asmdef` に配置（`[DefaultExecutionOrder(-60)]`）
9. `AbilityExecutor.Update()` が `TryConsume(ability.ActionName)` を poll（state check 先行 → 短絡評価）
10. Combo chain は `ClassAbilityData.ComboNext` SO 参照チェーン + `CancelWindowStartFrame / CancelWindowEndFrame`
11. ヒットボックスは `HitboxActiveStartFrame / HitboxActiveEndFrame` + `Physics2D.OverlapBoxNonAlloc` を active frames のみ呼出し + `_hitThisExecution HashSet` で重複ヒット防止
12. `NullAudioPublisher : IAudioPublisher` を `Game.Gameplay.asmdef` に Tier 0 stub として配置
13. `PlayerBootstrap` が Awake で `AbilityContext` を構築し `AbilityExecutor.Initialize()` に注入

### Architecture Diagram

```
┌──────────────────────────────────────────────────────┐
│ Game.Core.asmdef                                      │
│   IAbilityExecutor          (ADR-0008)                │
│   AbilityContext             (ADR-0008)               │
│     ├─ Motor:       ICharacterMotor  (ADR-0002)       │
│     ├─ ComboBuffer: IComboBuffer     (ADR-0007)       │
│     ├─ VFX:         IVFXPublisher    (ADR-0003)       │
│     └─ Audio:       IAudioPublisher  (ADR-0008 stub)  │
│   ClassAbilityData (ScriptableObject) (ADR-0008)      │
│   AbilityCapability (Flags enum)      (ADR-0008)      │
│   IAudioPublisher  (placeholder)      (ADR-0008)      │
│   AudioCueDefinition (ScriptableObject)(ADR-0008)     │
│   AudioCueArgs (struct)               (ADR-0008)      │
│   HitData (readonly struct)           (ADR-0008)      │
└──────────────────────────────────────────────────────┘
              ▲ implements
┌──────────────────────────────────────────────────────┐
│ Game.Gameplay.asmdef                                  │
│   AbilityExecutor [DefaultExecutionOrder(-60)]        │
│     ├─ _context: AbilityContext                       │
│     ├─ _abilities: ClassAbilityData[]                 │
│     ├─ _currentAbility: ClassAbilityData              │
│     ├─ _currentFrame: int                             │
│     ├─ _hitBuffer: Collider2D[8] (reuse array)        │
│     ├─ _hitThisExecution: HashSet<Collider2D>         │
│     ├─ _animatorHashCache: int[] (ability 毎)         │
│     ├─ Initialize(AbilityContext)                     │
│     ├─ Configure(ClassAbilityData[])                  │
│     ├─ HasCapability(AbilityCapability)               │
│     ├─ Update(): PollForNewAbility() / Tick()         │
│     └─ HitConfirmed: event Action<HitData>            │
│   NullAudioPublisher : IAudioPublisher (Tier 0 stub)  │
└──────────────────────────────────────────────────────┘

依存関係（Consumers → Providers）:
  AbilityExecutor → IComboBuffer    (TryConsume poll)
  AbilityExecutor → ICharacterMotor (RequestImpulse 等の motor commands)
  AbilityExecutor → IVFXPublisher   (PlayCue on start / on hit)
  AbilityExecutor → IAudioPublisher (PlayCue — Tier 1 active)
  ClassStateMachine → IAbilityExecutor (Configure on class switch, ADR-0001)
  PlayerBootstrap → AbilityExecutor  (Initialize with AbilityContext)
  CombatSystem (future) → AbilityExecutor.HitConfirmed (subscribe)
  Gate & Lock (#18) → IAbilityExecutor.HasCapability(AbilityCapability)

ExecutionOrder チェーン（同一 frame での処理順）:
  InputService     (-90) → InputEvent dispatch → ComboInputBuffer.OnInputReceived callback
  ComboInputBuffer (-80) → Update(): expired entry pruning
  ClassStateMachine (InputAction callback timing — fires before any Update())
    └─ SwitchTo() → AbilityExecutor.Configure(newAbilities) [immediate cancel]
  AbilityExecutor  (-60) → Update(): PollForNewAbility() or TickCurrentAbility()

HitConfirmed + Configure() レース契約:
  「HitConfirmed が発火した後に Configure() が呼ばれても、当該 HitData は確定。
   Combat System は受け取った HitConfirmed を取り消されないものとして処理する。
   Configure() より前に HitboxActive で未検出のものは破棄される（次フレーム以降の
   OverlapBoxNonAlloc は呼ばない）。この挙動は格闘ゲーム的に正しい:
   attack-cancel したら当たっていない攻撃は無効。」

データフロー例（ground combo: attack1 → attack2）:
  [Frame N] AbilityExecutor.PollForNewAbility():
    IsStateValid(GroundAttack1.StateRequirement=Grounded) = true
    TryConsume("attack") = true → StartAbility(GroundAttack1)
  [Frame N+8] TickCurrentAbility(): in CancelWindow (frame 8-18)
    IsStateValid(GroundAttack2.StateRequirement=Grounded) = true
    TryConsume("attack") for ComboNext(GroundAttack2) = true
    → StartAbility(GroundAttack2) [combo executed]

データフロー例（RequiredState による先行入力温存）:
  [Frame N, Airborne] Player presses Attack (airborne)
    → ComboInputBuffer buffers "attack"
    → PollForNewAbility(): GroundAttack1.StateRequirement=Grounded ≠ Airborne
      → IsStateValid = false → TryConsume NOT called → buffer entry preserved
    → AirAttack.StateRequirement=Airborne = true → TryConsume("attack") = true
    → StartAbility(AirAttack)
```

### Key Interfaces

```csharp
// Game.Core.asmdef — namespace Game.Core.Abilities

/// <summary>
/// ClassAbilityData が付与できる Metroidvania 能力フラグ。
/// Gate & Lock は AbilityCapability でドア解放条件を指定し ActionName 文字列に依存しない。
/// 4職目追加時、新クラスの Dash 相当能力に Dash フラグを付与するだけで
/// Gate & Lock のレベルデータ変更は不要。
/// </summary>
[Flags]
public enum AbilityCapability
{
    None        = 0,
    Dash        = 1 << 0,   // 短距離高速移動系
    DoubleJump  = 1 << 1,   // 空中追加ジャンプ
    WallCling   = 1 << 2,   // 壁張り付き・壁登り
    HighJump    = 1 << 3,   // 通常より高い跳躍
    GroundPound = 1 << 4,   // 下方向強力攻撃・破壊
}

/// <summary>
/// PlayerBootstrap が Awake で構築し AbilityExecutor に注入する DI container。
/// 将来 IAnimatorPublisher が 5 番目のフィールドとして追加される余地あり。
/// </summary>
public sealed class AbilityContext
{
    public ICharacterMotor Motor      { get; }  // ADR-0002
    public IComboBuffer    ComboBuffer { get; } // ADR-0007
    public IVFXPublisher   VFX        { get; } // ADR-0003
    public IAudioPublisher Audio      { get; } // ADR-0008 stub; Audio System ADR で実装差替え

    public AbilityContext(
        ICharacterMotor motor,
        IComboBuffer    comboBuffer,
        IVFXPublisher   vfx,
        IAudioPublisher audio)
    {
        Motor       = motor;
        ComboBuffer = comboBuffer;
        VFX         = vfx;
        Audio       = audio ?? NullAudioPublisher.Instance;
    }
}

/// <summary>
/// Audio サービス抽象。ADR-0003 の IVFXPublisher と対称。
/// Tier 0 では NullAudioPublisher (no-op)。Audio System ADR が本実装を提供。
/// </summary>
public interface IAudioPublisher
{
    void PlayCue(AudioCueDefinition cue, AudioCueArgs args);
}

/// <summary>
/// Audio cue データ定義（ADR-0003 の VfxCueDefinition と対称）。
/// Tier 0 ではフィールド空。Audio System ADR が中身を追加する。
/// </summary>
[CreateAssetMenu(menuName = "Game/Audio/Audio Cue Definition")]
public sealed class AudioCueDefinition : ScriptableObject { }

/// <summary>AudioCue 実行引数（ADR-0003 の VfxCueArgs と対称）。</summary>
public readonly struct AudioCueArgs
{
    public readonly Vector2 WorldPosition;
    // Tier 1: Pitch / Volume variation 追加余地

    public AudioCueArgs(Vector2 worldPosition) { WorldPosition = worldPosition; }
}

/// <summary>AbilityExecutor との ClassStateMachine 結合点。</summary>
public interface IAbilityExecutor
{
    /// <summary>PlayerBootstrap が Awake で 1 回呼ぶ。DI 注入。</summary>
    void Initialize(AbilityContext context);

    /// <summary>
    /// ClassStateMachine.SwitchTo() が InputAction callback 内で呼ぶ。
    /// 実行中 ability をキャンセルし新しい ability セットで再設定する。
    /// HitConfirmed 発火後の Configure() でも当該 HitData は確定（取り消されない）。
    /// </summary>
    void Configure(ClassAbilityData[] abilities);

    /// <summary>
    /// Gate &amp; Lock (#18) 等から参照。指定 capability を持つ ability が設定済みか判定。
    /// ActionName 文字列ではなく AbilityCapability enum で問い合わせることで
    /// 4職目追加時に Gate &amp; Lock のレベルデータ変更が不要になる。
    /// </summary>
    bool HasCapability(AbilityCapability capability);
}

/// <summary>
/// HitConfirmed event のペイロード。Combat System が IDamageReceiver bridge に使用。
/// SourceAbility 参照により Combat System は ClassAbilityData から直接
/// ダメージ / Hitstop / Knockback 値を読み、ActionName 逆引きレジストリが不要。
/// </summary>
public readonly struct HitData
{
    public readonly Vector2          WorldPosition;  // ヒット発生位置
    public readonly string           ActionName;     // ability の actionName
    public readonly Vector2          HitNormal;      // 撃った方向の法線（knockback 方向算出に使用）
    public readonly Collider2D       HitCollider;    // ヒットした Collider2D
    public readonly ClassAbilityData SourceAbility;  // ダメージ / Hitstop / Knockback 参照元 SO
    public readonly Vector2          AttackerFacing; // 攻撃者の Facing 方向（Vector2.right / left）

    public HitData(
        Vector2          pos,
        string           actionName,
        Vector2          hitNormal,
        Collider2D       collider,
        ClassAbilityData sourceAbility,
        Vector2          attackerFacing)
    {
        WorldPosition  = pos;
        ActionName     = actionName;
        HitNormal      = hitNormal;
        HitCollider    = collider;
        SourceAbility  = sourceAbility;
        AttackerFacing = attackerFacing;
    }
}
```

```csharp
// Game.Core.asmdef
namespace Game.Core.Abilities
{
    /// <summary>
    /// 1 アビリティの純データ定義。ロジックは AbilityExecutor が持つ (A1 三分割)。
    /// Magic numbers 禁止 — すべての調整値はこの SO フィールドで管理。
    /// ClassDefinition.Abilities[] の配列順序ルール (R11):
    ///   先頭 → 末尾の順で「より状態特異的 (state-specific)」を先に置くこと。
    ///   例: GroundAttack (Grounded) → AirAttack (Airborne) → DashAttack (Any)
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Ability/Class Ability Data")]
    public sealed class ClassAbilityData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("IComboBuffer の actionName と一致させること（例: attack, dash, jump）")]
        public string ActionName = "attack";

        [Tooltip("実行可能な MotorState。Any = 全状態で実行可")]
        public RequiredMotorState StateRequirement = RequiredMotorState.Any;

        [Tooltip("この ability が持つ MetroidVania 能力フラグ（Gate & Lock 判定に使用）")]
        public AbilityCapability Capabilities = AbilityCapability.None;

        [Header("Execution Timing (frames at 60fps)")]
        [Tooltip("ability の総継続フレーム数")]
        public int TotalDurationFrames = 24;

        [Tooltip("コンボ入力受付開始フレーム（ComboNext != null の場合のみ有効）")]
        public int CancelWindowStartFrame = 8;

        [Tooltip("コンボ入力受付終了フレーム")]
        public int CancelWindowEndFrame = 18;

        [Header("Combo Chain")]
        [Tooltip("コンボ続行先 ability。null = コンボなし。最大 2 段（ComboNext の ComboNext まで）。" +
                 "循環参照 (A.ComboNext = B, B.ComboNext = A) 禁止 — 実行時に無限ループする。")]
        public ClassAbilityData ComboNext = null;

        [Header("Hitbox")]
        [Tooltip("プレイヤー Position からのオフセット（AbilityExecutor が Facing 方向に反転）")]
        public Vector2 HitboxOffset = new Vector2(0.5f, 0f);

        [Tooltip("ヒットボックスのサイズ（幅 × 高さ）")]
        public Vector2 HitboxSize = new Vector2(1.0f, 0.8f);

        [Tooltip("hitbox active 開始フレーム")]
        public int HitboxActiveStartFrame = 4;

        [Tooltip("hitbox active 終了フレーム")]
        public int HitboxActiveEndFrame = 8;

        [Header("Motor Commands")]
        [Tooltip("ability 発動時の即時インパルス（Vector2.zero = なし）")]
        public Vector2 LaunchImpulse = Vector2.zero;

        [Tooltip("重力オーバーライド倍率（0 = なし）")]
        [Range(0f, 2f)]
        public float GravityOverrideMultiplier = 0f;

        [Tooltip("重力オーバーライド継続秒数")]
        [Range(0f, 1f)]
        public float GravityOverrideSec = 0f;

        [Tooltip("水平入力ロック秒数（0 = なし）")]
        [Range(0f, 0.5f)]
        public float LockHorizontalSec = 0f;

        [Header("VFX / SFX")]
        [Tooltip("ability 発動時 VFX（null = なし）")]
        public VfxCueDefinition VfxOnStart = null;

        [Tooltip("ヒット確定時 VFX（null = なし）")]
        public VfxCueDefinition VfxOnHit = null;

        [Tooltip("ability 発動時 Audio cue（null = なし）")]
        public AudioCueDefinition SfxOnStart = null;

        [Tooltip("ヒット確定時 Audio cue（null = なし）")]
        public AudioCueDefinition SfxOnHit = null;

        [Header("Animation")]
        [Tooltip("Animator.SetTrigger に渡すキー（空 = Animator 呼出しなし）。" +
                 "AbilityExecutor.Configure() 時に Animator.StringToHash でキャッシュされる。")]
        public string AnimatorTriggerKey = string.Empty;
    }

    /// <summary>ClassAbilityData.StateRequirement で使用するモータ状態制約。</summary>
    public enum RequiredMotorState
    {
        Any       = 0,
        Grounded  = 1,
        Airborne  = 2,
        WallSlide = 3,
    }
}
```

```csharp
// Game.Gameplay.asmdef
namespace Game.Gameplay.Abilities
{
    // ADR-0007 R11: ComboInputBuffer(-80) の後。ability poll が expired entry pruning 後に走る。
    // ADR-0002の CharacterController2D は FixedUpdate でSolver 動作。本クラスは Update 設計。
    // 将来 FixedUpdate 移行する場合は ordering の再検討が必要。
    [DefaultExecutionOrder(-60)]
    public sealed class AbilityExecutor : MonoBehaviour, IAbilityExecutor
    {
        // ─── Inspector References ───────────────────────────────────────────
        [SerializeField] private Animator  _animator;
        [SerializeField] private LayerMask _hitLayer;

        // ─── DI (Initialize で注入) ─────────────────────────────────────────
        private AbilityContext _context;

        // ─── Runtime State ──────────────────────────────────────────────────
        private ClassAbilityData[] _abilities;
        private ClassAbilityData   _currentAbility;
        private int                _currentFrame;

        // ─── GC-free buffers ────────────────────────────────────────────────
        private readonly Collider2D[]         _hitBuffer         = new Collider2D[8];
        private readonly HashSet<Collider2D>  _hitThisExecution  = new HashSet<Collider2D>();
        private          int[]                _animatorHashCache = System.Array.Empty<int>();

        // ─── Public Event ───────────────────────────────────────────────────
        /// <summary>
        /// Combat System が subscribe。HitConfirmed 発火後に Configure() が呼ばれても
        /// 当該 HitData は確定（R10: 取り消されない）。
        /// </summary>
        public event Action<HitData> HitConfirmed;

        // ─── IAbilityExecutor ───────────────────────────────────────────────
        public void Initialize(AbilityContext context)
        {
            Debug.Assert(context != null, "AbilityExecutor.Initialize: context が null", this);
            _context = context;
        }

        public void Configure(ClassAbilityData[] abilities)
        {
            _abilities = abilities;

            // R10: クラス切替時に実行中 ability を即座にキャンセル
            _currentAbility = null;
            _currentFrame   = 0;
            _hitThisExecution.Clear();

            // R14: AnimatorTriggerKey ハッシュをキャッシュ（Animator.SetTrigger(int) 経由で呼出し）
            if (abilities == null)
            {
                _animatorHashCache = System.Array.Empty<int>();
                return;
            }
            _animatorHashCache = new int[abilities.Length];
            for (int i = 0; i < abilities.Length; i++)
            {
                _animatorHashCache[i] = abilities[i] != null && !string.IsNullOrEmpty(abilities[i].AnimatorTriggerKey)
                    ? Animator.StringToHash(abilities[i].AnimatorTriggerKey)
                    : 0;
            }
        }

        public bool HasCapability(AbilityCapability capability)
        {
            if (_abilities == null) return false;
            foreach (var a in _abilities)
            {
                if (a != null && (a.Capabilities & capability) == capability)
                    return true;
            }
            return false;
        }

        // ─── MonoBehaviour ──────────────────────────────────────────────────
        private void Awake()
        {
            Debug.Assert(_animator != null, "AbilityExecutor: _animator 未アサイン",  this);
            Debug.Assert(_hitLayer  != 0,   "AbilityExecutor: _hitLayer 未設定（0 = 全レイヤー）", this);
        }

        private void Update()
        {
            if (_context == null) return;

            if (_currentAbility != null)
                TickCurrentAbility();
            else
                PollForNewAbility();
        }

        // ─── Poll ───────────────────────────────────────────────────────────
        private void PollForNewAbility()
        {
            if (_abilities == null) return;
            for (int i = 0; i < _abilities.Length; i++)
            {
                var ability = _abilities[i];
                if (ability == null) continue;
                // R5: state check を先に置き、state 不一致なら TryConsume を呼ばない（バッファ温存）
                if (IsStateValid(ability.StateRequirement)
                    && _context.ComboBuffer.TryConsume(ability.ActionName))
                {
                    StartAbility(ability, i);
                    return;
                }
            }
        }

        // ─── Tick ───────────────────────────────────────────────────────────
        private void TickCurrentAbility()
        {
            _currentFrame++;

            // R7: hitbox active window のみ OverlapBoxNonAlloc 呼出し
            if (_currentFrame >= _currentAbility.HitboxActiveStartFrame
                && _currentFrame <= _currentAbility.HitboxActiveEndFrame)
            {
                CheckHitbox(_currentAbility);
            }

            // Combo window: R6 循環参照ガード + state check
            if (_currentAbility.ComboNext != null
                && _currentAbility.ComboNext != _currentAbility  // 循環参照ガード (R6)
                && _currentFrame >= _currentAbility.CancelWindowStartFrame
                && _currentFrame <= _currentAbility.CancelWindowEndFrame)
            {
                var next = _currentAbility.ComboNext;
                if (IsStateValid(next.StateRequirement)
                    && _context.ComboBuffer.TryConsume(next.ActionName))
                {
                    StartAbilityByRef(next);
                    return;
                }
            }

            // ability 終了
            if (_currentFrame >= _currentAbility.TotalDurationFrames)
            {
                _currentAbility = null;
                _currentFrame   = 0;
                _hitThisExecution.Clear();
            }
        }

        // ─── Execute ────────────────────────────────────────────────────────
        private void StartAbility(ClassAbilityData ability, int arrayIndex)
        {
            _currentAbility = ability;
            _currentFrame   = 0;
            _hitThisExecution.Clear();  // R12: per-execution 重複ヒットリセット

            ApplyAbilityEffects(ability, arrayIndex);
        }

        private void StartAbilityByRef(ClassAbilityData ability)
        {
            // ComboNext 経由 — arrayIndex が不定のため AnimatorHash を直接算出
            _currentAbility = ability;
            _currentFrame   = 0;
            _hitThisExecution.Clear();

            int hash = string.IsNullOrEmpty(ability.AnimatorTriggerKey)
                ? 0
                : Animator.StringToHash(ability.AnimatorTriggerKey);
            ApplyAbilityEffectsWithHash(ability, hash);
        }

        private void ApplyAbilityEffects(ClassAbilityData ability, int arrayIndex)
        {
            ApplyAbilityEffectsWithHash(ability, _animatorHashCache[arrayIndex]);
        }

        private void ApplyAbilityEffectsWithHash(ClassAbilityData ability, int animHash)
        {
            // Animation (R14: int hash 経由)
            if (animHash != 0)
                _animator.SetTrigger(animHash);

            // Motor commands (A2: ICharacterMotor 経由のみ)
            if (ability.LaunchImpulse != Vector2.zero)
                _context.Motor.RequestImpulse(FaceAdjustedImpulse(ability.LaunchImpulse));

            if (ability.GravityOverrideMultiplier > 0f)
                _context.Motor.OverrideGravity(ability.GravityOverrideMultiplier, ability.GravityOverrideSec);

            if (ability.LockHorizontalSec > 0f)
                _context.Motor.LockHorizontalControl(ability.LockHorizontalSec);

            // VFX
            if (ability.VfxOnStart != null)
                _context.VFX.PlayCue(ability.VfxOnStart,
                    new VfxCueArgs { WorldPosition = _context.Motor.Position });

            // SFX (Tier 0: NullAudioPublisher で no-op)
            if (ability.SfxOnStart != null)
                _context.Audio.PlayCue(ability.SfxOnStart,
                    new AudioCueArgs(_context.Motor.Position));
        }

        private void CheckHitbox(ClassAbilityData ability)
        {
            var offset    = FaceAdjustedOffset(ability.HitboxOffset);
            var origin    = _context.Motor.Position + offset;
            var facing    = _context.Motor.Facing == Facing.Right ? Vector2.right : Vector2.left;

            int count = Physics2D.OverlapBoxNonAlloc(origin, ability.HitboxSize, 0f, _hitBuffer, _hitLayer);

            if (count > _hitBuffer.Length)
                Debug.LogWarning($"AbilityExecutor: hitBuffer overflow ({count} hits). 上限 {_hitBuffer.Length} を超えた。", this);

            for (int i = 0; i < count && i < _hitBuffer.Length; i++)
            {
                var col = _hitBuffer[i];
                // R12: per-execution 重複ヒット防止
                if (!_hitThisExecution.Add(col)) continue;

                var hitData = new HitData(origin, ability.ActionName, facing, col, ability, facing);
                HitConfirmed?.Invoke(hitData);

                if (ability.VfxOnHit != null)
                    _context.VFX.PlayCue(ability.VfxOnHit, new VfxCueArgs { WorldPosition = origin });

                if (ability.SfxOnHit != null)
                    _context.Audio.PlayCue(ability.SfxOnHit, new AudioCueArgs(origin));
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────
        private bool IsStateValid(RequiredMotorState req)
        {
            if (req == RequiredMotorState.Any) return true;
            var motorState = _context.Motor.State;
            return req switch
            {
                RequiredMotorState.Grounded  => motorState == MotorState.Grounded,
                RequiredMotorState.Airborne  => motorState == MotorState.Airborne,
                RequiredMotorState.WallSlide => motorState == MotorState.WallSliding,
                _                            => true,
            };
        }

        private Vector2 FaceAdjustedImpulse(Vector2 impulse)
            => _context.Motor.Facing == Facing.Right ? impulse : new Vector2(-impulse.x, impulse.y);

        private Vector2 FaceAdjustedOffset(Vector2 offset)
            => _context.Motor.Facing == Facing.Right ? offset : new Vector2(-offset.x, offset.y);
    }
}
```

```csharp
// Game.Gameplay.asmdef — Tier 0 Audio stub
namespace Game.Gameplay.Abilities
{
    public sealed class NullAudioPublisher : IAudioPublisher
    {
        public static readonly IAudioPublisher Instance = new NullAudioPublisher();
        private NullAudioPublisher() { }
        public void PlayCue(AudioCueDefinition cue, AudioCueArgs args) { }
    }
}
```

## Alternatives Considered

### Alternative B: IAbilityBehavior per-ability Strategy Objects

- **Description**: `ClassAbilityData` の代わりに `IAbilityBehavior : MonoBehaviour` のサブクラスごとに ability を実装。`AbilityExecutor` は `IAbilityBehavior[]` を保持し適切な behavior に delegate
- **Pros**: 各 ability の挙動が独立したクラスに分離 → 複雑な ability（チャネリング / スタック）は表現しやすい
- **Cons**: 2D メトロイドヴァニア MVP 2 職（計 4-6 abilities）にはオーバーアーキテクチャ。「4 職目 = コード変更ゼロ」制約（ADR-0001 R5）が MonoBehaviour サブクラス追加で崩れる
- **Rejection Reason**: MVP scope では MonoBehaviour サブクラス不要。SO + frame data で十分

### Alternative C: Coroutine 駆動実行

- **Description**: `StartAbility()` でコルーチンを起動し `yield return null` でフレームタイミングを制御
- **Pros**: 人間が読みやすいタイムライン記述
- **Cons**: （1）`yield return null` は GC alloc 可能性（0 byte 目標に反する）、（2）`StopCoroutine` タイミングが Unity execution order に依存し class switch 時の即時キャンセル（R10）が frame 境界に縛られる、（3）コルーチン内のロジックは EditMode unit test から呼べない
- **Rejection Reason**: GC alloc リスク + キャンセル保証の弱さ + テスタビリティ低下

### Alternative D: Animation Event 駆動ヒットボックス

- **Description**: hitbox の active / inactive タイミングを Animation Event で制御
- **Pros**: デザイナーが hitbox タイミングを Animation Clip 上で直接調整できる
- **Cons**: （1）Unit テストが PlayMode 必須（EditMode 不可）、（2）ClassAbilityData の frame data との二重管理リスク、（3）Tier 0 はアニメーション整備前のため hitbox タイミング定義ができない
- **Rejection Reason**: テスタビリティ低下 + Tier 0 アニメーション依存

### Alternative E: Push モデル（IInputEventStream.Subscribe）

- **Description**: AbilityExecutor が IInputEventStream を subscribe し InputEvent 発火時に ability を実行
- **Pros**: event-driven でレイテンシが低い
- **Cons**: （1）ADR-0007 の IComboBuffer.TryConsume() は Latest-first 消費 — 毎フレーム最新バッファから取り出す poll 型と完全に一致、push 型では RequiredMotorState の短絡評価（R5）が実現できない、（2）push 型では「state 不一致時にバッファ温存」という combo buffer の設計意図が崩れる
- **Rejection Reason**: IComboBuffer の poll セマンティクスと根本的に非整合

## Consequences

### Positive

- `ClassAbilityData` SO 純データ + `AbilityExecutor` 実装の三分割（ADR-0001 A1）が構造的に達成
- 4 職目追加 = `ClassDefinition_NewClass.asset` + `ClassAbilityData_NewAttack.asset` 追加のみ（コード変更ゼロ、`AbilityCapability` 付与でゲートドアも自動解除）
- `AbilityContext` の 4 interface が独立して置換可能（Audio System 完成時に NullAudioPublisher → AudioService に差し替えるだけ）
- `HitConfirmed event` + `SourceAbility` フィールドで Combat System が疎結合のまま Tier 1 ダメージ算出を実装可能
- `RequiredMotorState` 短絡評価でバッファエントリを温存 → 先行入力（ADR-0007）の設計意図と完全整合
- `HasCapability(AbilityCapability)` で Gate & Lock が ActionName 文字列に依存しない → リファクタ耐性

### Negative

- `ClassAbilityData[]` の先頭順序に依存（R11）するため、デザイナーの配列配置規律が必要
- `_hitThisExecution: HashSet<Collider2D>` は per-execution で Clear() するため GC alloc ゼロだが HashSet 自体は起動時に確保（許容範囲）
- `ClassAbilityData` の現フィールド集合が「移動 Impulse / 重力 Override / 水平制御 Lock / Hitbox / Combo chain」の能力空間をスパン。この空間外の能力（テレポート / グラビティ反転 / 時間操作）を Tier 3 クラス4に導入する場合は本 ADR の改訂が必要
- `IAudioPublisher.PlayCue(AudioCueDefinition, AudioCueArgs)` の AudioCueDefinition は Tier 0 で空 SO。Tier 1 で Audio System ADR が中身を定義するまで設定ミス（null cue 渡し）のデバッグが難しい
- Flush() と HitConfirmed の タイミング契約（R10 参照）は設計規律として文書化されているが、runtime enforcement は単体テスト（CA8）に依存

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| `ComboNext` 循環参照設定ミス（A → B → A）| LOW | HIGH | `ComboNext != _currentAbility` ガード (R6)。Unityオブジェクト比較は `==` を使用（`is null` 禁止） |
| OverlapBoxNonAlloc の同一 Physics 状態を複数 Update フレームから参照し重複ヒット | MEDIUM | MEDIUM | `_hitThisExecution HashSet` で per-execution 重複排除 (R12)。60fps Update vs 50Hz FixedUpdate のタイミングずれを許容（2D メトロイドヴァニアの操作感優先） |
| `_hitBuffer[8]` 超過（9+ 体同フレームヒット） | LOW | LOW | `count > _hitBuffer.Length` を `Debug.LogWarning` で検出。Tier 3 ボスフェーズ前にバッファサイズを `ClassAbilityData.MaxHitTargets` フィールドに昇格 |
| `ClassAbilityData.ActionName` タイポで TryConsume 永久 false | MEDIUM | LOW | CA0 unit test に「Configure 後に正しい ActionName で TryConsume = true」smoke test を追加 |
| Combo window 外連打が次 ability にリーク | MEDIUM | LOW | CA3 境界値テスト（CancelWindowEndFrame+1 フレームで TryConsume が false のまま）で verify |
| Animator.SetTrigger の 1 フレーム遅延（animation vs hitbox タイミングずれ） | MEDIUM | LOW | Tier 0 では ICharacterMotor.ApplyHitstop の 1-2 フレームが視覚的にカバー。Tier 1 で CA4 で hitbox と animation 同期を再検証 |
| AudioCueDefinition が Tier 0 で空 SO のまま null 参照 | MEDIUM | LOW | ClassAbilityData の SfxOnStart / SfxOnHit は null 許容（null check 後に PlayCue 呼出し）。NullAudioPublisher.PlayCue は引数 null でも no-op |

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/systems-index.md` | #11 Class Abilities System | A1: 3 分割（ClassAbilityData / IAbilityExecutor / AbilityContext） | ClassAbilityData SO + IAbilityExecutor interface + AbilityContext DI で完全実現 |
| `design/gdd/systems-index.md` | #11 Class Abilities System | AbilityContext に IComboBuffer を含む | AbilityContext.ComboBuffer: IComboBuffer フィールドを定義 |
| `design/gdd/systems-index.md` | #12 Combat System | HitConfirmed bridge で Combat System が damage pipeline を subscribe | HitConfirmed: event Action<HitData> を公開。HitData.SourceAbility で ClassAbilityData を直参照可能 |
| `design/gdd/systems-index.md` | #18 Gate & Lock System | ability-gated ドアの解放判定 | HasCapability(AbilityCapability) で ActionName 文字列非依存の能力問い合わせ |
| `design/gdd/systems-index.md` | CD4 Anti-Pillar guard | アビリティアンロックツリー / 強化スロット禁止 | ClassAbilityData に lock/unlock / tier/level フィールドなし。HasCapability は存在確認のみ（unlock 管理は Orb Acquisition System） |
| `docs/architecture/adr-0001-class-switch-architecture.md` | §Forward References | AbilityContext / IAbilityExecutor の契約確定 | 本 ADR で完全定義 |
| `docs/architecture/adr-0002-character-controller-motor.md` | §Forward References | ADR-0008 で AbilityContext.Motor が ICharacterMotor を使用 | AbilityContext.Motor: ICharacterMotor + FaceAdjusted helpers で意図 API のみ呼出し |
| `docs/architecture/adr-0007-combo-input-buffer.md` | R11 ExecutionOrder | AbilityExecutor は > -80 の ExecutionOrder が必要 | `[DefaultExecutionOrder(-60)]` で満足 |
| `.claude/docs/technical-preferences.md` | Magic numbers 禁止 | バランス値は ScriptableObject に配置 | ClassAbilityData の全タイミング値 / サイズ / インパルス値は SO フィールド |

## Performance Implications

| Metric | Value | Budget |
|--------|-------|--------|
| PollForNewAbility（idle 時） | O(n), n = abilities 数（最大 8）, TryConsume O(16)=O(1) per call | ≤ 0.05 ms / frame |
| TickCurrentAbility（実行中） | OverlapBoxNonAlloc O(k) は hitbox active frames のみ | ≤ 0.10 ms / frame |
| StartAbility（発動時） | O(1) — Animator.SetTrigger(int) + RequestImpulse + PlayCue | ≤ 0.05 ms |
| GC.Alloc per frame | 0 byte（OverlapBoxNonAlloc + _hitBuffer 再利用 + HashSet.Add は struct key で alloc なし） | 0 byte |
| Memory | Collider2D[8] ≈ 64B + HashSet<Collider2D> ≈ 256B + int[] hash cache ≈ 32B | < 1 KB |

## Migration Plan

```
[Tier 0 MVP — 本 ADR 実装範囲]
  PlayerBootstrap.Awake():
    var ctx = new AbilityContext(
        CharacterController2D.Instance,  // ICharacterMotor (ADR-0002)
        ComboInputBuffer.Instance,       // IComboBuffer    (ADR-0007)
        VFXPublisherService.Instance,    // IVFXPublisher   (ADR-0003)
        NullAudioPublisher.Instance      // Tier 0 stub
    );
    _abilityExecutor.Initialize(ctx);

  ClassDefinition_Swordsman.asset:
    Abilities[0] = GroundAttack1.asset  (ActionName="attack", StateRequirement=Grounded,
                                         Capabilities=None, ComboNext=GroundAttack2.asset)
    Abilities[1] = AirAttack.asset      (ActionName="attack", StateRequirement=Airborne,
                                         Capabilities=None, ComboNext=null)
    Abilities[2] = GroundDash.asset     (ActionName="dash",   StateRequirement=Any,
                                         Capabilities=Dash)

  GroundAttack1.asset:
    ComboNext = GroundAttack2.asset
    CancelWindowStartFrame=8, CancelWindowEndFrame=18
    TotalDurationFrames=24
    HitboxActiveStartFrame=4, HitboxActiveEndFrame=8
    HitboxOffset=(0.5,0), HitboxSize=(1.0,0.8)
    LaunchImpulse=(0,0)

[Tier 1 VS — Audio System ADR 完成後]
  PlayerBootstrap:
    var ctx = new AbilityContext(
        ...,
        AudioService.Instance   // NullAudioPublisher → AudioService に差し替え
    );
  ClassAbilityData の SfxOnStart / SfxOnHit が active 化

[Tier 1 VS — Combat System ADR 完成後]
  CombatSystem.Awake():
    _abilityExecutor.HitConfirmed += OnHitConfirmed;

  OnHitConfirmed(HitData data):
    // data.SourceAbility から Damage / HitstopSec / KnockbackImpulse を直読み（ADR-0009 R6）
    // HitNormal = AttackerFacing（OverlapBoxNonAlloc の接触法線ではない）
    _playerMotor?.ApplyHitstop(data.SourceAbility.HitstopSec);  // 攻撃側 Hitstop (ADR-0009)
    var receiver = data.HitCollider.GetComponent<IDamageReceiver>();
    Vector2 knockback = data.HitNormal * data.SourceAbility.KnockbackImpulse;
    receiver?.TakeDamage(data.SourceAbility.Damage, knockback);

[Tier 3 クラス4追加 — コード変更ゼロ]
  ClassDefinition_NewClass.asset 作成
  ClassAbilityData_Phase.asset: ActionName="dash", Capabilities=Dash (← Gate & Lock 自動対応)
  → Gate & Lock の HasCapability(AbilityCapability.Dash) が true を返す
```

## Validation Criteria

Validation Gate **CA0-CA8** 全通過で `Proposed` → `Accepted` に昇格。

- [ ] **CA0 — AbilityContext 注入**: `Initialize(ctx)` 後に `Update()` が null guard をクリアし処理実行（EditMode test）
- [ ] **CA1 — Configure キャンセル**: 実行中 ability の mid-execution（frame 5）中に `Configure(newAbilities)` を呼ぶと `_currentAbility == null` / `_currentFrame == 0` / `_hitThisExecution.Count == 0` になること（EditMode test）
- [ ] **CA2 — TryConsume poll**: ComboInputBuffer に "attack" を push → `AbilityExecutor.Update()` で `StartAbility()` が呼ばれること（PlayMode integration test）
- [ ] **CA3 — Combo chain + 境界値**: GroundAttack1 の CancelWindow フレーム内に "attack" を push → StartAbility(GroundAttack2) が呼ばれること。CancelWindowEndFrame + 1 フレームでは StartAbility が呼ばれないこと
- [ ] **CA4 — ヒットボックス検出**: HitboxActiveStartFrame - 1 では OverlapBoxNonAlloc が呼ばれず、active 区間では呼ばれること。dummy enemy Collider2D を配置して HitConfirmed event が発火し HitData.SourceAbility が正しい ClassAbilityData を参照していること
- [ ] **CA5 — RequiredMotorState 制約**: RequiredState=Grounded の ability は Airborne 中に TryConsume されないこと（バッファエントリが残ること）。接地後に消費されること
- [ ] **CA6 — ExecutionOrder**: ComboInputBuffer(-80) の Update() が AbilityExecutor(-60) の Update() より前に実行されること（同フレーム内で pruning → poll の順序保証）
- [ ] **CA7 — パフォーマンス**: 1000 フレーム連続実行 → p99 ≤ 0.2ms / frame、GC.Alloc = 0 byte を Profiler EditMode で verify
- [ ] **CA8 — HitConfirmed + Configure() レース**: ability HitboxActive フレーム中に HitConfirmed が発火した後 Configure() を呼んでも、既発火の HitData は subscriber に届いており「取り消し」が発生していないこと（同フレーム内の発火順序テスト）

## Related Decisions

- **Depends on**: [ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md)（ClassStateMachine / ClassDefinition / Configure forward ref）
- **Depends on**: [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md)（AbilityContext.Motor / ICharacterMotor API / MotorState / Facing）
- **Depends on**: [ADR-0003 VFX System Boundary + IVFXPublisher](adr-0003-vfx-system-boundary.md)（AbilityContext.VFX / VfxCueDefinition / VfxCueArgs）
- **Depends on**: [ADR-0007 Combo Input Buffer](adr-0007-combo-input-buffer.md)（AbilityContext.ComboBuffer / IComboBuffer.TryConsume / ExecutionOrder R11）
- **Enables**: Combat System ADR（HitConfirmed event / HitData.SourceAbility / IDamageReceiver bridge）
- **Enables**: Gate & Lock ADR (#18)（HasCapability(AbilityCapability) で ability-gated ドア解放判定）
- **Engine reference**: [docs/engine-reference/unity/VERSION.md](../../docs/engine-reference/unity/VERSION.md)
- **Implementation files (post-Accepted)**:
  - `src/core/abilities/IAbilityExecutor.cs`
  - `src/core/abilities/AbilityContext.cs`
  - `src/core/abilities/AbilityCapability.cs`
  - `src/core/abilities/ClassAbilityData.cs`
  - `src/core/abilities/IAudioPublisher.cs`
  - `src/core/abilities/AudioCueDefinition.cs`
  - `src/core/abilities/AudioCueArgs.cs`
  - `src/core/abilities/HitData.cs`
  - `src/gameplay/abilities/AbilityExecutor.cs`
  - `src/gameplay/abilities/NullAudioPublisher.cs`
  - `assets/data/abilities/GroundAttack1.asset`
  - `assets/data/abilities/GroundAttack2.asset`
  - `assets/data/abilities/AirAttack.asset`
  - `assets/data/abilities/GroundDash.asset`
  - `tests/unit/abilities/AbilityExecutorTests.cs`（CA0-CA8）
