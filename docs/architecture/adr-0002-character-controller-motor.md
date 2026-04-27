# ADR-0002: CharacterController2D + ICharacterMotor

## Status

**Proposed (Validation Gate: V1-V5)**

> 本 ADR は「Validation Gate」セクション V1-V5 の Cast/Solver spike を通過するまで Accepted に昇格しない。特に V1（`Rigidbody2D.Cast()` 系の API 名確認）と V3（高速移動時の tunneling 防止）が偽の場合、Decision の根幹（Solver 権威性 + Cast ベース衝突予測）が崩壊し、Alternative C 再評価となる。なお本 ADR は **Proposed 段階でも `docs/registry/architecture.yaml` への architectural stance を前倒し追記**する（API 名確定は V1 通過後に revise する旨の注記付き）。理由は ADR-0004 / Combat 系 ADR が本 ADR の interface stance を参照する必要があり、Accepted 待ちでブロックされるため。

## Date

2026-04-27

## Last Verified

2026-04-27

## Decision Makers

- Project Lead（ユーザ）— 最終決定権
- `creative-director` 経由 CD-SYSTEMS Note CD1（Tier 0 Hitstop 内蔵要件）
- `technical-director` 経由 TD-SYSTEM-BOUNDARY A2 + TD-ADR gate — 双方向結合防止 + Solver 権威性
- `producer` 経由 PR-SCOPE — Foundation 層 MVP 9 システムの 1 つとして Tier 0 着手対象
- `unity-specialist` — Unity 6.3 LTS 2D Physics（Box2D v3 統合）+ `Physics2D.SyncTransforms()` deprecated 移行レビュー

## Summary

本作 `職業オーブのレガシー` の Foundation 層において、自作 Kinematic `CharacterController2D` を **Solver 権威性 1 フレーム 1 solve** で実装し、外部からは `ICharacterMotor` interface のみを公開する。ability / combat / camera / enemy AI は意図ベース API（`RequestImpulse` / `OverrideGravity` / `LockHorizontalControl` / `ApplyHitstop` / `SetFacing`）でのみ Motor を駆動し、内部状態（`Rigidbody2D.linearVelocity` / `gravityScale` / `IsGrounded` flag / `Transform.position`）の直接 write は forbidden。Tier 0 では CD1 要件の Hitstop（30-50ms）を Motor 自身が内蔵し、Combat System の完成を待たずに Pillar 3「歯ごたえ」を検証する。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Physics（2D Physics / Box2D v3）+ Core（Kinematic Solver / 状態管理）+ Input（Input System 1.8+ 連携） |
| **Knowledge Risk** | **HIGH** — Unity 6.3 は内部物理エンジンを Box2D v3 に刷新（マルチスレッド化・決定論性強化）。`engine-reference/unity/modules/physics.md` は 3D Physics 中心で 2D Physics（特に `Rigidbody2D.Cast()` / `Collider2D.Cast()` / `ContactFilter2D` / Box2D v3 専用 API `UnityEngine.LowLevelPhysics2D` 等）の post-cutoff 挙動が未収録。`Physics2D.autoSyncTransforms` は 6.3 で deprecated（6.5 削除予定）、明示的 `Physics2D.SyncTransforms()` 必須 |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`modules/physics.md`、`modules/input.md`、`breaking-changes.md`（Box2D v3 統合 / `Physics.autoSyncTransforms` 非推奨セクション）、`deprecated-apis.md`（Physics 表）、`current-best-practices.md`（Input System / async）、`.claude/docs/technical-preferences.md`（Forbidden Patterns / Allowed Libraries） |
| **Post-Cutoff APIs Used** | `Rigidbody2D.linearVelocity`（Unity 6.0+ 改名、旧 `velocity` は obsolete warning）、`Rigidbody2D.Cast(Vector2, RaycastHit2D[])`、`Collider2D.Cast(Vector2, ContactFilter2D, RaycastHit2D[])`、`ContactFilter2D`（既存 API だが Box2D v3 で Result 順序保証強化）、`Physics2D.SyncTransforms()`（Unity 6.3 で明示呼び出し必須）、`UnityEngine.LowLevelPhysics2D`（Optional、Tier 1 で評価） |
| **Verification Required** | (1) `Rigidbody2D.Cast()` の overload 名と引数数（特に `ContactFilter2D` 省略形が 6.3 で存在するか、`Rigidbody2D.Cast(Vector2, RaycastHit2D[])` 2 引数 vs `Rigidbody2D.Cast(Vector2, ContactFilter2D, RaycastHit2D[], float)` 4 引数）、(2) `Collider2D.Cast()` の Result 配列の距離ソート順保証（Box2D v3 で「決定論性強化」と breaking-changes に記載あるが Cast Result 順序の保証文言は未検証）、(3) `Rigidbody2D.linearVelocity` の `velocity` からの置換状況（6.3 で警告か エラーか）、(4) `Physics2D.SyncTransforms()` 呼び出し後に「同 FixedUpdate 内の次 Cast クエリ」が更新後 Transform を参照することの実証（V2 と統合）、(5) Unityちゃん公式 PSB の SkinAnchor / Pivot とコライダー中心の整合、(6) `Physics2D.queriesStartInColliders` の Unity 6.3 デフォルト値（true なら `SkinWidth` オフセット起点 Cast の挙動への影響） — **すべて V1-V5 検証プロトタイプで実測必須** |

> **Note**: Knowledge Risk が HIGH のため、Unity 6.3 → 6.4 等のマイナー昇格時に `Physics2D.SyncTransforms()` の挙動・`Rigidbody2D.Cast()` の API 名が変わった場合、本 ADR を Superseded にし新 ADR を起こすこと。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None（Foundation 層 ADR、上流依存なし） |
| **Enables** | ADR-0004（Class Abilities System 詳細：`AbilityContext.Motor` で本 ADR の `ICharacterMotor` を埋める）、Combat System 系 ADR（ノックバック / 被ダメージ硬直 / Hitstop の API 経路を本 ADR で確定）、Enemy AI 系 ADR（敵側でも Motor 抽象を再利用する余地） |
| **Blocks** | `design/gdd/character-controller-2d.md` GDD authoring、`design/gdd/class-abilities-system.md` GDD authoring（`AbilityContext` の Motor 部分を埋めるため）、`design/gdd/combat-system.md` GDD authoring（ノックバック / Hitstop 経路）、Camera System GDD（Camera follow target が `ICharacterMotor.Position` 経由となる方針確定後） |
| **Ordering Note** | systems-index.md Recommended Design Order 表で **Order #3 CharacterController2D**（Class Switch System #5 より前）。本 ADR は Class Switch ADR-0001 と並走可能だが、ADR-0001 の Forward Reference `ICharacterMotor` を埋める責任を持つため ADR-0001 Accepted 前後どちらでも整合する。technical-preferences.md 想定リスト「ADR-006 Kinematic CharacterController2D 自作」と同一決定、実ファイル番号は ADR-0002。technical-preferences.md Architecture Decisions Log は本 ADR Accept 後に同期更新する |

