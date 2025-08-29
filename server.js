// server.js — KeNIC TLDs + registrars + landing page + (optional) RDAP checks
require('dotenv').config();
const express = require('express');
const bodyParser = require('body-parser');
const axios = require('axios');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(bodyParser.json());

/* -------------------- KeNIC TLD catalog -------------------- */
// Open registrations:
const OPEN_TLDS = [
  ".ke",
  ".co.ke",
  ".or.ke",
  ".ne.ke",
  ".me.ke",
  ".info.ke",
  ".mobi.ke",
];
// Restricted namespaces:
const RESTRICTED_TLDS = [
  ".ac.ke",
  ".sc.ke",
  ".go.ke",
];
// Normalized set for API
const KENIC_TLDS = [
  ...OPEN_TLDS.map(t => ({ tld: t, restricted: false })),
  ...RESTRICTED_TLDS.map(t => ({ tld: t, restricted: true })),
];

/* -------------------- Registrar directory -------------------- */
const ACCREDITED_REGISTRARS = [
  { name: "HostPinnacle", site: "https://www.hostpinnacle.co.ke/" },
  { name: "Truehost", site: "https://truehost.co.ke/" },
  { name: "EAC Directory (HOSTAFRICA)", site: "https://www.hostafrica.ke/" },
  { name: "Safaricom", site: "https://www.safaricom.co.ke/" },
  { name: "Digital Webframe", site: "https://digitalwebframe.com/" },
  { name: "Movetech Solutions", site: "https://movetechsolutions.com/" },
  { name: "Webhost Kenya", site: "https://webhostkenya.co.ke/" },
  { name: "Softlink Options", site: "https://softlinkoptions.co.ke/" }
];

/* -------------------- Health -------------------- */
app.get('/health', (_req, res) => res.json({ ok: true }));

/* -------------------- TLD list -------------------- */
app.get('/kenic/tlds', (_req, res) => {
  res.json({ tlds: KENIC_TLDS });
});

/* -------------------- Registrar list -------------------- */
app.get('/kenic/registrars', (_req, res) => {
  res.json({ registrars: ACCREDITED_REGISTRARS });
});

/* -------------------- RDAP availability (optional) -------------------- */
// Check full domain via RDAP. 200 => taken, 404 => likely available.
async function rdapCheck(domain) {
  const fqdn = (domain || "").toLowerCase();
  try {
    const url = fqdn.endsWith(".ke")
      ? `https://rdap.kenic.or.ke/domain/${fqdn}`
      : `https://rdap.org/domain/${fqdn}`;
    await axios.get(url, { timeout: 8000 });
    return { domain: fqdn, available: false };           // 200 => registered
  } catch (e) {
    const available = e?.response?.status === 404;       // 404 => free
    return { domain: fqdn, available };
  }
}

// POST { "labels": ["raytechgames","mybrand"], "tld": ".co.ke" }
app.post('/kenic/check-for-tld', async (req, res) => {
  const labels = Array.isArray(req.body.labels) ? req.body.labels : [];
  let tld = String(req.body.tld || "").trim().toLowerCase();
  if (!labels.length || !tld) return res.status(400).json({ error: "labels[] and tld required" });

  tld = tld.replace(/^\./, ""); // ".co.ke" or "co.ke" → "co.ke"

  const unique = [...new Set(labels.map(s => String(s).trim().toLowerCase()).filter(Boolean))];
  const domains = unique.map(l => `${l}.${tld}`);
  const results = await Promise.all(domains.map(rdapCheck));
  res.json({ tld, results });
});

