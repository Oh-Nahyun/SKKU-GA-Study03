# AI와 함께하는 그래픽 품질 개선 및 최적화 보고서

> **작성 상태**: 수치 반영 초안. 수치는 `result-table.md`의 실측값만 옮겼다. `[본인 작성]`은 사용자가 직접 쓸 부분, `[미수행]`은 이번에 측정하지 못한 항목이다.
> 제출: 11/3(화) 11:00까지 노션 링크. 스크린샷은 `Docs/Report/img/`의 파일명으로 표기했으니 노션에 직접 업로드한다.

## 1. 개요
- 프로젝트: Unity 6000.3 URP 3D FPS (플레이어가 총·폭탄·검으로 좀비를 처치)
- 목표 기기: 보급형~중급 PC (`PC_RPAsset` 기준)
- 사용한 AI: Claude Code (코드/설정 파일 분석, 변경 제안, 코드 수정, Unity Editor를 직접 조작한 측정)
- 측정 환경: i9-14900HX / RTX 4060 Laptop(VRAM 8GB) / RAM 32GB / Windows 11 / Unity 6000.3.22f1, 에디터 Play 모드(Development Build 미측정 → 에디터 값은 참고용)
- 측정 시나리오: Game 뷰 1920×1080, VSync 끔, 좀비 10마리 상한 도달(Play 후 약 35초) 후 측정. 수집은 `UnityEditor.UnityStats`(Game 뷰 Stats와 같은 소스) + `FrameTimingManager`(GPU ms)를 프레임마다 샘플링
- 측정 방식의 보정 (중요):
  - 좀비가 움직이면 Batches가 395~490으로 흔들려 렌더링 비교가 안 됐다. 그래서 조명·후처리·GPU 항목은 **좀비 10마리를 고정 위치에 놓고 `timeScale=0`**으로 정지시킨 뒤 같은 시점에서 비교했다. 이 구간에는 좀비 AI 같은 스크립트 비용이 빠진다.
  - 에디터 CPU 프레임 시간이 세션 중 7.7→10.5ms로 계속 올라가는 드리프트가 있어, 핵심 항목은 **기준–변경–변경–기준(ABBA)** 순서로 두 번씩 재서 평균을 비교했다. 지표는 GPU ms, Batches, SetPass 위주다.
  - 이 환경은 CPU가 병목이다(CPU 8~10ms, GPU 약 2ms). GPU 이득이 FPS로 바로 이어지지 않는다.

## 2. 조명 및 포스트프로세싱

### 2.1 이미 적용한 것 (환경 개선 전 `4376841` → 현재 HEAD)
| 항목 | 이전 | 현재 |
|---|---|---|
| 스카이박스 | Unity 기본 프로시저럴 | 전용 셰이더 `AoTSky` (그라데이션 + 적운 + 태양 글로우) |
| 태양광 | 흰색 2.0 | 따뜻한 색(1, 0.9, 0.72), 강도 2.4, 각도 32° |
| 환경광 | Skybox 모드 | Trilight(하늘/중간/바닥) |
| 안개 | 없음 | Exp2, 밀도 0.022 |
| 포스트프로세싱 | Volume 프로필이 **비어 있음** (효과 0개) | ACES 톤매핑, Color Adjustments(대비 +18, 채도 +10), Bloom, Vignette, Shadows/Midtones/Highlights |
| 지면·건물·박스 | 단색 | 절차적 초원/석재 벽돌/목재 텍스처 |
- Before/After 스크린샷(`4376841` 대비): **[미수행]** 이전 커밋은 별도 worktree로 열어 캡처해야 한다. 현재 HEAD 기준 3방향 스크린샷은 `img/base_before_view1~3.png`.