## Context

### Problem Statement

本作 2D メトロイドヴァニアの操作感（Pillar 3「歯ごたえ」）は、ジャンプ・ダッシュ・空中制御・着地・ノックバック・被ダメージ硬直・コンボキャンセルといった微細な物理応答に支配される。Unity 標準 Dynamic Rigidbody2D + Linear Damping ベースでは、空中制御の即応性・コヨーテタイム・Jump Buffer・Wall Slide といったメトロイドヴァニア定番要件で職人芸的なチューニング工数が膨れ、Pillar 3 の「重みのある操作レスポンス」を 6 週間 MVP で詰めきれないリスクが高い。

加えて、systems-index.md Architecture Note **A2** により、`archer dash` や `mage hover` のような Class Ability が CharacterController2D の内部状態（`linearVelocity` / `gravityScale` / `IsGrounded`）を直接書き換える設計は **明示的に禁止**された。理由は (a) Solver 権威性が崩壊し 1 フレーム 1 solve invariant が破綻する、(b) Box2D v3 のマルチスレッド `SyncTransforms` タイミングが保護されない、(c) ability ↔ motor の双方向結合により Class Abilities が God Object 化する（CD A1 違反の派生形）。

さらに systems-index.md Creative Director Note **CD1** により、Tier 0 hypothesis spike では VFX System / Audio System / Combat System 不在のまま「切替の satisfaction」と「打撃の歯ごたえ」を検証する必要がある。Class Switch System が color wash + SE を内包するのと同様、CharacterController2D は **Hitstop（30-50ms）を自己内包**しなければ、Combat System 完成前の Tier 0 Go/Pivot/Stop 判定が信頼できない。

### Current State

未実装。プロジェクトは onboarding 完了直後の状態。Foundation 層 9 MVP システムのうち、Input System / Save Data System / Class Switch System（ADR-0001 Proposed）と並んで本 ADR が確定対象。

### Constraints

- **Engine**: Unity 6.3 LTS / Unity 2D Physics（Box2D v3 マルチスレッド）/ Input System 1.8+
- **Forbidden Patterns**（`technical-preferences.md` + ADR-0001 由来）:
  - `Physics2D.autoSyncTransforms = true`（Unity 6.3 で deprecated、6.5 削除予定）→ 明示的 `Physics2D.SyncTransforms()` のみ
  - `GameObject.Find()` / `FindObjectOfType()` / `GetComponent()` in `Update()` / `FixedUpdate()` — `Awake()` でキャッシュ必須
  - Magic numbers（gameplay 値）— ScriptableObject `MotorTuning` に配置必須
  - `Rigidbody2D.bodyType = Dynamic` の motor への適用 — 本 ADR の Decision で Kinematic 固定
  - `PlayerPrefs` での motor tuning 永続化 — Save Data System 経路のみ
- **Performance budget**:
  - Motor `FixedUpdate` 1 回 ≤ **0.5 ms**（technical-preferences.md「Physics: ≤ 2 ms」内訳の 1/4 を割当）
  - GC alloc / FixedUpdate ≤ 0 byte（NonAlloc API + 事前確保配列）
  - Cast コール数 ≤ 6 / FixedUpdate（水平 2、垂直 2、wall check 2）
- **Localization 規律**（systems-index.md より）: コード内エラーメッセージ等は `Strings.Motor.*` 形式のキー参照のみ。`Debug.Assert` の文字列は規律外（英語生文字列許可）
- **Determinism**: 将来のリプレイ機能（concept R-T3 / スピードラン文化）に備え、`Time.fixedDeltaTime` ベース固定タイムステップで動作。`Random` の使用は禁止（ability 側で seed 管理）

### Requirements

- **R1**: 入力受信から motor 反映が `FixedUpdate` 同一サイクル内で完了
- **R2**: 1 フレーム 1 solve invariant — 同 FixedUpdate 内で `Rigidbody2D.MovePosition` が 1 回だけ呼ばれる（multi-call → 後勝ち上書きを防ぐ）
- **R3**: ability / combat / camera / AI から motor 内部状態（velocity / gravity / grounded / position）への直接 write が **コンパイル時に不能**（interface に setter なし、内部 component への参照を漏らさない）
- **R4**: Tier 0 で Hitstop 30-50ms を motor が自己発火可能（Combat System 不在でも Pillar 3 の打撃感検証可）
- **R5**: 4 職業の ability（dash / hover / heavy stomp / range knockback / class-switch invariant 維持）が **追加コードゼロ**で意図ベース API のみで表現可能
- **R6**: Class Switch（ADR-0001）の最中も motor は中断されない — `ClassChanged` event 通知だけ受け取り、velocity / position / grounded は維持（Pillar 1 の 1 フレーム視覚同期と物理連続性の両立）
- **R7**: Box2D v3 マルチスレッド前提で安全（`MovePosition` 後の `SyncTransforms()` 明示）
- **R8**: Unityちゃん公式 PSB の SkinAnchor / Pivot とコライダー中心が整合（足裏接地の視覚バグなし）

## Decision

**Kinematic Rigidbody2D + 自作 Cast ベース Solver + 公開 `ICharacterMotor` interface の 3 層構成を採用する：**

1. **`ICharacterMotor`** — interface（読み取り専用 state プロパティ + 意図ベース command + C# event 通知）
2. **`CharacterController2D`** — `MonoBehaviour` 実装、Kinematic `Rigidbody2D` を保持し 1 FixedUpdate 1 solve で動作
3. **`MotorTuning`** — `ScriptableObject`、tuning パラメータ（移動速度・ジャンプ高・gravity scale・hitstop デフォルト等）

`AbilityContext.Motor` には `ICharacterMotor` 型のみが格納され、内部の `Rigidbody2D` / `Collider2D` / `Transform` への参照は一切漏らさない。`internal` 修飾でも `Friend assembly` でも漏らさない（`Game.Core.asmdef` 内に閉じ込め、interface のみ Public Surface）。

### Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│ Player GameObject                                                     │
│  ├─ PlayerInput (Unity Input System 1.8+) — InputAction 配信          │
│  ├─ Rigidbody2D (bodyType: Kinematic, interpolation: Interpolate)     │
│  ├─ CapsuleCollider2D (足裏 = pivot 一致、SkinAnchor 整合)            │
│  ├─ CharacterController2D (MonoBehaviour, ICharacterMotor 実装)       │
│  │   ├─ [SerializeField] MotorTuning _tuning                          │
│  │   ├─ [SerializeField] LayerMask _solidLayers                       │
│  │   ├─ [SerializeField] LayerMask _oneWayLayers                      │
│  │   ├─ private Rigidbody2D _rb (Awake cache)                         │
│  │   ├─ private CapsuleCollider2D _collider (Awake cache)             │
│  │   ├─ private ContactFilter2D _solidFilter (Awake init)             │
│  │   ├─ private RaycastHit2D[] _castResults = new RaycastHit2D[8]     │
│  │   ├─ private Vector2 _velocity         // 内部のみ                 │
│  │   ├─ private Vector2 _pendingImpulse   // RequestImpulse 累積      │
│  │   ├─ private float _gravityMultiplier  // 1.0 default              │
│  │   ├─ private float _gravityOverrideRemainSec                       │
│  │   ├─ private float _hitstopRemainSec                               │
│  │   ├─ private float _horizontalLockRemainSec                        │
│  │   ├─ private MotorState _state                                     │
│  │   ├─ private Facing _facing                                        │
│  │   ├─ public event Action<Vector2> Landed       // arg: impact vel │
│  │   ├─ public event Action JumpStarted                               │
│  │   ├─ public event Action<Vector2> WallTouched   // arg: wall nrml  │
│  │   ├─ public event Action<float> HitstopApplied  // arg: duration   │
│  │   └─ public event Action<MotorState> StateChanged                  │
│  ├─ ClassStateMachine (ADR-0001) — ICharacterMotor を read-only 参照  │
│  ├─ AbilityExecutor (ADR-0004) — AbilityContext.Motor で意図呼出      │
│  ├─ SpriteRenderer / SpriteSkin (Unityちゃん公式 PSB)                 │
│  └─ Animator (parameter-driven only)                                  │
│                                                                       │
│ ScriptableObject Assets                                               │
│  ├─ MotorTuning_Default.asset                                         │
│  │   ├─ MaxRunSpeed: 7.5 m/s                                          │
│  │   ├─ AirAcceleration: 35 m/s²                                      │
│  │   ├─ GroundAcceleration: 60 m/s²                                   │
│  │   ├─ JumpInitialVelocity: 12.5 m/s                                 │
│  │   ├─ Gravity: 38 m/s² (downward)                                   │
│  │   ├─ MaxFallSpeed: 22 m/s                                          │
│  │   ├─ CoyoteTimeSec: 0.10                                           │
│  │   ├─ JumpBufferSec: 0.12                                           │
│  │   ├─ HitstopDefaultSec: 0.04                                       │
│  │   └─ SkinWidth: 0.02 m (Cast 距離オフセット)                       │
│  └─ MotorTuning_Boss.asset (将来：ボス戦専用パラメータ)               │
└──────────────────────────────────────────────────────────────────────┘

FixedUpdate Solver Sequence (1 cycle)

  [FixedUpdate begin]
   ├─ if (_hitstopRemainSec > 0)
   │     _hitstopRemainSec -= Time.fixedDeltaTime
   │     return                              ← Solver スキップ
   │
   ├─ horizontal_input = (_horizontalLockRemainSec > 0) ? 0 : input
   ├─ _velocity.x = AccelTowards(target_speed, accel, dt)
   │
   ├─ // Gravity (override 反映)
   │  effective_gravity = _tuning.Gravity * _gravityMultiplier
   │  _velocity.y -= effective_gravity * dt
   │  _velocity.y = max(_velocity.y, -_tuning.MaxFallSpeed)
   │  if (_gravityOverrideRemainSec > 0) _gravityOverrideRemainSec -= dt
   │  else _gravityMultiplier = 1.0
   │
   ├─ // Pending impulse (1 フレームのみ加算)
   │  _velocity += _pendingImpulse
   │  _pendingImpulse = Vector2.zero
   │
   ├─ // Cast based collision prediction
   │  delta = _velocity * dt
   │  CastAndSlide(delta, &final_position, &collision_normals)
   │  // 内訳: 水平 Cast → 法線 slide → 垂直 Cast → 法線 slide
   │
   ├─ // Single MovePosition (R2: 1 frame 1 solve invariant)
   │  _rb.MovePosition(final_position)
   │
   ├─ // 明示的 SyncTransforms (Unity 6.3 必須、A2 申し送り)
   │  Physics2D.SyncTransforms()
   │
   ├─ // State 更新 + event 発火
   │  prev_grounded = _state == Grounded
   │  _state = ResolveState(collision_normals)
   │  if (!prev_grounded && _state == Grounded) Landed?.Invoke(_velocity)
   │  if (collision_normals.HasWall) WalltTouched?.Invoke(...)
   │  if (_horizontalLockRemainSec > 0) _horizontalLockRemainSec -= dt
   │  StateChanged?.Invoke(_state)
   │
   ▼
  [FixedUpdate end]
```

### `Position` / `Velocity` プロパティのスナップショット意味論

`ICharacterMotor.Position => _rb.position` は **`MovePosition` 適用後の値**を返す（FixedUpdate 末尾の `Physics2D.SyncTransforms()` で Transform に伝播済）。Cinemachine 3 の Camera follow が LateUpdate で `Position` を読む場合、すでに当該 FixedUpdate の最終位置を参照可能。`Velocity` も同様に、当該 FixedUpdate の `_velocity` 計算後値を返す（impulse 加算後 / Cast slide 反映後）。Class Switch（ADR-0001）が空中切替時に motor 状態を read する場合、velocity 連続性は本 invariant により保証される（R6）。

### Key Interfaces

```csharp
// ICharacterMotor.cs  ← Public Surface（Game.Core.asmdef）
public enum MotorState
{
    Grounded,
    Airborne,
    WallSliding,
    Pinned,        // 外部から動かされた（cutscene 等）
}

public enum Facing { Left = -1, Right = 1 }

public interface ICharacterMotor
{
    // ── Read-only state（getter のみ、setter なし）
    Vector2 Position { get; }                    // ワールド座標
    Vector2 Velocity { get; }                    // 内部 _velocity のスナップショット
    bool IsGrounded { get; }
    Facing Facing { get; }
    MotorState State { get; }
    bool IsHorizontalControlLocked { get; }
    bool IsHitstopped { get; }

    // ── 意図ベース command（Solver は次 FixedUpdate に反映）
    void RequestImpulse(Vector2 impulse);                    // 1 フレーム加算（jump / dash / knockback）
    void OverrideGravity(float multiplier, float durationSec); // hover / 滑空 / 落下加速
    void LockHorizontalControl(float durationSec);           // 被ダメージ硬直 / dash 中
    void SetFacing(Facing direction);                        // ability 由来の向き強制
    void ApplyHitstop(float durationSec);                    // CD1: 自身の Solver を停止

    // ── Events（C# event、UnityEvent はインスペクタフックで併存）
    // 注: C# 構文上 generic 引数名は省略必須。コメントで意味を補う。
    //     `Grounded` event は廃案 — `Landed(Vector2)` で着地検出は十分、
    //     `IsGrounded` プロパティとの命名衝突を回避する設計判断。
    event Action<Vector2> Landed;            // arg: impact velocity（着地時のみ発火）
    event Action JumpStarted;
    event Action<Vector2> WallTouched;       // arg: wall normal
    event Action<float> HitstopApplied;      // arg: duration sec
    event Action<MotorState> StateChanged;   // arg: 新 state
}

