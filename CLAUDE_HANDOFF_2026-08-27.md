# Claude 작업 인수인계 프롬프트 — TopDownSurvivor

이 문서 전체를 작업 지침이자 현재 상태의 스냅샷으로 취급하라. 사용자가 별도 지시를 하지 않았다면 아래 우선순위대로 저장소와 Unity Editor를 직접 검사한 뒤 작업을 이어간다. 문서의 설명과 실제 상태가 다르면 실제 파일, Unity 직렬화 상태, Console 로그를 우선하되 차이를 사용자에게 짧게 보고한다.

## 0. 첫 응답에서 수행할 일

다음 순서를 지켜라.

1. `U:\Unity\TopDownSurvivor`를 작업 루트로 사용한다.
2. Git 상태, 현재 브랜치, 최근 커밋, Unity 버전, `Packages/manifest.json`을 읽기 전용으로 확인한다.
3. 아래에 기록된 미커밋 파일을 절대 폐기하거나 되돌리지 않는다. 다른 작업자/사용자의 변경으로 간주한다.
4. Unity MCP가 연결되어 있다면 Console 오류부터 읽고, 현재 열린 프로젝트가 반드시 `TopDownSurvivor`인지 확인한다.
5. 가장 먼저 "무기 VFX의 MaterialPropertyBlock 초기화 오류"를 해결하고 컴파일/런타임을 검증한다.
6. 검증 뒤 현재 무기 궤적·속도 기반 블러·잔상 작업을 완성한다.
7. 사용자가 명시적으로 요청하기 전에는 커밋, 푸시, 브랜치 변경을 하지 않는다.

첫 응답은 장황한 계획 대신 "현재 상태를 먼저 검사하고, 미커밋 작업을 보존한 채 확인된 VFX 오류부터 해결하겠다"는 취지로 짧게 말한 뒤 실제 검사를 시작한다.

## 1. 프로젝트 환경

- 프로젝트: `U:\Unity\TopDownSurvivor`
- Unity: `6000.3.20f1`
- 렌더 파이프라인: URP `17.3.0`, 2D 중심 프로젝트
- 입력: Input System 사용 (`activeInputHandler: 1`)
- 색 공간: Linear (`m_ActiveColorSpace: 1`)
- 기본 해상도: 1920×1080
- 주요 게임 씬: `Assets/Survivor_Game/Scenes/Game.unity`
- 애니메이션 테스트 씬: `Assets/Survivor_Game/Scenes/CharacterAnimationTest.unity`
- 현재 Build Settings에는 `SampleScene.unity`만 등록되어 있다. 의도된 상태인지 사용자에게 확인하기 전에는 임의 변경하지 말고, 빌드 작업 전 반드시 점검한다.
- Unity AI 패키지: `com.unity.ai.assistant 2.16.0-pre.1`
- 원격 저장소: `origin https://github.com/und000/VSopen.git`
- 브랜치: `master`
- 기준 HEAD: `b138ea94d6259f43c7aaf0d83cc098ced76f7f15` (`Add reusable enemy projectile patterns`)
- 인수인계 작성 직전 `master`는 `origin/master`와 같은 커밋을 가리켰다. 이후 상태는 다시 확인한다.

이 환경에서는 Git이 `dubious ownership`을 보고할 수 있다. 전역 설정을 함부로 바꾸지 말고 조회/작업 명령에 필요하면 다음처럼 저장소 한정 옵션을 사용한다.

```powershell
git -c safe.directory=U:/Unity/TopDownSurvivor status --short
```

## 2. Unity MCP 연결 지침

이 프로젝트에는 Unity MCP 브리지가 이미 포함되어 있다. 별도의 `com.coplaydev.unity-mcp` 패키지를 중복 설치하지 않는다. 다른 Unity 프로젝트 로그에 CoplayDev MCP가 보일 수 있으나 이 프로젝트의 구성으로 오인하지 않는다.

- Windows 릴레이: `C:\Users\NJ\.unity\relay\relay_win.exe`
- 필수 인수: `--mcp`
- 이 프로젝트를 정확히 지정할 인수: `--project-path U:\Unity\TopDownSurvivor`
- Unity 위치: `Edit > Project Settings > AI > Unity MCP Server`
- 브리지가 Stopped이면 Start, 최초 Claude 연결이 Pending이면 사용자가 Allow 해야 한다.
- Claude Code/Claude Desktop 설정은 Unity MCP Server의 `Integrations > Configure`를 우선 사용한다.
- 연결 후 `Unity_ReadConsole`, 씬/프리팹/에셋 관리 도구가 보이는지 확인한다.
- 이 패키지 버전에서는 Canvas, CanvasScaler, GraphicRaycaster, RectTransform이 있는 UI 계층에 무분별하게 `get_components`를 호출하면 Editor가 멈출 가능성이 문서화되어 있다. UI는 계층/직렬화 파일/개별 안전 컴포넌트 중심으로 검사한다.

