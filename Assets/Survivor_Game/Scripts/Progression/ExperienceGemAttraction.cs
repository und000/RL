using UnityEngine;

public class ExperienceGemAttraction : MonoBehaviour
{
    [Header("흡입 이동")]
    [SerializeField]
    [Min(0f)]
    private float startingSpeed = 4f;

    [SerializeField]
    [Min(0f)]
    private float acceleration = 18f;

    [SerializeField]
    [Min(0f)]
    private float maximumSpeed = 18f;

    [Header("흡입 연출")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float targetScaleMultiplier = 0.2f;

    [SerializeField]
    [Min(0f)]
    private float scaleShrinkSpeed = 2.5f;

    [SerializeField]
    private float rotationSpeed = 720f;

    private Transform attractionTarget;
    private Vector3 targetScale;
    private float currentSpeed;

    private void Awake()
    {
        targetScale = transform.localScale * targetScaleMultiplier;
    }

    private void Update()
    {
        if (attractionTarget == null)
        {
            return;
        }

        currentSpeed = Mathf.Min(
            currentSpeed + acceleration * Time.deltaTime,
            maximumSpeed
        );

        transform.position = Vector3.MoveTowards(
            transform.position,
            attractionTarget.position,
            currentSpeed * Time.deltaTime
        );

        transform.localScale = Vector3.MoveTowards(
            transform.localScale,
            targetScale,
            scaleShrinkSpeed * Time.deltaTime
        );

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (attractionTarget != null)
        {
            return;
        }

        PlayerPickupRange pickupRange =
            other.GetComponentInParent<PlayerPickupRange>();

        if (pickupRange == null)
        {
            return;
        }

        attractionTarget = pickupRange.transform;
        currentSpeed = startingSpeed;
    }
}
