// N5 第三批：把一次性库放大到基准规模（方案第 8 节「性能与安全审查的范围」）
// 只在 N5 用，不进 CI，不要指向用得着的库
//
//   node plans/scripts/n5-bench-data.mjs shared [--scale 1]     共库的数据：大租户、100 个共库租户、日志、通知、聊天、流程、文件、发件箱、知识库
//   node plans/scripts/n5-bench-data.mjs isolated [--scale 1]   10 个独立库租户（第二步：空载轮询先量没有独立库的一组）
//
// 环境变量：BENCH_API（宿主地址，缺省 http://localhost:9731）、BENCH_CONTAINER（PostgreSQL 容器）、BENCH_DB（库名）、BENCH_PG_PORT（宿主连库用的端口）、
// BENCH_MODEL（仿真模型端点，缺省 http://127.0.0.1:9734/v1）、BENCH_SUPERADMIN_PASSWORD（超级管理员密码）
// 宿主要已在这个库上跑着，并执行过 schema upgrade 与 seed（打开演示数据）；模型端点见 n5-bench-model.mjs。
// 先走真实接口造每张表的模板行，再按模板行克隆，只换主键、外键、时间与租户列；主键是时间有序的 UUID v7。
import { execFileSync } from 'node:child_process';

const API = process.env.BENCH_API ?? 'http://localhost:9731';
const CONTAINER = process.env.BENCH_CONTAINER ?? 'n5b-pg';
const DB = process.env.BENCH_DB ?? 'n5_bench';
const PG_PORT = process.env.BENCH_PG_PORT ?? '55433';
const MODEL = process.env.BENCH_MODEL ?? 'http://127.0.0.1:9734/v1';
const SUPERADMIN = process.env.BENCH_SUPERADMIN_PASSWORD ?? 'SuperAdmin@123';
const scaleArg = process.argv.indexOf('--scale');
const SCALE = scaleArg > 0 ? Number(process.argv[scaleArg + 1]) : 1;
const n = (value) => Math.max(1, Math.round(value * SCALE));
const stage = process.argv[2] ?? 'shared';

const TENANT = 'bench';
const PASSWORD = 'Bench@123456';