// CharacterController2D.cs（MonoBehaviour）
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class CharacterController2D : MonoBehaviour, ICharacterMotor
{
    [SerializeField] private MotorTuning _tuning;
    [SerializeField] private LayerMask _solidLayers;
    [SerializeField] private LayerMask _oneWayLayers;
    [SerializeField] private UnityEvent<MotorState> _onStateChangedUnityEvent;

    private Rigidbody2D _rb;
    private CapsuleCollider2D _collider;
    private ContactFilter2D _solidFilter;
    private readonly RaycastHit2D[] _castResults = new RaycastHit2D[8];

    private Vector2 _velocity;
    private Vector2 _pendingImpulse;
    private float _gravityMultiplier = 1f;
    private float _gravityOverrideRemainSec;
    private float _hitstopRemainSec;
    private float _horizontalLockRemainSec;
    private MotorState _state;
    private Facing _facing = Facing.Right;

    // ── ICharacterMotor read-only props ──
    public Vector2 Position => _rb.position;
    public Vector2 Velocity => _velocity;
    public bool IsGrounded => _state == MotorState.Grounded;
    public Facing Facing => _facing;
    public MotorState State => _state;
    public bool IsHorizontalControlLocked => _horizontalLockRemainSec > 0f;
    public bool IsHitstopped => _hitstopRemainSec > 0f;

    public event Action<Vector2> Landed;
    public event Action JumpStarted;
    public event Action<Vector2> WallTouched;
    public event Action<float> HitstopApplied;
    public event Action<MotorState> StateChanged;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CapsuleCollider2D>();
        Debug.Assert(_tuning != null, "CharacterController2D: _tuning 未アサイン", this);
        Debug.Assert(_rb.bodyType == RigidbodyType2D.Kinematic,
            "CharacterController2D: Rigidbody2D は Kinematic 必須", this);

        _solidFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _solidLayers,
            useTriggers = false,
        };
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (_hitstopRemainSec > 0f)
        {
            _hitstopRemainSec -= dt;
            return;  // R2 invariant: solver 1 サイクル完全スキップ
        }

        ApplyHorizontalIntent(dt);
        ApplyGravity(dt);
        _velocity += _pendingImpulse;
        _pendingImpulse = Vector2.zero;

        Vector2 delta = _velocity * dt;
        Vector2 finalPosition = CastAndSlide(delta, out var collisionInfo);

        _rb.MovePosition(finalPosition);
        Physics2D.SyncTransforms();   // Unity 6.3 明示呼び出し（autoSyncTransforms 非推奨）

        UpdateStateAndEvents(collisionInfo, dt);
    }

    // ── 意図ベース command 実装 ──
    public void RequestImpulse(Vector2 impulse)        => _pendingImpulse += impulse;
    public void OverrideGravity(float multiplier, float durationSec)
    {
        _gravityMultiplier = multiplier;
        _gravityOverrideRemainSec = Mathf.Max(_gravityOverrideRemainSec, durationSec);
    }
    public void LockHorizontalControl(float durationSec)
        => _horizontalLockRemainSec = Mathf.Max(_horizontalLockRemainSec, durationSec);
    public void SetFacing(Facing direction)
    {
        if (_facing == direction) return;
        _facing = direction;
        // visual flip は SpriteRenderer.flipX 側の subscriber が StateChanged で対応
    }
    public void ApplyHitstop(float durationSec)
    {
        _hitstopRemainSec = Mathf.Max(_hitstopRemainSec, durationSec);
        HitstopApplied?.Invoke(durationSec);
    }

    // ── 内部メソッド（private、Public Surface 漏洩なし）──
    private void ApplyHorizontalIntent(float dt) { /* ... */ }
    private void ApplyGravity(float dt) { /* ... */ }
    private Vector2 CastAndSlide(Vector2 delta, out CollisionInfo info) { /* ... */ }
    private void UpdateStateAndEvents(in CollisionInfo info, float dt) { /* ... */ }

    private struct CollisionInfo { public bool HitFloor, HitCeiling, HitWall; public Vector2 WallNormal; }
}

// MotorTuning.cs
[CreateAssetMenu(menuName = "Game/Motor Tuning")]
public class MotorTuning : ScriptableObject
{
    [Header("Horizontal")]
    public float MaxRunSpeed = 7.5f;
    public float GroundAcceleration = 60f;
    public float AirAcceleration = 35f;

    [Header("Vertical")]
    public float JumpInitialVelocity = 12.5f;
    public float Gravity = 38f;
    public float MaxFallSpeed = 22f;

    [Header("Buffering")]
    public float CoyoteTimeSec = 0.10f;
    public float JumpBufferSec = 0.12f;

    [Header("Tier 0 Self-Contained Feedback")]
    public float HitstopDefaultSec = 0.04f;     // CD1: 30-50ms 範囲

