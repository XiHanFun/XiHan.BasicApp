// N5 第一批：生成对齐矩阵的两侧清单与各组草稿（方案第 8 节「N5 对齐与收口的做法」）
// 只在 N5 用，不进 CI；4.x 目录在 N6 删除之后，plans/alignment/*.json 是矩阵依据的快照
//
//   node plans/scripts/n5-inventory.mjs [--blocks <草稿目录>]
//
// 对应关系是第一遍候选，结论由人工核对，名字对上不等于对齐
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const argBlocks = process.argv.indexOf('--blocks');
const blocksDir = argBlocks > 0 ? path.resolve(process.argv[argBlocks + 1]) : null;
const outDir = path.join(root, 'plans/alignment');

const walk = (dir, filter, found = []) => {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (['bin', 'obj', 'node_modules', 'dist', 'artifacts'].includes(entry.name)) continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full, filter, found);
    else if (filter(full)) found.push(full);
  }
  return found;
};
const read = (file) => fs.readFileSync(path.join(file.startsWith(root) ? '' : root, file), 'utf8');
const rel = (file) => path.relative(root, file).replace(/\\/g, '/');
// 4.x 的文档在 N5 第二批切走了，从标签读
const read4x = (file) => execFileSync('git', ['show', `v5.9.1:${file}`], { cwd: root, encoding: 'utf8', maxBuffer: 1 << 26 });

// ---------- 4.x ----------

function featureRows() {
  const rows = [];
  let section = '';
  for (const line of read4x('docs/features.md').split(/\r?\n/)) {
    if (line.startsWith('## ')) section = line.slice(3);
    const hit = line.match(/^- \*\*(.+?)\*\*：(.*)$/);
    if (hit) rows.push({ section, name: hit[1], text: hit[2] });
  }
  return rows;
}

function permissionConstants() {
  const constants = {};
  for (const file of walk(path.join(root, 'backend/src'), (p) => /PermissionCodes\.cs$/.test(p))) {
    const stack = [];
    let depth = 0;
    for (const line of fs.readFileSync(file, 'utf8').split(/\r?\n/)) {
      const cls = line.match(/\bclass\s+(\w+)/);
      if (cls) stack.push({ name: cls[1], depth });
      const before = depth;
      const value = line.match(/public const string (\w+)\s*=\s*"([^"]+)"/);
      if (value) {
        const names = stack.map((s) => s.name);
        constants[`${names.join('.')}.${value[1]}`] = value[2];
        constants[`${names.at(-1)}.${value[1]}`] = value[2];
      }
      depth += (line.match(/\{/g) || []).length - (line.match(/\}/g) || []).length;
      if (depth < before) while (stack.length && depth <= stack.at(-1).depth) stack.pop();
    }
  }
  return constants;
}

