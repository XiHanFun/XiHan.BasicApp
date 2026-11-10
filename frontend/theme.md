来源：https://basicapp.docs.xihanfun.com/frontend/theme

# 布局与主题

本页说明前端的设计令牌、样式约束、布局与偏好同步，适用于前端开发者。

XiHan.UI 是组件与样式契约的真源：组件只认 `--xh-*` 语义令牌，应用把自己的配色、圆角与布局语义映射上去。

## 令牌分层

| 层 | 位置 | 作用 |
| --- | --- | --- |
| 应用令牌 | `packages/design/variables.css` | HSL 分量形式的应用令牌（`--primary`、`--background`、`--radius`…），明暗两套 |
| 令牌桥 | `packages/design/xihan-ui.css` | 在 `xihan.overrides` 层把 XiHan.UI 的 `--xh-*` 改写到应用令牌上；`:root`、`.dark` 与浮层壳的 `[data-theme]` 各写一份，组件库与浮层随之换色 |
| Tailwind | `src/styles/index.css` | 样式总入口：定死层序，`@theme inline` 把页面用到的主题色与排版工具类映射到 `hsl(var(--x))` 与 `--xh-*`，在使用处解析，嵌套的 `.dark` 容器随之切换 |
| 应用样式 | `packages/design/*.css`、`src/styles/admin-page.css` | 宿主基线、动效令牌桥、页面过渡、原生滚动条与共享结构件，只读 `--xh-*` 语义令牌 |

偏好由 `useTheme` 与 `useHtmlStyle` 写到 `<html>`：明暗写 `.dark` / `.light` 类与 `data-theme`，密度写 `data-density`，图表配色写 `data-xh-chart-palette`，主题色写 `--primary` 与派生的品牌色阶，圆角写 `--radius` / `--radius-card`，另有根字号、灰度与色弱滤镜。

## 约束

| 项 | 规则 |
| --- | --- |
| 圆角 | 偏好设置的 `--radius` 接到组件库的圆角种子 `--xh-radius-seed`：控件、承载面与浮层同一档（卡片取 `--radius-card`），内嵌件（菜单与下拉的行、方框、已选标签）取一半，对话框与抽屉朝页面的两角、通知卡片取 1.25 倍；自写样式只写形状角色 `--xh-shape-control / surface / overlay / inset / sheet`，不写 `--xh-radius-*` 档位；`pill` 只用于明确的胶囊语义，正方盒取 `circle` |
| 按压 | 按钮按下只换面、不缩放（随组件库缺省，不改写 `--xh-motion-scale-press`）；减弱动效下去掉非必要的位移 |
| 聚焦环 | 与组件库同一道：`outline: var(--xh-ring-width) solid var(--xh-ring-focus); outline-offset: var(--xh-ring-offset)`，2px 画在元素自己的盒内，外沿与边框外沿重合，裁切容器不必为它留位。面是实心或任意颜色时内侧衬一道底色间隔线：`box-shadow: inset 0 0 0 calc(var(--xh-ring-width) + var(--xh-ring-gap-width)) var(--xh-ring-gap)`（有边框时扣掉边框宽） |
| 浮层 | 锚定浮层走 frosted（实体底 + 描边 + 柔和投影，不叠背景模糊）；玻璃拟态只用在命令面板、登录与注册页的卡片和右上角工具条、灵动岛、标签总览遮罩、控制中心面板、页面切换的加载遮罩，统一取 `packages/design/ui.css` 的 `.app-glass` / `--app-glass-*` |
| 取值 | 颜色、间距、圆角、阴影、层级、动效只用语义令牌，不写裸色值 |
| CSS 写法 | `.css` 文件和 `.vue` 的 `<style>` 块用与 XiHan.UI 皮肤同一套 stylelint 规则（`stylelint.config.mjs`，随 `pnpm lint`）：逻辑属性；颜色只取 `--xh-*` 或由它调出的 `color-mix()`，不读应用配色变量，组件局部变量的取值也一样；圆角、内距、间隙只取 `--xh-shape-*` / `--xh-space-*`；过渡逐个列属性、不写 `all`；字号取 `--xh-font-size-*` / `--xh-text-*-size`、图标尺寸取 `--xh-glyph-size-*`，不写 px、rem；原生滚动条只在 `scrollbar.css` 里给 |
| 裸 px | `pnpm px:check`：`.css` 与 `.vue` 的 `<style>` 里的裸 px 按文件计数（`scripts/px-allowlist.json`），只许减不许增，新文件不进白名单；`0px`、`1px` / `2px`（描边与发丝线）、`9999px` 不计 |
| 令牌 | 引用的 `--xh-*` 必须存在于已安装的组件库里（`pnpm token:check`） |
| 修补 | 不用页面 CSS 修补组件库的通用缺陷，回 XiHan.UI 修；令牌桥只映射品牌与布局语义，不改组件结构 |
| 风格 | 紧凑、任务导向，不在控制台页面用营销式大标题、装饰性 Hero、夸张渐变或装饰留白 |

