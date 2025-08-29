using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CartSystemAuto : MonoBehaviour
{
    [Header("Refs")]
    public DomainSearchAndSpawn domainManager;     // drag your DomainSearchAndSpawn here
    public Transform listContent;                  // ScrollView/Viewport/Content (RectTransform)
    public GameObject linePrefab;                  // prefab for a single row (optional)
    public TextMeshProUGUI emptyHint;              // optional: "Cart empty" label

    [Header("Options")]
    public bool autoAddOnPick = true;              // picked => auto add to cart
    public bool preventDuplicates = true;          // domains unique in cart

    [Header("ScrollView auto-wiring (optional)")]
    public ScrollRect scrollRect;                  // auto-found if null
    public RectTransform viewport;                 // uses content.parent if null

    [Header("Row layout & text style")]
    public float rowHeight   = 72f;                // min/preferred height per row
    public float rowSpacing  = 12f;                // space between rows
    public bool  useAutoSize = true;
    public int   rowFontSize = 40;                 // used when useAutoSize = false
    public int   autoSizeMin = 34;
    public int   autoSizeMax = 56;
    public float rowPadX     = 24f;                // TMP margins
    public float rowPadY     = 14f;

    // -------- model (case-insensitive de-dupe) --------
    readonly List<string> items = new List<string>();
    readonly Dictionary<string, GameObject> map =
        new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);

    // --- button colors ---
    static readonly Color BtnNormal  = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color BtnHover   = new Color(0.10f, 0.10f, 0.10f, 0.85f);
    static readonly Color BtnPressed = new Color(0f, 0f, 0f, 0.95f);

    void Awake()
    {
        EnsureScrollViewLayout();

        if (domainManager)
            domainManager.OnDomainPicked += OnPicked;

        RefreshEmptyState();
    }

    void OnDestroy()
    {
        if (domainManager)
            domainManager.OnDomainPicked -= OnPicked;
    }

    void OnPicked(string domain)
    {
        if (autoAddOnPick) Add(domain);
    }

    // ----------------- Public API -----------------
    public void Add(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return;
        if (preventDuplicates && map.ContainsKey(domain)) return;

        // create row (fallback if no prefab)
        GameObject row = linePrefab ? Instantiate(linePrefab, listContent)
                                    : CreateDefaultRow((RectTransform)listContent);

        NormalizeRow(row);
        ApplyTextStyle(row);
        BindRow(row, domain); // sets domain text + styles/hook remove

        items.Add(domain);
        if (!map.ContainsKey(domain)) map.Add(domain, row);

        RefreshEmptyState();
        ForceLayout();
    }

    public void Remove(string domain)
    {
        if (!map.TryGetValue(domain, out var row)) return;
        map.Remove(domain);
        items.Remove(domain);
        if (row) Destroy(row);
        RefreshEmptyState();
        ForceLayout();
    }

    public void Clear()
    {
        foreach (var kv in map) if (kv.Value) Destroy(kv.Value);
        map.Clear();
        items.Clear();
        RefreshEmptyState();
        ForceLayout();
    }

    // Optional: wire your existing "Remove From Cart" button to this
    public void RemoveCurrentlyPicked()
    {
        if (!domainManager) return;
        var d = domainManager.CurrentSelectedDomain;
        if (!string.IsNullOrEmpty(d)) Remove(d);
    }

    public IReadOnlyList<string> GetCartItems() => items;

    // ----------------- Layout helpers -----------------
    void EnsureScrollViewLayout()
    {
        if (!listContent)
        {
            Debug.LogError("CartSystemAuto: assign 'listContent' (ScrollView/Viewport/Content).");
            return;
        }

        var content = (RectTransform)listContent;

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

        // Content anchors: stretch width, top-anchored height
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

    void NormalizeRow(GameObject row)
    {
        var rt = row.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;                 // squash weird prefab scaling
            rt.localRotation = Quaternion.identity;
        }

        // force a sane minimum/Preferred height
        var le = row.GetComponent<LayoutElement>();
        if (!le) le = row.AddComponent<LayoutElement>();
        if (le.minHeight < rowHeight)       le.minHeight       = rowHeight;
        if (le.preferredHeight < rowHeight) le.preferredHeight = rowHeight;

        // Ensure a horizontal layout for (text + remove button)
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        if (!hlg) hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childControlWidth  = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = false;

        // Root ContentSizeFitter fights the parent layout — remove it
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
            t.overflowMode = TextOverflowModes.Ellipsis;
        }

        // ensure final height fits chosen font + padding
        float minH = (useAutoSize ? autoSizeMin : rowFontSize) + (rowPadY * 2f) + 8f;
        var le = row.GetComponent<LayoutElement>();
        if (!le) le = row.AddComponent<LayoutElement>();
        if (le.minHeight < minH)       le.minHeight       = minH;
        if (le.preferredHeight < minH) le.preferredHeight = minH;
    }

    void BindRow(GameObject row, string domain)
    {
        // find or create the domain text
        TMP_Text label = null;
        var t = row.transform.Find("DomainText");
        if (t) label = t.GetComponent<TMP_Text>();
        if (!label) label = row.GetComponentInChildren<TMP_Text>(true);
        if (!label)
        {
            var go = new GameObject("DomainText", typeof(RectTransform));
            go.transform.SetParent(row.transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
        }
        label.text = domain;

        // Left-align domain, keep remove button on the right
        if (label is TextMeshProUGUI tmpDomain)
        {
            tmpDomain.alignment = TextAlignmentOptions.MidlineLeft;
            tmpDomain.enableAutoSizing = true;
            tmpDomain.fontSizeMin = autoSizeMin;
            tmpDomain.fontSizeMax = autoSizeMax;
            tmpDomain.margin = new Vector4(16, 8, 8, 8);
        }

        // find or create the remove button
        Button removeBtn = null;
        var rb = row.transform.Find("RemoveButton");
        if (rb) removeBtn = rb.GetComponent<Button>();
        if (!removeBtn)
        {
            var btnGO = new GameObject("RemoveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(row.transform, false);
            removeBtn = btnGO.GetComponent<Button>();

            // give it a label now so styling can outline it
            var xTMP = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            xTMP.transform.SetParent(btnGO.transform, false);
            xTMP.text = "✕";
        }

        // hook remove + style
        removeBtn.onClick.RemoveAllListeners();
        removeBtn.onClick.AddListener(() => Remove(domain));
        StyleRemoveButton(removeBtn);
    }

    GameObject CreateDefaultRow(RectTransform parent)
    {
        var row = new GameObject("CartRow", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = new Color(1, 1, 1, 0); // transparent row bg

        // Domain text
        var labelGO = new GameObject("DomainText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        var domainTMP = labelGO.GetComponent<TextMeshProUGUI>();
        domainTMP.text = "example.com";
        domainTMP.enableAutoSizing = true;
        domainTMP.fontSizeMin = autoSizeMin; domainTMP.fontSizeMax = autoSizeMax;
        domainTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Remove button
        var btnGO = new GameObject("RemoveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        var btn = btnGO.GetComponent<Button>();

        // label inside the button so StyleRemoveButton can outline it
        var xTMP = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        xTMP.transform.SetParent(btnGO.transform, false);
        xTMP.text = "✕";

        // baseline layout so row behaves
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childControlWidth  = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(8, 8, 4, 4);

        var le = row.AddComponent<LayoutElement>();
        le.minHeight = Mathf.Max(rowHeight, 56f);
        le.preferredHeight = le.minHeight;

        // style the X button
        StyleRemoveButton(btn);

        return row;
    }

    void ForceLayout()
    {
        if (!listContent) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)listContent);
    }

    void RefreshEmptyState()
    {
        if (emptyHint) emptyHint.gameObject.SetActive(items.Count == 0);
    }

    // ----------------- Styling helpers -----------------
    void StyleTMPWithOutline(TextMeshProUGUI tmp, float outlineWidth = 0.35f)
    {
        if (!tmp) return;
        tmp.color = Color.white;
        tmp.fontStyle |= FontStyles.Bold;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20; tmp.fontSizeMax = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.margin = new Vector4(8, 6, 8, 6);

        // Use a cloned material so we don't affect all TMP texts
        var mat = new Material(tmp.fontSharedMaterial);
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, outlineWidth);
        mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color(0, 0, 0, 0.85f));
        tmp.fontMaterial = mat;
    }

    void StyleRemoveButton(Button btn)
    {
        if (!btn) return;

        // Background image & tint
        var img = btn.GetComponent<Image>() ?? btn.gameObject.AddComponent<Image>();
        img.raycastTarget = true;
        img.color = BtnNormal;

        // Outer stroke around the button rect
        var uiOutline = btn.GetComponent<Outline>() ?? btn.gameObject.AddComponent<Outline>();
        uiOutline.effectColor = new Color(1f, 1f, 1f, 0.45f);
        uiOutline.effectDistance = new Vector2(2f, -2f);

        // Button state colors
        var cb = btn.colors;
        cb.normalColor      = BtnNormal;
        cb.highlightedColor = BtnHover;
        cb.pressedColor     = BtnPressed;
        cb.selectedColor    = BtnNormal;
        cb.disabledColor    = new Color(0f, 0f, 0f, 0.35f);
        cb.colorMultiplier  = 1f;
        btn.colors = cb;

        // Hit area
        var le = btn.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
        le.minWidth = Mathf.Max(le.minWidth, 120f);
        le.preferredWidth = Mathf.Max(le.preferredWidth, 140f);
        le.minHeight = Mathf.Max(le.minHeight, rowHeight);

        // ✕ label styling
        var xLabel = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!xLabel)
        {
            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btn.transform, false);
            xLabel = txtGO.GetComponent<TextMeshProUGUI>();
            xLabel.text = "✕"; // try "×" if your font renders nicer
        }
        StyleTMPWithOutline(xLabel, 0.40f);
    }
}
