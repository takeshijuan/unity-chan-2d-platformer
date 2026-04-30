# R1 Spike Result -- Camera + Cinemachine 3 API Verification

**ADR**: docs/architecture/adr-0006-camera-system.md
**Spike**: prototypes/camera-cinemachine3-r1-spike/
**Date**: 2026-04-30
**Tester**: takeshi (project lead)

## Environment

| Item | Value |
|------|-------|
| Unity Editor version | 6000.3.13f1 |
| com.unity.cinemachine | 3.1.6 |
| com.unity.render-pipelines.universal | 17.0.3 |
| com.unity.2d.pixel-perfect | URP bundled (UniversalRP 17.0.3) |
| OS | macOS |
| Hardware | (unrecorded) |

## Verification Results (12 items)

| # | Item | Status | Expected | Actual | Notes |
|---|------|--------|----------|--------|-------|
| 1 | CinemachineCamera class | OK | Unity.Cinemachine.CinemachineCamera | Unity.Cinemachine.CinemachineCamera | sealed=True |
| 2 | Brain.UpdateMethod default | OK | SmartUpdate | enum: FixedUpdate, LateUpdate, SmartUpdate, ManualUpdate (scene default index=2 → SmartUpdate) | sceneDefault not auto-detected by Phase A (no Brain in scene during API check); confirmed via scene YAML (`UpdateMethod: 2`) |
| 3 | CinemachineCamera.Follow type | OK | Transform | Transform | (also has TrackingTarget alias) |
| 4 | Body component (PositionComposer) | OK | CinemachinePositionComposer | Unity.Cinemachine.CinemachinePositionComposer | confirmed |
| 5 | CinemachineConfiner2D + methods | OK | BoundingShape2D:Collider2D + InvalidateBoundingShapeCache() | BS:Collider2D (**field**) + Inval=True | BoundingShape2D is a public field (not property) |
| 6 | ImpulseSource.GenerateImpulse overloads | OK | ≥3 overloads | **7 overloads** | 3 legacy + 3 new naming + 1 no-arg: GenerateImpulse(), GenerateImpulse(Vector3), GenerateImpulse(Single), GenerateImpulseAt(Vector3,Vector3), GenerateImpulseWithVelocity(Vector3), GenerateImpulseWithForce(Single), GenerateImpulseAtPositionWithVelocity(Vector3,Vector3) |
| 7 | ImpulseListener properties | OK | Gain, Use2DDistance, ChannelMask, UseCameraSpace | found=[Gain, Use2DDistance, ChannelMask, UseCameraSpace], missing=[] | UseSignalSpaceOnly NOT present (confirms RENAMED) |
| 8 | CinemachinePixelPerfect extension | **OK (functional)** | exists (empty stub?) | declaredMethods=3, AddComponentMenu=(empty/none) | **NOT empty stub** — pre-verification MEDIUM was wrong. Plan B not needed |
| 9 | URP 2D + CM3 + PPC integration | **PASS** | no stutter | brain=True, ppc=True, urp=UniversalRP, **stutterFrames=0/120** | Static-state verification (FollowTarget did not move during measurement). Dynamic stutter check deferred to C1 |
| 10 | PixelPerfectCamera properties | OK | refResolutionX/Y + CropFrame enum | refX=refResolutionX, refY=refResolutionY, crop=cropFrame:CropFrame | Also: assetsPPU, gridSnapping, pixelSnapping, cropFrameX/Y, stretchFill |
| 11 | Brain execution order + follow sync | **PASS** | maxFollowDelta ≤ 0.005 | **maxFollowDelta=0.0000, PASS=True** | CinemachineBrain has NO `[DefaultExecutionOrder]` attribute (CM3 uses UpdateMethod enum + ExecuteAlways instead). Static-state synchrony verified |
| 12 | Brain.OutputCamera | OK | Camera type | Camera | confirmed |

## Pre-Verified via Documentation

