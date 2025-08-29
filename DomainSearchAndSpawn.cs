using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class DomainSearchAndSpawn : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField input;
    public Button searchButton;
    public TextMeshProUGUI statusText;

    [Header("Gameplay")]
    public GameObject domainLabelPrefab; // the prefab to spawn
    public Transform player;             // optional (for facing)

    [Header("Spawn Points")]
    public Transform spawnPointsParent;  // parent whose children are spawn points
    public Transform[] spawnPoints;      // or assign points individually
    public bool randomizePoints = true;
    public bool allowReuseIfOverflow = true;
    public bool clearBetweenSearches = true;
    public bool facePlayer = true;
    public float faceYOffset = 1.5f;

    [Header("Spacing / Overlap")]
    public float samePointRadiusStep = 0.7f;   // horizontal spacing (meters)
    public float samePointVerticalStep = 0.0f; // vertical stacking per item (meters)
    public float samePointJitter = 0.05f;      // tiny random wiggle

    [Header("Picked Domains UI")]
    public TextMeshProUGUI pickedListText;     // panel that shows picked

    [Header("Debug / Testing")]
    public bool bypassAvailabilityForTest = false;
    public bool logVerbose = true;

    [Header("Placement")]
    public bool lockYToSpawnPoint = true;      // <-- keep Y exactly at spawn point

    Transform spawnedRoot;

    // store picked domains
    readonly List<string> pickedDomains = new List<string>();

    void Awake()
    {
        if (searchButton != null)
            searchButton.onClick.AddListener(() => StartCoroutine(RunSearch()));

        RefreshPickedUI();
    }

    [ContextMenu("Spawn Test At Points")]
    void SpawnTestAtPoints()
    {
        var demo = new List<string> { "test1.co.ke", "test2.com", "mybrand.co.ke", "another.org" };
        if (statusText) statusText.text = "Spawning TEST labels at points…";
        SpawnAtPoints(demo);
    }

    IEnumerator RunSearch()
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
        {
            if (statusText) statusText.text = "Type a keyword first.";
            yield break;
        }

        string keyword = input.text.Trim();
        if (statusText) statusText.text = "Searching…";
        if (searchButton) searchButton.interactable = false;

        var candidates = new List<string>
        {
            $"{keyword}.co.ke",
            $"{keyword}.or.ke",
            $"{keyword}.me.ke",
            $"{keyword}.info.ke",
            $"{keyword}.com",
            $"get{keyword}.co.ke",
            $"{keyword}ke.co.ke",
            $"try{keyword}.com"
        };

        var available = new List<string>();

        if (bypassAvailabilityForTest)
        {
            available.AddRange(candidates);
        }
        else
        {
            foreach (var fqdn in candidates)
            {
                bool? isAvailable = null;
                yield return StartCoroutine(CheckAvailability(fqdn, v => isAvailable = v));

                if (logVerbose) Debug.Log($"[RDAP] {fqdn} => {(isAvailable == true ? "AVAILABLE" : "REGISTERED/ERR")}");
                if (isAvailable == true) available.Add(fqdn);

                yield return new WaitForSeconds(0.15f);
            }
        }

        if (available.Count == 0)
        {
            if (statusText) statusText.text = "No available suggestions (or network blocked). Try another word or toggle bypass.";
        }
        else
        {
            if (statusText) statusText.text = $"Found {available.Count} idea(s). Walk into one to pick it.";
            SpawnAtPoints(available);
        }

        if (searchButton) searchButton.interactable = true;
    }

    string BuildRdapUrl(string fqdn)
    {
        string lower = fqdn.ToLowerInvariant();
        if (lower.EndsWith(".ke")) return "https://rdap.kenic.or.ke/domain/" + lower;
        return "https://rdap.org/domain/" + lower;
    }

    IEnumerator CheckAvailability(string fqdn, System.Action<bool> onDone)
    {
        string url = BuildRdapUrl(fqdn);
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onDone(false); // 200 => registered
            else
                onDone((int)req.responseCode == 404); // 404 => likely available
        }
    }

    // ---- Spawn at pre-placed points; updates ALL TMP_Texts and spaces items on same point ----
    void SpawnAtPoints(List<string> domains)
    {
        if (domainLabelPrefab == null)
        {
            Debug.LogError("Assign 'domainLabelPrefab' on DomainSearchAndSpawn.");
            return;
        }

        List<Transform> points = CollectSpawnPoints();
        if (points.Count == 0)
        {
            Debug.LogError("No spawn points found. Assign 'spawnPointsParent' or fill 'spawnPoints'.");
            return;
        }

        if (clearBetweenSearches) ClearSpawned();
        if (randomizePoints) Shuffle(points);
        if (spawnedRoot == null)
        {
            var root = new GameObject("DomainSpawnRoot");
            spawnedRoot = root.transform;
        }

        int spawnCount = domains.Count;
        if (!allowReuseIfOverflow) spawnCount = Mathf.Min(spawnCount, points.Count);
        else if (domains.Count > points.Count && logVerbose)
            Debug.Log($"More domains ({domains.Count}) than points ({points.Count}). Reusing points.");

        // track how many items landed on each point
        var usage = new Dictionary<Transform, int>();

        for (int i = 0; i < spawnCount; i++)
        {
            // reuse only if we truly overflow
            bool needReuse = allowReuseIfOverflow && domains.Count > points.Count;
            Transform pt = needReuse ? points[Random.Range(0, points.Count)] : points[i];

            // spacing when multiple items use the same point
            int used = 0;
            usage.TryGetValue(pt, out used);
            used++;
            usage[pt] = used;

            Vector3 pos = pt.position;
            Quaternion rot = pt.rotation;

            if (used > 1)
            {
                // golden-angle spiral for even separation
                float kGolden = 137.508f * Mathf.Deg2Rad;
                float r = samePointRadiusStep * Mathf.Sqrt(used - 1);
                float ang = (used - 1) * kGolden;

                pos += new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;

                // tiny jitter + optional vertical stacking
                pos += new Vector3(
                    Random.Range(-samePointJitter, samePointJitter),
                    samePointVerticalStep * (used - 1),
                    Random.Range(-samePointJitter, samePointJitter)
                );
            }

            if (facePlayer && player != null)
            {
                Vector3 lookFrom = pos + Vector3.up * faceYOffset;
                Vector3 lookTo = player.position + Vector3.up * faceYOffset;
                rot = Quaternion.LookRotation(lookTo - lookFrom);
            }

            // NEW: lock vertical to the spawn point if desired; otherwise keep auto-lift
            if (lockYToSpawnPoint)
                pos.y = pt.position.y;
            else
                pos = EnsureAboveGround(pos);

            var go = Instantiate(domainLabelPrefab, pos, rot, spawnedRoot);
            if (logVerbose) Debug.Log($"Spawned '{go.name}' at {pos}");

            // Ensure pickup component exists (root or child)
            var pickup = go.GetComponent<DomainPickup>();
            if (pickup == null) pickup = go.GetComponentInChildren<DomainPickup>(true);
            if (pickup == null) pickup = go.AddComponent<DomainPickup>();

            // Give pickup a reference to this manager so it can report picks
            pickup.manager = this;

            // Make sure it’s pickable
            var col = go.GetComponent<Collider>();
            if (col == null) { col = go.AddComponent<BoxCollider>(); col.isTrigger = true; }
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) { rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false; }

            // Update ALL TMP_Texts so no placeholder remains
            pickup.Domain = domains[i];
            var allTmps = go.GetComponentsInChildren<TMP_Text>(true);
            for (int t = 0; t < allTmps.Length; t++)
                allTmps[t].text = domains[i];

            if (pickup.text == null && allTmps.Length > 0)
                pickup.text = allTmps[0];
        }
    }

    List<Transform> CollectSpawnPoints()
    {
        var list = new List<Transform>();

        if (spawnPointsParent != null)
            foreach (Transform child in spawnPointsParent) if (child) list.Add(child);

        if (spawnPoints != null && spawnPoints.Length > 0)
            foreach (var t in spawnPoints) if (t && !list.Contains(t)) list.Add(t);

        return list;
    }

    void ClearSpawned()
    {
        if (spawnedRoot == null) return;
        for (int i = spawnedRoot.childCount - 1; i >= 0; i--)
            Destroy(spawnedRoot.GetChild(i).gameObject);
    }

    void Shuffle<T>(IList<T> arr)
    {
        for (int i = arr.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var pt in CollectSpawnPoints())
        {
            if (pt == null) continue;
            Gizmos.DrawWireSphere(pt.position, 0.25f);
            Gizmos.DrawLine(pt.position, pt.position + Vector3.up * 0.6f);
        }
    }

    // ---------------- Pick list (called by DomainPickup) ----------------
    
    public string CurrentSelectedDomain { get; private set; }
    public void AddPickedDomain(string domain)
    {
        if (!pickedDomains.Contains(domain))
            pickedDomains.Add(domain);

        RefreshPickedUI();
        OnDomainPicked?.Invoke(domain);   // << notify cart UI
        CurrentSelectedDomain = domain;   // remember latest pick
        RefreshPickedUI();
        OnDomainPicked?.Invoke(domain);   // cart will listen to this

    }


    public void ClearPicked()
    {
        pickedDomains.Clear();
        RefreshPickedUI();
    }

    void RefreshPickedUI()
    {
        if (pickedListText == null) return;
        if (pickedDomains.Count == 0)
            pickedListText.text = "Picked: (none)";
        else
            pickedListText.text = "Picked:\n" + string.Join("\n", pickedDomains);
    }

    // keep spawns from sinking below colliders when lockYToSpawnPoint is false
    Vector3 EnsureAboveGround(Vector3 p)
    {
        const float startY = 10000f;
        const float maxDist = 20000f;
        const float lift = 0.25f;

        Vector3 origin = new Vector3(p.x, startY, p.z);

        if (Physics.Raycast(origin, Vector3.down, out var hit, maxDist, ~0, QueryTriggerInteraction.Ignore))
        {
            float minY = hit.point.y + lift;
            if (p.y < minY) p.y = minY;
        }
        return p;
    }
    // NEW: notify UI when a domain is picked
    public event System.Action<string> OnDomainPicked;

    // NEW: safe read-only view of the picked list
    public IReadOnlyList<string> GetPickedDomains() => pickedDomains;
    
    
    


}
