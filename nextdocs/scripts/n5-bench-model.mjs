// N5 第三批：本机仿真 OpenAI 端点（/v1/models、/v1/chat/completions、/v1/embeddings），固定延迟，只量应用本身的开销
//
//   node nextdocs/scripts/n5-bench-model.mjs [--port 9734] [--delay 50] [--dims 1536]
import http from 'node:http';

const argument = (name, fallback) => {
  const at = process.argv.indexOf(`--${name}`);
  return at > 0 ? Number(process.argv[at + 1]) : fallback;
};
const port = argument('port', 9734);
const delay = argument('delay', 50);
const dims = argument('dims', 1536);
const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const read = (request) => new Promise((resolve) => {
  let body = '';
  request.on('data', (chunk) => { body += chunk; });
  request.on('end', () => resolve(body));
});
const json = (response, value) => {
  response.writeHead(200, { 'Content-Type': 'application/json' });
  response.end(JSON.stringify(value));
};
const base = { id: 'chatcmpl-bench', object: 'chat.completion.chunk', created: 1, model: 'fake' };

http.createServer(async (request, response) => {
  const raw = request.method === 'POST' ? await read(request) : '';
  const body = raw ? JSON.parse(raw) : {};
  if (request.url.endsWith('/models')) {
    json(response, { object: 'list', data: [{ id: 'fake', object: 'model' }] });
    return;
  }

  if (request.url.endsWith('/embeddings')) {
    const inputs = Array.isArray(body.input) ? body.input : [body.input];
    await wait(delay);
    json(response, { object: 'list', model: 'fake', usage: { prompt_tokens: 1, total_tokens: 1 }, data: inputs.map((text, index) => ({ object: 'embedding', index, embedding: Array.from({ length: dims }, (_, d) => Math.sin(String(text).length + d)) })) });
    return;
  }

  if (!request.url.endsWith('/chat/completions')) {
    response.writeHead(404);
    response.end();
    return;
  }

  await wait(delay);
  if (body.stream !== true) {
    json(response, { id: 'chatcmpl-bench', object: 'chat.completion', created: 1, model: 'fake', choices: [{ index: 0, message: { role: 'assistant', content: '仿真应答' }, finish_reason: 'stop' }], usage: { prompt_tokens: 1, completion_tokens: 1, total_tokens: 2 } });
    return;
  }

  response.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-cache' });
  for (const word of ['这是', '仿真', '端点', '的', '应答']) {
    response.write(`data: ${JSON.stringify({ ...base, choices: [{ index: 0, delta: { role: 'assistant', content: word }, finish_reason: null }] })}\n\n`);
    await wait(5);
  }

  response.write(`data: ${JSON.stringify({ ...base, choices: [{ index: 0, delta: {}, finish_reason: 'stop' }], usage: { prompt_tokens: 3, completion_tokens: 5, total_tokens: 8 } })}\n\n`);
  response.write('data: [DONE]\n\n');
  response.end();
}).listen(port, '127.0.0.1', () => console.log(`仿真模型端点 http://127.0.0.1:${port}/v1，延迟 ${delay} 毫秒`));
