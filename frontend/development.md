来源：https://basicapp.docs.xihanfun.com/frontend/development

# 开发流程

本页以 Settings 模块的参数页为例，说明前端新增页面的步骤，适用于前端开发者。阅读前建议先了解[框架简介](./introduction)。

## 一、确定后端契约

前端不手写接口：操作、请求 / 响应、权限码与页面登记都在后端（见[后端开发流程](../backend/development)）。后端改完先导出契约，再生成契约层：

```bash
# 仓库根；dotnet run 以工程目录为工作目录，--out 要给绝对路径
dotnet run --project backend/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend/contracts"
cd frontend && pnpm gen:contracts
```

`contracts/*` 与 `packages/contracts/*` 都入库，和后端改动、页面改动放在同一个提交。写调用前先看生成的 `operations.ts`（路由表与调用函数）和 `types.ts`。契约不对就回后端改，不在前端补字段或写 `as any`。

## 二、创建页面

页面路径由后端页面登记决定：路由 `/settings/configs` → 组件路径 `settings/configs/index` → 文件 `src/modules/settings/views/configs/index.vue`。

```vue
<script lang="ts" setup>
import type { ListFieldSchema, PageSchema } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { SchemaPage } from '~/components'
import { configsResource } from './use-configs'

defineOptions({ name: 'SettingsConfigsView' })
const { t } = useI18n()
const resource = configsResource()

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('settings.configs.search'), dataType: 'string', visible: false, searchable: true },
  { key: 'name', title: t('settings.configs.fields.name'), dataType: 'string', sortable: true, exportable: true, minWidth: 160 },
  // …
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'settings.configs',
  pageName: t('settings.configs.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('settings.configs.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'settings.configs.create' },
  ],
}))
</script>

<template>
  <SchemaPage :schema="schema" />
</template>
```

- 列表页一律用 [SchemaPage](./schema-page)。资源适配器 `use-<页面>.ts` 调生成的操作客户端（`import { configs } from '~/contracts'`），经 `toPageRequest` / `toSchemaPageResult` 对接分页。
- 新增 / 编辑用 `XEditModal` + `XhFormRoot`：每个字段都有可见标签；单个字段的说明写在 `XFieldLabel` 的 `help` 上（字段名后面的问号提示），不放在字段下方；字段错误用 `XhFieldRoot :invalid` + `XhFieldErrorText` 显示 `ApiError.errors`。
- 按钮的 `permission` 直接填操作的权限码，门禁核对它存在于生成的 `permissions.ts`。
- 大页面拆成 `index.vue` + `use-*.ts` 组合式函数；样板见 `src/modules/settings/views/configs`。
- 新模块要有 `setup.ts`（至少登记文案）、`locales/` 与 `views/`，模块名与后端能力包一致。

## 三、文案

在 `src/modules/settings/locales/<区域>.ts` 加键，10 种区域都要有、键集完全对称。菜单标题的键来自后端页面登记（如 `settings.configs.title`）；枚举文案在前端按契约字面量静态写出。见[国际化与时区](./i18n)。

## 四、样式

- 只用 XiHan.UI 组件与语义令牌；不新增裸 px（`pnpm px:check` 按文件计数，只许减不许增）；
- 自写样式的圆角只写形状角色 `--xh-shape-control / surface / overlay / inset / sheet`，不写 `--xh-radius-*` 档位；按下只换面、不缩放；浮层只用 frosted，玻璃拟态只在指定几处用 `.app-glass`（见主题）；
- 列 `render` 用 `h()` 生成的节点不带页面 scope，样式写 `:deep()`；
- 前端保持紧凑，不加营销式大标题。

细则见[布局与主题](./theme)。

## 五、门禁与测试

```bash
pnpm check   # type-check → lint → px → token → i18n → modules → tests → headers → contracts → permissions
pnpm test    # Vitest + jsdom
pnpm build
```

