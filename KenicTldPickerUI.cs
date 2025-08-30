using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System; // Uri

public class KenicTldListSimple : MonoBehaviour
{
    [Header("Server")]
    public string backendBase = "https://domaininverse254ke.onrender.com";

    [Header("UI")]
    public GameObject panelRoot;
    public RectTransform listContent;     // ScrollView/Viewport/Content
    public TextMeshProUGUI statusLabel;

    [Header("Row style")]
    public float rowHeight = 72f;
    public float rowSpacing = 12f;
    public float padX = 24f;
    public float padY = 14f;
    public int   autoMin = 28;
    public int   autoMax = 60;

    [Header("What to send")]
    public bool includePickedAlso = false; // by default only cart items are sent
    public bool dedupeDomains = true;

    [Header("Data sources")]
    public CartSystemAuto cart;                 // supplies full domains (FQDN) from the cart
    public DomainSearchAndSpawn domainManager;  // optional: also include picked list

    [Header("List mode")]
    public bool showRegistrarsInsteadOfTlds = true;   // ON => show registrar names, OFF => show TLDs
    public bool openRegistrarDirect = false;          // keep FALSE to open landing (choose one domain)

    [Header("Current selection")]
    public string currentSuffix = ".ke";

    // ===== Entry point (wire to your Checkout button) =====
    public void OpenFromCheckout()
    {
        if (panelRoot) panelRoot.SetActive(true);
        EnsureScrollViewLayout();
        Clear(listContent);
        StopAllCoroutines();

        if (showRegistrarsInsteadOfTlds)
        {
            StartCoroutine(Co_LoadRegistrars());
            Say("Pick a registrar to continue.");
        }
        else
        {
            StartCoroutine(Co_LoadTlds());
            Say("Pick your TLD to continue.");
        }
    }

    public void ClosePanel()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    // ---------- Load REGISTRARS ----------
    IEnumerator Co_LoadRegistrars()
    {
        using (var req = UnityWebRequest.Get($"{backendBase}/kenic/registrars"))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Say("Failed to load registrars: " + req.error);
                yield break;
            }

            var resp = JsonUtility.FromJson<RegistrarsResp>(req.downloadHandler.text);
            if (resp?.registrars == null || resp.registrars.Length == 0)
            {
                Say("No registrars returned.");
                yield break;
            }