### 2.2 조사로 확인한 조명 상태 (AI가 파일을 분석한 결과)
- `GameScene.unity`에 Directional Light가 **2개** 활성이었다: Light A(`Map_v1` 프리팹 내부, Mixed, 강도 1.5, 흰색) / Light B(`Environment`, Realtime, 강도 2.4, 따뜻한 색). URP 메인 라이트(`RenderSettings.sun`)는 B로 자동 선택되고 있었다. 베이크 라이트맵, Light Probe, Reflection Probe는 **없었다**.
- `PC_Renderer.asset`: 렌더링 경로는 **Forward+**(`m_RenderingMode: 2`), **SSAO가 이미 활성**(풀해상도). AI가 처음에 이 경로를 Deferred로 잘못 읽었고, 측정 단계에서 바로잡았다(→ 4.3). → "AO를 추가한다"가 아니라 "이미 있는 AO의 비용을 조절한다"가 맞다.
- `PC_RPAsset.asset`: 그림자 맵 2048, 캐스케이드 4, 거리 50, 소프트 품질 High, Opaque Texture 켬.

### 2.3 조명 개선 시도와 결과
정지 좀비 기준, Frame(CPU) ms는 3회 창 중앙값. 전체 표는 `result-table.md` A절.

| 단계 | 변경 | Frame ms | Batches | SetPass | Shadow casters | 스크린샷 |
|---|---|---|---|---|---|---|
| 1-1 | Light A 비활성, `RenderSettings.sun`=Light B 명시 | 8.17 → 8.03 | 635 → 635 | 93 → 93 | 657 → 657 | `L0_before_view*`, `L1_after_view*` |
| 1-2 | Light B Mixed + Distance Shadowmask, 라이트맵 1장(2048², 10 texels/unit, 베이크 AO, Progressive GPU, 베이크 17초) | 7.72 → 7.77 | 641 → 641 | 90 → 90 | 745 → 745 | `S11_view*`, `S12_view*` |
| 1-3 | + Light Probe 50개 + Reflection Probe 2개(Box projection) | 7.77 → 7.94 | 641 → 643 | 90 → 90 | 745 → 745 | `S13_view1~3` |

- 세 단계 모두 **프레임 시간이 노이즈(±0.3~0.5ms) 안**이라 성능 개선은 없었다. 이 단계의 가치는 화질이다.
- 1-1: Light A는 흰색 1.5 보조광이라 제거하자 화면이 눈에 띄게 어두워졌다(성능 이득 없이 화질만 변화).
- 1-2: 이 맵은 작아서 대부분 오브젝트가 그림자 거리(50) 안에 있어 Distance Shadowmask가 아끼는 "원거리 실시간 그림자"가 거의 없었다. 라이트맵 `.exr`은 약 32MB.
- 1-3: 프로브 비용은 작다는 것을 확인(Batches +2).

**1-4 그림자 설정 (URP 에셋 값만 바꿔 같은 세션에서 비교, 단독)**
| 구성 | Frame ms | Batches | Tris | Shadow casters | 스크린샷 |
|---|---|---|---|---|---|
| 기준(캐스케이드 4, 거리 50, 소프트 High) | 8.19 | 643 | 7.75M | 745 | `S13_view*` |
| 캐스케이드 4→2 | 8.17 | 546 | 6.70M | 467 | `S14_cascade2_view*` |
| 거리 50→35 | 8.06 | 704 | 9.26M | 683 | `S14_dist35_view*` |
| 소프트 High→Low | 8.15 | 644 | 7.75M | 745 | `S14_soft1_view*` |
- 거리 50→35는 Shadow casters가 줄었는데도 Batches/Tris가 **오히려 늘었다**. 원인은 미확정(캐스케이드 분할 경계 변화로 추정, Frame Debugger 확인 안 함) → 채택하지 않았다.

### 2.4 후처리 효과별 비교
같은 세션에서 효과를 하나씩 끄고 측정. GPU ms는 `FrameTimingManager`. (Frame ms는 드리프트가 커서 판단에 쓰지 않음.)

