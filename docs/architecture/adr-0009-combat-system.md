# ADR-0009: Combat System（HitConfirmed → IDamageReceiver Thin Mediator）

## Status

**Proposed (Validation Gate: CS0-CS5)**

> ADR-0008 (Class Abilities System) が Proposed のため、本 ADR も同様に Proposed。
> ADR-0008 の CA0-CA8 通過後に本 ADR の CS0-CS5 検証を実施し、両者を連動 Accepted へ昇格する。
> Tier 0 MVP: CombatSystem を player prefab に配置し、DummyEnemy の
> HitConfirmed → hitstop + knockback + disable を検証することで Pillar 3「歯ごたえ」を確認する。

## Date

2026-04-29

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Core / Gameplay（Combat mediator） |
| **Knowledge Risk** | LOW — 本 ADR の実装は純 C# の event 購読 + `GetComponent<T>()` + `Rigidbody2D.linearVelocity`（DummyEnemy のみ）。engine-specific post-cutoff API 依存ゼロ |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`docs/architecture/adr-0008-class-abilities-system.md`（HitConfirmed / HitData / ClassAbilityData フィールド定義）、`docs/architecture/adr-0002-character-controller-motor.md`（ApplyHitstop / RequestImpulse / ICharacterMotor）、`design/gdd/systems-index.md`（#12 Combat System / CD2 Tier 0 scope）、`docs/registry/architecture.yaml`（ability_hit_contract / motor_intent_command） |
| **Post-Cutoff APIs Used** | `Rigidbody2D.linearVelocity` — Unity 6.0 で `.velocity` から改名。旧名は `[Obsolete]`。DummyEnemy で使用（Tier 0 のみ）。Tier 1 では `ICharacterMotor.RequestImpulse` に移行するため実質的影響ゼロ |
| **Verification Required** | CS0: HitConfirmed → OnHitConfirmed コールバック到達 / CS1: 攻撃側 Hitstop 適用確認 / CS3: DummyEnemy 1 ヒット disable |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0008 Class Abilities System（`HitConfirmed` event / `HitData` struct / `ClassAbilityData.HitstopSec` / `.KnockbackImpulse` / `.Damage` — Proposed だが event signature 確定済みのため並列進行可）、ADR-0002 CharacterController2D（`ICharacterMotor.ApplyHitstop(float)` — 同 Proposed） |
| **Enables** | Health & Damage System ADR（`IDamageReceiver` contract を本 ADR が確定、Tier 1 `HealthComponent` が実装）、Enemy AI System（Tier 0 `DummyEnemy` が `IDamageReceiver` を実装）、HUD ADR（`HealthComponent.HealthChanged` event は Health & Damage System ADR で定義） |
| **Blocks** | `design/gdd/combat-system.md` GDD authoring（`IDamageReceiver` interface 確定前は damage pipeline が定義できない）、Enemy AI Tier 0 story authoring（`IDamageReceiver` 実装先が未定義） |
| **Ordering Note** | ADR-0008 CA4（HitConfirmed 発火テスト）通過後に CS0-CS1 確認を実施。ADR-0002 V2（ApplyHitstop テスト）と並走可 |

## Context

### Problem Statement

ADR-0008 が確立した `AbilityExecutor.HitConfirmed: event Action<HitData>` は「衝突を検出したことを通知する」が、
誰が被ダメージを受け取るか・Hitstop を誰が発火するか・Knockback をどう伝達するかは未定義である。
Tier 0 で「Pillar 3 歯ごたえ = hitstop + knockback + impact frame」を自己検証するには、
`HitConfirmed → 攻撃側 hitstop + IDamageReceiver.TakeDamage` のルーティング層が必要。

ADR-0002 は `ICharacterMotor.ApplyHitstop` を提供済み（CD1）。ADR-0008 は `HitData.SourceAbility` で
`ClassAbilityData` を直参照できるように設計済み（D1）。不足しているのは：

