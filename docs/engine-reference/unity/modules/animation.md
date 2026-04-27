# Unity 6.3 — Animation Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 animation improvements, Timeline enhancements

---

## Overview

Unity 6.3 animation systems:
- **Animator Controller (Mecanim)**: State machine-based (RECOMMENDED)
- **Timeline**: Cinematic sequences, cutscenes
- **Animation Rigging**: Procedural runtime animation
- **Legacy Animation**: Deprecated, avoid

---

## Key Changes from 2022 LTS

### Animation Rigging Package (Production-Ready in Unity 6)

```csharp
// Install: Package Manager > Animation Rigging
// Runtime IK, aim constraints, procedural animation
```

### Timeline Improvements
- Better performance
- More track types
- Improved signal system

---

## Animator Controller (Mecanim)

### Basic Setup

```csharp
// Create: Assets > Create > Animator Controller
// Add to GameObject: Add Component > Animator
// Assign Controller: Animator > Controller = YourAnimatorController
```

### State Transitions

```csharp
Animator animator = GetComponent<Animator>();

// ✅ Trigger transition
animator.SetTrigger("Jump");

// ✅ Bool parameter
animator.SetBool("IsRunning", true);

// ✅ Float parameter (blend trees)
animator.SetFloat("Speed", currentSpeed);

// ✅ Integer parameter
animator.SetInteger("WeaponType", 2);
```

### Animation Layers
- **Base Layer**: Default animations (locomotion)
- **Override Layers**: Replace base layer (e.g., weapon swap)
- **Additive Layers**: Add on top of base (e.g., breathing, aim offset)

```csharp
// Set layer weight (0-1)
animator.SetLayerWeight(1, 0.5f); // 50% blend
```

---

## Blend Trees

### 1D Blend Tree (Speed blending)

```csharp
// Idle (Speed = 0) → Walk (Speed = 0.5) → Run (Speed = 1.0)
animator.SetFloat("Speed", moveSpeed);
```

### 2D Blend Tree (Directional movement)

```csharp
// X-axis: Strafe (-1 to 1)
// Y-axis: Forward/Back (-1 to 1)
animator.SetFloat("MoveX", input.x);
animator.SetFloat("MoveY", input.y);
```

---

## Animation Events

### Trigger Events from Animation Clips

```csharp
// Add in Animation window: Right-click timeline > Add Animation Event
// Must have matching method on GameObject:

public void OnFootstep() {
    // Play footstep sound
    AudioSource.PlayClipAtPoint(footstepClip, transform.position);
}

public void OnAttackHit() {
    // Deal damage
    DealDamageInFrontOfPlayer();
}
```

---

## Root Motion

### Character Movement via Animation

```csharp
Animator animator = GetComponent<Animator>();
animator.applyRootMotion = true; // Move character based on animation

void OnAnimatorMove() {
    // Custom root motion handling
    transform.position += animator.deltaPosition;
    transform.rotation *= animator.deltaRotation;
}
```

---

## Animation Rigging (Unity 6+)

### IK (Inverse Kinematics)

```csharp
// Install: Package Manager > Animation Rigging
// Add: Rig Builder component + Rig GameObject

// Two Bone IK (Arm/Leg)
// - Add Two Bone IK Constraint
// - Assign Tip (hand/foot), Mid (elbow/knee), Root (shoulder/hip)
// - Set Target (where hand/foot should reach)

// Runtime control:
TwoBoneIKConstraint ikConstraint = rig.GetComponentInChildren<TwoBoneIKConstraint>();
ikConstraint.data.target = targetTransform;
ikConstraint.weight = 1f; // 0-1 blend
```

### Aim Constraint (Look At)

```csharp
// Character looks at target
MultiAimConstraint aimConstraint = rig.GetComponentInChildren<MultiAimConstraint>();
aimConstraint.data.sourceObjects[0] = new WeightedTransform(targetTransform, 1f);
```

---

## Timeline (Cutscenes)

### Basic Timeline Setup

```csharp
// Create: Assets > Create > Timeline
// Add to GameObject: Add Component > Playable Director
// Assign Timeline: Playable Director > Playable = YourTimeline

// Play from script:
PlayableDirector director = GetComponent<PlayableDirector>();
director.Play();
```

