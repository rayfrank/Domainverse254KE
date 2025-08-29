// server.js — KeNIC TLD list + simple landing page + (optional) availability check
require('dotenv').config();
const express = require('express');
const bodyParser = require('body-parser');
const axios = require('axios');

const app = express();
app.use(bodyParser.json());

/* ---------- KeNIC namespaces ---------- */
// Open: .ke (second-level), .co.ke, .or.ke, .ne.ke, .me.ke, .info.ke, .mobi.ke
// Restricted: .ac.ke, .sc.ke, .go.ke
const KENIC_TLDS = [
  { tld: ".ke",     restricted: false, note: "Second-level .ke" },
  { tld: ".co.ke",  restricted: false },
  { tld: ".or.ke",  restricted: false },
  { tld: ".ne.ke",  restricted: false },
  { tld: ".me.ke",  restricted: false },
  { tld: ".info.ke",restricted: false },
  { tld: ".mobi.ke",restricted: false },
  { tld: ".ac.ke",  restricted: true  },
  { tld: ".sc.ke",  restricted: true  },
  { tld: ".go.ke",  restricted: true  },
];

/* ---------- Simple list of registrars to show on the landing page ---------- */
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

/* ---------- Health ---------- */
app.get('/health', (_, res) => res.json({ ok: true }));

/* ---------- Return all TLDs ---------- */
app.get('/kenic/tlds', (_, res) => {
  res.json({ tlds: KENIC_TLDS });
});

/* ---------- (Optional) RDAP availability for labels + TLD ---------- */
async function rdapCheck(domain) {
  const fqdn = domain.toLowerCase();
  try {
    const url = fqdn.endsWith(".ke")
      ? `https://rdap.kenic.or.ke/domain/${fqdn}`
      : `https://rdap.org/domain/${fqdn}`;
    await axios.get(url, { timeout: 8000 });
    return { domain, available: false };           // 200 => registered
  } catch (e) {
    return { domain, available: e?.response?.status === 404 }; // 404 => likely free
  }
}

app.post('/kenic/check-for-tld', async (req, res) => {
  const labels = (req.body.labels || [])
    .map(s => String(s).trim().toLowerCase())
    .filter(Boolean);
  let tld = String(req.body.tld || "").trim().toLowerCase();
  if (!labels.length || !tld) return res.status(400).json({ error: "labels[] and tld required" });

  tld = tld.replace(/^\./, ""); // accept ".ke" or "ke"
  const domains = [...new Set(labels.map(l => `${l}.${tld}`))];
  const results = await Promise.all(domains.map(rdapCheck));
  res.json({ tld, results });
});

/* ---------- Minimal HTML landing page per TLD ---------- */
function renderLandingHtml(tld, labels) {
  const safeTld = tld.startsWith('.') ? tld : `.${tld}`;
  const title = `Buy ${safeTld} domains`;
  const labelsHtml = labels && labels.length
    ? `<p><strong>Your names:</strong> ${labels.map(l => `${l}${safeTld}`).join(', ')}</p>`
    : '';

  const cards = ACCREDITED_REGISTRARS.map(r => `
    <div style="padding:14px;border:1px solid #ddd;border-radius:10px;margin:10px 0;">
      <div style="font-weight:600;font-size:16px;margin-bottom:6px;">${r.name}</div>
      <a href="${r.site}" target="_blank" style="
        display:inline-block;padding:10px 16px;border-radius:8px;
        border:1px solid #222;text-decoration:none;">Go to registrar</a>
    </div>
  `).join('');

  return `<!doctype html>
  <html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
  <title>${title}</title></head>
  <body style="font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;max-width:640px;margin:12px auto;padding:0 12px;">
    <h2 style="margin:8px 0 6px 0;">${title}</h2>
    ${labelsHtml}
    <p>Select a registrar below to complete your purchase:</p>
    ${cards}
    <p style="color:#666;margin-top:18px;font-size:12px">
      Tip: on the registrar site, search the exact name shown above (e.g. <code>yourname${safeTld}</code>).
    </p>
  </body></html>`;
}

app.get('/kenic/landing', (req, res) => {
  let tld = String(req.query.tld || '').trim().toLowerCase().replace(/^\./,'');
  if (!tld) return res.status(400).send('Missing ?tld');
  const labels = String(req.query.labels || '')
    .split(',').map(s => s.trim().toLowerCase()).filter(Boolean);
  res.set('Content-Type','text/html; charset=utf-8');
  res.send(renderLandingHtml(tld, labels));
});

/* ---------- Start ---------- */
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`KeNIC Option-B API listening on :${PORT}`));
