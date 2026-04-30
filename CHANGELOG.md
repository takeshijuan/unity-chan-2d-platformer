# Changelog

All notable changes to this project will be documented in this file.

## [0.0.3.0] - 2026-04-30

### Added

- **ADR-0006 Camera System を Accepted (Provisional) に昇格** — C0 validation gate (R1 spike) を Unity 6.3 LTS (6000.3.13f1) + Cinemachine 3.1.6 + URP 17.0.3 で完全通過、weighted score **18/18**（Critical 3 件 #8/#9/#11 すべて PASS）。ADR-0006 Status を `Provisional, C0-C1 pending` から `Accepted (Provisional, C0 PASS 2026-04-30 / C1 — pending ADR-0002 V1)` に変更。
- **ADR-0006a Camera System R1 Findings 起草** — empirical R1 spike findings を反映する follow-up ADR。Decision 1 (API 命名 10 件確定) / Decision 2 (CinemachinePixelPerfect 採用方針 — functional 確認、コード/プレハブ追加経路) / Decision 3 (UpdateMethod = SmartUpdate default lock) / Decision 4 (C1 protocol を 4 段階 A/B/C/D に拡張) / Decision 5 (D2 Look Ahead と Pillar 2 Design Test 接続)。CD-PLAYTEST gate CONCERNS A/B/C 全反映。
- **R1 Camera Cinemachine 3 Spike** — `prototypes/camera-cinemachine3-r1-spike/`（README + Scripts + REPORT、throw-away prototype）と `Assets/_R1Spike/`（Unity 配置: scene + Editor reflection checker + runtime MonoBehaviour）を新規追加。`R1CinemachineSpike.cs` は EditorWindow 経由で Cinemachine 3 reflection API checks 10 項目（Play Mode 不要）+ MonoBehaviour で runtime stutter / execution order 検証 2 項目（120 フレーム計測）を統合。Editor 検証完了、evidence は `production/qa/evidence/r1-camera-cinemachine3-spike-result.md` に保管。
- **Engine reference: Cinemachine 3 empirical findings** — `docs/engine-reference/unity/plugins/cinemachine.md` に **R1 Spike Findings (2026-04-30)** セクション追加。Cinemachine 2.x → 3.x mapping 表（10 API）、`GenerateImpulse` 7 overloads 一覧、`UpdateMethod` enum 値、`CinemachinePixelPerfect` 隠蔽運用、`PixelPerfectCamera` プロパティ全列挙。**`UseSignalSpaceOnly` → `UseCameraSpace` の意味的同等性は未検証** — 機械的 rename 禁止の caveat を明記。
- **Engine reference: Cinemachine 2.x deprecation table** — `docs/engine-reference/unity/deprecated-apis.md` に Cinemachine セクションを R1 確定形に更新。9 件の 2.x → 3.x mapping、CinemachinePixelPerfect 隠蔽運用注記、URP bundled `refResolutionX/Y` 命名差異。
- **Architecture registry append** — `docs/registry/architecture.yaml` に api_decisions 8 件（cinemachine_camera_class / brain_update_method / position_composer / confiner2d_bounding_shape / impulse_listener_use_camera_space / pixel_perfect_extension / pixel_perfect_camera_ref_resolution / brain_output_camera）+ forbidden_patterns 3 件（use_signal_space_only_property_reference / pixel_perfect_inspector_menu_expectation / brain_default_execution_order_attribute_assumption）追記。

### Changed

- **`technical-preferences.md` ADR Log** — ADR-0006 に `Accepted Provisional / C0 PASS 2026-04-30` 注記、ADR-0006a を新規エントリ追加。

## [0.0.2.0] - 2026-04-30

### Added

- **Unity 6.3 LTS プロジェクト本体初期化** — Universal 2D Cross-Platform 6.1.2 テンプレートを worktree ルートに展開（`Assets/`、`Packages/`、`ProjectSettings/` 22 ファイル + `ProjectVersion.txt`）。Asmdef 5 件（`Game.Core` / `Game.Gameplay` / `Game.Editor` / `Game.Tests.EditMode` / `Game.Tests.PlayMode`）を `autoReferenced: false` で配置し、ADR-0009 で言及済みの構造を確定。
- **Unity ローカル compile-check パイプライン** — `tools/ci/unity-compile-check.sh`（~95 行 bash、Personal license 対応、macOS-first）と `tools/ci/README.md` を新規追加。Path traversal 対策（version regex バリデーション）、`Library/Locks/Packages.lock` + `Temp/UnityLockfile` 検出、cross-platform `mktemp`（BSD/GNU 両対応）、BOM/CRLF strip、trap-based log cleanup、Unity Hub install URL 付き error message、CI 環境自動判定など safety hardening を組込み。
- **Quality-gate skill 統合** — `/gate-check` 4 phase gates（Tech Setup → Pre-Production / Pre-Production → Production / Production → Polish / Polish → Release）と `/story-done` Phase 3 に compile-check 実行ステップを追加。
- **Bats unit tests** — `tools/ci/tests/unity-compile-check.bats`（8 ケース、~91 行）。exit code 2/3/4/5 + BOM/CRLF stripping + path traversal rejection。`brew install bats-core && bats tools/ci/tests/unity-compile-check.bats` で実行。
- **Directory documentation** — `.claude/docs/directory-structure.md` を Unity レイアウト + `tools/ci/` 実態に更新。

