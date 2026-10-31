import { createRequire } from "node:module";
import type { DefaultTheme } from "vitepress";
import { defineXiHanConfig } from "@xihanfun/vitepress-theme/config";
const require = createRequire(import.meta.url);

// 导航末项显示的版本号取自 6.0 管理端的 package.json（与后端 Directory.Build.props 同号），发版时只改那一处。
const { version } = require("../../frontend-next/package.json");

const title: string = "曦寒基础应用 6.0 文档";
const description: string = "基于曦寒开发框架 5 的能力包式中后台应用（6.0 预览）";
const keywords: string =
  "曦寒,曦寒懿,基础应用,中后台,多租户,权限,能力包,操作契约,官方文档,开源,XiHanFun,XiHan.BasicApp,6.0";

// 生成手册条目：自动带序号前缀
function manual(
  dir: "backend" | "frontend",
  entries: [text: string, name: string][],
): DefaultTheme.SidebarItem[] {
  return entries.map(([text, name], index) => ({
    text: `${index + 1}. ${text}`,
    link: `/${dir}/${name}`,
  }));
}

const startSidebar: DefaultTheme.SidebarItem[] = [
  {
    text: "开始",
    collapsed: false,
    items: [
      { text: "应用简介", link: "/introduction" },
      { text: "为什么是 6.0", link: "/why" },
      { text: "系统概述", link: "/overview" },
      { text: "开发环境", link: "/dev-environment" },
      { text: "快速开始", link: "/getting-started" },
      { text: "目录结构", link: "/project-structure" },
      { text: "常见问题", link: "/faq" },
    ],
  },
  {
    text: "AI 辅助开发",
    collapsed: false,
    items: [
      { text: "For Agents", link: "/ai/agents" },
      { text: "MCP Server", link: "/ai/mcp" },
      { text: "LLMs.txt", link: "/ai/llms" },
    ],
  },
  {
    text: "参考",
    collapsed: false,
    items: [
      { text: "接口对接指南", link: "/api-guide" },
      { text: "配置参考", link: "/configuration" },
      { text: "功能清单", link: "/features" },
      { text: "功能预览", link: "/preview" },
      { text: "部署", link: "/deployment" },
      { text: "更新日志", link: "/changelog" },
    ],
  },
];

// 条目与 5.x 文档一一对应；6.0 尚未实现的写明阶段，改了口径的在名字上写清楚
const backendSidebar: DefaultTheme.SidebarItem[] = [
  {
    text: "后端手册",
    collapsed: false,
    items: manual("backend", [
      ["架构总览", "introduction"],
      ["开发流程", "development"],
      ["请求生命周期", "request-lifecycle"],
      ["实体基类", "entity"],
      ["数据库配置", "database"],
      ["数据模型", "data-model"],
      ["统一认证", "authentication"],
      ["权限管理", "permission"],
      ["数据权限与字段安全", "data-permission"],
      ["访问策略与权限条件", "access-policy"],
      ["组织架构", "organization"],
      ["多租户 SaaS", "multi-tenancy"],
      ["缓存与异步", "caching"],
      ["定时任务", "scheduling"],
      ["工作流（规划中 · N3）", "workflow"],
      ["约束规则与审批（审批随 N3）", "approval"],
      ["消息通知", "messaging"],
      ["实时推送（聊天随 N3）", "realtime"],
      ["打印模板（规划中 · N3）", "printing"],
      ["文件与存储", "file"],
      ["日志审计", "logging"],
      ["健康与可观测性", "health-observability"],
      ["系统设置", "settings"],
      ["表结构升级（不做 5.x 迁移）", "upgrade"],
      ["开放平台", "open-api"],
      ["代码生成（规划中 · N3）", "code-generation"],
      ["AI 能力（规划中 · N4）", "ai"],
      ["工作台（6.0 新增）", "workbench"],
    ]),
  },
];

const frontendSidebar: DefaultTheme.SidebarItem[] = [
  {
    text: "前端手册",
    collapsed: false,
    items: manual("frontend", [
      ["框架简介", "introduction"],
      ["开发流程", "development"],
      ["菜单与路由", "routing"],
      ["契约与请求", "request"],
      ["Schema 驱动页面", "schema-page"],
      ["权限与脱敏", "permission"],
      ["布局与主题", "theme"],
      ["国际化与时区", "i18n"],
      ["字体图标", "icon"],
      ["实时通信", "realtime"],
      ["常用组件", "components"],
    ]),
  },
];

