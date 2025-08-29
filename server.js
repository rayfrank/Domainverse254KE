// server.js
require("dotenv").config();
const express = require("express");
const bodyParser = require("body-parser");

const app = express();
const PORT = process.env.PORT || 10000;

app.use(bodyParser.json());

// ---------------------------------------------------------------------------
// Registrar catalog per suffix.
// If you find a registrar deep-link to prefill the domain, add searchUrl
// with {DOMAIN} placeholder (we URL-encode it in the landing page).
// ---------------------------------------------------------------------------
const registrarsBySuffix = {
  ".ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
    { name: "EAC Directory (HOSTAFRICA)", href: "https://www.hostafrica.ke/" },
    { name: "Safaricom", href: "https://www.safaricom.co.ke/business/cloud-solutions/domains" },
    { name: "Digital Webframe", href: "https://digitalwebframe.com/" },
    { name: "Movetech Solutions Ltd", href: "https://movetechsolutions.com/" },
    { name: "Webhost Kenya", href: "https://webhostkenya.co.ke/" },
    { name: "Softlink Options", href: "https://softlinkoptions.co.ke/" }
  ],
  ".co.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
    { name: "EAC Directory (HOSTAFRICA)", href: "https://www.hostafrica.ke/" },
    { name: "Safaricom", href: "https://www.safaricom.co.ke/business/cloud-solutions/domains" }
  ],
  ".go.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" },
    { name: "EAC Directory (HOSTAFRICA)", href: "https://www.hostafrica.ke/" }
  ],
  ".me.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ],
  ".info.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ],
  ".or.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ],
  ".ac.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ],
  ".sc.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ],
  ".ne.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ],
  ".mobi.ke": [
    { name: "HostPinnacle", href: "https://hostpinnacle.co.ke/" },
    { name: "Truehost", href: "https://truehost.co.ke/", searchUrl: "https://truehost.co.ke/?search={DOMAIN}" }
  ]
};

// ------------------------------- helpers ------------------------------------
function normSuffix(sfx) {
  if (!sfx) return ".ke";
  sfx = sfx.trim();
  return sfx.startsWith(".") ? sfx.toLowerCase() : ("." + sfx.toLowerCase());
}
function toFqdn(name, suffix) {
  if (!name) return null;
  const lower = name.toLowerCase();
  return lower.endsWith(suffix) ? lower : lower + suffix;
}
function splitCsv(csv) {
  if (!csv) return [];
  return String(csv).split(",").map(s => s.trim()).filter(Boolean);
}
function escapeHtml(s) {
  return String(s || "").replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
}

// ----------------------------- JSON for Unity --------------------------------
// Unity: GET /kenic/registrars?suffix=.co.ke
app.get("/kenic/registrars", (req, res) => {
  const suffix = normSuffix(req.query.suffix || ".ke");
  const regs = registrarsBySuffix[suffix] || registrarsBySuffix[".ke"] || [];
  res.json({
    suffix,
    count: regs.length,
    registrars: regs.map(r => ({
      name: r.name,
      href: r.href,
      searchUrl: r.searchUrl || null
    }))
  });
});