| 구성 | GPU ms | Batches | SetPass | Tris | 화질 변화 |
|---|---|---|---|---|---|
| 전체 켬 (기준) | 2.25 | 640 | 87 | 7.75M | 기준 (`P_base_view*`) |
| Bloom 끔 | 2.20 | 623 | 71 | 7.75M | 육안으로 거의 차이 없음 |
| **SSAO 끔** | **1.59 (−29%)** | **479** | 70 | **5.70M** | 접촉 그림자가 사라져 장면이 평평해짐 (`P_ssao_off_view*`) |
| Vignette 끔 | 2.25 | 639 | 87 | 7.75M | 가장자리 어두움만 제거 |
| Color Adjustments 끔 | 2.24 | 640 | 87 | 7.75M | 색 보정 제거 |
| Tonemapping(ACES) 끔 | 2.22 | 640 | 87 | 7.75M | 대비가 빠지고 밝고 뿌연 화면. **룩에 가장 큰 영향** (`P_tonemap_off_view*`) |
| Shadows/Midtones/Highlights 끔 | 2.25 | 639 | 87 | 7.75M | (스크린샷 저장, 육안 비교 미수행) |
| Balanced 세트(Bloom 반복 6→4·Downscale Half→Quarter, SSAO Downsample 켬·Samples Medium→Low) | 2.05 (−9%) | 633 | 81 | 7.75M | (스크린샷 저장, 육안 비교 미수행) `P_balanced_view*` |
| Performance 세트(Balanced Bloom + SSAO 끔) | 1.57 (−30%) | 473 | 64 | 5.70M | SSAO 끔과 같은 평평한 룩 `P_performance_view*` |

- 비용의 대부분은 **SSAO**다. Color/Tonemap/Vignette/SMH는 URP가 Uber 포스트 패스 하나로 합쳐 처리해 개별 비용이 거의 없다(추정). Bloom은 패스 수는 줄지만 비용 자체는 작다.
- 이 표는 **측정만** 했고 설정은 원래 값으로 되돌려 두었다. 어떤 세트를 쓸지는 [본인 작성: 룩 우선인지 성능 우선인지].

## 3. 최적화

### 3.1 AI가 제안한 방법과 채택 여부
AI에게 "CPU/GPU/RAM 관점에서 개선 효과가 큰 항목"을 프로젝트 파일을 읽고 찾게 했다. 제안과 최종 판단:

| 영역 | AI가 찾은 문제 (근거) | 결과 |
|---|---|---|
| GPU | 미니맵 카메라가 맵 전체를 매 프레임 한 번 더 렌더링(그림자·Depth/Opaque 복사 켬) | **채택** (G1) |
| GPU | 좀비 1마리 = SkinnedMeshRenderer 14개, 전부 그림자 캐스팅 | 측정 후 **기각** (G2) |
| GPU | 캐스케이드 4 / 소프트 High / Opaque Texture 켬 | **채택** (G3) |
| GPU | "Deferred 렌더링" → Forward+ 실험 | **전제가 틀림**: 이미 Forward+. Deferred와 비교 후 미적용 (G4) |
| CPU | `Zombie.cs` 매 프레임 `SetDestination`, `Vector3.Distance` | 코드 적용, **측정상 효과 없음** (C1) |
| CPU | `UI_PlayerWeapons.cs` 매 프레임 문자열 2개 생성 | 코드 적용, 측정상 효과 없음 (C2) |
| CPU | `ZombieHealthBar.cs`, `PlayerZoom.cs` `Camera.main` | 코드 적용, 측정상 효과 없음 (C3) |
| CPU | `Box.cs` 박스 파괴 시 큐브 27개 `CreatePrimitive` | **미수행** (C4) |
| RAM | 산업 팩 큰 텍스처 2048², Streaming Mipmaps 꺼짐 | 일부 **채택** (R1) |
| RAM | 플레이어 노멀맵 타입 오류, FBX 불필요 임포트 | 일부 **채택** (R2) |

**AI 제안을 그대로 쓰지 않은 경우 (직접 판단)**
- `StopAgent()`를 Idle/Attack에서 매 프레임 호출하는 것: 호출을 줄이면 NavMeshAgent가 밀려서 미끄러질 수 있어 동작 확인 전에는 바꾸지 않았다.
- `UI_PlayerStats`의 스태미너 슬라이더 매 프레임 대입: `Slider.value`는 같은 값이면 내부에서 건너뛰므로 이득이 거의 없다고 보고 적용하지 않았다.
- `Debug.Log`→`GameLogger.Log` 교체: `ENABLE_LOG`가 Standalone에 정의돼 있어 에디터에서는 효과가 보이지 않는다(심볼을 제거해야 효과 확인 가능, 이번엔 미측정).
- "미사용 에셋 정리로 메모리 절감" 후보: 빌드에는 씬이 참조하는 것만 포함되므로 런타임 메모리와 무관하다고 정리했다(디스크·임포트 시간에만 영향).