            foreach (var r in resp.registrars) CreateRegistrarButtonRow(r);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        }
    }

    void CreateRegistrarButtonRow(RegistrarItem r)
    {
        var row = MakeRowGO("Registrar Button");
        var txt = AddLabel(row.transform);
        txt.text = (r.name ?? "Registrar").Trim();

        var btn = row.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (openRegistrarDirect && !string.IsNullOrEmpty(r.site))
            {
                Application.OpenURL(r.site);           // opens registrar home
                Say($"Opening {r.name}…");
            }
            else
            {
                OpenLandingForCart();                   // preferred: go through landing
            }
        });
    }

    // ---------- Load TLDs (optional original mode) ----------
    IEnumerator Co_LoadTlds()
    {
        using (var req = UnityWebRequest.Get($"{backendBase}/kenic/tlds"))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Say("Failed to load TLDs: " + req.error);
                yield break;
            }

            var resp = JsonUtility.FromJson<TldsResp>(req.downloadHandler.text);
            if (resp?.tlds == null || resp.tlds.Length == 0)
            {
                Say("No TLDs returned.");
                yield break;
            }

            foreach (var t in resp.tlds) CreateTldButtonRow(t);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        }
    }

    void CreateTldButtonRow(TldItem t)
    {
        var row = MakeRowGO("TLD Button");
        var txt = AddLabel(row.transform);

        txt.text = (t.tld ?? "").Trim();
        if (t.restricted) txt.text += "  (restricted)";

        var btn = row.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            SetSuffix(t.tld);
            OpenLandingForCart();
        });
    }

    public void SetSuffix(string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix)) suffix = ".ke";
        suffix = suffix.Trim().ToLower();
        if (!suffix.StartsWith(".")) suffix = "." + suffix;
        currentSuffix = suffix;
        Debug.Log("[KenIC] currentSuffix = " + currentSuffix);
    }

    // ---------- Open landing page with ALL domains in cart ----------
    void OpenLandingForCart()
    {
        var names = GatherCartDomains(); // full FQDNs
        if (names.Count == 0)
        {
            Say("Your cart is empty.");
            return;
        }

        if (dedupeDomains)
            names = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Encode EACH name, join with commas (commas must NOT be encoded)
        var encodedEach = names.Select(Uri.EscapeDataString);
        string domainsCsv = string.Join(",", encodedEach);

        string url = $"{backendBase.TrimEnd('/')}/kenic/landing?domains={domainsCsv}";
        Debug.Log("[TrustedList] Opening: " + url);
        Application.OpenURL(url);
        Say($"Opening registrars… ({names.Count} name(s))");
    }

    // Full FQDNs from cart (+ optional picked)
    List<string> GatherCartDomains()
    {
        var list = new List<string>();

        if (cart != null)
        {
            var items = cart.GetCartItems();
            if (items != null)
            {
                foreach (var d in items)
                {
                    var s = (d ?? "").Trim();
                    if (!string.IsNullOrEmpty(s)) list.Add(s);
                }
            }
        }

        if (includePickedAlso && domainManager != null)
        {
            var picked = domainManager.GetPickedDomains();
            if (picked != null)
            {
                foreach (var d in picked)
                {
                    var s = (d ?? "").Trim();
                    if (!string.IsNullOrEmpty(s)) list.Add(s);
                }
            }
        }

        return list;
    }

    // ---------- Row / Label helpers ----------
    GameObject MakeRowGO(string name)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(listContent, false);

        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.localScale = Vector3.one;

        var le = row.GetComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        le.flexibleWidth = 1f;

        row.GetComponent<Image>().color = new Color(1, 1, 1, 0.14f);
        return row;
    }

    TextMeshProUGUI AddLabel(Transform parent)
    {
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.offsetMin = new Vector2(padX, padY);
        lrt.offsetMax = new Vector2(-padX, -padY);

        var txt = labelGO.AddComponent<TextMeshProUGUI>();
        txt.enableAutoSizing = true;
        txt.fontSizeMin = autoMin;
        txt.fontSizeMax = autoMax;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.overflowMode = TextOverflowModes.Overflow;
        txt.enableWordWrapping = false;
        return txt;
    }

    // ---------- ScrollView auto-wiring ----------
    void EnsureScrollViewLayout()
    {
        if (!listContent)
        {
            Debug.LogError("Assign 'listContent' (ScrollView/Viewport/Content).");
            return;
        }
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot     = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.localScale = Vector3.one;

        var vlg = listContent.GetComponent<VerticalLayoutGroup>() ??
                  listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = rowSpacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = listContent.GetComponent<ContentSizeFitter>() ??
                     listContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var viewport = listContent.parent as RectTransform;
        if (viewport && !viewport.GetComponent<RectMask2D>())
            viewport.gameObject.AddComponent<RectMask2D>();

        var scroll = listContent.GetComponentInParent<ScrollRect>();
        if (scroll)
        {
            scroll.content = listContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    void Say(string m)
    {
        if (statusLabel) statusLabel.text = m;
        Debug.Log("[TrustedList] " + m);
    }

    void Clear(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // DTOs for JsonUtility
    [System.Serializable] class RegistrarsResp { public RegistrarItem[] registrars; }
    [System.Serializable] class RegistrarItem { public string name; public string site; public string searchUrl; }
    [System.Serializable] class TldsResp { public TldItem[] tlds; }
    [System.Serializable] class TldItem { public string tld; public bool restricted; public string note; }
}