### Timeline Tracks
- **Activation Track**: Enable/disable GameObjects
- **Animation Track**: Play animations on Animator
- **Audio Track**: Synchronized audio playback
- **Cinemachine Track**: Camera movement
- **Signal Track**: Trigger events at specific times

### Signal System (Events)

```csharp
// Create Signal Asset: Assets > Create > Signals > Signal
// Add Signal Emitter to Timeline track
// Add Signal Receiver component to GameObject

public class CutsceneEvents : MonoBehaviour {
    public void OnDialogueStart() {
        // Triggered by signal
    }
}
```

---

## Animation Playback Control

### Play Animation Directly (No State Machine)

```csharp
// ✅ CrossFade (smooth transition)
animator.CrossFade("Attack", 0.2f); // 0.2s transition

// ✅ Play (instant)
animator.Play("Idle");

// ❌ Avoid: Legacy Animation component
Animation anim = GetComponent<Animation>(); // DEPRECATED
```

---

## Animation Curves

### Custom Property Animation

```csharp
// In Animation window: Add Property > Custom Component > Your Script > Your Float

public class WeaponTrail : MonoBehaviour {
    public float trailIntensity; // Animated by clip

    void Update() {
        // Intensity controlled by animation curve
        trailRenderer.startWidth = trailIntensity;
    }
}
```

---

## Performance Optimization

### Culling
- `Animator > Culling Mode`:
  - **Always Animate**: Always update (expensive)
  - **Cull Update Transforms**: Stop updating bones when off-screen (RECOMMENDED)
  - **Cull Completely**: Stop all animation when off-screen

### LOD (Level of Detail)
- Simpler animations for distant characters
- Reduce skeleton bone count for LOD meshes

---

## Common Patterns

### Check if Animation Finished

```csharp
AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
if (stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 1.0f) {
    // Attack animation finished
}
```

### Override Animation Speed

```csharp
animator.speed = 1.5f; // 150% speed
```

### Get Current Animation Name

```csharp
AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
string currentClip = clipInfo[0].clip.name;
```

---

## Debugging

### Animator Window
- `Window > Animation > Animator`
- Visualize state machine, see active state

### Animation Window
- `Window > Animation > Animation`
- Edit animation clips, add events

---

## Sources
- https://docs.unity3d.com/6000.0/Documentation/Manual/AnimationOverview.html
- https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html

---

## 2D Animation 13.x（Skeletal）— Unity 6.3 LTS

> **Last verified:** 2026-04-27
> **Package:** `com.unity.2d.animation` **13.0.4**（Unity 6000.3.x 同梱）
> **Knowledge Risk:** MEDIUM — v13.0 は 2025-07 リリース。LLM 訓練データに含まれない可能性が高い。

---

### Overview

| 項目 | 内容 |
|---|---|
| パッケージ名 | `com.unity.2d.animation` |
| Unity 6.3 LTS 同梱バージョン | **13.0.4**（旧 10.x は Unity 2023.1 用、13.x と別物） |
| URP 2D との関係 | URP 2D Renderer 必須（GPU Deformation は URP 専用）。CPU Deformation は Built-in でも動作するが非推奨 |
| PSD/PSB Importer 連携 | `com.unity.2d.psdimporter` と組み合わせて `.psb` ファイルから Skeletal Prefab を生成。Unityちゃん公式素材の推奨インポート経路 |
| 名前空間 | `UnityEngine.U2D.Animation` |

---

### SpriteSkin（Skeletal の最小セットアップ）

`SpriteSkin` は `SpriteRenderer` と同一 GameObject に配置し、ボーン変形を担当するコンポーネント。

