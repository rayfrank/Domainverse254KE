// server.js
// KeNIC option B backend: TLDs, registrars, and a landing page
// where the user picks ONE domain and we deep-link to registrar search/cart.

const express = require("express");
const bodyParser = require("body-parser");

const app = express();
app.use(bodyParser.json());

// ---------- Data ----------

// KeNIC .KE family (mark some as restricted to display in the app if desired)
const TLD_LIST = [
  { tld: ".ke",     restricted: false, note: "Direct .ke" },
  { tld: ".co.ke",  restricted: false, note: "Companies / commercial" },
  { tld: ".or.ke",  restricted: false, note: "Organizations" },
  { tld: ".ne.ke",  restricted: false, note: "Networks / providers" },
  { tld: ".go.ke",  restricted: true,  note: "Government (restricted)" },
  { tld: ".ac.ke",  restricted: true,  note: "Academic (restricted)" },
  { tld: ".sc.ke",  restricted: true,  note: "Schools (restricted)" },
  { tld: ".me.ke",  restricted: false, note: "Personal" },
  { tld: ".info.ke",restricted: false, note: "Information" },
  { tld: ".mobi.ke",restricted: false, note: "Mobile" },
];

// A compact list of accredited registrars (add/update freely)
const ACCREDITED_REGISTRARS = [
  { name: "HostPinnacle",                 site: "https://hostpinnacle.co.ke/" },
  { name: "Truehost",                     site: "https://truehost.co.ke/" },
  { name: "EAC Directory (HOSTAFRICA)",   site: "https://www.hostafrica.ke/" },
  { name: "Safaricom",                    site: "https://www.safaricom.co.ke/" },
  { name: "Digital Webframe",             site: "https://digitalwebframe.com/" },
  { name: "Movetech Solutions",           site: "https://movetechsolutions.com/" },
  { name: "Webhost Kenya",                site: "https://webhostkenya.co.ke/" },
  { name: "Softlink Options",             site: "https://softlinkoptions.co.ke/" }
];

// ---------- Deep-link builders (auto-search/pricing on registrar) ----------

function normalizeBase(url) {
  return String(url || "").replace(/\/+$/, "");
}

// Many registrars use WHMCS. Typical deep link:
//   https://<host>/cart.php?a=add&domain=register&query=<domain>
const DEEP_LINK_BUILDERS = {
  "hostpinnacle.co.ke":     d => `https://hostpinnacle.co.ke/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "truehost.co.ke":         d => `https://truehost.co.ke/cloud/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "hostafrica.ke":          d => `https://www.hostafrica.ke/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "safaricom.co.ke":        d => `https://domains.safaricom.co.ke/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "digitalwebframe.com":    d => `https://digitalwebframe.com/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "movetechsolutions.com":  d => `https://movetechsolutions.com/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "webhostkenya.co.ke":     d => `https://webhostkenya.co.ke/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`,
  "softlinkoptions.co.ke":  d => `https://softlinkoptions.co.ke/cart.php?a=add&domain=register&query=${encodeURIComponent(d)}`
};

function buildDeepLink(site, domain) {
  const base = normalizeBase(site);
  const host = base.replace(/^https?:\/\/([^/]+).*/, "$1").toLowerCase();

  for (const key of Object.keys(DEEP_LINK_BUILDERS)) {
    if (host.includes(key)) return DEEP_LINK_BUILDERS[key](domain);
  }
  // fallback: generic WHMCS path (if registrar doesn't use WHMCS,
  // user still lands on a cart/search page and can search there)
  return `${base}/cart.php?a=add&domain=register&query=${encodeURIComponent(domain)}`;
}

// ---------- JSON APIs ----------

app.get("/kenic/tlds", (req, res) => {
  res.json({ tlds: TLD_LIST });
});

app.get("/kenic/registrars", (req, res) => {
  res.json({ registrars: ACCREDITED_REGISTRARS });
});

// ---------- Landing page (single-select) ----------

function escHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;").replace(/</g, "&lt;")
    .replace(/>/g, "&gt;").replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function tldOf(d) {
  const i = d.indexOf(".");
  return i > 0 ? d.substring(i) : "";
}

