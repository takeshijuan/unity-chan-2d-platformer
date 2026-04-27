# ADR-0001: Class Switch Architecture

## Status

**Proposed (Validation Gate: R5)**

> このADRは「Validation Gate」セクションに定義された R5 検証プロトタイプ通過まで Accepted に昇格しない。R5 が偽の場合、本Decisionの根幹（1フレーム視覚同期）が崩壊し、Alternative 再評価となる。

## Date

2026-04-27

## Last Verified

2026-04-27

## Decision Makers

- Project Lead（ユーザ）— 最終決定権
- `creative-director` 経由 CD-SYSTEMS gate — Pillar 1 整合性レビュー
- `technical-director` 経由 TD-SYSTEM-BOUNDARY + TD-ADR gate — アーキテクチャ層境界レビュー
- `producer` 経由 PR-SCOPE gate — Tier 0 スコープ整合性レビュー
- `unity-specialist` — Unity 6.3 LTS / 2D Animation 10.x 実 API レビュー

## Summary

本作 `職業オーブのレガシー` の Pillar 1「切替が、花になる」を成立させる Class Switch System のアーキテクチャを `ScriptableObject(ClassDefinition) + MonoBehaviour(ClassStateMachine) + 2D Animation 10.x SpriteLibrary` の3層構造で決定する。Tier 0 では VFX/Audio System 不在でも minimal feedback の color wash + SE を ClassStateMachine が自己内包し、Tier 1 で `IVFXPublisher` / `IAudioPublisher` へリファクタする経路を持つ。Hitstop の権威は ADR-0002 の `ICharacterMotor.ApplyHitstop()` にあり、Class Switch は呼び出し元として連携する（CD1 申し送り対応の責務分離、`/architecture-review 2026-04-27` CONCERN-1 解消）。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Animation (2D Sprite Library) + Core (ScriptableObject + State Machine) |
| **Knowledge Risk** | **HIGH** — `com.unity.2d.animation` 10.x の Sprite Library API は LLM 訓練データ cutoff（May 2025）以降に変更された可能性あり、`engine-reference/unity/modules/animation.md` 未収録 |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`modules/animation.md`、`breaking-changes.md`、`deprecated-apis.md` |
| **Post-Cutoff APIs Used** | `SpriteLibrary` / `SpriteLibraryAsset` / `SpriteResolver`（`com.unity.2d.animation` 10.x）、`AssetReferenceT<T>`（`com.unity.addressables` 2.0+） |
| **Verification Required** | (1) `SpriteLibrary` のプロパティ名（`spriteLibraryAsset` か `mainLibrary` か）、(2) `SpriteResolver` のメソッド名（`ResolveSpriteToSpriteRenderer()` か `ResolveSpriteToRenderer()` か）、(3) `spriteLibraryAsset` 設定後に自動 resolve されるかどうか、(4) `SpriteSkin`（Skeletal）+ `SpriteLibraryAsset` ランタイムスワップが Unityちゃん公式 PSB で動作するかどうか — **すべて R5 検証プロトタイプで実測必須** |

> **Note**: Knowledge Risk が HIGH のため、`com.unity.2d.animation` パッケージのメジャーバージョンが上がった場合は本 ADR を Superseded にし、新 ADR を起こすこと。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None（最初の ADR） |
| **Enables** | ADR-0002（CharacterController2D + ICharacterMotor）、ADR-0003（VFX System Boundary + IVFXPublisher）、ADR-0004（Class Abilities System 詳細） |
| **Blocks** | `design/gdd/class-switch-system.md` GDD authoring、`design/gdd/class-abilities-system.md` GDD authoring、`design/gdd/combat-system.md` GDD authoring（Class Abilities 経由で間接依存） |
| **Ordering Note** | 想定リストの「ADR-004 職業切替アーキテクチャ」（technical-preferences.md 内）は本ファイル ADR-0001 と同一決定。実ファイル番号は `/architecture-decision` の scan-based 採番により ADR-0001。technical-preferences.md の Architecture Decisions Log は本 ADR Accept 後に同期更新する |

## Context

### Problem Statement

