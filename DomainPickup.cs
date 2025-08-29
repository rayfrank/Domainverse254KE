using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class DomainPickup : MonoBehaviour
{
    [Header("Data")]
    public string Domain;

    [Header("Visual")]
    public TMP_Text text;                 // optional; spawner updates all TMPs anyway

    [HideInInspector] public DomainSearchAndSpawn manager;  // set by spawner

    void Awake()
    {
        // ensure physics for trigger pickup
        var col = GetComponent<Collider>();
        if (col == null) { col = gameObject.AddComponent<BoxCollider>(); col.isTrigger = true; }

        var rb = GetComponent<Rigidbody>();
        if (rb == null) { rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false; }
    }

    void Start()
    {
        if (text != null) text.text = Domain; // harmless if also set by spawner
    }

    [System.Obsolete]
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (manager == null) manager = FindObjectOfType<DomainSearchAndSpawn>();
        if (manager != null) manager.AddPickedDomain(Domain);

        Debug.Log("[DomainPickup] Picked: " + Domain, this);
        Destroy(gameObject);
    }
} 