// 每个顶部导航板块各自一份侧栏，由路径前缀决定用哪一份；
// 首页是 layout: home，不落任何一份。
const sidebar: DefaultTheme.Sidebar = {
  "/backend/": backendSidebar,
  "/frontend/": frontendSidebar,
  "/ai/": startSidebar,
  "/": startSidebar,
};

const nav: DefaultTheme.NavItem[] = [
  {
    text: "指南",
    activeMatch:
      "^/(introduction|why|overview|dev-environment|getting-started|project-structure|faq|api-guide|configuration|features|preview|deployment|ai/.*)$",
    items: [
      {
        text: "快速开始",
        items: [
          { text: "介绍", link: "/introduction" },
          { text: "快速上手", link: "/getting-started" },
          { text: "常见问题", link: "/faq" },
        ],
      },
      {
        text: "AI 辅助开发",
        items: [
          { text: "For Agents", link: "/ai/agents" },
          { text: "MCP Server", link: "/ai/mcp" },
        ],
      },
    ],
  },
  { text: "后端", link: "/backend/introduction", activeMatch: "/backend/" },
  { text: "前端", link: "/frontend/introduction", activeMatch: "/frontend/" },
  {
    text: "生态",
    items: [
      {
        text: "官方生态",
        items: [
          { text: "开发框架", link: "https://framework.docs.xihanfun.com" },
          { text: "视图组件", link: "https://ui.docs.xihanfun.com" },
          { text: "基础应用", link: "/" },
        ],
      },
      {
        text: "设计器",
        items: [
          { text: "打印设计器", link: "https://print.docs.xihanfun.com" },
          { text: "流程设计器", link: "https://flow.docs.xihanfun.com" },
          { text: "表单设计器", link: "https://form.docs.xihanfun.com" },
          { text: "报表设计器", link: "https://report.docs.xihanfun.com" },
        ],
      },
    ],
  },
  {
    text: "支持",
    items: [
      { text: "公约", link: "https://docs.xihanfun.com/cosmos/code-of-conduct" },
      { text: "参与", link: "https://docs.xihanfun.com/cosmos/contributing" },
      { text: "赞助", link: "https://docs.xihanfun.com/cosmos/sponsor" },
    ],
  },
  {
    text: `v${version}`,
    items: [
      { text: "更新日志", link: "/changelog" },
      { text: "5.x 文档", link: "https://basicapp.docs.xihanfun.com" },
    ],
  },
];

export default defineXiHanConfig({
  title,
  description,
  keywords,
  repo: "XiHan.BasicApp",
  banner: {
    id: "2026-10-basicapp-6-preview",
    text: "这里是 6.0 预览版（next 分支）文档，尚未发布；正在使用 5.x 的请看 5.x 文档",
    link: "https://basicapp.docs.xihanfun.com",
    linkText: "前往 5.x 文档",
  },
  llms: {
    title: "曦寒基础应用 6.0",
    summary:
      "6.0 预览版：后端基于 .NET 10 与 XiHan.Framework 5 的能力包与操作契约（一份契约同时服务 HTTP / gRPC / MCP），前端基于 Vue 3 与 XiHan.UI，契约层由后端 openapi.json 生成；内置多租户、RBAC + Deny + 继承委托 + 数据范围 + 字段安全 + ABAC 的权限体系。",
    // 6.0 还没有单独的站点地址；GA 时本站替换 5.x 文档，沿用其地址（与 package.json 的 homepage 一致）
    site: "https://basicapp.docs.xihanfun.com",
    sections: [
      { dir: ".", label: "开始" },
      { dir: "ai", label: "AI 辅助开发" },
      { dir: "backend", label: "后端手册" },
      { dir: "frontend", label: "前端手册" },
    ],
    bundles: [
      { name: "backend", label: "后端手册", dirs: ["backend"] },
      { name: "frontend", label: "前端手册", dirs: ["frontend"] },
    ],
  },
  themeConfig: {
    nav,
    sidebar,
    // 6.0 文档在 next 分支的 docs-next/ 下，覆盖主题按 main/docs 生成的编辑地址
    editLink: {
      text: "在 GitHub 上编辑此页",
      pattern: "https://github.com/XiHanFun/XiHan.BasicApp/tree/next/docs-next/:path",
    },
  },
});