    [Header("Solver")]
    public float SkinWidth = 0.02f;
}
```

### Forward References (non-binding)

> 以下の名称は本作内で言及済みだが、本 ADR では契約として確定しない：

- **`AbilityContext`** — ADR-0004 で定義。`Motor: ICharacterMotor` フィールドを持つ
- **`IComboBuffer`** — Combat 系 ADR で定義。本 ADR の motor は ComboBuffer を知らない（input 受け取り側のみが知る）
- **`IDamageReceiver`** — 将来の Health & Damage System ADR で定義。motor は damage 受信を知らないが、knockback impulse は `RequestImpulse` 経由で受け付ける
- **`InputBindingsService`** — Input System 系 ADR 確定後、本 ADR との接続点を確認（現状: PlayerInput が直接 motor の意図 API を呼ぶ）

### Implementation Guidelines

1. **API 名検証を最優先（V1 検証プロトタイプ）**：実装着手前に Unity Editor 上で以下を実機確認すること：
   - `Rigidbody2D.linearVelocity` プロパティが Unity 6.3 で正式名（`velocity` は obsolete warning か error か）
   - `Rigidbody2D.Cast(Vector2 direction, RaycastHit2D[] results)` overload の引数順序
   - `Collider2D.Cast(Vector2, ContactFilter2D, RaycastHit2D[])` の Result 順序保証（Box2D v3 で強化された旨）
   - `Physics2D.SyncTransforms()` が `autoSyncTransforms = true` 時と等価挙動か
2. **Kinematic Rigidbody2D 必須**：`Awake()` で `Debug.Assert(_rb.bodyType == RigidbodyType2D.Kinematic)`。Editor で誤って Dynamic にすると即検出
3. **`MovePosition` は FixedUpdate 内で 1 回のみ**：multi-call は最後の値で上書きされ R2 invariant 違反。テストで境界（1 サイクル内 1 call）を確認
4. **`Physics2D.SyncTransforms()` を `MovePosition` 直後に明示**：Unity 6.3 deprecated migration、Box2D v3 マルチスレッド対応のため
5. **CastResults 配列は事前確保**：`new RaycastHit2D[8]` を Awake 時に確保、FixedUpdate 中は再利用（GC alloc 0 byte）
6. **ContactFilter2D は Awake で初期化、毎フレーム再構築禁止**：`useTriggers = false` 等を一度だけ設定
7. **Hitstop 中は Solver 完全スキップ**：途中 return で `_velocity` の変更も行わない。Class Switch（ADR-0001）からの `ClassChanged` event を受け取った場合も hitstop 中なら state 維持
8. **Pillar 1 invariant: Class Switch は motor を中断しない**：ADR-0001 の `ClassStateMachine.ClassChanged` を購読する義務は motor にない。逆に motor 状態は ClassStateMachine から read-only でのみ参照可（Class Switch が空中切替時に velocity を保つため）
9. **`UnityEvent<MotorState>` インスペクタフック併存**：C# event は型安全な ability/AI 側、UnityEvent は Designer がインスペクタから VFX/SE Hook 後付け
10. **`ScriptableObject MotorTuning` 1 個 / プレイヤーキャラ**：ボス戦・ステルスエリア等で別 MotorTuning を一時 swap 可能な余地（Tier 1 で `OverrideTuning(MotorTuning, durationSec)` 追加候補、本 ADR では未確定）
11. **motor は `Animator` の parameter を駆動しない**：`Animator.SetFloat("Speed", _velocity.x)` 等を motor 内で行うと motor の責務が膨れ A2 と同型の drift を招く。代わりに **Animator parameter は `StateChanged` / `Landed` event subscriber が `Update()` で同フレーム反映**する。1 frame 遅延が問題になるケース（着地アニメ即時反映等）は `StateChanged` event 経由で `Update()` 中に `Animator.Play(stateHash, 0, normalizedTime)` を直接呼ぶ subscriber を別途用意（本 ADR の責務外）。
12. **`OverrideGravity` / `LockHorizontalControl` の重複呼び出し合成規則**：`LockHorizontalControl(durationSec)` は `Mathf.Max(残り時間, durationSec)`（**長い方優先**）。`OverrideGravity(multiplier, durationSec)` は **multiplier は後勝ち、durationSec は max**（hover 中に heavy stomp が来たら multiplier は heavy stomp の値で上書き、duration は両者の max）。この非対称は意図的：移動制限は累積したい / 重力ゲートは「最新の意図」を優先したい。ADR-0004 着手時に「合成規則を後勝ち統一にすべきか」の再評価候補として残す（本 ADR では上記規則で固定）。
13. **`Debug.Assert` は Release ビルドでも条件評価が残る**：Unity の `Debug.Assert` は `UNITY_ASSERTIONS` シンボル制御で、**Release Player でも stripping されず実行される**。Awake() 内 1 回実行のため performance 影響は無視できるが、FixedUpdate / Update 内で `Debug.Assert` を多用する場合は `#if UNITY_EDITOR || DEVELOPMENT_BUILD` ガードを別途用いること。本 ADR の Awake `Debug.Assert` は条件評価コストを許容する設計。

## Alternatives Considered

### Alternative A: Kinematic Rigidbody2D + 自作 Cast Solver + ICharacterMotor（採用）

- **Description**: 上記 Decision のとおり
- **Pros**:
  - Solver 権威性 1 frame 1 solve invariant が言語レベルで強制可能（interface に setter なし）
  - Box2D v3 マルチスレッドの衝突情報を `Cast` 系で活用しつつ Solver 自前でメトロイドヴァニア向け精密性を担保
  - Hitstop / OverrideGravity / LockHorizontalControl が motor 内蔵で Tier 0 から Pillar 3 検証可能
  - ability ↔ motor 双方向結合を構造的に防止（A2 解決）
  - 4 職分の ability が **追加コードゼロ**で意図 API のみで表現可能
- **Cons**:
  - Cast ベース Solver の自作は 1-1.5 週間の集中実装が必要
  - Box2D v3 の Cast 系 API 名が post-cutoff のため要検証（V1）
  - 初期チューニング（Coyote / Jump Buffer / Wall Slide 等）は試行錯誤コスト
- **Estimated Effort**: 基準（他案との比較ベース）
- **Selection Rationale**: A2 + CD1 + 言語レベルでの結合防止という 3 要件を同時に満たす唯一の選択肢

### Alternative B: Dynamic Rigidbody2D + linearDamping + Velocity 制御

- **Description**: `Rigidbody2D.bodyType = Dynamic`、`AddForce` / `linearVelocity` 制御。Unity 標準 2D Physics に Solver 全面委譲
- **Pros**:
  - 実装工数が最小（Box2D Solver を全面活用、自作 Cast ロジック不要）
  - `OnCollisionEnter2D` 等の Unity 標準 callback が使え、衝突情報取得が容易
  - 物理パズル要素（押せる箱・斜面・流体）への拡張が後付けで容易
- **Cons**:
  - **空中制御の即応性が物理的に弱い**：linearDamping + AddForce では Hollow Knight 級のキビキビした空中制御に届かない（メトロイドヴァニア定番要件未達）
  - Coyote Time / Jump Buffer / Wall Slide 等の入力バッファ系を motor 外で組むと結合点が増え、ability から motor の velocity を直接書きたくなる drift（A2 違反 risk 高）
  - **Pillar 3「歯ごたえ」の重量感を Damping カーブで再現するのは非常に困難**：プロのチューニングが Tier 0 6 週間に入らない
  - `Time.fixedDeltaTime` 変動下で Determinism 担保が難しい（リプレイ機能の土台が弱る）
- **Estimated Effort**: ×0.6（実装は速いが、Pillar 3 を満たすチューニングが終わらない）
- **Rejection Reason**: Pillar 3「歯ごたえ」要件を 6 週間で満たせない蓋然性が高い + A2 違反 drift リスク

### Alternative C: Pure Transform + OverlapBox + 自前 Resolve（Box2D 不使用）

- **Description**: `Rigidbody2D` を使わず、`Transform.position` を直接書き、`Physics2D.OverlapBox` / `BoxCast` で衝突検出して自前で resolve
- **Pros**:
  - Solver の完全制御が可能（最大の自由度）
  - Determinism が `Time.fixedDeltaTime` 固定で完璧に再現
- **Cons**:
  - **Box2D v3 マルチスレッド + リプレイ向け衝突情報のメリットを放棄**：Unity 6.3 の最大の物理改善を捨てる
  - `Rigidbody2D` を使わないため、敵側 AI（同じく `ICharacterMotor` 抽象を共有予定）との互換性が低下する（敵は Dynamic Rigidbody2D で実装したい場面がある）
  - `Cinemachine 3` の 2D Confiner Extension が `Collider2D` 前提のため、camera 側で workaround が必要
  - Trigger / Sensor 系（ベンチセーブ・ゲート）と統合する際に Rigidbody2D 不在の例外処理が増える
  - テスト工数（Solver の単体テスト + 4 方向衝突マトリクス）が ×1.5
- **Estimated Effort**: ×1.4（Solver 自由度高だが、外部システム結合工数が膨れる）
- **Rejection Reason**: Box2D v3 メリット放棄 + Cinemachine 互換性損失 + 統合工数増。本作の単一プレイヤーキャラ規模では over-engineering

### Alternative D: サードパーティアセット（Corgi Engine / Pro2D Platformer / Rewired+aspect Movement）

