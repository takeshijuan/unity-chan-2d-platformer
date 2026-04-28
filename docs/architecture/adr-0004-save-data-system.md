# ADR-0004: Save Data System (ISaveable + Section-based JSON + Atomic Write)

## Status

**Proposed (Validation Gate: S1-S5)**

> 本 ADR は「Validation Gate」セクション S1-S5 の検証を通過するまで Accepted に昇格しない。特に S2（atomic write のクラッシュ耐性）と S3（schemaVersion migration chain）が偽の場合、Decision の根幹（atomic write + .bak 1 世代 + migration chain）が崩壊し、Alternative 再評価となる。なお本 ADR は **Proposed 段階でも `docs/registry/architecture.yaml` への architectural stance を前倒し追記**する（Tier 0 MVP に最小スタブを前倒し配備するため、`ISaveable` interface contract と forbidden patterns を story 起草時点で参照可能にする必要がある）。

## Date

2026-04-27

## Last Verified

2026-04-27

## Decision Makers

- Project Lead（ユーザ）— 最終決定権
- `producer` 経由 PR-SCOPE — Save Data MVP 前倒し（最小スタブ）の整合性
- `technical-director` 経由 TD-ADR gate — Foundation 層 7 依存 bottleneck の責務分割整合性
- `unity-specialist` — Newtonsoft.Json for Unity / Application.persistentDataPath / Unity 6.3 .NET ランタイム挙動レビュー
- `creative-director` 経由 CD-SYSTEMS Note CD3 — Tier 1 Go/Pivot/Stop ゲートで save 互換失敗が致命であることの確認
- `narrative-director` — Save Point / Bench System (#20) と本 ADR の責務境界（save trigger UX）の確認

## Summary

本作 `職業オーブのレガシー` の Foundation 層において、Save Data System を **Newtonsoft.Json for Unity + `Application.persistentDataPath` + section-based JSON（player / world / settings / meta）+ `ISaveable` library-agnostic interface + atomic write (`.tmp` → rename + `.bak` 1 世代) + schemaVersion migration chain + `ICloudSync` 抽象** で確定する。Tier 0 MVP は最小スタブ（schemaVersion=1 + JSON I/O + NullCloudSync）として前倒し、Tier 2a Demo で `SteamCloudSync : ICloudSync` を別 asmdef で注入する。Save trigger は Save Point manual + zone transition auto + quit auto の hybrid（Hollow Knight 型ベースに safety net 追加）。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.x) |
| **Domain** | Core / Persistence（JSON Serialization, File I/O, Steam Cloud） |
| **Knowledge Risk** | **MEDIUM** — `engine-reference/unity/modules/` に core / scripting / persistence module 文書なし。`Application.persistentDataPath` と `System.IO` はランタイム横断の安定 API、`com.unity.nuget.newtonsoft-json` は技術選定済（technical-preferences line 82）。Steamworks.NET 統合は本 ADR では interface のみ確定し、実装は Tier 2a 時に別 ADR / 別 asmdef で追加 |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`、`docs/engine-reference/unity/breaking-changes.md`、`docs/engine-reference/unity/deprecated-apis.md`、`docs/engine-reference/unity/current-best-practices.md`、`.claude/docs/technical-preferences.md`（Forbidden Patterns: PlayerPrefs ban, Allowed Libraries: Newtonsoft.Json）、`design/gdd/game-concept.md`（R-T5、Technical Considerations Serialization）、`design/gdd/systems-index.md`（A5、#3 Save Data System、Production Constraint Option A）、`docs/architecture/tr-registry.yaml`（TR-save-001 / TR-arch-A5 / TR-prod-002） |
| **Post-Cutoff APIs Used** | `com.unity.nuget.newtonsoft-json`（Unity 6.3 同梱バージョン、`JsonConvert.SerializeObject` / `JsonConvert.DeserializeObject<T>` / `JObject` / `[JsonProperty]`）、`Application.persistentDataPath`（プラットフォーム横断）、`System.IO.File.Replace()`（atomic rename、Windows / macOS / Linux 全対応）、`UnityEngine.SystemInfo.deviceUniqueIdentifier`（save id 補助）、`Steamworks.NET ISteamRemoteStorage`（Tier 2a で別 asmdef 注入時のみ） |
| **Verification Required** | (1) `File.Replace(source, destination, backupFile)` が Windows NTFS / macOS APFS / Linux ext4 / Steam Deck SD カード（exFAT / ext4）すべてで atomic に動作するか — **exFAT は journaling 無しで非 atomic、`FileStream.Flush(flushToDisk: true)` 補完必須**、(2) Newtonsoft.Json for Unity の同梱バージョンが `JsonConvert.PopulateObject` をサポートするか（migration chain で in-place update に使用）、(3) Steam Deck で persistentDataPath への 100KB 級 write が 100ms 以内に完了するか、(4) **IL2CPP ビルドで Newtonsoft.Json reflection が動作するか（CI 上で IL2CPP build + S1 round-trip テストを毎 PR で gate 化）**、(5) Application.persistentDataPath 上の JSON ファイル read が `await File.ReadAllTextAsync` で main thread をブロックしないか（`.Result` / `.Wait()` 禁止、`ConfigureAwait(false)` 必須）、(6) **Cross-volume `File.Replace`（persistentDataPath と tmp が別ファイルシステム上）の挙動** — **すべて S1 / S2 / S5 / S6 検証プロトタイプで実測必須** |

> **Note**: Knowledge Risk が MEDIUM のため、Newtonsoft.Json package のメジャーバージョンが上がった場合や Unity 6.3 → 6.4 で `System.IO.File.Replace` 挙動が変わった場合、本 ADR を Superseded にし新 ADR を起こすこと。IL2CPP リンクストリッピングの問題が顕在化したら link.xml に `<assembly fullname="Newtonsoft.Json" preserve="all"/>` を追加。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None（Foundation 層 ADR、上流依存なし）— ただし読み取り対象として ADR-0001（`current_class` state）と ADR-0002（`motor_position` / `motor_facing`）の interface に従う |
| **Requires (revise requests)** | **ADR-0002 V1 revise: `ICharacterMotor.Teleport(Vector2 position, Facing facing)` API 追加** — load 時の motor 状態正常化（velocity reset / state=Airborne / 内部 cache 無効化 + `Physics2D.SyncTransforms()`）を Solver authority 内で行うため。本 ADR が `external_motor_state_write` forbidden を侵犯しない唯一の path。**ADR-0001 R5 revise: `SwitchContext { PlayerInput, SystemRestore, NarrativeForced }` enum 追加 + `ClassStateMachine.SwitchTo(ClassDefinition, SwitchContext)` overload** — load 時に Tier 0 inline color-wash が誤発火するのを防ぐ（Pillar 1「切替が、花になる」は player input への報酬であり、system restore で発火すべきではない）。**両 revise が ADR-0001 / ADR-0002 にコミットされない限り、本 ADR は Accepted 昇格不可** |
| **Enables** | ADR-0005 Game State Machine（save trigger / load 完了時の状態遷移を駆動）、`design/gdd/save-data-system.md` GDD authoring（最小スタブ MVP scope）、systems-index #17 Orb Acquisition / #18 Gate & Lock / #20 Save Point / Bench / #21 Map Tracking / #24 Title Menu UI / #27 Localization rebinding 永続化、Tier 2a で ADR-XXXX Steam Integration（`SteamCloudSync : ICloudSync` 注入）|
| **Blocks** | systems-index VS 段階の上記 6 systems すべて（Save Data の最小スタブ無しでは進行が永続化できない）、Tier 0 MVP playtest フィードバック収集（save 不能では複数セッション渡るプレイテスト不可） |
| **Ordering Note** | systems-index.md Recommended Design Order #2（Foundation 層、CharacterController2D 直前）。Production Constraint Option A により MVP に**前倒し**確定（最小スタブ = schemaVersion=1 + JSON I/O のみ）。本 ADR Accepted 後、ADR-0005 Game State Machine が Loading 状態の所有権を確定し、save trigger orchestration の責務が ADR-0005 完全所管となる（本 ADR は **passive service** として `SaveAll` / `LoadAll` API + 完了 event のみ提供） |

## Context

### Problem Statement

本作はメトロイドヴァニア（Hollow Knight 型）のため、プレイヤーはホットスポット間を移動し、**取得オーブ / 解放ゲート / 訪問エリア / Save Point 状態 / 設定（rebinding 含む）**を複数セッションに渡って永続化する必要がある。systems-index.md 分析で Save Data System は **7 依存の最大 bottleneck** となり、進行系（Orb Acquisition / Gate & Lock / Map Tracking）/ 設定系（Localization / Title UI）/ Meta 系（Steam Integration）すべてが Save Data に依存する。

加えて、game-concept R-T5 は schemaVersion + migration chain + .bak 1 世代を明示要求し、systems-index PR-SCOPE Note は MVP に **最小スタブを前倒し配備**することを Option A で確定した。technical-preferences.md は **PlayerPrefs を save data に使用禁止**（Steam Cloud 同期外、Windows Registry 依存で破損しやすい）と forbidden 化し、Newtonsoft.Json + persistentDataPath + Steam Remote Storage を推奨経路として規定済み。

しかし、interface 詳細（ISaveable の shape、SaveSection の型、ICloudSync の境界）/ Save trigger UX（manual のみ / hybrid / continuous）/ atomic write 戦略（tmp / .bak / 多世代）/ Steam Cloud 統合タイミング（Tier 0 / Tier 2a）/ section 分割粒度 / migration chain の実装パターンが未定義。`docs/registry/architecture.yaml` に Save Data 関連 stance は未登録のため、依存システム（systems-index #17/#18/#20/#21）の story authoring がブロックされる。

### Current State

- `.claude/docs/technical-preferences.md` で PlayerPrefs ban / Newtonsoft.Json 採用 / Steam Cloud 経路は確定済み（line 65, 82, 99）
- `design/gdd/systems-index.md` A5 で「ISaveable + section-based JSON（player / world / settings / meta）+ System split しない」が確定（line 119-123）
- `design/gdd/game-concept.md` R-T5 で schemaVersion + .bak 1 世代 + migration chain が要求（line 335）
- `docs/architecture/tr-registry.yaml` v2 で TR-save-001 / TR-arch-A5 / TR-prod-002 の 3 件が **Foundation Blocking** として Save Data に集約
- ADR-0001 / 0002 / 0003 はすでに Save Data から read される state を interface 経由で公開済み（current_class / motor_position / motor_facing）
- registry に `vfx-system` 等の確定 system 名と `audio-system-future` 等の placeholder 名が併存。本 ADR で Save Data の system 名を `save-data-system` で確定する

### Constraints

- **Engine**: Unity 6.3 LTS / .NET Standard 2.1 ランタイム / IL2CPP ビルド対応必須（reflection ストリッピング注意）
- **Library**: `com.unity.nuget.newtonsoft-json` のみ採用、Odin Serializer / MemoryPack / MessagePack 不採用（technical-preferences 規定）
- **API ban**: `PlayerPrefs.SetXxx / GetXxx` for save data 禁止（settings の volume 等 trivial state も含む）、`BinaryFormatter` 禁止（.NET 8 deprecated + security risk）
- **Determinism**: Save Data は gameplay state を mutate せず、`ISaveable.Capture()` 時に各 owner の read-only API から snapshot を取る（ADR-0001 / 0002 / 0003 の write authority 排他性を維持）
- **Performance (Save I/O)**: off-frame 実行（pause UI / loading screen / save point interaction で許容）、frame budget の 16.6ms には影響しないこと
- **File size**: 1 save file あたり ≤ 1 MB（Steam Cloud quota 余裕、典型 50-200 KB）
- **Atomic write**: 書込中クラッシュで save corruption を起こさない（POSIX rename / NTFS MoveFileEx + .bak 1 世代）
- **Steam Cloud**: Tier 2a Demo まで実 Steamworks.NET 統合は延期、ただし interface 抽象は Tier 0 から導入（Save layer のリファクタを避ける）
- **Save Trigger**: Save Point manual（Hollow Knight 型）+ zone transition auto + quit auto の hybrid（クラッシュ耐性）
- **Localization**: settings section に rebinding 永続化、Strings.[Category].[Key] キー参照のみ（生文字列禁止、TR-prod-001）

### Requirements

- **R1**: Foundation/Core 層に library-agnostic な `ISaveable` interface を `Game.Core.asmdef` で提供（Newtonsoft.Json への直接結合を避ける）
- **R2**: 4 section（player / world / settings / meta）を 1 ファイル `save_{slot}.json` に集約、System split しない（A5）
- **R3**: schemaVersion 付き JSON、migration chain で v1 → vN 段階変換、各 step は `ISchemaMigrator` 実装
- **R4**: atomic write: `.tmp` ファイルへ書込 → `File.Replace()` で `.bak` 退避 + 本ファイル置換、クラッシュ時は `.bak` から復元
- **R5**: `ICloudSync` interface（`Game.Core.asmdef`）と `NullCloudSync`（Tier 0 default、no-op）を Tier 0 から導入、Tier 2a で `SteamCloudSync` を `Game.SteamCloud.asmdef` で別 assembly として注入
- **R6**: Save trigger hybrid（Save Point manual + zone transition auto + quit auto）、save in-progress 中の重複呼出を mutex で防止
- **R7**: Save Data は ADR-0001/0002/0003 の forbidden patterns を維持（external_motor_state_write / direct_cross_system_state_write / animator_play_for_class_switch）
- **R8**: Restoration 時、motor 状態は **Spawn Point 経由で正常化**（Save Data は `motor_position` を直接 write しない）、class 状態は `ClassStateMachine.SwitchTo` 経由で復元
- **R9**: PlayerPrefs / BinaryFormatter / JsonUtility（save data 用途）の使用を Roslyn analyzer で build error 化
- **R10**: Settings section の rebinding（Input Actions JSON）、volume、language、accessibility（text size / colorblind mode）を含む
- **R11**: Meta section に `schemaVersion` / `lastSaveTimestamp` (UTC ISO 8601) / `totalPlaytime` (秒) / `saveSlotId` / `gameVersion` (semver) / `engineVersion` (Unity LTS string) を含む

## Decision

Save Data System を以下の構造で確定する：

1. **`ISaveable` interface** を `Game.Core.asmdef` に配置（Newtonsoft.Json 非依存）
2. **`SaveSection` POCO**（`Dictionary<string, object>` + `int SchemaVersion` + `string SectionId`）を `Game.Core.asmdef` に配置
3. **`ICloudSync` interface + `NullCloudSync` 実装**（Tier 0 default）を `Game.Core.asmdef` に配置
4. **`ISchemaMigrator` interface**（`int FromVersion / int ToVersion / SaveDocument Migrate(SaveDocument)`）を `Game.Core.asmdef` に配置
5. **`SaveDataService` MonoBehaviour**（autoload ではなく Scene root に install、`DontDestroyOnLoad`）を `Game.Persistence.asmdef` に配置。`ISaveable` 登録 / `SaveAll()` / `LoadAll()` / `MigrationChain` 実行 / atomic file write を担当
6. **`Game.SteamCloud.asmdef` 別 assembly**（Tier 2a で追加、Game.Core 参照、Game.Persistence は参照しない）に `SteamCloudSync : ICloudSync` を実装。MVP build には含まれない（asmdef Define Constraint で `STEAM_INTEGRATION` シンボルガード）
7. **Save trigger**: Save Data **passive service**（trigger orchestration ゼロ責任）。Save Point manual + zone transition auto + quit auto の 3 経路は `Game State Machine`（ADR-0005 future）が完全所管し、`SaveDataService.SaveAll(slot)` を呼ぶ。本 ADR が公開するのは `SaveAll/LoadAll` API + `OnSaveCompleted` / `OnLoadCompleted` event のみ
8. **Atomic write**: `(1) save_{slot}.json.tmp に SerializeObject(saveDocument)` → `(2) FileStream.Flush(flushToDisk: true)` で OS バッファフラッシュ（exFAT / SD カード対策）→ `(3) File.Replace(tmp, json, bak)` で本ファイル置換 + 旧版を `.bak` に退避
9. **Migration chain**: 起動時 `MigrationChain` が `ISchemaMigrator` 全実装を `FromVersion` 順に sort、save document の current version から target version まで sequential に Migrate を実行
10. **Restoration order**: meta → settings → world → player の順（player の current_class / spawn_point_id 復元には class system / world data が先行 load されている必要）。**Player restoration は `ICharacterMotor.Teleport(savedPos, savedFacing)`（ADR-0002 V1 revise 要請）+ `ClassStateMachine.SwitchTo(savedClass, SwitchContext.SystemRestore)`（ADR-0001 R5 revise 要請）経由で行い、Transform / SpriteRenderer の直接書込は禁止**
11. **ISaveable 登録**: `[RegisterSaveable]` attribute + reflection scan を **唯一の登録 path** とする。`SaveDataService.Awake` で AppDomain 全 assembly を scan し attribute 付き型を発見、自動 instantiate / register。明示的 `Register(ISaveable)` 呼出 / DI コンテナ注入は禁止（cyclic asmdef 防止 + B3 fix）

### Architecture

```
┌──────────────────────────────────────────────────────────────┐
│ Game.Core.asmdef (Foundation, no library deps)               │
│   ISaveable                  (this ADR)                      │
│   SaveSection (POCO)         (this ADR)                      │
│   SaveDocument (POCO)        (this ADR)                      │
│   ICloudSync                 (this ADR)                      │
│   NullCloudSync              (this ADR — Tier 0 default)     │
│   ISchemaMigrator            (this ADR)                      │
│   ICharacterMotor (read)     (ADR-0002)                      │
│   IVFXPublisher              (ADR-0003)                      │
└──────────────────────────────────────────────────────────────┘
              ▲                            ▲
              │ implements                  │ implements