- `IDamageReceiver` interface の shape（signature、asmdef 配置）
- `CombatSystem` MonoBehaviour の責務と配置（player-local vs. scene singleton）
- `ClassAbilityData` の combat fields（`Damage` / `HitstopSec` / `KnockbackImpulse` — ADR-0008 の SO 定義に追記が必要）
- Tier 0 `DummyEnemy` の `IDamageReceiver` 実装方針
- Health & Damage System（Tier 1）への拡張経路

### Constraints

- **GC Alloc**: `CombatSystem.OnHitConfirmed` の hot path は 0 byte GC alloc（`GetComponent` は per-hit 1 回の Tier 0 許容）
- **Performance**: `OnHitConfirmed` ≤ 0.05ms / 発火（per-hit overhead）
- **Interface 配置**: `IDamageReceiver` は `Game.Core.asmdef`（Health & Damage / Enemy AI が参照するため）
- **CombatSystem 配置**: `Game.Gameplay.asmdef`、player prefab の同 GameObject 上 sibling component
- **ClassAbilityData 拡張**: ADR-0008 の `ClassAbilityData` SO に `Damage / HitstopSec / KnockbackImpulse` フィールドを追記（本 ADR 実装時に `src/core/abilities/ClassAbilityData.cs` を更新）
- **Tier 0 scope**: HP / damage calculation なし。DummyEnemy は 1 ヒット即死、hitstop + knockback の feel のみ確認
- **Singleton 不使用**: Combat は Feature 層（foundation_singleton_pattern 適用外）。player prefab の component として動作

### Requirements

- **R1**: `IDamageReceiver` interface を `Game.Core.asmdef` に配置: `void TakeDamage(float damage, Vector2 knockbackImpulse)`
- **R2**: `CombatSystem : MonoBehaviour` を `Game.Gameplay.asmdef` に配置、player prefab に添付
- **R3**: `CombatSystem.Awake()` で `GetComponent<ICharacterMotor>()` を `_playerMotor` にキャッシュ
- **R4**: `CombatSystem.OnEnable()` で `_abilityExecutor.HitConfirmed += OnHitConfirmed`、`OnDisable()` で Unsubscribe（leak 防止）
- **R5**: `OnHitConfirmed(HitData data)` で: (1) `_playerMotor.ApplyHitstop(data.SourceAbility.HitstopSec)`、(2) `data.HitCollider.GetComponent<IDamageReceiver>()?.TakeDamage(damage, knockback)` ただし `knockback = data.HitNormal * data.SourceAbility.KnockbackImpulse`
- **R6**: `ClassAbilityData`（ADR-0008 SO）に `[Header("Combat")]` セクションを追記: `float Damage = 1f; float HitstopSec = 0.04f; float KnockbackImpulse = 5f;`（本 ADR の実装 story で ADR-0008 実装ファイルを更新）
- **R7**: Tier 0 `DummyEnemy : MonoBehaviour, IDamageReceiver` を `Game.Gameplay.asmdef` に配置: `TakeDamage` で `_rb.linearVelocity = knockbackImpulse`（AddForce 禁止 — 理由後述）+ `gameObject.SetActive(false)`
- **R8**: `data.SourceAbility == null` の防御的 null guard — `TakeDamage` を呼ばず `Debug.LogWarning` を出力
- **R9**: Unit test: CS0-CS5 全 gate を EditMode / PlayMode で自動実施

## Decision

Combat System を **Thin Mediator パターン（イベントルーター + 攻撃側 Hitstop + IDamageReceiver 委譲）** で実装する。

`CombatSystem` は：
1. `AbilityExecutor.HitConfirmed` を subscribe（player prefab の sibling component として）
2. 攻撃側 motor に `ApplyHitstop` を適用（Pillar 3 hit feel の主役）
3. 被撃側に `IDamageReceiver.TakeDamage(damage, knockbackImpulse)` を委譲（ゲームプレイ応答はすべて receiver が責任を持つ）

`CombatSystem` はダメージ計算・HP 管理・状態異常を担わない（Tier 1 Health & Damage System ADR に委任）。