- **Description**: Asset Store から既製 2D platformer kit を導入
- **Pros**:
  - 即座に動く motor が手に入り、Tier 0 6 週間に余裕が生まれる
- **Cons**:
  - **Unityちゃん公式 PSB / 2D Animation 10.x との統合コストが未知数**（Corgi 等は標準 SpriteRenderer 前提が多い）
  - Class Switch（ADR-0001）の `ICharacterMotor` 抽象に合わせる adapter 工数が発生 → 結局 ICharacterMotor 実装が必要
  - **License 問題**：UCL 2.0 + サードパーティアセット License の両立検証が Q2（UCL 照会）と並列で必要、Tier 2b Steam EA 時のリスク
  - Asset 内部のコードが Black Box で、Unity 6.3 / Box2D v3 対応状況が不明
  - プロジェクトのコーディング規約（PascalCase / Forbidden Patterns）と合わない可能性
- **Estimated Effort**: ×0.5 短期 / ×1.3 累積（adapter + license + Black Box 対応で結局増える）
- **Rejection Reason**: License 不確実性 + Unity ちゃん統合不確実性 + 長期累積で純益マイナス

## Consequences

### Positive

- A2 申し送り「ability ↔ motor 双方向結合防止」が言語レベル（interface に setter なし）で強制される
- CD1 申し送り「Tier 0 で Hitstop を motor 内蔵」が `ApplyHitstop()` 1 メソッドで実現
- Pillar 3「歯ごたえ」の重量感が motor チューニング集中で MVP 内達成可能
- Pillar 1「切替が花」と Pillar 3「歯ごたえ」の両立 — Class Switch（ADR-0001）と motor は独立、`ClassChanged` event 経由でしか相互作用しない
- 4 職拡張・敵 AI 共通化・将来の Steam Deck Verified パフォーマンス目標すべてが同じ motor 抽象で対応可能
- Determinism 担保（fixedDeltaTime + 内部 random なし）でリプレイ・スピードラン文化への土台確立

### Negative

- **Cast ベース Solver 自作は 1-1.5 週間の集中実装が必要**（Tier 0 6 週間内に組む。MotorTuning 初期値の試行錯誤コストはこれと別）
- Box2D v3 API 名の post-cutoff 不確実性 — V1 検証プロトタイプで API 名確定が必須
- ScriptableObject `MotorTuning` を後から複数作成する場合（ボス専用・ステルスエリア専用）、Inspector swap UI が必要 — 本 ADR では Tier 1 検討課題として残置
- Class Switch（ADR-0001）の Tier 0 minimal feedback と CharacterController2D の Hitstop が独立に発火する — 重複発火（`ClassStateMachine.SwitchTo` と `motor.ApplyHitstop` が同フレーム）の演出干渉リスクは Tier 1 で `IVFXPublisher` / `IAudioPublisher` 経由統合時に解決

### Neutral

- 自作 Solver 採用により、Unity の物理エンジン更新（6.4 → 6.5）でも本 motor の挙動は基本不変。逆に Solver 改善の恩恵も自動では受けない
- `MotorTuning` ScriptableObject 化により Designer がインスペクタで重力等を変えられる — Forbidden Patterns「Magic numbers」を遵守

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **V1**: Unity 6.3 で `Rigidbody2D.Cast()` の overload / `linearVelocity` 改名が想定と異なる | Medium | High | Tier 0 spike 開始時に Unity Editor で API 名を実測。失敗時は擬似コードを修正し本 ADR Last Verified を更新 |
| **V2**: `Physics2D.SyncTransforms()` を `MovePosition` 直後に呼んでも Box2D v3 マルチスレッドで race condition が残る | Low | High | Unity 6.3 release notes + Box2D v3 統合 doc を追加調査。最悪の場合 `LowLevelPhysics2D` 直接利用へ Tier 1 移行（本 ADR Supersede 候補） |
| **V3**: ハイスピード移動（dash + jump 同時発動）で tunneling（壁すり抜け）発生 | Medium | High | (a) Cast ベース予測 solver で原理的に防止、(b) `JumpBufferSec` / `MaxFallSpeed` を tuning で抑制、(c) 物理スパイクで 30 m/s 級 dash 想定の境界テストを必須化 |
| **V4**: Motor `FixedUpdate` 1 回 ≤ 0.5 ms 予算超過 | Low | Medium | (a) Cast 配列事前確保で GC alloc 0 byte、(b) Cast コール 6 回上限、(c) Profiler で初回 FixedUpdate cold path を必ず計測 |
| **V5**: Unityちゃん公式 PSB の SkinAnchor / Pivot とコライダー中心が ずれて足裏接地の視覚バグ発生 | Medium | Medium | Sprite Library 提供 PSB の rig 仕様を Editor で確認、Pivot を足裏に揃える前処理 + コライダー中心 Y 値を `MotorTuning` で調整可能化 |
| **V6**: Class Switch 中 motor velocity が drift（ClassChanged event 購読時の race） | Low | Medium | motor は `ClassChanged` event を購読しない契約を本 ADR で明文化（Implementation Guidelines #8）。Class Switch System 側が motor を read のみする invariant をテスト 1 本で守る |
| **V7**: Tier 1 で Combat System が完成した後、motor の Hitstop と Combat System の Hitstop が競合 | High | Low | Tier 1 リファクタ計画で「motor の `ApplyHitstop` を Combat System から呼ぶ」フローに統一する（既に意図 API 経由で結合点が 1 箇所のため低コスト） |
| **V8**: 将来の `OverrideTuning(MotorTuning)` 追加で Inspector swap UI が必要になる | Medium | Low | Tier 1 で必要が出た時点で別 ADR として `MotorTuning` swap API を追加。本 ADR では `_tuning` 単一参照で固定 |
| **V9**: `LockHorizontalControl` 中の Jump Buffer 消費規則（先行入力 4-6 フレーム）が本 ADR 単独で確定できない | Low | Medium | Combo Input Buffer ADR で確定する責務委譲。本 ADR は `LockHorizontalControl` で水平軸のみ抑制し、Jump 入力の Buffer 消費可否は決めない。Pillar 1（切替即時性）+ Pillar 3（被ダメージ硬直）の境界仕様は Combo Input Buffer ADR との連携設計で解決 |
| **V10**: V2 失敗時の代替候補 `UnityEngine.LowLevelPhysics2D` の API が完全 post-cutoff | Medium | Low | 本 ADR 範囲外。V2 失敗時は別 spike として `LowLevelPhysics2D` の Managed wrapper 有無 / スレッドセーフ保証 / Unity License 制限を調査する別 ADR を起こす。本 ADR Rollback 経路は Alternative B (Dynamic Rigidbody2D) または Alternative C (Pure Transform) を優先 |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| **CPU (1 FixedUpdate)** | N/A | 0.4-0.5 ms | **0.5 ms** |
| **GC Alloc / FixedUpdate** | N/A | 0 byte | 0 byte |
| **Cast コール / FixedUpdate** | N/A | 4-6 回 | 6 回上限 |
| **Memory (motor instance)** | N/A | ~200 byte（プリミティブ field） | 1 KB |
| **Memory (MotorTuning SO)** | N/A | ~100 byte / asset | 200 byte |
| **Load Time** | N/A | 0 ms（Awake キャッシュ） | N/A |

