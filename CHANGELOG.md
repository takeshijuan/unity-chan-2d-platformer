# Changelog

All notable changes to this project will be documented in this file.

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
