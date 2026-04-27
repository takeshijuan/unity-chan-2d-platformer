using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.U2D.Animation;

/// <summary>
/// ADR-0001 Validation Gate R5 spike: SpriteSkin x SpriteLibraryAsset
/// ランタイムスワップの最小実証。Unity 6.3 LTS / com.unity.2d.animation 13.0.4 想定。
/// throw-away — 通過条件 (a)-(e) を Unity Editor で実測するための単機能スクリプト。
/// </summary>
public sealed class R5ClassSwitchSpike : MonoBehaviour
{
    [Header("References (Inspector assign required)")]
    [SerializeField] private SpriteLibrary _spriteLibrary;
    [SerializeField] private SpriteResolver _spriteResolver;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("SLA Variants (must share identical skeleton)")]
    [SerializeField] private SpriteLibraryAsset _slaA;
    [SerializeField] private SpriteLibraryAsset _slaB;

    [Header("Color Wash (validates condition e)")]
    [SerializeField] private Color _washA = new Color(0.90f, 0.22f, 0.27f); // 剣士相当
    [SerializeField] private Color _washB = new Color(0.30f, 0.78f, 0.92f); // 弓士相当
    [SerializeField, Min(0f)] private float _washSec = 0.15f;

    [Header("Validation Toggles")]
    [Tooltip("ON: ResolveSpriteToSpriteRenderer() を明示呼び出し / OFF: SLA 代入のみ（自動 resolve に依存）")]
    [SerializeField] private bool _explicitResolve = false;

    private bool _isB;
    private Coroutine _washRoutine;

    private void Awake()
    {
        Debug.Assert(_spriteLibrary != null, "[R5] _spriteLibrary 未アサイン", this);
        Debug.Assert(_spriteResolver != null, "[R5] _spriteResolver 未アサイン", this);
        Debug.Assert(_spriteRenderer != null, "[R5] _spriteRenderer 未アサイン", this);
        Debug.Assert(_slaA != null && _slaB != null, "[R5] SLA A/B 両方アサイン必須", this);
    }

    private void Update()
    {
        bool pressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (pressed) PerformSwitch();
    }

    private void PerformSwitch()
    {
        int frameBefore = Time.frameCount;
        Sprite spriteBefore = _spriteRenderer.sprite;

        Profiler.BeginSample("R5.SwapSLA");
        _isB = !_isB;
        _spriteLibrary.spriteLibraryAsset = _isB ? _slaB : _slaA;
        if (_explicitResolve) _spriteResolver.ResolveSpriteToSpriteRenderer();
        Profiler.EndSample();

        Profiler.BeginSample("R5.ColorWash");
        if (_washRoutine != null) StopCoroutine(_washRoutine);
        _washRoutine = StartCoroutine(ColorWashCoroutine(_isB ? _washB : _washA));
        Profiler.EndSample();

        Sprite spriteAfter = _spriteRenderer.sprite;
        Debug.Log(
            $"[R5] frame={frameBefore} sameFrame={Time.frameCount == frameBefore} " +
            $"spriteChanged={spriteBefore != spriteAfter} " +
            $"from={(spriteBefore != null ? spriteBefore.name : "null")} " +
            $"to={(spriteAfter != null ? spriteAfter.name : "null")} " +
            $"explicitResolve={_explicitResolve}",
            this);
    }

    private IEnumerator ColorWashCoroutine(Color tint)
    {
        _spriteRenderer.color = tint;
        yield return new WaitForSeconds(_washSec);
        _spriteRenderer.color = Color.white;
    }
}