```csharp
// SpriteSkin の公開 API（com.unity.2d.animation 13.0.4）
// RequireComponent(typeof(SpriteRenderer)) — 自動アタッチ

public sealed class SpriteSkin : MonoBehaviour
{
    // ボーン自動探索（SpriteRenderer.sprite 変更時に発火）
    public bool autoRebind { get; set; }

    // 常時更新（カリング時も変形を継続するか）
    public bool alwaysUpdate { get; set; }

    // CPU 強制（GPU Deformation が有効でも CPU で処理）
    public bool forceCpuDeformation { get; set; }

    // ボーン Transform 配列（読み取り専用、要素変更不可）
    public Transform[] boneTransforms { get; }

    // ルートボーン Transform
    public Transform rootBone { get; }

    // ボーン Transform の設定（API 経由）
    public SpriteSkinState SetBoneTransforms(Transform[] boneTransformsArray);
    public SpriteSkinState SetRootBone(Transform rootBoneTransform);
}
```

**CPU vs GPU Deformation の選択指針**:

| モード | バッチング | 推奨ケース |
|---|---|---|
| CPU Deformation | Dynamic Batching | キャラクター多数・低ポリゴン |
| GPU Deformation（URP 専用） | SRP Batcher（1 draw call/object） | キャラクター少数・高ポリゴン |

**GPU Deformation の禁止事項**（公式明記）:
- `MaterialPropertyBlock` の使用 → SRP Batcher を破壊し CPU Skinning フォールバックが発生
- `Sprite Mask` の使用 → 同様に SRP Batcher 非互換

---

### SpriteLibraryAsset / SpriteLibrary / SpriteResolver の関係

```
SpriteLibraryAsset（.spriteLib / .asset）
  ├─ Category: "Body"
  │   ├─ Label: "Swordsman" → Sprite ref
  │   ├─ Label: "Archer"    → Sprite ref
  │   └─ Label: "Mage"      → Sprite ref
  └─ Category: "Head"
      ├─ Label: "Swordsman" → Sprite ref
      └─ ...

SpriteLibrary（MonoBehaviour — ルート GameObject に配置）
  └─ spriteLibraryAsset: SpriteLibraryAsset { get; set; }

SpriteResolver（MonoBehaviour — 各 SpriteRenderer GameObject に配置）
  ├─ Category: "Body"
  ├─ Label: "Swordsman"
  └─ spriteLibrary: SpriteLibrary { get; }  ← 読み取り専用、自動解決
```

**API 一覧（確定 / com.unity.2d.animation 13.0.4）**:

```csharp
// SpriteLibrary
public SpriteLibraryAsset spriteLibraryAsset { get; set; }
public Sprite GetSprite(string category, string label);
public void AddOverride(Sprite sprite, string category, string label);
public void AddOverride(SpriteLibraryAsset spriteLib, string category);
public void AddOverride(SpriteLibraryAsset spriteLib, string category, string label);
public void RemoveOverride(string category, string label);

// SpriteResolver
public string GetCategory();
public string GetLabel();
public bool SetCategoryAndLabel(string category, string label);
public bool ResolveSpriteToSpriteRenderer();  // bool: 成功可否を返す
public SpriteLibrary spriteLibrary { get; }   // 読み取り専用

// SpriteLibraryAsset
public IEnumerable<string> GetCategoryNames();
public IEnumerable<string> GetCategoryLabelNames(string category);
public Sprite GetSprite(string category, string label);
```

---

### ランタイムスワップ手順（Class Switch System 向け ≤30 行 C# 例）

```csharp
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// SpriteLibraryAsset ランタイムスワップの最小実装。
/// com.unity.2d.animation 13.0.4 / Unity 6.3 LTS で検証済み API を使用。
/// </summary>
public sealed class ClassVisualSwapper : MonoBehaviour
{
    [SerializeField] private SpriteLibrary _spriteLibrary;
    [SerializeField] private SpriteResolver _spriteResolver;  // オプション

    // SLA スワップのみで SpriteResolver は自動 resolve する（公式サンプル準拠）
    // ただし「同フレーム内完了」は要 Editor 実測
    public void SwapLibrary(SpriteLibraryAsset newAsset)
    {
        if (newAsset == null)
        {
            Debug.LogWarning("[ClassVisualSwapper] newAsset is null — swap skipped.");
            return;
        }

        // Step 1: SLA を差し替える
        // SpriteResolver は SpriteLibrary 変更を検知して自動 resolve
        _spriteLibrary.spriteLibraryAsset = newAsset;

        // Step 2: 同フレーム内に確実に resolve したい場合は明示呼び出し
        // bool success = _spriteResolver.ResolveSpriteToSpriteRenderer();
        // 要 Editor 実測: Step 2 が不要かどうかをフレームカウントで確認
    }

    // 部分スワップ（1 カテゴリのみ変更したい場合）
    public void SwapCategory(string category, string label)
    {
        bool success = _spriteResolver.SetCategoryAndLabel(category, label);
        if (!success)
            Debug.LogWarning($"[ClassVisualSwapper] Resolve failed: {category}/{label}");
    }
}
```

