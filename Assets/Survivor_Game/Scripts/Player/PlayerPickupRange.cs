using UnityEngine;

public class PlayerPickupRange : MonoBehaviour
{
    [SerializeField]
    [Min(0.1f)]
    private float pickupRange = 2.5f;

    private CircleCollider2D pickupCollider;

    public float GetPickupRange()
    {
        return pickupRange;
    }

    public void IncreasePickupRange(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetPickupRange(pickupRange + amount);
    }

    public void SetPickupRange(float newRange)
    {
        pickupRange = Mathf.Max(0.1f, newRange);
        UpdateColliderRadius();
    }

    private void Awake()
    {
        GameObject rangeObject = new GameObject("PickupRange");
        rangeObject.transform.SetParent(transform, false);

        pickupCollider = rangeObject.AddComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        UpdateColliderRadius();
    }

    private void OnValidate()
    {
        pickupRange = Mathf.Max(0.1f, pickupRange);

        if (pickupCollider != null)
        {
            UpdateColliderRadius();
        }
    }

    private void UpdateColliderRadius()
    {
        if (pickupCollider == null)
        {
            return;
        }

        float largestScale = Mathf.Max(
            Mathf.Abs(pickupCollider.transform.lossyScale.x),
            Mathf.Abs(pickupCollider.transform.lossyScale.y)
        );

        pickupCollider.radius = pickupRange / Mathf.Max(largestScale, 0.001f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