/* -------------------- Landing page -------------------- */
// Render landing using full domains list
function renderLandingHtmlFromDomains(domains) {
  const clean = (domains || [])
    .map(s => String(s).trim().toLowerCase())
    .filter(Boolean);
  const unique = [...new Set(clean)];

  const tlds = new Set();
  unique.forEach(d => {
    const i = d.indexOf('.');
    if (i > 0) tlds.add(d.substring(i)); // ".co.ke"
  });
  const title = (tlds.size === 1)
    ? `Buy ${[...tlds][0]} domains`
    : `Buy selected .KE domains`;

  const namesHtml = unique.length
    ? `<p><strong>Your names:</strong> ${unique.join(', ')}</p>`
    : '';

  const cards = ACCREDITED_REGISTRARS.map(r => `
    <div style="padding:14px;border:1px solid #444;border-radius:12px;margin:10px 0;">
      <div style="font-weight:600;font-size:16px;margin-bottom:8px;">${r.name}</div>
      <a href="${r.site}" target="_blank" rel="noopener" style="
        display:inline-block;padding:10px 16px;border-radius:999px;
        border:1px solid #6b4ce6;text-decoration:none;">
        Go to registrar
      </a>
    </div>
  `).join('');

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${title}</title>
<style>
  :root{color-scheme:light dark;}
  body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;
       max-width:860px;margin:12px auto;padding:0 16px;line-height:1.45}
  h1{font-size:clamp(24px,4vw,32px);letter-spacing:.2px;margin:.4rem 0 1rem}
  .muted{color:#888}
  code{padding:.1em .3em;border-radius:4px;background:#eee}
  @media (prefers-color-scheme: dark){ code{background:#333} }
</style>
</head>
<body>
  <h1>${title}</h1>
  ${namesHtml}
  <p>Select a registrar below to complete your purchase:</p>
  ${cards}
  <p class="muted" style="margin-top:18px;font-size:12px">
    Tip: on the registrar site, search exactly the name shown above
    (e.g. <code>yourname.co.ke</code>).
  </p>
</body></html>`;
}

// Legacy path: build from tld + labels
function renderLandingHtmlFromLabels(tldInput, labels) {
  const safeTld = (tldInput || "").startsWith('.') ? tldInput : `.${tldInput}`;
  const title = `Buy ${safeTld} domains`;

  const clean = (labels || [])
    .map(s => String(s).trim().toLowerCase())
    .filter(Boolean);
  const unique = [...new Set(clean)];

  const namesHtml = unique.length
    ? `<p><strong>Your names:</strong> ${unique.map(l => `${l}${safeTld}`).join(', ')}</p>`
    : '';

  const cards = ACCREDITED_REGISTRARS.map(r => `
    <div style="padding:14px;border:1px solid #444;border-radius:12px;margin:10px 0;">
      <div style="font-weight:600;font-size:16px;margin-bottom:8px;">${r.name}</div>
      <a href="${r.site}" target="_blank" rel="noopener" style="
        display:inline-block;padding:10px 16px;border-radius:999px;
        border:1px solid #6b4ce6;text-decoration:none;">
        Go to registrar
      </a>
    </div>
  `).join('');

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${title}</title>
<style>
  :root{color-scheme:light dark;}
  body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;
       max-width:860px;margin:12px auto;padding:0 16px;line-height:1.45}
  h1{font-size:clamp(24px,4vw,32px);letter-spacing:.2px;margin:.4rem 0 1rem}
  .muted{color:#888}
  code{padding:.1em .3em;border-radius:4px;background:#eee}
  @media (prefers-color-scheme: dark){ code{background:#333} }
</style>
</head>
<body>
  <h1>${title}</h1>
  ${namesHtml}
  <p>Select a registrar below to complete your purchase:</p>
  ${cards}
  <p class="muted" style="margin-top:18px;font-size:12px">
    Tip: on the registrar site, search exactly the name shown above
    (e.g. <code>yourname${safeTld}</code>).
  </p>
</body></html>`;
}

// Prefer ?domains=, fallback to ?tld + ?labels
app.get('/kenic/landing', (req, res) => {
  const domainsParam = String(req.query.domains || '').trim();
  if (domainsParam) {
    const domains = domainsParam.split(',').map(s => s.trim()).filter(Boolean);
    res.set('Content-Type','text/html; charset=utf-8');
    return res.send(renderLandingHtmlFromDomains(domains));
  }

  let tld = String(req.query.tld || '').trim().toLowerCase().replace(/^\./,'');
  if (!tld) return res.status(400).send('Missing ?tld (or supply ?domains=...)');

  const labels = String(req.query.labels || '')
    .split(',')
    .map(s => s.trim())
    .filter(Boolean);

  res.set('Content-Type','text/html; charset=utf-8');
  res.send(renderLandingHtmlFromLabels(tld, labels));
});

/* -------------------- Boot -------------------- */
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`KeNIC backend listening on :${PORT}`));
