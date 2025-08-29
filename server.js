/* server.js */
require('dotenv').config();
const express = require('express');
const bodyParser = require('body-parser');

const app  = express();
const PORT = process.env.PORT || 10000;

app.use(bodyParser.json());

/* -------------------------------------------------------------
 * Registrar directory (correct base paths for WHMCS instances)
 * ----------------------------------------------------------- */
const REGISTRARS = [
  // IMPORTANT: 'base' should be where WHMCS lives (cart.php is there)
  { slug: 'hostpinnacle',
    name: 'HostPinnacle',
    site: 'https://www.hostpinnacle.co.ke/',
    base: 'https://www.hostpinnacle.co.ke/clients' },   // <- /clients

  { slug: 'truehost',
    name: 'Truehost',
    site: 'https://truehost.co.ke/',
    base: 'https://truehost.co.ke/cloud' },             // <- /cloud

  { slug: 'eac-hostafrica',
    name: 'EAC Directory (HOSTAFRICA)',
    site: 'https://hostafrica.ke/',
    base: 'https://portal.hostafrica.ke' },

  { slug: 'safaricom',
    name: 'Safaricom',
    site: 'https://domains.safaricom.co.ke/',
    base: 'https://domains.safaricom.co.ke' },

  { slug: 'digital-webframe',
    name: 'Digital Webframe Solutions',
    site: 'https://digitalwebframe.com/',
    base: 'https://clients.digitalwebframe.com' },

  { slug: 'movetech',
    name: 'Movetech Solutions Ltd',
    site: 'https://movetechsolutions.co.ke/',
    base: 'https://clients.movetechsolutions.co.ke' },

  { slug: 'webhost-kenya',
    name: 'Webhost Kenya',
    site: 'https://webhostkenya.co.ke/',
    base: 'https://clients.webhostkenya.co.ke' },

  { slug: 'softlink-options',
    name: 'Softlink Options Limited',
    site: 'https://softlinkoptions.co.ke/',
    base: 'https://billing.softlinkoptions.co.ke' }
];

/* -------------------------------------------------------------
 * Approved KeNIC TLDs (same as before)
 * ----------------------------------------------------------- */
const TLD_LIST = [
  { tld: '.ke',       restricted: true,  note: '2nd level; restricted' },
  { tld: '.co.ke',    restricted: false },
  { tld: '.or.ke',    restricted: false },
  { tld: '.me.ke',    restricted: false },
  { tld: '.sc.ke',    restricted: true },
  { tld: '.ac.ke',    restricted: true },
  { tld: '.go.ke',    restricted: true },
  { tld: '.info.ke',  restricted: false },
  { tld: '.mobi.ke',  restricted: false },
  { tld: '.ne.ke',    restricted: false }
];

/* -------------------------------------------------------------
 * Deep-link builders
 * ----------------------------------------------------------- */
// Standard WHMCS deep link: opens the cart with the query prefilled
function whmcsLink(base, domain) {
  const root = (base || '').replace(/\/+$/, '');
  return `${root}/cart.php?a=add&domain=register&query=${encodeURIComponent(domain)}`;
}

// Some brands may need custom routes. Add per-slug tweaks here.
function buildDeepLink(reg, domain) {
  const d = (domain || '').trim();
  if (!d) return reg.site;

  switch (reg.slug) {
    // Examples if you later find a brand with a special checker:
    // case 'some-brand':
    //   return `${reg.base.replace(/\/+$/,'')}/domainchecker.php?search=${encodeURIComponent(d)}`;
    default:
      return whmcsLink(reg.base || reg.site, d);
  }
}

/* -------------------------------------------------------------
 * API (unchanged behavior)
 * ----------------------------------------------------------- */
app.get('/', (req, res) => res.send('KeNIC Option-B API OK'));

app.get('/kenic/registrars', (req, res) => {
  res.json({ registrars: REGISTRARS.map(({ slug, name, site }) => ({ slug, name, site })) });
});

app.get('/kenic/tlds', (req, res) => {
  res.json({ tlds: TLD_LIST });
});