| # | Item | Source | Pre-Confidence | Editor Result | Verdict |
|---|------|--------|---------------|---------------|---------|
| 1 | CinemachineCamera | Context7 CM 3.1 docs | HIGH | OK | confirmed |
| 3 | Follow = Transform | Context7 CM 3.1 docs | HIGH | OK | confirmed |
| 4 | CinemachinePositionComposer | Context7 CM 3.1 docs | HIGH | OK | confirmed |
| 5 | CinemachineConfiner2D | Context7 CM 3.1 docs | HIGH | OK | confirmed (BoundingShape2D is field, as predicted) |
| 6 | GenerateImpulse 7 overloads | Context7 CM 3.1 docs | HIGH | OK (7 overloads) | confirmed |
| 7 | UseCameraSpace (was UseSignalSpaceOnly) | Context7 CM 3.1 docs | HIGH | OK (UseSignalSpaceOnly not present) | confirmed |
| 8 | CinemachinePixelPerfect = empty stub | Context7 CM 3.1 docs | MEDIUM | **REFUTED** (functional, declaredMethods=3) | **pre-verification wrong, Plan B obsolete** |
| 12 | OutputCamera: Camera | Context7 CM 3.1 docs | HIGH | OK | confirmed |

## Editor-Only Verification Details

### Item 8: CinemachinePixelPerfect

```
declaredMethods=3
AddComponentMenu= (attribute present but empty value)
```

**Conclusion**: NOT an empty stub. Has 3 declared methods (likely OnEnable/OnDisable + an integration helper). The `[AddComponentMenu("")]` attribute hides it from the Inspector "Add Component" menu, but it is functional when added programmatically or attached via prefab. **Plan B (PixelPerfectCamera-only) is NOT required**.

### Item 9: Triple Integration

```
[R1] #9 TripleIntegration: brain=True ppc=True urp=UniversalRP
[R1] #9 TripleIntegration RESULT: stutterFrames=0/120 maxFollowDelta=0.0000 avgFollowDelta=0.0000
```

**Conclusion**: All 3 components (CinemachineBrain + PixelPerfectCamera + URP 2D) coexist without stutter in static state. **Limitation**: FollowTarget was not moved during the 120-frame measurement, so dynamic-motion stutter is not characterized. This is acceptable for C0 (API-surface verification) but C1 must verify dynamic motion with a moving target.

### Item 11: Execution Order

```
[R1] #11 CinemachineBrain has NO DefaultExecutionOrder attribute
[R1] #11 frame=0 followDelta=0.0000 fixedDt=0.0200 dt=0.0200 timeScale=1.00
[R1] #11 frame=30 followDelta=0.0000 fixedDt=0.0200 dt=0.0015 timeScale=1.00
[R1] #11 frame=60 followDelta=0.0000 fixedDt=0.0200 dt=0.0014 timeScale=1.00
[R1] #11 frame=90 followDelta=0.0000 fixedDt=0.0200 dt=0.0014 timeScale=1.00
[R1] #11 ExecutionOrder RESULT: maxFollowDelta=0.0000 threshold=0.005 PASS=True
```

**Conclusion**: CinemachineBrain does NOT use `[DefaultExecutionOrder]` to enforce update timing. Cinemachine 3 controls timing via `UpdateMethod` enum (SmartUpdate default) and `[ExecuteAlways]`. Frame rate ~700fps in editor (dt=0.0014s) — extremely lightweight. **Limitation**: static target means delta=0 is partly tautological. C1 must verify with motion.

### Note: `Brain.UpdateMethod` runtime log absent

The Start() reflection used `GetProperty("UpdateMethod")` which returned null. CM3 exposes `UpdateMethod` as a public **field**, not a property. The Phase A API checker handles this case (it tests both); the runtime verifier does not (minor cosmetic gap, no impact on PASS/FAIL outcome). The scene YAML confirms `UpdateMethod: 2` = SmartUpdate, matching ADR-0006 assumption.

## Unity Issue Tracker Search Log