### 3.2 적용한 코드 변경 (C1~C3)

**C1. 좀비 AI** (`Assets/02.Scripts/Monster/Zombie.cs`)
```csharp
// Before: 매 프레임 호출 (좀비 수에 비례해 경로 재계산 요청)
_agent.SetDestination(_player.transform.position);

// After: 0.25초(인스펙터에서 조정) 주기로만 갱신
UpdateDestination(_player.transform.position);

private void UpdateDestination(Vector3 destination)
{
    _pathUpdateTimer -= Time.deltaTime;
    if (_pathUpdateTimer > 0f) return;

    _pathUpdateTimer = _pathUpdateInterval;
    _agent.SetDestination(destination);
}
```
- `Vector3.Distance` → `sqrMagnitude` 비교, `Debug.Log` 9곳 → `GameLogger.Log`, 공격마다 `GetComponent<IDamageable>()` → `Start`에서 캐시, 상태 전이 직후 목적지를 바로 설정하도록 타이머 초기화.

**C2. 무기 UI** (`UI/UI_PlayerWeapons.cs`): 이전 표시값과 다를 때만 텍스트 갱신.

**C3. `Camera.main` 캐시** (`ZombieHealthBar.cs`, `PlayerZoom.cs`): 매 프레임 조회 → 한 번 조회 후 필드 저장.

**검증**: `dotnet format --verify-no-changes`, `dotnet build` 통과, 플레이 중 좀비 추적·UI 정상 동작 확인.

### 3.3 설정/에셋 변경과 결과 (Before / After)

**GPU 계열.** 같은 세션 A/B, 핵심 항목은 ABBA. 기준 = 1-3 조명 상태 + URP 원본값.

| ID | 변경 | 적용 | Frame(CPU) ms | GPU ms | Batches | SetPass | Tris |
|---|---|---|---|---|---|---|---|
| G1-a | 미니맵 카메라 Render Shadows 끔 | ✅ | - | 2.23 → 1.87 | 643 → 473 | 90 → 83 | 7.75M → 5.69M |
| G1-b | 미니맵 Depth/Opaque Texture 끔 | ✅ | - | 2.22 | 641 | 88 | 7.75M |
| G1-c | 미니맵 RT 512→256 | ❌(화질 우선) | - | 2.20 | 643 | 90 | 7.75M |
| **G1 합계** | | | **8.63 → 7.93 (−0.70ms, −8%)** | **2.24 → 1.82** | **643 → 472** | 90 → 81 | 7.75M → 5.69M |
| G2 | 좀비 손·팔뚝 SMR 4개 그림자 끔 | ❌ | 8.20/8.22 vs 8.30 (−0.1, 노이즈) | 2.24 (변화 없음) | 643 → 582 | 90 | 7.74M |
| G3-a | Opaque Texture 끔 | ✅ | - | 2.26 → 2.21 | 643 → 641 | 90 → 87 | 7.75M |
| G3-b | 캐스케이드 4→2 | ✅ | - | 2.26 → 2.06 | 643 → 545 | 90 → 83 | 7.75M → 6.70M |
| G3-c | 소프트 High→Low | ✅ | - | 2.23 | 643 | 90 | 7.75M |
| **G3 합계** | | | 8.30/8.29 → 8.19/8.15 (−0.14ms) | **2.24 → 2.05** | **643 → 543** | 90 → 80 | 7.75M → 6.70M |
| G4 | Forward+ → Deferred (실험) | ❌ | 참고용 | 2.24 → 2.13 | 643 → 496 | 90 → 86 | 7.75M → 5.70M |
| G4 | Forward+ → Forward (실험) | ❌ | 참고용 | 2.24 → 2.21 | 643 | 90 | 7.75M |

**GPU 계열 최종(G1·G3·R1·R2 모두 적용 후 재측정, `P3_final_view1~3.png`)**: GPU **2.24 → 1.81ms(−19%)**, Batches **643 → 433(−33%)**, SetPass 90 → 76, Tris 7.75M → 5.16M, Shadow casters 745 → 297, Frame(CPU) 8.3~8.6 → 7.47ms. 화질은 육안으로 동일.