┌──────────────────────────────┐   ┌──────────────────────────┐
│ Game.Persistence.asmdef      │   │ Game.Gameplay.asmdef     │
│   SaveDataService            │   │   PlayerSaveable         │
│   AtomicFileWriter           │   │     (current_class read  │
│   NewtonsoftJsonSerializer   │   │      via ClassStateMach) │
│   MigrationChain             │   │   WorldSaveable          │
│   SchemaMigrator_v1_to_v2    │   │   SettingsSaveable       │
│     (MVP では空、stub)       │   │     (rebinding / volume) │
│   SaveDocument DTO            │   │   MetaSaveable           │
└──────────────────────────────┘   │     (schemaVersion 等)   │
              │                    └──────────────────────────┘
              │ writes
              ▼
        ┌───────────────────────────────────────┐
        │ {Application.persistentDataPath}/     │
        │   save_{slot}.json     (current)      │
        │   save_{slot}.json.tmp (atomic stage) │
        │   save_{slot}.json.bak (1 generation) │
        └───────────────────────────────────────┘

[Tier 2a 以降 — 別 asmdef]
┌──────────────────────────────────────────┐
│ Game.SteamCloud.asmdef                   │
│   define constraint: STEAM_INTEGRATION   │
│   SteamCloudSync : ICloudSync            │
│   (Steamworks.NET ISteamRemoteStorage)   │
└──────────────────────────────────────────┘
              │ injected at startup (DI / SerializeField)
              ▼
        SaveDataService._cloudSync
