function setCors(res) {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");
}

async function parseBody(req) {
  if (req.body && typeof req.body === "object") return req.body;
  const chunks = [];
  for await (const chunk of req) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString("utf8").trim();
  if (!raw) return {};
  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

export default async function handler(req, res) {
  setCors(res);
  if (req.method === "OPTIONS") {
    res.status(204).end();
    return;
  }

  if (req.method === "GET") {
    res.status(200).json({
      ok: true,
      service: "invite-proxy",
      hasOpenAIKey: Boolean(process.env.OPENAI_API_KEY),
    });
    return;
  }

  if (req.method !== "POST") {
    res.status(405).json({ error: "Method not allowed" });
    return;
  }

  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey) {
    res.status(500).json({ error: "OPENAI_API_KEY is not set on server" });
    return;
  }

  const body = await parseBody(req);
  const input = String((body && body.input) || "").trim();
  const lastInviteLine = String((body && body.lastInviteLine) || "").trim();
  if (!input) {
    res.status(400).json({ error: "input is required" });
    return;
  }

  try {
    const r = await fetch("https://api.openai.com/v1/responses", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${apiKey}`,
      },
      body: JSON.stringify({
        model: "gpt-4o-mini",
        input: [
          {
            role: "system",
            content:
              "你只输出一句中文。要有一点哲理，但自然口语。必须包含“Leo”和“烧烤”，并且表达邀请Leo来我家吃烧烤。",
          },
          {
            role: "user",
            content: `${input}\n避免重复上一次：${lastInviteLine || "无"}`,
          },
        ],
        temperature: 1.05,
        max_output_tokens: 80,
      }),
    });

    if (!r.ok) {
      const msg = await r.text();
      res.status(502).json({ error: "OpenAI request failed", detail: msg.slice(0, 400) });
      return;
    }

    const data = await r.json();
    const text = String(data.output_text || "").trim();
    if (!text) {
      res.status(502).json({ error: "Empty output from OpenAI" });
      return;
    }

    res.status(200).json({ text });
  } catch (err) {
    res.status(500).json({ error: "Proxy runtime error", detail: String(err && err.message ? err.message : err) });
  }
}
