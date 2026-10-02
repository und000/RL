using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMovementVFX : MonoBehaviour
{
    [Header("Character image only (exclude weapon)")]
    [SerializeField] private SpriteRenderer characterImage;
    [Header("Dash afterimages")]
    [SerializeField] private MovementVfxStamp afterimagePrefab;
    [SerializeField] private Color afterimageColor = new Color(.3f,.65f,1f,.28f);
    [SerializeField, Min(.01f)] private float afterimageInterval = .02f;
    [SerializeField, Min(.02f)] private float afterimageLifetime = .12f;
    [Header("Sprint smoke")]
    [SerializeField] private MovementVfxStamp smokePrefab;
    [SerializeField] private Color smokeColor = new Color(.8f,.87f,.95f,.55f);
    [SerializeField, Min(.02f)] private float smokeInterval = .075f;
    [SerializeField, Min(.02f)] private float smokeLifetime = .32f;
    [SerializeField, Min(.01f)] private float smokeSize = .5f;
    [SerializeField] private Vector2 footOffset = new Vector2(0,.05f);
    private PlayerDodge dodge;
    private PlayerMovement movement;
    private Vector3 lastPosition;
    private float nextGhost, nextSmoke;

    private void Awake()
    {
        dodge = GetComponent<PlayerDodge>();
        movement = GetComponent<PlayerMovement>();
    }
    private void OnEnable() { lastPosition = transform.position; nextGhost = nextSmoke = 0; }
    private void LateUpdate()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        if (Time.deltaTime <= 0 || characterImage == null || !characterImage.enabled ||
            !characterImage.gameObject.activeInHierarchy || movement == null || !movement.enabled) return;
        if (dodge != null && dodge.IsDodgeMoving)
        {
            if (Time.time >= nextGhost && afterimagePrefab != null)
            {
                nextGhost = Time.time + afterimageInterval;
                var go = PrefabPool.Spawn(afterimagePrefab.gameObject, characterImage.transform.position, characterImage.transform.rotation);
                go.GetComponent<MovementVfxStamp>().Play(characterImage.sprite, characterImage.transform.lossyScale,
                    afterimageColor, afterimageLifetime, 1, Vector3.zero, characterImage.sortingLayerID,
                    characterImage.sortingOrder - 1, characterImage.flipX, characterImage.flipY);
            }
            return;
        }
        nextGhost = 0;
        if (!movement.IsSprinting || delta.sqrMagnitude < .000001f) { nextSmoke = 0; return; }
        if (Time.time < nextSmoke || smokePrefab == null) return;
        nextSmoke = Time.time + smokeInterval;
        Vector3 feet = new Vector3(characterImage.bounds.center.x, characterImage.bounds.min.y, transform.position.z);
        Vector3 direction = delta.normalized;
        for (int i = 0; i < 3; i++)
        {
            Vector3 scatter = (Vector3)Random.insideUnitCircle * .12f;
            var go = PrefabPool.Spawn(smokePrefab.gameObject, feet + (Vector3)footOffset + scatter, Quaternion.Euler(0,0,Random.Range(0,360)));
            go.GetComponent<MovementVfxStamp>().Play(null, Vector3.one * smokeSize * Random.Range(.8f,1.2f),
                smokeColor, smokeLifetime, 2.2f, -direction * .5f + Vector3.up * .3f,
                characterImage.sortingLayerID, characterImage.sortingOrder - 1, false, false);
        }
    }
}
