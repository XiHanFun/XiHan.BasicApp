// N5 第一批：把七组核对片段合并成 plans/对齐矩阵.md，并统计结论（方案第 8 节「N5 对齐与收口的做法」）
// 只在 N5 用，不进 CI
//
//   node plans/scripts/n5-matrix.mjs <片段目录>
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const blocksDir = path.resolve(process.argv[2]);
const inventory = JSON.parse(fs.readFileSync(path.join(root, 'plans/alignment/inventory-4x.json'), 'utf8'));

const NOT_LISTED = '未列入 features.md';
const LEFTOVER = '没有挂到按钮的 4.x 接口';

// 一个片段按 ### 切成节，节名相同的（各组的 LEFTOVER）带上组名
const sections = new Map();
const leftovers = [];
for (const group of ['A', 'B', 'C', 'D', 'E', 'F']) {
  const text = fs.readFileSync(path.join(blocksDir, `${group}.md`), 'utf8').replace(/\r\n/g, '\n');
  for (const part of text.split(/^(?=### )/m)) {
    const name = part.match(/^### (.+)$/m)?.[1]?.trim();
    if (!name) continue;
    if (name === LEFTOVER) leftovers.push([group, part.trim()]);
    else if (sections.has(name)) throw new Error(`功能节重复：${name}`);
    else sections.set(name, part.trim());
  }
}

const groupNames = { A: 'Identity、Organization', B: 'Access', C: 'Tenancy、Settings、Maintenance', D: 'Notification、Files', E: 'Auditing、OpenPlatform、Workbench、壳层', F: 'Chat、Workflow、Printing、CodeGeneration、AI' };
const body = [];
const bySection = new Map();
for (const feature of inventory.features) (bySection.get(feature.section) ?? bySection.set(feature.section, []).get(feature.section)).push(feature.name);
const missing = [];
for (const [section, names] of bySection) {
  body.push(`## ${section}`, '');
  for (const name of names) {
    if (!sections.has(name)) missing.push(name);
    else body.push(sections.get(name), '');
    sections.delete(name);
  }
}
if (missing.length) throw new Error(`功能条目没有去处：${missing.join('、')}`);
if (sections.has(NOT_LISTED)) {
  body.push(`## ${NOT_LISTED}`, '', '4.x 登记了、功能清单的 63 条里没有的页面。', '', sections.get(NOT_LISTED).replace(/^### .*\n/, ''), '');
  sections.delete(NOT_LISTED);
}
if (sections.size) throw new Error(`多出的节：${[...sections.keys()].join('、')}`);
body.push(`## ${LEFTOVER}`, '', '按钮和页面读权限之外的接口。「删除」多半是这里。', '');
for (const [group, part] of leftovers) body.push(`### ${groupNames[group]}`, '', part.replace(/^### .*\n/, '').trim(), '');
body.push('## 非页面能力', '', fs.readFileSync(path.join(blocksDir, 'G.md'), 'utf8').replace(/\r\n/g, '\n').trim(), '');
const content = body.join('\n');

// 统计：每个表行里取结论列
const counts = {};
const flagged = [];
for (const line of content.split('\n')) {
  if (!line.startsWith('| ') || /^\| ?---/.test(line)) continue;
  const cells = line.split(/(?<!\\)\|/).slice(1, -1).map((c) => c.trim());
  const verdict = cells.find((c) => /^(对齐|改口径|删除|缺口)/.test(c));
  if (!verdict) continue;
  const kind = verdict.match(/^(对齐|改口径？|改口径|删除？|删除|缺口)/)[1];
  counts[kind] = (counts[kind] ?? 0) + 1;
  if (['缺口', '改口径？', '删除？'].includes(kind)) flagged.push(`${kind}\t${line}`);
}

const header = [
  '# 对齐矩阵',
  '',
  '4.x 的每项能力在 6.0 的去处，N5 第一批逐项核销（方案第 8 节「N5 对齐与收口的做法」）。第 7 节的粗表是总览，这里是逐项结论。',
  '',
  '- 一级是 4.x 功能清单的 63 条（`v5.9.1` 的 `docs/features.md`），二级是 4.x 的页面、按钮与接口；非页面能力在最后一节。',
  '- 结论：**对齐**；**改口径**（引修订号或第 10 节的拍板）；**删除**（在第 7 节「明确删除」里）。N5 第一批的缺口都已补完并改为对齐，用户确认的改口径与删除见修订（一百三十二）。',
  '- 证据写测试类名；没有用例的写页面文件并标「无用例」。名字对上不等于对齐：每行都读过两侧代码。',
  '- 依据的清单快照在 `plans/alignment/`（`plans/scripts/n5-inventory.mjs` 生成），4.x 目录在 N6 删除之后以它为准。',
  '',
  '## 结论统计',
  '',
  '| 结论 | 行数 |',
  '| --- | --- |',
  ...Object.entries(counts).map(([kind, n]) => `| ${kind} | ${n} |`),
  '',
].join('\n');
fs.writeFileSync(path.join(root, 'plans/对齐矩阵.md'), `${header}\n${content}`);
fs.writeFileSync(path.join(blocksDir, 'flagged.tsv'), `${flagged.join('\n')}\n`);
console.log(counts, '总行', Object.values(counts).reduce((a, b) => a + b, 0));