const psqlOn = (db, sql) => execFileSync('docker', ['exec', '-i', CONTAINER, 'psql', '-U', 'postgres', '-d', db, '-v', 'ON_ERROR_STOP=1', '-qAt'], { input: sql, encoding: 'utf8', maxBuffer: 1 << 28 }).trim();
const psql = (sql) => psqlOn(DB, sql);
const step = (text) => console.log(`[${new Date().toISOString().slice(11, 19)}] ${text}`);
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function call(token, method, path, body) {
  const response = await fetch(`${API}/api/${path}`, { method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  let parsed = null;
  try { parsed = text ? JSON.parse(text) : null; } catch { parsed = text; }
  if (!response.ok) throw new Error(`${method} ${path}: ${response.status} ${text.slice(0, 400)}`);
  return parsed;
}
const login = async (userName, password, tenant) => (await call(null, 'POST', 'auth:login', { userName, password, ...(tenant ? { tenant } : {}) })).token.accessToken;
async function eachLimit(items, limit, work) {
  let next = 0;
  await Promise.all(Array.from({ length: limit }, async () => {
    while (next < items.length) await work(items[next++]);
  }));
}

const platform = await login('superadmin', SUPERADMIN);
const editions = await call(platform, 'GET', 'editions');
const enterprise = editions.items.find((e) => e.code === 'enterprise').id;

if (stage === 'isolated') {
  step(`独立库租户 ${n(10)} 个`);
  for (let i = 1; i <= n(10); i++) {
    const id = `iso${i}`;
    const database = `${DB}_${id}`;
    psqlOn('postgres', `DROP DATABASE IF EXISTS "${database}" WITH (FORCE)`);
    psqlOn('postgres', `CREATE DATABASE "${database}"`);
    const tenant = await call(platform, 'POST', 'tenants', { tenantId: id, name: `Isolated ${i}`, editionId: enterprise });
    await call(platform, 'POST', `tenants/${tenant.id}:configure-database`, { isolationMode: 'Database', databaseProvider: 'postgresql', connectionString: `Host=127.0.0.1;Port=${PG_PORT};Database=${database};Username=postgres;Password=postgres` });
    const initialized = await call(platform, 'POST', `tenants/${tenant.id}:initialize-database`, {});
    step(`  ${id}: ${initialized.databaseStatus}`);
  }
  process.exit(0);
}

// ---------- 通过接口造模板行 ----------
step(`共库租户 ${n(100)} 个`);
await eachLimit(Array.from({ length: n(100) }, (_, i) => i + 1), 4, async (i) => {
  await call(platform, 'POST', 'tenants', { tenantId: `t${i}`, name: `Tenant ${i}`, editionId: enterprise });
});

step('大租户与管理员');
const bench = await call(platform, 'POST', 'tenants', { tenantId: TENANT, name: 'Bench', editionId: enterprise, userLimit: 1000000 });
await call(platform, 'POST', `tenants/${bench.id}:initialize-admin`, { userName: 'admin', displayName: 'Bench Admin', password: PASSWORD });
const admin = await login('admin', PASSWORD, TENANT);
const adminId = (await call(admin, 'GET', 'auth:me')).user.id;

step('角色、部门、用户、公告、聊天、流程、文件的模板行');
const role = await call(admin, 'POST', 'roles', { code: 'tpl-role', name: 'Template role', dataScope: 'DepartmentOnly' });
const department = await call(admin, 'POST', 'departments', { code: 'tpl-dept', name: 'Template department' });
const limited = await call(admin, 'POST', 'users', { userName: 'limited', displayName: 'Limited', password: PASSWORD });
await call(admin, 'POST', `roles/${role.id}:assign`, { userIds: [limited.id] });
await call(admin, 'POST', 'memberships', { userId: limited.id, departmentId: department.id, isPrimary: true });
// 受限账号能读这一侧所有读得了的东西，数据范围只有自定义的那些部门
const tenantPermissions = (await call(admin, 'GET', 'auth:permissions')).permissions;
await call(admin, 'POST', `roles/${role.id}:grant`, { permissions: tenantPermissions.filter((code) => code.endsWith('.read')) });
await call(admin, 'PUT', `roles/${role.id}`, { name: 'Template role', description: 'Diff log template', dataScope: 'DepartmentOnly' });
const notice = await call(admin, 'POST', 'notifications', { title: 'Template', content: 'Template', deliveryChannels: ['InApp'] });
await call(admin, 'POST', `notifications/${notice.id}:publish`, {});
const conversation = await call(admin, 'POST', 'chat-conversations:open-single', { peerUserId: limited.id });
await call(admin, 'POST', 'chat-messages:send', { conversationId: conversation.conversationId, messageType: 'Text', content: 'Template message' });
const flowDocument = JSON.stringify({ id: 'x', name: 'bench-flow', displayName: 'Bench flow', category: 'Bench', nodes: [
  { id: 'start', activityType: 'Start' },
  { id: 'approve', activityType: 'UserTask', properties: { AssigneeRules: [{ type: 'User', values: [limited.id] }], Title: 'Approve' } },
  { id: 'end', activityType: 'End' },
], transitions: [
  { id: 't1', sourceNodeId: 'start', targetNodeId: 'approve' },
  { id: 't2', sourceNodeId: 'approve', targetNodeId: 'end' },
] });
const flow = await call(admin, 'POST', 'workflow-definitions', { document: flowDocument });
await call(admin, 'POST', `workflow-definitions/${flow.id}:publish`, {});
await call(admin, 'POST', 'workflow-instances:start', { name: 'bench-flow', displayName: 'Bench flow' });
const form = new FormData();
form.set('file', new Blob(['bench'], { type: 'text/plain' }), 'bench.txt');
const uploaded = await fetch(`${API}/api/files/upload`, { method: 'POST', headers: { Authorization: `Bearer ${admin}` }, body: form });
if (!uploaded.ok) throw new Error(`upload: ${uploaded.status} ${await uploaded.text()}`);
await login('limited', PASSWORD, TENANT);
await call(admin, 'POST', 'users:list', { page: { index: 1, size: 5 } });
// 异常日志第一次写入才建表，没有能稳定触发的入口：暂时把字典表改名，让一个列表真的抛异常
psql('ALTER TABLE sys_dict RENAME TO sys_dict_bench;');
const broken = await fetch(`${API}/api/dicts:list`, { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${admin}` }, body: '{}' });
psql('ALTER TABLE sys_dict_bench RENAME TO sys_dict;');
step(`  触发一条异常日志：${broken.status}`);
await sleep(8000); // 日志写入队列落库

// ---------- 克隆的工具 ----------
psql(`
CREATE OR REPLACE FUNCTION bench_uuid7(ts timestamptz) RETURNS uuid LANGUAGE sql VOLATILE AS $$
  SELECT encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid()) PLACING substring(int8send((extract(epoch FROM ts) * 1000)::bigint) FROM 3) FROM 1 FOR 6), 52, 1), 53, 1), 'hex')::uuid
