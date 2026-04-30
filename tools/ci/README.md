# tools/ci/

ローカル / CI 検証用スクリプト。

## unity-compile-check.sh

Unity 6.3 LTS のローカルコンパイルチェック（Personal license で動作）。

### Usage

```bash
bash tools/ci/unity-compile-check.sh           # 標準
bash tools/ci/unity-compile-check.sh --verbose # ログパス保存
```

### Requirements

- **macOS**: `/Applications/Unity/Hub/Editor/<version>/` に対象 Unity Editor がインストール済
- **Linux / 別パス**: `UNITY_EDITOR_PATH=/path/to/Unity` 環境変数で override
- **Windows**: 未対応（WSL + `UNITY_EDITOR_PATH` で動作可能性）

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | コンパイル成功 |
| 1 | コンパイルエラー検出 |
| 2 | Unity Editor binary 不在 |
| 3 | `ProjectSettings/ProjectVersion.txt` 不在 |
| 4 | Editor lock 検出（並行起動） |
| 5 | 不正な Unity バージョン形式 |
| 6 | Unity batch-mode timeout（既定 600 秒、`UNITY_COMPILE_CHECK_TIMEOUT` で override 可） |
| 7 | Unity log ファイル読み取り不可（silent failure 検出） |

### When to invoke

- `/gate-check` および `/story-done` skill 内で自動チェック対象
- 手動: 大きな変更後に `bash tools/ci/unity-compile-check.sh` を実行

### Performance

- Cold (no `Library/`): 60-120 秒
- Warm: 20-40 秒

CI 化は将来の検討事項（Pro license 取得後 game-ci/unity-test-runner v4 へ移行）。

### Tests

Bats unit tests for error paths (exit codes 2, 3, 4, 5):

```bash
brew install bats-core   # 初回のみ
bats tools/ci/tests/unity-compile-check.bats
```

10 ケース：missing ProjectVersion.txt, invalid version format, path traversal rejection, editor-not-found, lock detection (2 paths), BOM strip, CRLF strip, batch-mode timeout, log file unreadable (silent failure mitigation)。PASS path (exit 0) と compile-error path (exit 1) は実 Unity 起動が必要なため manual verification（ship cycle で実行）。

### Errors only / Warnings 許容

User 指示により `error CS*` パターンのみブロック対象。Warning は CI ログ表示なし（zero-warning policy は release-stage gate で別途検討）。