### Changed

- **`Packages/manifest.json`** — Unity 6.3 LTS 互換性のためのパッケージ整理：
  - **削除**: `com.unity.collab-proxy@2.6.0`（Plastic SCM、Git 運用で不要、Unity 6.3 で `ObjectInfo` ambiguous reference エラー発生）
  - **削除**: `com.unity.visualscripting@1.9.5`（C# 運用、`technical-preferences.md` の Allowed Libraries 外）
  - **更新**: `com.unity.inputsystem 1.12.0 → 1.14.2`（1.12.0 が Unity 6.3 で削除された `BuildTarget.ReservedCFE` を参照していたため）

### Fixed

- **`tools/ci/unity-compile-check.sh` mktemp テンプレート** — `.XXXXXX.log` サフィックスが macOS BSD `mktemp` の「File exists」エラーを引き起こしていた（X 群がベース名末尾になければならない仕様）。`.log` サフィックスを削除（trap によるクリーンアップは継続）。
- **`tools/ci/unity-compile-check.sh` trap quoting** — `trap "rm -f '$COMPILE_LOG'"` から `trap 'rm -f "$COMPILE_LOG"' EXIT` に変更（pre-landing review 指摘、defensive 改善）。

### Documented (deferred to future iterations)

- `/release-checklist` / `/launch-checklist` / `/smoke-check` への compile-check 追加（Tier 1 PR1 着手時に再検討）
- CI YAML（`.github/workflows/`）/ git pre-commit hook（Pro license 取得 or Tier 1 完了後）
- Linux パス autodetect（Linux dev 参加時）
- Custom EditorScript with `EditorUtility.scriptCompilationFailed`（必要性が出てから）

### Reviews

- `/autoplan` Phase 1 CEO + Phase 3 Eng + Phase 3.5 DX subagent voices（Codex 認証 revoke のため subagent-only）。CEO subagent クリティカル指摘で当初 5 skill 更新計画を 2 skill に縮小。Eng/DX 13 findings 全 auto-fix。
- `/ship` Step 9 pre-landing review: 0 critical, 7 informational, 2 auto-fix 適用。
- `/ship` Step 11 adversarial review: Claude subagent 13 findings（informational class、ship-blocking なし、PR body に明記）。

## [0.0.1.0] - 2026-04-29

### Added

- **ADR-0007: Combo Input Buffer** — Ring Buffer + IComboBuffer + ScriptableObject Window。4-6f 先行入力バッファ、職業間受け渡し、Pause 保持の設計を確定。`IComboBuffer.TryConsume` の Latest-first セマンティクスと Validation Gate CB0-CB7 を定義。
- **ADR-0008: Class Abilities System** — ClassAbilityData ScriptableObject + AbilityContext + HitConfirmed event の設計を確定。IComboBuffer 注入、攻撃判定フロー、アビリティ実行チェーンの構造を定義。Validation Gate CA0-CA8。
- **ADR-0009: Combat System** — HitConfirmed → IDamageReceiver Thin Mediator パターン。CombatSystem player-local MonoBehaviour、`Rigidbody2D.linearVelocity` での knockback（Box2D v3 `AddForce+SetActive(false)` 競合回避）。Tier 0 → Tier 1 Migration Plan。Validation Gate CS0-CS5。
- **Architecture Review 2026-04-29** — ADR-0001〜ADR-0009 全 9 件の coverage チェック。GDDカバレッジ 74% → 測定完了。Foundation 100%、Core MVP 87%。

### Changed

- **architecture.yaml registry** — ADR-0007/0008/0009 の stances を 14 件追記・更新：`combo_buffer_state`、`combo_buffer_query`、`combo-input-buffer` budget、`combo_buffer_window_config`、`flush_combo_buffer_on_class_switch`、`direct_combo_buffer_array_access`、`class_ability_state`、`ability_hit_contract`（更新）、`motor_intent_command`（consumers に combat-system 追加）、`damage_receiver_contract`、`combat-system` budget、`hitstop_attacker_delivery`、`dummy_enemy_knockback_api`、`enemy_rigidbody_direct_write_tier1`、`hit_confirmed_non_combat_hitstop`。
- **tr-registry.yaml + traceability-index.md** — TR-CMB-001〜TR-CMB-008 (Combat System) のトレーサビリティ追記。ADR-0007/0008/0009 と GDD systems-index の対応付け完了。
- **ADR-0001 / ADR-0002** — referenced_by リストに ADR-0009 を追記。
