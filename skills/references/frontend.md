# 前端开发与统一设计规范

## 技术与目录

- 使用 Vue 3、TypeScript、Vite、Pinia、Vue Router 和 XiHan.UI。
- `@` 指向 `frontend/src`，`~` 指向 `frontend/packages`。
- 页面放 `src/views`，API 与相邻 DTO 类型放 `src/api/modules`，跨页面能力放 `packages`。
- 可选模块由 `src/modules/<module>/setup.ts` 自动发现；保持模块后端开关、路由与语言注册一致。
- 不使用或重新引入 Naive UI、Ant Design Vue，也不复制 XiHan.UI 源码形成应用私有分叉。

## 类型化 API

- 页面和 store 不直接调用 `fetch`，通过现有 request 包与类型化 API facade。
- Dynamic API 复用 `createDynamicApiClient`、`createCommandApi`、`createReadApi` 和 `formatDynamicApiRouteValue`。
- `*.types.ts` 与后端 DTO 的字段、nullable、分页、ID 和日期类型严格一致。
- 请求层统一处理认证、刷新、语言、时区和响应拆包；页面不重复实现。
- 契约不匹配时修正真源，不用 `as any`、默认空对象或双字段兼容绕过。

## 状态、路由与权限

- 跨页面状态使用 Pinia；页面局部筛选、抽屉、弹窗、选中、分页和 loading 留在页面或局部 composable。
- 动态路由由后端菜单和前端组件路径映射生成；不要硬编码另一份同名业务路由。
- 静态模式只用于仓库现有 `VITE_AUTH_ROUTE_MODE=static` 场景。
- UI 使用现有权限 hook/store 和后端权限码控制可见性，但安全判断始终由后端执行。
- Icon-only 操作必须有可访问名称或 Tooltip，键盘焦点可见。

## 统一视觉

| 对象 | 圆角 |
| --- | --- |
| 普通 control | 4px |
| card / surface | 8px |
| dialog / popover / overlay | 12px |
| pill | 仅标签、状态或明确胶囊身份 |

- 使用 XiHan.UI 语义令牌确定颜色、间距、圆角、阴影、层级和动效。
- 不新增裸色值或任意 px；确有技术必要时先说明原因，并通过当前 px/token 门禁。
- 离散 Action Control 按下使用 120ms 缩放到 0.97，释放 200ms 回到 1；reduced motion 下关闭非必要位移和缩放。
- 禁止 glass 材质和兼容别名；透明浮层只允许 frosted 柔和模糊，并提供 reduced transparency 下的不透明表现。
- 管理端优先信息密度、扫描效率和清晰层级，避免营销式 Hero、过大标题、夸张渐变与装饰性留白。
- 暗色、窄视口、RTL、键盘、粗指针、forced colors 和打印样式按组件影响范围验证。

## 组件选择

1. 先搜索现有 XiHan.UI 组件和 `frontend/packages/components`。
2. 业务组合留在 BasicApp；通用组件能力回到 XiHan.UI 实现。
3. 不用页面 CSS 修补 XiHan.UI 的通用缺陷；先确认是组件问题还是应用组合问题。
4. 主题桥只映射应用品牌与布局语义，不重写每个组件 anatomy。

## 验证

在 `frontend/` 执行：

```bash
pnpm run check
pnpm run test
pnpm run build
```

视觉和交互修改还要在真实浏览器检查：正常/加载/空/错误/禁用状态，亮暗主题，窄屏，键盘焦点，浮层定位，退出动画和 reduced motion。jsdom 不能代替真实布局与计算样式验证。