```

データフロー（Save 例）:
```
[Trigger] Save Point interaction / Zone transition / Quit detected
    ↓
GameStateMachine.RequestSave(slot=0)         (ADR-0005 future)
    ↓
SaveDataService.SaveAll(slot=0)
    ├─ acquire SaveMutex (busy=true なら drop + log)
    ├─ for each ISaveable in registry:
    │     section = saveable.Capture()       (read-only API 経由)
    ├─ saveDocument = { schemaVersion: 1, sections: [...] }
    ├─ json = JsonConvert.SerializeObject(saveDocument, Indented)
    ├─ AtomicFileWriter.WriteAtomic(path, json)
    │     ├─ File.WriteAllTextAsync("{path}.tmp", json)
    │     ├─ verify: parse round-trip OK + size > 0
    │     ├─ File.Replace("{path}.tmp", path, "{path}.bak")
    │     └─ throw on any failure (release mutex without commit)
    ├─ ICloudSync.UploadAsync(File.ReadAllBytes(path)) [fire-and-forget]
    └─ release SaveMutex, fire OnSaveCompleted event
```

データフロー（Load 例）:
```
[Trigger] Title menu Continue button / slot select
    ↓
SaveDataService.LoadAll(slot=0)
    ├─ ICloudSync.DownloadAsync() [if available, compare timestamps]
    ├─ try parse "{path}":
    │     on parse failure → fallback to "{path}.bak"
    │     on .bak failure → throw SaveCorruptedException → UI shows error
    ├─ saveDocument = JsonConvert.DeserializeObject<SaveDocument>(json)
    ├─ if saveDocument.schemaVersion < CurrentSchemaVersion:
    │     saveDocument = MigrationChain.MigrateUpTo(saveDocument, CurrentSchemaVersion)
    ├─ for each section in [meta, settings, world, player]:  // order matters
    │     ISaveable subscriber = registry[section.SectionId]
    │     subscriber.Restore(section)
    │     // motor_position は WorldSaveable が spawn_point_id として復元、
    │     // PlayerSaveable は ClassStateMachine.SwitchTo() 経由で class 復元
    └─ fire OnLoadCompleted event
```

### Key Interfaces

```csharp
// Game.Core.asmdef
namespace Game.Core.Persistence
{
    /// <summary>
    /// Library-agnostic interface for systems that contribute state to save data.
    /// Implementations must NOT depend on Newtonsoft.Json — use SaveSection's
    /// Dictionary<string, object> as the wire format.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>Stable id like "player" / "world" / "settings" / "meta". Used as section key.</summary>
        string SectionId { get; }

        /// <summary>Current schema version this saveable produces.</summary>
        int SchemaVersion { get; }

        /// <summary>Capture current runtime state as a serializable POCO snapshot.</summary>
        SaveSection Capture();

