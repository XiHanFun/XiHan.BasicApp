# 6.0 管理端开发参考

真源顺序：后端契约（`frontend-next/contracts/openapi.json` 与生成的 `packages/contracts/*`）→ 代码与门禁脚本 → 方案第 6 节 → `docs-next/frontend/*.md`。

## 目录

```text
frontend-next/
├─ contracts/openapi.json、realtime.json   后端导出，入库
├─ scripts/                                 gen-contracts 与各门禁（px / token / i18n / modules / permissions）
├─ packages/   (~)                          contracts、request、stores、router、layouts、components（含 SchemaPage）、locales、design …
└─ src/        (@)                          app、router、styles、modules/<包>/{views/**, locales/<区域>.ts, setup.ts}
```

- `packages/` 不引用 `@/`；模块之间不互相引用，共用的东西放 `packages/`。
- 注释（`//`、`/* */`、JSDoc、Vue 模板的 `<!-- -->` 与样式里的 `/* */`）句末不加「。」，一行里有多句时句间照常用「。」；与后端同一写法，前端没有门禁守它，靠评审。文案（`locales`、`t(...)` 的值）不是注释，照常带标点。
- 模块的 `setup.ts` 登记本模块文案（`registerModuleLocales(import.meta.glob(...))`）；视图键全局唯一（`modules:check`）。可选模块的 `setup.ts` 另把要接进公共包的函数登记上（如打印的 `configurePrinting`），只登记函数、不发请求。壳层的顶栏按钮、浮层经 `registerShellExtension`，看板的列表型小组件经 `registerWorkbenchWidget`（挂在模块的一个页面上，后端不下发页面即隐藏，方案修订（一百））。
- 设计器（Web Components）共用的接线在 `packages/designers`，各款设计器在自己的包或子路径（`packages/printing`、`~/printing/designer`）；依赖走 catalog，模板里 `xh-` 开头的标签是自定义元素（`vite.config.ts` 的 `isCustomElement`）。
- 新增 `src/modules/*/views/**/index.vue` 后 Vitest 可能还看不到（`node_modules/.vitest-cache` 缓存了 `import.meta.glob` 的结果），先删缓存再跑。
- `src/types/env.d.ts` 的 `declare module '*.vue'` 让缺失的 `.vue` 导入照样过类型检查与构建，删 `.vue` 文件后要 grep 导入。

## 契约层

- 操作客户端、请求 / 响应类型、枚举值集、权限码表（`permissionDefinitions`，显示名与说明是英文、不拿来显示；界面上的权限名取 `permissions.list`，只有权限码时用 access 的 `usePermissionNames()`）、推送方法名（`realtime.ts`）都从 `~/contracts` 取，不手写路由、字段或枚举。
- 路由由框架 HTTP 约定决定：`list` 走 `GET /<资源>`（带 `PageRequest` 的是 `POST /<资源>:list`），`get` / `delete` 走资源路径或查询串，`create` → `POST`、`update` → `PUT` 资源根，其余写动词 `POST /<资源>:<动词>`，声明了 `ReadOnly` 的查询在没有嵌套对象时走 `GET`。没有请求体的操作照样调 `xxx({})`。
- 写页面前先 `pnpm gen:contracts` 看 `operations.ts` 里的模板，再写调用与测试。
- 请求层（`~/request`）统一处理认证、刷新、区域与 Problem Details：`SECURITY_DENIED_001` / `003` 先单飞刷新再重放，`IDENTITY_DENIED_001` 回登录页，`IDENTITY_DENIED_002` 进锁屏，`RUNTIME_UNAVAILABLE_003` 与 `KERNEL_UNAVAILABLE_001` 走维护提示；页面不重复实现。登录与刷新以 `anonymous: true` 调用。
- 后端 `long` 在 6.0 的报文里是数字（契约类型 `number`，4.x 是数字字符串）；时间按偏好设置的应用时区显示。
- 宿主普通端点的下载（产物包、导出结果）用 `~/request` 的 `downloadEndpoint(path)`：带令牌，未认证时与操作一样换令牌重放一次，返回 `{ blob, fileName }` 再交给 `downloadBlob`；不自己拼 `fetch`。