### Architecture Diagram

```
[Player GameObject]
  ├─ AbilityExecutor         (ADR-0008, ExecutionOrder: -60)
  │   └─ HitConfirmed: event Action<HitData>
  │         │ Subscribe(OnEnable) / Unsubscribe(OnDisable)
  ├─ CombatSystem            (ADR-0009, ExecutionOrder: -40)
  │   └─ OnHitConfirmed(HitData data)
  │         ├─ 1. _playerMotor.ApplyHitstop(data.SourceAbility.HitstopSec)
  │         │        └─▶ CharacterController2D (ADR-0002) — 攻撃側が HitstopSec 秒停止
  │         └─ 2. data.HitCollider.GetComponent<IDamageReceiver>()
  │                  └─▶ IDamageReceiver.TakeDamage(damage, knockback)
  └─ CharacterController2D   (ADR-0002, ICharacterMotor 実装)

[Tier 0 Enemy GameObject]
  ├─ Collider2D              (hitbox — OverlapBoxNonAlloc で検出)
  ├─ Rigidbody2D             (dynamic, knockback 受け取り)
  └─ DummyEnemy : IDamageReceiver
       └─ TakeDamage(damage, knockbackImpulse)
            ├─ _rb.linearVelocity = knockbackImpulse  // AddForce 禁止（Box2D v3 同フレーム SetActive 競合）
            └─ gameObject.SetActive(false)  // 1-hit KO (Tier 0)

[Tier 1 Enemy — Health & Damage System ADR が定義]
  ├─ Collider2D
  ├─ CharacterController2D (enemy motor、optional)
  └─ HealthComponent : IDamageReceiver
       └─ TakeDamage(damage, knockbackImpulse)
            ├─ CurrentHp -= damage
            ├─ _motor.RequestImpulse(knockbackImpulse)  // ICharacterMotor 経由
            └─ HealthChanged event 発火 → HUD 更新

ClassAbilityData SO 追加フィールド（ADR-0009 R6 — ADR-0008 ClassAbilityData.cs に追記）:
  [Header("Combat")]
  float Damage          = 1f     // Tier 1 Health & Damage が使用するベースダメージ
  float HitstopSec      = 0.04f  // CombatSystem が ApplyHitstop に渡す秒数 (CD1: 30-50ms)
  float KnockbackImpulse = 5f   // TakeDamage の knockbackImpulse 大きさ (方向: HitNormal)

データフロー例（地上攻撃 → DummyEnemy ヒット）:
  [Player] 攻撃入力 → ComboBuffer.TryConsume → AbilityExecutor.StartAbility(GroundAttack)
  frame 4-8 (HitboxActive) → OverlapBoxNonAlloc → DummyEnemy.Collider2D 検出
  → HitConfirmed({WorldPos, ActionName="attack", HitNormal=(1,0), HitCollider=DummyEnemy,
                   SourceAbility=GroundAttack, AttackerFacing=(1,0)})
        │ [CombatSystem.OnHitConfirmed]
  → 1. _playerMotor.ApplyHitstop(0.04f)   // player が 40ms 停止 → hit feel
  → 2. DummyEnemy.TakeDamage(1f, (5,0))  // AddForce(5N right) → enemy 飛ぶ + SetActive(false)
```

### Key Interfaces