**CPU 코드 (C1~C3).** 코드 변경을 stash로 되돌린 HEAD와 비교. 좀비가 **움직이는** 상태 10초×3창. `Update.ScriptRunBehaviourUpdate`(모든 스크립트 Update 합계)와 `GC Allocated In Frame`을 `ProfilerRecorder`로 수집.

| 지표 | HEAD | C1~C3 적용 |
|---|---|---|
| 스크립트 Update 합계 | 0.12ms | 0.13ms |
| GC Alloc/프레임(중앙값) | 16.9KB | 16.7KB |

→ **측정 가능한 개선 없음.** (4장 참고)

**RAM.** 플레이 진입 8~10초 후 `Texture.currentTextureMemory`(에디터 값, 참고용).

| ID | 변경 | 적용 | Texture 메모리 |
|---|---|---|---|
| 기준 | | - | 109.3MB |
| R1-a | 산업 팩 텍스처 4개(Hangar_v3/v4, Cargo_container_v1, Industrial_pipe_v1) 2048→1024 | ✅ | 109.3 → 102.3MB (−7.0) |
| R1-b | Streaming Mipmaps 켬(28개 텍스처 + PC 퀄리티) | ❌ 원복 | 102.3 → 102.3 (변화 없음) |
| R2-a | 플레이어 normal 2장 → Normal map 타입(2048 유지) | ❌ | 개당 5.36 → 10.71MB (**+10.7MB 합계, 저하**) |
| R2-b | 위 + 해상도 1024 | ✅ | 개당 2.71MB (원본 대비 −5.3MB 합계) |
| R2-c | 플레이어/검 FBX의 BlendShape·Camera·Light·Animation 임포트 끔 | ❌ 원복 | Mesh 메모리 64.32/39.02MB 변화 없음 |
| **최종(R1-a + R2-b)** | | | **109.3 → 99.6MB (−9.7MB, −9%)** |

스크린샷: `img/R1_after_containers.png`(컨테이너 1024 화질), `img/P3_final_view1~3.png`(최종 3방향).

**측정하지 못한 것** (과제 요건에 비해 부족한 부분): Frame Debugger 캡처, Profiler 창 스크린샷(대신 `UnityStats`/`ProfilerRecorder` 수치를 사용), Development Build 측정. 필요하면 사용자가 직접 캡처해 보강한다. [본인 작성/캡처]

## 4. 분석

### 4.1 개선된 항목과 이유
- **미니맵 그림자 끄기(G1)가 가장 효과가 컸다.** CPU 프레임 −0.70ms(−8%), GPU −0.4ms, Batches −171. 미니맵 카메라가 메인 카메라와 별개로 그림자 패스까지 한 번 더 돌고 있었던 것으로 보인다. 몇 개의 체크박스를 끈 것만으로 얻은 이득이라 비용 대비 효과가 가장 좋았다.
- **캐스케이드 4→2(G3)**: Shadow casters 745→467, Batches −98, GPU −0.20ms. 스크린샷으로는 차이를 구분하기 어려웠다.
- **후처리는 SSAO가 비용의 거의 전부**다(GPU −29%). 반면 화질 기여도 가장 눈에 띄는 접촉 그림자라서 "룩 vs 성능" 선택이 필요하다.
- **RAM**: 텍스처 4개를 1024로 줄여 −7.0MB, 플레이어 normal 2장을 올바른 타입 + 1024로 −5.3MB. 합계 −9.7MB.

