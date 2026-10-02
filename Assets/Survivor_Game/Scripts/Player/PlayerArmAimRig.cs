using System;
using UnityEngine;

/// <summary>
/// 어깨 위치는 고정한 채 팔만 회전시키고, 무기 소켓은 커서 각도에 따라
/// 가슴 중심 주변을 타원 궤도로 이동한다.
/// 팔은 커서가 아니라 "무기 그립"을 조준하므로 공격 애니메이션과 조준 추적이
/// 그대로 팔에 반영된다. 그래서 무기 쪽 갱신(WeaponAimController, Animator)이
/// 끝난 뒤에 실행되어야 한다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
[AddComponentMenu("Player/Player Arm Aim Rig")]
public class PlayerArmAimRig : MonoBehaviour
{
    [Serializable]
    public class AimArm
    {
        [Tooltip("어깨 피벗. localPosition은 수정하지 않고 회전만 준다.")]
        public Transform shoulder;
        [Tooltip("팔꿈치 노드. 지정하면 2본 IK로 팔꿈치를 굽힌다. 비우면 팔 전체가 통짜로 회전한다.")]
        public Transform elbow;
        [Tooltip("손 위치. 2본 IK에서는 elbow의 자식이어야 한다. 팔 길이 측정에 쓴다.")]
        public Transform hand;
        [Tooltip("그립 지점에서 이 손이 잡는 지점까지의 오프셋. 앞손/뒷손을 벌릴 때 쓴다.")]
        public Vector2 gripOffset;
        [Tooltip("0이면 휴식 포즈 유지, 1이면 완전히 그립을 따라간다. 한손 무기의 보조 팔에 쓴다.")]
        [Range(0f, 1f)] public float followWeight = 1f;
        [Tooltip("followWeight가 1 미만일 때 섞을 휴식 각도.")]
        public float restLocalAngle;
        [Tooltip("팔 스프라이트가 늘어진 방향 보정. -Y로 늘어져 있으면 90.")]
        public float angleOffset = 90f;

        [Header("Elbow (2-bone IK)")]
        [Tooltip("팔꿈치가 반대로 꺾이면 뒤집는다.")]
        public bool invertBend;
        [Tooltip("커서가 반대편으로 넘어가면 굽는 방향을 자동으로 뒤집는다.")]
        public bool autoFlipBend = true;

        [NonSerialized] public float upperLength;
        [NonSerialized] public float lowerLength;

        [Header("Depth")]
        public SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        [Tooltip("몸통보다 앞에 그릴 때 몸통 sortingOrder에 더할 값.")]
        public int frontOrderOffset = 1;
        [Tooltip("몸통보다 뒤에 그릴 때 몸통 sortingOrder에 더할 값.")]
        public int backOrderOffset = -1;

        [NonSerialized] public float restLength;
    }

