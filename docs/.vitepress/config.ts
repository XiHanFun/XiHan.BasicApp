import { createRequire } from "node:module";
import type { DefaultTheme } from "vitepress";
import { defineXiHanConfig } from "@xihanfun/vitepress-theme/config";
const require = createRequire(import.meta.url);

// 导航末项显示的版本号取自应用包 package.json，发版时只改那一处。
const { version } = require("../../frontend/package.json");

const title: string = "曦寒基础应用文档";
const description: string = "基于曦寒开发框架的企业级中后台应用";
const keywords: string =
  "曦寒,曦寒懿,基础应用,中后台,多租户,权限,官方文档,开源,XiHanFun,XiHan.BasicApp";

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
      { text: "为什么选择曦寒", link: "/why" },
      { text: "系统概述", link: "/overview" },
      { text: "开发环境", link: "/dev-environment" },
      { text: "快速开始", link: "/getting-started" },
      { text: "目录结构", link: "/project-structure" },
      { text: "常见问题", link: "/faq" },
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

const backendSidebar: DefaultTheme.SidebarItem[] = [
  {
    text: "后端手册",
    collapsed: false,
    items: manual("backend", [
      ["框架简介", "introduction"],
      ["开发流程", "development"],
      ["请求生命周期", "request-lifecycle"],
      ["实体基类", "entity"],
      ["数据库配置", "database"],
      ["数据模型", "data-model"],
      ["统一认证", "authentication"],
      ["权限管理", "permission"],
      ["数据权限", "data-permission"],
      ["组织架构", "organization"],
      ["多租户 SaaS", "multi-tenancy"],
      ["缓存与异步", "caching"],
      ["定时任务", "scheduling"],
      ["工作流", "workflow"],
      ["审批与约束", "approval"],
      ["消息通知", "messaging"],
      ["即时通讯", "realtime"],
      ["打印模板", "printing"],
      ["文件与存储", "file"],
      ["日志审计", "logging"],
      ["健康与可观测性", "health-observability"],
      ["系统设置", "settings"],
      ["升级与迁移", "upgrade"],
      ["开放接口", "open-api"],
      ["代码生成", "code-generation"],
      ["AI 能力", "ai"],
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
      ["服务端交互", "request"],
      ["Schema 驱动页面", "schema-page"],
      ["权限与脱敏", "permission"],
      ["布局与主题", "theme"],
      ["国际化", "i18n"],
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
  "/": startSidebar,
};

const nav: DefaultTheme.NavItem[] = [
  {
    text: "指南",
    activeMatch:
      "^/(introduction|why|overview|dev-environment|getting-started|project-structure|faq|api-guide|configuration|features|preview|deployment)$",
    items: [
      {
        text: "快速开始",
        items: [
          { text: "介绍", link: "/introduction" },
          { text: "快速上手", link: "/getting-started" },
          { text: "常见问题", link: "/faq" },
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
    items: [{ text: "更新日志", link: "/changelog" }],
  },
];

export default defineXiHanConfig({
  title,
  description,
  keywords,
  repo: "XiHan.BasicApp",
  llms: {
    title: "曦寒基础应用",
    summary:
      "企业级中后台内核：后端基于 .NET 与 XiHan.Framework，前端基于 Vue 3 与 XiHan.UI，开箱即带多租户、RBAC + 数据范围 + 字段脱敏的权限体系、代码生成与实时通信。",
    sections: [
      { dir: ".", label: "开始" },
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
  },
});
