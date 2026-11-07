# Schema 驱动页面

本页说明列表页引擎 SchemaPage 的页面定义、资源适配器与导入导出，适用于编写列表页的前端开发者。

`SchemaPage`（`~/components`）用一份字段定义（`ListFieldSchema[]`）同时派生搜索、列、导出与导入；列设置、多列排序、密度、个人视图、树表、行速览、批量删除与批量启停都已内置。资源适配器把它的查询参数转成后端的 `PageRequest`。

用户、角色、菜单、租户、套餐、部门、岗位、参数、字典、业务编号、文件、各日志页等列表页都用它。样板见 `src/modules/settings/views/configs`。

## 页面定义

```ts
const schema = computed<PageSchema>(() => ({
  pageCode: 'settings.configs', // 列设置、搜索设置、个人视图、导入历史按它存
  pageName: t('settings.configs.title'),
  rowKey: 'id', // 缺省 basicId，契约的行主键是 id
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'], // 资源适配器
  exportPermission: 'settings.configs.read',
  importPermission: 'settings.configs.create',
  batchRemovable: true,
  removePermission: 'settings.configs.delete',
  statusPermission: 'settings.configs.update',
  actions: [/* 页面级、行级与批量操作 */],
}))
```

`PageSchema` 不带泛型，回调里用 `asRow(row)` 转成本页的行类型。其他项：`pageSize`（缺省 20）、`tree`（`{ childrenKey?, defaultExpandAll? }`，存在即树表）。页面经模板 ref（`SchemaPageInstance`）调用 `reload()` 重新取数，`@action` 接收操作事件。

### 字段

```ts
{ key: 'name', title: t('…'), dataType: 'string', sortable: true, exportable: true, importable: true, required: true, minWidth: 160, order: 1 }
```

| 分组 | 项 |
| --- | --- |
| 基础 | `key`、`title`、`dataType`（`string` / `text` / `number` / `boolean` / `enum` / `date` / `datetime` / `money` / `percent` / `tag` / `json` / `image` / `file` / `avatar` / `email` / `phone` / `url`）、`order` |
| 列 | `visible`、`width` / `minWidth`、`fixed`、`ellipsis`、`render`、`formatter`（`maskPhone` / `maskEmail` / `maskIdCard`）、`treeColumn` |
| 搜索 | `searchable`（常用搜索）、`advancedSearch`（高级搜索）、`searchRange`（日期区间）、`searchMultiple`（多选）、`searchFilterable`（可输入筛选的单选下拉）、`searchPlaceholder` |
| 选项 | `options`、`optionsLoader`（异步加载，同一加载器只请求一次）、`dictCode`（系统字典） |
| 排序 | `sortable`（服务端排序，可多列） |
| 导入导出 | `exportable`、`importable`、`required`（导入必填） |
| 权限 | `permission`：没有该权限时字段隐藏 |

- 只放进搜索、不当列的字段写 `visible: false`；
- 可搜索、可排序的只放后端 `Queries/` 白名单里的字段，否则请求会被后端以 VALIDATION 拒绝；
- 状态列用 `render` + `renderTag(label, tone)`，不用 `dictionaryCode`（它走枚举元数据，后端没有对应接口）；
- `render` 用 `h()` 生成的节点不带页面 scope，样式写 `:deep()`。

### 操作

```ts
{ key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil',
  visible: row => editor.isOwn(asRow(row)), permission: 'settings.configs.update' }
```

- `scope: 'page' | 'row' | 'batch'`：工具栏、行操作菜单、勾选后的批量浮条；
- `tone`（组件库语气，如 `brand`、`danger`、`warning`）、`confirm` 与 `confirmText`（行级可传函数，写明操作对象）、`disabled`；
- `permission` 直接填操作的权限码，没有权限的按钮不显示；
- `visible` 处理业务上的可见性（例如租户看到平台参数只能「覆盖此参数」，不能编辑）。

## 资源适配器

