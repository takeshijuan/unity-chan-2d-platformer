// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do 12 Cinemachine 3.x API items match ADR-0006 assumptions?
// Date: 2026-04-30

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class R1CinemachineSpike : MonoBehaviour
{
    [Header("Runtime Verification (#9, #11)")]
    [SerializeField] private Component _brain;
    [SerializeField] private Transform _followTarget;
    [SerializeField, Min(1)] private int _sampleFrames = 120;

    private int _frameCount;
    private float _maxDelta;
    private float _sumDelta;
    private int _stutterCount;
    private float _prevCamY;
    private bool _done;
    private Camera _mainCamera;

    private static readonly string Prefix = "[R1]";

    private void Start()
    {
        if (_brain == null || _followTarget == null)
        {
            Debug.LogError($"{Prefix} _brain or _followTarget not assigned. Disabling.");
            enabled = false;
            return;
        }

        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError($"{Prefix} Camera.main not found. Disabling.");
            enabled = false;
            return;
        }

        var brainType = _brain.GetType();
        var execOrderAttrs = brainType.GetCustomAttributes(
            typeof(DefaultExecutionOrder), true);
        if (execOrderAttrs.Length > 0)
        {
            var attr = (DefaultExecutionOrder)execOrderAttrs[0];
            Debug.Log($"{Prefix} #11 CinemachineBrain DefaultExecutionOrder={attr.order}");
        }
        else
        {
            Debug.Log($"{Prefix} #11 CinemachineBrain has NO DefaultExecutionOrder attribute");
        }

        var updateProp = brainType.GetProperty("UpdateMethod");
        if (updateProp != null)
            Debug.Log($"{Prefix} #11 Brain.UpdateMethod={updateProp.GetValue(_brain)}");

        _prevCamY = _mainCamera.transform.position.y;

        bool hasBrain = _brain != null;
        var ppcType = FindType("UnityEngine.U2D.PixelPerfectCamera", "")
                   ?? FindType("UnityEngine.Rendering.Universal.PixelPerfectCamera", "");
        bool hasPPC = ppcType != null && _mainCamera.GetComponent(ppcType) != null;
        string urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name ?? "NONE";
        Debug.Log($"{Prefix} #9 TripleIntegration: brain={hasBrain} ppc={hasPPC} urp={urp}");
    }

    private void LateUpdate()
    {
        if (_done) return;
        if (_frameCount >= _sampleFrames) { Finish(); return; }

        var camPos = _mainCamera.transform.position;
        var targetPos = _followTarget.position;
        float followDelta = Vector2.Distance(
            new Vector2(camPos.x, camPos.y),
            new Vector2(targetPos.x, targetPos.y));

        float frameDelta = Mathf.Abs(camPos.y - _prevCamY);
        if (_frameCount > 10 && frameDelta > 0.01f && frameDelta < 0.5f)
            _stutterCount++;
        _maxDelta = Mathf.Max(_maxDelta, followDelta);
        _sumDelta += followDelta;
        _prevCamY = camPos.y;

        if (_frameCount % 30 == 0)
        {
            Debug.Log(
                $"{Prefix} #11 frame={_frameCount} " +
                $"followDelta={followDelta:F4} " +
                $"fixedDt={Time.fixedDeltaTime:F4} " +
                $"dt={Time.deltaTime:F4} " +
                $"timeScale={Time.timeScale:F2}");
        }

        _frameCount++;
    }

    private void Finish()
    {
        if (_done) return;
        _done = true;

        float avgDelta = _sampleFrames > 0 ? _sumDelta / _sampleFrames : 0f;
        Debug.Log(
            $"{Prefix} #9 TripleIntegration RESULT: " +
            $"stutterFrames={_stutterCount}/{_sampleFrames} " +
            $"maxFollowDelta={_maxDelta:F4} " +
            $"avgFollowDelta={avgDelta:F4}");
        Debug.Log(
            $"{Prefix} #11 ExecutionOrder RESULT: " +
            $"maxFollowDelta={_maxDelta:F4} " +
            $"threshold=0.005 " +
            $"PASS={_maxDelta <= 0.005f}");
        Debug.Log($"{Prefix} === Runtime verification complete. Copy console output to evidence file. ===");
    }

    private static Type FindType(string fullName, string assemblyHint)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.IsNullOrEmpty(assemblyHint) &&
                !asm.GetName().Name.Contains(assemblyHint))
                continue;
            try
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            catch (ReflectionTypeLoadException) { }
        }
        return null;
    }

