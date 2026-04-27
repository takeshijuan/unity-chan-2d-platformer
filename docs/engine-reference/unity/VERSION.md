# Unity Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | **Unity 6.3 LTS** (internal: `6000.3.x`) |
| **Release Date** | December 2025 |
| **Project Pinned** | 2026-04-23 |
| **Last Docs Verified** | 2026-04-23 |
| **LTS Support End** | 2027-12（Enterprise/Industry は +1年） |
| **LLM Knowledge Cutoff** | May 2025（setup-engine skill 基準） |
| **Actual Assistant Cutoff** | January 2026（Claude 4.X 系） |
| **Risk Level** | **MEDIUM-HIGH** — Unity 6.3 LTS は 2025-12 リリース、LLM 訓練データカバレッジ不完全 |
| **Project Focus** | 2D メトロイドヴァニア、URP 2D Renderer、2D Animation (Skeletal)、Box2D v3、MonoBehaviour ベース（DOTS 不使用） |

## Knowledge Gap Warning

The LLM's training data likely covers Unity up to ~2022 LTS (2022.3) and partially Unity 6.0. The Unity 6.1 / 6.2 / 6.3 series introduced significant changes that the model does NOT reliably know about. Always cross-reference this directory before suggesting Unity API calls.

**本プロジェクトで特に注意すべき領域**（2D メトロイドヴァニア向け）:

- **URP 2D Renderer** — Unity 6.3 で Unified Render Graph へ統合。旧 Compatibility Mode は削除
- **2D Animation / Sprite Library** — パッケージ 10.x 系、PSD Importer 経由で Unityちゃん公式素材を扱う場合の API
- **2D Physics (Box2D v3)** — Unity 6.3 でマルチスレッド化。決定論性向上
- **Input System** — 1.8+ の Action Rebinding UI（Steam Input API 連携）
- **Cinemachine 3** — `CinemachineCamera` 新 API、2D Confiner Extension 連携
- **Addressables 2.0** — 例外スロー変更など API 挙動改訂

LLM が提示する API が**古い記憶**からの場合、以下のファイルで検証：
- `deprecated-apis.md` — 非推奨化された API ではないか？
- `breaking-changes.md` — Unity 6.0 → 6.3 で破壊的変更があった領域ではないか？
- `current-best-practices.md` — 現行ベストプラクティスに沿っているか？

## Post-Cutoff Version Timeline

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 6.0 | Oct 2024 | HIGH | Unity 6 rebrand, new rendering features, Entities 1.3, DOTS improvements |
| 6.1 | Nov 2024 | MEDIUM | Bug fixes, stability improvements |
| 6.2 | Dec 2024 | MEDIUM | Performance optimizations, new input system improvements |
| 6.3 LTS | Dec 2025 | HIGH | First LTS since 6.0, production-ready DOTS, enhanced graphics features |

## Major Changes from 2022 LTS to Unity 6.3 LTS

### Breaking Changes
- **Entities/DOTS**: Major API overhaul in Entities 1.0+, complete redesign of ECS patterns
- **Input System**: Legacy Input Manager deprecated, new Input System is default
- **Rendering**: URP/HDRP significant upgrades, SRP Batcher improvements
- **Addressables**: Asset management workflow changes
- **Scripting**: C# 9 support, new API patterns

### New Features (Post-Cutoff)
- **DOTS**: Production-ready Entity Component System (Entities 1.3+)
- **Graphics**: Enhanced URP/HDRP pipelines, GPU Resident Drawer
- **Multiplayer**: Netcode for GameObjects improvements
- **UI Toolkit**: Production-ready for runtime UI (replaces UGUI for new projects)
- **Async Asset Loading**: Improved Addressables performance
- **Web**: WebGPU support

### Deprecated Systems
- **Legacy Input Manager**: Use new Input System package
- **Legacy Particle System**: Use Visual Effect Graph
- **UGUI**: Still supported, but UI Toolkit recommended for new projects
- **Old ECS (GameObjectEntity)**: Replaced by modern DOTS/Entities

## Verified Sources

- Official docs: https://docs.unity3d.com/6000.0/Documentation/Manual/index.html
- Unity 6 release: https://unity.com/releases/unity-6
- Unity 6.3 LTS announcement: https://unity.com/blog/unity-6-3-lts-is-now-available
- Migration guide: https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- Unity 6 support: https://unity.com/releases/unity-6/support
- C# API reference: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/index.html