Codex용 전역 MCP 항목은 이미 `unity-mcp`라는 이름으로 등록했지만 Claude 설정과는 별개다.

## 3. 절대 보존해야 하는 현재 워킹 트리

인수인계 작성 직전 다음 변경이 미커밋 상태였다. 아래 목록과 실제 `git status`를 다시 비교한다. 이 문서 자체도 새 미추적 파일로 보일 수 있다.

### 수정된 추적 파일

- `Assets/Survivor_Game/Animations/Weapons/Weapon1/Weapon1_Attack.anim`
- `Assets/Survivor_Game/Animations/Weapons/Weapon1/Weapon1_Attack2.anim`
- `Assets/Survivor_Game/Animations/Weapons/Weapon1/Weapon1_Idle.anim`
- `Assets/Survivor_Game/Data/Weapons/WeaponStats_Weapon1.asset`
- `Assets/Survivor_Game/Prefabs/Player/Weapon/Weapon1.prefab`
- `Assets/Survivor_Game/Scenes/CharacterAnimationTest.unity`
- `Assets/Survivor_Game/Scenes/Game.unity`
- `Assets/Survivor_Game/Scripts/Combat/MeleeWeaponAttack.cs`
- `Assets/Survivor_Game/Scripts/Combat/WeaponStatsProfile.cs`
- `Assets/Survivor_Game/Scripts/Player/PlayerWeaponEquipment.cs`
- `ProjectSettings/ProjectSettings.asset`

### 새 미추적 작업

- `Assets/Survivor_Game/Materials/VFX/Weapon/` 전체와 폴더 meta
- `Assets/Survivor_Game/Prefabs/VFX/Weapon/` 전체와 폴더 meta
- `Assets/Survivor_Game/Resources/1_Player/Sample/` 전체와 폴더 meta
- `Assets/Survivor_Game/Resources/5_Weapon/` 전체와 폴더 meta
- `Assets/Survivor_Game/Scripts/VFX/SpriteFractureDissolveVFX.cs(.meta)`
- `Assets/Survivor_Game/Scripts/VFX/WeaponSwingAfterimage.cs(.meta)`
- `Assets/Survivor_Game/Scripts/VFX/WeaponSwingVFX.cs(.meta)`
- `Assets/Survivor_Game/Shaders/` 전체와 meta
- `Assets/Survivor_Game/VFX/Textures/` 전체와 meta

특히 `.anim`, 두 씬, `ProjectSettings.asset`, `Resources/1_Player/Sample`은 사용자의 Unity 편집 작업이 크게 섞여 있다. 광범위 재작성, 자동 포맷, checkout/reset, prefab 재생성으로 덮어쓰지 않는다. 사용자는 Player 쪽을 다른 분기/작업에서도 계속 수정할 수 있다고 명시했다. Player 관련 수정은 필요한 최소 줄만 바꾸고 충돌 가능성을 먼저 확인한다.

## 4. 현재 가장 최근의 미완성 작업

사용자가 원하는 것은 검을 휘두를 때 무기마다 이미지를 일일이 그리지 않고 재사용 가능한 셰이더 기반 궤적을 만드는 것이다. 이동량과 속도에 따라 방향성 블러 및 잔상이 생겨야 한다. 나중에 사용하지 않으면 쉽게 끌 수 있고, 다른 무기나 스킬에도 재사용 가능해야 한다.

현재 구현 방향은 다음과 같다.

### 자연스러운 Idle 복귀

- `WeaponStatsProfile.idleReturnBlendDuration`
- `MeleeWeaponAttack.PlayIdle(bool immediate = false)`
- 공격 종료 시 `Animator.CrossFadeInFixedTime`으로 Idle 복귀
- Disable/강제 취소 시에는 즉시 Idle 처리 가능
- Weapon1 기본값은 0.1초

이 변경은 아직 최종 플레이 검증이 필요하다. 콤보 연속 입력 중 불필요하게 Idle을 거치지 않는지, 공격/스킬 취소 시 상태가 남지 않는지 확인한다.

### 절차적 검 궤적

