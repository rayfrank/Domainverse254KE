// SecondSearchListUI.cs
// Searches RDAP, populates a ScrollView with readable rows,
// lets the user SELECT exactly one domain, shows which one is selected,
// and on Checkout opens your deployed landing page with the remaining
// domains in this list (your "cart"). "Remove Selected" removes just the
// selected row from THIS UI (no external cart link).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SecondSearchListUI : MonoBehaviour
{
    [Header("FIRST panel (where user types)")]
    public TMP_InputField input;
    public Button searchButton;
    public TextMeshProUGUI statusText;   // used while searching
    public GameObject firstPanel;

    [Header("SECOND panel (results list)")]
    public GameObject secondPanel;
    public TextMeshProUGUI secondStatusText; // shows result summary + selection

    [Header("ScrollView wiring")]
    public ScrollRect scrollRect;         // optional; auto-find if null
    public RectTransform viewport;        // optional; will use content.parent if null
    public RectTransform content;         // REQUIRED: ScrollView/Viewport/Content

    [Header("Row prefab + layout")]
    public GameObject rowPrefab;          // optional; if null, a fallback row is created
    public float rowHeight = 72f;
    public float rowSpacing = 12f;

    [Header("Row text style")]
    public bool useAutoSize = true;
    public int rowFontSize = 44;          // when useAutoSize = false
    public int autoSizeMin = 36;          // when useAutoSize = true
    public int autoSizeMax = 64;
    public float rowPadX = 24f;
    public float rowPadY = 14f;

    [Header("Flow")]
    public float panelSwitchDelay = 1.0f;
    public bool clearListOnSearch = true;

    [Header("Search options")]
    public bool bypassAvailabilityForTest = false;
    public bool logVerbose = true;

    [Header("Checkout target")]
    [Tooltip("Your deployed backend base URL, e.g. https://domaininverse254ke.onrender.com")]
    public string backendBase = "https://domainverse254ke.onrender.com";
    public Button checkoutButton;         // tap -> open landing
    public Button removeSelectedButton;   // tap -> remove selected row (local only)

    [Header("Checkout behaviour")]
    [Tooltip("When true (default), Checkout sends ALL remaining rows (cart).")]
    public bool includeAllRemainingOnCheckout = true;
    [Tooltip("If a domain is selected, put it first so landing preselects it.")]
    public bool placeSelectedFirstInLanding = true;

    // ---------- internal state ----------
    readonly Dictionary<string, GameObject> rowMap =
        new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    string selected;                      // exactly one selected domain (or null)

    void Awake()
    {
        if (searchButton) searchButton.onClick.AddListener(() => StartCoroutine(RunSearch()));

        if (checkoutButton)
        {
            checkoutButton.onClick.RemoveAllListeners();
            checkoutButton.onClick.AddListener(OnCheckout);
        }

        if (removeSelectedButton)
        {
            removeSelectedButton.onClick.RemoveAllListeners();
            removeSelectedButton.onClick.AddListener(OnRemoveSelected);
        }

        if (firstPanel) firstPanel.SetActive(true);
        if (secondPanel) secondPanel.SetActive(false);

        EnsureScrollViewLayout();
        SetSelected(null);
        UpdateSelectionStatus();
    }

    // ---------- SEARCH ----------
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

        if (bypassAvailabilityForTest) available.AddRange(candidates);
        else
        {
            foreach (var fqdn in candidates)
            {
                bool? ok = null;
                yield return StartCoroutine(CheckAvailability(fqdn, v => ok = v));
                if (logVerbose) Debug.Log($"[RDAP] {fqdn} => {(ok == true ? "AVAILABLE" : "REGISTERED/ERR")}");
                if (ok == true) available.Add(fqdn);
                yield return new WaitForSeconds(0.15f);
            }
        }

        if (available.Count == 0)
        {
            if (statusText) statusText.text = "No available suggestions (or network blocked). Try another word or toggle bypass.";
            if (searchButton) searchButton.interactable = true;
            yield break;
        }

        if (clearListOnSearch) ClearList();
        PopulateList(available);

        string line = $"Found {available.Count} ideas.";
        if (statusText) statusText.text = line;

        yield return new WaitForSeconds(panelSwitchDelay);

        if (firstPanel) firstPanel.SetActive(false);
        if (secondPanel) secondPanel.SetActive(true);
        if (secondStatusText) secondStatusText.text = line;

        if (searchButton) searchButton.interactable = true;

        // default: no preselection
        SetSelected(null);
        UpdateSelectionStatus();
    }

    string BuildRdapUrl(string fqdn)
    {
        string lower = fqdn.ToLowerInvariant();
        return lower.EndsWith(".ke")
            ? "https://rdap.kenic.or.ke/domain/" + lower
            : "https://rdap.org/domain/" + lower;
    }

    IEnumerator CheckAvailability(string fqdn, System.Action<bool> done)
    {
        using (var req = UnityWebRequest.Get(BuildRdapUrl(fqdn)))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success) done(false);               // 200 => registered
            else done((int)req.responseCode == 404);                                     // 404 => likely free
        }
    }

    // ---------- LIST BUILDING ----------
    void PopulateList(List<string> domains)
    {
        if (!content)
        {
            Debug.LogError("Assign 'content' (ScrollView/Viewport/Content).");
            return;
        }

        foreach (var d in domains)
        {
            if (rowMap.ContainsKey(d)) continue;

            GameObject row = rowPrefab
                ? Instantiate(rowPrefab, content)
                : CreateDefaultRow(content);

            NormalizeRow(row);

            // Put domain text into any TMP label (or create one)
            var label = row.GetComponentInChildren<TMP_Text>(true);
            if (!label) label = row.AddComponent<TextMeshProUGUI>();
            label.text = d;

            ApplyTextStyle(row);

            // Clicking a row selects that domain
            var btn = row.GetComponentInChildren<Button>(true) ?? row.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                SetSelected(d);
                UpdateSelectionStatus();
            });

            rowMap[d] = row;
            UpdateRowVisual(d);
        }

        ForceLayout();
    }

    void ClearList()
    {
        foreach (var kv in rowMap)
            if (kv.Value) Destroy(kv.Value);
        rowMap.Clear();
    }

    // ---------- SELECTION ----------
    void SetSelected(string domainOrNull)
    {
        selected = string.IsNullOrWhiteSpace(domainOrNull) ? null : domainOrNull.Trim();

        if (checkoutButton) checkoutButton.interactable = rowMap.Count > 0; // checkout enabled if anything left
        if (removeSelectedButton) removeSelectedButton.interactable = !string.IsNullOrEmpty(selected);

        foreach (var key in rowMap.Keys.ToArray())
            UpdateRowVisual(key);
    }

    void UpdateRowVisual(string domain)
    {
        if (!rowMap.TryGetValue(domain, out var row) || row == null) return;

        var img = row.GetComponent<Image>() ?? row.AddComponent<Image>();
        bool isSel = !string.IsNullOrEmpty(selected) &&
                     selected.Equals(domain, StringComparison.OrdinalIgnoreCase);

        img.color = isSel ? new Color(1f, 1f, 1f, 0.22f) : new Color(1f, 1f, 1f, 0.10f);

        var outline = row.GetComponent<Outline>() ?? row.AddComponent<Outline>();
        outline.effectColor = isSel ? new Color(1f, 0.6f, 0.2f, 0.65f) : new Color(1f, 1f, 1f, 0.25f);
        outline.effectDistance = isSel ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
    }

    void UpdateSelectionStatus()
    {
        string line = string.IsNullOrEmpty(selected) ? "Selected: (none)" : $"Selected: {selected}";
        if (secondStatusText) secondStatusText.text = line;
    }

    // ---------- BUTTONS ----------
    void OnCheckout()
    {
        // collect the "cart": everything still in the list
        var cart = GetCartDomains().ToList();
        if (cart.Count == 0)
        {
            if (secondStatusText) secondStatusText.text = "Cart is empty.";
            return;
        }

        // optionally put selected first so landing preselects it
        if (placeSelectedFirstInLanding && !string.IsNullOrEmpty(selected))
        {
            cart = cart
                .OrderBy(d => d.Equals(selected, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ToList();
        }

        string domainsCsv = BuildDomainsCsv(cart);
        string url = $"{backendBase.TrimEnd('/')}/kenic/landing?domains={domainsCsv}";
        Application.OpenURL(url);

        if (secondStatusText) secondStatusText.text = $"Opening… {cart.Count} name(s)";
    }

    void OnRemoveSelected()
    {
        if (string.IsNullOrEmpty(selected)) return;

        if (rowMap.TryGetValue(selected, out var row) && row)
            Destroy(row);
        rowMap.Remove(selected);

        SetSelected(null);
        UpdateSelectionStatus();
        ForceLayout();
    }

    // ---------- CART HELPERS ----------
    IEnumerable<string> GetCartDomains()
    {
        // all keys that still have a row alive
        foreach (var kv in rowMap)
        {
            if (kv.Value) yield return kv.Key;
        }
    }

    string BuildDomainsCsv(IEnumerable<string> names)
    {
        // encode EACH name, join with commas (do not encode the commas)
        var encoded = names.Select(Uri.EscapeDataString);
        return string.Join(",", encoded);
    }

    // ---------- AUTO-LAYOUT WIRING ----------
    void EnsureScrollViewLayout()
    {
        if (!content)
        {
            Debug.LogError("Content is required. Drag ScrollView/Viewport/Content here.");
            return;
        }

        if (!viewport && content.parent) viewport = content.parent as RectTransform;

        if (!scrollRect) scrollRect = content.GetComponentInParent<ScrollRect>();
        if (!scrollRect && viewport)
        {
            scrollRect = viewport.GetComponent<ScrollRect>();
            if (!scrollRect) scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        }

        if (scrollRect)
        {
            scrollRect.content = content;
            if (viewport) scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        if (viewport && !viewport.GetComponent<RectMask2D>())
            viewport.gameObject.AddComponent<RectMask2D>();

        // Content: stretch horizontally, top-anchored
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;
        content.localRotation = Quaternion.identity;

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = rowSpacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Guarantee each row participates in layout correctly
    void NormalizeRow(GameObject row)
    {
        var rt = row.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        var le = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
        if (le.minHeight < rowHeight) le.minHeight = rowHeight;
        if (le.preferredHeight < rowHeight) le.preferredHeight = rowHeight;

        // Remove any ContentSizeFitter on the row root (fights layout)
        var badFitter = row.GetComponent<ContentSizeFitter>();
        if (badFitter) Destroy(badFitter);
    }

    void ApplyTextStyle(GameObject row)
    {
        var tmps = row.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps)
        {
            if (useAutoSize)
            {
                t.enableAutoSizing = true;
                t.fontSizeMin = autoSizeMin;
                t.fontSizeMax = autoSizeMax;
            }
            else
            {
                t.enableAutoSizing = false;
                t.fontSize = rowFontSize;
            }

            t.margin = new Vector4(rowPadX, rowPadY, rowPadX, rowPadY);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.overflowMode = TextOverflowModes.Truncate;
        }

        float minH = (useAutoSize ? autoSizeMin : rowFontSize) + (rowPadY * 2f) + 8f;
        var le = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
        if (le.minHeight < minH) le.minHeight = minH;
        if (le.preferredHeight < minH) le.preferredHeight = minH;
    }

    // Minimal fallback row (used when no prefab is assigned)
    GameObject CreateDefaultRow(Transform parent)
    {
        var row = new GameObject("_Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = new Color(1, 1, 1, 0.10f);

        // Label
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.offsetMin = new Vector2(rowPadX, rowPadY);
        lrt.offsetMax = new Vector2(-rowPadX, -rowPadY);

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.enableAutoSizing = useAutoSize;
        if (!useAutoSize) label.fontSize = rowFontSize;
        label.fontSizeMin = autoSizeMin;
        label.fontSizeMax = autoSizeMax;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "example.co.ke";

        // Clickable area
        var btn = row.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.24f);
        btn.colors = colors;

        // Baseline layout
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = Mathf.Max(rowHeight, 56f);
        le.preferredHeight = le.minHeight;

        return row;
    }

    // Optional: wire to a Back button if you still want it
    public void BackToFirstPanel()
    {
        if (secondPanel) secondPanel.SetActive(false);
        if (firstPanel) firstPanel.SetActive(true);
    }

    // Force the ScrollView to rebuild its layout now
    void ForceLayout()
    {
        if (!content) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.MarkLayoutForRebuild(content);
    }
}