    [Header("References")]
    [SerializeField] private UpperbodyCursorFacing aimSource;
    [Tooltip("어깨와 소켓이 함께 놓이는 로컬 공간. 보통 Upper.")]
    [SerializeField] private Transform upperRoot;
    [Tooltip("무기가 붙는 소켓. PlayerWeaponEquipment의 Weapon Container로도 지정한다.")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private PlayerWeaponEquipment weaponEquipment;
    [Tooltip("스폰된 무기 안에서 팔이 조준할 트랜스폼 경로. 못 찾으면 소켓을 쓴다.")]
    [SerializeField] private string weaponGripPath = "AimRoot";

    [Header("Socket Orbit")]
    [Tooltip("Upper 로컬 기준 가슴/어깨선 중심.")]
    [SerializeField] private Vector2 orbitPivot = new Vector2(0f, 0.725f);
    [SerializeField] private float orbitRadiusX = 0.55f;
    [Tooltip("탑다운 원근 때문에 보통 X보다 작게 잡는다.")]
    [SerializeField] private float orbitRadiusY = 0.28f;
    [SerializeField] private float socketAngleOffset;
    [Tooltip("0이면 즉시 스냅. 초당 각도.")]
    [SerializeField, Min(0f)] private float maximumTurnSpeed;

    [Header("Arms")]
    [SerializeField] private AimArm[] arms = Array.Empty<AimArm>();
    [Tooltip("그립이 팔 길이보다 멀거나 가까울 때 팔을 늘였다 줄인다. 2본 IK 전 임시 대안.")]
    [SerializeField] private bool stretchToReach;
    [SerializeField] private Vector2 stretchRange = new Vector2(0.85f, 1.15f);

    [Header("Upper Flip")]
    [Tooltip("커서가 왼쪽으로 가면 Upper의 X 스케일을 뒤집어 주무기 팔이 항상 가까운 쪽에 오게 한다. " +
             "이게 없으면 반대편 팔이 무기까지 닿지 못한다.")]
    [SerializeField] private bool flipUpperByAim = true;

    [Header("Depth Sorting")]
    [SerializeField] private SpriteRenderer torsoRenderer;
    [Tooltip("비워두면 스폰된 무기의 그립 하위 렌더러를 자동으로 수집한다.")]
    [SerializeField] private SpriteRenderer[] weaponRenderers = Array.Empty<SpriteRenderer>();
    [SerializeField] private int weaponFrontOrderOffset = 2;
    [SerializeField] private int weaponBackOrderOffset = -2;
    [Tooltip("위쪽을 조준하면 팔과 무기를 몸통 뒤로 보낸다.")]
    [SerializeField] private bool flipDepthByAim = true;

    private float currentAngle;
    private bool angleInitialized;
    private bool mirrored;
    private GameObject cachedWeaponInstance;
    private Transform cachedGrip;
    private SpriteRenderer[] runtimeWeaponRenderers = Array.Empty<SpriteRenderer>();

    public float CurrentAimAngle => currentAngle;
    public bool IsMirrored => mirrored;
    public Transform WeaponSocket => weaponSocket;

    /// <summary>소켓 궤도와 조준 각도가 계산되는 공간. 미러링되지 않는다.</summary>
    private Transform SocketSpace =>
        weaponSocket != null && weaponSocket.parent != null ? weaponSocket.parent : upperRoot;

    /// <summary>Upper 로컬 공간 기준 조준 X. 반전 중이면 항상 양수 쪽으로 접힌다.</summary>
    private float LocalAimX
    {
        get
        {
            float cos = Mathf.Cos(currentAngle * Mathf.Deg2Rad);
            return mirrored ? -cos : cos;
        }
    }

    private void Awake()
    {
        if (upperRoot == null && weaponSocket != null) upperRoot = weaponSocket.parent;
        if (aimSource == null) aimSource = GetComponentInParent<UpperbodyCursorFacing>();
        if (weaponEquipment == null) weaponEquipment = GetComponentInParent<PlayerWeaponEquipment>();

        foreach (AimArm arm in arms)
        {
            if (arm == null || arm.shoulder == null || arm.hand == null) continue;

            if (arm.elbow != null)
            {
                // 2본 체인. hand는 elbow의 자식이라 각 뼈 길이를 localPosition으로 잰다.
                arm.upperLength = ((Vector2)arm.elbow.localPosition).magnitude;
                arm.lowerLength = ((Vector2)arm.hand.localPosition).magnitude;
                arm.restLength = arm.upperLength + arm.lowerLength;
                continue;
            }

            float length = ((Vector2)arm.hand.localPosition).magnitude;
            arm.restLength = length > 0.0001f ? length : 0f;
        }
    }

    private void LateUpdate()
    {
        if (upperRoot == null) return;
        if (aimSource == null || !aimSource.HasAimDirection) return;

        UpdateAimAngle();
        UpdateUpperFlip();
        UpdateSocket();
        UpdateArms(ResolveGrip());
        UpdateDepth();
    }

    private void UpdateAimAngle()
    {
        Transform space = SocketSpace;
        if (space == null) return;
        Vector2 localAim = space.InverseTransformDirection(aimSource.AimDirection);
        float targetAngle = Mathf.Atan2(localAim.y, localAim.x) * Mathf.Rad2Deg;

        if (!angleInitialized || maximumTurnSpeed <= 0f)
        {
            currentAngle = targetAngle;
            angleInitialized = true;
            return;
        }

        currentAngle = Mathf.MoveTowardsAngle(
            currentAngle, targetAngle, maximumTurnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 왼쪽을 조준하면 Upper를 좌우 반전한다. 어깨와 그립이 모두 Upper 로컬 공간에서
    /// 거울상이 되므로 반대편 팔이 닿지 못하는 문제가 사라진다.
    /// 무기 소켓은 Upper 바깥(SocketSpace)에 있어서 반전되지 않는다.
    /// </summary>
    private void UpdateUpperFlip()
    {
        mirrored = flipUpperByAim && Mathf.Cos(currentAngle * Mathf.Deg2Rad) < 0f;
        if (!flipUpperByAim || upperRoot == null) return;

        Vector3 scale = upperRoot.localScale;
        float magnitude = Mathf.Abs(scale.x);
        float signed = mirrored ? -magnitude : magnitude;
        if (Mathf.Approximately(scale.x, signed)) return;

        scale.x = signed;
        upperRoot.localScale = scale;
    }

    private void UpdateSocket()
    {
        if (weaponSocket == null) return;

        float radians = currentAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(
            Mathf.Cos(radians) * orbitRadiusX,
            Mathf.Sin(radians) * orbitRadiusY);

        weaponSocket.localPosition = new Vector3(
            orbitPivot.x + offset.x,
            orbitPivot.y + offset.y,
            weaponSocket.localPosition.z);
        weaponSocket.localRotation = Quaternion.Euler(0f, 0f, currentAngle + socketAngleOffset);
    }

    private Transform ResolveGrip()
    {
        GameObject instance = weaponEquipment != null ? weaponEquipment.WeaponInstance : null;
        if (instance != cachedWeaponInstance)
        {
            cachedWeaponInstance = instance;
            cachedGrip = null;
            runtimeWeaponRenderers = Array.Empty<SpriteRenderer>();
            if (instance != null)
            {
                if (!string.IsNullOrWhiteSpace(weaponGripPath))
                {
                    cachedGrip = instance.transform.Find(weaponGripPath);
                }
                if (cachedGrip == null) cachedGrip = instance.transform;
                // 그립 하위만 모은다. 스윙 VFX는 자체적으로 정렬을 관리하므로 건드리지 않는다.
                runtimeWeaponRenderers = cachedGrip.GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        return cachedGrip != null ? cachedGrip : weaponSocket;
    }

    private void UpdateArms(Transform grip)
    {
        if (grip == null) return;

        float aimX = LocalAimX;

        foreach (AimArm arm in arms)
        {
            if (arm == null || arm.shoulder == null) continue;
            Transform shoulderParent = arm.shoulder.parent;
            if (shoulderParent == null) continue;

            Vector3 worldGrip = grip.TransformPoint(arm.gripOffset);
            Vector2 localGrip = shoulderParent.InverseTransformPoint(worldGrip);
            Vector2 toGrip = localGrip - (Vector2)arm.shoulder.localPosition;
            if (toGrip.sqrMagnitude <= 0.000001f) continue;

            if (arm.elbow != null && arm.upperLength > 0.0001f && arm.lowerLength > 0.0001f)
            {
                SolveTwoBone(arm, toGrip, aimX);
                continue;
            }

            float reachAngle = Mathf.Atan2(toGrip.y, toGrip.x) * Mathf.Rad2Deg + arm.angleOffset;
            float finalAngle = arm.followWeight >= 1f
                ? reachAngle
                : Mathf.LerpAngle(arm.restLocalAngle, reachAngle, arm.followWeight);
            arm.shoulder.localRotation = Quaternion.Euler(0f, 0f, finalAngle);

            if (!stretchToReach || arm.restLength <= 0f) continue;
            float ratio = Mathf.Clamp(
                toGrip.magnitude / arm.restLength, stretchRange.x, stretchRange.y);
            float blended = Mathf.Lerp(1f, ratio, arm.followWeight);
            Vector3 scale = arm.shoulder.localScale;
            arm.shoulder.localScale = new Vector3(scale.x, blended, scale.z);
        }
    }

    /// <summary>
    /// 해석적 2본 IK. 삼각형 세 변(upperLength, lowerLength, 어깨-그립 거리)에서
    /// 코사인 법칙으로 어깨 각과 팔꿈치 내각을 직접 구한다. 반복 없이 한 번에 수렴한다.
    /// </summary>
    private void SolveTwoBone(AimArm arm, Vector2 toGrip, float aimX)
    {
        float upper = arm.upperLength;
        float lower = arm.lowerLength;
        // 닿을 수 없는 거리는 클램프한다. 최소 거리는 팔을 접었을 때, 최대는 폈을 때.
        float distance = Mathf.Clamp(
            toGrip.magnitude,
            Mathf.Abs(upper - lower) + 0.0001f,
            upper + lower - 0.0001f);

        float baseAngle = Mathf.Atan2(toGrip.y, toGrip.x);
        float shoulderInner = Mathf.Acos(Mathf.Clamp(
            (upper * upper + distance * distance - lower * lower) / (2f * upper * distance),
            -1f, 1f));
        float elbowInner = Mathf.Acos(Mathf.Clamp(
            (upper * upper + lower * lower - distance * distance) / (2f * upper * lower),
            -1f, 1f));

        float bend = arm.invertBend ? -1f : 1f;
        if (arm.autoFlipBend && aimX < 0f) bend = -bend;

        float shoulderAngle =
            (baseAngle + bend * shoulderInner) * Mathf.Rad2Deg + arm.angleOffset;
        float elbowAngle = -bend * (180f - elbowInner * Mathf.Rad2Deg);

        if (arm.followWeight < 1f)
        {
            shoulderAngle = Mathf.LerpAngle(arm.restLocalAngle, shoulderAngle, arm.followWeight);
            elbowAngle = Mathf.LerpAngle(0f, elbowAngle, arm.followWeight);
        }

        arm.shoulder.localRotation = Quaternion.Euler(0f, 0f, shoulderAngle);
        arm.elbow.localRotation = Quaternion.Euler(0f, 0f, elbowAngle);
    }

    private void UpdateDepth()
    {
        if (!flipDepthByAim || torsoRenderer == null) return;

        int torsoOrder = torsoRenderer.sortingOrder;
        // 위(90도)를 볼수록 몸통 뒤, 아래(-90도)를 볼수록 몸통 앞.
        bool aimingAway = Mathf.Sin(currentAngle * Mathf.Deg2Rad) > 0f;
        float aimX = LocalAimX;

        foreach (AimArm arm in arms)
        {
            if (arm == null || arm.shoulder == null) continue;
            // 커서 반대편 어깨의 팔은 몸통 뒤로 넘긴다.
            bool farSide = arm.shoulder.localPosition.x * aimX < 0f;
            bool behind = aimingAway || farSide;
            ApplyOrder(arm.renderers,
                torsoOrder + (behind ? arm.backOrderOffset : arm.frontOrderOffset));
        }

        SpriteRenderer[] weapon = weaponRenderers != null && weaponRenderers.Length > 0
            ? weaponRenderers
            : runtimeWeaponRenderers;
        ApplyOrder(weapon,
            torsoOrder + (aimingAway ? weaponBackOrderOffset : weaponFrontOrderOffset));
    }

    private static void ApplyOrder(SpriteRenderer[] renderers, int order)
    {
        if (renderers == null) return;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null) renderer.sortingOrder = order;
        }
    }

    private void OnValidate()
    {
        orbitRadiusX = Mathf.Max(0f, orbitRadiusX);
        orbitRadiusY = Mathf.Max(0f, orbitRadiusY);
        maximumTurnSpeed = Mathf.Max(0f, maximumTurnSpeed);
        stretchRange.x = Mathf.Max(0.1f, stretchRange.x);
        stretchRange.y = Mathf.Max(stretchRange.x, stretchRange.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (upperRoot == null) return;

        Gizmos.color = Color.cyan;
        Vector3 previous = Vector3.zero;
        for (int i = 0; i <= 48; i++)
        {
            float radians = i / 48f * Mathf.PI * 2f;
            Vector3 point = upperRoot.TransformPoint(orbitPivot + new Vector2(
                Mathf.Cos(radians) * orbitRadiusX,
                Mathf.Sin(radians) * orbitRadiusY));
            if (i > 0) Gizmos.DrawLine(previous, point);
            previous = point;
        }
        Gizmos.DrawWireSphere(upperRoot.TransformPoint(orbitPivot), 0.03f);
    }
}