### 4.2 개선되지 않았거나 저하된 항목과 원인 (추정은 추정으로 표기)
- **조명 1-1~1-3**: 프레임 시간이 노이즈 범위. Light A는 Deferred가 아니라 Forward+에서 추가 라이트 하나일 뿐이고 그림자도 메인 라이트만 드리워서 비용이 작았다(추정). Distance Shadowmask는 그림자 거리 **밖**의 정적 오브젝트에만 베이크 그림자를 쓰는데, 소형 맵이라 대부분이 거리 안에 있어 효과가 없었다(추정).
- **그림자 거리 50→35**: Shadow casters는 줄었지만 Batches/Tris가 오히려 증가. 원인 미확정.
- **좀비 그림자(G2)**: 드로우콜은 줄었지만 프레임 시간 이득이 없다. 좀비 메시가 작아(14개 합쳐 약 3.5k 버텍스) 병목이 아니었다. 팔 그림자가 사라지는 화질 손실만 남아 미적용.
- **C1~C3 코드**: 스크립트 Update 합계가 0.12→0.13ms로 변화 없음. 모든 스크립트 Update의 합이 프레임(8~10ms)의 약 1.5%라 개선 여지 자체가 작았다. GC 지표는 에디터/샘플러 자체 할당(약 16KB/프레임)이 바닥이라 문자열 몇 개 차이는 가려진다. 코드 품질·안전성 측면의 개선일 뿐 성능 증거는 없다. (`ENABLE_LOG`가 켜져 있어 로그 제거 효과도 측정에 반영되지 않았다.)
- **R2 normal 타입 수정**: 올바른 타입(DXT5nm)으로 바꾸면 메모리가 2배(+10.7MB)가 된다. 정확성과 메모리의 트레이드오프라 해상도를 1024로 낮춰 타협했다.
- **Streaming Mipmaps / FBX 임포트 옵션**: 효과 없음. 맵이 작고 예산(512MB)이 커서 모든 밉이 필요했고, FBX에는 클립·블렌드셰이프가 없었다. 원복.
- **GPU 이득이 FPS로 이어지지 않음**: 이 환경은 CPU 병목이라 GPU −19%가 FPS에는 거의 반영되지 않는다. 더 약한 GPU나 빌드에서 재측정하면 달라질 수 있다(추정).

### 4.3 AI 분석의 오류와 보정
- **렌더링 경로 오독**: AI가 `PC_Renderer.asset`의 `m_RenderingMode: 2`를 Deferred로 읽어 "Deferred → Forward+ 실험(G4)"을 계획했다. 실제로는 이미 Forward+였다. 측정 중 런타임 값을 확인하고 바로잡았다. 파일 값만 보고 enum을 해석할 때는 반드시 실행 중 값으로 교차 확인해야 한다는 교훈.
- **텍스처 절감 예상치**: AI는 4개 텍스처 축소로 약 −14MB를 예상했지만 실측은 −7.0MB였다.
- **측정 방식 문제**: 처음의 "좀비가 움직이는 구간" 측정은 좀비 위치에 따라 Batches가 395~490으로 흔들려 조명 비교에 쓸 수 없었고, 좀비를 고정하는 방식으로 바꿨다.

### 4.4 에디터 측정의 한계
- 에디터 오버헤드와 세션 중 프레임 시간 드리프트(7.7→10.5ms)가 있어 0.5ms 이내 차이는 판단하지 않았다. GPU ms/Batches/SetPass는 드리프트와 무관하게 안정적이었다.
- 좀비 고정 중 `NavMeshAgent`를 꺼서 콘솔에 `SetDestination` 에러가 쌓였다(측정 방식의 부작용, 게임 버그 아님).
- Development Build 값, Frame Debugger, Profiler 캡처는 이번에 수행하지 못했다. [본인 작성: 보강 여부]

### 4.5 후속 과제 (미수행)
- 플레이어/검 FBX 메시가 34.6만·30.1만 버텍스, 합계 약 103MB로 매우 크다. 가장 큰 RAM 항목이므로 리메시/LOD/버텍스 정리를 검토할 만하다.
- Deferred가 GPU −0.11ms, Batches −147로 약간 유리했지만 화질/호환성 비교는 하지 않았다.
- C4(박스 파괴 시 `CreatePrimitive` 27개 → 풀링)는 수행하지 않았다.

## 5. 느낀 점
[본인 작성] — 예시 질문: 가장 효과가 컸던 항목은 무엇이었나(미니맵 그림자)? AI의 제안 중 실측과 달랐던 것은(렌더링 경로 오독, 코드 최적화 효과 없음)? 품질을 얼마나 양보해서 얼마의 성능을 얻었나? "측정하지 않고 적용했다면 어떻게 됐을까?"