// ----------------------------- HTML landing ----------------------------------
// Browser: GET /kenic/landing?names=raytech,raytechgames&suffix=.co.ke
app.get("/kenic/landing", (req, res) => {
  const suffix = normSuffix(req.query.suffix || ".ke");
  const fqdns = splitCsv(req.query.names || "")
    .map(n => toFqdn(n, suffix))
    .filter(Boolean);

  const registrars = registrarsBySuffix[suffix] || registrarsBySuffix[".ke"] || [];
  const title = `Buy ${suffix} domains`;
  const yourNamesText = fqdns.length ? fqdns.join(", ") : "(none)";
  const selectedDefault = fqdns[0] || "";

  res.type("html").send(`<!doctype html>
<html lang="en"><head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>${escapeHtml(title)}</title>
<style>
  :root { --bg:#0f0f12; --card:#17171c; --text:#e8e8ef; --muted:#bcbccd; --accent:#7b61ff; }
  * { box-sizing:border-box; font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, 'Helvetica Neue', Arial; }
  body { margin:0; background:var(--bg); color:var(--text); padding:24px; }
  h1 { margin:0 0 16px 0; font-weight:700; }
  .muted { color: var(--muted); }
  .wrap { max-width:1140px; margin:0 auto; }
  .card { background:var(--card); border:1px solid #2a2a32; border-radius:14px; padding:18px; }
  .row { display:flex; align-items:center; justify-content:space-between; gap:12px; }
  .domain-list { display:grid; grid-template-columns:1fr; gap:10px; }
  .domain-item { display:flex; align-items:center; gap:10px; padding:10px 12px; border:1px solid #2b2b34; border-radius:12px; background:#14141a; }
  .domain-item input[type="radio"]{ width:20px; height:20px; accent-color:var(--accent);}
  .btn { border:1px solid #3a3a44; background:#1c1c22; color:var(--text); border-radius:12px; padding:10px 14px; cursor:pointer;}
  .btn:hover { background:#23232b; border-color:#4c4c59; }
  .registrar { display:flex; align-items:center; justify-content:space-between; gap:10px; }
  .r-title { font-size:18px; font-weight:600; }
  .pill { display:inline-block; padding:3px 8px; border:1px solid #34343f; border-radius:999px; color:#c8c8d6; }
  .selected-line { margin:10px 0 0; }
</style>
</head><body>
<div class="wrap">
  <h1>${escapeHtml(title)}</h1>
  <p class="muted">Your names: <span id="names-line">${escapeHtml(yourNamesText)}</span></p>

  <div class="card">
    <div class="row">
      <div><strong>Choose the domain to buy</strong></div>
      <div class="pill">${escapeHtml(suffix)}</div>
    </div>
    <div class="domain-list" id="domainList">
      ${fqdns.map((fqdn,i)=>`
        <label class="domain-item">
          <input type="radio" name="picked" value="${escapeHtml(fqdn)}" ${i===0?"checked":""}/>
          <div>${escapeHtml(fqdn)}</div>
        </label>`).join("")}
    </div>
    <div id="pickedLine" class="muted selected-line">Selected: <strong>${escapeHtml(selectedDefault)}</strong></div>
  </div>

  <div style="height:12px"></div>

  <div class="card">
    <div style="font-weight:600; margin-bottom:10px;">Select a registrar</div>
    ${registrars.map(r => `
      <div class="registrar" style="margin-bottom:10px;">
        <div class="r-title">${escapeHtml(r.name)}</div>
        <button class="btn" data-href="${escapeHtml(r.href)}" ${r.searchUrl?`data-search="${escapeHtml(r.searchUrl)}"`:""} onclick="goRegistrar(this)">
          Go to registrar
        </button>
      </div>`).join("")}
  </div>

  <p class="muted" style="font-size:12px; margin-top:10px;">
    If a registrar doesn't support deep links, we'll open their homepage and you can paste the domain.
  </p>
</div>

<script>
  const radios=[...document.querySelectorAll('input[name="picked"]')];
  const pickedLine=document.getElementById('pickedLine');

  function getPicked(){ const r=radios.find(x=>x.checked); return r? r.value.trim() : ""; }
  function setPickedLine(){ pickedLine.innerHTML='Selected: <strong>'+escapeHtml(getPicked())+'</strong>'; }
  radios.forEach(r=>r.addEventListener('change', setPickedLine)); setPickedLine();

  function goRegistrar(btn){
    const picked=getPicked();
    if(!picked){ alert('Select one domain first.'); return; }
    const base=btn.getAttribute('data-href');
    const tmpl=btn.getAttribute('data-search');
    const url=tmpl ? tmpl.replace('{DOMAIN}', encodeURIComponent(picked)) : base;
    window.open(url, '_blank');
  }
  function escapeHtml(s){ return (s||'').replace(/[&<>"]/g, c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }
</script>
</body></html>`);
});

// health
app.get("/", (_,res)=>res.send("KeNIC Option-B API up"));
app.listen(PORT, ()=>console.log(`KeNIC Option-B API listening on :${PORT}`));