## 布局

`packages/layouts/basic` 提供前端壳：侧栏、顶栏、标签栏、面包屑、内容区、页脚。偏好中心可切换布局形态（`side`、`side-mixed`、`top`、`mix`、`header-mix`、`header-sidebar`、`full`）、侧栏宽度、标签样式、内容宽度、页脚等。

模块经 `registerShellExtension`（`~/stores`）向壳层注入顶栏按钮、布局级浮层与布局集成钩子（如聊天的顶栏按钮、浮动面板与实时连接），经 `registerWorkbenchWidget` 向工作台登记列表型小组件；布局不依赖具体模块，删掉模块目录即随之消失。

壳层的常驻能力：

| 能力 | 说明 |
| --- | --- |
| 全局搜索 | 命令面板，`Ctrl / ⌘ + K`，支持拼音首字母直达页面 |
| 标签总览 | `Alt + B` |
| 收藏 | 页面收藏，跨端同步 |
| 分屏 | 两个页面左右并排，窄屏不可用 |
| 通知中心 | 顶栏，读收件箱，收到 `ReceiveNotification` 时刷新 |
| 公告拦截 | 强制阅读的公告以不可关闭的弹窗逐条拦截，登录弹窗公告随后逐条弹出 |
| 灵动岛 | 实时连接状态、后台任务进度（`TaskProgress`）、导出与设置同步等过程提示 |
| 锁屏 | `Alt + L`，调 `auth.lock` / `auth.unlock`，服务端强制 |
| 切换租户、切换角色、模仿 | 顶栏入口（切换租户在控制中心 `/control-center`）；换会话后整页重建 |
| 水印 | 偏好中心开关 |
| 新版本提示 | 正式部署的站点定时比对 `version.json`，有新版本时提示刷新 |

快捷键还有 `Alt + Q` 退出登录，偏好中心里可逐项关闭。

## 偏好与同步

亮 / 暗 / 跟随系统、主题色、圆角、密度、字号、色弱 / 灰度、布局、区域与时区等偏好存在本地，并经用户设置（`user-settings`）跨端同步：开着同步时以后端那份为准，登录或刷新时拉取。偏好与收藏在其他设备上改了经 `UserSettingChanged` 实时应用；搜索设置、表格设置与看板布局在打开页面时取后端那份。偏好、收藏、搜索、表格与看板各有同步开关（设备本地，不上行），缺省都开。

## 无障碍与适配

- 只有图标的操作要有可访问名称或提示，键盘焦点可见；
- 视觉或交互改动在真实浏览器里检查：亮 / 暗色、窄视口（手机宽度）、键盘导航与焦点、减少动态效果；按影响范围再看 RTL、粗指针、强制色与打印。jsdom 不能代替真实布局与计算样式。

## 下一步

- [常用组件](./components) · [字体图标](./icon) · [国际化与时区](./i18n)
