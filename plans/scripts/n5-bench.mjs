// N5 第三批：性能基准（方案第 8 节「性能与安全审查的范围」），先记基准再改，不设先验阈值
// 只在 N5 用，不进 CI。要独占机器：跑之前看有没有别的会话在跑全量
//
//   node nextdocs/scripts/n5-bench.mjs <项> [--out 目录] [--label 标记] [--rounds 3]
//
//   login      登录与 auth.permissions：并发 1、10、50，冷启动与缓存命中各一组
//   lists      用户与日志列表：大租户管理员与受限范围两种身份，一次请求多少条 SQL
//   export     导出 10 万行：耗时、内存峰值、调用次数
//   idle       空载轮询：--minutes 10，按库读扫描次数（--label 区分独立库 0、10 两组）
//   fanout     推送扇出：--connections 500，--instances 1 或 2（两实例要 --redis 连接串）
//   workflow   工作流待办：有无大量待办两组
//   ai         流式首包与检索（要仿真模型端点，延迟扣除 --model-delay）
//   provider   删提供商：所有侧的引用检查耗时
//   frontend   dist 里首屏与懒加载块的 gzip 体积；给 BENCH_PLAYWRIGHT 时再量首屏与路由装配
//   restart    只重启宿主（两实例之后回到单实例）
//   sql        查 N+1：同一列表按页大小 20 与 50 各跑 10 次，去掉后台轮询，看每个请求每条语句执行几次
//
// 环境变量：BENCH_API、BENCH_CONTAINER、BENCH_DB、BENCH_HOST_EXE（宿主可执行文件，login、fanout 要重启它；环境变量沿用当前进程的）、
// BENCH_SUPERADMIN_PASSWORD；造数见 n5-bench-data.mjs
import { execFileSync, spawn } from 'node:child_process';
import fs from 'node:fs';
import { createRequire } from 'node:module';
import path from 'node:path';
import { gzipSync } from 'node:zlib';

const argument = (name, fallback) => {
  const at = process.argv.indexOf(`--${name}`);
  return at > 0 ? process.argv[at + 1] : fallback;
};
const API = process.env.BENCH_API ?? 'http://localhost:9731';
const CONTAINER = process.env.BENCH_CONTAINER ?? 'n5b-pg';
const DB = process.env.BENCH_DB ?? 'n5_bench';
const SUPERADMIN = process.env.BENCH_SUPERADMIN_PASSWORD ?? 'SuperAdmin@123';
const HOST_EXE = process.env.BENCH_HOST_EXE;
const TENANT = 'bench';
const PASSWORD = 'Bench@123456';
const item = process.argv[2];
const label = argument('label', 'default');
const rounds = Number(argument('rounds', 3));
const outDir = argument('out', '.');

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const log = (text) => console.log(`[${new Date().toISOString().slice(11, 19)}] ${text}`);
const psqlOn = (db, sql) => execFileSync('docker', ['exec', '-i', CONTAINER, 'psql', '-U', 'postgres', '-d', db, '-v', 'ON_ERROR_STOP=1', '-qAt'], { input: sql, encoding: 'utf8', maxBuffer: 1 << 28 }).trim();
const psql = (sql) => psqlOn(DB, sql);