- 핵심 스크립트: `Assets/Survivor_Game/Scripts/VFX/WeaponSwingVFX.cs`
- 잔상: `Assets/Survivor_Game/Scripts/VFX/WeaponSwingAfterimage.cs`
- 셰이더: `Assets/Survivor_Game/Shaders/VFX/WeaponSwingArc.shader`
- 머티리얼: `Assets/Survivor_Game/Materials/VFX/Weapon/M_WeaponSwingArc.mat`
- 프리팹:
  - `Assets/Survivor_Game/Prefabs/VFX/Weapon/VFX_WeaponSwingArc.prefab`
  - `Assets/Survivor_Game/Prefabs/VFX/Weapon/VFX_WeaponSwingAfterimage.prefab`
- 풀링: 기존 `PrefabPool`과 `IPrefabPoolLifecycle` 재사용
- 설정 위치: `WeaponStatsProfile`의 공용 VFX 프리팹 + 각 `WeaponAttackStep.swingVfx`
- 공격 연결: `MeleeWeaponAttack`가 공격 애니메이션 시작 시 VFX를 풀에서 생성
- 무기 팁 속도 측정: `Weapon1.prefab`의 `VFXTip` Transform을 `swingVfxMotionPoint`로 사용
- Weapon1 프로필의 Attack1/Attack2에 원호 크기, 각도, 두께, 색, 재생 시간, 속도 범위, 잔상 주기/수명 등을 저장
- Attack2는 `reverse`를 켠 상태

`WeaponSwingArc.shader`의 현재 블러는 진짜 2D Gaussian kernel이 아니라 이동 방향으로 4회 추가 샘플하고 가중치를 적용한 저비용 방향성 블러 근사다. 사용자가 말한 "가우시안 느낌"과 실제 화면을 비교해 필요하면 샘플/가중치를 개선하되, 모바일/다수 이펙트 비용을 고려한다.

기능 비활성화는 최소한 다음 방식이 가능해야 한다.

- 공격 단계별 `swingVfx.enabled = false`
- 프로필의 VFX prefab 참조를 비움
- `useAfterimages = false`로 잔상만 끔

별도 무기 전용 코드 복사 없이 다른 무기 프로필에서 같은 VFX 프리팹과 서로 다른 설정을 재사용하는 구조를 유지한다.

### 무기 스프라이트 디졸브 실험

- `SpriteFractureDissolveVFX.cs`
- `SpriteFractureDissolve.shader`
- `M_SpriteFractureDissolve.mat`
- `WeaponStatsProfile.WeaponDissolveSettings`
- `PlayerWeaponEquipment`가 장착 무기 안의 `SpriteFractureDissolveVFX`에 프로필 설정을 전달
- 관련 노이즈 텍스처가 `Assets/Survivor_Game/VFX/Textures/Weapon/`에 여러 개 있음

이 기능은 궤적보다 실험 성격이 강하며 최종 연결 상태를 반드시 Unity에서 확인한다. `SpriteFlame`, `SpriteDirectionalFade` 셰이더/머티리얼도 같은 작업 중 생성되었지만 전부 최종 기능에 연결됐다고 가정하지 않는다. 미사용 에셋을 즉시 삭제하지 말고 참조 검색과 사용자 확인 후 정리한다.

## 5. 확인된 최우선 오류

TopDownSurvivor가 열린 직후 Editor 로그에서 다음 UnityException 3개가 확인됐다.

- `SpriteFractureDissolveVFX`: MonoBehaviour 필드 초기화에서 `new MaterialPropertyBlock()` 호출
- `WeaponSwingVFX`: 직렬화 중 필드 초기화에서 `new MaterialPropertyBlock()` 호출
- `WeaponSwingAfterimage`: MonoBehaviour 필드 초기화에서 `new MaterialPropertyBlock()` 호출

Unity 메시지는 `CreateImpl is not allowed to be called from a MonoBehaviour constructor (or instance field initializer)`다. 세 클래스의 `MaterialPropertyBlock`을 필드 선언 시 생성하지 말고 `Awake`, `OnEnable` 또는 안전한 lazy getter에서 생성하도록 바꾼다. 필요하면 `readonly`를 제거한다. 풀 반환 시 `Renderer.SetPropertyBlock(null)` 정리는 유지한다.

수정 후 다음을 확인한다.