```csharp
// Game.Core.asmdef — Game.Core.Combat namespace
namespace Game.Core.Combat
{
    /// <summary>
    /// ダメージを受け取ることができるエンティティの抽象。
    /// Health &amp; Damage System (Tier 1) / DummyEnemy (Tier 0) / 環境ハザード / ボス等が実装する。
    /// ノックバックの適用方法（ICharacterMotor.RequestImpulse / Rigidbody2D.linearVelocity 等）は
    /// 実装側が決定する — CombatSystem はルーターのみ。
    ///
    /// Signature 設計 Note (TD CONCERN-1):
    ///   Tier 0 は (float damage, Vector2 knockbackImpulse) で十分。
    ///   Tier 1 で属性 / 状態異常 / 攻撃者 ID 等が必要になった場合は
    ///   overload 追加 または `in DamageEvent evt` struct 版を Health &amp; Damage System ADR で再評価。
    ///   Tier 1 まで interface を変更しないことで Tier 0 DummyEnemy を throwaway として扱える。
    /// </summary>
    public interface IDamageReceiver
    {
        /// <summary>
        /// ダメージとノックバックを受け取る。
        /// </summary>
        /// <param name="damage">
        /// ベースダメージ値。Tier 0 の DummyEnemy は無視（1 ヒット即死）。
        /// Tier 1 の HealthComponent が防御値と合わせて最終ダメージを算出する。
        /// </param>
        /// <param name="knockbackImpulse">
        /// ノックバック衝力ベクトル（方向 + 大きさ）。
        /// CombatSystem が data.HitNormal * data.SourceAbility.KnockbackImpulse で算出済み。
        /// 受信側は ForceMode2D.Impulse または ICharacterMotor.RequestImpulse で適用する。
        /// </param>
        void TakeDamage(float damage, Vector2 knockbackImpulse);
    }
}
```

```csharp
// Game.Gameplay.asmdef — Game.Gameplay.Combat namespace
namespace Game.Gameplay.Combat
{
    /// <summary>
    /// HitConfirmed → 攻撃側 Hitstop + IDamageReceiver ルーティング の thin mediator。
    /// ダメージ計算・HP 管理・状態異常は担わない（Tier 1 Health &amp; Damage System に委任）。
    /// player prefab の AbilityExecutor と同 GameObject に配置する。
    /// </summary>
    // ExecutionOrder -40: AbilityExecutor(-60) の後に Awake を実行。
    // Update() なし — event-driven のみ（HitConfirmed callback から同フレーム内に処理完結）。
    [DefaultExecutionOrder(-40)]
    public sealed class CombatSystem : MonoBehaviour
    {
        // ─── Dependencies ────────────────────────────────────────────────
        [SerializeField] private AbilityExecutor _abilityExecutor;
        private ICharacterMotor _playerMotor;

        // ─── Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _playerMotor = GetComponent<ICharacterMotor>();

            // SerializeField 未アサイン時は同 GameObject から fallback
            if (_abilityExecutor == null)
                _abilityExecutor = GetComponent<AbilityExecutor>();
        }

        private void OnEnable()
        {
            if (_abilityExecutor != null)
                _abilityExecutor.HitConfirmed += OnHitConfirmed;
        }

        private void OnDisable()
        {
            if (_abilityExecutor != null)
                _abilityExecutor.HitConfirmed -= OnHitConfirmed;
        }

        // ─── Combat Routing ──────────────────────────────────────────────
        private void OnHitConfirmed(HitData data)
        {
            if (data.SourceAbility == null)
            {
                Debug.LogWarning("[CombatSystem] HitData.SourceAbility is null — skipping TakeDamage", this);
                return;
            }

            // 1. 攻撃側 Hitstop（Pillar 3 hit feel の主役）
            // ADR-0002: ApplyHitstop は Mathf.Max(_hitstopRemainSec, requested) で合成。
            // 被撃側(Tier 1 HealthComponent)が別途 ApplyHitstop を呼ぶ場合も max 合成で二重停止は起きない。
            _playerMotor?.ApplyHitstop(data.SourceAbility.HitstopSec);

            // 2. 被撃側へダメージ委譲
            // HitData.HitNormal は OverlapBoxNonAlloc の接触法線ではなく AttackerFacing の Vector2 化。
            // +1 = 右向き攻撃、-1 = 左向き攻撃。knockback 方向算出に使用する。
            var receiver = data.HitCollider.GetComponent<IDamageReceiver>();
            if (receiver != null)
            {
                Vector2 knockback = data.HitNormal * data.SourceAbility.KnockbackImpulse;
                receiver.TakeDamage(data.SourceAbility.Damage, knockback);
            }
        }
    }
}
```