```ts
import type { SchemaQueryParams, SchemaResource } from '~/components'
import { configs as configsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { throwOnFailed } from '~/request'

export function configsResource(): SchemaResource<ConfigRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await configsApi.list(toPageRequest(params, {
        filters: { group: 'eq', dataType: 'eq', owner: 'eq', status: 'eq', isEncrypted: 'eq' },
      })),
      params,
    ),
    remove: async id => throwOnFailed(await configsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => { /* 先取后改：update 是整体覆盖 */ },
    create: record => configsApi.create({ /* 导入：importable 字段组装的记录 → 新建请求 */ }),
  }
}
```

| 资源方法 | 用途 |
| --- | --- |
| `page` | 分页查询（列表模式） |
| `tree` | 树表一次取整棵（不分页） |
| `remove` | 删除单条；行删除与批量删除都调它 |
| `updateStatus(id, enabled)` | 启停单条；批量启停调它 |
| `create(record)` | 导入时逐行新建 |
| `export` | `{ businessType, buildQuery? }`：接入导出中心 |

`toPageRequest(params, mapping)`：

| 映射项 | 作用 |
| --- | --- |
| `keyword` | 落到 `PageRequest.keyword` 的搜索字段（缺省 `keyword`） |
| `filters` | 其余搜索字段的运算符：`{ 字段: 'eq' \| 'contains' \| 'ge' \| 'le' }` |
| `ranges` | `searchRange` 的字段：区间拆成后端的 `ge` 与 `le` 两条 |
| `multiples` | `searchMultiple` 的字段：选中值合成 `in`（逗号分隔） |
| `bounds` | 上下限的两个输入框落到同一个后端字段：`{ 搜索键: { field, operator: 'ge' \| 'le' } }`（如日志的最小、最大耗时） |

只接登记过的字段：出现没登记的搜索字段、区间或多选条件即抛错，不静默丢掉条件。区间与多选要后端同样接受。`toSchemaPageResult(result, params)` 把 `{ items, total, page }` 转成 SchemaPage 读的分页结果。

套餐、菜单这类后端不分页的列表，资源在前端切片或本地筛选整棵树；出现后端不支持的条件同样报错。

## 导出与导入

页面声明了 `exportPermission`（填读权限）且当前用户有这个权限，工具栏才出导出按钮。导出的列是标了 `exportable` 的字段，没有标的就导出当前可见列；单元格按字段类型与 `options` 写文本，不走列的 `render`，要导出的值必须在行的同名键上（规则项数这类算出来的列，由资源在分页时算进行里）。

- 只声明了 `exportPermission`：按当前筛选逐页取回，在浏览器里生成 CSV（至多 5000 行；树表导出当前整棵树）。
- 资源还实现了 `export`：导出菜单多出当前页、搜索结果、全部三项，向导出中心提交任务（`export-tasks.submit`，`businessType` 是列表操作 ID，如 `users.list`）。后台以当前用户的身份逐页调用同一个列表操作，生成 CSV 或 XLSX 文件，进度经 `TaskProgress` 推送到灵动岛，结果在 `/files/export-center` 下载。用户页与六种日志页（操作、访问、登录、异常、数据变更、开放接口）接了导出中心。

有 `importable` 字段且资源实现了 `create` 时出导入按钮，声明了 `importPermission` 的还要有该权限（导入用新建权限）。导入时下载按 `importable` 字段生成的 CSV 模板，上传 CSV 后前端解析、校验必填，再以并发 4 逐行调用 `create`，失败行可下载错误明细；每次导入记一条导入历史（`import-histories.create`），对话框里显示最近几次。

## 个人设置

列设置、搜索设置与个人视图按 `pageCode` 存在本地，并写入用户设置（`user-settings`，场景 `Page`）：开着同步时以后端那份为准，换设备打开页面即取到。

## 已知限制

- 树表的批量操作只认勾选的顶层行；
- 页面级设置的实时推送（`UserSettingChanged`，场景 `Page`）尚未接入，其他设备上的改动要重新打开页面才生效。

## 下一步

- [契约与请求](./request) · [常用组件](./components) · [权限与脱敏](./permission)