async function raw(base, token, method, target, body) {
  const started = performance.now();
  const response = await fetch(`${base}/api/${target}`, { method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  let parsed = null;
  try { parsed = text ? JSON.parse(text) : null; } catch { parsed = text; }
  return { status: response.status, ok: response.ok, body: parsed, ms: performance.now() - started };
}
async function call(token, method, target, body, base = API) {
  const result = await raw(base, token, method, target, body);
  if (!result.ok) throw new Error(`${method} ${target}: ${result.status} ${JSON.stringify(result.body).slice(0, 400)}`);
  return result.body;
}
const loginAs = async (userName, password = PASSWORD, tenant = TENANT, base = API) => (await call(null, 'POST', 'auth:login', { userName, password, ...(tenant ? { tenant } : {}) }, base)).token.accessToken;

function stats(samples) {
  const sorted = [...samples].sort((a, b) => a - b);
  const at = (q) => sorted[Math.min(sorted.length - 1, Math.floor(q * sorted.length))] ?? 0;
  const round = (v) => Math.round(v * 10) / 10;
  return { n: sorted.length, p50: round(at(0.5)), p95: round(at(0.95)), p99: round(at(0.99)), max: round(sorted.at(-1) ?? 0) };
}
const median = (values) => [...values].sort((a, b) => a - b)[Math.floor(values.length / 2)];
// 多轮结果按 p50 取中位数那一轮
const medianRun = (runs) => [...runs].sort((a, b) => a.p50 - b.p50)[Math.floor(runs.length / 2)];
// concurrency 个工人一起跑，共 total 次；失败的计数，不进延迟
async function hit(concurrency, total, work) {
  const samples = [];
  let failures = 0;
  let next = 0;
  const started = performance.now();
  await Promise.all(Array.from({ length: concurrency }, async () => {
    while (next < total) {
      const index = next++;
      const result = await work(index);
      if (result.ok) samples.push(result.ms);
      else failures++;
    }
  }));
  return { ...stats(samples), failures, rps: Math.round((total / (performance.now() - started)) * 10000) / 10 };
}
function write(name, value) {
  fs.mkdirSync(outDir, { recursive: true });
  const file = path.join(outDir, `${name}-${label}.json`);
  fs.writeFileSync(file, JSON.stringify(value, null, 2));
  log(`写入 ${file}`);
}

// ---------- 宿主 ----------
let host = null;
async function waitReady(base) {
  for (let i = 0; i < 90; i++) {
    try {
      if ((await fetch(`${base}/api/auth:login-config`)).ok) return;
    } catch { /* 还没起来 */ }
    await sleep(1000);
  }
  throw new Error(`host at ${base} did not start`);
}
function stopOwnHosts() {
  // 按可执行文件的完整路径停：只停这份构建起的宿主，不碰别的检出
  const script = `Get-CimInstance Win32_Process -Filter "Name='${path.basename(HOST_EXE)}'" | Where-Object { $_.ExecutablePath -eq '${path.resolve(HOST_EXE)}' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }`;
  execFileSync('powershell.exe', ['-NoProfile', '-Command', script], { stdio: 'ignore' });
}
function startHost(base, extraEnv = {}) {
  const child = spawn(HOST_EXE, [], { cwd: path.dirname(HOST_EXE), env: { ...process.env, ...extraEnv }, stdio: 'ignore', detached: true });
  child.unref();
  return waitReady(base).then(() => child);
}
async function restartHost(extraEnv = {}) {
  if (!HOST_EXE) throw new Error('BENCH_HOST_EXE is required');
  stopOwnHosts();
  await sleep(1500);
  host = await startHost(API, extraEnv);
}
function hostPid() {
  const script = `(Get-CimInstance Win32_Process -Filter "Name='${path.basename(HOST_EXE)}'" | Where-Object { $_.ExecutablePath -eq '${path.resolve(HOST_EXE)}' } | Select-Object -First 1).ProcessId`;
  return execFileSync('powershell.exe', ['-NoProfile', '-Command', script], { encoding: 'utf8' }).trim();
}
function workingSetMb(pid) {
  const line = execFileSync('tasklist', ['/FI', `PID eq ${pid}`, '/FO', 'CSV', '/NH'], { encoding: 'utf8' }).trim();
  const kb = Number(line.split('","').at(-1)?.replace(/[^0-9]/g, '') ?? 0);
  return Math.round(kb / 1024);
}

// ---------- SQL ----------
const resetStatements = () => psql('SELECT pg_stat_statements_reset();');
function statements(limit = 15) {
  return psql(`SELECT calls || '|' || round(total_exec_time::numeric, 1) || '|' || rows || '|' || (shared_blks_hit + shared_blks_read) || '|' || left(regexp_replace(query, '\\s+', ' ', 'g'), 220)
    FROM pg_stat_statements s JOIN pg_database d ON d.oid = s.dbid WHERE d.datname LIKE '${DB}%' AND query NOT LIKE '%pg_stat_statements%' ORDER BY total_exec_time DESC LIMIT ${limit}`)
    .split('\n').filter(Boolean).map((line) => {
      const [calls, ms, rows, blocks, query] = line.split('|');
      return { calls: Number(calls), ms: Number(ms), rows: Number(rows), blocks: Number(blocks), query };
    });
}
const statementCalls = () => Number(psql(`SELECT coalesce(sum(calls), 0) FROM pg_stat_statements s JOIN pg_database d ON d.oid = s.dbid WHERE d.datname LIKE '${DB}%' AND query NOT LIKE '%pg_stat_statements%'`));

// ---------- 各项 ----------
async function benchLogin() {
  const result = { cold: [], login: {}, permissions: {} };
  for (let round = 0; round < rounds; round++) {
    await restartHost();
    // 冷启动：刚起的宿主，第一个请求与之后 20 个没进缓存的用户
    const first = await raw(API, null, 'POST', 'auth:login', { userName: 'admin', password: PASSWORD, tenant: TENANT });
    const firstPermissions = await raw(API, first.body.token.accessToken, 'GET', 'auth:permissions');
    const users = [];
    for (let i = 1; i <= 20; i++) users.push((await raw(API, null, 'POST', 'auth:login', { userName: `bench${i}`, password: PASSWORD, tenant: TENANT })));
    const coldPermissions = [];
    for (const user of users) coldPermissions.push((await raw(API, user.body.token.accessToken, 'GET', 'auth:permissions')).ms);
    result.cold.push({ firstLoginMs: Math.round(first.ms), firstPermissionsMs: Math.round(firstPermissions.ms), coldLogin: stats(users.map((u) => u.ms)), coldPermissions: stats(coldPermissions) });
    log(`第 ${round + 1} 轮冷启动：首次登录 ${Math.round(first.ms)} 毫秒，首次 auth.permissions ${Math.round(firstPermissions.ms)} 毫秒`);
  }

  const tokens = [];
  for (let i = 1; i <= 50; i++) tokens.push(await loginAs(`bench${i}`));
  for (const token of tokens) await raw(API, token, 'GET', 'auth:permissions');
  for (const concurrency of [1, 10, 50]) {
    const logins = [];
    const permissions = [];
    for (let round = 0; round < rounds; round++) {
      // 登录每人只来一次：账号都不同，PBKDF2 的开销是真实的
      const offset = 100 + (round * 500);
      logins.push(await hit(concurrency, concurrency * 8, (i) => raw(API, null, 'POST', 'auth:login', { userName: `bench${offset + i}`, password: PASSWORD, tenant: TENANT })));
      permissions.push(await hit(concurrency, concurrency * 40, (i) => raw(API, tokens[i % tokens.length], 'GET', 'auth:permissions')));
    }

    result.login[concurrency] = { median: medianRun(logins), runs: logins };
    result.permissions[concurrency] = { median: medianRun(permissions), runs: permissions };
    log(`并发 ${concurrency}：登录 ${JSON.stringify(result.login[concurrency].median)}；auth.permissions ${JSON.stringify(result.permissions[concurrency].median)}`);
  }

  write('login', result);
}

const LIST_TARGETS = [
  ['users', 'users:list'],
  ['access-logs', 'access-logs:list'],
  ['operation-logs', 'operation-logs:list'],
  ['login-logs', 'login-logs:list'],
  ['diff-logs', 'diff-logs:list'],
  ['permission-change-logs', 'permission-change-logs:list'],
  ['exception-logs', 'exception-logs:list'],
  ['notifications', 'notifications:list'],
  ['inbox', 'inbox:list'],
  ['files', 'files:list'],
];
async function benchLists() {
  const identities = { admin: await loginAs('admin'), limited: await loginAs('limited') };
  const result = {};
  for (const [who, token] of Object.entries(identities)) {
    result[who] = {};
    for (const [name, target] of LIST_TARGETS) {
      const body = { page: { index: 1, size: 20 } };
      const probe = await raw(API, token, 'POST', target, body);
      if (!probe.ok) {
        result[who][name] = { status: probe.status, code: probe.body?.code };
        log(`${who} ${name}：${probe.status} ${probe.body?.code}`);
        continue;
      }

      const runs = [];
      for (let round = 0; round < rounds; round++) runs.push(await hit(1, 20, () => raw(API, token, 'POST', target, body)));
      // 翻到靠后的一页，看分页的开销
      const deep = await raw(API, token, 'POST', target, { page: { index: 200, size: 20 } });
      // 一次请求多少条 SQL
      resetStatements();
      await raw(API, token, 'POST', target, body);
      const calls = statementCalls();
      const top = statements(6);
      result[who][name] = { total: probe.body.total, median: medianRun(runs), deepPageMs: Math.round(deep.ms), sqlPerRequest: calls, top };
      log(`${who} ${name}：共 ${probe.body.total} 行，${JSON.stringify(medianRun(runs))}，靠后一页 ${Math.round(deep.ms)} 毫秒，一次请求 ${calls} 条 SQL`);
    }
  }

  // 列表的查询计划：日志要裁剪到分表，受限范围要走索引。慢查询的完整计划在 auto_explain 的日志里
  write('lists', result);
}

// 后台轮询的语句：发件箱、调度、工作流书签、租户目录、连接复位、分表存在性检查、命中记录
const BACKGROUND = /outbox_message|scheduler_|job_run|job_entity|job_definition|workflow_bookmark_record|tenant_record|DISCARD ALL|relname|pg_tables|sys_condition_hit|lease|information_schema/i;
async function benchSql() {
  const identities = { admin: await loginAs('admin'), limited: await loginAs('limited') };
  const targets = [...LIST_TARGETS, ['tasks', 'tasks:list']];
  const repeat = 10;
  const result = {};
  for (const [who, token] of Object.entries(identities)) {
    result[who] = {};
    for (const [name, target] of targets) {
      const bySize = {};
      for (const size of [20, 50]) {
        const body = { page: { index: 1, size } };
        await raw(API, token, 'POST', target, body);
        resetStatements();
        let rows = 0;
        for (let i = 0; i < repeat; i++) {
          const response = await raw(API, token, 'POST', target, body);
          rows = response.body?.items?.length ?? 0;
        }

        const seen = statements(200).filter((s) => !BACKGROUND.test(s.query));
        const perRequest = seen.reduce((sum, s) => sum + s.calls, 0) / repeat;
        bySize[size] = { rows, perRequest, statements: seen.map((s) => ({ perRequest: s.calls / repeat, msPerCall: Math.round((s.ms / s.calls) * 100) / 100, blocksPerCall: Math.round(s.blocks / s.calls), query: s.query })) };
      }

      // 语句数随返回行数变多就是 N+1：行数没变的不算
      const grew = bySize[50].rows > bySize[20].rows && bySize[50].perRequest > bySize[20].perRequest + 0.5;
      result[who][name] = { ...bySize, nPlusOne: grew };
      log(`${who} ${name}：20 行 ${bySize[20].perRequest} 条，${bySize[50].rows} 行 ${bySize[50].perRequest} 条${grew ? '，随行数增长' : ''}`);
    }
  }

  write('sql', result);
}

async function benchExport() {
  const admin = await loginAs('admin');
  const pid = hostPid();
  const month = new Date();
  month.setUTCDate(1);
  month.setUTCHours(0, 0, 0, 0);
  const runs = [];
  for (let round = 0; round < rounds; round++) {
    resetStatements();
    const before = workingSetMb(pid);
    let peak = before;
    const started = performance.now();
    const task = await call(admin, 'POST', 'export-tasks:submit', {
      businessType: 'access-logs.list',
      format: 'Csv',
      scope: 'SearchResult',
      taskName: `bench-${round}`,
      querySnapshot: JSON.stringify({ page: { index: 1, size: 20 }, filters: [{ field: 'occurredAt', operator: 'ge', value: month.toISOString() }], sorts: [], keyword: null }),
      columns: [{ key: 'occurredAt', title: 'Time' }, { key: 'path', title: 'Path' }, { key: 'statusCode', title: 'Status' }, { key: 'userName', title: 'User' }],
    });
    let state = task;
    while (state.status !== 'Success' && state.status !== 'Failed' && performance.now() - started < 1800000) {
      await sleep(1000);
      peak = Math.max(peak, workingSetMb(pid));
      state = await call(admin, 'GET', `export-tasks/${task.id}`);
    }

    const run = { status: state.status, error: state.errorMessage, rows: state.processedCount, totalCount: state.totalCount, fileMb: Math.round((state.fileSize ?? 0) / 104857.6) / 10, seconds: Math.round((performance.now() - started) / 100) / 10, workingSetBeforeMb: before, workingSetPeakMb: peak, sqlCalls: statementCalls(), top: statements(5), p50: performance.now() - started };
    runs.push(run);
    log(`第 ${round + 1} 轮：${run.status} ${run.rows} 行，${run.seconds} 秒，内存 ${before} → ${peak} MB，${run.sqlCalls} 条 SQL，文件 ${run.fileMb} MB`);
  }

  write('export', { median: medianRun(runs), runs });
}

async function benchIdle() {
  const minutes = Number(argument('minutes', 10));
  const tenants = psql("SELECT count(*) FILTER (WHERE isolation_mode = 1) || '/' || count(*) FROM sys_tenant");
  log(`空载 ${minutes} 分钟，独立库/全部租户 ${tenants}`);
  resetStatements();
  await sleep(minutes * 60000);
  const perDatabase = psql(`SELECT d.datname || '|' || sum(s.calls) || '|' || round(sum(s.total_exec_time)::numeric, 1) || '|' || sum(s.shared_blks_hit + s.shared_blks_read) FROM pg_stat_statements s JOIN pg_database d ON d.oid = s.dbid WHERE query NOT LIKE '%pg_stat_statements%' GROUP BY d.datname ORDER BY sum(s.calls) DESC`)
    .split('\n').filter(Boolean).map((line) => {
      const [database, calls, ms, blocks] = line.split('|');
      return { database, calls: Number(calls), ms: Number(ms), blocks: Number(blocks), callsPerMinute: Math.round(Number(calls) / minutes) };
    });
  const top = statements(30);
  log(`合计 ${perDatabase.reduce((sum, d) => sum + d.calls, 0)} 条 SQL，${perDatabase.length} 个库`);
  for (const row of top.slice(0, 12)) log(`  ${row.calls} 次 ${row.ms} 毫秒 ${row.blocks} 块：${row.query.slice(0, 110)}`);
  write('idle', { minutes, tenants, perDatabase, top });
}

function signalR() {
  const root = path.resolve('frontend-next/node_modules/.pnpm');
  const folder = fs.readdirSync(root).find((f) => f.startsWith('@microsoft+signalr@'));
  return createRequire(path.join(root, folder, 'node_modules/@microsoft/signalr/package.json'))('@microsoft/signalr');
}
async function benchFanout() {
  const connections = Number(argument('connections', 500));
  const instances = Number(argument('instances', 1));
  const redis = argument('redis', null);
  const secondBase = 'http://localhost:9735';
  const extra = instances > 1 ? { XiHan__Bindings__cache__value: redis, XiHan__Caching__Redis__KeyPrefix: 'n5-bench:' } : {};
  if (HOST_EXE) await restartHost(extra);
  if (instances > 1) {
    await startHost(secondBase, { ...extra, Kestrel__Endpoints__Http__Url: secondBase, Kestrel__Endpoints__Grpc__Url: 'http://127.0.0.1:9736' });
  }

  const admin = await loginAs('admin');
  const accounts = psql(`SELECT user_name || '|' || id FROM sys_user WHERE user_name ~ '^bench[0-9]+$' ORDER BY user_name LIMIT ${connections}`).split('\n').map((line) => line.split('|'));
  const { HubConnectionBuilder, HttpTransportType, LogLevel } = signalR();
  const hubs = [];
  let received = [];
  let next = 0;
  log(`建 ${accounts.length} 个连接，${instances} 个实例`);
  const startedConnecting = performance.now();
  await Promise.all(Array.from({ length: 16 }, async () => {
    while (next < accounts.length) {
      const index = next++;
      const token = await loginAs(accounts[index][0]);
      const base = instances > 1 && index % 2 === 1 ? secondBase : API;
      const hub = new HubConnectionBuilder().withUrl(`${base}/hubs/notification`, { accessTokenFactory: () => token, transport: HttpTransportType.WebSockets, skipNegotiation: true }).configureLogging(LogLevel.None).build();
      hub.on('ReceiveNotification', () => received.push(performance.now()));
      await hub.start();
      hubs.push(hub);
    }
  }));
  log(`连接完成，${Math.round(performance.now() - startedConnecting)} 毫秒`);
  const pid = HOST_EXE ? hostPid() : null;
  const memory = pid ? workingSetMb(pid) : null;
  const ids = accounts.map(([, id]) => id);
  const runs = [];
  for (let round = 0; round < Math.max(rounds, 5); round++) {
    received = [];
    const started = performance.now();
    const notification = await call(admin, 'POST', 'notifications', { title: `Fan-out ${round}`, content: 'x', targetType: 'User', targetIds: ids, deliveryChannels: ['InApp'], publishImmediately: true });
    const responded = performance.now() - started;
    const deadline = performance.now() + 60000;
    while (received.length < ids.length && performance.now() < deadline) await sleep(20);
    const latency = received.map((at) => at - started);
    runs.push({ ...stats(latency), delivered: received.length, expected: ids.length, responseMs: Math.round(responded), id: notification.id });
    log(`第 ${round + 1} 轮：送达 ${received.length}/${ids.length}，接口 ${Math.round(responded)} 毫秒，${JSON.stringify(stats(latency))}`);
    await sleep(1000);
  }

  await Promise.all(hubs.map((hub) => hub.stop().catch(() => {})));
  write('fanout', { connections, instances, memoryMb: memory, median: medianRun(runs), runs });
}

async function benchWorkflow() {
  const heavy = await loginAs('limited');
  const light = await loginAs('bench1');
  const result = {};
  for (const [who, token] of [['大量待办', heavy], ['没有待办', light]]) {
    const body = { page: { index: 1, size: 20 } };
    const probe = await raw(API, token, 'POST', 'tasks:list', body);
    if (!probe.ok) throw new Error(`tasks:list ${probe.status} ${JSON.stringify(probe.body).slice(0, 300)}`);
    const runs = [];
    for (let round = 0; round < rounds; round++) runs.push(await hit(1, 10, () => raw(API, token, 'POST', 'tasks:list', body)));
    resetStatements();
    await raw(API, token, 'POST', 'tasks:list', body);
    result[who] = { total: probe.body.total, median: medianRun(runs), sqlPerRequest: statementCalls(), top: statements(6) };
    log(`${who}：共 ${probe.body.total} 条，${JSON.stringify(result[who].median)}，一次请求 ${result[who].sqlPerRequest} 条 SQL`);
  }

  write('workflow', result);
}

async function benchAi() {
  const modelDelay = Number(argument('model-delay', 50));
  const platform = await loginAs('superadmin', SUPERADMIN, null);
  const admin = await loginAs('admin');
  const code = `bench-${Date.now()}`;
  const assistant = await call(platform, 'POST', 'ai-assistants', { code, name: 'Bench', enableKnowledge: true, historyRounds: 2 });
  const opened = await call(admin, 'POST', 'chat-conversations:open-assistant', { assistantId: assistant.id });
  const conversationId = opened.conversationId;
  const { HubConnectionBuilder, HttpTransportType, LogLevel } = signalR();
  const hub = new HubConnectionBuilder().withUrl(`${API}/hubs/chat`, { accessTokenFactory: () => admin, transport: HttpTransportType.WebSockets, skipNegotiation: true }).configureLogging(LogLevel.None).build();
  let firstDelta = null;
  let completed = null;
  hub.on('ChatAssistantDelta', () => { firstDelta ??= performance.now(); });
  hub.on('ChatAssistantCompleted', () => { completed ??= performance.now(); });
  await hub.start();
  await hub.invoke('JoinConversation', conversationId);
  const firsts = [];
  const totals = [];
  for (let i = 0; i < Math.max(rounds, 10); i++) {
    await call(admin, 'POST', 'chat-messages:send', { conversationId, messageType: 'Text', content: `知识库基准问题 ${i}` });
    firstDelta = null;
    completed = null;
    const started = performance.now();
    await call(admin, 'POST', 'chat-messages:reply-assistant', { conversationId, replyId: `bench-${i}` });
    const deadline = performance.now() + 30000;
    while (completed === null && performance.now() < deadline) await sleep(5);
    if (firstDelta !== null) firsts.push(firstDelta - started - (modelDelay * 2));
    if (completed !== null) totals.push(completed - started);
  }

  await hub.stop();
  const search = await hit(1, 30, (i) => raw(API, admin, 'GET', `knowledge-documents:search?question=${encodeURIComponent(`知识库基准 ${i}`)}&top=5`));
  const result = { firstDeltaOverheadMs: stats(firsts), completedMs: stats(totals), searchMs: search, note: `首包扣除了两次模型延迟（向量一次、对话一次），各 ${modelDelay} 毫秒` };
  log(JSON.stringify(result));
  write('ai', result);
}

async function benchProvider() {
  const platform = await loginAs('superadmin', SUPERADMIN, null);
  const sides = psql('SELECT count(*) FROM sys_tenant');
  const samples = [];
  for (let round = 0; round < Math.max(rounds, 5); round++) {
    const provider = await call(platform, 'POST', 'ai-providers', { code: `bench-del-${Date.now()}`, name: 'Delete me', vendor: 'OpenAI', chatModel: 'fake', endpoint: 'https://example.com/v1' });
    resetStatements();
    const deleted = await raw(API, platform, 'DELETE', `ai-providers?ids=${provider.id}`);
    samples.push({ ms: Math.round(deleted.ms), status: deleted.status, succeeded: deleted.body?.succeeded?.length, failed: deleted.body?.failed, sqlCalls: statementCalls() });
    log(`第 ${round + 1} 轮：${JSON.stringify(samples.at(-1))}`);
  }

  write('provider', { sides: Number(sides), medianMs: median(samples.map((s) => s.ms)), samples });
}

function walk(folder) {
  return fs.readdirSync(folder, { withFileTypes: true }).flatMap((entry) => (entry.isDirectory() ? walk(path.join(folder, entry.name)) : [path.join(folder, entry.name)]));
}
async function benchFrontend() {
  const dist = path.resolve('frontend-next/dist');
  const html = fs.readFileSync(path.join(dist, 'index.html'), 'utf8');
  const initial = new Set([...html.matchAll(/(?:src|href)="\/?([^"]+\.(?:js|css))"/g)].map((m) => m[1]));
  const files = walk(dist).filter((f) => /\.(js|css)$/.test(f)).map((file) => {
    const relative = path.relative(dist, file).replaceAll('\\', '/');
    const content = fs.readFileSync(file);
    return { file: relative, bytes: content.length, gzip: gzipSync(content).length, initial: initial.has(relative) };
  }).sort((a, b) => b.gzip - a.gzip);
  const sum = (list) => Math.round(list.reduce((total, f) => total + f.gzip, 0) / 1024);
  const groups = { designer: /workflow|designer|flow|x6|logicflow/i, markdown: /markdown|md-editor|mdeditor|xhmd/i, chart: /chart/i, monaco: /monaco|editor\.api|ts\.worker|css\.worker|html\.worker|json\.worker/i };
  const result = {
    totalGzipKb: sum(files),
    initialGzipKb: sum(files.filter((f) => f.initial)),
    initialFiles: files.filter((f) => f.initial).map((f) => ({ file: f.file, gzipKb: Math.round(f.gzip / 1024) })),
    groups: Object.fromEntries(Object.entries(groups).map(([name, pattern]) => [name, sum(files.filter((f) => pattern.test(f.file)))])),
    largest: files.slice(0, 15).map((f) => ({ file: f.file, gzipKb: Math.round(f.gzip / 1024) })),
    note: '数字是临时 overrides（本地 XiHan.UI）下的',
  };
  log(JSON.stringify({ totalGzipKb: result.totalGzipKb, initialGzipKb: result.initialGzipKb, groups: result.groups }));
  write('frontend', result);
  if (process.env.BENCH_PLAYWRIGHT) {
    const require = createRequire(path.join(process.env.BENCH_PLAYWRIGHT, 'package.json'));
    const { chromium } = require('playwright');
    const base = argument('base', 'http://127.0.0.1:9133');
    const browser = await chromium.launch();
    const timings = [];
    for (let round = 0; round < rounds; round++) {
      const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'zh-CN' });
      const page = await context.newPage();
      const started = performance.now();
      await page.goto(`${base}/#/auth/login`, { waitUntil: 'commit' });
      await page.getByPlaceholder('请输入密码').waitFor({ timeout: 30000 });
      const loginReady = performance.now() - started;
      const paint = await page.evaluate(() => performance.getEntriesByType('paint').find((e) => e.name === 'first-contentful-paint')?.startTime ?? null);
      await page.getByPlaceholder('租户 ID').fill(TENANT);
      await page.getByPlaceholder('请输入用户名或邮箱').fill('admin');
      await page.getByPlaceholder('请输入密码').fill(PASSWORD);
      const submitted = performance.now();
      await page.getByRole('button', { name: '登录' }).click();
      await page.waitForURL((url) => !url.href.includes('/auth/login'), { timeout: 60000 });
      const routed = performance.now() - submitted;
      await page.locator('nav').first().waitFor({ timeout: 30000 });
      await page.waitForLoadState('networkidle').catch(() => {});
      timings.push({ firstContentfulPaintMs: paint === null ? null : Math.round(paint), loginPageReadyMs: Math.round(loginReady), loginToRouteMs: Math.round(routed), loginToIdleMs: Math.round(performance.now() - submitted), landed: page.url().split('#')[1] });
      await context.close();
    }

    await browser.close();
    result.browser = timings;
    log(JSON.stringify(timings));
  }

  write('frontend', result);
}

const benches = { restart: restartHost, sql: benchSql, login: benchLogin, lists: benchLists, export: benchExport, idle: benchIdle, fanout: benchFanout, workflow: benchWorkflow, ai: benchAi, provider: benchProvider, frontend: benchFrontend };
if (!benches[item]) {
  console.error(`未知的项 ${item}，可选：${Object.keys(benches).join('、')}`);
  process.exit(2);
}

await benches[item]();
process.exit(0);