```csharp
// Game.Gameplay.asmdef — Tier 0 DummyEnemy
namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// Tier 0 MVP 用ダミー敵。HP なし、1 ヒットで disable。
    /// Tier 1 で HealthComponent + EnemyController に置き換える（throwaway）。
    /// </summary>
    public sealed class DummyEnemy : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private Rigidbody2D _rb;

        public void TakeDamage(float damage, Vector2 knockbackImpulse)
        {
            // Tier 0: AddForce は SetActive(false) と同フレームで Box2D v3 マルチスレッドに
            // 衝撃が届く前に Rigidbody2D が無効化され impulse が破棄される場合がある。
            // linearVelocity 直接代入は即時反映され同フレーム内で確実にノックバック見た目を確保。
            // Unity 6.0 で Rigidbody2D.velocity → linearVelocity に改名（旧名は [Obsolete]）。
            // Tier 1 では ICharacterMotor.RequestImpulse 経由に移行する（throwaway code）。
            if (_rb != null && knockbackImpulse.sqrMagnitude > 0f)
                _rb.linearVelocity = knockbackImpulse;

            // 1-hit KO（Tier 0 のみ。Tier 1 は HealthComponent.TakeDamage に委任）
            gameObject.SetActive(false);
        }
    }
}
```

```csharp
// ClassAbilityData 追加フィールド（ADR-0009 R6 — ADR-0008 実装時に追記）
// [Header("Motor Commands")] セクションの後に追加する:

[Header("Combat")]
[Tooltip("被ダメージ側に渡すベースダメージ値。Tier 0 DummyEnemy は無視（1 ヒット即死）。" +
         "Health & Damage System (Tier 1) が防御値と合わせて最終ダメージを算出する。")]
[Range(0f, 999f)]
public float Damage = 1f;

[Tooltip("ヒット確定時に攻撃者 ICharacterMotor.ApplyHitstop に渡す秒数（Pillar 3 hit feel）。" +
         "MotorTuning.HitstopDefaultSec (0.04s) と同値をデフォルトとする。" +
         "0 を設定するとヒット感が薄れるためデザイナー判断で調整すること。")]
[Range(0f, 0.2f)]
public float HitstopSec = 0.04f;

[Tooltip("被撃側に渡すノックバック衝力の大きさ（N·s）。" +
         "方向は data.HitNormal（CombatSystem が乗算）。" +
         "0 を設定するとノックバックなし（ボス戦の特定フェーズ等に使用）。")]
[Range(0f, 30f)]
public float KnockbackImpulse = 5f;
```

## Alternatives Considered

### Alternative 1: CombatSystem がダメージ計算まで担当（All-in-one）

- **Description**: `CombatSystem` が `ClassAbilityData.Damage` × enemy defense 等の計算を行い、HP 減算まで責任を持つ
- **Pros**: 1 システムで complete な damage pipeline
- **Cons**: （1）Tier 0 scope 違反（systems-index CD2「HP/ダメージ計算なし」方針）、（2）Health & Damage System との責務重複で Tier 1 リファクタ必須、（3）「ダメージ計算 + hitstop + knockback + 状態異常 + HP 管理」の God Object 化
- **Rejection Reason**: YAGNI。Thin Mediator で Tier 0 検証が成立し、Tier 1 拡張も IDamageReceiver 実装だけで完了する。

### Alternative 2: AbilityExecutor が直接 IDamageReceiver を呼ぶ（Combat layer なし）

- **Description**: `AbilityExecutor.CheckHitbox()` 内で `GetComponent<IDamageReceiver>()?.TakeDamage(...)` を直接呼ぶ
- **Pros**: 中間層ゼロ、コードが短い
- **Cons**: （1）AbilityExecutor が「ability 発動」と「damage delivery」を双方担い God Object 化（ADR-0008 の separation 破壊）、（2）hitstop 発火責任が不明確、（3）Combat System の future 拡張（無敵フレーム / 属性相性 / 状態異常）が AbilityExecutor に蓄積
- **Rejection Reason**: ADR-0008 が `HitConfirmed` を「thin event notification」として設計した意図に反する。