| 门禁 | 检查 |
| --- | --- |
| `type-check` | `vue-tsc --noEmit` |
| `lint` | oxlint + eslint（`@antfu/eslint-config`）+ stylelint（`.css` 与 `.vue` 的 `<style>`，后者经 postcss-html 解析）；`packages/` 不得引用 `@/` |
| `px:check` | 样式里的裸 px 按文件计数（`scripts/px-allowlist.json`，`--update` 收紧） |
| `token:check` | 引用的 `--xh-*` 令牌在已安装的组件库里存在 |
| `i18n:check` | 10 种区域键集对称；源码静态引用的 `t('…')` 键存在 |
| `modules:check` | 模块名是后端能力包名；有 `setup.ts`、`locales/`、`views/`；视图路径是 kebab-case 且全局唯一 |
| `tests:check` | 测试只在所属包或模块的 `tests/` 目录里，命名 `*.spec.ts` |
| `headers:check` | `src`、`packages` 下的 `.ts`、`.vue`、`.css` 以版权头开头（测试不加）；`node scripts/check-headers.mjs --write` 补齐 |
| `contracts:check` | 按 `contracts/*` 重新生成契约层，与入库文件一致 |
| `permissions:check` | `permission:`、`permissions:`、`can('…')`、`hasPermission('…')` 里的权限码都存在于 `permissions.ts` |

- 单测放在所属包或模块的 `tests/` 目录（`packages/<包>/tests/`、`src/modules/<模块>/tests/`），按被测文件的子路径摆放，命名 `*.spec.ts`，导入写 `~/`、`@/` 别名；`scripts/vitest-setup.ts` 把 `navigator.languages` 固定为 `zh-CN` 并预装十种区域的文案，`vi.resetModules()` 之后要自己重装。
- 新增 `views/**/index.vue` 后 Vitest 可能看不到它（`node_modules/.vitest-cache` 缓存了 `import.meta.glob` 的结果），先删缓存再跑。
- 修改共享组件、路由、请求、权限或主题时三项都跑。视觉与交互改动还要在真实浏览器里看亮 / 暗色、窄视口、键盘与焦点、减少动态效果。

### 端到端冒烟

`e2e/` 下是 Playwright 用例，按真实用户的操作走一条主干：超管登录与退出、开通企业版租户并初始化管理员、租户管理员以租户 ID 登录、建角色与成员并直授申请权限、成员提交角色申请、管理员在审批中心同意、成员看到已批准。

```bash
pnpm exec playwright install chromium   # 首次
pnpm e2e
```

`playwright.config.ts` 自己起服务，不碰 9710 / 9100 上的开发服务：

| 服务 | 做法 |
| --- | --- |
| 后端 | 在宿主目录 `dotnet run -c Release`，Development 环境；端口 9730（gRPC 9731），库 `xihan_basicapp_e2e`（启动时建库、建表、播种，`E2E_DATABASE` 可换连接串），关掉图形验证码，超管密码取 `e2e/env.ts`；日志出现 `Startup seeding completed.` 才算就绪 |
| 前端 | `pnpm build` 之后 `vite preview`，端口 9130，代理到 9730 |

本机这两个端口已有服务就直接复用。库在本机保留，每次开通的租户与账号带运行后缀，不会重名。失败时 `test-results/<用例>/` 里有截图、轨迹与页面结构快照（`error-context.md`），`playwright-report/` 是 HTML 报告。CI 的端到端作业带 PostgreSQL 服务容器跑同一套用例，失败时上传这两个目录。

## 六、XiHan.UI 本地联调

常规开发与 CI 用 `catalog` 里的发布版。联调未发布的组件库改动时，在 `pnpm-workspace.yaml` 里临时加 `overrides`，把 `@xihan-ui/*` 指到同级源码（`link:../../XiHan.UI/ui/packages/<分组>/<包>`），overrides 不提交。组件库的缺陷回 XiHan.UI 修，BasicApp 不写兼容。

## 下一步

- [菜单与路由](./routing) · [契约与请求](./request) · [Schema 驱动页面](./schema-page)
