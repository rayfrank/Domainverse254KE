// server.js — KeNIC TLDs + availability (Option B)
require('dotenv').config();
const express = require('express');
const bodyParser = require('body-parser');
const axios = require('axios');

const app = express();
app.use(bodyParser.json());

// ---- Full KeNIC namespaces ----
// Open SLDs: .co.ke .or.ke .ne.ke .me.ke .info.ke .mobi.ke
// Restricted SLDs: .ac.ke .sc.ke .go.ke
// Direct .ke (second-level) is also allowed here.
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

app.get('/health', (_, res) => res.json({ ok: true }));

app.get('/kenic/tlds', (_, res) => {
  res.json({ tlds: KENIC_TLDS });
});

async function rdapCheck(domain) {
  const fqdn = domain.toLowerCase();
  try {
    const url = fqdn.endsWith(".ke")
      ? `https://rdap.kenic.or.ke/domain/${fqdn}`
      : `https://rdap.org/domain/${fqdn}`; // (fallback for non-.ke)
    await axios.get(url, { timeout: 8000 });
    return { domain, available: false };     // 200 => registered
  } catch (e) {
    return { domain, available: e?.response?.status === 404 }; // 404 => likely free
  }
}

// body: { labels: ["raytechgames","mybrand"], tld: ".co.ke" | "co.ke" | ".ke" | "ke" }
app.post('/kenic/check-for-tld', async (req, res) => {
  const labels = (req.body.labels || [])
    .map(s => String(s).trim().toLowerCase())
    .filter(Boolean);

  let tld = String(req.body.tld || "").trim().toLowerCase();
  if (!labels.length || !tld) return res.status(400).json({ error: "labels[] and tld required" });

  // accept ".co.ke" or "co.ke"; ".ke" or "ke"
  tld = tld.replace(/^\./, "");

  const domains = [...new Set(labels.map(l => `${l}.${tld}`))];
  const results = await Promise.all(domains.map(rdapCheck));
  res.json({ tld, results }); // [{ domain, available }]
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`KeNIC Option-B API listening on :${PORT}`));
