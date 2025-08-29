// server.js
require('dotenv').config();
const express = require('express');
const bodyParser = require('body-parser');
const axios = require('axios');

const app = express();
app.use(bodyParser.json());

// ---- KeNIC-approved 2LDs under .KE ----
const KENIC_TLDS = [
  // Open namespaces
  "co.ke","or.ke","me.ke","info.ke","ne.ke","mobi.ke",
  // Typically restricted (you can grey these out in UI)
  "ac.ke","sc.ke","go.ke"
];

// RDAP availability helper
async function rdapCheck(domain) {
  const fqdn = domain.toLowerCase();
  try {
    const url = fqdn.endsWith(".ke")
      ? `https://rdap.kenic.or.ke/domain/${fqdn}`
      : `https://rdap.org/domain/${fqdn}`;
    await axios.get(url, { timeout: 8000 });
    return { domain, available: false };        // 200 => registered
  } catch (e) {
    const code = e?.response?.status;
    return { domain, available: code === 404 }; // 404 => likely free
  }
}

// health
app.get('/health', (_, res) => res.json({ ok: true }));

// list TLD chips
app.get('/kenic/tlds', (req, res) => {
  res.json({
    tlds: KENIC_TLDS.map(t => ({
      tld: t,
      restricted: ["ac.ke","sc.ke","go.ke"].includes(t)
    }))
  });
});

// check many labels under one TLD
// body: { "labels": ["raytechgames","mybrand"], "tld": "co.ke" }
app.post('/kenic/check-for-tld', async (req, res) => {
  const labels = (req.body.labels || [])
    .map(s => String(s).trim().toLowerCase())
    .filter(Boolean);
  const tld = String(req.body.tld || "").trim().toLowerCase();
  if (!labels.length || !tld) return res.status(400).json({ error: "labels[] and tld required" });

  const domains = [...new Set(labels.map(l => `${l}.${tld}`))];
  const results = await Promise.all(domains.map(rdapCheck));
  res.json({ tld, results }); // [{ domain, available }]
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`KeNIC Option-B API listening on :${PORT}`));
