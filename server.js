// server.js
require("dotenv").config();
const express = require("express");
const bodyParser = require("body-parser");

const app = express();
const PORT = process.env.PORT || 10000;

app.use(bodyParser.json());

// ---- Registrar catalog ------------------------------------------------------
// Add or adjust searchUrl once you've confirmed the pattern.
// {DOMAIN} will be replaced, URL-encoded.
const registrarsBySuffix = {
  ".ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" }, // no public deep link
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
    { name: "EAC Directory (HOSTAFRICA)", href: "https://www.hostafrica.ke/" /*, searchUrl: "https://www.hostafrica.ke/domain-name-search/?domain={DOMAIN}" */ },
    { name: "Safaricom", href: "https://www.safaricom.co.ke/business/cloud-solutions/domains" },
    { name: "Digital Webframe", href: "https://digitalwebframe.com/" },
    { name: "Movetech Solutions Ltd", href: "https://movetechsolutions.com/" },
    { name: "Webhost Kenya", href: "https://webhostkenya.co.ke/" },
    { name: "Softlink Options", href: "https://softlinkoptions.co.ke/" },
  ],
  ".co.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
    { name: "EAC Directory (HOSTAFRICA)", href: "https://www.hostafrica.ke/" },
    { name: "Safaricom", href: "https://www.safaricom.co.ke/business/cloud-solutions/domains" },
  ],
  ".go.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
    { name: "EAC Directory (HOSTAFRICA)", href: "https://www.hostafrica.ke/" },
    { name: "Safaricom", href: "https://www.safaricom.co.ke/business/cloud-solutions/domains" },
  ],
  ".me.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
  ".info.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
  ".or.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
  ".ac.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
  ".sc.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
  ".ne.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
  ".mobi.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
  ],
};

// ---- Helpers ----------------------------------------------------------------
function normSuffix(sfx) {
  if (!sfx) return ".ke";
  sfx = sfx.trim();
  return sfx.startsWith(".") ? sfx.toLowerCase() : ("." + sfx.toLowerCase());
}

// If name already ends with suffix, keep it; else append.
function toFqdn(name, suffix) {
  if (!name) return null;
  const lower = name.toLowerCase();
  return lower.endsWith(suffix.toLowerCase()) ? lower : (lower + suffix);
}

// Splits "a,b,c" -> ["a","b","c"] (trimmed, no empties)
function splitCsv(csv) {
  if (!csv) return [];
  return String(csv)
    .split(",")
    .map(s => s.trim())
    .filter(Boolean);
}

