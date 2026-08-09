# 布局与主题

外观相关的一切：布局结构、Tailwind v4 的用法与陷阱、主题系统、偏好同步。

## 布局

`packages/layouts/` 提供整套外壳：侧边栏、顶栏、标签页（tabbar）、内容区、页脚。

### 滚动模型

::: warning 滚动源不是 `window`
布局根是**定高**的（`h-full`），滚动发生在**内容容器内部**。这意味着：

- 监听 `window` 的 scroll 事件拿不到滚动；
- 需要吸顶/吸底的整屏页直接用 `h-full` 或 `XPageShell` 即可；
- **不要写 per-page 的 JS 定高 hack**。

回到顶部、路由切换重置滚动都已由布局适配器统一处理。
:::

## 样式：Tailwind CSS 4（CSS-first）

用 `@tailwindcss/vite` + **CSS-first `@theme`**（入口 `src/styles/index.css`），**没有 JS config 文件**。

三条关键约定：

| 约定 | 说明 |
| --- | --- |
| **只引入 theme + utilities，不引入 preflight** | 基础重置由 Naive UI 与 `design/global.css` 的最小重置负责，避免与 Naive UI 打架 |
| 颜色令牌用运行时 HSL CSS 变量 | `--color-primary: hsl(var(--primary))` 等，保证明暗与主题色能动态切换 |
| 暗色走 class 策略 | `@custom-variant dark (&:where(.dark, .dark *))` |

### 嵌套 `.dark` 的坑

::: danger 深色侧栏里 `bg-*` / `text-*` 不变暗
`@theme` 里声明的 `--color-*` **只在 `:root` 解析**。深色侧栏、深色子栏、深色顶栏这类**嵌套 `.dark`** 的容器里，Tailwind 的颜色工具类拿不到暗色值，表现是「局部深色区域里的元素还是亮色」。

修法：在 `variables.css` 的 **`:root, .dark`** 块里**重新声明全部 `--color-*`**，让嵌套作用域也能解析到。
:::

## 主题系统

`useTheme()`（`~/hooks/useTheme.ts`）管理外观：

| 维度 | 可调 |
| --- | --- |
| 明暗 | 亮 / 暗 / 跟随系统 |
| 主题色 | 任意品牌色 |
| 形状 | 圆角 |
| 排版 | 字号 |

### Material You 动态取色

**从单个品牌色派生整套和谐色阶**——辅色、容器色、前景色、聚焦环、带品牌色相的中性色，明暗自适应，以内联 CSS 变量写到根元素。

所以**换主题色不需要改任何 CSS**：改一个色值，整套配色跟着重算。

主色、明暗、圆角、字号变化都会同步到 CSS 变量，非 Naive 的自定义元素也能直接用这些变量。

### 切换动画

`startViewTransition` 从点击处圆形扩散——明暗切换时的视觉过渡。

## 偏好同步

UI 偏好走 `UserSettingAppService`，按**场景 + key** 存取：

```text
localStorage（事实源）
   ↕
后端 SysUserSetting（用户 × 场景 × 设置键）
   ↕ SignalR UserSettingChanged
其它设备已打开的页面即时应用
```

三条策略：

1. **localStorage 是事实源**——后端加载成功则覆盖本地，保存失败静默忽略（尽力而为，不阻断交互）。
2. **服务端不解释 value**，只作跨端状态载体。
3. 其它设备保存后经 SignalR 广播实时应用。

Schema 页的列设置、搜索设置、个人视图按 `pageCode` 走同一套机制，见 [Schema 驱动页面](./schema-page#偏好与视图)。

::: tip `pageCode` 要稳定
偏好按 `pageCode` 存储，**改了等于用户的所有个性化配置丢失**。页面上线后不要再动它。
:::

## 排查

| 现象 | 原因 |
| --- | --- |
| 深色区域里的元素还是亮色 | 嵌套 `.dark` 令牌问题，见上面的 danger 块 |
| 改了主题色但某些地方没变 | 那些地方写死了颜色值，没用 CSS 变量 |
| 页面滚不动 / 吸顶失效 | 用了 `window` 滚动的假设；改用 `h-full` 或 `XPageShell` |
| 样式被 Naive UI 覆盖 | preflight 是**故意关闭**的，别去打开——会破坏 Naive UI |
| 偏好换台设备就没了 | 后端保存失败被静默忽略了，查接口是否可用 |

## 相关页面

- [国际化](./i18n)：文案与枚举标签
- [字体图标](./icon)：图标用法与离线限制
- [常用组件](./components)：布局相关组件
- [框架简介](./introduction)：前端分层与启动引导
