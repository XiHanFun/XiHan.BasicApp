// 构建期把文档站落成机读资产：llms.txt 索引、llms-full.txt 全站正文、按栏目的分册，外加每页一份 .md
// （正文上方「取本页 Markdown」的直链指向它）。开发服务器在 /__markdown/ 下按需生成同一份单页。
// 全部内容从本仓 docs 现算，没有手写清单；站点是纯静态的，产物一律写进 outDir。
import { mkdir, readdir, readFile, writeFile } from "node:fs/promises";
import { createRequire } from "node:module";
import { dirname, join, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

const DOCS = fileURLToPath(new URL("..", import.meta.url));

// 来源地址取 package.json 的 homepage，与站点域名同一处维护
const SITE: string = createRequire(import.meta.url)("../package.json").homepage.replace(/\/+$/, "");

/** 栏目：一个顶层目录在索引里的名字与排位。 */
export interface LlmsSection {
  /** 顶层目录名；站点根目录下的页面用 "." */
  dir: string;
  /** 索引里的栏目名，同时作分册的标题 */
  label: string;
  /** 给出时该栏目另外汇编成一份 llms-<bundle>.txt */
  bundle?: string;
}

/** 机读资产的站点信息。 */
export interface LlmsOptions {
  /** 站点名，作 llms.txt 的一级标题 */
  title: string;
  /** 一段话简介，作 llms.txt 标题下的引用块 */
  summary: string;
  /** 栏目按此顺序排列；未登记的目录排在末尾，以目录名作栏目名 */
  sections: LlmsSection[];
}

interface Page {
  rel: string;
  section: string;
  url: string;
  title: string;
  summary: string;
  text: string;
}

/** 目录下递归取相对 root 的 .md 路径（posix 分隔），跳过站点自身的目录。 */
async function markdownFiles(root: string, base = ""): Promise<string[]> {
  const out: string[] = [];
  for (const entry of await readdir(join(root, base), { withFileTypes: true })) {
    if (entry.name === "node_modules" || entry.name === ".vitepress" || entry.name === "public")
      continue;
    const rel = base ? `${base}/${entry.name}` : entry.name;
    if (entry.isDirectory())
      out.push(...(await markdownFiles(root, rel)));
    else if (entry.name.endsWith(".md"))
      out.push(rel);
  }
  return out.sort();
}

/** 拆出 frontmatter 与正文，frontmatter 保留原文。 */
function splitFrontmatter(source: string): { frontmatter: string; body: string } {
  const hit = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/.exec(source);
  return hit
    ? { frontmatter: hit[1], body: source.slice(hit[0].length) }
    : { frontmatter: "", body: source };
}

/** frontmatter 里某个顶层键的标量值。 */
function frontmatterValue(frontmatter: string, key: string): string {
  const hit = new RegExp(`^${key}:\\s*(.+)$`, "m").exec(frontmatter);
  return hit ? hit[1].trim().replace(/^["']|["']$/g, "") : "";
}

/** 源路径 → 站点地址（cleanUrls，index.md 落在目录上）。 */
function urlOf(rel: string): string {
  return `${SITE}/${rel.replace(/\.md$/, "").replace(/(^|\/)index$/, "$1")}`;
}

const HTML_ENTITIES: Record<string, string> = { lt: "<", gt: ">", amp: "&", quot: "\"", "#39": "'" };

/** 行内代码：内容自带反引号时加长定界符，贴边的反引号再垫一个空格。 */
function inlineCode(code: string): string {
  const text = code.replace(/&(lt|gt|amp|quot|#39);/g, (_, name: string) => HTML_ENTITIES[name]);
  const longest = Math.max(0, ...(text.match(/`+/g) ?? []).map(run => run.length));
  const ticks = "`".repeat(longest + 1);
  const pad = text.startsWith("`") || text.endsWith("`") ? " " : "";
  return `${ticks}${pad}${text}${pad}${ticks}`;
}

/**
 * 压平只给 VitePress 看的写法：<code v-pre> 是为了躲开 Vue 插值，纯 Markdown 里就是行内代码；
 * [[toc]] 由站点展开，纯 Markdown 里没有意义。围栏代码块里是示例原文，不动。
 */
function flattenVitePressSyntax(body: string): string {
  const out: string[] = [];
  let fence = "";
  for (const line of body.split(/\r?\n/)) {
    const marker = /^\s*(`{3,}|~{3,})(.*)$/.exec(line);
    if (marker) {
      if (!fence)
        fence = marker[1];
      else if (marker[1][0] === fence[0] && marker[1].length >= fence.length && !marker[2].trim())
        fence = "";
      out.push(line);
      continue;
    }
    if (fence) {
      out.push(line);
      continue;
    }
    if (/^\s*\[\[toc\]\]\s*$/i.test(line))
      continue;
    out.push(line.replace(/<code v-pre>([\s\S]*?)<\/code>/g, (_, code: string) => inlineCode(code)));
  }
  return out.join("\n");
}

/** 一句话描述：一级标题之后的第一段正文，压成单行并截到一句。 */
function summaryOf(text: string): string {
  // 先整块去掉围栏代码：代码块里常有空行，按空行切段会把它切成几截当正文
  const prose = text
    .replace(/^(`{3,}|~{3,}).*\n[\s\S]*?^\1[ \t]*$/gm, "")
    .replace(/^# .*$/m, "");
  for (const block of prose.split(/\n\s*\n/)) {
    const paragraph = block.trim();
    if (!paragraph || /^(#|<|:::|\||!\[|-{3,}$)/.test(paragraph))
      continue;
    const flat = paragraph
      .replace(/^>\s?/gm, "")
      .replace(/\s+/g, " ")
      .replace(/\[([^\]]+)\]\([^)]*\)/g, "$1")
      .replace(/\*\*([^*]+)\*\*/g, "$1")
      .trim();
    const stop = flat.indexOf("。");
    if (stop !== -1 && stop < 200)
      return flat.slice(0, stop + 1);
    return flat.length > 160 ? `${flat.slice(0, 160)}…` : flat;
  }
  return "";
}

/** 读出一页：机读正文、标题与一句话描述。 */
function readPage(rel: string, source: string): Page {
  const { frontmatter, body } = splitFrontmatter(source);
  let text = flattenVitePressSyntax(body).trim();
  // 首页正文是空的，内容全在 frontmatter 的 hero 与 features 里
  if (!text && frontmatter)
    text = `\`\`\`yaml\n${frontmatter.trim()}\n\`\`\``;
  const heading = /^# +(\S.*)$/m.exec(text)?.[1].replace(/<[^>]+>/g, "").trim();
  return {
    rel,
    section: rel.includes("/") ? rel.slice(0, rel.indexOf("/")) : ".",
    url: urlOf(rel),
    title: heading || frontmatterValue(frontmatter, "title") || rel,
    summary: summaryOf(text) || frontmatterValue(frontmatter, "titleTemplate"),
    text,
  };
}

/** 一页的形态：来源地址 + 正文，正文不以一级标题开头时补一个。 */
function pageBlock(page: Page): string {
  const heading = page.text.startsWith("# ") ? "" : `# ${page.title}\n\n`;
  return `来源：${page.url}\n\n${heading}${page.text}\n`;
}

/** 开发服务器按需生成与构建产物相同的单页 Markdown；路径越界或页面不存在时返回 null。 */
export async function renderPageMarkdown(relativePath: string): Promise<string | null> {
  const rel = relativePath.replaceAll("\\", "/").replace(/^\/+/, "");
  if (!rel.endsWith(".md") || rel.split("/").includes(".."))
    return null;
  let source: string;
  try {
    source = await readFile(join(DOCS, rel), "utf8");
  }
  catch (error) {
    if ((error as { code?: string }).code === "ENOENT")
      return null;
    throw error;
  }
  return pageBlock(readPage(rel, source));
}

/** 构建期写出全部机读资产：每页 .md、llms.txt、llms-full.txt 与各栏目分册。 */
export async function writeLlmsAssets(outDir: string, options: LlmsOptions): Promise<void> {
  const pages: Page[] = [];
  for (const rel of await markdownFiles(DOCS))
    pages.push(readPage(rel, await readFile(join(DOCS, rel), "utf8")));

  const order = options.sections.map(section => section.dir);
  const rank = (dir: string): number => (order.includes(dir) ? order.indexOf(dir) : order.length);
  const dirs = [...new Set(pages.map(page => page.section))]
    .sort((a, b) => rank(a) - rank(b) || a.localeCompare(b));
  const pagesIn = (dir: string): Page[] => pages.filter(page => page.section === dir);
  const labelOf = (dir: string): string => options.sections.find(section => section.dir === dir)?.label ?? dir;

  const bundles = options.sections.flatMap(section =>
    section.bundle ? [{ name: section.bundle, label: section.label, list: pagesIn(section.dir) }] : []);
  // 分册登记了目录却一页都没有，说明目录改名或登记写错，产出空文件只会让读者以为这一册没内容
  for (const bundle of bundles) {
    if (bundle.list.length === 0)
      throw new Error(`[gen-llms] 分册 llms-${bundle.name}.txt 对应的栏目「${bundle.label}」下没有任何页面，检查 sections 登记的 dir`);
  }

  await mkdir(outDir, { recursive: true });

  // 每页一份 .md：正文上方「取本页 Markdown」的直链指向它
  for (const page of pages) {
    const target = join(outDir, page.rel);
    await mkdir(dirname(target), { recursive: true });
    await writeFile(target, pageBlock(page), "utf8");
  }

  const sample = pages.find(page => page.section !== "." && !page.rel.endsWith("index.md"));
  const index = [
    `# ${options.title}`,
    "",
    `> ${options.summary}`,
    "",
    "本文件由文档站构建期生成，内容与仓库文档同源。",
    "",
    "## 机读资产",
    "",
    `- [llms-full.txt](${SITE}/llms-full.txt): 全部 ${pages.length} 页正文`,
    ...bundles.map(bundle => `- [llms-${bundle.name}.txt](${SITE}/llms-${bundle.name}.txt): ${bundle.list.length} 页${bundle.label}`),
    ...(sample ? [`- 每页 Markdown：把站点地址后缀成 \`.md\`，如 ${sample.url}.md`] : []),
    "",
  ];
  for (const dir of dirs) {
    index.push(`## ${labelOf(dir)}`, "");
    for (const page of pagesIn(dir))
      index.push(`- [${page.title}](${page.url})${page.summary ? `: ${page.summary}` : ""}`);
    index.push("");
  }

  const compile = (title: string, list: Page[]): string => [
    [`# ${options.title} · ${title}`, "", `共 ${list.length} 页。索引见 ${SITE}/llms.txt`, ""].join("\n"),
    ...list.map(pageBlock),
  ].join("\n---\n\n");

  await writeFile(join(outDir, "llms.txt"), index.join("\n"), "utf8");
  await writeFile(join(outDir, "llms-full.txt"), compile("全站正文", dirs.flatMap(pagesIn)), "utf8");
  for (const bundle of bundles)
    await writeFile(join(outDir, `llms-${bundle.name}.txt`), compile(bundle.label, bundle.list), "utf8");

  console.log(
    `[gen-llms] ${pages.length} 页${bundles.map(bundle => ` · ${bundle.label} ${bundle.list.length}`).join("")} → ${relative(join(DOCS, ".."), outDir).split(sep).join("/")}`,
  );
}
