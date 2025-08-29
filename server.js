/* server.js */
require('dotenv').config();

const express = require('express');
const bodyParser = require('body-parser');

const app  = express();
const PORT = process.env.PORT || 10000;

app.use(bodyParser.json());

/* ------------------------------------------------------------------ */
/* 1) Registrar directory                                              */
/* ------------------------------------------------------------------ */
/* NOTE:
   - 'site' is the marketing site users recognize.
   - 'base' is where WHMCS (or the billing portal) lives. We use it
     to build the deep-link that opens the registrar WITH the domain
     already searched (pricing visible).
   - If you’re unsure of a base, leave it equal to the site; you can
     refine later without changing Unity.
*/
const REGISTRARS = [
  {
    slug: 'hostpinnacle',
    name: 'HostPinnacle',
    site: 'https://www.hostpinnacle.co.ke/',
    base: 'https://www.hostpinnacle.co.ke'      // WHMCS lives here
  },
  {
    slug: 'truehost',
    name: 'Truehost',
    site: 'https://truehost.co.ke/',
    base: 'https://truehost.co.ke'              // WHMCS lives here
  },
  {
    slug: 'eac-hostafrica',
    name: 'EAC Directory (HOSTAFRICA)',
    site: 'https://hostafrica.ke/',
    base: 'https://portal.hostafrica.ke'        // Customer portal
  },
  {
    slug: 'safaricom',
    name: 'Safaricom',
    site: 'https://domains.safaricom.co.ke/',
    base: 'https://domains.safaricom.co.ke'
  },
  {
    slug: 'digital-webframe',
    name: 'Digital Webframe Solutions',
    site: 'https://digitalwebframe.com/',
    base: 'https://clients.digitalwebframe.com'
  },
  {
    slug: 'movetech',
    name: 'Movetech Solutions Ltd',
    site: 'https://movetechsolutions.co.ke/',
    base: 'https://clients.movetechsolutions.co.ke'
  },
  {
    slug: 'webhost-kenya',
    name: 'Webhost Kenya',
    site: 'https://webhostkenya.co.ke/',
    base: 'https://clients.webhostkenya.co.ke'
  },
  {
    slug: 'softlink-options',
    name: 'Softlink Options Limited',
    site: 'https://softlinkoptions.co.ke/',
    base: 'https://billing.softlinkoptions.co.ke'
  }
];

/* ------------------------------------------------------------------ */
/* 2) Approved KeNIC TLDs (short, stable list; extend anytime)        */
/* ------------------------------------------------------------------ */
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

/* ------------------------------------------------------------------ */
/* 3) Helpers to build registrar deep links                            */
/* ------------------------------------------------------------------ */
/* Most Kenyan registrars use WHMCS. This deep link opens the cart with
   the given domain already searched (pricing ready). */
function whmcsLink(base, domain) {
  return `${base.replace(/\/+$/,'')}/cart.php?a=add&domain=register&query=${encodeURIComponent(domain)}`;
}

/* If a registrar needs a special format, add it in this switch. */
function buildDeepLink(reg, domain) {
  const d = (domain || '').trim();
  if (!d) return reg.site;

  switch (reg.slug) {
    // Example custom:
    // case 'some-registrar':
    //   return `${reg.base}/domainchecker.php?search=${encodeURIComponent(d)}`;

    default:
      return whmcsLink(reg.base || reg.site, d);
  }
}

/* ------------------------------------------------------------------ */
/* 4) API endpoints for Unity                                          */
/* ------------------------------------------------------------------ */
app.get('/', (req, res) => res.send('KeNIC Option-B API OK'));

app.get('/kenic/registrars', (req, res) => {
  res.json({ registrars: REGISTRARS.map(({ slug, name, site }) => ({ slug, name, site })) });
});

app.get('/kenic/tlds', (req, res) => {
  res.json({ tlds: TLD_LIST });
});

/* ------------------------------------------------------------------ */
/* 5) Landing page (radio pick ONE domain + registrar list)            */
/* ------------------------------------------------------------------ */
app.get('/kenic/landing', (req, res) => {
  const raw = (req.query.domains || '').trim();
  const domains = raw
    ? raw.split(',').map(s => s.trim()).filter(Boolean)
    : [];
  const preselect = domains[0] || '';

  const page = renderLanding(domains, preselect);
  res.setHeader('Content-Type', 'text/html; charset=utf-8');
  res.send(page);
});