function renderLandingHtmlFromDomains(domains, registrars) {
  const list = (domains || [])
    .map(s => String(s).trim().toLowerCase())
    .filter(Boolean);
  const unique = [...new Set(list)];
  const tlds = new Set(unique.map(tldOf).filter(Boolean));
  const title = tlds.size === 1 ? `Buy ${[...tlds][0]} domains` : `Buy selected .KE domains`;

  const radios = unique.map((d, i) => `
    <label style="display:flex;gap:.6rem;align-items:center;margin:.35rem 0;">
      <input type="radio" name="pick" value="${escHtml(d)}" ${i===0 ? "checked" : ""}>
      <code style="font-size:0.98rem">${escHtml(d)}</code>
    </label>
  `).join("");

  const cards = registrars.map(r => `
    <div class="card">
      <div class="card-title">${escHtml(r.name)}</div>
      <a href="${escHtml(r.site)}" data-site="${escHtml(r.site)}" class="go" target="_blank" rel="noopener">
        Go to registrar
      </a>
    </div>
  `).join("");

  // We compute deep links client-side so the chosen domain (radio) is respected.
  // The mapping mirrors our server builders above.
  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${escHtml(title)}</title>
<style>
  :root{color-scheme:light dark;}
  body{
    font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;
    max-width:980px;margin:16px auto;padding:0 16px;line-height:1.45
  }
  h1{font-size:clamp(24px,4vw,32px);letter-spacing:.2px;margin:.4rem 0 1rem}
  .panel{padding:14px;border:1px solid #444;border-radius:12px;margin:.7rem 0}
  .card{padding:14px;border:1px solid #444;border-radius:12px;margin:.7rem 0}
  .card-title{font-weight:600;font-size:16px;margin-bottom:8px}
  code{padding:.12em .35em;border-radius:4px;background:#eee}
  a.go{display:inline-block;padding:10px 16px;border-radius:999px;
       border:1px solid #6b4ce6;text-decoration:none}
  @media (prefers-color-scheme: dark){ code{background:#333} }
</style>
</head>
<body>
  <h1>${escHtml(title)}</h1>

  <div class="panel">
    <div style="font-weight:600;margin-bottom:.4rem">Choose one name:</div>
    ${radios}
  </div>

  <div class="panel">
    <div style="font-weight:600;margin-bottom:.4rem">Select a registrar:</div>
    ${cards}
  </div>

  <p style="color:#888;font-size:12px;margin-top:12px">
    Buttons deep-link into registrar search/cart where supported so pricing shows immediately.
  </p>

<script>
  function selectedDomain(){
    const r = document.querySelector('input[name="pick"]:checked');
    return r ? r.value : '';
  }
  function buildDeepLink(site, domain){
    site = String(site||'').replace(/\\/+$/, '');
    const host = site.replace(/^https?:\\/\\/([^/]+).*/, '$1').toLowerCase();
    const whmcs = base => base + '/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('hostpinnacle.co.ke')) return 'https://hostpinnacle.co.ke/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('truehost.co.ke'))     return 'https://truehost.co.ke/cloud/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('hostafrica.ke'))      return 'https://www.hostafrica.ke/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('safaricom.co.ke'))    return 'https://domains.safaricom.co.ke/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('digitalwebframe.com'))return 'https://digitalwebframe.com/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('movetechsolutions.com'))return 'https://movetechsolutions.com/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('webhostkenya.co.ke')) return 'https://webhostkenya.co.ke/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    if (host.includes('softlinkoptions.co.ke'))return 'https://softlinkoptions.co.ke/cart.php?a=add&domain=register&query=' + encodeURIComponent(domain);
    return whmcs(site);
  }
  document.querySelectorAll('a.go').forEach(a=>{
    a.addEventListener('click', ev=>{
      const d = selectedDomain();
      if(!d){ ev.preventDefault(); alert('Pick a name first.'); return; }
      const site = a.getAttribute('data-site');
      a.href = buildDeepLink(site, d);
    });
  });
</script>
</body>
</html>`;
}

app.get("/kenic/landing", (req, res) => {
  const domainsParam = String(req.query.domains || "").trim();
  let domains = [];

  if (domainsParam) {
    domains = domainsParam.split(",").map(s => s.trim()).filter(Boolean);
  } else {
    // legacy fallback ?tld=.co.ke&labels=foo,bar
    const tld = String(req.query.tld || "").trim().replace(/^\./, "");
    const labels = String(req.query.labels || "").split(",").map(s=>s.trim()).filter(Boolean);
    domains = labels.map(l => `${l}.${tld}`).filter(Boolean);
  }

  res.set("Content-Type", "text/html; charset=utf-8");
  res.send(renderLandingHtmlFromDomains(domains, ACCREDITED_REGISTRARS));
});

// Health
app.get("/health", (req, res) => res.json({ ok: true }));

// Listen
const PORT = process.env.PORT || 10000;
app.listen(PORT, () => {
  console.log("KeNIC Option-B API listening on :", PORT);
});