const constants = permissionConstants();
const resolvePermission = (text) => {
  if (!text) return null;
  const value = text.trim();
  if (value === 'null') return null;
  if (value.startsWith('"')) return value.replace(/"/g, '');
  return constants[value] ?? constants[value.split('.').slice(-2).join('.')] ?? `?${value}`;
};
const splitArguments = (text) => {
  const parts = [];
  let current = '';
  let quoted = false;
  for (const ch of text) {
    if (ch === '"') quoted = !quoted;
    if (ch === ',' && !quoted) {
      parts.push(current.trim());
      current = '';
    } else current += ch;
  }
  if (current.trim()) parts.push(current.trim());
  return parts;
};
const literal = (text) => (text === undefined || text === 'null' ? null : text.replace(/"/g, ''));

function pagesAndButtons() {
  const pages = [];
  const buttons = [];
  for (const file of walk(path.join(root, 'backend/src'), (p) => /Pages[\\/]PageRegistry\.cs$/.test(p))) {
    const module = file.match(/XiHan\.BasicApp\.(\w+)/)[1];
    const directoryConstants = Object.fromEntries([...fs.readFileSync(file, 'utf8').matchAll(/const string (\w+DirectoryCode)\s*=\s*"([^"]+)"/g)].map((m) => [m[1], m[2]]));
    // 3 个模块目录跨行写：先把 new(...) 按括号收成一条
    const text = fs.readFileSync(file, 'utf8').replace(/^\s*\/\/.*$/gm, '').replace(/^\s*\/\/\/.*$/gm, '');
    for (const match of text.matchAll(/\bnew\(([\s\S]*?)\)\s*[,;]\s*(?:\r?\n|\/\/)/g)) {
      const args = splitArguments(match[1].replace(/\s*\r?\n\s*/g, ' '));
      const code = (value) => directoryConstants[value] ?? literal(value);
      if (args.length >= 10 && /MenuType\./.test(args[3])) {
        pages.push({ module, code: code(args[0]), title: literal(args[1]), type: args[3].replace('MenuType.', ''), route: literal(args[4]), component: literal(args[6]), parent: code(args[7]), permission: resolvePermission(args[8]) });
      } else if (args.length === 5 && !/MenuType\./.test(args[3])) {
        buttons.push({ module, code: literal(args[0]), title: literal(args[1]), page: code(args[2]), permission: resolvePermission(args[3]) });
      }
    }
  }
  return { pages, buttons };
}

function frontendCallNames() {
  const names = new Set();
  const files = [...walk(path.join(root, 'frontend/src'), (p) => /\.(ts|vue)$/.test(p) && !/\.test\./.test(p)), ...walk(path.join(root, 'frontend/packages'), (p) => /\.(ts|vue)$/.test(p) && !/\.test\./.test(p))];
  for (const file of files) {
    const text = fs.readFileSync(file, 'utf8');
    for (const m of text.matchAll(/\.(?:get|post|put|delete|patch)(?:<[^(]*>)?\(\s*['"`]([A-Za-z/]+)/g)) names.add(m[1].split('/').pop());
    for (const m of text.matchAll(/create(?:ReadApi|CommandApi)(?:<[^(]*>)?\(\s*['"]([A-Za-z]+)['"](?:\s*,\s*['"]([A-Za-z]+)['"])?/g)) {
      if (m[2]) {
        names.add(`${m[2]}Page`);
        names.add(`${m[2]}Detail`);
        names.add(m[2]);
      }
    }
  }
  return names;
}

function dynamicApis(calls) {
  const methods = [];
  const stripVerb = (name) => name.replace(/^(Get|Create|Update|Delete|Add|Remove|Set|Put|Post)/, '');
  for (const file of walk(path.join(root, 'backend/src'), (p) => p.endsWith('.cs'))) {
    const text = fs.readFileSync(file, 'utf8');
    if (!/\[DynamicApi[\](]/.test(text) || /abstract class/.test(text)) continue;
    const service = (text.match(/\[DynamicApi[^\]]*\][\s\S]{0,300}?class\s+(\w+)/) || [])[1];
    if (!service) continue;
    for (const m of text.matchAll(/((?:[ \t]*\[[^\]\n]*\][ \t]*\r?\n)*)[ \t]*public\s+(?:virtual\s+|override\s+|async\s+)*(?:Task(?:<[^\n(]*>)?|ValueTask(?:<[^\n(]*>)?)\s+(\w+)\s*\(/g)) {
      const name = m[2].replace(/Async$/, '');
      const permission = (m[1].match(/PermissionAuthorize\(([^),]*)/) || [])[1];
      const anonymous = /AllowAnonymous/.test(m[1]);
      methods.push({
        module: (file.match(/XiHan\.BasicApp\.(\w+)/) || [])[1],
        service: service.replace(/AppService$/, '').replace(/QueryService$/, 'Query'),
        method: name,
        permission: resolvePermission(permission),
        anonymous,
        frontend: calls.has(name) || calls.has(stripVerb(name)),
      });
    }
  }
  return methods;
}

function configurationRows(file, reader = read) {
  const rows = [];
  let h2 = '';
  let h3 = '';
  for (const line of reader(file).split(/\r?\n/)) {
if (line.startsWith('## ')) {
      h2 = line.slice(3).replace(/`/g, '');
      h3 = '';
    } else if (line.startsWith('### ')) h3 = line.slice(4).replace(/`/g, '');
    else {
      const hit = line.match(/^\| `([^`]+)`/);
      if (hit) rows.push({ section: h3 ? `${h2} / ${h3}` : h2, key: hit[1] });
    }
  }
  return rows;
}

function plainEndpoints(dir) {
  const rows = [];
  for (const file of walk(path.join(root, dir), (p) => p.endsWith('.cs'))) {
    for (const m of fs.readFileSync(file, 'utf8').matchAll(/\.?(Map(?:Get|Post|Put|Delete|Methods|Hub))(?:<(\w+)>)?\(\s*([^,)\n]+)/g)) {
      if (!/^Map/.test(m[1]) || /GetSection|GetRequired/.test(m[1])) continue;
      rows.push({ file: rel(file), verb: m[1].replace('Map', ''), hub: m[2] ?? null, route: m[3].trim().slice(0, 90) });
    }
  }
  return rows;
}

function inventory4x() {
  const { pages, buttons } = pagesAndButtons();
  const calls = frontendCallNames();
  const api = dynamicApis(calls);
  const seeders = walk(path.join(root, 'backend/src'), (p) => /Seeders?[\\/].*Seeder\.cs$/.test(p) && !/[\\/]Base[\\/]/.test(p)).map((file) => {
    const text = fs.readFileSync(file, 'utf8');
    return { file: rel(file), name: (text.match(/Name\s*=>\s*"([^"]+)"/) || [])[1] ?? path.basename(file, '.cs') };
  });
  const hosted = walk(path.join(root, 'backend/src'), (p) => /HostedService\.cs$/.test(p)).map((file) => path.basename(file, '.cs'));
  const tasks = [];
  for (const file of walk(path.join(root, 'backend/src'), (p) => /TaskSeeder\.cs$/.test(p))) {
    for (const m of fs.readFileSync(file, 'utf8').matchAll(/TaskCode = "([^"]+)"/g)) tasks.push(m[1]);
  }
  const realtime = new Set();
  for (const file of walk(path.join(root, 'backend/src'), (p) => /\.cs$/.test(p))) {
    const text = fs.readFileSync(file, 'utf8');
    for (const m of text.matchAll(/const string \w+ = "((?:Receive|Chat|Force|Task|User|Notification|Online)[A-Za-z]*)"/g)) realtime.add(m[1]);
  }
  return {
    features: featureRows(),
    pages,
    buttons,
    permissions: [...new Set(Object.values(constants))].filter((v) => /:/.test(v)).sort(),
    api,
    seeders,
    hosted,
    tasks,
    realtime: [...realtime].sort(),
    configuration: configurationRows('docs/configuration.md', read4x),
    endpoints: plainEndpoints('backend/src'),
  };
}

// ---------- 6.0 ----------

function operationAttributes() {
  const byId = {};
  for (const file of walk(path.join(root, 'backend/src'), (p) => p.endsWith('.cs'))) {
    const text = fs.readFileSync(file, 'utf8');
    const local = Object.fromEntries([...text.matchAll(/public const string Id = "([^"]+)"/g)].map((m, i) => [i, m[1]]));
    const ids = Object.values(local);
    let index = 0;
    for (const m of text.matchAll(/\[Operation\(([^\]]*)\)\]/g)) {
      const head = m[1];
      const quoted = (head.match(/^"([^"]+)"/) || [])[1];
      const id = quoted ?? ids[index++];
      if (!id) continue;
      byId[id] = {
        file: rel(file),
        expose: (head.match(/Expose\s*=\s*([^,]+)/) || [])[1]?.trim() ?? null,
        policy: (head.match(/Policy\s*=\s*([^,]+)/) || [])[1]?.trim() ?? null,
        readOnly: /ReadOnly\s*=\s*true/.test(head),
      };
    }
  }
  return byId;
}

function inventory6x() {
  const openapi = JSON.parse(read('frontend/contracts/openapi.json'));
  const attributes = operationAttributes();
  const operations = [];
  for (const [route, item] of Object.entries(openapi.paths)) {
    for (const [verb, op] of Object.entries(item)) {
      if (!op.operationId) continue;
      operations.push({ id: op.operationId, http: `${verb.toUpperCase()} ${route}`, permission: op['x-xihan-permission'] ?? null, ...(attributes[op.operationId] ?? {}) });
    }
  }
  operations.sort((a, b) => a.id.localeCompare(b.id));
  const pages = [];
  const permissions = [];
  const seeders = [];
  const jobs = [];
  const sections = new Set();
  for (const file of walk(path.join(root, 'backend/src'), (p) => p.endsWith('.cs'))) {
    const text = fs.readFileSync(file, 'utf8');
    if (/Pages\.cs$/.test(file) && !/PageCatalog/.test(file)) {
      for (const m of text.matchAll(/new\(\s*"([a-z0-9.\-_]+)",\s*"(\/[^"]*)",\s*"([^"]+)"/g)) pages.push({ code: m[1], route: m[2], titleKey: m[3] });
    }
    if (/Permissions\.cs$/.test(file)) {
      for (const m of text.matchAll(/public const string (\w+)\s*=\s*"([a-z0-9-]+\.[a-z0-9-]+\.[a-z0-9-]+)"/g)) permissions.push(m[2]);
    }
    if (/[\\/]Seeds[\\/].*Seeder\.cs$/.test(file)) {
      const name = (text.match(/public string Name => "([^"]+)"/) || [])[1];
      if (name) seeders.push(name);
    }
    if (/[\\/]Jobs[\\/].*Job\.cs$/.test(file)) {
      const name = (text.match(/public const string Name = "([^"]+)"/) || [])[1];
      if (name) jobs.push(name);
    }
    for (const m of text.matchAll(/(?:ConfigurationSection|Section) = "((?:BasicApp|XiHan)[^"]*)"/g)) sections.add(m[1]);
  }
  return {
    operations,
    pages,
    permissions: [...new Set(permissions)].sort(),
    seeders: seeders.sort(),
    jobs: jobs.sort(),
    configurationSections: [...sections].sort(),
    configuration: configurationRows('docs/configuration.md'),
    realtime: JSON.parse(read('frontend/contracts/realtime.json')).hubs,
    endpoints: plainEndpoints('backend/src'),
  };
}

// ---------- 第一遍候选 ----------

// 4.x 页面 → 组、功能条目、6.0 页面；E 组的「未列入 features.md」是矩阵新增的一级行
const NOT_LISTED = '未列入 features.md';
const PAGE_MAP = {
  'identity.user': ['A', '用户管理', 'identity.users'],
  'identity.online-user': ['A', '在线用户', 'identity.online-users'],
  'workbench.profile': ['A', '个人中心', '静态路由 /profile'],
  'identity.org': ['A', '组织机构', 'organization.departments'],
  'identity.position': ['A', '岗位管理', 'organization.positions'],
  'identity.role': ['B', '角色管理', 'access.roles'],
  'identity.permission': ['B', '权限管理', 'access.permissions'],
  'identity.field-security': ['B', '字段安全', 'access.field-security'],
  'identity.authorization': ['B', '授权申请与委托', 'access.delegations'],
  'setting.menu': ['B', '菜单管理', 'access.menus'],
  'setting.access-policy': ['B', '访问策略', 'access.policies'],
  'approval.constraint': ['B', '审批约束', 'access.constraint-rules'],
  'tenant.list': ['C', '租户管理', 'tenancy.tenants'],
  'tenant.edition': ['C', '版本套餐', 'tenancy.editions'],
  'tenant.subscription': ['C', '我的订阅', 'tenancy.subscription'],
  'setting.dict': ['C', '字典管理', 'settings.dicts'],
  'setting.config': ['C', '参数配置', 'settings.configs'],
  'setting.numbering': ['C', '业务编号', 'settings.numbering-rules'],
  'setting.job': ['C', '任务调度', 'maintenance.jobs'],
  'setting.cache': ['C', '缓存管理', 'maintenance.caches'],
  'setting.server': ['C', '服务监控', 'maintenance.server'],
  'setting.version': ['C', '版本管理', 'maintenance.versions'],
  'log.migration': ['C', '升级记录', 'maintenance.migrations'],
  'message.notification': ['D', '通知公告', 'notification.notifications'],
  'message.record': ['D', '邮件短信', 'notification.messages'],
  'message.template': ['D', '消息模板', 'notification.templates'],
  'setting.email-config': ['D', '邮件配置', 'notification.email-configs'],
  'setting.sms-config': ['D', '短信配置', 'notification.sms-configs'],
  'setting.bot-config': ['D', '机器人配置', 'notification.bot-configs'],
  'setting.telegram-bot': ['D', 'Telegram 机器人', 'notification.telegram-bots'],
  'workbench.inbox': ['D', '我的消息', 'notification.inbox'],
  'file.library': ['D', '文件管理', 'files.library'],
  'file.storage': ['D', '存储配置', 'files.storage-configs'],
  'file.export-center': ['D', '导出中心', 'files.export-center'],
  'log.trace': ['E', '日志链路追踪', 'auditing.traces'],
  'log.access': ['E', '访问日志', 'auditing.access-logs'],
  'log.access-policy': ['E', '条件命中', 'auditing.condition-hits'],
  'log.api': ['E', '开放接口日志', 'auditing.open-api-logs'],
  'log.operation': ['E', '操作日志', 'auditing.operation-logs'],
  'log.login': ['E', '登录日志', 'auditing.login-logs'],
  'log.exception': ['E', '异常日志', 'auditing.exception-logs'],
  'log.diff': ['E', '数据变更日志', 'auditing.diff-logs'],
  'log.permission-change': ['E', '权限变更日志', 'auditing.permission-change-logs'],
  'openapi.app': ['E', '应用管理', 'openplatform.oauth-apps'],
  'workbench.dashboard': ['E', '工作台', 'workbench.dashboard'],
  'about.project': ['E', NOT_LISTED, '无'],
  'about.docs': ['E', NOT_LISTED, '无'],
  'about.github': ['E', NOT_LISTED, '无'],
  'about.gitee': ['E', NOT_LISTED, '无'],
  'message.chat': ['F', '在线聊天', 'chat.conversations'],
  'message.chat-audit': ['F', '聊天审计', 'chat.audit'],
  workflow_approval: ['F', '审批中心', 'workflow.approvals'],
  workflow_definition: ['F', '流程定义', 'workflow.definitions'],
  workflow_instance: ['F', '流程实例', 'workflow.instances'],
  'setting.print-template': ['F', '打印模板', 'printing.print-templates'],
  code_gen: ['F', '代码生成', 'codegeneration.code-gen'],
  ai_provider: ['F', 'AI 提供商', 'ai.providers'],
  ai_prompt: ['F', 'AI 提示词', 'ai.prompts'],
  knowledge_base: ['F', '知识库', 'ai.knowledge-documents'],
  ai_assistant: ['F', 'AI 助手', 'ai.assistants'],
};

// 不挂 4.x 页面的功能条目：由接口和前端壳承载
const FEATURE_ONLY = {
  控制中心: 'A',
  登录认证: 'A',
  开放接口凭证: 'A',
  偏好设置: 'E',
  高级列表: 'E',
  内容编辑: 'E',
  全局导航: 'E',
};

const SERVICE_GROUPS = [
  [/^(Auth|Profile|User|UserQuery|UserSecurity|UserSession|UserStatistics|OnlineUser|ExternalLogin|PasswordHistory|SessionRole|Department|Position|UserDepartment|DepartmentManagement|UserManagement|UserSetting)/, 'A'],
  [/^(Role|UserRole|UserPermission|UserDataScope|Permission|Operation|Resource|FieldLevelSecurity|Menu|AccessPolicy|ConstraintRule)/, 'B'],
  [/^(Tenant|Dict|Config|Numbering|Task|Server|Version|EnumMetadata|TimeZoneMetadata)/, 'C'],
  [/^(Notification|Message|EmailConfig|SmsConfig|BotConfig|TelegramBot|UserInbox|File|StorageConfig|ExportTask|ImportHistory)/, 'D'],
  [/^(AccessLog|ApiLog|OperationLog|LoginLog|ExceptionLog|DiffLog|PermissionChangeLog|Trace|OAuth|MyOAuthApp|OpenApi)/, 'E'],
  [/^(Chat|Workflow|PrintTemplate|PrintDataSource|CodeGen|CodeGeneration|DynamicRuntime|Ai|Knowledge)/, 'F'],
];
const groupOfService = (service) => SERVICE_GROUPS.find(([pattern]) => pattern.test(service))?.[1] ?? 'E';

const RESOURCES = {
  user: ['users'], 'user-security': ['users'], 'user-role': ['roles'], 'user-permission': ['user-grants'], 'user-data-scope': ['user-data-scopes'],
  'user-session': ['sessions'], 'user-department': ['memberships'], impersonation: ['impersonation'], 'permission-condition': ['permission-conditions'],
  role: ['roles'], 'role-permission': ['roles'], 'role-data-scope': ['roles'], 'role-hierarchy': ['roles'], department: ['departments'], position: ['positions'],
  permission: ['permissions'], 'field-level-security': ['field-security'], 'permission-request': ['permission-requests'], 'permission-delegation': ['delegations'],
  tenant: ['tenants'], 'tenant-edition': ['editions'], 'tenant-edition-permission': ['editions'], notification: ['notifications'], message: ['emails', 'sms'],
  'message-template': ['message-templates'], 'email-config': ['email-configs'], 'sms-config': ['sms-configs'], 'bot-config': ['bot-configs'], 'telegram-bot': ['telegram-bots'],
  'constraint-rule': ['constraint-rules'], file: ['files'], 'storage-config': ['storage-configs'], 'oauth-app': ['oauth-apps'], menu: ['menus'], dict: ['dicts'], config: ['configs'],
  'access-policy': ['policies'], 'access-policy-hit': ['condition-hits'], numbering: ['numbering-rules'], task: ['jobs'], 'task-log': ['job-runs'], cache: ['caches'], version: ['versions'],
  'access-log': ['access-logs'], 'api-log': ['open-api-logs'], 'operation-log': ['operation-logs'], 'login-log': ['login-logs'], 'exception-log': ['exception-logs'], 'diff-log': ['diff-logs'],
  ai_assistant: ['ai-assistants'], ai_prompt: ['ai-prompts'], knowledge_base: ['knowledge-documents'], ai: ['ai-providers'], chat: ['chat-messages', 'chat-conversations'],
  code_gen: ['codegen-tables', 'codegen-templates', 'codegen-datasources'], 'print-template': ['print-templates'], workflow: ['workflow-definitions', 'workflow-instances'],
};
const ACTIONS = {
  create: ['create', 'grant', 'assign', 'start', 'ingest', 'upload', 'submit'], update: ['update', 'set', 'save', 'reschedule', 'reindex'], delete: ['delete', 'revoke', 'unassign'],
  status: ['set-status', 'enable', 'disable', 'suspend', 'resume', 'resend', 'pause', 'lock', 'revoke'], read: ['list', 'get'], grant: ['grant', 'assign', 'set', 'create', 'set-permissions'],
  revoke: ['revoke', 'unassign', 'delete', 'set-permissions'], publish: ['publish'], trial: ['trial', 'batch-trial'], run: ['trigger'], 'run-status': ['trigger'], reset: ['reset'], secret: ['reset-secret'],
  test: ['test'], execute: ['test', 'query', 'generate', 'start', 'publish', 'signal'], import: ['import', 'db-tables', 'create'], send: ['send'], manage: ['add-members', 'set-member-role'], use: ['resolve'],
  lock: ['lock', 'unlock'], 'reset-password': ['reset-password'], 'reset-two-factor': ['reset-two-factor'], start: ['start'], ban: ['lift', 'list'], default: ['set-default', 'update'], clear: ['delete', 'delete-by-prefix'],
  initdb: ['initialize-database'], 'transfer-owner': ['transfer-owner'], 'support-member': ['create'], 'allocation-read': ['allocations'], 'global-manage': ['create', 'update'], withdraw: ['withdraw'],
};

function candidatesOf(permission, operationIds) {
  if (!permission || permission.startsWith('?')) return [];
  const parts = permission.split(':');
  const resource = parts.length === 3 ? parts[1] : parts[0];
  const action = parts.at(-1);
  const hits = [];
  for (const target of RESOURCES[resource] ?? []) {
    for (const verb of ACTIONS[action] ?? [action]) {
      if (operationIds.has(`${target}.${verb}`)) hits.push(`${target}.${verb}`);
    }
    if (action === 'status' && !hits.length && operationIds.has(`${target}.update`)) hits.push(`${target}.update（启停并入修改）`);
  }
  return hits;
}

// ---------- 草稿 ----------

const cell = (value) => (value === null || value === undefined || value === '' ? '—' : String(value).replace(/\|/g, '\\|'));
const row = (...cells) => `| ${cells.map(cell).join(' | ')} |`;
const HEADER = ['4.x 页面（路由、组件）', '4.x 按钮 / 接口', '6.0 页面', '6.0 操作 ID', '6.0 权限码', '结论', '证据'];

function buildBlocks(old, next) {
  const operationIds = new Set(next.operations.map((o) => o.id));
  const permissionOf = new Map(next.operations.map((o) => [o.id, o.permission]));
  const usedApi = new Set();
  const blocks = { A: [], B: [], C: [], D: [], E: [], F: [] };
  const featureNames = new Set(old.features.map((f) => f.name));
  const candidateCells = (permission) => {
    const hits = candidatesOf(permission, operationIds);
    const clean = hits.map((h) => h.replace(/（.*$/, ''));
    return [hits.join('<br>'), [...new Set(clean.map((h) => permissionOf.get(h)).filter(Boolean))].join('<br>')];
  };
  const apiNote = (permission) => {
    if (!permission) return '';
    const methods =old.api.filter((a) => a.permission === permission && !usedApi.has(`${a.service}.${a.method}`));
    methods.forEach((a) => usedApi.add(`${a.service}.${a.method}`));
    return methods.map((a) => `${a.service}.${a.method}`).join('<br>');
  };
  const byFeature = {};
  for (const page of old.pages.filter((p) => p.type === 'Menu')) {
    const mapped = PAGE_MAP[page.code];
    if (!mapped) throw new Error(`4.x 页面没有分组：${page.code}`);
    const [group, feature, page6] = mapped;
    if (feature !== NOT_LISTED && !featureNames.has(feature)) throw new Error(`功能条目不存在：${feature}`);
    (byFeature[`${group}|${feature}`] ??= []).push({ page, page6 });
  }
  for (const [name, group] of Object.entries(FEATURE_ONLY)) byFeature[`${group}|${name}`] ??= [];
  for (const [key, list] of Object.entries(byFeature)) {
    const [group, feature] = key.split('|');
    const lines = [`### ${feature}`, ''];
    const text = old.features.find((f) => f.name === feature)?.text;
    if (text) lines.push(`> ${text}`, '');
    lines.push(row(...HEADER), row(...HEADER.map(() => '---')).replace(/\\/g, ''));
    for (const { page, page6 } of list) {
      const [operations, permissions] = candidateCells(page.permission);
      lines.push(row(`${page.code}<br>${page.route}<br>${page.component ?? '外链'}`, `页面读权限 ${page.permission ?? '无'}<br>${apiNote(page.permission)}`, page6, operations, permissions, '', ''));
      for (const button of old.buttons.filter((b) => b.page === page.code)) {
        const [ops, perms] = button.permission.endsWith(':export') ? ['export-tasks.submit', ''] : candidateCells(button.permission);
        lines.push(row('', `${button.code}（${button.title}）=${button.permission}<br>${apiNote(button.permission)}`, page6, ops, perms, '', ''));
      }
    }
    lines.push('');
    blocks[group].push(lines.join('\n'));
  }
  const rest = old.api.filter((a) => !usedApi.has(`${a.service}.${a.method}`));
  const leftovers = { A: [], B: [], C: [], D: [], E: [], F: [] };
  for (const api of rest) leftovers[groupOfService(api.service)].push(api);
  for (const [group, list] of Object.entries(leftovers)) {
    if (!list.length) continue;
    const lines = ['### 没有挂到按钮的 4.x 接口', '', row('4.x 接口', '权限码', '前端调用', '6.0 操作 ID', '6.0 权限码', '结论', '证据'), row('---', '---', '---', '---', '---', '---', '---')];
    for (const api of list) lines.push(row(`${api.service}.${api.method}`, api.anonymous ? '匿名' : api.permission ?? '登录即可', api.frontend ? '有' : '没查到', '', '', '', ''));
    lines.push('');
    blocks[group].push(lines.join('\n'));
  }
  return blocks;
}

function buildNonPageBlock(old, next) {
  const table = (title, head, rows) => [`### ${title}`, '', row(...head), row(...head.map(() => '---')), ...rows.map((r) => row(...r)), ''].join('\n');
  const verdict = ['6.0 去处', '结论', '证据'];
  return [
    table('后台作业与托管服务', ['4.x', ...verdict], [...old.hosted.map((h) => [h, '', '', '']), ...old.tasks.map((t) => [`任务种子 ${t}`, '', '', ''])]),
    `6.0 作业：${next.jobs.join('、')}\n`,
    table('配置键', ['4.x 节', '4.x 键', ...verdict], old.configuration.map((c) => [c.section, c.key, '', '', ''])),
    `6.0 配置节：${next.configurationSections.join('、')}（键见 docs/configuration.md）\n`,
    table('种子', ['4.x', ...verdict], old.seeders.map((s) => [`${s.name}（${s.file.split('/').pop()}）`, '', '', ''])),
    `6.0 种子：${next.seeders.join('、')}\n`,
    table('普通端点', ['4.x', ...verdict], old.endpoints.map((e) => [`${e.verb} ${e.hub ?? e.route}（${e.file.split('/').pop()}）`, '', '', ''])),
    `6.0 端点：${next.endpoints.map((e) => `${e.verb} ${e.hub ?? e.route}`).join('；')}\n`,
    table('实时方法', ['4.x', ...verdict], old.realtime.map((m) => [m, '', '', ''])),
    `6.0 实时：${next.realtime.map((h) => `${h.path}：${h.methods.join('、')}；调用 ${(h.invocations ?? []).join('、') || '无'}`).join('。')}\n`,
    '### 机器人与短信渠道\n\n（读 4.x 的 SysBotConfig.Enum.cs、SysSms.Enum.cs、SysSmsConfig.Enum.cs 与 6.0 的 Notification/Infrastructure 传输类后填表）\n',
    '### 内置模板\n\n（读 4.x 的 SaasMessageTemplateSeeder、打印与代码生成的模板种子、访问策略预置与工作流演示，对 6.0 的种子后填表）\n',
  ].join('\n');
}

// ---------- 输出 ----------

const old = inventory4x();
const next = inventory6x();
fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'inventory-4x.json'), `${JSON.stringify(old, null, 1)}\n`);
fs.writeFileSync(path.join(outDir, 'inventory-6x.json'), `${JSON.stringify(next, null, 1)}\n`);

if (blocksDir) {
  fs.mkdirSync(blocksDir, { recursive: true });
  const blocks = buildBlocks(old, next);
  for (const [group, sections] of Object.entries(blocks)) fs.writeFileSync(path.join(blocksDir, `${group}.draft.md`), sections.join('\n'));
  fs.writeFileSync(path.join(blocksDir, 'G.draft.md'), buildNonPageBlock(old, next));
}

const summary = {
  features: old.features.length,
  pages4: old.pages.length,
  menus4: old.pages.filter((p) => p.type === 'Menu').length,
  buttons4: old.buttons.length,
  permissions4: old.permissions.length,
  api4: old.api.length,
  services4: new Set(old.api.map((a) => a.service)).size,
  api4WithoutFrontend: old.api.filter((a) => !a.frontend).length,
  operations6: next.operations.length,
  operations6WithPermission: next.operations.filter((o) => o.permission).length,
  pages6: next.pages.length,
  permissions6: next.permissions.length,
  seeders: `${old.seeders.length} → ${next.seeders.length}`,
  jobs: `${old.hosted.length} + ${old.tasks.length} → ${next.jobs.length}`,
  configuration: `${old.configuration.length} → ${next.configuration.length}`,
  endpoints: `${old.endpoints.length} → ${next.endpoints.length}`,
};
console.log(summary);