Pillar 1「切替が、花になる」は本作のメカニカル中核であり、R1/L1 の 1 ボタンで剣士・弓士・魔法使いを切替えるアクションが視覚的に 1 フレーム同期しないと、「切替が遅い」プレイヤー体感によって本作のコンセプトそのものが破綻する（`game-concept.md` Pillar 1 design test）。

加えて、systems-index.md CD-SYSTEMS Note **CD1** により、Tier 0 hypothesis spike では VFX System / Audio System が不在のまま「切替の satisfaction」を検証する必要がある。Class Switch System 自身が minimal feedback（color wash + SE + hitstop）を自己内包するアーキテクチャでなければ、Go/Pivot/Stop ゲート判定が信頼できない。

さらに、systems-index.md Architecture Note **A1** により、Class Abilities System は God Object 化を構造的に防止するため `ClassAbilityData (SO) + IAbilityExecutor (MB) + AbilityContext (DI)` 三分割を要求される。本 ADR はその三分割の枠組みも確立する。

### Current State

未実装。プロジェクトは onboarding 完了直後の状態（`design/gdd/game-concept.md` のみ存在、ADR 0 件、systems-index.md は本 ADR 直前に作成）。

### Constraints

- **Engine**: Unity 6.3 LTS / URP 2D Renderer / 2D Animation 10.x（Skeletal）
- **Forbidden Patterns**（`technical-preferences.md` 由来）:
  - `Animator.Play()` で即時視覚切替を期待 — 次フレーム反映のため Pillar 1 不成立
  - `GameObject.Find()` / `FindObjectOfType()` / `GetComponent()` in `Update()` — Awake() でキャッシュ必須
  - Magic numbers — ScriptableObject に配置必須
- **Performance budget**: Update loop 全体で 4ms。Class Switch 1 回あたり ≤ 0.8ms（後述 Performance Implications）
- **Localization 規律**（systems-index.md より）: コードに生文字列禁止、`Strings.Class.Swordsman` 形式のキー参照のみ
- **PR-SCOPE 制約**: Tier 0 MVP は 9 システム / 6 週間。Class Switch 内部に minimal feedback を内包させて VFX/Audio System を Tier 0 から外す方針

### Requirements

- 入力受信から視覚反映が **同一フレーム内に完了**（Pillar 1 要件）
- 戦闘中・空中・コンボ中いつでも切替可能、クールダウン無し（Pillar 1 design test「切替を遅らせる/コスト化を拒否」）
- 切替時の視覚報酬（color wash）と聴覚報酬（SE）が **本システム自体から発火**（CD1）
- 4 職目（Tier 3）追加が **コード変更ゼロ**で対応可能
- Class Abilities が `ClassAbilityData / IAbilityExecutor / AbilityContext` 三分割される枠組み（CD A1）
- Tier 1 で IVFXPublisher / IAudioPublisher へリファクタ可能な経路（合意済み Tier 0 負債）

## Decision

**3 層構造を採用する：**

1. **`ClassDefinition`** — `ScriptableObject` による純データ
2. **`ClassStateMachine`** — `MonoBehaviour` による状態管理 + 切替ロジック + Tier 0 minimal feedback
3. **`SpriteLibrary` + `SpriteResolver`** — `com.unity.2d.animation` 10.x の API による視覚切替

`AbilityContext` の具体的フィールド構成は本 ADR で確定しない。**ADR-0002（CharacterController2D + ICharacterMotor）** と **ADR-0003（VFX System Boundary + IVFXPublisher）** および将来の Audio System ADR で段階的に確定する。本 ADR は注入点（`AbilityExecutor.Configure(AbilityContext)`）の存在のみコミットする。

### Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│ Player GameObject                                                    │
│  ├─ PlayerInput (Unity Input System 1.8+)                            │
│  ├─ ClassStateMachine (MonoBehaviour) ◀── Awake() で参照キャッシュ   │
│  │   ├─ [SerializeField] ClassDefinition[] _availableClasses         │
│  │   ├─ [SerializeField] SpriteRenderer _spriteRenderer              │
│  │   ├─ [SerializeField] SpriteLibrary _spriteLibrary                │
│  │   ├─ [SerializeField] SpriteResolver _spriteResolver              │
│  │   ├─ [SerializeField] AudioSource _audioSourceMinimal // Tier 0   │
│  │   ├─ [SerializeField] AbilityExecutor _abilityExecutor            │
│  │   ├─ [SerializeField] UnityEvent<ClassDefinition>                 │
│  │   │                   _onClassChangedUnityEvent                   │
│  │   ├─ private int _currentIndex                                    │
│  │   ├─ public event Action<ClassDefinition> ClassChanged            │
│  │   └─ private Coroutine _colorWashCoroutine                        │
│  ├─ AbilityExecutor (MonoBehaviour) — ADR-0004 で詳細                │
│  ├─ SpriteRenderer                                                   │
│  ├─ SpriteSkin (2D Animation 10.x — Unityちゃん公式 PSB 由来)        │
│  ├─ Animator (Mecanim、parameter-driven only — 切替時の Play() 禁止) │
│  └─ AudioSource (Tier 0 minimal feedback 用、Tier 1 で削除)          │
│                                                                      │
│ ScriptableObject Assets (Project Assets)                             │
│  ├─ ClassDefinition_Swordsman.asset                                  │
│  │   ├─ DisplayNameKey: "Strings.Class.Swordsman"                    │
│  │   ├─ ColorWash: #E63946                                           │
│  │   ├─ SpriteLibraryAssetVariant: SLA_Swordsman.asset               │
│  │   ├─ SwitchSEKey: "Strings.Audio.Switch.Swordsman"                │
│  │   ├─ SwitchSEMVP: AudioClip ref (Tier 0 のみ、Tier 1 で削除)      │
│  │   ├─ SwitchSERef: AssetReferenceT<AudioClip> (Tier 1 で活性化)    │
│  │   └─ Abilities: ClassAbilityData[]                                │
│  ├─ ClassDefinition_Archer.asset                                     │
│  └─ ClassDefinition_Mage.asset                                       │
│                                                                      │
│ SpriteLibraryAsset Variants (1 asset / class)                        │
│  ├─ SLA_Swordsman.asset (Idle/Run/Jump/Attack1/Attack2 Sprites)      │
│  ├─ SLA_Archer.asset                                                 │
│  └─ SLA_Mage.asset                                                   │
└─────────────────────────────────────────────────────────────────────┘

Switch Sequence (target: same frame as input)

  [Frame N]
   InputAction.performed (R1 pressed)
        │
        ▼
   ClassStateMachine.SwitchNext()
        │
        ├─ _currentIndex = (_currentIndex + 1) % _availableClasses.Length
        │
        ├─ // Visual swap (要 Editor 確認: 10.x API 名)
        │  _spriteLibrary.spriteLibraryAsset = newClass.SpriteLibraryAssetVariant
        │  _spriteResolver.ResolveSpriteToRenderer()
        │
        ├─ // Color wash (URP 2D + MPB は不採用、SpriteRenderer.color 直接書込)
        │  StartCoroutine(ColorWashCoroutine(newClass.ColorWash, durationSec: 0.15f))
        │
        ├─ // Audio (Tier 0: 直接 AudioSource、Tier 1: IAudioPublisher へ移行)
        │  _audioSourceMinimal.PlayOneShot(newClass.SwitchSEMVP)
        │
        ├─ // Ability reconfiguration (ADR-0004 で詳細)
        │  _abilityExecutor.Configure(newClass.Abilities)
        │
        ├─ // Notify subscribers
        │  ClassChanged?.Invoke(newClass)
        │  _onClassChangedUnityEvent?.Invoke(newClass)
        │
        ▼
   [End of Frame N — render with new sprite]
```

### Key Interfaces

```csharp
// ClassDefinition.cs
[CreateAssetMenu(menuName = "Game/Class Definition")]
public class ClassDefinition : ScriptableObject
{
    [Header("Identification")]
    public string DisplayNameKey;       // Localization key, e.g. "Strings.Class.Swordsman"

    [Header("Tier 0 Minimal Feedback")]
    public Color ColorWash;             // SpriteRenderer.color に 0.15s 適用
    public AudioClip SwitchSEMVP;       // Tier 0 only — 削除予定 (Tier 1)

    [Header("Tier 1 Forward-Compat")]
    public AssetReferenceT<AudioClip> SwitchSERef;  // Tier 1 で活性化、Addressables 経由

    [Header("Visual")]
    public SpriteLibraryAsset SpriteLibraryAssetVariant;