/* Landing page (radio pick ONE domain + registrar list) — same as you had */
app.get('/kenic/landing', (req, res) => {
  const raw = (req.query.domains || '').trim();
  const domains = raw ? raw.split(',').map(s => s.trim()).filter(Boolean) : [];
  const selected = domains[0] || '';

  res.setHeader('Content-Type', 'text/html; charset=utf-8');
  res.send(renderLanding(domains, selected));
});

function renderLanding(domains, selected) {
  const esc = s => String(s || '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const radios = domains.length
    ? domains.map(d => `
      <label class="radio-row">
        <input type="radio" name="picked" value="${esc(d)}" ${d===selected?'checked':''}/>
        <span>${esc(d)}</span>
      </label>`).join('')
    : `<p class="muted">(none)</p>`;

  const regs = REGISTRARS.map(r => `
    <div class="reg-row">
      <div class="reg-name">${esc(r.name)}</div>
      <a class="btn" href="#" data-slug="${esc(r.slug)}">Go to registrar</a>
    </div>`).join('');

  return `<!doctype html>
<html lang="en"><head>
<meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Buy .ke domains</title>
<style>
  :root { --bg:#111214; --card:#1b1d21; --text:#e7e7ea; --muted:#a0a0aa; --accent:#7b5cff; }
  body { margin:0; background:var(--bg); color:var(--text); font-family:system-ui,-apple-system,Segoe UI,Roboto,Ubuntu,Cantarell,'Helvetica Neue',Arial,'Noto Sans',sans-serif; }
  .wrap { max-width:1100px; margin:32px auto; padding:0 20px; }
  h1 { font-weight:800; font-size:38px; margin:0 0 8px; }
  .section { background:var(--card); border:1px solid #2a2d33; border-radius:14px; padding:18px; margin-top:16px; }
  .muted { color:var(--muted); }
  .radio-row { display:flex; gap:14px; align-items:center; padding:12px; border:1px solid #2a2d33; border-radius:10px; background:#17191c; margin:8px 0; }
  .reg-row { display:flex; justify-content:space-between; align-items:center; padding:12px; border:1px solid #2a2d33; border-radius:10px; background:#17191c; margin:8px 0; }
  .reg-name { font-weight:600; font-size:18px; }
  .btn { background:var(--accent); color:#fff; text-decoration:none; padding:10px 14px; border-radius:10px; }
</style>
</head><body>
<div class="wrap">
  <h1>Buy .ke domains</h1>

  <div class="section">
    <div class="muted" style="margin-bottom:10px;">Choose the domain to buy</div>
    ${radios}
    <div class="muted" id="pickedLine" style="margin-top:10px;">Selected: ${esc(selected || '(none)')}</div>
  </div>

  <div class="section">
    <div class="muted" style="margin-bottom:10px;">Select a registrar</div>
    ${regs}
  </div>
</div>

<script>
(function(){
  const pickedLine = document.getElementById('pickedLine');
  function getPicked(){ const r=document.querySelector('input[name="picked"]:checked'); return r?r.value:''; }
  document.querySelectorAll('input[name="picked"]').forEach(inp=>{
    inp.addEventListener('change',()=> pickedLine.textContent='Selected: '+(getPicked()||'(none)'));
  });
  function jump(slug, domain){
    if(!domain){ alert('Please pick a domain first.'); return; }
    window.location.href = '/kenic/jump?slug='+encodeURIComponent(slug)+'&domain='+encodeURIComponent(domain);
  }
  document.querySelectorAll('a.btn[data-slug]').forEach(a=>{
    a.addEventListener('click', e=>{ e.preventDefault(); jump(a.dataset.slug, getPicked()); });
  });
})();
</script>
</body></html>`;
}

/* Jump: redirects to the registrar with the domain pre-searched */
app.get('/kenic/jump', (req, res) => {
  const slug   = String(req.query.slug || '').trim();
  const domain = String(req.query.domain || '').trim();
  const reg = REGISTRARS.find(r => r.slug === slug);
  if (!reg) return res.status(404).send('Unknown registrar');
  const url = buildDeepLink(reg, domain);
  return res.redirect(302, url);
});

app.listen(PORT, () => console.log(`KeNIC Option-B API listening on :${PORT}`));