**Skeletal（SpriteSkin）との組み合わせ条件（公式必須要件）**:
- スワップ対象の全 Sprite が **同一スケルトン**（ボーン名・階層が一致）を持つこと
- Skinning Editor の Copy & Paste でボーンデータを複製すること

---

### Performance Notes

#### SRP Batcher 干渉回避

| 操作 | SRP Batcher への影響 | 推奨可否 |
|---|---|---|
| `SpriteRenderer.color = newColor` | **干渉しない**（per-object buffer 経由） | **推奨** |
| `MaterialPropertyBlock` 経由の色変更 | **SRP Batcher を破壊**（公式明記） | **禁止** |
| `GPU Deformation` + `SpriteRenderer.color` | 要 Editor 実測（公式記述なし） | 要確認 |

#### `SpriteRenderer.color` の URP 2D 適用挙動

- `SpriteRenderer.color` は URP 2D Sprite シェーダーの頂点カラー（`_Color`）として機能する per-object プロパティ。Material 定数バッファを変更しないため SRP Batch を分割しない。
- `MaterialPropertyBlock` 経由の `_Color` 設定は SRP Batcher 非互換（Unity 6.3 LTS 公式 API リファレンス明記）。
- GPU Deformation 有効時に `MaterialPropertyBlock` を使うと CPU Skinning フォールバックが発生する（SpriteSkin マニュアル 13.0.4 明記）。

#### テクスチャ再バインドコスト（SLA スワップ時）

- `_spriteLibrary.spriteLibraryAsset` を変更すると、各 `SpriteResolver` が新しい Sprite を解決し `SpriteRenderer.sprite` を更新する。
- 異なるテクスチャアトラスを参照する SLA をスワップした場合、テクスチャ再バインドが発生する。
- **推奨**: 職業ごとの Sprite を同一テクスチャアトラス（Sprite Atlas）に収録し、スワップ時のテクスチャバインドコストをゼロに抑える。

#### 未確認項目（要 Editor 実測）

- `_spriteLibrary.spriteLibraryAsset =` 代入から `SpriteRenderer.sprite` 更新までが同一フレーム内に完了するか（ADR-0001 の Pillar 1 要件）。
- SpriteSkin + GPU Deformation 有効時に `SpriteRenderer.color` 変更がバッチ分割を引き起こすか。
- Unityちゃん公式 PSB 由来の Skeletal Sprite で SLA スワップ後にボーン変形が崩れないか（スケルトン一致の確認）。

---

### Sources

1. Unity 6.3 LTS 同梱パッケージバージョン確認: https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.2d.animation.html
2. SpriteLibrary API（v13.0.4）: https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/api/UnityEngine.U2D.Animation.SpriteLibrary.html
3. SpriteResolver API（v13.0.4）: https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/api/UnityEngine.U2D.Animation.SpriteResolver.html
4. SpriteSkin manual（v13.0.4）: https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SpriteSkin.html
5. Sprite Swap examples — Full Skin Swap（v13.0.4）: https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/ex-sprite-swap.html
6. Sprite Swap intro — skeletal limitations（v13.0.4）: https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SpriteSwapIntro.html
7. MaterialPropertyBlock — SRP Batcher 非互換（Unity 6.3 LTS）: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MaterialPropertyBlock.html
8. SRP Batcher（Unity 6.3 LTS）: https://docs.unity3d.com/6000.3/Documentation/Manual/SRPBatcher.html
