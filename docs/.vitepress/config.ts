import { createRequire } from "node:module";
import { DefaultTheme, HeadConfig, defineConfig } from "vitepress";

// 导航末项显示的版本号取自本站 package.json，发版时只改那一处
const { version } = createRequire(import.meta.url)("../package.json");

const title: string = "曦寒基础应用文档";
const description: string = "基于曦寒开发框架的企业级中后台应用";
const keywords: string =
  "曦寒,曦寒懿,基础应用,中后台,多租户,权限,官方文档,开源,XiHanFun,XiHan.BasicApp";
const logo: string = "/images/logo.png";
const head: HeadConfig[] = [
  ["meta", { name: "author", content: "XiHanFun" }],
  [
    "meta",
    {
      name: "keywords",
      content: keywords,
    },
  ],
  ["link", { rel: "icon", href: "/favicon.ico" }],
];

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
    text: "开始",
    link: "/introduction",
    activeMatch:
      "^/(introduction|why|overview|dev-environment|getting-started|project-structure|faq|api-guide|configuration|features|deployment)$",
  },
  { text: "后端手册", link: "/backend/introduction", activeMatch: "/backend/" },
  {
    text: "前端手册",
    link: "/frontend/introduction",
    activeMatch: "/frontend/",
  },
  {
    text: "探索未知",
    items: [
      {
        text: "关于我们",
        items: [
          {
            text: "官方网站",
            link: "https://www.xihanfun.com",
          },
          {
            text: "组织文档",
            link: "https://docs.xihanfun.com",
          },
        ],
      },
      {
        text: "生态文档",
        items: [
          {
            text: "后端 | 开发框架",
            link: "https://framework.docs.xihanfun.com",
          },
          {
            text: "前端 | 视图组件",
            link: "https://ui.docs.xihanfun.com",
          },
        ],
      },
      {
        text: "引用下载",
        items: [
          {
            text: "后端 | nuget",
            link: "https://www.nuget.org/profiles/XiHanFun",
          },
          {
            text: "前端 | npm",
            link: "https://www.npmjs.com/org/xihan-ui",
          },
        ],
      },
      {
        text: "在线体验",
        items: [
          {
            text: "后端 | 开发框架",
            link: "https://framework.xihanfun.com",
          },
          {
            text: "前端 | 视图组件",
            link: "https://ui.xihanfun.com",
          },
          {
            text: "用例 | 基础应用",
            link: "https://basicapp.xihanfun.com",
          },
        ],
      },
    ],
  },
  {
    text: "代码仓库",
    items: [
      {
        text: "Github主库(国际)",
        link: "https://github.com/XiHanFun/XiHan.BasicApp",
      },
      {
        text: "Gitee同步备库(国内)",
        link: "https://gitee.com/XiHanFun/XiHan.BasicApp",
      },
      {
        text: "GitCode同步备库(国内)",
        link: "https://gitcode.com/XiHanFun/XiHan.BasicApp",
      },
    ],
  },
  {
    text: "参与贡献",
    items: [
      {
        text: "公约",
        link: "https://docs.xihanfun.com/cosmos/code-of-conduct",
      },
      {
        text: "指南",
        link: "https://docs.xihanfun.com/cosmos/contributing",
      },
      {
        text: "贡献者",
        link: "https://docs.xihanfun.com/cosmos/contributors",
      },
      {
        text: "支持&赞助",
        link: "https://docs.xihanfun.com/cosmos/sponsor",
      },
    ],
  },
  {
    text: `v${version}`,
    items: [{ text: "更新日志", link: "/changelog" }],
  },
];

function searchOptions(): Partial<DefaultTheme.AlgoliaSearchOptions> {
  return {
    placeholder: "搜索文档",
    translations: {
      button: {
        buttonText: "搜索文档",
        buttonAriaLabel: "搜索文档",
      },
      modal: {
        searchBox: {
          resetButtonTitle: "清除查询条件",
          resetButtonAriaLabel: "清除查询条件",
          cancelButtonText: "取消",
          cancelButtonAriaLabel: "取消",
        },
        startScreen: {
          recentSearchesTitle: "搜索历史",
          noRecentSearchesText: "没有搜索历史",
          saveRecentSearchButtonTitle: "保存至搜索历史",
          removeRecentSearchButtonTitle: "从搜索历史中移除",
          favoriteSearchesTitle: "收藏",
          removeFavoriteSearchButtonTitle: "从收藏中移除",
        },
        errorScreen: {
          titleText: "无法获取结果",
          helpText: "你可能需要检查你的网络连接",
        },
        footer: {
          selectText: "选择",
          navigateText: "切换",
          closeText: "关闭",
          searchByText: "搜索提供者",
        },
        noResultsScreen: {
          noResultsText: "无法找到相关结果",
          suggestedQueryText: "你可以尝试查询",
          reportMissingResultsText: "你认为该查询应该有结果？",
          reportMissingResultsLinkText: "点击反馈",
        },
      },
    },
  };
}

export default defineConfig({
  lang: "zh-CN",
  title: title,
  description: description,
  head: head,
  lastUpdated: true,
  cleanUrls: true,
  themeConfig: {
    logo: logo,
    socialLinks: [
      { icon: "github", link: "https://github.com/XiHanFun/XiHan.BasicApp" },
      { icon: "gitee", link: "https://gitee.com/XiHanFun/XiHan.BasicApp" },
      { icon: "gitcode", link: "https://gitcode.com/XiHanFun/XiHan.BasicApp" },
    ],
    search: {
      provider: "local",
      options: searchOptions(),
    },
    nav: nav,
    sidebar: sidebar,
    docFooter: {
      prev: "上一页",
      next: "下一页",
    },
    outline: {
      label: "目录",
      level: "deep",
    },
    langMenuLabel: "多语言",
    returnToTopLabel: "回到顶部",
    sidebarMenuLabel: "菜单",
    darkModeSwitchLabel: "主题",
    lightModeSwitchTitle: "切换到浅色模式",
    darkModeSwitchTitle: "切换到深色模式",
    skipToContentLabel: "跳转到内容",
    notFound: {
      title: "页面未找到",
      quote:
        "但如果你不改变方向，并且继续寻找，你可能最终会到达你所前往的地方。",
      linkLabel: "前往首页",
      linkText: "带我回首页",
    },
    editLink: {
      text: "在 GitHub 上编辑此页",
      pattern:
        "https://github.com/XiHanFun/XiHan.BasicApp/tree/main/docs/:path",
    },
    lastUpdated: {
      text: "最后更新于",
    },
    footer: {
      message:
        "Released under The <a href='https://opensource.org/license/MIT' target='_blank'>MIT</a> License",
      copyright:
        "Copyright ©2021-Present <a href='https://www.xihanfun.com' target='_blank'>XiHanFun</a> and contributors.",
    },
  },
});