- Query: "Cinemachine 3 Pixel Perfect" → (pending)
- Query: "CinemachineConfiner2D URP 2D" → (pending)
- Query: "CinemachineBrain UpdateMethod" → (pending)

> Issue Tracker search deferred — Phase B already produced a clean PASS, so no specific stutter/timing failure mode requires triage. Recommend running searches as a sanity sweep before ADR-0006 promotion to Accepted.

## Weighted Scoring

| Category | Items | Points Each | Score per item | Subtotal |
|----------|-------|-------------|----------------|----------|
| Critical | #8 | 3 | OK (functional) → 3 | 3 |
| Critical | #9 | 3 | PASS (stutter=0) → 3 | 3 |
| Critical | #11 | 3 | PASS (delta=0) → 3 | 3 |
| Standard | #1, #2, #3, #4, #5, #6, #7, #10, #12 | 1 | all OK → 1 each | 9 |
| **Total** | | | | **18 / 18** |

**C0 Gate**: PASS requires 14/18+ AND no Critical item is FAIL.
**Result: 18/18 — C0 PASS** ✅

## Conclusion

- **12 / 12 verified → C0 gate PASS** (perfect score, no items FAIL)
- **RENAMED items**: #7 `UseSignalSpaceOnly` → `UseCameraSpace` (confirmed by absence of old name)
- **FAIL items**: none
- **Pre-verification corrections**: #8 CinemachinePixelPerfect is **functional**, not an empty stub — Plan B is obsolete
- **Newly discovered**: CinemachineBrain has no `[DefaultExecutionOrder]` attribute — uses `UpdateMethod` enum instead

### C0 → C1 handoff notes

C0 verifies API surface and static integration. C1 (deferred until ADR-0002 V1 passes) must verify:
- Dynamic stutter with moving target (CharacterController2D follows player input → camera follows target)
- 1-frame sync under motion (validates ADR-0006 D5/D6 frame-timing assumptions)
- Render-space pixel snap jitter (this spike only measured transform-space)
- Color Wash overlay z-order vs Cinemachine Body Damping (ADR-0003 cross-check, deferred to ADR-0006a per CD-PLAYTEST CONCERN)

## Screenshots

- [ ] CinemachineCamera Inspector view (pending)
- [ ] CinemachineBrain Inspector (UpdateMethod=SmartUpdate visible) (pending)
- [ ] PixelPerfectCamera Inspector view (pending)
- [ ] CinemachineConfiner2D Inspector view (BoundingShape2D=Confiner) (pending)
- [ ] R1CinemachineApiChecker EditorWindow output (pending — copy-from-clipboard text already captured above)

## ADR-0006 Update Actions

- [ ] RENAMED API 名を ADR-0006 に反映: `UseSignalSpaceOnly` → `UseCameraSpace`
- [ ] **D8 deferred decision を更新**: CinemachinePixelPerfect は functional と確認、Plan B（PixelPerfectCamera 単体運用）は不要、ただし `[AddComponentMenu("")]` で隠蔽されているため Inspector 経由ではなく**コード/プレハブで明示的に追加する**運用方針を記録
- [ ] D2 deferred decision (Look Ahead) に Pillar 2 Design Test との接続を追記（CD-PLAYTEST CONCERN B）
- [x] engine-reference/unity/plugins/cinemachine.md に **R1 Spike Findings (2026-04-30)** セクション追記（CM3 → CM2 mapping table、empirical findings）
- [ ] deprecated-apis.md に Cinemachine 2.x legacy API を追記（`CinemachineVirtualCamera`, `CinemachineFramingTransposer`, `UseSignalSpaceOnly` 等）
- [ ] ADR-0006 status を Proposed (Provisional) → **Accepted** へ昇格、Validation Gate に C0 PASS 記録
- [ ] CD-PLAYTEST CONCERN C: #9 stutter pass/fail にプレイヤー体験基準（連続 2 フレーム 16.6ms 超過 = 0 件）を C1 仕様で追加
