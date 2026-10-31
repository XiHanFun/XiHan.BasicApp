# 开发流程

以「给 Settings 模块加一个页面」为线索。先读[框架简介](./introduction)。

## 一、后端先定契约

前端不手写接口：操作、DTO、权限码与页面登记都在后端（见[后端开发流程](../backend/development)）。后端改完导出契约：

```bash
# 仓库根
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend-next/contracts"
cd frontend-next && pnpm gen:contracts
```

`contracts/openapi.json` 与 `packages/contracts/*` 都是生成物、都入库，和后端改动、页面改动**同一个提交**。

## 二、建页面

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
  resource,
  actions: [
    { key: 'create', title: t('settings.configs.actions.create'), scope: 'page', icon: 'lucide:plus', permission: 'settings.configs.create' },
  ],
}))
</script>

<template>
  <SchemaPage :schema="schema" />
</template>
```

- 列表页一律用 [SchemaPage](./schema-page)；新增 / 编辑用 `XEditModal`，**每个字段都有可见标签**。
- 资源适配器（`use-<页面>.ts`）用生成的操作客户端：`import { configs } from '~/contracts'`，经 `toPageRequest` / `toSchemaPageResult` 对接分页。
- 按钮的 `permission` 直接填操作的权限码；门禁会核对它存在于生成的 `permissions.ts`。
- 大页面拆成 `index.vue` + `use-*.ts` 组合式函数。

## 三、文案

在 `src/modules/settings/locales/<区域>.ts` 加键，10 种区域都要有、键集完全对称（`pnpm i18n:check`）。菜单标题的键来自后端页面登记（如 `settings.configs.title`），枚举文案在前端按契约字面量写出。

## 四、样式

- 只用 XiHan.UI 组件与语义令牌；不新增裸 px（`pnpm px:check` 按文件计数，只许减不许增）；
- 圆角：control 4px、surface 8px、overlay 12px；按下 120ms 缩放到 0.97、释放 200ms；浮层只用 frosted，不用 glass；
- 管理端保持紧凑，不加营销式大标题。

## 五、门禁与测试

```bash
pnpm check   # type-check → lint → px → token → i18n → modules → contracts → permissions
pnpm test    # Vitest + jsdom
pnpm build
```

| 门禁 | 检查 |
| --- | --- |
| `type-check` | `vue-tsc --noEmit` |
| `lint` | oxlint + eslint（antfu） |
| `px:check` | 裸 px 按文件计数的白名单（`scripts/px-allowlist.json`，`--update` 收紧） |
| `token:check` | 引用的 `--xh-*` 令牌在当前组件库版本里存在 |
| `i18n:check` | 10 种区域键集对称；源码静态引用的 `t('…')` 键存在 |
| `modules:check` | 模块名与后端能力包一致；有 `setup.ts` 与 `locales/`；视图键全局唯一 |
| `contracts:check` | 重新生成契约，diff 必须为空 |
| `permissions:check` | `permission:`、`can('…')` 里的权限码都存在于生成的 `permissions.ts` |

视觉与交互改动还要在真实浏览器里看亮 / 暗色、窄视口、键盘与焦点、减少动态效果。

## 六、XiHan.UI 本地联调

目标是 XiHan.UI 的 `next` 线。需要联调未发布的组件库改动时，在 `pnpm-workspace.yaml` 里临时加 `overrides` 指向同级源码（`link:../../XiHan.UI/ui/packages/<目录>`），**overrides 不提交**。组件库的缺陷回 XiHan.UI 修，BasicApp 不写兼容。

## 下一步

- [菜单与路由](./routing) · [契约与请求](./request) · [Schema 驱动页面](./schema-page)
