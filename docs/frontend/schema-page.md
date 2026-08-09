# Schema 驱动页面

BasicApp 的列表页几乎都不手写表格。**一份字段声明（`ListFieldSchema[]`）同时派生出搜索表单、表格列、导出字段、导入模板与详情展示**，整页交给 `<SchemaPage>` 渲染。本页是写这类页面的完整手册。

组件源码在 `packages/components/schema/`。

## 心智模型

```text
ListFieldSchema[]  ← 字段单一事实源（禁止重复定义）
      │
      ├──► 搜索表单（searchable / advancedSearch / searchRange / searchMultiple）
      ├──► 表格列（visible / width / fixed / render / sortable）
      ├──► 导出字段（exportable）
      ├──► 导入模板（importable）
      └──► 表单编辑（editable）

PageSchema         ← 整页事实源（字段 + 资源适配器 + 操作 + 权限码）
      │
      └──► <SchemaPage :schema @action>
```

**页面代码只做三件事**：声明字段、把归一化查询参数映射成后端 DTO、处理操作事件。表格、分页、列设置、偏好同步这些都不用管。

## 内置能力

`SchemaPage` 开箱提供：

- **搜索**：常用搜索 + 高级搜索浮层；时间区间（→ `Between`）、枚举/标签多选（→ `In`）
- **表格**：列设置（显隐/顺序/固定/列宽）、密度切换、斑马纹/边框风格、多选、序号列、行悬停速览、树形模式
- **列宽拖拽**：拖表头右边框调宽，值写入列设置可保存
- **多字段排序**：点多个列头累加，数组顺序即优先级
- **导出**：本地 CSV，或提交到[导出中心](../backend/file#导出中心-file-export-center)异步导出
- **导入**：模板下载、CSV 解析、预校验、批量创建对话框
- **批量操作**：批量删除、批量启停、页面自定义批量动作
- **个人视图/搜索方案**：保存当前筛选 + 排序为命名方案（经组件实例方法 `saveView` / `applyView` 暴露，无内置 UI 入口）
- **偏好云端同步**：列设置与搜索设置按 `pageCode` 同步后端，多端一致

## 字段 Schema：`ListFieldSchema`

完整定义以 `~/components/schema/types.ts` 为准。

| 属性 | 作用 |
| --- | --- |
| `key` | 字段键，对应行数据的属性名 |
| `title` | 列标题（**建议传 i18n key 的翻译结果**） |
| `dataType` | `string` / `enum` / `datetime` / `money` / `tag` / `json` / `image` 等，决定默认渲染器与搜索控件 |
| `visible` | 是否作为表格列。**`false` = 仅搜索用，不出现在表格** |
| `searchable` / `advancedSearch` | 进入常用搜索 / 高级搜索浮层 |
| `searchRange` | 时间字段：渲染区间选择器 + 便捷预设，下发 `Between` |
| `searchMultiple` | 枚举/标签字段：渲染多选下拉，下发 `In` |
| `sortable` | 服务端排序（列头出现排序箭头） |
| `exportable` / `importable` / `editable` | 参与导出 / 导入模板 / 表单编辑 |
| **`permission`** | **字段级权限码**：无此权限时该列与该搜索项**整个不渲染** |
| `dictionaryCode` | 枚举名或字典码，运行时异步拉取选项注入 `options` |
| `options` | 静态选项（`dictionaryCode` 解析为空时兜底，**保证绝不出现空下拉**） |
| `render` | 自定义单元格渲染（最高优先级，返回 `VNodeChild`） |
| `treeColumn` | 树形模式下承载展开箭头的列（应有且仅有一个） |
| `order` | 排序值，越小越靠前 |
| `width` / `minWidth` / `fixed` | 列宽 / 最小列宽 / 固定方向 |

## 页面 Schema：`PageSchema`

| 属性 | 作用 |
| --- | --- |
| `pageCode` | 页面唯一码，**偏好与视图按此维度存储**（如 `log.access`） |
| `pageName` | 页面名 |
| `resourceCode` | 后端资源码，用于匹配字段脱敏（FLS）规则；**缺省则不拉取脱敏规则** |
| `resource` | 数据资源适配器：`page` / `tree` / `remove` / `updateStatus` / `create` / `export` |
| `fields` | 字段单一事实源 |
| `actions` | 操作集合（`page` 工具栏 / `row` 行菜单 / `batch` 批量浮条） |
| `rowKey` | 行主键，默认 `basicId` |
| `exportPermission` | 导出按钮权限码。**精准门控：声明后仅有权用户可见；未声明则该页不显示导出** |
| `importPermission` / `removePermission` / `statusPermission` | 导入 / 批量删除 / 批量启停的权限码 |
| `batchRemovable` | 启用内置批量删除（依赖 `resource.remove`） |
| `tree` | 存在即启用树形模式（走 `resource.tree`、不分页、按 `childrenKey` 展开） |
| `scrollX` / `pageSize` | 表格横向滚动宽度 / 默认每页数量 |

## 完整骨架

以「访问日志」页（`src/views/log/access/index.vue`）为规范范例：

```vue
<script setup lang="ts">
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { createPageRequest, logManagementApi, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'

const { t } = useI18n()

// ① 字段单一事实源（列 + 常用搜索 + 高级搜索，全在这里声明一次）
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索、不作为列
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string',
    visible: false, searchable: true, order: 0 },

  // 枚举多选搜索 + 自定义标签渲染
  {
    key: 'accessResult', title: t('log.access.access_result'), dataType: 'enum',
    searchable: true, searchMultiple: true, sortable: true,
    options: accessResultOptions.value, order: 18,
    render: row => h(NTag, { type: accessResultType(row.accessResult) }, () => /* label */),
  },

  // 时间区间搜索
  { key: 'accessTime', title: t('log.access.access_time'), dataType: 'datetime',
    sortable: true, searchable: true, searchRange: true, advancedSearch: true, order: 26 },
])

// ② 查询构建：归一化参数 → 后端分页 DTO（resource.page 与导出复用同一个函数）
function buildAccessQuery(params: SchemaQueryParams) {
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: {
        sorts: querySortsFromSchema(params.sorts),      // 多字段排序
        filters: params.conditionFilters ?? [],          // 区间/多选（框架已算好）
      },
    }),
    keyword: (params.filters.keyword as string)?.trim() || undefined,
    // …其余顶层查询字段
  }
}

// ③ 页面 Schema
const schema = computed<PageSchema>(() => ({
  pageCode: 'log.access',
  pageName: t('log.access.page_name'),
  exportPermission: 'saas:access-log:export',   // 权限码常量见后端 SaasPermissionCodes（登记在 SaasPermissionDefinitions）
  rowKey: 'basicId',
  scrollX: 2200,
  fields: fields.value,
  resource: {
    page: params => logManagementApi.access.page(buildAccessQuery(params)),
    export: { businessType: 'log.access', buildQuery: buildAccessQuery },
  },
  actions: [
    { key: 'view', title: t('common.actions.view_detail'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

// 操作事件统一上抛，页面自己处理业务
function onAction(payload: SchemaActionPayload) {
  if (payload.key === 'view' && payload.row) { /* 打开详情抽屉 */ }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <!-- 默认插槽：承载页面自有弹窗/抽屉 -->
    <LogDetailDrawer v-model:show="detailVisible" :record="detailData" />
  </SchemaPage>
</template>
```

三条要点：

1. **操作不在 Schema 里实现逻辑**，只声明 `key` / `scope` / `icon` / `permission`；点击时经 `@action` 上抛 `{ key, scope, row?, rows? }`，由页面处理。
2. `resource.page` 收到框架**归一化**的 `SchemaQueryParams`（`page` / `pageSize` / `sorts` / `filters` / `conditionFilters`），映射成后端 DTO 的适配器**是页面自己写的**——框架只依赖归一化契约，不关心你的 DTO 长什么样。
3. **导出复用同一个 `buildQuery`**，保证导出内容与当前筛选一致。

### 插槽

| 插槽 | 用途 |
| --- | --- |
| 默认 | 页面自有弹窗/抽屉 |
| `#toolbar` | 追加工具栏项 |
| `#expand` | 行展开内容（如任务调度展开触发器信息） |

## 多字段排序

排序**前端驱动、后端应用**：

- 列头点击累加为 `sorts: SchemaSortRule[]`，**数组顺序即优先级**（下标 0 为主排序）。
- `querySortsFromSchema(params.sorts)` 转成后端 `conditions.sorts`，每条带 `priority = 下标`。
- 后端 `ApplySorts` 应用排序，并受 **FLS 门控**：只有「可读且未脱敏」的字段才允许作为排序键，其余被静默剔除；剔完没有有效排序时回退该接口的默认排序。
- 列设置里可为每列设默认排序（单图标循环 无→升→降），优先级按列顺序。树表/子表不参与。

::: warning 点了排序没反应
先查该字段的字段级权限与脱敏规则——被 FLS 剔掉时后端不会报错，只是排序不生效。见 [权限与脱敏](./permission)。
:::

## 搜索：区间与多选

`searchRange` / `searchMultiple` 字段的当前值由框架的 `queryFiltersFromSchema` 派生成后端通用过滤 `conditions.filters`：

| 声明 | 下发的操作符 | 细节 |
| --- | --- | --- |
| `searchRange` | `Between`（`4000`） | 值为 ISO 字符串；`date` 粒度末值补到当天 `23:59:59.999` 以含整日 |
| `searchMultiple` | `In`（`3000`） | 值走 `values` 数组 |

这些统一放进 `params.conditionFilters`，页面适配器并入 `conditions.filters` 即可。其余普通搜索字段仍由页面的 `buildXxxQuery` 映射为 DTO 顶层字段。

相关封装组件：`SchemaSearchField` / `SchemaSearchDateRange` / `SchemaSearchMultiSelect`。

## 树形模式

`PageSchema.tree` 存在即启用：走 `resource.tree`、**不分页**、按 `childrenKey` 展开。字段里要指定**有且仅有一个** `treeColumn`（承载展开箭头的列）。

组织机构、菜单管理这类天然层级的页面用它。

## 导入导出

### 导出

```ts
resource: {
  export: { businessType: 'log.access', buildQuery: buildAccessQuery },
}
```

声明后走[导出中心](../backend/file#导出中心-file-export-center)的异步链路（提交任务 → 队列 → 后台生成 → 下载）；未声明则退化为本地 CSV。

**按钮显隐由 `exportPermission` 精准门控**——未声明该字段的页面不显示导出按钮。

### 导入

`SchemaImportDialog` 内置：模板下载 → 选文件 → CSV 解析 → 预校验 → 批量创建。模板字段由 `importable: true` 的字段派生，**不需要单独维护模板文件**。

## 偏好与视图

| 能力 | 说明 |
| --- | --- |
| **列设置 / 搜索设置** | 按 `pageCode` 同步后端（`useUserSettingSync`），多端一致 |
| **个人视图 / 搜索方案** | 保存当前筛选 + 排序 + 每页数量为命名方案（`useViewManager`）；无内置 UI，经模板 ref 调用 `saveView` / `applyView` |

同步策略：**localStorage 仍是事实源**，后端加载成功则覆盖本地，保存失败静默忽略（尽力而为）。其它设备保存后经 SignalR `UserSettingChanged` 实时推送并应用到已打开的页面。

::: tip `pageCode` 要稳定
偏好、视图、列设置全按 `pageCode` 存储。**改了 `pageCode` 等于用户的所有个性化配置丢失**，页面上线后不要再动它。
:::

## 加一个列表页的清单

| 步骤 | 做什么 |
| --- | --- |
| 1 | 后端 `PageRegistry` 登记页面与按钮（**建菜单即绑权限**） |
| 2 | 后端加分页查询方法，**记得标 `[HttpPost]`** |
| 3 | 前端 `src/api/modules/{域}/xxx.ts` 加 API（标准 CRUD 直接用 `defineResource`） |
| 4 | 前端 `src/views/{域}/{页}/index.vue` 按上面的骨架写 |
| 5 | `packages/locales/langs/{zh-CN,en-US}/` 补文案 |

后端侧的完整接线见 [二次开发](../backend/development)。

## 相关页面

- [权限与脱敏](./permission)：字段级权限与 FLS 如何影响渲染
- [路由与菜单](./routing)：页面怎么被路由到
- [主题与国际化](./theme)：枚举标签、文案、时区
- [接口对接指南](../api-guide#分页与查询协议)：`conditions` / `page` 的完整协议