$$;
`);
const columnsCache = new Map();
function columnInfo(table) {
  if (!columnsCache.has(table)) {
    columnsCache.set(table, psql(`SELECT column_name || '|' || data_type || '|' || is_nullable FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '${table}' ORDER BY ordinal_position`).split('\n').filter(Boolean).map((line) => {
      const [name, type, nullable] = line.split('|');
      return { name, type, nullable };
    }));
  }
  return columnsCache.get(table);
}
const columns = (table) => columnInfo(table).map((c) => c.name);
// 新主键：UUID 列用时间有序的 v7，文本列用 v7 的文本，整数列接着最大值
function newId(table, ts = 'r.ts') {
  const id = columnInfo(table).find((c) => c.name === 'id');
  if (id?.type === 'uuid') return `bench_uuid7(${ts})`;
  if (/int/.test(id?.type ?? '')) return `(SELECT coalesce(max(id), 0) FROM "${table}") + row_number() OVER ()`;
  return `bench_uuid7(${ts})::text`;
}
// 从 source（别名 s，要带 seq 列）克隆：overrides 是「列 → SQL 表达式」，可用 s 的列与 r.ts（这一行的时间）；没覆盖的列抄模板行 t
function cloneFrom(table, template, source, overrides, ts = "now() - random() * interval '90 days'") {
  const cols = columns(table);
  if (cols.length === 0) throw new Error(`no table ${table}`);
  const exprs = cols.map((c) => overrides[c] ?? (['created_at', 'occurred_at'].includes(c) ? 'r.ts' : c === 'id' ? newId(table) : `t."${c}"`));
  psql(`INSERT INTO "${table}" (${cols.map((c) => `"${c}"`).join(',')})
    SELECT ${exprs.join(',')} FROM (${template} LIMIT 1) t CROSS JOIN ${source} CROSS JOIN LATERAL (SELECT (${ts}) + (s.seq * 0) * interval '1 second' AS ts) r;`);
}
const series = (count) => `(SELECT g AS seq FROM generate_series(1, ${count}) g) s`;
const clone = (table, template, count, overrides, ts) => cloneFrom(table, template, series(count), overrides, ts);
const hasRow = (sql) => psql(`SELECT EXISTS (${sql})`) === 't';
const stamp = "replace(gen_random_uuid()::text, '-', '')";

step(`用户：大租户 ${n(8000)}，共库租户合计 ${n(2000)}`);
const userTpl = `SELECT * FROM sys_user WHERE id = '${limited.id}'`;
clone('sys_user', userTpl, n(8000), { user_name: "'bench' || s.seq", display_name: "'用户 ' || s.seq", email: "'bench' || s.seq || '@bench.test'", phone_number: "'139' || lpad(s.seq::text, 8, '0')" });
clone('sys_user', userTpl, n(2000), { user_name: "'other' || s.seq", display_name: "'其他用户 ' || s.seq", email: "'other' || s.seq || '@bench.test'", phone_number: 'NULL', home_tenant_id: `'t' || (s.seq % ${n(100)} + 1)` });
psql(`DROP TABLE IF EXISTS bench_user, bench_other, bench_dept, bench_role, bench_chat, bench_instance;
  CREATE TABLE bench_user AS SELECT id, row_number() OVER (ORDER BY id) AS seq, home_tenant_id FROM sys_user WHERE user_name ~ '^bench[0-9]+$';
  CREATE TABLE bench_other AS SELECT id, home_tenant_id FROM sys_user WHERE user_name ~ '^other[0-9]+$';
  CREATE UNIQUE INDEX ON bench_user (seq);`);
cloneFrom('sys_user_security', `SELECT * FROM sys_user_security WHERE user_id = '${limited.id}'`, '(SELECT id AS user_id, 0 AS seq FROM bench_user UNION ALL SELECT id, 0 FROM bench_other) s', { user_id: 's.user_id' }, 'now()');

step(`部门 ${n(500)}（6 层，含闭包表）`);
const fanout = 4; // 四叉树：500 个部门正好落在 6 层
psql(`CREATE TABLE bench_dept AS SELECT bench_uuid7(now() + g * interval '1 millisecond') AS id, g AS seq, CASE WHEN g = 1 THEN NULL ELSE (g - 2) / ${fanout} + 1 END AS parent_seq FROM generate_series(1, ${n(500)}) g;`);
const deptCols = columns('sys_department');
psql(`INSERT INTO sys_department (${deptCols.map((c) => `"${c}"`).join(',')})
  SELECT ${deptCols.map((c) => (c === 'id' ? 'd.id' : c === 'code' ? "'d' || d.seq" : c === 'name' ? "'部门 ' || d.seq" : c === 'parent_id' ? 'p.id' : c === 'created_at' ? 'now()' : `t."${c}"`)).join(',')}
  FROM (SELECT * FROM sys_department WHERE id = '${department.id}') t CROSS JOIN bench_dept d LEFT JOIN bench_dept p ON p.seq = d.parent_seq;
  WITH RECURSIVE chain AS (
    SELECT id AS ancestor, id AS descendant, 0 AS depth, parent_seq FROM bench_dept
    UNION ALL SELECT p.id, c.descendant, c.depth + 1, p.parent_seq FROM chain c JOIN bench_dept p ON p.seq = c.parent_seq
  )
  INSERT INTO sys_department_hierarchy (id, ancestor_id, descendant_id, depth, tenant_id, concurrency_stamp)
  SELECT bench_uuid7(now()), ancestor, descendant, depth, '${TENANT}', ${stamp} FROM chain;`);
step(`  最深 ${psql("SELECT max(depth) + 1 FROM sys_department_hierarchy WHERE tenant_id = 'bench'")} 层`);

step(`角色 ${n(50)}（每个 100 条授予），用户各配 2 个角色与 1 个部门`);
clone('sys_role', `SELECT * FROM sys_role WHERE id = '${role.id}'`, n(50), { code: "'r' || s.seq", name: "'角色 ' || s.seq" }, 'now()');
const grantId = newId('permission_grant_record', 'now()');
psql(`CREATE TABLE bench_role AS SELECT id, row_number() OVER (ORDER BY id) - 1 AS seq FROM sys_role WHERE owner_tenant_id = '${TENANT}' AND code ~ '^r[0-9]+$';
  INSERT INTO permission_grant_record (id, subject_kind, subject_id, permission, tenant_id, created_at)
  SELECT ${grantId}, t.subject_kind, r.id::text, p.permission, '${TENANT}', now()
  FROM (SELECT subject_kind FROM permission_grant_record WHERE subject_id = '${role.id}' LIMIT 1) t
  CROSS JOIN bench_role r
  CROSS JOIN (VALUES ${tenantPermissions.slice(0, 100).map((code) => `('${code}')`).join(',')}) p(permission)
  ON CONFLICT DO NOTHING;
  INSERT INTO sys_user_role (id, owner_tenant_id, user_id, role_id, created_at, concurrency_stamp)
  SELECT bench_uuid7(now()), '${TENANT}', u.id, r.id, now(), ${stamp}
  FROM bench_user u JOIN bench_role r ON r.seq = u.seq % ${n(50)} OR r.seq = (u.seq + ${Math.floor(n(50) / 2)}) % ${n(50)}
  ON CONFLICT DO NOTHING;`);
cloneFrom('sys_user_department', `SELECT * FROM sys_user_department WHERE user_id = '${limited.id}'`, `(SELECT u.id AS user_id, d.id AS department_id, 0 AS seq FROM bench_user u JOIN bench_dept d ON d.seq = u.seq % ${n(500)} + 1) s`, { user_id: 's.user_id', department_id: 's.department_id' }, 'now()');
// 一个角色自定义 200 个部门（量 IN 列表），limited 只持有这个角色
const customDepartments = psql(`SELECT id FROM bench_dept WHERE seq <= ${n(200)}`).split('\n').map((departmentId) => ({ departmentId, includeChildren: false }));
await call(admin, 'PUT', `roles/${role.id}`, { name: 'Template role', dataScope: 'Custom', customDepartments });

step(`日志：操作、访问、登录、异常、差异、权限变更各 ${n(300000)}（3 个月）`);
const months = [0, 1, 2].map((back) => { const d = new Date(); d.setUTCDate(1); d.setUTCMonth(d.getUTCMonth() - back); return d; });
const suffix = (d) => `${d.getUTCFullYear()}${String(d.getUTCMonth() + 1).padStart(2, '0')}01`;
function syntheticDefault({ name, type, nullable }) {
  if (name === 'tenant_id') return `'${TENANT}'`;
  if (name === 'id') return type === 'uuid' ? 'gen_random_uuid()' : "replace(gen_random_uuid()::text, '-', '')";
  if (nullable === 'YES') return 'NULL';
  if (/timestamp/.test(type)) return 'now()';
  if (/uuid/.test(type)) return 'gen_random_uuid()';
  if (/bool/.test(type)) return 'false';
  if (/int|numeric|double|real/.test(type)) return '0';
  return "'bench'";
}
function ensureTemplate(table) {
  // 异常日志没有能稳定触发的入口：没有模板行时按列类型合成一行，只用于量规模
  if (hasRow(`SELECT 1 FROM "${table}" WHERE tenant_id = '${TENANT}'`)) return;
  const rows = columnInfo(table);
  psql(`INSERT INTO "${table}" (${rows.map((c) => `"${c.name}"`).join(',')}) VALUES (${rows.map(syntheticDefault).join(',')});`);
  step(`  ${table} 没有模板行，按列类型合成`);
}
const logShape = psql("SELECT string_agg(tablename, ',') FROM pg_tables WHERE schemaname = 'public' AND tablename ~ '^sys_[a-z_]+_log_2[0-9]+$'").split(',').filter(Boolean);
step(`  已有分表：${logShape.join(' ')}`);
for (const log of ['sys_access_log', 'sys_login_log', 'sys_diff_log', 'sys_permission_change_log', 'sys_exception_log']) {
  const current = `${log}_${suffix(months[0])}`;
  const shape = logShape.find((t) => t.startsWith(`${log}_`));
  if (!shape) {
    step(`  ${log}：还没有物理表，跳过`);
    continue;
  }
  psql(`CREATE TABLE IF NOT EXISTS "${current}" (LIKE "${shape}" INCLUDING ALL);`);
  columnsCache.delete(current);
  ensureTemplate(current);
  const each = Math.round(n(300000) / 3);
  for (const month of months) {
    const physical = `${log}_${suffix(month)}`;
    psql(`CREATE TABLE IF NOT EXISTS "${physical}" (LIKE "${current}" INCLUDING ALL);`);
    columnsCache.delete(physical);
    const start = `timestamp '${month.toISOString().slice(0, 10)}'`;
    clone(physical, `SELECT * FROM "${current}" WHERE tenant_id = '${TENANT}'`, each, {}, `least(${start} + random() * interval '30 days', (now() AT TIME ZONE 'UTC')::timestamp)`);
  }
}
const auditCols = columns('audit_record');
psql(`INSERT INTO audit_record (${auditCols.map((c) => `"${c}"`).join(',')})
  SELECT ${auditCols.map((c) => (c === 'id' ? newId('audit_record', 'now()') : c === 'sequence' ? '(SELECT max(sequence) FROM audit_record) + g' : c === 'timestamp' ? "now() - random() * interval '90 days'" : `t."${c}"`)).join(',')}
  FROM (SELECT * FROM audit_record WHERE tenant_id = '${TENANT}' LIMIT 1) t CROSS JOIN generate_series(1, ${n(300000)}) g;`);

step(`通知 ${n(1000)}、收件人 ${n(500000)}`);
clone('sys_notification', `SELECT * FROM sys_notification WHERE id = '${notice.id}'`, n(1000), { title: "'公告 ' || s.seq" });
const perNotice = Math.max(1, Math.round(n(500000) / n(1000)));
const noticeBuckets = Math.max(1, Math.round(n(8000) / perNotice));
cloneFrom('sys_user_notification', `SELECT * FROM sys_user_notification WHERE notification_id = '${notice.id}'`, `(SELECT n.id AS notification_id, u.id AS user_id, 0 AS seq FROM (SELECT id, row_number() OVER (ORDER BY id) AS rn FROM sys_notification WHERE title LIKE '公告 %') n JOIN bench_user u ON u.seq % ${noticeBuckets} = n.rn % ${noticeBuckets}) s`, { notification_id: 's.notification_id', user_id: 's.user_id' });
step(`  实际 ${psql('SELECT count(*) FROM sys_user_notification')} 条`);

step(`聊天：${n(5000)} 个会话、${n(200000)} 条消息`);
const conversationId = conversation.conversationId;
const perConversation = Math.max(1, Math.round(n(200000) / n(5000)));
clone('sys_chat_conversation', `SELECT * FROM sys_chat_conversation WHERE id = '${conversationId}'`, n(5000), { pair_key: "'bench-' || s.seq", last_seq: String(perConversation), member_count: '2' });
psql("CREATE TABLE bench_chat AS SELECT id, row_number() OVER (ORDER BY id) AS seq FROM sys_chat_conversation WHERE pair_key LIKE 'bench-%';");
// 成员分散在大租户的用户里；每 100 个会话有一个算到 admin 头上，它的会话列表是 50 个
cloneFrom('sys_chat_conversation_member', `SELECT * FROM sys_chat_conversation_member WHERE conversation_id = '${conversationId}'`, `(SELECT c.id AS conversation_id, CASE WHEN c.seq % 100 = 0 THEN '${adminId}'::uuid ELSE u1.id END AS user_id, 0 AS seq FROM bench_chat c JOIN bench_user u1 ON u1.seq = c.seq % ${n(8000)} + 1 UNION ALL SELECT c.id, u2.id, 0 FROM bench_chat c JOIN bench_user u2 ON u2.seq = (c.seq * 7 + 3) % ${n(8000)} + 1) s`, { conversation_id: 's.conversation_id', user_id: 's.user_id' });
cloneFrom('sys_chat_message', `SELECT * FROM sys_chat_message WHERE conversation_id = '${conversationId}'`, `(SELECT c.id AS conversation_id, g AS seq FROM bench_chat c CROSS JOIN generate_series(1, ${perConversation}) g) s`, { conversation_id: 's.conversation_id', seq: 's.seq', content: "'消息 ' || s.seq", client_message_id: 'NULL' });

step(`工作流：${n(10000)} 个实例（一半已结束）、${n(5000)} 个待办（都在 limited 名下）、${n(2000)} 个定时书签`);
const instanceTpl = "SELECT * FROM workflow_instance_record WHERE definition_name = 'bench-flow'";
const templateInstanceId = psql(`SELECT id FROM (${instanceTpl}) x LIMIT 1`);
const castOf = (table) => (columnInfo(table).find((c) => c.name === 'document')?.type === 'jsonb' ? '::jsonb' : '');
const maxSequence = (table) => `(SELECT coalesce(max(sequence), 0) FROM ${table})`;
psql(`CREATE TABLE bench_instance AS SELECT lower(replace(bench_uuid7(now() + g * interval '1 millisecond')::text, '-', '')) AS id, g AS seq FROM generate_series(1, ${n(10000)}) g;`);
psql(`INSERT INTO workflow_instance_record (${columns('workflow_instance_record').map((c) => `"${c}"`).join(',')})
  SELECT ${columns('workflow_instance_record').map((c) => {
    if (c === 'id') return 'i.id';
    if (c === 'title') return "'流程 ' || i.seq";
    if (c === 'sequence') return `${maxSequence('workflow_instance_record')} + i.seq`;
    if (c === 'created_at') return "now() - random() * interval '90 days'";
    if (c === 'document') return `replace(t.document::text, t.id, i.id)${castOf('workflow_instance_record')}`;
    if (c === 'status') return `CASE WHEN i.seq <= ${n(5000)} THEN t.status ELSE 'Completed' END`;
    if (c === 'ended_at') return `CASE WHEN i.seq <= ${n(5000)} THEN NULL ELSE now() END`;
    return `t."${c}"`;
  }).join(',')}
  FROM (${instanceTpl} LIMIT 1) t CROSS JOIN bench_instance i;`);
const bookmarkTpl = `SELECT * FROM workflow_bookmark_record WHERE instance_id = '${templateInstanceId}'`;
const insertBookmarks = (count, offset, extra) => psql(`INSERT INTO workflow_bookmark_record (${columns('workflow_bookmark_record').map((c) => `"${c}"`).join(',')})
  SELECT ${columns('workflow_bookmark_record').map((c) => {
    if (extra[c]) return extra[c];
    if (c === 'id') return "lower(replace(gen_random_uuid()::text, '-', ''))";
    if (c === 'instance_id') return 'i.id';
    if (c === 'search_title') return "'待办 ' || i.seq";
    if (c === 'sequence') return `${maxSequence('workflow_bookmark_record')} + i.seq`;
    if (c === 'created_at') return "now() - random() * interval '90 days'";
    if (c === 'document') return `replace(replace(t.document::text, t.instance_id, i.id), t.id, lower(replace(gen_random_uuid()::text, '-', '')))${castOf('workflow_bookmark_record')}`;
    return `t."${c}"`;
  }).join(',')}
  FROM (${bookmarkTpl} LIMIT 1) t JOIN bench_instance i ON i.seq > ${offset} AND i.seq <= ${offset + count};`);
insertBookmarks(n(5000), 0, {});
insertBookmarks(n(2000), n(5000), { kind: "'bench-timer'", key: "'bench-timer-' || i.seq", due_at: "now() + interval '30 days'" });

step(`文件 ${n(20000)}、发件箱 ${n(50000)}（到期时间在 30 天后，空载轮询量的是待派发，不是真派发）`);
clone('sys_file', `SELECT * FROM sys_file WHERE tenant_id = '${TENANT}' ORDER BY created_at DESC`, n(20000), { name: "'file' || s.seq || '.txt'", original_name: "'file' || s.seq || '.txt'", hash: 'md5(s.seq::text)' });
if (!hasRow('SELECT 1 FROM outbox_message')) {
  const outbox = columnInfo('outbox_message');
  psql(`INSERT INTO outbox_message (${outbox.map((c) => `"${c.name}"`).join(',')}) VALUES (${outbox.map((c) => (c.name === 'topic' ? "'bench.noop'" : c.name === 'payload' ? (c.type === 'bytea' ? "'\\x7b7d'::bytea" : "'{}'") : c.name === 'created_at' ? 'now()' : c.name === 'next_attempt_at' ? "now() + interval '30 days'" : c.name === 'attempts' ? '0' : syntheticDefault(c))).join(',')});`);
}
clone('outbox_message', 'SELECT * FROM outbox_message ORDER BY created_at DESC', n(50000), { topic: "'bench.noop'", dispatched_at: 'NULL', dead_lettered_at: 'NULL', attempts: '0', last_error: 'NULL', next_attempt_at: "now() + interval '30 days'" }, 'now()');

step(`知识库 ${n(500)} 篇，向量走仿真端点`);
const provider = await call(platform, 'POST', 'ai-providers', { code: 'bench', name: 'Bench', vendor: 'OpenAI', chatModel: 'fake', embeddingModel: 'fake', endpoint: MODEL, apiKey: 'bench', isDefault: true });
const paragraph = '知识库基准用的正文段落，内容本身不重要，长度决定切块数量。'.repeat(14);
let chunks = 0;
await eachLimit(Array.from({ length: n(500) }, (_, i) => i + 1), 8, async (i) => {
  const text = Array.from({ length: 20 }, (_, p) => `第 ${i} 篇第 ${p} 段。${paragraph}`).join('\n\n');
  const document = await call(admin, 'POST', 'knowledge-documents:ingest', { title: `文档 ${i}`, text });
  chunks += document.chunkCount;
});
step(`  ${chunks} 块，提供商 ${provider.id}`);

psql('ANALYZE;');
const counts = psql("SELECT relname || '=' || n_live_tup FROM pg_stat_user_tables WHERE n_live_tup > 500 ORDER BY n_live_tup DESC LIMIT 30").split('\n').join(' ');
step(`完成：${counts}`);