### Alternative 3: Global Event Bus 経由の疎結合

- **Description**: `AbilityExecutor` → global `EventBus.Publish(HitEvent)` → `CombatSystem` が subscribe
- **Pros**: より疎結合
- **Cons**: （1）`EventBus` は architecture.yaml に登録されていない新規パターン（既存は C# event + direct_call のみ）、（2）debugging が困難（bus を流れる event の追跡コスト）、（3）ADR-0008 の `event Action<HitData>` が既に十分疎結合、（4）Event Bus 導入は別 ADR が必要
- **Rejection Reason**: 既存 registry の C# event pattern で十分。新規 EventBus パターンは scope 外。

### Alternative 4: Scene-level Combat Manager singleton（DDOL）

- **Description**: `CombatManager` を DontDestroyOnLoad singleton にし、全キャラクターの `HitConfirmed` を集約管理
- **Pros**: 複数キャラクター（協力プレイ等）に対応しやすい
- **Cons**: （1）本作は single-player、DDOL singleton は Foundation 5 に限定する `foundation_singleton_pattern` stance 適用外の Feature 層システム、（2）scene 間での戦闘状態持ち越しリスク
- **Rejection Reason**: Over-engineering。single-player 2D メトロイドヴァニアでは player-local component が最シンプル。

## Consequences

### Positive

- `IDamageReceiver` interface が `Game.Core.asmdef` に確定することで Health & Damage System / Enemy AI / Boss AI が独立に開発可能
- `CombatSystem` は薄い mediator（60 行以内）— 拡張なしで Tier 1 に渡せる
- `ClassAbilityData.HitstopSec` により各アビリティのヒット感がデザイナー調整可能（コード変更ゼロ）
- `DummyEnemy` が `IDamageReceiver` を実装することで Enemy AI Tier 0 story が即座に authoring 可能
- 攻撃側 `ApplyHitstop` の呼出し権限が `CombatSystem` に集約（`AbilityExecutor` は hitstop を呼ばない — hit confirmation 時に発火する責任を `CombatSystem` に持たせることで一貫性を保つ）

### Negative