#if UNITY_EDITOR
    [MenuItem("Window/R1 Spike/Cinemachine 3 API Check")]
    static void OpenWindow() => EditorWindow.GetWindow<R1ApiCheckerWindow>("R1 CM3 API Check");

    public sealed class R1ApiCheckerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _output = "";

        private void OnGUI()
        {
            if (GUILayout.Button("Run All Checks", GUILayout.Height(30)))
                RunAllChecks();

            if (GUILayout.Button("Copy to Clipboard") && !string.IsNullOrEmpty(_output))
                EditorGUIUtility.systemCopyBuffer = _output;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RunAllChecks()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# R1 Cinemachine 3 API Verification");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Unity: {Application.unityVersion}");
            sb.AppendLine();
            sb.AppendLine("| # | Item | Status | Expected | Actual | Notes |");
            sb.AppendLine("|---|------|--------|----------|--------|-------|");

            Check01(sb); Check02(sb); Check03(sb); Check04(sb);
            Check05(sb); Check06(sb); Check07(sb); Check08(sb);
            Check10(sb); Check12(sb);

            sb.AppendLine();
            sb.AppendLine("Items #9 (triple integration) and #11 (execution order) require Play mode.");
            sb.AppendLine("See R1CinemachineSpike MonoBehaviour console output.");

            _output = sb.ToString();
            Debug.Log($"{Prefix} === API Check Complete ===\n{_output}");
        }

        private void Row(StringBuilder sb, int num, string item, string status,
                         string expected, string actual, string notes)
        {
            string safeNotes = notes.Replace("|", "\\|");
            string safeActual = actual.Replace("|", "\\|");
            sb.AppendLine($"| {num} | {item} | {status} | {expected} | {safeActual} | {safeNotes} |");
            Debug.Log($"{Prefix} #{num} {item}: {status} expected={expected} actual={actual} {notes}");
        }

        private void Check01(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachineCamera", "Cinemachine");
            if (t != null)
                Row(sb, 1, "CinemachineCamera", "OK",
                    "Unity.Cinemachine.CinemachineCamera", t.FullName,
                    $"sealed={t.IsSealed}");
            else
                Row(sb, 1, "CinemachineCamera", "FAIL",
                    "Unity.Cinemachine.CinemachineCamera", "NOT FOUND", "");
        }

        private void Check02(StringBuilder sb)
        {
            var brainType = FindType("Unity.Cinemachine.CinemachineBrain", "Cinemachine");
            if (brainType == null) { Row(sb, 2, "Brain.UpdateMethod", "FAIL", "CinemachineBrain", "NOT FOUND", ""); return; }

            var enumNames = Array.Empty<string>();
            string defaultVal = "UNKNOWN";

            var prop = brainType.GetProperty("UpdateMethod", BindingFlags.Public | BindingFlags.Instance);
            var field = brainType.GetField("UpdateMethod", BindingFlags.Public | BindingFlags.Instance);
            var sField = brainType.GetField("m_UpdateMethod", BindingFlags.NonPublic | BindingFlags.Instance);

            Type enumType = prop?.PropertyType ?? field?.FieldType ?? sField?.FieldType;
            if (enumType != null && enumType.IsEnum)
                enumNames = Enum.GetNames(enumType);

            var brainInScene = UnityEngine.Object.FindFirstObjectByType(brainType);
            if (brainInScene != null && prop != null)
                defaultVal = prop.GetValue(brainInScene)?.ToString() ?? "null";

            string warning = defaultVal == "UNKNOWN" ? " [WARNING: no Brain in scene, default not verified]" : "";
            Row(sb, 2, "Brain.UpdateMethod", enumType != null ? "OK" : "FAIL",
                "SmartUpdate etc.",
                string.Join(",", enumNames),
                $"sceneDefault={defaultVal}{warning}");
        }

        private void Check03(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachineCamera", "Cinemachine");
            if (t == null) { Row(sb, 3, "Follow type", "FAIL", "Transform", "class NOT FOUND", ""); return; }

            var followProp = t.GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
            if (followProp != null)
                Row(sb, 3, "Follow type",
                    followProp.PropertyType == typeof(Transform) ? "OK" : "RENAMED",
                    "Transform", followProp.PropertyType.Name, "");
            else
            {
                var alt = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.Name.Contains("Track") || p.Name.Contains("Follow"))
                    .Select(p => $"{p.Name}:{p.PropertyType.Name}");
                Row(sb, 3, "Follow type", "RENAMED",
                    "Follow:Transform", string.Join(", ", alt), "");
            }
        }

        private void Check04(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachinePositionComposer", "Cinemachine");
            if (t != null)
                Row(sb, 4, "PositionComposer", "OK",
                    "CinemachinePositionComposer", t.FullName, "");
            else
            {
                var alternatives = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name.Contains("Cinemachine"))
                    .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException) { return Array.Empty<Type>(); } })
                    .Where(x => x.Name.Contains("Composer") || x.Name.Contains("Transposer"))
                    .Select(x => x.Name);
                Row(sb, 4, "PositionComposer", "RENAMED",
                    "CinemachinePositionComposer", string.Join(", ", alternatives), "");
            }
        }

        private void Check05(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachineConfiner2D", "Cinemachine");
            if (t == null) { Row(sb, 5, "Confiner2D", "FAIL", "CinemachineConfiner2D", "NOT FOUND", ""); return; }

            var bsField = t.GetField("BoundingShape2D", BindingFlags.Public | BindingFlags.Instance);
            var bsProp = t.GetProperty("BoundingShape2D", BindingFlags.Public | BindingFlags.Instance);
            var bsType = bsField?.FieldType ?? bsProp?.PropertyType;
            string bsAccess = bsField != null ? "field" : bsProp != null ? "property" : "NOT FOUND";

            var invalMethod = t.GetMethod("InvalidateBoundingShapeCache",
                BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

            bool contractMet = bsType != null && invalMethod != null;
            Row(sb, 5, "Confiner2D", contractMet ? "OK" : "FAIL",
                "BoundingShape2D + InvalidateBoundingShapeCache()",
                $"BS:{bsType?.Name ?? "N/A"}({bsAccess}) Inval={invalMethod != null}",
                contractMet ? "" : "CONTRACT INCOMPLETE");
        }

        private void Check06(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachineImpulseSource", "Cinemachine");
            if (t == null) { Row(sb, 6, "ImpulseSource overloads", "FAIL", "", "NOT FOUND", ""); return; }

            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("GenerateImpulse"))
                .Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})")
                .ToArray();

            Row(sb, 6, "ImpulseSource overloads",
                methods.Length >= 3 ? "OK" : "FAIL",
                "GenerateImpulse() >= 3 overloads",
                $"{methods.Length} overloads",
                string.Join(" ; ", methods));
        }

        private void Check07(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachineImpulseListener", "Cinemachine");
            if (t == null) { Row(sb, 7, "ImpulseListener", "FAIL", "", "NOT FOUND", ""); return; }

            var expected = new[] { "Gain", "Use2DDistance", "ChannelMask", "UseCameraSpace" };
            var members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                .Select(m => m.Name)
                .ToArray();

            var found = expected.Where(e => members.Contains(e)).ToArray();
            var missing = expected.Where(e => !members.Contains(e)).ToArray();
            bool hasOld = members.Contains("UseSignalSpaceOnly");

            Row(sb, 7, "ImpulseListener", missing.Length == 0 ? "OK" : "RENAMED",
                string.Join(",", expected),
                $"found=[{string.Join(",", found)}] missing=[{string.Join(",", missing)}]",
                $"UseSignalSpaceOnly={hasOld}");
        }

        private void Check08(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachinePixelPerfect", "Cinemachine");
            if (t == null)
            {
                Row(sb, 8, "CinemachinePixelPerfect", "FAIL",
                    "exists", "NOT FOUND", "Check Samples folder");
                return;
            }

            var declaredMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var menuAttr = t.GetCustomAttributes(true)
                .FirstOrDefault(a => a.GetType().Name == "AddComponentMenuAttribute");
            bool isStub = declaredMethods.Length == 0;

            Row(sb, 8, "CinemachinePixelPerfect",
                isStub ? "STUB" : "OK",
                "CinemachinePixelPerfect (functional)",
                $"declaredMethods={declaredMethods.Length}",
                isStub ? "EMPTY STUB - use Plan B (see README)" : $"AddComponentMenu={menuAttr}");
        }

        private void Check10(StringBuilder sb)
        {
            var t = FindType("UnityEngine.U2D.PixelPerfectCamera", "")
                 ?? FindType("UnityEngine.Rendering.Universal.PixelPerfectCamera", "");
            if (t == null) { Row(sb, 10, "PixelPerfectCamera", "FAIL", "", "NOT FOUND", "check com.unity.2d.pixel-perfect or URP bundle"); return; }

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => $"{p.Name}:{p.PropertyType.Name}").ToArray();
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => $"{f.Name}:{f.FieldType.Name}").ToArray();

            var refXProp = t.GetProperty("refResolutionX") ?? t.GetProperty("referenceResolutionX");
            var refYProp = t.GetProperty("refResolutionY") ?? t.GetProperty("referenceResolutionY");
            var cropProp = t.GetProperties().FirstOrDefault(p =>
                p.Name.Contains("cropFrame") || p.Name.Contains("CropFrame"));

            string hint = (refXProp == null && refYProp == null) ? " [check assetsPPU or URP bundle naming]" : "";
            Row(sb, 10, "PixelPerfectCamera", t != null ? "OK" : "FAIL",
                "refResolutionX/Y + CropFrame",
                $"refX={refXProp?.Name ?? "N/A"} refY={refYProp?.Name ?? "N/A"} crop={cropProp?.Name ?? "N/A"}",
                $"allProps=[{string.Join(",", props.Concat(fields).Take(25))}]{hint}");
        }

        private void Check12(StringBuilder sb)
        {
            var t = FindType("Unity.Cinemachine.CinemachineBrain", "Cinemachine");
            if (t == null) { Row(sb, 12, "Brain.OutputCamera", "FAIL", "", "NOT FOUND", ""); return; }

            var prop = t.GetProperty("OutputCamera", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
                Row(sb, 12, "Brain.OutputCamera", "OK", "Camera", prop.PropertyType.Name, "");
            else
            {
                var alt = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(Camera)).Select(p => p.Name);
                Row(sb, 12, "Brain.OutputCamera", "RENAMED",
                    "OutputCamera:Camera", string.Join(",", alt), "");
            }
        }
    }
#endif
}
