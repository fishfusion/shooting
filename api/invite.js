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

function extractTextFromResponse(data) {
  if (!data || typeof data !== "object") return "";
  if (typeof data.output_text === "string" && data.output_text.trim()) return data.output_text.trim();
  if (Array.isArray(data.output_text) && data.output_text.length) {
    const joined = data.output_text.map((x) => String(x || "")).join("").trim();
    if (joined) return joined;
  }

  if (Array.isArray(data.output)) {
    const parts = [];
    for (const item of data.output) {
      if (!item || !Array.isArray(item.content)) continue;
      for (const c of item.content) {
        if (!c) continue;
        if (typeof c.text === "string" && c.text.trim()) parts.push(c.text.trim());
        if (typeof c.output_text === "string" && c.output_text.trim()) parts.push(c.output_text.trim());
      }
    }
    const joined = parts.join(" ").trim();
    if (joined) return joined;
  }
  return "";
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
  const mode = String((body && body.mode) || "invite").trim();
  const lastInviteLine = String((body && body.lastInviteLine) || "").trim();
  const lastWarningLine = String((body && body.lastWarningLine) || "").trim();
  if (!input) {
    res.status(400).json({ error: "input is required" });
    return;
  }

  const isWarningMode = mode === "rate_warning";
  const systemPrompt = isWarningMode
    ? "你只输出一句中文短句，用于提醒玩家：开火过快会弹尽粮绝。语气克制、有一点压力感，不要说教，不要解释，不要加引号。"
    : "你只输出一句中文。对话对象是玩家（用“你”），不是Leo本人。核心意思必须是：让玩家下周末叫上Leo，一起来我家吃烧烤。语气自然，有一点哲理，不鸡汤。必须包含“叫上Leo”和“烧烤”，并且必须包含一个明确时间词（例如：下周六/周日/周末/明晚）。禁止以“Leo”开头或直接称呼“Leo，”。";
  const avoidLine = isWarningMode ? lastWarningLine : lastInviteLine;

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
            content: systemPrompt,
          },
          {
            role: "user",
            content: `${input}\n避免重复上一次：${avoidLine || "无"}`,
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
    const text = extractTextFromResponse(data);
    if (!text) {
      res.status(502).json({
        error: "Empty output from OpenAI",
        detail: JSON.stringify(
          {
            id: data && data.id,
            model: data && data.model,
            output_len: Array.isArray(data && data.output) ? data.output.length : 0,
          },
          null,
          0
        ),
      });
      return;
    }

    res.status(200).json({ text });
  } catch (err) {
    res.status(500).json({ error: "Proxy runtime error", detail: String(err && err.message ? err.message : err) });
  }
}