## 页面

- 列表页：`SchemaPage` + `PageSchema`（不带泛型，回调里 `asRow(row)` 转成本页行类型）；资源适配器 `use-<页面>.ts` 用 `toPageRequest(params, { filters: { 字段: 'eq' | 'contains' }, ranges, multiples })` 与 `toSchemaPageResult`，只登记后端 `Queries/` 白名单里的字段。区间与多选要在映射里登记 `ranges`（拆成 ge / le）与 `multiples`（合成 in），后端也要接；数字上下限的两个输入框用 `bounds`（搜索键 → 后端字段与 ge / le）；不用 `dictionaryCode`，状态列用 `render` + `renderTag(label, tone)`。样板见 `src/modules/settings/views/configs`。
- 编辑弹窗：`XEditModal` + `XhFormRoot`（表单写 `novalidate`），每个字段有可见标签；字段错误用 `XhFieldRoot :invalid` + `XhFieldErrorText`；数字字段用 `XhNumberFieldRoot`（`v-model:value` 是字符串）。
- 弹窗里的表格与列表页同样式（`XDataTable` 缺省尺寸 + 列分隔线，分页出同款底栏）。`XDataTable` 的 `rowKey` 缺省是 4.x 的 `basicId`，6.0 的行要显式传 `row-key`；大量只读行放 `shallowRef`。
- 公共组件里有从 4.x 搬来的代码编辑器 `XCodeEditor`（monaco，只读预览与模板编辑）与文件引用上传 `XFileRefUpload`（存文件库的文件 ID），用到时直接取 `~/components`。
- 代码生成的前端模板（后端 `CodeGeneration/Seeds/Templates/Frontend/*.sbn`）照这里的页面写法出页面与 schema：改了 SchemaPage、编辑弹窗或契约层的写法要同时改模板，并跑后端技能里的代码生成退出验证。
- 列 `render` / 树 `render-label` 用 `h()` 生成的节点不带页面 scope，样式写 `:deep()`。
- 权限：按钮 `permission` 与 `can('…')` 写三段权限码字面量，`permissions:check` 核对它们都在生成的权限表里；`auth.can` 精确匹配。
- 换会话（切换租户、模仿、退出模仿）一律 `relaunchApp()` 整页重建。

## 文案

- 10 种区域（`en-US`、`zh-CN`、`zh-TW`、`ja-JP`、`ko-KR`、`de-DE`、`es-ES`、`fr-FR`、`pt-BR`、`ru-RU`）键集对称，源码静态引用的 `t('…')` 键都存在（`i18n:check`）。
- 菜单标题键来自后端页面登记；vue-i18n 文案里不能有裸 `@`。

## 视觉

规则以仓库根 `AGENTS.md`「前端架构与设计」为准（圆角只写 `--xh-shape-*`、按下缩放、盒内 2px 聚焦环、浮层 frosted、禁止 glass、紧凑不加营销式标题）。不新增裸 px：`scripts/px-allowlist.json` 是按文件计数的白名单，只许减不许增（`node scripts/check-px.mjs --update` 收紧）；引用的 `--xh-*` 令牌要在当前组件库版本里存在（`token:check`）。

## 测试与验证

- Vitest + jsdom；`scripts/vitest-setup.ts` 固定 `navigator.languages = ['zh-CN']` 并预装十区域文案，`vi.resetModules()` 之后要自己重装区域。
- 门禁：`pnpm check`（八项）、`pnpm test`、`pnpm build`。改共享组件、路由、请求、权限或主题时三者都跑。
- 浏览器验证：开发服务器 9100，`/api`、`/blobs`、`/hubs` 代理到后端 9710；用正式账号登录（验证码请用户输入），看亮 / 暗色、窄视口、键盘与焦点、减少动态效果。
- XiHan.UI 联调：`pnpm-workspace.yaml` 临时加 `overrides` 指向同级 XiHan.UI 源码，不提交；组件库缺陷在 XiHan.UI 修。