    [Header("Abilities — ADR-0004 で詳細")]
    public ClassAbilityData[] Abilities;
}

// ClassStateMachine.cs
public class ClassStateMachine : MonoBehaviour
{
    [SerializeField] private ClassDefinition[] _availableClasses;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SpriteLibrary _spriteLibrary;
    [SerializeField] private SpriteResolver _spriteResolver;
    [SerializeField] private AudioSource _audioSourceMinimal;  // Tier 0 only
    [SerializeField] private AbilityExecutor _abilityExecutor;
    [SerializeField] private UnityEvent<ClassDefinition> _onClassChangedUnityEvent;
    [SerializeField] private float _colorWashDurationSec = 0.15f;

    private int _currentIndex;
    private Coroutine _colorWashCoroutine;

    public ClassDefinition CurrentClass => _availableClasses[_currentIndex];
    public event Action<ClassDefinition> ClassChanged;

    private void Awake()
    {
        Debug.Assert(_availableClasses != null && _availableClasses.Length >= 2,
            "ClassStateMachine: _availableClasses は最低 2 要素必須", this);
        Debug.Assert(_spriteLibrary != null, "ClassStateMachine: _spriteLibrary 未アサイン", this);
        Debug.Assert(_spriteResolver != null, "ClassStateMachine: _spriteResolver 未アサイン", this);
        Debug.Assert(_spriteRenderer != null, "ClassStateMachine: _spriteRenderer 未アサイン", this);
        Debug.Assert(_audioSourceMinimal != null, "ClassStateMachine: _audioSourceMinimal 未アサイン (Tier 0)", this);
        Debug.Assert(_abilityExecutor != null, "ClassStateMachine: _abilityExecutor 未アサイン", this);
    }