// ---- Landing page: show radios for domains & registrar list -----------------
app.get("/kenic/landing", (req, res) => {
  const suffix = normSuffix(req.query.suffix || ".ke");
  // 'names' can be plain labels ("raytechgames") or fqdn; we normalize to fqdn
  const rawNames = splitCsv(req.query.names || "");
  const fqdns = rawNames
    .map(n => toFqdn(n, suffix))
    .filter(Boolean);

  const displaySuffix = suffix;
  const registrars = registrarsBySuffix[suffix] || registrarsBySuffix[".ke"] || [];

  const title = `Buy ${displaySuffix} domains`;
  const yourNamesText = fqdns.length > 0 ? fqdns.join(", ") : "(none)";

  // First domain pre-selected
  const selectedDefault = fqdns[0] || "";

  // HTML
  res.type("html").send(`<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>${title}</title>
<meta name="viewport" content="width=device-width, initial-scale=1"/>
<style>
  :root { --bg:#0f0f12; --card:#17171c; --text:#e8e8ef; --muted:#bcbccd; --accent:#7b61ff; }
  * { box-sizing:border-box; font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, 'Helvetica Neue', Arial; }
  body { margin:0; background:var(--bg); color:var(--text); padding:24px; }
  h1 { margin:0 0 16px 0; font-weight:700; }
  .muted { color: var(--muted); }
  .wrap { max-width: 1140px; margin: 0 auto; }
  .grid { display: grid; grid-template-columns: 1fr; gap:16px; }
  .card { background:var(--card); border:1px solid #2a2a32; border-radius:14px; padding:18px; }
  .row { display:flex; align-items:center; justify-content:space-between; gap:12px; }
  .domain-list { display:grid; grid-template-columns: 1fr; gap:10px; }
  .domain-item { display:flex; align-items:center; gap:10px; padding:10px 12px; border:1px solid #2b2b34; border-radius:12px; background:#14141a; }
  .domain-item input[type="radio"] { width:20px; height:20px; accent-color: var(--accent);}
  .btn { appearance:none; border:1px solid #3a3a44; background:#1c1c22; color:var(--text);
         border-radius:12px; padding:10px 14px; cursor:pointer; transition: .2s; }
  .btn:hover { background:#23232b; border-color:#4c4c59; }
  .btn.primary { background:var(--accent); border-color:var(--accent); color:white; }
  .registrar { display:flex; align-items:center; justify-content:space-between; gap:10px; }
  .r-title { font-size:18px; font-weight:600; }
  .tiny { font-size:12px; }
  .pill { display:inline-block; padding:3px 8px; border:1px solid #34343f; border-radius:999px; color:#c8c8d6; }
  .selected-line { margin:10px 0 0; }
</style>
</head>
<body>
  <div class="wrap">
    <h1>${title}</h1>
    <p class="muted">Your names: <span id="names-line">${yourNamesText}</span></p>

    <!-- 1) Pick exactly ONE domain -->
    <div class="card">
      <div class="row">
        <div><strong>Choose the domain to buy</strong></div>
        <div class="pill">${displaySuffix}</div>
      </div>
      <div class="domain-list" id="domainList">
        ${fqdns.map((fqdn, i) => `
          <label class="domain-item">
            <input type="radio" name="picked" value="${escapeHtml(fqdn)}" ${i===0 ? "checked" : ""}/>
            <div>${escapeHtml(fqdn)}</div>
          </label>
        `).join("")}
      </div>
      <div id="pickedLine" class="muted selected-line">Selected: <strong>${escapeHtml(selectedDefault)}</strong></div>
    </div>

    <!-- 2) Choose a registrar -->
    <div class="grid" style="margin-top:18px;">
      ${registrars.map(r => `
        <div class="card registrar">
          <div class="r-title">${escapeHtml(r.name)}</div>
          <div class="row" style="gap:8px;">
            <button class="btn"
              data-href="${escapeHtml(r.href)}"
              ${r.searchUrl ? `data-search="${escapeHtml(r.searchUrl)}"` : ""}
              onclick="goRegistrar(this)">Go to registrar</button>
          </div>
        </div>
      `).join("")}
    </div>

    <p class="tiny muted" style="margin-top:12px;">
      Note: some registrars don't provide a public deep link for search — in those cases
      we’ll open their homepage and you can paste the domain.
    </p>
  </div>

<script>
  const radios = Array.from(document.querySelectorAll('input[name="picked"]'));
  const pickedLine = document.getElementById('pickedLine');

  function getPicked() {
    const r = radios.find(x => x.checked);
    return r ? r.value.trim() : "";
  }

  function setPickedLine() {
    const v = getPicked();
    pickedLine.innerHTML = 'Selected: <strong>' + escapeHtml(v) + '</strong>';
  }

  radios.forEach(r => r.addEventListener('change', setPickedLine));
  setPickedLine();

  function goRegistrar(btn) {
    const picked = getPicked();
    if (!picked) {
      alert('Select one domain first.');
      return;
    }

    const base = btn.getAttribute('data-href');
    const tmpl = btn.getAttribute('data-search'); // optional template
    const url = tmpl
      ? tmpl.replace('{DOMAIN}', encodeURIComponent(picked))
      : base;

    window.open(url, '_blank'); // open in new tab
  }

  // tiny client-side version of escape for dynamic line
  function escapeHtml(s) {
    return (s || '').replace(/[&<>"]/g, c =>{
      return ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]);
    });
  }
</script>
</body>
</html>`);
});

// simple health
app.get("/", (_, res) => res.send("KeNIC Option-B API up"));
app.listen(PORT, () => console.log(`KeNIC Option-B API listening on :${PORT}`));

// ---- util for server-side escaping
function escapeHtml(s) {
  return String(s || "").replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
}
