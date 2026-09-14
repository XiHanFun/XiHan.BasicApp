---
name: xihan-basicapp-frontend-development
description: 实现、重构、审查或测试 XiHan.BasicApp Vue 管理端时使用。覆盖 XiHan.UI、类型化 API、Pinia、动态路由、插件模块、权限可见性、统一视觉和浏览器验证；后端协议变化使用全栈契约技能。
---

# XiHan.BasicApp 前端开发

开始前读 `references/frontend.md`，并检查相邻页面、API 类型、store、路由、语言资源和现有 XiHan.UI 组件。

## 工程边界

- 页面位于 `frontend/src/views`，API 与 DTO 类型位于 `frontend/src/api/modules`，跨页面能力位于 `frontend/packages`。
- 页面和 store 通过现有 request 与类型化 API facade 请求后端，不直接调用 `fetch`。
- 跨页面状态使用 Pinia；页面局部筛选、弹窗、选择和分页留在页面或局部 composable。
- 后端菜单驱动动态路由；可选模块保持后端注册、前端 `setup.ts`、页面和语言资源一致。
- UI 使用 XiHan.UI，不引入 Naive UI、Ant Design Vue 或应用私有组件皮肤。
- 通用组件缺陷回到 XiHan.UI 修复；BasicApp 只负责业务组合和应用主题桥。

## 统一视觉

- 普通 control 4px、surface 8px、overlay 12px；pill 仅用于明确胶囊身份。
- 离散按钮按下 120ms 缩放至 0.97，释放 200ms；禁止 glass，只允许 frosted 柔和模糊。
- 颜色、间距、圆角、阴影和动效使用 XiHan.UI 语义令牌，不扩大 px/token allowlist 掩盖问题。
- 管理端保持紧凑、清晰和任务导向，并验证暗色、窄屏、键盘焦点与 reduced motion。

## 验证

在 `frontend/` 运行 `pnpm check`、相关 `pnpm test` 和 `pnpm build`。视觉与浮层行为必须在真实浏览器验证，jsdom 不替代布局和计算样式。
