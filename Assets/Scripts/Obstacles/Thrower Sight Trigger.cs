using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ThrowerSightTrigger : MonoBehaviour
{
    [SerializeField] private ThrowerObstacle owner;

    public void Initialize(ThrowerObstacle throwerOwner)
    {
        owner = throwerOwner;
    }

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<ThrowerObstacle>();
        }

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null)
        {
            return;
        }

        StoneController stone = other.GetComponentInParent<StoneController>();
        if (stone != null)
        {
            owner.RegisterSightTarget(stone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner == null)
        {
            return;
        }

        StoneController stone = other.GetComponentInParent<StoneController>();
        if (stone != null)
        {
            owner.UnregisterSightTarget(stone);
        }
    }
}