function renderLanding(domains, selected) {
  const esc = s => String(s || '').replace(/[&<>"']/g, c => (
    { '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;' }[c]
  ));

  const radios = domains.length
    ? domains.map(d => `
      <label class="radio-row">
        <input type="radio" name="picked" value="${esc(d)}" ${d===selected?'checked':''} />
        <span>${esc(d)}</span>
      </label>`).join('\n')
    : `<p class="muted">(none)</p>`;

  const regs = REGISTRARS.map(r => `
    <div class="reg-row">
      <div class="reg-name">${esc(r.name)}</div>
      <a class="btn" href="#" data-slug="${esc(r.slug)}">Go to registrar</a>
    </div>`).join('\n');

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Buy .ke domains</title>
<style>
  :root { --bg:#111214; --card:#1b1d21; --text:#e7e7ea; --muted:#a0a0aa; --accent:#7b5cff; }
  * { box-sizing:border-box; }
  body { margin:0; font-family:system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, 'Helvetica Neue', Arial, 'Noto Sans', 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', sans-serif; background:var(--bg); color:var(--text); }
  .wrap { max-width:1100px; margin:32px auto; padding:0 20px; }
  h1 { font-weight:800; font-size:38px; margin:0 0 8px 0; }
  .section { background:var(--card); border:1px solid #2a2d33; border-radius:14px; padding:18px 18px; margin-top:16px; }
  .muted { color:var(--muted); }
  .pill { background:#23252b; color:#ccc; border:1px solid #31343b; padding:6px 10px; border-radius:999px; font-size:13px; }
  .heading { display:flex; gap:10px; align-items:center; margin-bottom:10px; }
  .radio-row { display:flex; align-items:center; gap:14px; padding:12px 12px; border-radius:10px; border:1px solid #2a2d33; background:#17191c; margin:8px 0; }
  .radio-row input { width:18px; height:18px; }
  .reg-row { display:flex; align-items:center; justify-content:space-between; padding:12px 12px; border-radius:10px; border:1px solid #2a2d33; background:#17191c; margin:8px 0; }
  .reg-name { font-weight:600; font-size:18px; }
  .btn { display:inline-block; color:white; text-decoration:none; background:var(--accent); padding:10px 14px; border-radius:10px; }
  .btn:hover { filter:brightness(1.05); }
</style>
</head>
<body>
  <div class="wrap">
    <h1>Buy .ke domains</h1>

    <div class="section">
      <div class="heading">
        <div class="muted">Your names:</div>
        <div>${domains.length ? esc(domains.join(', ')) : '<span class="muted">(none)</span>'}</div>
        <span class="pill">.ke</span>
      </div>

      <div style="margin-top:10px;">
        <div class="muted" style="margin-bottom:10px;">Choose the domain to buy</div>
        ${radios}
        <div class="muted" id="pickedLine" style="margin-top:10px;">Selected: ${esc(selected || '(none)')}</div>
      </div>
    </div>

    <div class="section">
      <div class="muted" style="margin-bottom:10px;">Select a registrar</div>
      ${regs}
    </div>
  </div>

<script>
(function(){
  const pickedLine = document.getElementById('pickedLine');
  function getPicked() {
    const r = document.querySelector('input[name="picked"]:checked');
    return r ? r.value : '';
  }
  document.querySelectorAll('input[name="picked"]').forEach(inp=>{
    inp.addEventListener('change', ()=>{
      pickedLine.textContent = 'Selected: ' + (getPicked() || '(none)');
    });
  });

  function jump(slug, domain) {
    if (!domain) { alert('Please pick a domain first.'); return; }
    const url = '/kenic/jump?slug=' + encodeURIComponent(slug) + '&domain=' + encodeURIComponent(domain);
    window.location.href = url;
  }

  document.querySelectorAll('a.btn[data-slug]').forEach(a=>{
    a.addEventListener('click', (e)=>{
      e.preventDefault();
      jump(a.dataset.slug, getPicked());
    });
  });
})();
</script>
</body>
</html>`;
}

/* ------------------------------------------------------------------ */
/* 6) Jump: redirect to registrar with the domain pre-searched         */
/* ------------------------------------------------------------------ */
app.get('/kenic/jump', (req, res) => {
  const slug   = String(req.query.slug || '').trim();
  const domain = String(req.query.domain || '').trim();

  const reg = REGISTRARS.find(r => r.slug === slug);
  if (!reg) return res.status(404).send('Unknown registrar');

  const url = buildDeepLink(reg, domain);
  return res.redirect(302, url);
});

/* ------------------------------------------------------------------ */

app.listen(PORT, () => {
  console.log(`KeNIC Option-B API listening on :${PORT}`);
});