        /// <summary>Restore runtime state from a snapshot. May be called during scene load.</summary>
        void Restore(SaveSection section);
    }

    /// <summary>
    /// POCO save section. Contains arbitrary key-value data plus version metadata.
    /// Newtonsoft.Json serializes this transparently via custom contract resolver.
    /// </summary>
    [System.Serializable]
    public sealed class SaveSection
    {
        public string SectionId { get; set; }
        public int SchemaVersion { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();

        public T Get<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }

        public void Set<T>(string key, T value) => Data[key] = value;
    }

    /// <summary>Top-level save document. One per slot. Serialized to a single JSON file.</summary>
    [System.Serializable]
    public sealed class SaveDocument
    {
        public int SchemaVersion { get; set; }   // file-level schema version
        public string GameVersion { get; set; }  // semver, e.g. "0.1.0-mvp"
        public string EngineVersion { get; set; } // "Unity 6.3.0f1"
        public string LastSavedAt { get; set; }   // ISO 8601 UTC
        public List<SaveSection> Sections { get; set; } = new();
    }

    /// <summary>Cloud sync abstraction. Tier 0 binds to NullCloudSync; Tier 2a binds to SteamCloudSync.</summary>
    public interface ICloudSync
    {
        bool IsAvailable { get; }
        Task UploadAsync(string slotKey, byte[] data, CancellationToken ct);
        Task<byte[]> DownloadAsync(string slotKey, CancellationToken ct);
        Task<DateTime?> GetRemoteTimestampAsync(string slotKey, CancellationToken ct);
    }

    /// <summary>Tier 0 default. No-op cloud sync (local-only).</summary>
    public sealed class NullCloudSync : ICloudSync
    {
        public bool IsAvailable => false;
        public Task UploadAsync(string _, byte[] __, CancellationToken ___) => Task.CompletedTask;
        public Task<byte[]> DownloadAsync(string _, CancellationToken __) => Task.FromResult<byte[]>(null);
        public Task<DateTime?> GetRemoteTimestampAsync(string _, CancellationToken __) => Task.FromResult<DateTime?>(null);
    }

    /// <summary>One step of schema migration. Implementations registered with MigrationChain.</summary>
    public interface ISchemaMigrator
    {
        int FromVersion { get; }
        int ToVersion { get; }
        SaveDocument Migrate(SaveDocument source);
    }

    /// <summary>Marker attribute for ISaveable auto-registration. SaveDataService.Awake scans
    /// AppDomain assemblies, instantiates and registers all attributed types. Sole registration
    /// path — explicit Register() / DI is forbidden (B3 fix from TD-ADR review).</summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RegisterSaveableAttribute : System.Attribute
    {
        public int LoadOrder { get; }
        public RegisterSaveableAttribute(int loadOrder = 0) { LoadOrder = loadOrder; }
    }

    /// <summary>Save / Load completion event payload. ADR-0005 (Game State Machine) and
    /// ADR-0006 (Input System) subscribe to these events for UI feedback / rebinding confirm.</summary>
    public sealed class SaveLifecycleEventArgs
    {
        public int Slot { get; }
        public bool Success { get; }
        public System.Exception Error { get; }
        public System.TimeSpan Duration { get; }

        public SaveLifecycleEventArgs(int slot, bool success, System.Exception error, System.TimeSpan duration)
        { Slot = slot; Success = success; Error = error; Duration = duration; }
    }

    /// <summary>SaveDataService public surface. Trigger orchestration is NOT here — it lives
    /// in ADR-0005 Game State Machine. SaveDataService is a passive service.</summary>
    public interface ISaveDataService
    {
        Task<SaveLifecycleEventArgs> SaveAllAsync(int slot, CancellationToken ct);
        Task<SaveLifecycleEventArgs> LoadAllAsync(int slot, CancellationToken ct);

        /// <summary>Fired after SaveAll completes (success or failure). Subscribers use for
        /// UI feedback (toast / icon flash). MUST not call SaveAll/LoadAll from handler.</summary>
        event System.Action<SaveLifecycleEventArgs> SaveCompleted;
        event System.Action<SaveLifecycleEventArgs> LoadCompleted;
    }
}
```

### Implementation Guidelines

1. **`Game.Persistence.asmdef` 新規作成**: `Game.Core.asmdef` + `com.unity.nuget.newtonsoft-json` に依存。`Game.Gameplay.asmdef` には依存しない（依存方向は publisher → core のみ）
2. **`SaveDataService` install**: Scene root に GameObject 1 つ、`DontDestroyOnLoad`、`[DefaultExecutionOrder(-100)]` で全 MonoBehaviour より先に Awake 実行。Awake で AppDomain assembly を scan し `[RegisterSaveable]` 付き型を発見・自動 instantiate / register、ICloudSync 起動、既存 save の事前検出
3. **ISaveable 登録**: `[RegisterSaveable]` attribute + reflection scan を **唯一の path**（B3 fix）。`SaveDataService.Awake` で `AppDomain.CurrentDomain.GetAssemblies()` を回し、`Type.GetCustomAttribute<RegisterSaveableAttribute>()` を持つ型を `Activator.CreateInstance` で生成・登録。**明示的 `Register(ISaveable)` 呼出 / DI コンテナ経由は禁止**（cyclic asmdef 防止）。LoadOrder で section restoration 順序を制御
4. **SaveSection.Data 値型制約**: **primitive / string / bool / `List<T>` / `Dictionary<string, T>` / `[Serializable]` POCO のみ許可**。Vector2 / Vector3 / Color / Quaternion 等 Unity 型を `object` として直接格納するとデシリアライズ後 `JObject` / `Int64` に化け、`Get<Vector2>("position")` が silent default を返す事故源。**Vector2 は `float[]` または `(float x, float y)` POCO に展開して格納する**。spawn point は `string saveSpotId` で参照（実 Vector2 は World 側が解決）
5. **Atomic write 実装**（exFAT / SD カード対策込み）:
   ```csharp
   public sealed class AtomicFileWriter
   {
       public async Task WriteAtomicAsync(string path, string content, CancellationToken ct)
       {
           string tmp = path + ".tmp";
           string bak = path + ".bak";
           // exFAT / SD カードで電源断耐性を担保するため Flush(flushToDisk: true) を明示
           using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
           using (var writer = new StreamWriter(fs, new UTF8Encoding(false)))
           {
               await writer.WriteAsync(content).ConfigureAwait(false);
               await writer.FlushAsync().ConfigureAwait(false);
               fs.Flush(flushToDisk: true); // ★ exFAT 対策（HIGH-fsync finding）
           }
           // verify round-trip
           string verify = await File.ReadAllTextAsync(tmp, ct).ConfigureAwait(false);
           if (verify != content) throw new IOException("Verify failed after tmp write.");
           if (File.Exists(path))
               File.Replace(tmp, path, bak); // atomic + .bak rotation
           else
               File.Move(tmp, path); // first write, no .bak yet
       }
   }
   ```
6. **Load with .bak fallback**:
   ```csharp
   public async Task<SaveDocument> LoadAsync(int slot, CancellationToken ct)
   {
       string path = SlotPath(slot);
       string bak = path + ".bak";
       try { return Deserialize(await File.ReadAllTextAsync(path, ct).ConfigureAwait(false)); }
       catch (Exception primaryFail)
       {
           if (!File.Exists(bak)) throw new SaveCorruptedException(primaryFail);
           Debug.LogWarning($"[Save] Primary corrupted, falling back to .bak: {primaryFail.Message}");
           try { return Deserialize(await File.ReadAllTextAsync(bak, ct).ConfigureAwait(false)); }
           catch (Exception bakFail)
           {
               throw new SaveCorruptedException(new AggregateException(primaryFail, bakFail));
           }
       }
   }
   ```
7. **Async 規律**: `SaveAll/LoadAll` は `async Task<...>` で返す。**caller は必ず `await` する** — `.Result` / `.Wait()` は Unity SynchronizationContext (single-threaded) で deadlock するため禁止。I/O 層では `ConfigureAwait(false)` を全 await に付与
8. **Migration chain 実装**: `List<ISchemaMigrator>` を `FromVersion` 昇順で sort、現バージョンから target まで `while (current < target)` で逐次適用。MVP 時点では schemaVersion=1 のみ、ダミー `SchemaMigrator_v1_to_v2` を用意して S3 検証。**新 `ISchemaMigrator` 実装を追加する PR は必ず `tests/fixtures/save/v(N-1).json` 凍結ファイルを追加し、integration test で v(N-1) → vN round-trip をアサート**（process control、TD finding H-MigRisk）
9. **Save mutex**: `SemaphoreSlim(1, 1)` で同時 1 save、`TryAcquire(timeout=0)` で busy 時は drop（次の trigger を待つ）。Save と Load の重複も同 mutex で block
10. **IL2CPP 対応**: `Assets/link.xml` に以下を必ず含める（HIGH-Linkxml finding）:
    ```xml
    <linker>
      <assembly fullname="Newtonsoft.Json" preserve="all"/>
      <assembly fullname="Game.Core">
        <type fullname="Game.Core.Persistence.SaveDocument" preserve="all"/>
        <type fullname="Game.Core.Persistence.SaveSection" preserve="all"/>
        <type fullname="Game.Core.Persistence.SaveLifecycleEventArgs" preserve="all"/>
      </assembly>
      <assembly fullname="Game.Persistence" preserve="all"/>
      <assembly fullname="Game.Gameplay" preserve="all"/>
    </linker>
    ```
    MVP は assembly 単位 `preserve="all"` で安全側、Tier 1 でプロファイル絞込み。`[Preserve]` attribute も SaveSection 関連型に併用
11. **Restoration の motor 制約**（B1 fix）: `WorldSaveable.Restore(section)` は `last_save_point_id` を読み、`SpawnPointService.SpawnPlayerAt(savePointId)` を呼ぶ。`SpawnPointService` は **`ICharacterMotor.Teleport(Vector2 position, Facing facing)`**（ADR-0002 V1 revise で追加要請、Solver authority 内で position / velocity reset / state=Airborne / 内部 cache 無効化 + `Physics2D.SyncTransforms()` を実施）を呼ぶ。**Transform 直接書込みは禁止**、Roslyn exception list は不要
12. **Class restoration の制約**（B2 fix）: `PlayerSaveable.Restore(section)` は `current_class_id` を読み、`ClassStateMachine.SwitchTo(classDef, SwitchContext.SystemRestore)` を呼ぶ（ADR-0001 R5 revise で `SwitchContext` enum + overload を追加要請）。SystemRestore 時は inline color-wash + SE feedback を **抑制**（Pillar 1 の player input 報酬を保つ）
13. **SettingsSaveable rebinding 形式**（M4 fix）: `SettingsSaveable.Data["rebinding"]` は **`InputActionAsset.SaveBindingOverridesAsJson()`** が返す string verbatim を保存。Restore 時は `InputActionAsset.LoadBindingOverridesFromJson(json)` で復元。**形式は Input System 1.8+ が所有、Save Data は opaque blob として扱う**（ADR-0006 Input System が schema を規定）
14. **Save trigger orchestration**: 本 ADR は **passive service**。Game State Machine (ADR-0005) が `OnZoneTransition` / `OnQuitRequested` / `OnSavePointInteract` イベントを subscribe し、`SaveDataService.SaveAll()` を呼ぶ。本 ADR は trigger source の知識ゼロ、API + 完了 event のみ提供
15. **`Get<T>` type mismatch 警告**（L2 fix）: `SaveSection.Get<T>(key, default)` で `Data[key]` が T と一致しなかった場合、`Debug.LogWarning($"[Save] Type mismatch for key '{key}' in section {SectionId}: expected {typeof(T).Name}, got {value?.GetType().Name ?? "null"}")` を返す前にログ。silent default は debug 困難なため
16. **Roslyn analyzer rule**: `PlayerPrefs.SetInt/SetFloat/SetString` の usage を gameplay assembly から block（settings UI の volume slider も SettingsSaveable 経由とする）。`BinaryFormatter` 全面禁止。`JsonUtility.ToJson/FromJson` を save data 用途で禁止（assembly attribute で識別）。**analyzer は PR1 で配備**（PR2 で書く Saveable 実装が gate される、TD finding H-PROrder）
17. **Scene-loaded precondition**（H-SceneLoad finding）: `LoadAll(slot)` は **gameplay scene が active で全 ISaveable が `[RegisterSaveable]` で発見・登録済み**である必要がある。Title menu Continue 経由は: scene load → wait OnLoadCompleted → `SaveDataService.LoadAll()` の順序を Game State Machine が orchestration する。**「scene loaded」前提に違反した呼出は `SaveSequenceException` を throw**

## Alternatives Considered

### Alternative 1: Unity JsonUtility のみ採用（Newtonsoft.Json 不採用）

- **Description**: Unity 標準 `JsonUtility.ToJson()` / `JsonUtility.FromJson<T>()` で全 serialization を行う
- **Pros**: 追加 package 不要、AOT / IL2CPP で高速、reflection なし、Unity 公式
- **Cons**: `Dictionary<,>` 直接サポートなし（List of KVP に展開必須）、polymorphism 不可（基底型での deserialize 不可）、`[SerializeReference]` でも circular ref 不可、ISaveable.SaveSection が `Dictionary<string, object>` を持てない
- **Estimated Effort**: 中（Dictionary 展開ヘルパー / type discriminator manual 実装）
- **Rejection Reason**: A5「section-based JSON」と R10「rebinding 永続化」（Input Actions JSON は polymorphic struct）が JsonUtility では成立しない。technical-preferences で Newtonsoft.Json が allowed library として規定済み

### Alternative 2: BinaryFormatter / Binary Serialization

- **Description**: `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` でバイナリ save
- **Pros**: 最高速、ファイルサイズ最小、reflection で全フィールドを自動 serialize
- **Cons**: **.NET 8 で deprecated（CA2300 SecurityException）**、デシリアライズ時に任意コード実行の **重大なセキュリティリスク**、デバッグ不能（バイナリは grep / diff 不可）、schema migration が手動オフセット計算
- **Rejection Reason**: セキュリティ + .NET ランタイム deprecation + デバッグ性で論外

### Alternative 3: MessagePack-Csharp / MemoryPack

- **Description**: 高性能バイナリ serializer、`[MessagePackObject]` attribute で型注釈
- **Pros**: JSON より 5-10x 高速、ファイルサイズ 1/3、IL2CPP 対応、polymorphism / Union 型対応
- **Cons**: バイナリゆえ debug / hand-edit 不能（modder community 対応で QA に有利な点を失う）、code generator 必須（IL2CPP では MemoryPack の source generator 動作が要検証、Unity 6.3 で未検証）、reflection モードは IL2CPP で動作不安定、library 依存追加
- **Estimated Effort**: 大（package 評価 + IL2CPP 検証 + migration tool 整備）
- **Rejection Reason**: typical save サイズ（50-200 KB）では JSON で性能十分、デバッグ性が QA / modding に有利、technical-preferences で未規定。将来 save サイズが 1MB 超え + 性能ボトルネックが顕在化したら再評価

### Alternative 4: Odin Serializer（Odin Inspector 同梱）

- **Description**: Odin Inspector の高機能 serializer（polymorphism / Dictionary / Vector 自動対応）
- **Pros**: Unity との親和性高、polymorphism 完全対応、interface field の serialize 可能
- **Cons**: **商用ライセンス必須**（個人 $55 / 商用 $80+）、Odin に強結合（serializer 単独抜き出し不可）、reflection 駆動で AOT / IL2CPP 注意必要
- **Rejection Reason**: technical-preferences で未規定、商用ライセンス追加コスト、Newtonsoft.Json で要件充足

### Alternative 5: PlayerPrefs（settings のみ）

- **Description**: 進行データは Newtonsoft.Json、settings（volume / language）のみ PlayerPrefs
- **Pros**: settings 系 trivial、Unity 標準
- **Cons**: **technical-preferences で forbidden**（Steam Cloud 同期外、Windows Registry 依存で破損しやすい）、rebinding（Input Actions JSON ~5KB）は PlayerPrefs string 化に向かない
- **Rejection Reason**: technical-preferences forbidden 違反、registry に forbidden_pattern として登録済（本 ADR でも継承）

### Alternative 6: Continuous / tick-based auto-save

- **Description**: 数秒ごとに自動保存、save point 不要
- **Pros**: クラッシュ時の進行ロスト最小、UI 不要
- **Cons**: JSON I/O 負荷の常時発生（pause UI 出さずに hitch リスク）、メトロイドヴァニアの「やり直し」デザイン（Bench に戻る = 一区切り）と不整合、Save Point / Bench System (#20) の存在意義消失
- **Rejection Reason**: Hollow Knight 型ジャンル設計と相反、playtest デザイン上の "区切り感" を損なう

### Alternative 7: Save Point manual のみ（Hollow Knight 原代型）

- **Description**: Bench / Save Point UI でしか save しない、auto-save なし
- **Pros**: 最もシンプル、UX クリア、原典準拠
- **Cons**: Save Point UI を MVP に必須化（VS プライオリティに反する）、デスクトップ シケジュアル（OS shutdown / power loss）で進行ロスト不可避、現代プレイヤー期待値とのズレ
- **Rejection Reason**: MVP scope 違反 + クラッシュ耐性ゼロ。zone transition / quit auto を併用する Hybrid が安全

### Alternative 8: 多世代ローテーション（.bak1 / .bak2 / .bak3）

- **Description**: 世代ごとに `.bak1` / `.bak2` / ... を保持、player は復旧 UI から世代選択
- **Pros**: 復旧選択肢広い、modder コミュニティに有利
- **Cons**: MVP scope 連伸、Steam Cloud quota 消費（最大 3-4MB / slot）、テストケース階乗増、復旧 UI を MVP に必要
- **Rejection Reason**: R-T5 は明確に「.bak 1 世代」、Steam Cloud quota / scope の両面で過剰

### Alternative 9: Tier 0 から実 Steamworks.NET 統合

- **Description**: MVP build から Steamworks.NET を依存に含め、Steam Deck Verified 検証を最早化
- **Pros**: Cloud quota / 実 Steam Deck で早期検証可能、Tier 2a で interface refactor 不要
- **Cons**: スタンドアロンビルド不可（CI ヘッドレス + Steam SDK 未起動環境でテスト不可）、MVP に Steamworks.NET 依存 = playtest 配布時に Steam ログイン要件、Steam パートナー登録の手続き加速必要
- **Rejection Reason**: MVP scope 鎮圧 + テストインフラ複雑化。`ICloudSync` 抽象 + `NullCloudSync` Tier 0 + `SteamCloudSync` Tier 2a 別 asmdef 注入で同等の最終品質を達成可能

### Alternative 10: ISaveable を Newtonsoft 直接結合（`JObject Save() / void Load(JObject)`）

- **Description**: `ISaveable.Save()` が `Newtonsoft.Json.Linq.JObject` を返す
- **Pros**: 代シリアライズコストゼロ、JObject API は強力
- **Cons**: `Game.Core.asmdef` が Newtonsoft.Json に依存 = Foundation 層が library 結合、将来の library swap（MemoryPack 移行など）で interface refactor 必須、unit test も Newtonsoft 必須
- **Rejection Reason**: Library-agnostic POCO（`SaveSection`）で同等表現可能、Foundation 層の純度を保つ

### Alternative 11: Reflection-based attribute serialization（`[Saveable]` attribute）

- **Description**: ISaveable interface 不要、各クラスに `[Saveable]` attribute を付け、framework が reflection で field を集約
- **Pros**: コードボイラープレート最小、デザイナーが新フィールド追加だけで自動 save
- **Cons**: フィールド名変更が backward compat 表現困難、IL2CPP でのリフレクション挙動注意、暗黙的 save により事故源（**意図せず save された field が migration を破壊**）、unit test の対象明確性失う
- **Rejection Reason**: 明示的 ISaveable contract が事故防止 + テスト容易性 + library-agnostic。reflection は migration chain 内部で限定的に利用

## Consequences

### Positive

- Save Data System が library-agnostic（ISaveable / SaveSection / ICloudSync が Foundation 層、Newtonsoft.Json は Game.Persistence 内に隔離）
- 4 section 分割により player / world / settings / meta が論理的に独立、A5 の「System split しない」を維持しつつ責務分離
- Atomic write + .bak 1 世代でクラッシュ耐性、R-T5 要件充足
- ICloudSync 抽象により Tier 2a の Steam Cloud 統合が **interface 変更ゼロ**で実現可能（Tier 0 から契約固定）
- Save trigger Hybrid により Hollow Knight 型 UX とクラッシュ耐性を両立
- Migration chain で schemaVersion アップグレード時のセーブ互換性保証、playtest 〜 Tier 2b EA まで再起不能事故を防ぐ
- registry の `vfx-system-future` / `audio-system-future` placeholder と並行する `save-data-system` 確定参照を提供、systems-index #17/#18/#20/#21 の story 起草解禁
- Roslyn analyzer rule で PlayerPrefs / BinaryFormatter / JsonUtility の混入を CI で防止、forbidden 違反を build error 化

### Negative

- IL2CPP で Newtonsoft.Json reflection ストリッピング対策（link.xml / [Preserve] attribute / S6 CI gate）の保守コスト
- Migration chain のテストケースが schema version 増加とともに階乗増（v1→v2、v2→v3 だけでなく v1→v3 経路も検証必要、ただし chain 設計上は逐次適用なので O(N) でテスト可能）+ 新 migrator PR 毎の fixture 凍結負担
- ICloudSync の Tier 0 / Tier 2a 二重実装（NullCloudSync stub）で early MVP の "実 Cloud sync 動作確認" は不可
- Newtonsoft.Json package 依存（`com.unity.nuget.newtonsoft-json`）追加、ビルドサイズ +約 800KB
- `SaveSection.Data` の `Dictionary<string, object>` は型安全性が string-typed access で弱い → `Get<T>(key, default)` helper + type mismatch warning ログで軽減するが完全な型安全には至らない。Vector2 等 Unity 型を直接格納できない（float[] / POCO 展開必須）
- **本 ADR は ADR-0001 R5 revise（`SwitchContext`）と ADR-0002 V1 revise（`Teleport` API）に依存**、両 revise が拒否された場合は fallback path（`IMotorTeleporter` separate interface / `SetClassWithoutFeedback` parallel API）を用意するが drift 源になりやすい
- `[RegisterSaveable]` reflection scan は IL2CPP で AOT 制約あり、`AppDomain.CurrentDomain.GetAssemblies()` の挙動を Steam Deck で検証必要

### Neutral

- Tier 0 MVP は schemaVersion=1 + 単一 ダミー SchemaMigrator_v1_to_v2（無動作 stub、S3 検証用）のみ。実 migration ロジックは VS / Tier 2a での schema 拡張時に追加
- Save UI（Save Point interaction / Title menu Continue / slot select）は本 ADR scope 外、UX spec で別途定義
- File path 構成（`save_{slot}.json`）は slot 数が確定するまで暫定、現状 slot 0 のみ（複数セーブスロット対応は VS / Tier 2a で再評価）

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| **R-A**: Migration chain bug が全 save data を破損 | **MEDIUM** | CRITICAL | (1) 必ず .bak 保持、(2) integration test で v1→v2→...→vN を逐次検証、(3) Migrate 失敗時は exception throw + .bak から自動復旧、(4) **新 `ISchemaMigrator` 実装を追加する PR は必ず `tests/fixtures/save/v(N-1).json` 凍結 fixture を添付し vN deserialization round-trip を assert**（process control）、(5) playtest フィードバックループに save corruption 報告チャネル設置。**first real migration ship 後に LOW へ再評価** |
| **R-B**: Newtonsoft.Json バージョン不整合（other Unity package との conflict） | MEDIUM | MEDIUM | `com.unity.nuget.newtonsoft-json` 公式 package を pin、CI で dependency tree チェック、別 package が直接 Newtonsoft.Json import している場合は `[assembly: AssemblyVersion("3.X")]` で明示 |
| **R-C**: Steam Cloud quota 超過（save サイズ 1MB 超え） | LOW | MEDIUM | save サイズ警告閾値 800KB を `SaveDataService` で監視、超過時は Debug.LogWarning + 開発者通知。VS / Tier 2a で実 Steam Cloud quota（100MB / 1000 files default）を検証 |
| **R-D**: Save 中 / Load 中の race condition | LOW | HIGH | `SemaphoreSlim(1, 1)` で同時 1 save / load mutex、busy 時は drop + log。Save と Load の重複実行は明示的に block |
| **R-E**: Settings の cloud sync で他マシン上書き（rebinding 競合） | MEDIUM | MEDIUM | timestamp 比較 UI、リモート > ローカル 5 秒以上ずれていたら user に確認ダイアログ（"クラウドの設定で上書きしますか？"）。MVP では timestamp 比較のみ実装、UI は VS で追加 |
| **R-F**: IL2CPP リンクストリッピングで Newtonsoft.Json 動作不能 | MEDIUM | HIGH | `Assets/link.xml` で Newtonsoft.Json + SaveSection 関連型を preserve（具体エントリ Implementation Guideline §10）、**S6 hard CI gate**: GitHub Actions `unity-builder@v4` IL2CPP build + S1 round-trip test を毎 PR で必須化（src/core/persistence/** または src/persistence/** 変更時） |
| **R-G**: Save in middle of class switch / combat で gameplay state が部分書込 | LOW | MEDIUM | `Game State Machine` (ADR-0005) が transition 中の save を block、SaveDataService は state machine の "Saveable" 状態でのみ save 受付 |
| **R-I**: `LoadAll()` 呼出前に gameplay scene が未 load の場合、`[RegisterSaveable]` 走査が空 / Saveable.Restore でリファレンス未解決 | MEDIUM | HIGH | `LoadAll()` は scene-loaded precondition を要求し違反時 `SaveSequenceException` throw。Game State Machine の orchestration: scene load → OnLoadCompleted → LoadAll の順序を ADR-0005 で確定（Implementation Guideline §17） |
| **R-J**: Cross-volume `File.Replace` 失敗（persistentDataPath と tmp が別ファイルシステム） | LOW | MEDIUM | `tmp` を `persistentDataPath + ".tmp"` に固定（同一ボリューム保証）、cross-volume 検出時は `File.Move` + `File.Delete` fallback。S2 で cross-volume シナリオも検証 |
| **R-K**: `SaveSection.Get<T>(key, default)` の type mismatch silent default で feel regression が見えない | MEDIUM | MEDIUM | type mismatch 時 `Debug.LogWarning` を必ず出力（Implementation Guideline §15）、CI で warning count を監視 |
| **R-L**: ADR-0002 V1 revise の `Teleport` API 追加が拒否された場合 | LOW | HIGH | Fallback: `IMotorTeleporter` separate interface を `Game.Core.asmdef` で定義し、CharacterController2D に追加実装。それでも拒否なら ADR-0004 を Superseded にし spawn 専用 motor 抽象を作る ADR を起こす。**ADR-0002 owner との事前合意必須** |
| **R-M**: ADR-0001 R5 revise の `SwitchContext` 追加が拒否された場合 | LOW | MEDIUM | Fallback: `ClassStateMachine.SetClassWithoutFeedback(ClassDefinition)` parallel API。ただし維持コスト高（drift 源）。**ADR-0001 owner との事前合意必須** |

## Performance Implications

| Metric | Tier 0 (MVP minimal stub) | Tier 1 (VS full) | Budget |
|--------|---------------------------|------------------|--------|
| Save I/O time (typical) | ~10-30ms (JSON ~50KB) | ~30-80ms (JSON ~200KB) | ≤ 100ms (off-frame) |
| Save I/O time (Steam Deck SD card) | ~30-80ms | ~80-150ms | ≤ 200ms (off-frame) |
| Memory (peak during save) | ~100 KB (string + JObject) | ~500 KB | ≤ 5 MB |
| Load I/O time (cold) | ~20-50ms | ~50-150ms | ≤ 300ms (during loading screen) |
| Migration chain (N steps) | N/A (only v1) | ~5ms / step | ≤ 50ms total |
| GC Alloc (per save) | ~10-50 KB (string + Dictionary) | ~50-200 KB | acceptable (off-frame) |
| Steam Cloud upload | N/A (NullCloudSync) | ~100ms (async, fire-and-forget) | non-blocking |

**Steam Deck 検証**: S5 で `≤ 100ms save I/O` を目標（SD カード環境では 200ms 上限）。Unity Profiler を Steam Deck 実機接続で測定。

## Migration Plan

```
[現在] ── ADR-0001 (R5 待ち) ── ADR-0002 (V1-V5 待ち) ── ADR-0003 (G1-G5 待ち)
            │                       │                       │
            └──── 並行 OK ─────────┴────── 並行 OK ────────┘
                       │
                       ▼
              ADR-0004 S1-S5 検証プロトタイプ
                       │
                       ▼
        Tier 0 MVP 実装（最小スタブ）
              ・Game.Persistence.asmdef 作成、Newtonsoft.Json 依存
              ・ISaveable / SaveSection / SaveDocument / ICloudSync / NullCloudSync 実装
              ・SaveDataService（SaveAll / LoadAll / Mutex / Migration stub）
              ・AtomicFileWriter（tmp + File.Replace + .bak）
              ・PlayerSaveable / WorldSaveable / SettingsSaveable / MetaSaveable（最小実装）
              ・schemaVersion=1、SchemaMigrator_v1_to_v2 ダミー実装
              ・Roslyn analyzer rule: PlayerPrefs / BinaryFormatter / JsonUtility ban
              ・S1-S5 検証
                       │
                       ▼
        Tier 1 VS 拡張
              ・各 Saveable の data field 拡充（Orb Acquisition / Gate & Lock / Map Tracking 連携）
              ・Save Point / Bench System (#20) UI 連携
              ・Title Menu Continue / slot select UI 連携
              ・Localization rebinding 永続化
              ・schemaVersion=2 への migration 実装（実機能、データ追加伴う）
                       │
                       ▼
        Tier 2a Demo
              ・Game.SteamCloud.asmdef 追加、Steamworks.NET 注入
              ・SteamCloudSync : ICloudSync 実装
              ・SaveDataService の bind を NullCloudSync → SteamCloudSync に切替
              ・Steam Deck Verified 申請準備
              ・Cloud quota 検証 + timestamp 比較 UI
```

**Step-by-step**:

1. **前提**: ADR-0001 R5 revise（`SwitchContext` enum + overload）+ ADR-0002 V1 revise（`ICharacterMotor.Teleport` API）が両方コミット済
2. ADR-0004 Accepted（S1-S6 通過）
3. **Tier 0 PR1（Foundation + Roslyn analyzer 配備）** ※ analyzer を PR1 に前倒し（TD finding H-PROrder）:
   - `Game.Persistence.asmdef` 作成、Newtonsoft.Json 依存設定
   - Foundation 型定義: `ISaveable` / `RegisterSaveableAttribute` / `SaveSection` / `SaveDocument` / `ICloudSync` / `NullCloudSync` / `ISchemaMigrator` / `SaveLifecycleEventArgs` / `ISaveDataService`
   - `SaveDataService` 最小実装（`[DefaultExecutionOrder(-100)]`、reflection scan による登録）+ `AtomicFileWriter`（exFAT fsync 込み）+ `MigrationChain` (空 chain)
   - `Assets/link.xml` 追加（Implementation Guideline §10 のエントリ）
   - **Roslyn analyzer 実装**: PlayerPrefs / BinaryFormatter / JsonUtility save data 利用を build error 化（PR2 で書く Saveable 実装が gate される）
   - Unit test: Save / Load round-trip、Crash safety（exFAT pull-plug 含む）、Migration chain (v1→v2 ダミー)、type mismatch warning ログ
4. **Tier 0 PR2（Saveable 実装 + Teleport 経由 spawn）**:
   - 各 ISaveable 実装（Player / World / Settings / Meta）に `[RegisterSaveable]` attribute、最小スタブ
   - `SpawnPointService.SpawnPlayerAt(savePointId)` が `_motor.Teleport(pos, facing)` を呼ぶ（ADR-0002 V1 Teleport API 使用）
   - `PlayerSaveable.Restore` が `SwitchTo(class, SwitchContext.SystemRestore)` を呼ぶ（ADR-0001 R5 SwitchContext 使用）
   - PlayMode test: Save Point に立つ → save → quit → relaunch → load → 同位置で復元（color-wash 誤発火なし、motor velocity reset 確認）
5. **Tier 0 PR3（CI gate + 5 anchor smoke test）**:
   - GitHub Actions IL2CPP build + S1 round-trip test を毎 PR で gate 化（S6）
   - 5 anchor save scenarios smoke test（new game / save / quit / load / settings change）
   - cold-miss / read-after-write smoke 自動化
6. ADR-0004 Status を `Accepted` に昇格、registry の forbidden_patterns / state_ownership / interfaces / api_decisions を確定参照化

**Rollback plan**:

- **S1（round-trip）失敗の場合**: serializer 選択を Alternative 3（MemoryPack）に切替検討、ADR Status を Superseded に
- **S2（atomic write）失敗の場合**: `File.Replace` の挙動が Windows / macOS / Linux で不一致 → 自前の rename + delete 実装に fallback、新 ADR で代替方式
- **Tier 1 拡張時に schema migration が破壊的変更を要求する場合**: schemaVersion を inkrement、ISchemaMigrator 実装、playtest フィードバックで段階デプロイ
- **Tier 2a で Steam Cloud quota 超過判明**: save 圧縮（gzip / brotli）導入、ADR-0004 revise（Newtonsoft.Json + gzip）

## Validation Criteria

Validation Gate S1-S6 全通過で `Proposed` → `Accepted` に昇格する。

- [ ] **S1 — Save / Load Round-trip**: 5 anchor 状態（current_class / motor_position via spawn_point_id / 取得オーブ list / 解放ゲート set / settings rebinding JSON）を save → quit → relaunch → load で完全復元。Editor PlayMode test + Unity Test Framework
- [ ] **S2 — Crash Safety**: save 中に process kill (`-9` / Task Manager 強制終了) → 次回起動時に `.bak` から復元可能。**exFAT (Steam Deck SD カード) 環境での電源断シナリオを含む**。**Cross-volume `File.Replace` 失敗時の `File.Move` fallback 動作も検証**。EditMode test で File.WriteAllTextAsync 中の例外シミュレーション + 実機 SD カード環境での pull-the-plug テスト
- [ ] **S3 — Migration Chain**: schemaVersion 1 → 2 ダミー migration を実装（version 番号のみ書換、data 不変）、v1 ファイルが v2 に変換 → v2 として load 可能。`tests/fixtures/save/v1.json` を凍結し v2 round-trip を assert（process control の harness 機能確認）。EditMode test
- [ ] **S4 — Forbidden Pattern Enforcement**: Roslyn analyzer rule で gameplay assembly 内の `PlayerPrefs.SetXxx` / `BinaryFormatter` / `JsonUtility.ToJson(saveTypes)` を CI build で error 化。CI test pipeline で禁止コード混入時に build fail を確認
- [ ] **S5 — Steam Deck Performance**: Steam Deck 実機（1080p、SD カード環境）で 100KB save の write が 100ms 以内、200KB save の write が 200ms 以内。`production/qa/evidence/adr-0004-s5-steamdeck-save.json` に Profiler キャプチャ保管
- [ ] **S6 — IL2CPP CI Round-trip Gate**（new、TD finding M1）: GitHub Actions `unity-builder@v4` で IL2CPP build を実行し、build artifact 上で S1 round-trip test を実行。`src/core/persistence/**` または `src/persistence/**` または `Assets/link.xml` 変更を含む PR で必須 gate（fail 時 merge block）。`production/qa/evidence/adr-0004-s6-il2cpp-roundtrip.log` に CI ログ保管

## GDD Requirements Addressed

> TR-IDs は `docs/architecture/tr-registry.yaml` v2 (main 規約: `TR-[system-slug]-[NNN]`) に準拠。`TR-save-001` (schemaVersion + migration chain) と `TR-save-002` (.bak 1 世代 backup) は本 ADR で詳細化されたが registry 上は未登録（Save Data GDD authoring 時に追加予定 — tr-registry.yaml lines 245-269 の future-entry コメント参照）。

| GDD Document | System | Requirement (TR-ID) | How This ADR Satisfies It |
|-------------|--------|---------------------|---------------------------|
| `design/gdd/game-concept.md` | R-T5 | schemaVersion + マイグレーションチェーン + .bak 1 世代バックアップ（TR-save-001 / TR-save-002 future） | `SaveDocument.SchemaVersion` + `MigrationChain` + `AtomicFileWriter` の `.tmp → fsync → File.Replace + .bak` で完全充足 |
| `design/gdd/game-concept.md` | Technical Considerations Serialization | Newtonsoft.Json for Unity + Steam Cloud (`ISteamRemoteStorage`)（TR-save-004） | `com.unity.nuget.newtonsoft-json` 採用、Tier 0 `NullCloudSync` + Tier 2a `SteamCloudSync : ICloudSync` 別 asmdef 注入で完全充足 |
| `design/gdd/systems-index.md` | A5 / TR-save-003 | Save Data System を ISaveable + section-based JSON（player/world/settings/meta）で系統内分割、System split しない | 本 ADR が `ISaveable` interface + `SaveDocument.Sections` 4-section リスト構造で確定 |
| `design/gdd/systems-index.md` | Production Constraint Option A (TR-save-005) | Save Data 最小スタブを MVP に前倒し（schemaVersion=1 + JSON I/O のみ、実体データは VS で拡張） | Tier 0 PR1-3 で最小スタブ配備、Tier 1 で Saveable data field 拡充の段階移行を Migration Plan で定義 |
| `design/gdd/systems-index.md` | systems-index #17 / #18 / #20 / #21 / #24 / #28 dependencies on Save Data | これらのシステムが Save Data に depend on | `ISaveable` interface 確定により各 system が `[RegisterSaveable]` 付き ISaveable 実装で section 登録、依存解禁 |
| `.claude/docs/technical-preferences.md` | Forbidden Pattern: PlayerPrefs for save data | Steam Cloud 同期外、Windows Registry 依存で破損しやすい | Roslyn analyzer rule（Tier 0 PR1）で gameplay assembly から PlayerPrefs save data 利用を build error 化、registry forbidden_patterns に登録 |
| `.claude/docs/technical-preferences.md` | Required Tests: セーブデータの schemaVersion マイグレーション | Low risk だが事故ると致命 | S3 Validation Gate + Tier 0 PR1 で migration chain integration test を必須化、新 ISchemaMigrator PR は fixture-based regression test 必須 |

## Related

- **Depends on (read-only)**: [ADR-0001 Class Switch Architecture](adr-0001-class-switch-architecture.md)（`ClassStateMachine.CurrentClass` を read、復元は `SwitchTo(class, SwitchContext.SystemRestore)` API 経由）
- **Depends on (read-only)**: [ADR-0002 CharacterController2D + ICharacterMotor](adr-0002-character-controller-motor.md)（`ICharacterMotor.Position` / `.Facing` を read、復元は `ICharacterMotor.Teleport(pos, facing)` 経由）
- **Requires revise**: ADR-0001 R5 revise — `SwitchContext { PlayerInput, SystemRestore, NarrativeForced }` enum + `SwitchTo(ClassDefinition, SwitchContext)` overload を追加。Tier 0 inline color-wash + SE feedback は SystemRestore で抑制（Pillar 1 player input 報酬保護）
- **Requires revise**: ADR-0002 V1 revise — `ICharacterMotor.Teleport(Vector2 position, Facing facing)` API を追加。motor 状態正常化（velocity reset / state=Airborne / 内部 cache 無効化 + `Physics2D.SyncTransforms()`）を Solver authority 内で実施
- **Coordinates with**: [ADR-0003 VFX System Boundary](adr-0003-vfx-system-boundary.md)（VFX 状態は save 対象外、replay 安全性維持 — registry forbidden に `vfx_state_in_save_document` を追加要請）
- **Enables**: ADR-0005 Game State Machine（save trigger orchestration / Loading 状態の所有 / Title Menu UX、`SaveLifecycleEventArgs` event を subscribe）、ADR-0006 Input System（rebinding を SettingsSaveable 経由で永続化、`SaveBindingOverridesAsJson()` verbatim 形式）
- **Enables (Tier 2a)**: ADR-XXXX Steam Integration（`SteamCloudSync : ICloudSync` 別 asmdef 実装、本 ADR の interface に bind）
- **Engine reference**: `docs/engine-reference/unity/current-best-practices.md`（PlayerPrefs 用法 line 165 — Save Data には不適）
- **Implementation files (post-Accepted)**: `src/core/persistence/ISaveable.cs`、`src/core/persistence/RegisterSaveableAttribute.cs`、`src/core/persistence/SaveSection.cs`、`src/core/persistence/SaveDocument.cs`、`src/core/persistence/SaveLifecycleEventArgs.cs`、`src/core/persistence/ISaveDataService.cs`、`src/core/persistence/ICloudSync.cs`、`src/core/persistence/NullCloudSync.cs`、`src/core/persistence/ISchemaMigrator.cs`、`src/persistence/SaveDataService.cs`、`src/persistence/AtomicFileWriter.cs`、`src/persistence/MigrationChain.cs`、`src/persistence/SpawnPointService.cs`（Teleport 経由）、`Assets/link.xml`、`tests/fixtures/save/v1.json`
