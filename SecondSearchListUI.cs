// SecondSearchListUI.cs
// Searches RDAP, updates status, swaps panels, and fills a ScrollView list.
// No world spawning. Auto-wires ScrollView layout so rows never overlap,
// and scales row text so it’s easy to read on mobile.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SecondSearchListUI : MonoBehaviour
{
    [Header("FIRST panel (where user types)")]
    public TMP_InputField input;
    public Button searchButton;
    public TextMeshProUGUI statusText;
    public GameObject firstPanel;

    [Header("SECOND panel (results list)")]
    public GameObject secondPanel;
    public TextMeshProUGUI secondStatusText;

    [Header("ScrollView wiring")]
    public ScrollRect scrollRect;              // optional; auto-find if null
    public RectTransform viewport;             // optional; will use content.parent if null
    public RectTransform content;              // REQUIRED: ScrollView/Viewport/Content

    [Header("Row prefab + layout")]
    public GameObject rowPrefab;               // your item prefab (root is the row)
    public float rowHeight = 72f;              // baseline height used if prefab has none
    public float rowSpacing = 12f;             // spacing between rows

    [Header("Row text style")]
    public bool useAutoSize = true;
    public int rowFontSize = 44;     // when useAutoSize = false
    public int autoSizeMin = 36;     // when useAutoSize = true
    public int autoSizeMax = 64;
    public float rowPadX = 24f;      // TMP margin left/right
    public float rowPadY = 14f;      // TMP margin top/bottom

    [Header("Flow")]
    public float panelSwitchDelay = 1.0f;
    public bool clearListOnSearch = true;

    [Header("Search options")]
    public bool bypassAvailabilityForTest = false;
    public bool logVerbose = true;

    void Awake()
    {
        if (searchButton) searchButton.onClick.AddListener(() => StartCoroutine(RunSearch()));

        if (firstPanel)  firstPanel.SetActive(true);
        if (secondPanel) secondPanel.SetActive(false);

        EnsureScrollViewLayout();
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

        if (firstPanel)  firstPanel.SetActive(false);
        if (secondPanel) secondPanel.SetActive(true);
        if (secondStatusText) secondStatusText.text = line;

        if (searchButton) searchButton.interactable = true;
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
            GameObject row = rowPrefab
                ? Instantiate(rowPrefab, content)
                : CreateDefaultRow(content);

            NormalizeRow(row);

            // Put domain text into any TMP label (or create one)
            var label = row.GetComponentInChildren<TMP_Text>(true);
            if (!label) label = row.AddComponent<TextMeshProUGUI>();
            label.text = d;

            ApplyTextStyle(row);

            // Optional: copy-to-clipboard if row has a Button
            var btn = row.GetComponentInChildren<Button>(true);
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    GUIUtility.systemCopyBuffer = d;
                    if (secondStatusText) secondStatusText.text = $"Copied '{d}'";
                });
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    void ClearList()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
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
        content.pivot     = new Vector2(0.5f, 1f);
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
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Make each row participate in layout correctly
    void NormalizeRow(GameObject row)
    {
        var rt = row.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;            // squash weird prefab scaling
            rt.localRotation = Quaternion.identity;
        }

        var le = row.GetComponent<LayoutElement>();
        if (!le) le = row.AddComponent<LayoutElement>();
        le.ignoreLayout = false;

        // guarantee a minimum height even if prefab has none
        if (le.minHeight < rowHeight) le.minHeight = rowHeight;
        if (le.preferredHeight < rowHeight) le.preferredHeight = rowHeight;

        // a root ContentSizeFitter fights the parent layout — remove it
        var badFitter = row.GetComponent<ContentSizeFitter>();
        if (badFitter) Destroy(badFitter);
    }

    // Apply big, readable text + margins to all TMPs in the row
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

        // Ensure row is tall enough for the chosen font
        float minH = (useAutoSize ? autoSizeMin : rowFontSize) + (rowPadY * 2f) + 8f;
        var le = row.GetComponent<LayoutElement>();
        if (!le) le = row.AddComponent<LayoutElement>();
        if (le.minHeight < minH) le.minHeight = minH;
        if (le.preferredHeight < minH) le.preferredHeight = minH;
    }

    // Minimal fallback row (used when no prefab is assigned)
    GameObject CreateDefaultRow(Transform parent)
    {
        var row = new GameObject("_Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = new Color(1, 1, 1, 0); // transparent

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = rowFontSize;
        label.margin = new Vector4(rowPadX, rowPadY, rowPadX, rowPadY);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset((int)rowPadX, (int)rowPadX, (int)rowPadY, (int)rowPadY);
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;

        return row;
    }

    // Optional: wire to a Back button on the results panel
    public void BackToFirstPanel()
    {
        if (secondPanel) secondPanel.SetActive(false);
        if (firstPanel)  firstPanel.SetActive(true);
    }
    
}