1. 스크립트 리컴파일 후 위 UnityException이 재발하지 않는다.
2. 새 C# 컴파일 오류와 셰이더 오류가 없다.
3. Weapon1 장착/공격/콤보/비활성화/재활성화 시 VFX가 풀에서 정상 재사용된다.
4. VFX가 자연 종료된 뒤 `MeleeWeaponAttack.activeSwingVfx`가 오래된 pooled instance를 계속 가리키는 위험을 검토한다. 오래된 참조가 나중에 재사용된 인스턴스를 잘못 Release할 수 있다면 소유권 토큰, 완료 콜백, 명시적 참조 해제 중 가장 단순한 안전책을 넣는다.
5. 시간 감속/정지 중 VFX가 `Time.deltaTime` 기준으로 멈추는 것이 의도와 맞는지 확인한다. 현재 기본 의도는 게임 시간의 영향을 받는 것이다.

Editor.log의 후반부에 `Assets/TutorialInfo`, `Assets/UiDissolve.shader` 오류가 보일 수 있으나 이 저장소에는 해당 경로가 없다. 다른 Unity 프로젝트가 같은 로그 파일을 이어 쓴 흔적으로 판단되므로 TopDownSurvivor 오류로 잘못 수정하지 않는다. Unity MCP에서 현재 프로젝트 Console을 직접 읽어 구분한다.

## 6. 사용자 선호와 구조적 규칙

다음은 기능 구현 시 지켜야 할 장기 규칙이다.

- 런타임 코드로 텍스처, SpriteRenderer, Animator, Collider, 임시 리소스를 생성해 프리팹을 채우지 않는다.
- 교체 가능한 시각 요소는 미리 완성된 프리팹으로 만들고, 스크립트는 프리팹을 생성/재생만 한다.
- 프로필은 가능하면 "어떤 프리팹을 쓸지"와 게임플레이/연출 파라미터를 보관한다. 프리팹 내부 컴포넌트 구조는 Editor에서 눈으로 수정 가능해야 한다.
- 프리팹의 텍스처/렌더러는 루트가 아니라 `Render` 같은 자식 오브젝트 아래 둔다.
- 투사체 구조는 `루트(스크립트) > Render(Animator 선택) > Sprite`를 따른다.
- 적 투사체 이름은 `Projectile_Enemy번호`; 같은 적이 여러 종류를 쓰면 `_1`, `_2`를 붙인다.
- 보스는 100번대. 현재 임시 보스는 `Enemy101`이다.
- 표시 전용 UI는 사용자 입력을 받지 않도록 `DisplayOnlyUI` 패턴을 재사용한다.
- 일반/엘리트/보스 체력바는 각각 프리팹을 사용하고 공용 Canvas/Manager에서 적 위치를 추적한다.
- 체력 감소 흰색 잔여 게이지는 즉시 사라지지 않고 현재 체력 방향으로 천천히 줄어든다. 향후 회복 자원으로 사용할 가능성이 있다.
- 레벨업/텔레포트 등 원형 충격파는 새 시스템을 복제하지 말고 `RadialImpactVisual`을 재사용한다. 판정 시간과 시각 수명은 분리한다.
- 사용자가 기존 기능과 중복되는 새 구조를 요청하면 구현 전에 기존 대체 기능을 알려준다.
- 일반 몬스터는 스크립트 기반 패턴, 엘리트/보스도 모듈형 스크립트/프로필 조합을 우선한다. Behavior Tree는 사용하지 않기로 결정했다.
- 밸런싱은 현재 ScriptableObject/Inspector 기반을 유지하고 CSV 전환은 나중에 한다.
- Inspector에서 조절하기로 한 값은 하드코딩하지 않는다. 한국어 Header/Tooltip을 이미 널리 사용 중이므로 일관성을 유지하고 깨진 문자가 생기지 않게 UTF-8을 보존한다.
- Unity `.meta` 파일과 GUID를 반드시 보존한다. 에셋 이동/생성은 가능하면 Unity Editor/MCP로 수행한다.
- 씬/프리팹 YAML을 직접 수정할 때는 최소 변경만 하고 Unity 재임포트로 직렬화를 검증한다.

## 7. 이미 구현된 핵심 시스템 지도

새 기능을 만들기 전에 아래 시스템을 검색해 재사용한다.

### 전투/무기