### CPU Breakdown（0.5 ms 内訳）

| 処理 | 想定コスト |
|---|---|
| 入力 + intent 計算 | 0.05 ms |
| 重力 + impulse 適用 | 0.05 ms |
| Cast x 4-6（水平 / 垂直 / wall 各 2 方向） | 0.20 ms |
| `MovePosition` + `Physics2D.SyncTransforms()` | 0.15 ms |
| 状態更新 + event 発火 | 0.05 ms |
| **合計** | **0.50 ms** |

### GC Allocation 注意点

- `RaycastHit2D[]` は Awake で 1 回確保、FixedUpdate 中は再利用 → 0 byte
- `event Action` の delegate invocation は subscriber 数 ≤ 5 で alloc 観測なし、Tier 1 で大量購読が出たら interface 経由に置換
- `struct CollisionInfo` は値型、heap alloc なし

### Profile タイミング

- Cold path（最初の 10 FixedUpdate）：Box2D v3 マルチスレッド初期化コストで 0.6-0.8 ms 許容
- Warm path（定常）：≤ 0.5 ms 期待
- **Profiler GC.Alloc / Physics 2D / CPU Usage を初回 dash + jump 同時発動時に計測必須**（V3 + V4 の同時検証）

## Migration Plan

新規実装のため移行不要。ただし Tier 0 → Tier 1 のリファクタ計画を以下に明示：

1. **Tier 0 実装**：`CharacterController2D` が Hitstop を内蔵、Combat System 不在で Pillar 3 検証
2. **Tier 1 リファクタ Step 1**：Combat System が完成、`IDamageReceiver` が `motor.ApplyHitstop(hitData.HitstopSec)` を呼ぶフローに統一。motor 内 hitstop ロジックは変更なし、呼び出し元のみ移行
3. **Tier 1 リファクタ Step 2**：`IVFXPublisher`（ADR-0003）注入後、motor の `Landed` event を VFX 着地エフェクトに subscribe、`HitstopApplied` event を VFX freeze frame に subscribe
4. **Tier 2 リファクタ候補**：`OverrideTuning(MotorTuning, durationSec)` API 追加判断（ボス戦専用パラメータが必要になった場合）。要件が出るまで保留
5. **Tier 3 候補**：敵 AI で `ICharacterMotor` を共通化（敵にも本 ADR の motor 抽象を再利用）

**Rollback plan**：V1 / V3 検証失敗時、本 ADR を `Superseded by ADR-0002a` にし、Alternative B（Dynamic Rigidbody2D）または Alternative C（Pure Transform）の新 ADR を起こす。Pillar 3 要件は維持、Solver 戦略のみ差し替え。

## Validation Criteria

### MVP 達成条件（Tier 0 終了時）

- [ ] 入力受信から `Rigidbody2D.MovePosition` 反映が同 FixedUpdate サイクル内（R1）
- [ ] FixedUpdate 内 `MovePosition` 呼び出し 1 回限定の単体テスト pass（R2 invariant）
- [ ] `ICharacterMotor` interface 経由でしか motor 状態を変更できないコンパイル時保証（R3、internal 確認テスト）
- [ ] `ApplyHitstop(0.04f)` で Solver が完全停止し 0.04s 後に再開する単体テスト pass（R4）
- [ ] 4 ability 想定（dash / hover / heavy stomp / knockback）のすべてが意図ベース API のみで表現できる shim テスト pass（R5）
- [ ] Class Switch（ADR-0001 R5 spike）と同時に動作させ、空中切替時の velocity 連続性を確認（R6）
- [ ] `Physics2D.autoSyncTransforms` 警告ログが出ないことを Editor 起動時に確認（R7）
- [ ] Unityちゃん公式 PSB で着地時の足裏接地位置が SkinAnchor と整合（V5、Editor 目視）
- [ ] motor `FixedUpdate` ≤ 0.5 ms（warm path 平均、Unity Profiler 計測、V4）
- [ ] 30 m/s dash 想定の境界テストで tunneling 発生なし（V3）

### Tier 1 達成条件

- [ ] Combat System が `motor.ApplyHitstop` を呼ぶフローへ移行完了
- [ ] `IVFXPublisher.PlayCue` を `Landed` / `HitstopApplied` event subscriber 経由で発火
- [ ] motor の `OverrideTuning(MotorTuning, durationSec)` 追加要件評価（ボス戦・ステルスエリアで必要なら別 ADR）

## Validation Gate

> **本 ADR は本セクション V1-V5 を通過するまで `Accepted` に昇格しない。**

### V1-V5 検証プロトタイプ（最優先タスク）

V1（Cast 系 API 名）と V3（tunneling）が偽の場合、Decision の根幹が崩壊する。以下のプロトタイプで実機検証する：

**Cross-System Spike Integration（重要）**:
**ADR-0001 R5 spike（SpriteSkin × SpriteLibraryAsset ランタイムスワップ）と本 ADR V1-V5 spike を同一プロトタイプセッションで合体実施する。** 両 spike とも「Unityちゃん公式 PSB + 単一 Player GameObject」を要求し、別シーンで実施するのは工数の二重消費かつ R6（Class Switch 中 motor velocity 連続性）の検証機会を失う。合体シーンでは **空中切替時の velocity 連続性（R6）を必須検証シナリオに追加**する（dash 中に R1 切替 → motor velocity が drift しないこと、`ClassChanged` event 発火後も `_velocity` / `_pendingImpulse` が保持されること）。失敗時の Rollback Plan は両 ADR とも独立しており、合体実施は Rollback の独立性を損なわない。

**プロトタイプ範囲**:
- 単一シーン、最小限の Player GameObject（ADR-0001 R5 spike と共通）
- Unityちゃん公式 PSB（オリジナルの Skeletal Sprite アセット）を 1 体配置
- `CharacterController2D` + `Kinematic Rigidbody2D` + `CapsuleCollider2D` 構成
- 2 SpriteLibraryAsset（剣士相当・弓士相当、ADR-0001 R5 範囲）+ R1 入力切替
- 7 種テストシナリオ実装（V1-V5 + R5 + R6、≈130 行 C# + 1 シーン）