    public void SwitchTo(int targetIndex) { /* same-frame swap、上記 Switch Sequence 通り */ }
    public void SwitchNext() => SwitchTo((_currentIndex + 1) % _availableClasses.Length);
    public void SwitchPrevious() => SwitchTo((_currentIndex - 1 + _availableClasses.Length) % _availableClasses.Length);
}
```

### Forward References (non-binding)

> 以下の名称は **systems-index.md Architecture Notes** で言及済みだが、本 ADR では契約として確定しない。各々の最終的なシグネチャは指定 ADR で確定する：

- **`AbilityContext`** — ADR-0004 で定義。`AbilityExecutor.Configure()` の引数として渡される。
- **`ICharacterMotor`** — ADR-0002 で定義。`AbilityContext` のフィールドとして含まれる予定（systems-index.md A2）。
- **`IComboBuffer`** — Combat 系 ADR で定義。
- **`IVFXPublisher`** — ADR-0003 で定義。Tier 1 で ClassStateMachine の color wash がこの経路に切替わる。
- **`IAudioPublisher`** — 将来の Audio System ADR で定義。Tier 1 で `_audioSourceMinimal` 直叩きがこの経路に切替わる。

### Implementation Guidelines

1. **API 名検証を最優先**：実装着手前に Unity Editor 上で 2D Animation 10.x の Sprite Library / SpriteResolver の API 名を実機確認すること。本 ADR の擬似コードは仮表記（要 Editor 確認）。
2. **`SpriteRenderer.color` を採用、MaterialPropertyBlock は不採用**：URP 2D Renderer + MPB の SRP Batcher 干渉問題を回避。バッチング効率の損失は color wash 0.15s の短時間のみ、許容範囲。
3. **Awake() で `Debug.Assert` 必須**：`RequireComponent` ではなく Inspector アサイン前提の Assert によりエラーを早期検出。
4. **`UnityEvent<ClassDefinition>` をインスペクタフックとして併存**：C# event は型安全な購読、UnityEvent は Designer がインスペクタから VFX/Audio Hook を後付けするため。
5. **AudioClip 二重持ち**（`SwitchSEMVP` + `SwitchSERef`）：Tier 1 で Addressables へ移行する際の asset データ破損を回避するための forward-compat。
6. **MVP では `_availableClasses[]` 直配列、Tier 1 で `UnlockClass(ClassDefinition)` 公開 API**：runtime unlock は MVP scope 外（PR-SCOPE 制約）。
7. **サイクル順序の単体テスト**：MVP テストとして「Inspector 配列順に R1 連打で正しく循環する」アサーション 1 本を必須化。
8. **ColorWashCoroutine の多重起動ガード必須**：連打時に複数コルーチンが重複起動して `SpriteRenderer.color` が競合・残色するリスクを防ぐため、`StartCoroutine` 前に `if (_colorWashCoroutine != null) StopCoroutine(_colorWashCoroutine)` を必須化。Tier 1 で `Awaitable`（Unity 6.0+）への置換時にも同等のキャンセル機構（`CancellationTokenSource.Cancel()` 経由）を維持する。`/architecture-review 2026-04-27` AP-1 対応（unity-specialist consultation）。

## Alternatives Considered

### Alternative 1: ScriptableObject + ClassStateMachine + SpriteLibrary（採用）

- **Description**: 上記 Decision のとおり
- **Pros**:
  - 4 職目追加がコード変更ゼロ（asset 追加のみ）
  - `SpriteRenderer.sprite` 直接代入経路で 1 フレーム視覚同期可能
  - Class Abilities の三分割（CD A1）と Tier 0 minimal feedback（CD1）を構造的に両立
  - Localization キー駆動で UI 文字列を静的に管理
- **Cons**:
  - SpriteLibrary 10.x API の post-cutoff 挙動が不確定（R5 で検証）
  - ClassDefinition と ClassAbilityData の 2 段 SO 階層は学習コストあり
- **Estimated Effort**: 基準（他案との比較ベース）

### Alternative 2: Animator Controller (Mecanim) + Layer-based Class State

- **Description**: Animator の Layer / SubStateMachine で各職業の State Graph を持ち、Layer Weight や Bool で職業を表現
- **Pros**:
  - Animator のビジュアルツールで遷移を直感的に管理
  - Mecanim は Unity 中核機能、API 安定性高
- **Cons**:
  - **`Animator.Play()` / `SetBool()` は次フレーム反映** — Pillar 1「即時切替」を物理的に破綻させる
  - 4 職目追加は State Graph 編集が必要、データ駆動性低
  - Class 固有ロジック（color wash 発火・SE）を Animation Event で仕込むと状態数に応じて煩雑化
- **Estimated Effort**: ×1.5（State Graph メンテ込み）
- **Rejection Reason**: **1 フレーム視覚遅延が Pillar 1 を破綻させる**ため不採用（technical-preferences.md Forbidden Patterns 違反）

### Alternative 3: Component-Swap Pattern

- **Description**: 職業ごとに `SwordsmanController : MonoBehaviour` 等を作成し、切替時に enable/disable
- **Pros**:
  - 各職業のロジックが独立 class にあり初期実装が直感的
- **Cons**:
  - 職業追加 = 新 class 作成 = コード重複が線形以上に増加
  - **Class Abilities が God Object 化（CD A1 違反）**：各 Controller が ability + combat + movement + VFX trigger を抱えがち
  - 共通インターフェース（ICharacterMotor 等）の徹底が組織的に困難
  - 切替時の状態同期（HP / 位置 / velocity）が散逸
- **Estimated Effort**: ×2（4 職分の重複実装 + 統合コスト）
- **Rejection Reason**: God Object 化リスク（CD A1 違反）と拡張コストが採用案の倍以上

## Consequences

### Positive

- Pillar 1「切替が花になる」が 1 ボタン即時 + 1 フレーム視覚同期で物理的に成立
- 4 職目追加が ScriptableObject Asset 1 個 + Inspector 配列追加で済む（コード変更ゼロ）
- ClassDefinition / ClassAbilityData / AbilityExecutor / AbilityContext の責任分離が God Object を構造的に防止
- ClassStateMachine が minimal feedback を内包し、Tier 0 で VFX/Audio System 不在でも Pillar 1 検証可能
- `UnityEvent<ClassDefinition>` インスペクタフックにより、Designer が後付けで VFX/SE 等を追加可能

### Negative

- ClassDefinition と ClassAbilityData の 2 段 ScriptableObject 階層は学習コストあり
- **Tier 0 minimal feedback の Tier 1 リファクタコスト（半日見積）が確定的に発生する**：これは「不可視の税」ではなく「合意済み Tier 0 負債」として明示受容（TD-ADR §2）
- SpriteLibraryAsset 個別 asset が職業数分必要（asset 管理コスト）
- AudioClip 二重持ち（`SwitchSEMVP` + `SwitchSERef`）はインスペクタの肥大化を招く（Tier 1 で `SwitchSEMVP` field 削除予定）

### Neutral

- ScriptableObject 選好により Editor 中心ワークフローに最適化（コード生成パイプラインへの依存度は下がるが、Asset 管理タスクが増える）

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **R1**: 2D Animation 10.x の Sprite Library API 名が想定と異なる | Medium | High | Tier 0 spike 開始時に Unity Editor で `SpriteLibrary` プロパティ名 / `SpriteResolver` メソッド名を実測。失敗時は擬似コードを修正し本 ADR Last Verified を更新 |
| **R2**: AudioSource.PlayOneShot 連打で voice stealing | Low | Low | Tier 0 では同一 AudioSource で許容（pillar に反する制約ではない）。Tier 1 で IAudioPublisher プール化により解決 |
| **R3**: 切替連打で VFX cue 過剰発火 | Low | Medium | 同一クラスへの再切替は no-op、別クラスは即時許可（Pillar 1 design test と矛盾しない範囲で voice stealing 防止） |
| **R4**: ClassDefinition[] の Inspector 順序が R1/L1 順序に暗黙バインド | Low | Medium | (a) ScriptableObject インスペクタ Header にコメント表示、(b) 配列順循環の単体テスト 1 本必須化 |
| **R5**: **SpriteSkin（Skeletal）+ SpriteLibraryAsset ランタイムスワップが Unityちゃん公式 PSB で動作するか UNVERIFIED** | Medium | **Critical** | **Validation Gate（後述）で実証必須**。失敗時は Decision 全体が無効化、Alternative 2/3 再評価 |
| **R6**: Tier 1 のオーブ取得フローで `IClassRoster` 抽象化が必要 | High | Medium | Tier 1 ADR（または本 ADR の Supersede）で `UnlockClass(ClassDefinition)` API を正式化する前提。本 ADR の `_availableClasses` 直配列は Tier 1 で Supersede される可能性が高い |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| **CPU (1 回の切替)** | N/A | 0.7-0.8 ms | 0.8 ms |
| **Memory (per class)** | N/A | ScriptableObject ~1 KB + SpriteLibraryAsset 1-2 MB | 2 MB / class |
| **Memory (3 class total)** | N/A | ~3-6 MB | 8 MB（Tier 3 で 4 職時） |
| **Load Time** | N/A | 0 ms（Awake 時にキャッシュ、ランタイム参照） | N/A |
| **Network** | N/A | 該当なし | N/A |

### CPU Breakdown（0.8 ms 内訳）

| 処理 | 想定コスト |
|---|---|
| `SpriteLibrary.spriteLibraryAsset` 設定（テクスチャ再バインド含む） | 0.3 ms |
| State mutation + `SpriteRenderer.color` assign + ColorWashCoroutine 起動 | 0.05 ms |
| `AudioSource.PlayOneShot` | 0.1 ms |
| `AbilityExecutor.Configure(abilities)`（abilities 配列 3-5 要素） | 0.2 ms |
| Event invoke（subscriber 数 ≤ 10 前提） | 0.05 ms |
| バッファ（GC alloc 等） | 0.1 ms |
| **合計** | **0.8 ms** |

### GC Allocation 注意点

- `event Action<ClassDefinition>` の delegate invocation は subscriber 数が多い場合に小さい alloc が発生する可能性。Tier 1 で `IClassChangedObserver` interface ベースに置換できるよう設計余地を残す
- `Coroutine` 起動は alloc を発生させる。Tier 1 で `Awaitable` (Unity 6.0+) または手動 timer に置換検討

### Profile タイミング

- 初回切替（cold path）: テクスチャバインドコスト最大、≤ 1.0 ms 許容
- 連続切替（warm path）: ≤ 0.5 ms 期待
- **Profiler GC.Alloc カラム + CPU Usage セクションで初回スイッチ時を計測必須**（Validation Criteria）

## Migration Plan

新規実装のため移行不要。ただし Tier 0 → Tier 1 のリファクタ計画を以下に明示：

1. **Tier 0 実装**: ClassStateMachine が `_audioSourceMinimal.PlayOneShot()` と `SpriteRenderer.color` を直叩き
2. **Tier 1 リファクタ Step 1**: `IVFXPublisher` 注入後、color wash を `IVFXPublisher.PlayCue("VFX.ClassSwitch.Wash", classDef.ColorWash)` へ置換。`SwitchSEMVP` field 利用継続
3. **Tier 1 リファクタ Step 2**: `IAudioPublisher` 注入後、`PlayOneShot` を `IAudioPublisher.PlayCue("Audio.Switch." + classDef.DisplayNameKey)` へ置換。`SwitchSEMVP` field 削除、`SwitchSERef` を Addressables 経由で活性化
4. **Tier 1 リファクタ Step 3**: `_audioSourceMinimal` MonoBehaviour 削除、Inspector からアサイン解除

**Rollback plan**: R5 検証失敗時、本 ADR を `Superseded by ADR-0001a` にし、Alternative 2（Mecanim ベース）の新 ADR を起こす。Mecanim ベースは 1 フレーム遅延を許容する代わりに「切替時のレスポンス遅延を anti-flicker 演出で隠蔽」する Decision を新たに必要とする。

## Validation Criteria

### MVP 達成条件（Tier 0 終了時）

- [ ] R1/L1 入力から `SpriteRenderer.sprite` 表示が **同一フレーム内に完了**（Unity Editor `Time.frameCount` ベースで確認）
- [ ] 切替コスト ≤ 0.8 ms（Unity Profiler 平均値、warm path）
- [ ] 初回切替コスト ≤ 1.0 ms（cold path）
- [ ] 4 職目追加が **コード変更ゼロ**（ScriptableObject Asset 追加 + Inspector 配列追加のみで動作）
- [ ] サイクル順序の単体テスト 1 本以上 pass
- [ ] Tier 0 prototype プレイテストで「切替の satisfaction」が color wash + SE + hitstop の合算結果として **5 名中 4 名が「もう一回切替えたい」と感じる**
- [ ] `AbilityExecutor.Configure(abilities)` インターフェースが ADR-0004 から正しく consume できる

### Tier 1 達成条件

- [ ] `IVFXPublisher` / `IAudioPublisher` 経由へ refactor 完了、minimal feedback コードを削除
- [ ] `UnlockClass(ClassDefinition)` 公開 API 実装、Orb Acquisition フローから呼び出し可能

## Validation Gate

> **本 ADR は本セクションを通過するまで `Accepted` に昇格しない。**

### R5 検証プロトタイプ（最優先タスク）

R5（SpriteSkin × SpriteLibraryAsset ランタイムスワップ）が偽の場合、Decision の根幹（1 フレーム視覚同期）が崩壊する。以下のプロトタイプで実機検証する：

**プロトタイプ範囲**:
- 単一シーン、最小限の Player GameObject
- Unityちゃん公式 PSB（オリジナルの Skeletal Sprite アセット）を 1 体配置
- SpriteSkin コンポーネント装着済み
- SpriteLibrary + SpriteResolver 構成
- 仮 SpriteLibraryAsset を 2 つ作成（剣士相当・弓士相当、Sprite カテゴリは同じ Idle / Run）
- R1 押下で SLA をスワップする最小コード（≈30 行）

**通過条件（すべて満たすこと）**:
- (a) Unityちゃん公式 PSB の SpriteSkin が `_spriteLibrary.spriteLibraryAsset = newSLA` 後に正しく描画される（骨格表示が崩れない）
- (b) `SpriteResolver.ResolveSpriteToRenderer()`（または Editor で確認した正しいメソッド名）が同フレーム内に SpriteRenderer.sprite を更新する
- (c) Profiler 計測で切替コスト ≤ 0.8 ms（warm path 平均）
- (d) `SpriteLibrary` のプロパティ名 / `SpriteResolver` のメソッド名 / 自動 resolve 有無を文書化し、本 ADR の API 仮表記を確定形に更新
- (e) `SpriteRenderer.color` 直接書込が URP 2D Renderer 環境で SRP Batcher と干渉せず描画される

**通過時のアクション**:
1. 本 ADR の `Status` を `Accepted` に変更
2. `Engine Compatibility` セクションの `Verification Required` を「✅ 検証済（YYYY-MM-DD）」と注記
3. `Decision` の擬似コード API 名仮表記を確定形に更新
4. `docs/registry/architecture.yaml` に本 ADR 由来の architectural stance を追記

**失敗時のアクション**:
1. 本 ADR を `Superseded by ADR-0001a` にマーク
2. Alternative 2 (Mecanim) ベースの新 ADR を起こす
3. `game-concept.md` Pillar 1 design test に「切替時 1 フレーム遅延を anti-flicker 演出で隠蔽するメカニクス」を追加検討

**担当・記録**:
- 実装担当: ユーザ（プロジェクトリード）または `unity-specialist` 経由のスパイクタスク
- 記録先: 本 ADR の `Engine Compatibility` セクション + `production/qa/evidence/` への計測結果保管

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `design/gdd/game-concept.md` | Class Switch | Pillar 1「切替が、花になる」R1/L1 1 ボタン即時切替 | 1 フレーム視覚同期を保証する SpriteLibrary 直接 resolve 経路を Decision で規定 |
| `design/gdd/game-concept.md` | Class Switch | Core Mechanic 1「切替自体が視覚/聴覚の報酬」 | ClassStateMachine が color wash + AudioSource.PlayOneShot を発火（CD1） |
| `design/gdd/game-concept.md` | Risks R-T3 | 職業切替 1 フレーム同期（Low risk） | SpriteLibrary 経由のスプライト直接差し替え + ScriptableObject ベースで実現 |
| `design/gdd/systems-index.md` | CD1 Tier 0 ミニマル feedback | Class Switch GDD 内で minimal feedback 自己内包 | ClassStateMachine が `_audioSourceMinimal` + `SpriteRenderer.color` を直叩き（合意済み Tier 0 負債） |
| `design/gdd/systems-index.md` | CD A1 Class Abilities God Object 防止 | ClassAbilityData (Data) + AbilityExecutor (Logic) + AbilityContext (DI) 三分割 | 本 ADR で枠組み確立、ADR-0004 で詳細化 |
| `design/gdd/systems-index.md` | Pillar 4「全部でもっと深く」4 職拡張 | コード変更ゼロで 4 職目追加可能 | ScriptableObject Asset 追加 + Inspector 配列拡張のみで対応（MVP）。Tier 1 で UnlockClass API 追加 |
| `.claude/docs/technical-preferences.md` | Forbidden | `Animator.Play()` で即時視覚切替を期待 | Alternative 2 を rejection、SpriteRenderer 直接代入経路を採用 |
| `.claude/docs/technical-preferences.md` | Forbidden | Magic numbers（ゲームバランス） | ClassDefinition の ScriptableObject 化で全パラメータがインスペクタ可視 |

## Related

- **`design/gdd/game-concept.md`** — Pillar 1, Core Mechanic 1, R-T3, Visual Identity Anchor Principle 3
- **`design/gdd/systems-index.md`** — Architecture Notes A1 / A4, Creative Director Notes CD1
- **`.claude/docs/technical-preferences.md`** — Forbidden Patterns, Engine Specialists 表（unity-specialist primary）
- **`docs/engine-reference/unity/VERSION.md`** — Unity 6.3 LTS pin、knowledge gap MEDIUM-HIGH
- **`docs/engine-reference/unity/breaking-changes.md`** — Box2D v3、URP RenderGraph、Input System 必須化
- **未来の関連 ADR**:
  - ADR-0002 (planned): CharacterController2D + ICharacterMotor — 本 ADR の AbilityContext で参照される `ICharacterMotor` の確定 ADR
  - ADR-0003 (planned): VFX System Boundary + IVFXPublisher — Tier 1 リファクタの target インターフェース
  - ADR-0004 (planned): Class Abilities System 詳細 — `ClassAbilityData` / `AbilityExecutor` / `AbilityContext` の細部
- **Followup Tasks**:
  - `[Spike]` R5 検証プロトタイプ（本 ADR Validation Gate 通過のため）
  - `[Test]` ClassStateMachine の配列順循環単体テスト
  - `[Sync]` 本 ADR Accepted 後、`technical-preferences.md` Architecture Decisions Log を更新