- `CombatSystem` は `AbilityExecutor` の具象クラスを参照（`HitConfirmed` が `IAbilityExecutor` interface に現在未定義）。ADR-0008 の IAbilityExecutor に `event Action<HitData> HitConfirmed` を追加すれば解消できるが、Tier 0 scope では具象参照を許容する
- Tier 0 `DummyEnemy` が `Rigidbody2D.linearVelocity` 直接代入 — Tier 1 の `ICharacterMotor.RequestImpulse` 経由と一貫性が取れない。`Rigidbody2D.AddForce` は Box2D v3 マルチスレッドで `SetActive(false)` と同フレーム競合があるため linearVelocity に変更済み。Tier 1 移行時に `DummyEnemy` を `EnemyController + HealthComponent` に置き換える（意図的な throwaway code）
- `GetComponent<IDamageReceiver>()` の per-hit lookup — 1 ヒット = 1 `GetComponent` で許容範囲（Tier 1 で連続 multi-hit が想定される場合はキャッシュ方式に移行）
- 攻撃側と被撃側の両方が `ApplyHitstop` を呼ぶケース（double-hitstop）: CombatSystem が攻撃者の `ApplyHitstop` を呼び、Tier 1 `HealthComponent` がオプションで被撃側の `ApplyHitstop` を呼ぶと、双方の motor で hitstop が発火する。ADR-0002 の `ApplyHitstop` 実装は `_hitstopRemainSec = Mathf.Max(_hitstopRemainSec, requested)` で合成されるため同一 motor へ二重適用しても最大値が優先（安全）。攻撃者 motor ≠ 被撃側 motor なので double-stop は設計意図通り（攻撃者と敵が独立して停止）

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| `AbilityExecutor` と `CombatSystem` が同一 GameObject にいない prefab 構成 | LOW | MEDIUM | `Awake()` で `_abilityExecutor` null guard + `Debug.LogError` でセットアップ漏れを即検出 |
| `DummyEnemy.SetActive(false)` 後も Collider2D が HitConfirmed を受け続ける | LOW | LOW | ADR-0008 R12 の `_hitThisExecution` HashSet で 1 execution 内の重複ヒットは構造的に防止済み |
| `HitstopSec = 0` の ability（ダッシュ等）で `ApplyHitstop(0)` が呼ばれる | HIGH | LOW | `ApplyHitstop(0)` は ADR-0002 実装の `Mathf.Max(_hitstopRemainSec, 0)` → no-op（パフォーマンス影響なし） |
| Tier 1 Health & Damage System が `TakeDamage(float, Vector2)` 以上の情報（属性 / 状態異常）を必要とする | MEDIUM | MEDIUM | Interface overload または `HitData` を追加引数とした拡張で対応。Tier 1 ADR で再評価 |

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/systems-index.md` | CD2 Combat System Tier 0 | HP=1 dummy + hitstop + knockback のみ | `DummyEnemy : IDamageReceiver` で 1-hit disable、`ApplyHitstop(HitstopSec)`、`AddForce(knockback)` |
| `design/gdd/systems-index.md` | Pillar 3「歯ごたえ」 | hit feel = hitstop + knockback + impact frame | `HitstopSec: 0.04f`（CD1）を確実に発火。knockback で enemy 吹き飛び。VFX (hit_spark) は ADR-0003 / `ClassAbilityData.VfxOnHit` 経由 |
| `design/gdd/systems-index.md` | #12 Combat System MVP | ADR-0008 Enables 参照 | `IDamageReceiver` + `CombatSystem` thin mediator で MVP pipeline 完成 |
| `design/gdd/systems-index.md` | #13 Health & Damage System | `IDamageReceiver` contract | `IDamageReceiver` を `Game.Core` に確定、Tier 1 `HealthComponent` が実装するための contract 提供 |
| `docs/architecture/adr-0002-character-controller-motor.md` | `motor_intent_command` (combat-system future consumer) | combat-system が `ApplyHitstop` を呼ぶ | `CombatSystem` が `_playerMotor.ApplyHitstop(...)` を呼出し。registry の `additional_consumers_future.combat-system` が本 ADR で実現 |

## Performance Implications

| Metric | Value | Budget |
|--------|-------|--------|
| `OnHitConfirmed` (per hit) | `GetComponent` × 1 + `ApplyHitstop` × 1 + `TakeDamage` × 1 | ≤ 0.05ms / 発火 |
| `GetComponent<IDamageReceiver>()` | ~0.01-0.03ms / call | per-hit 1 回 |
| `ApplyHitstop(float)` | O(1) | ≤ 0.01ms |
| `IDamageReceiver.TakeDamage` dispatch | O(1) | ≤ 0.01ms |
| メモリ | `CombatSystem`: reference fields 2 個 ≈ 16 byte | < 64 byte |

`GetComponent<IDamageReceiver>()` は hit detection per-execution（1 ヒット = 1 call）のため hot-path ではない。
Tier 1 でボス戦など multi-hit per frame が想定される場合は enemy 側の `Awake()` キャッシュ方式に移行する。

## Migration Plan

```
[現在] HitConfirmed: event Action<HitData> (ADR-0008, subscriber なし)
            │
            ▼
[ADR-0009 Tier 0 実装 — Tier 0 PR に含む]
    1. IDamageReceiver.cs を Game.Core.asmdef に追加
    2. CombatSystem.cs を Game.Gameplay.asmdef に追加 (player prefab にアタッチ)
    3. DummyEnemy.cs を Game.Gameplay.asmdef に追加 (enemy prefab にアタッチ)
    4. ClassAbilityData.cs に Damage / HitstopSec / KnockbackImpulse 追記
    5. CS0-CS5 全 gate を CI PlayMode test に追加
    → Pillar 3「歯ごたえ」Tier 0 検証: hitstop + knockback を確認
            │
            ▼