**通過条件（すべて満たすこと）**:
- (V1-a) `Rigidbody2D.Cast(Vector2 direction, RaycastHit2D[] results)` の正確な overload を Editor で確認、本 ADR の擬似コードを確定形に更新
- (V1-b) `Rigidbody2D.linearVelocity` の `velocity` からの置換状況（warning か error か）を Editor で確認、`MotorTuning` 初期値設定経路を確定
- (V2) `MovePosition` 直後の `Physics2D.SyncTransforms()` 明示呼び出しで Box2D v3 マルチスレッド race が起きないことを 1000 FixedUpdate 連続実行で確認、加えて **「`MovePosition` → `Physics2D.SyncTransforms()` → 同 FixedUpdate 内の次 Cast クエリ」の 3 ステップ sequence で Cast が更新後 Transform を参照する**ことを単体テストで実証する（意図的に位置変更 → 直後 Cast 実行のシナリオで、ヒット結果が更新後位置基準であることを assert）
- (V3) 30 m/s 級 dash + jump 同時発動で tunneling（壁すり抜け）発生 0 件 / 100 試行
- (V4) `FixedUpdate` 平均 ≤ 0.5 ms（warm path）、cold path ≤ 0.8 ms（Unity Profiler、`Physics 2D` + `CPU Usage` 計測）
- (V5) Unityちゃん公式 PSB の SkinAnchor / 足裏接地が `MotorTuning.SkinWidth` 微調整で視覚的に整合

**通過時のアクション**:
1. 本 ADR の `Status` を `Accepted` に変更
2. `Engine Compatibility` の `Verification Required` を「✅ 検証済（YYYY-MM-DD）」と注記
3. `Decision` の擬似コード API 仮表記を確定形に更新（`linearVelocity` / `Rigidbody2D.Cast` overload 等）
4. `docs/registry/architecture.yaml` に **既に Proposed 段階で前倒し追記済の本 ADR stance** の API 名項目を確定形に revise（前倒し追記時点では「API 名は V1 後に確定」注記付き）
5. ADR-0001 Forward Reference の `ICharacterMotor` を本 ADR への確定リンクに更新

**失敗時のアクション**:
1. 本 ADR を `Superseded by ADR-0002a` にマーク
2. Alternative B（Dynamic Rigidbody2D）または Alternative C（Pure Transform）の新 ADR を起こす
3. Pillar 3 design test を再評価し、必要なら motor 戦略を変更

**担当・記録**:
- 実装担当: ユーザ（プロジェクトリード）または `unity-specialist` 経由のスパイクタスク
- 記録先: 本 ADR `Engine Compatibility` セクション + `production/qa/evidence/` への計測結果保管

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/game-concept.md` | Pillar 3 | 「歯ごたえ」=操作レスポンスの重量感 | `MotorTuning` で重力・加速・コヨーテ等を中央集権化、Hitstop を motor 内蔵 |
| `design/gdd/game-concept.md` | Pillar 1 | 切替時 motor 中断不可 invariant | motor は `ClassChanged` event を購読しない契約を本 ADR で明文化、Class Switch 中の物理連続性を保証 |
| `design/gdd/game-concept.md` | Risks R-T1 | Kinematic CharacterController2D 自作 | 本 ADR が Decision として確定、Box2D v3 マルチスレッド対応 |
| `design/gdd/game-concept.md` | Technical Considerations | Kinematic Rigidbody2D + 自作 CharacterController2D | Decision 通り |
| `design/gdd/systems-index.md` | A2 双方向結合防止 | ability が motor 内部状態を直接書かない | `ICharacterMotor` interface に setter なし、`Rigidbody2D` への参照を `internal` で閉じ込め、ability は意図 API のみ |
| `design/gdd/systems-index.md` | A2 意図 API | `RequestImpulse` / `OverrideGravity` / `LockHorizontalControl` | 本 ADR `Key Interfaces` で定義、`SetFacing` / `ApplyHitstop` 追加 |
| `design/gdd/systems-index.md` | CD1 Tier 0 Hitstop | 30-50ms hitstop を CC2D 側に持たせる | `ApplyHitstop(durationSec)` + `MotorTuning.HitstopDefaultSec = 0.04s`（範囲内） |
| `design/gdd/systems-index.md` | High-Risk: CC2D | 完全自作 Kinematic、Box2D v3 整合 | `Physics2D.SyncTransforms()` 明示 + `Cast` ベース Solver で対応、V1-V3 検証ゲートで実証必須 |
| `design/gdd/systems-index.md` | Localization Discipline | Strings.Motor.* キー参照 | Implementation Guidelines #移行で明文化（Tier 1 で String Table 経由に移行） |
| `.claude/docs/technical-preferences.md` | Forbidden | `Physics.autoSyncTransforms` | `Physics2D.SyncTransforms()` 明示呼び出しを Decision に組込 |
| `.claude/docs/technical-preferences.md` | Forbidden | `GetComponent()` in `Update()` | `Awake()` で `_rb` / `_collider` キャッシュ |
| `.claude/docs/technical-preferences.md` | Forbidden | Magic numbers | すべて `MotorTuning` ScriptableObject 経由 |
| `.claude/docs/technical-preferences.md` | Performance Budget | Physics ≤ 2 ms | motor ≤ 0.5 ms（Physics 全体予算の 1/4） |

## Related

- **`design/gdd/game-concept.md`** — Pillar 3, Technical Considerations (Kinematic + 自作 CC2D), R-T1, R-T3
- **`design/gdd/systems-index.md`** — Architecture Note A2, Creative Director Note CD1, High-Risk: CharacterController2D
- **`docs/architecture/adr-0001-class-switch-architecture.md`** — Forward Reference の `ICharacterMotor` を本 ADR が確定。`AbilityContext.Motor` で本 ADR の interface を埋める
- **`.claude/docs/technical-preferences.md`** — Forbidden Patterns（autoSyncTransforms / Magic numbers / GetComponent in Update）、Engine Specialists（unity-specialist primary）
- **`docs/engine-reference/unity/VERSION.md`** — Unity 6.3 LTS pin、Knowledge Risk MEDIUM-HIGH
- **`docs/engine-reference/unity/breaking-changes.md`** — `Physics.autoSyncTransforms` 非推奨セクション、Box2D v3 統合
- **`docs/engine-reference/unity/deprecated-apis.md`** — Physics 表（`Physics2D.autoSyncTransforms` deprecated）
- **未来の関連 ADR**:
  - ADR-0003 (planned): VFX System Boundary + IVFXPublisher — 本 ADR の `Landed` / `HitstopApplied` event を Tier 1 で VFX subscriber に接続
  - ADR-0004 (planned): Class Abilities System 詳細 — `AbilityContext.Motor: ICharacterMotor` を本 ADR で確定済
  - 将来の Combat System ADR — `IDamageReceiver` が `motor.ApplyHitstop` / `motor.RequestImpulse(knockback)` を呼ぶ経路
- **Followup Tasks**:
  - `[Spike]` V1-V5 検証プロトタイプ（本 ADR Validation Gate 通過のため）
  - `[Test]` `ICharacterMotor` setter 不在のコンパイル時保証テスト（R3）
  - `[Test]` FixedUpdate 1 サイクル内 `MovePosition` 1 回のみ invariant テスト（R2）
  - `[Test]` `ApplyHitstop` 中の Solver 完全停止単体テスト（R4）
  - `[Sync]` 本 ADR Accepted 後、`technical-preferences.md` Architecture Decisions Log を更新（ADR-006 候補項目を本 ADR-0002 に同期）
  - `[Sync]` ADR-0001 Forward References の `ICharacterMotor` を本 ADR への確定リンクに更新
