# Schema 驱动页面

本页说明列表页引擎 SchemaPage 的页面定义、资源适配器与导入导出，适用于编写列表页的前端开发者。

`SchemaPage`（`~/components`）从 5.x 原样搬入：一份字段定义（`ListFieldSchema[]`）同时派生搜索、列、导出与导入；列设置、多列排序、密度、个人视图、树表、行速览与导出中心均已内置。6.0 的差别只在资源适配器：它把 SchemaPage 的查询参数转成后端的 `PageRequest`。

6.0 的十几个列表页（用户、角色、菜单、租户、套餐、部门、岗位、参数、字典、业务编号、文件、全部日志页……）都用它。

## 页面定义

```ts
const schema = computed<PageSchema>(() => ({
  pageCode: 'settings.configs',          // 个人视图、列设置、导入历史按它存
  pageName: t('settings.configs.title'),
  rowKey: 'id',
  fields: fields.value,
  resource,                              // 资源适配器
  exportPermission: 'settings.configs.read',
  importPermission: 'settings.configs.create',
  batchRemovable: true,
  removePermission: 'settings.configs.delete',
  statusPermission: 'settings.configs.update',
  actions: [ /* 页面级与行级操作 */ ],
}))
```

`PageSchema` 的其他项：`views`（预置视图）、`pageSize`、`tree` / `childrenKey` / `defaultExpandAll`（树表）。

### 字段

```ts
{ key: 'name', title: t('…'), dataType: 'string', sortable: true, exportable: true, importable: true, required: true, minWidth: 160, order: 1 }
```

| 分组 | 项 |
| --- | --- |
| 基础 | `key`、`title`、`dataType`（`string` / `text` / `number` / `boolean` / `enum` / `datetime` …）、`order` |
| 列 | `visible`、`width` / `minWidth`、`fixed`、`ellipsis`、`render`、`formatter`、`treeColumn` |
| 搜索 | `searchable`（常用搜索）、`advancedSearch`（高级搜索）、`searchRange`（区间）、`searchMultiple`（多选）、`searchPlaceholder` |
| 选项 | `options`、`optionsLoader`、`dictCode`（字典） |
| 排序筛选 | `sortable`、`filterable` |
| 导入导出 | `exportable`、`importable`、`required`、`defaultValue` |
| 权限 | `permission`、`readonly`、`editable` |

只放进搜索、不当列的字段写 `visible: false`；可排序与可筛选的只放后端白名单里的字段，否则请求会被后端以 VALIDATION 拒绝。

### 操作

```ts
{ key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil',
  visible: row => editor.isOwn(asRow(row)), permission: 'settings.configs.update' }
```

- `scope: 'page' | 'row'`，`tone`（`brand` / `danger` / `warning`）、`confirm` 与 `confirmText`；
- `permission` 直接填操作的权限码（6.0 没有按钮码）；没有权限的按钮不显示；
- `visible` 处理业务上的可见性（例如租户看到平台参数只能「覆盖此参数」，不能编辑）。

## 资源适配器

```ts
import { configs } from '~/contracts'
import { toPageRequest, toSchemaPageResult } from '~/contracts'

export function configsResource(): SchemaResource<ConfigRow> {
  return {
    page: async params => toSchemaPageResult(
      await configs.list(toPageRequest(params, {
        filters: { group: 'eq', dataType: 'eq', status: 'eq', isEncrypted: 'eq' },
      })),
      params,
    ),
    remove: async id => failedCodes(await configs.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => { /* 先取后改：update 是整体覆盖 */ },
    create: record => configs.create({ /* 导入：importable 字段组装的记录 → 新建请求 */ }),
  }
}
```

`toPageRequest(params, mapping)`：

| 映射项 | 作用 |
| --- | --- |
| `keyword` | 落到 `PageRequest.keyword` 的搜索字段（缺省 `keyword`） |
| `filters` | 其余搜索字段的运算符：`{ 字段: 'eq' \| 'contains' \| … }` |
| 区间 | `searchRange` 的字段拆成后端的 `ge` 与 `le` 两条 |
| 多选 | `searchMultiple` 的选中值合成 `in`（逗号分隔） |

只接登记过的字段：出现没登记的区间或多选条件即抛错，而不是静默丢掉条件。资源还可以实现 `tree`（树表一次取整棵）、`export`、`buildQuery`。

套餐、菜单这类后端不分页的列表，资源在前端切片或本地筛选整棵树；出现后端不支持的条件同样报错。

## 导出与导入

导出时，页面的导出菜单（当前页、搜索结果、全部）向后端导出中心提交任务（`export-tasks.submit`）。后台以当前用户的身份逐页调用同一个列表操作，生成 CSV 或 XLSX 文件，进度经 `TaskProgress` 推送到灵动岛，结果在 `/files/export-center` 下载。

导入时，前端解析 CSV 或 XLSX 文件，按 `importable` 字段组装记录，逐行调用资源的 `create`，并记录导入历史（`import-histories.create`）。

## 个人设置

列设置、搜索设置、个人视图都存在用户设置里（`user-settings`），经 `UserSettingChanged` 在同一用户的其他设备上实时应用。

## 已知限制

- 树表的批量操作只认勾选的顶层行（与 5.x 相同）；
- 页面宽度超过 600 行的单文件拆分规则尚未加 lint，用户页、字典页偏大。

## 下一步

- [契约与请求](./request) · [常用组件](./components) · [权限与脱敏](./permission)