[Tier 1 — Health & Damage System ADR（別 ADR）]
    HealthComponent : IDamageReceiver を実装
      ├─ CurrentHp -= damage
      ├─ _motor.RequestImpulse(knockbackImpulse)  // ICharacterMotor 経由に昇格
      └─ HealthChanged event → HUD 更新
    DummyEnemy を HealthComponent + EnemyController に置き換え
    CombatSystem のコード変更ゼロ（IDamageReceiver routing は変わらない）
            │
            ▼
[Tier 1 — Enemy AI System ADR（別 ADR）]
    EnemyController : ICharacterMotor 実装（enemy もモーター化）
    DummyEnemy.linearVelocity 直代入 → EnemyController._motor.RequestImpulse に移行
    ⚠️ Tier 1 以降: enemy 系の knockback に Rigidbody2D.AddForce / linearVelocity 直書きは禁止。
       external_motor_state_write forbidden pattern を enemy motor にも適用する（ADR-0002 準拠）。
       必ず ICharacterMotor.RequestImpulse 経由とし、Solver 権威性を維持すること。
```

## Validation Criteria

Validation Gate **CS0-CS5** 全通過で `Proposed` → `Accepted` に昇格。

- [ ] **CS0 — HitConfirmed routing**: PlayMode test で DummyEnemy Collider2D を配置し `AbilityExecutor` が `HitConfirmed` を発火 → `CombatSystem.OnHitConfirmed` が呼ばれること
- [ ] **CS1 — 攻撃側 Hitstop**: `HitConfirmed` 発火後に `_playerMotor.IsHitstopped == true` かつ `ClassAbilityData.HitstopSec` 秒経過後に `false` になること（ADR-0002 V2 と連動）
- [ ] **CS2 — TakeDamage 呼出し**: `DummyEnemy.TakeDamage` が呼ばれ、`damage` = `ClassAbilityData.Damage`、`knockbackImpulse.magnitude` ≈ `ClassAbilityData.KnockbackImpulse`（HitNormal 長 = 1 のため近似一致）で到達すること
- [ ] **CS3 — DummyEnemy 1-hit disable**: CS0 条件下で `DummyEnemy` が `gameObject.SetActive(false)` になること
- [ ] **CS4 — No double-hit**: 同一 ability execution 内で同一 `DummyEnemy` が 2 回 `TakeDamage` を受けないこと（ADR-0008 R12 の `_hitThisExecution` 保証の確認）
- [ ] **CS5 — パフォーマンス**: Profiler PlayMode で `OnHitConfirmed` 1000 回 → p99 ≤ 0.05ms。GC.Alloc = 0 byte（`GetComponent` は Tier 0 許容）

## Related Decisions

- **Depends on**: [ADR-0008 Class Abilities System](adr-0008-class-abilities-system.md)（`HitConfirmed` / `HitData` / `ClassAbilityData` combat fields）
- **Depends on**: [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md)（`ApplyHitstop`）
- **Enables**: Health & Damage System ADR（`IDamageReceiver` 実装 / `HealthComponent` / `HealthChanged` event）
- **Enables**: Enemy AI System ADR（Tier 0 `DummyEnemy` → Tier 1 `EnemyController` への移行経路）
- **Referenced by (future)**: HUD ADR（`HealthChanged` event は Health & Damage System 経由）
- **Engine reference**: [docs/engine-reference/unity/VERSION.md](../../docs/engine-reference/unity/VERSION.md)
- **Implementation files (post-Accepted)**:
  - `src/core/combat/IDamageReceiver.cs`（`IDamageReceiver` interface）
  - `src/gameplay/combat/CombatSystem.cs`（`CombatSystem` MonoBehaviour）
  - `src/gameplay/enemy/DummyEnemy.cs`（Tier 0 DummyEnemy、throwaway）
  - `src/core/abilities/ClassAbilityData.cs`（ADR-0009 R6 — combat fields 追記）