- `PlayerCombatStats`: 기본 공격력 + 무기 + 장비, 레벨별 복리 성장, 주는 피해 증가, 방어 관통, 고정 관통, 방어 무시 피해
- 기본 공격력 스케일은 10배 기준이며 `characterBaseAttack` 기본값은 20
- 기본 레벨 공격 성장률 3%, 특정 레벨 구간 override 가능
- 정상 피해 계산은 현재 `baseAttack * (levelGrowthMultiplier + damageIncreaseRate + conditionalRate)` 형태
- 방어 무시 피해는 레벨/주는 피해 배율을 받지 않고 `baseAttack * trueDamageRate`
- `WeaponStatsProfile`: 무기 기본 피해, 공격 속도/피해 배율, Idle, 기본 공격 단계, 다중 hit window, 콤보 입력 구간, 스킬 목록, VFX 설정
- `MeleeWeaponAttack`: 콤보, 입력 버퍼, 다중 타격 판정, knockback, aim lock/tracking, 애니메이션
- `WeaponAimController`: 공격 중 방향 고정 또는 최대 회전 속도 제한 추적
- `WeaponSkillController`, `WeaponSkillProfile`: 향후 무기 스킬 확장점
- `PlayerWeaponEquipment`: 장착 정보로 무기 프리팹을 Upperbody 아래 생성. 기본 무기는 Weapon1

### 플레이어

- `PlayerLevel`, `PlayerLevelUI`, `LevelUpUI`
- 레벨업 시 팝업이 닫힌 후 `PlayerLevelShockwave`; 0 피해 + 강한 넉백
- 레벨업 진입 시 설정 시간 동안 감속, 최저 timeScale 조절
- `PlayerHealth`: 레벨업마다 최대 체력 복리 증가 후 최대치까지 완전 회복
- 체력 성장률: 첫 레벨업 10%, 레벨업마다 0.2%p 감소, 최저 1%
- `PlayerDodge`: Space 회피, 무적/적 충돌 무시, 월드 장애물은 통과하지 않음
- `PlayerTeleportSkill`: 우클릭 조준, 마우스/카메라, 시간 감속 후 복귀, 순간이동/무적/충격파/쿨다운 플래시
- `TeleportVisualProfile`과 텔레포트 VFX 프리팹들
- `CameraFollow`: 평상시 플레이어를 부드럽게 추적; 최근 커밋에서 일반 추적 지연 조정됨
- `UpperbodyCursorFacing`, `PlayerLowerbodyFacing`
- `PlayerPickupRange`, `ExperienceGemAttraction`: 보석은 즉시 삭제가 아니라 중심으로 빨려들며 축소/연출 후 습득

### 적/보스/투사체

- `EnemyProfile`, `EnemyHealth`, `EnemyMovement`, `EnemyAwareness`
- `EnemyHitEffect`: 피격 흰색 flash와 shake, Inspector 조절
- `EnemyKnockback`: 넉백과 넉백 후 이동 정지
- `EnemyPhysicsSetup`: 적끼리 밀침/떨림 완화 구조
- `EnemyLifecycleVisual`: 카메라 안에서 스폰된 적만 등장 연출, 사망 연출
- Enemy1/2/3 이동 패턴은 각각 모듈형 스크립트
- `EnemyPatternController`, `EnemyPhaseController`, `EnemyAttackPattern`, `EnemyProjectileAttackPattern`
- 투사체 후속 발사/분열은 `EnemyProjectileEmissionScheduler`, `EnemyProjectileSpawnOnFinish` 재사용
- 임시 보스 `Enemy101`, 전용 투사체 `Projectile_Enemy101`
- 보스 체력바와 오프스크린 마커가 구현됨

### 스폰/거리 최적화

- `EnemySpawner`는 실제 AddComponent 메뉴에서 `Spawn Director`로 보임
- `SpawnZone`별 `EnemySpawnTable`
- 소환 모드: `WeightedRandom`, `FixedBatch`, `Sequence`
- `PrefabPool`, `PooledEnemy`, `IEnemyPoolLifecycle`
- `EnemyActivationAgent`: 먼 pooled 적의 AI 비활성화/더 멀면 풀 반환
- `ScenePlacedEnemy`, `ScenePlacedEnemyManager`: 직접 배치 적 거리 비활성화
- 스폰 위치는 현재 Zone 내부, 플레이어 최소 거리 밖, 카메라 밖, 차단 레이어/맵 bounds 검사

### UI/VFX

- `EnemyHealthBarManager`, `HealthBarView`, `EnemyHealthBar`
- `EnemyOffscreenMarkerManager/Target/View`: 일반/보스 프리팹, 방향 회전, 거리별 크기/알파
- `DisplayOnlyUI`
- `GameFontManager`: Nanum Myeongjo 전역 적용
- `RadialImpactVisual`: Expanding/Instant/DelayedInstant, 판정과 시각 수명 분리, Editor 범위 preview
- `ProjectileImpactVisual`: 투사체 충돌 연출 프리팹

