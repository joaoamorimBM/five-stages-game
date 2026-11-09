// Respostas JSON das Edge Functions. O jogo não precisa de CORS, mas deixamos
// liberado pra dar pra testar pelo navegador.
const CABECALHOS = {
  "Content-Type": "application/json; charset=utf-8",
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
  "Access-Control-Allow-Headers": "content-type",
};

export function responder(status: number, corpo: unknown): Response {
  return new Response(JSON.stringify(corpo), { status, headers: CABECALHOS });
}

export function preflight(): Response {
  return new Response(null, { status: 204, headers: CABECALHOS });
}

// Lê o corpo como JSON respeitando um limite de tamanho.
export async function lerJson(req: Request, limiteBytes: number): Promise<{ json?: unknown; erro?: Response }> {
  const declarado = Number(req.headers.get("content-length") ?? "0");
  if (declarado > limiteBytes) return { erro: responder(413, { erro: "grande_demais" }) };

  const texto = await req.text();
  if (new TextEncoder().encode(texto).length > limiteBytes) return { erro: responder(413, { erro: "grande_demais" }) };

  try {
    return { json: JSON.parse(texto) };
  } catch {
    return { erro: responder(400, { erro: "json_invalido" }) };
  }
}