## 8. 데이터/프리팹 명명과 현재 주요 에셋

- 적 프로필: `Assets/Survivor_Game/Data/Enemies/EnemyProfile_Enemy*.asset`
- 적 프리팹: `Assets/Survivor_Game/Prefabs/Enemies/Enemy*.prefab`
- 기본 스폰 테이블: `Assets/Survivor_Game/Data/Spawning/SpawnTable_Default.asset`
- 체력바: `UI_HealthBar_Normal`, `UI_HealthBar_Elite`, `UI_HealthBar_Boss`
- 마커: `UI_EnemyPos_Normal`, `UI_EnemyPos_Boss`
- 텔레포트 VFX: `Assets/Survivor_Game/Prefabs/VFX/Teleport/`
- 레벨업 충격파: `VFX_LevelUp_Shockwave.prefab`
- Weapon1 데이터: `Assets/Survivor_Game/Data/Weapons/WeaponStats_Weapon1.asset`
- Weapon1 프리팹: `Assets/Survivor_Game/Prefabs/Player/Weapon/Weapon1.prefab`

## 9. 현재 작업의 완료 조건

다음을 모두 만족하기 전에는 무기 모션블러/VFX 작업을 완료했다고 보고하지 않는다.

1. 현재 TopDownSurvivor Console에 C# 컴파일 오류, 새 UnityException, 해당 셰이더 오류가 없다.
2. `MaterialPropertyBlock`이 MonoBehaviour 필드 초기화/직렬화 중 생성되지 않는다.
3. Game 씬 또는 CharacterAnimationTest 씬에서 Weapon1 Attack1/Attack2가 정상 재생된다.
4. 공격 종료 후 Idle로 부드럽게 복귀하고 콤보 중 불필요한 Idle 끊김이 없다.
5. 원호가 무기/Upperbody 방향을 제대로 따르고 Attack2 reverse가 의도대로 보인다.
6. 속도가 낮을 때 블러/잔상이 약하고 빠를 때 강해지며 Inspector 값 변화가 즉시 이해 가능하다.
7. 잔상과 원호가 풀링 후 잘못된 Transform, MaterialPropertyBlock, 활성 상태를 유지하지 않는다.
8. VFX를 끈 경우 공격 판정/애니메이션이 그대로 작동한다.
9. 다른 무기 프로필에서도 프리팹 재사용이 가능한 구조다.
10. 수정한 씬/프리팹/ScriptableObject 참조에 Missing이 없다.

검증 결과는 "무엇을 고쳤는지 / Unity에서 무엇을 실제로 확인했는지 / 남은 수동 튜닝 값"으로 나눠 사용자에게 짧게 보고한다.

## 10. 이후 사용자가 새 기능을 요청할 때 사용할 판단 프롬프트

새 구현 전에 스스로 다음을 확인한다.

> 이 요청은 이미 존재하는 `RadialImpactVisual`, `PrefabPool`, `EnemyProjectileEmissionScheduler`, `EnemyProjectileSpawnOnFinish`, `EnemyHealthBarManager`, `EnemyOffscreenMarkerManager`, `WeaponStatsProfile`, `WeaponSkillProfile`, `TeleportVisualProfile` 중 하나로 구성할 수 있는가? 가능하면 새 프레임워크를 만들지 말고 기존 시스템을 확장하라. 구조 중복 가능성이 있으면 사용자에게 기존 대안을 먼저 알리고 진행하라.

## 11. Git 마무리 규칙

- 사용자의 명시적 요청 전에는 commit/push 금지.
- 커밋 요청을 받으면 먼저 실제 diff와 Unity Console을 확인한다.
- 사용자 씬/애니메이션 변경과 작업 변경이 섞여 있어도 임의로 빼거나 되돌리지 않는다. 범위가 불명확하면 파일별 변경 목적을 정리해서 사용자에게 확인한다.
- `Library`, `Temp`, `Logs`, `UserSettings`를 커밋하지 않는다.
- 새 Unity 에셋은 대응하는 `.meta`와 함께 포함한다.
- 푸시 전 `origin/master`와의 관계를 다시 확인한다. 강제 푸시는 하지 않는다.

---

이 문서는 2026-08-27 기준이다. 반드시 실제 저장소와 Unity Editor 상태를 다시 읽고 이어서 작업하라.
