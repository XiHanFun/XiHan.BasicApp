<script setup lang="ts">
import type { MenuNode } from '@xihan-ui/headless'
import type { Tone } from '@xihan-ui/kernel'
import type { ActionSchema, ListFieldSchema, PageSchema, SchemaActionPayload, SchemaColumn } from './types'
import type { ApiId } from '~/types/contracts'
import { XhButton, XhCardBody, XhCardRoot, XhMenuRoot, XhSkeletonBone, XhSkeletonRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useSlots, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialog, toast } from '~/composables'
import { islandStart } from '~/composables/useDynamicIsland'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAppStore } from '~/stores'
import XIconButton from '../common/XIconButton.vue'
import SchemaActionPanel from './SchemaActionPanel.vue'
import SchemaImportDialog from './SchemaImportDialog.vue'
import SchemaSearchPanel from './SchemaSearchPanel.vue'
import SchemaSearchSettings from './SchemaSearchSettings.vue'
import SchemaTablePanel from './SchemaTablePanel.vue'
import SchemaTableSettings from './SchemaTableSettings.vue'
import { queryFiltersFromSchema, toColumns, toExportFields, toImportFields } from './selectors'
import { useSchemaDictionaries } from './useSchemaDictionaries'
import { useSchemaExport } from './useSchemaExport'
import { useSchemaTable } from './useSchemaTable'
import { useSearchSettings } from './useSearchSettings'
import { useTableSettings } from './useTableSettings'
import { useViewManager } from './useViewManager'

defineOptions({ name: 'SchemaPage' })

const props = defineProps<{
  /** 页面单一事实源 */
  schema: PageSchema<Row>
}>()

const emit = defineEmits<{
  /** 操作事件（页面级/行级/批量级统一上抛，由页面处理具体逻辑） */
  action: [payload: SchemaActionPayload<Row>]
}>()

// 行类型在框架边界放宽：页面侧以 PageSchema<ConcreteDto> 定义时保有完整类型安全；
// 此处用宽松行类型规避 Vue 泛型组件 prop 协变限制（具名 DTO 无索引签名，不兼容 Record<string, unknown>）。
// eslint-disable-next-line ts/no-explicit-any
type Row = Record<string, any>

const { t } = useI18n()
const { hasPermission } = usePermission()
const appStore = useAppStore()

const firstLoaded = ref(false)
const checkedKeys = ref<Array<string | number>>([])

const table = useSchemaTable<Row>(props.schema)
const { loading, rows, total, page, pageSize, filters, sorts, search, reset, changePage, changePageSize, changeSort, remove } = table

/**
 * 字典/枚举异步取值：按字段 dictionaryCode 拉取元数据并注入 field.options，
 * 使单元格按值映射 label、搜索区自动渲染为下拉。静态 options 优先。
 */
const dictionaries = useSchemaDictionaries(() => props.schema.fields)
const resolvedFields = computed<ListFieldSchema[]>(() =>
  props.schema.fields.map((field) => {
    // 字典/枚举选项注入（字段脱敏已由服务端在响应里落地，前端不再二次打码）
    // dictionaryCode 解析结果优先（本地化选项）；为空时回退字段静态 options 兜底
    if (!field.dictionaryCode) {
      return field
    }
    const options = dictionaries.optionsMap.value[field.dictionaryCode]
    return options?.length ? { ...field, options } : field
  }),
)
const resolvedSchema = computed<PageSchema<Row>>(() => ({ ...props.schema, fields: resolvedFields.value }))

/** 搜索字段池：所有 searchable 或 advancedSearch 且有权限的字段（保持 schema order） */
const searchPool = computed<ListFieldSchema[]>(() =>
  [...resolvedFields.value]
    .filter(f => (f.searchable || f.advancedSearch) && (!f.permission || hasPermission(f.permission)))
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
)

/** 搜索设置（固定/排序，按 pageCode 持久化）→ 派生常用/高级字段 */
const searchSettings = useSearchSettings(props.schema.pageCode, searchPool)
const searchFields = searchSettings.commonFields
const advancedFields = searchSettings.advancedFields

/** 表格可选列字段（可见 + 有权限），作为列设置来源 */
const columnFields = computed<ListFieldSchema[]>(() =>
  resolvedFields.value.filter(f => f.visible !== false && (!f.permission || hasPermission(f.permission))),
)

/**
 * 悬停速览字段：全部可读字段（含表格隐藏列——速览的增量价值），脱敏已由服务端落地。
 * 受偏好「表格悬停速览」开关控制：关闭时返回空列表，下游据此停用悬停行为。
 */
const peekFields = computed<ListFieldSchema[]>(() =>
  appStore.tableRowPeek
    ? resolvedFields.value.filter(f => !f.permission || hasPermission(f.permission))
    : [],
)

/** 列设置（显隐/顺序/固定/密度/风格/多选/序号/列宽/每列默认排序，按 pageCode 持久化）。多选默认打开 */
const settings = useTableSettings(props.schema.pageCode, columnFields, { defaultSelectable: true })

// 初始默认排序：来自列设置本地持久化（restore 同步执行）；用户列头排序/方案应用会覆盖本会话
if (settings.defaultSorts.value.length) {
  sorts.value = [...settings.defaultSorts.value]
}

// 远端 hydrate 带来默认排序：仅当首屏已加载且用户尚未手动排序时应用并刷新（避免覆盖用户当前排序）
watch(() => settings.defaultSorts.value, (ds) => {
  if (ds.length && firstLoaded.value && !sorts.value.length) {
    changeSort([...ds])
  }
})

/** 列内排序图标循环（无→升→降）：更新列设置并立即应用到当前表格（优先级=列顺序，重新取数） */
function onCycleSort(key: string) {
  settings.cycleSort(key)
  changeSort([...settings.defaultSorts.value])
}

/**
 * 表格重挂载令牌：拖拽列宽会写入 Naive 内部缓存（覆盖 column.width），
 * 当通过「列宽输入框 / 恢复默认」改宽度时需重建表格清掉该缓存，新值才生效。
 */
const tableRemountKey = ref(0)
/** 本会话被拖拽过的列（其宽度由 Naive 内部缓存接管，需重挂载才能被输入框覆盖） */
const draggedColumnKeys = new Set<string>()

function remountTable() {
  tableRemountKey.value += 1
  draggedColumnKeys.clear()
}

/** 拖拽列宽 → 即时写入列设置（待「保存」落库）；记录该列已被拖拽 */
function onColumnResize(key: string, width: number) {
  draggedColumnKeys.add(key)
  settings.setWidth(key, width)
}

/** 输入框改列宽 → 写入列设置；若该列曾被拖拽，重建表格以让新值覆盖 Naive 缓存 */
function onColumnWidthInput(key: string, width: number | undefined) {
  settings.setWidth(key, width)
  if (draggedColumnKeys.has(key)) {
    remountTable()
  }
}

/** 恢复默认：重置设置并重建表格，清掉 Naive 的列宽拖拽缓存 */
function onResetTableSettings() {
  settings.resetDefault()
  remountTable()
}

/** 表格设置：调整即时生效，点击「保存」才落库（本地 + 后端）；同步反馈由灵动岛统一展示 */
function onSaveTableSettings() {
  settings.save()
}

/** 搜索设置：调整即时生效，点击「保存」才落库（本地 + 后端）；同步反馈由灵动岛统一展示 */
function onSaveSearchSettings() {
  searchSettings.save()
}

/** 局部全屏 */
const isFullscreen = ref(false)
function toggleFullscreen() {
  isFullscreen.value = !isFullscreen.value
}

/**
 * 搜索方案（个人视图）—— 作为接口暴露，不内置 UI。
 * 当前持久化到本地（localStorage，按 pageCode）；后续可替换为后端按用户保存。
 * 页面可通过模板 ref 调用 saveView / applyView / views 自定义方案入口。
 */
const viewManager = useViewManager(props.schema.pageCode)

/** 保存当前列表状态为命名方案 */
function saveView(name: string) {
  viewManager.addView(name, {
    filters: { ...filters },
    sorts: [...sorts.value],
    pageSize: pageSize.value,
  })
}

/** 应用方案：落地快照到表格状态并刷新 */
function applyView(code: string) {
  const snapshot = viewManager.applyView(code)
  if (!snapshot) {
    return
  }
  // 置 null 而非 delete：保持搜索控件受控，避免 Naive 回退内部值而残留旧选择（同 reset）
  for (const key of Object.keys(filters)) {
    filters[key] = null
  }
  Object.assign(filters, snapshot.filters)
  sorts.value = snapshot.sorts ? [...snapshot.sorts] : []
  if (snapshot.pageSize) {
    pageSize.value = snapshot.pageSize
  }
  search()
}

/** 行级操作（有权限） */
const rowActions = computed(() =>
  (props.schema.actions ?? []).filter(a => a.scope === 'row' && (!a.permission || hasPermission(a.permission))),
)

/** 批量操作（有权限） */
const batchActions = computed(() =>
  (props.schema.actions ?? []).filter(a => a.scope === 'batch' && (!a.permission || hasPermission(a.permission))),
)

/** 选中的行对象 */
const selectedRows = computed(() => {
  const rowKey = props.schema.rowKey ?? 'basicId'
  const keySet = new Set(checkedKeys.value)
  return rows.value.filter(row => keySet.has((row as Record<string, unknown>)[rowKey] as string | number))
})

/** 列：schema 派生列（应用列设置：显隐/顺序/固定）+ 行操作列 */
const columns = computed<SchemaColumn<Row>[]>(() => {
  const base = toColumns(resolvedSchema.value, hasPermission, {
    visibleKeys: settings.visibleKeys.value,
    columnOrder: settings.columnOrder.value,
    fixedMap: settings.fixedMap.value,
    widthMap: settings.widthMap.value,
  })
  if (rowActions.value.length === 0) {
    return base
  }
  const actionColumn: SchemaColumn<Row> = {
    key: '__actions__',
    title: t('component.schema_page.actions_column'),
    width: 90,
    fixed: 'right',
    render: (row: Row) => renderRowActions(row),
  }
  return [...base, actionColumn]
})

/** 行展开：页面提供 #expand 作用域插槽时启用（行前出现展开箭头，展开渲染该插槽内容） */
const slots = useSlots()
const renderExpand = computed(() => (slots.expand ? (row: Row) => slots.expand!({ row }) : undefined))

function visibleRowActions(row: Row): ActionSchema<Row>[] {
  return rowActions.value.filter(a => !a.visible || a.visible(row))
}

/** 操作 Schema 的 type 到组件库 tone 轴的换算（Schema 里的词汇沿用页面既有声明，不改） */
function toneOfActionType(type: ActionSchema<Row>['type']): Tone {
  switch (type) {
    case 'primary':
      return 'brand'
    case 'error':
      return 'danger'
    case 'info':
    case 'success':
    case 'warning':
      return type
    default:
      return 'neutral'
  }
}

function renderRowActions(row: Row) {
  const collection: MenuNode[] = visibleRowActions(row).map(a => ({
    value: a.key,
    label: a.title,
    disabled: a.disabled ? a.disabled(row) : false,
  }))
  if (collection.length === 0) {
    return h('span', { class: 'text-foreground/30' }, '-')
  }
  return h(
    XhMenuRoot,
    {
      collection,
      // 借用下面那颗按钮当触发器。不借的话菜单根会自己包一颗裸 button，
      // 而 menu 皮肤没有 trigger 这一部件，露出的是浏览器默认按钮样式
      triggerAsChild: true,
      onSelect: (details: { value: string }) => dispatchAction(details.value, { key: details.value, scope: 'row', row }),
    },
    {
      trigger: () => h(
        XhButton,
        { variant: 'outline', size: 'sm' },
        () => [t('component.schema_page.more'), h(Icon, { icon: 'lucide:chevron-down' })],
      ),
    },
  )
}

/**
 * 派发操作：声明了 confirm 的先弹二次确认，确认后才上抛给页面。
 * 确认在此统一处理，页面侧只管收到事件后执行，不必各自写 dialog。
 */
function dispatchAction(key: string, payload: SchemaActionPayload<Row>) {
  const action = (props.schema.actions ?? []).find(item => item.key === key && item.scope === payload.scope)
  if (!action?.confirm) {
    emit('action', payload)
    return
  }

  void dialog.confirm({
    title: action.title,
    content: action.confirmText ?? t('component.schema_page.action_confirm'),
    badge: 'warning',
    okText: t('component.schema_page.confirm'),
    cancelText: t('component.schema_page.cancel'),
    onOk: () => {
      emit('action', payload)
    },
  })
}

function onPageAction(key: string) {
  dispatchAction(key, { key, scope: 'page' })
}

function onBatchAction(key: string) {
  dispatchAction(key, { key, scope: 'batch', rows: selectedRows.value })
}

function clearSelection() {
  checkedKeys.value = []
}

/** 内置批量删除：依赖 resource.remove + schema.batchRemovable */
const canBatchRemove = computed(() => !!props.schema.batchRemovable && !!props.schema.resource.remove && (!props.schema.removePermission || hasPermission(props.schema.removePermission)))
const batchRemoving = ref(false)

/** 批量启停：依赖 resource.updateStatus，按 statusPermission 门控 */
const canBatchStatus = computed(() => !!props.schema.resource.updateStatus && (!props.schema.statusPermission || hasPermission(props.schema.statusPermission)))
const batchStatusUpdating = ref(false)

function handleBatchRemove() {
  const targets = selectedRows.value
  const removeFn = props.schema.resource.remove
  if (targets.length === 0 || !removeFn) {
    return
  }
  const rowKey = props.schema.rowKey ?? 'basicId'
  void dialog.confirm({
    title: t('component.schema_page.batch_delete_title'),
    content: t('component.schema_page.batch_delete_content', { count: targets.length }),
    badge: 'warning',
    tone: 'danger',
    okText: t('component.schema_page.batch_delete_confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      batchRemoving.value = true
      try {
        const results = await Promise.allSettled(
          targets.map(row => removeFn((row as Record<string, unknown>)[rowKey] as ApiId)),
        )
        const failed = results.filter(r => r.status === 'rejected').length
        if (failed === 0) {
          toast.success(t('component.schema_page.deleted_count', { count: targets.length }))
        }
        else {
          toast.warning(t('component.schema_page.delete_partial', { success: targets.length - failed, failed }))
        }
        clearSelection()
        await table.load()
      }
      finally {
        batchRemoving.value = false
      }
    },
  })
}

/** 批量启停：对选中行逐个调用 resource.updateStatus(id, enabled)，并发执行后汇总 */
function handleBatchStatus(enabled: boolean) {
  const targets = selectedRows.value
  const updateFn = props.schema.resource.updateStatus
  if (targets.length === 0 || !updateFn) {
    return
  }
  const rowKey = props.schema.rowKey ?? 'basicId'
  const label = enabled ? t('component.schema_page.label_enable') : t('component.schema_page.label_disable')
  void dialog.confirm({
    title: t('component.schema_page.batch_action_title', { label }),
    content: t('component.schema_page.batch_action_content', { label, count: targets.length }),
    badge: 'warning',
    okText: t('component.schema_page.batch_status_confirm', { label }),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      batchStatusUpdating.value = true
      try {
        const results = await Promise.allSettled(
          targets.map(row => updateFn((row as Record<string, unknown>)[rowKey] as ApiId, enabled)),
        )
        const failed = results.filter(r => r.status === 'rejected').length
        if (failed === 0) {
          toast.success(t('component.schema_page.status_done_count', { label, count: targets.length }))
        }
        else {
          toast.warning(t('component.schema_page.status_partial', { label, success: targets.length - failed, failed }))
        }
        clearSelection()
        await table.load()
      }
      finally {
        batchStatusUpdating.value = false
      }
    },
  })
}

function reload() {
  return table.load()
}

/** 导出字段（exportable + 权限） */
const exportFields = computed(() => toExportFields(resolvedSchema.value, hasPermission))

/** 有效导出列：页面声明了 exportable 则用之，否则回退为当前可见列（"导出所见"） */
const effectiveExportFields = computed(() => exportFields.value.length ? exportFields.value : columnFields.value)

/** 取导出行：列表模式翻页拉全集（受安全上限约束）；树形模式展平当前树 */
async function fetchExportRows(): Promise<Row[]> {
  const childrenKey = props.schema.tree?.childrenKey ?? 'children'
  if (table.isTree) {
    const flat: Row[] = []
    const walk = (nodes: Row[]) => {
      for (const node of nodes) {
        flat.push(node)
        const children = (node as Record<string, unknown>)[childrenKey] as Row[] | undefined
        if (children?.length) {
          walk(children)
        }
      }
    }
    walk(rows.value)
    return flat
  }
  const pageFn = props.schema.resource.page
  if (!pageFn) {
    return [...rows.value]
  }
  const size = pageSize.value
  const cap = 5000
  const target = Math.min(total.value || cap, cap)
  const collected: Row[] = []
  let current = 1
  while (collected.length < target) {
    const result = await pageFn({
      page: current,
      pageSize: size,
      sorts: [...sorts.value],
      filters: { ...filters },
      // 导出复用列表的区间/多选过滤，保证导出范围与界面筛选一致
      conditionFilters: queryFiltersFromSchema(props.schema.fields, filters),
    })
    const items = result.items ?? []
    if (items.length === 0) {
      break
    }
    collected.push(...items)
    if (items.length < size) {
      break
    }
    current += 1
  }
  return collected
}

const { exporting, exportCsv } = useSchemaExport<Row>({
  fields: () => effectiveExportFields.value,
  fileName: () => props.schema.pageCode,
  fetchRows: fetchExportRows,
})

// ── 导出中心：提交异步导出（resource.export 存在时启用「提交到导出中心」入口） ──
const appContext = useAppContext()
const canSubmitExport = computed(() => !!props.schema.resource.export)
const submittingExport = ref(false)

const exportMenuOptions = computed<MenuNode[]>(() => [
  { value: 'center:1', label: t('component.schema_page.export_results') },
  { value: 'center:0', label: t('component.schema_page.export_current_page') },
  { value: 'center:2', label: t('component.schema_page.export_all') },
  // 本地 CSV 与三档「提交到导出中心」不是一类事，条前画一条分隔线
  { value: 'local', label: t('component.schema_page.export_csv_local'), separatorBefore: true },
])

/** 导出列定义：键/标题 + 枚举/字典 valueMap（原始值 → label，供服务端渲染） */
function buildExportColumns() {
  return effectiveExportFields.value.map((field) => {
    const column: { key: string, title: string, valueMap?: Record<string, string> } = { key: field.key, title: field.title }
    if (field.options?.length) {
      column.valueMap = Object.fromEntries(field.options.map(option => [String(option.value), option.label]))
    }
    return column
  })
}

/** 提交导出任务到导出中心（scope：0 当前页 / 1 查询结果 / 2 全部） */
async function submitExport(scope: number) {
  const cfg = props.schema.resource.export
  if (!cfg) {
    return
  }
  const params = {
    page: page.value,
    pageSize: pageSize.value,
    sorts: [...sorts.value],
    filters: { ...filters },
    // 导出复用列表的区间/多选过滤，保证导出范围与界面筛选一致
    conditionFilters: queryFiltersFromSchema(props.schema.fields, filters),
  }
  const query = cfg.buildQuery ? cfg.buildQuery(params) : params
  submittingExport.value = true
  const task = islandStart('export:submit', t('island.export.submitting'), { icon: 'lucide:download', progress: 0 })
  try {
    await appContext.apis.exportTaskApi.submit({
      businessType: cfg.businessType,
      scope,
      format: 0,
      querySnapshot: JSON.stringify(query),
      columns: buildExportColumns(),
    })
    task.success(t('island.export.submitted'), { detail: t('island.export.submitted_detail') })
  }
  catch (error) {
    task.error(t('island.export.submit_failed'), { detail: (error as Error).message })
  }
  finally {
    submittingExport.value = false
  }
}

function onExportSelect(key: string) {
  if (key === 'local') {
    void exportCsv()
    return
  }
  if (key.startsWith('center:')) {
    void submitExport(Number(key.slice('center:'.length)))
  }
}

/** 导出按钮权限门控（严格）：仅在页面声明了 exportPermission 且当前用户拥有该权限时才显示导出 */
const canExportPermitted = computed(() => !!props.schema.exportPermission && hasPermission(props.schema.exportPermission))
/** 导入按钮权限门控：声明了 importPermission 则需有权限才显示（导入仅在 resource.create + importable 时存在） */
const canImportPermitted = computed(() => !props.schema.importPermission || hasPermission(props.schema.importPermission))

/** 导入：字段含 importable 且 resource.create 存在 + 导入权限通过时，工具栏出现内置导入按钮 */
const importFields = computed(() => toImportFields(resolvedSchema.value, hasPermission))
const canImport = computed(() => importFields.value.length > 0 && !!props.schema.resource.create && canImportPermitted.value)
const importVisible = ref(false)

/** 导入完毕：有成功行则刷新列表 */
function onImportFinished(summary: { total: number, success: number, failed: number }) {
  if (summary.success > 0) {
    void table.load()
  }
}

onMounted(async () => {
  void dictionaries.resolve()
  await table.load()
  firstLoaded.value = true
})

defineExpose({
  reload,
  remove,
  clearSelection,
  filters,
  // 搜索方案接口（无内置 UI，供页面自定义方案入口调用）
  views: viewManager.views,
  activeViewCode: viewManager.activeCode,
  saveView,
  applyView,
  removeView: viewManager.removeView,
  setDefaultView: viewManager.setDefault,
})

// 列表骨架屏：列宽/列数/行高全部对应真实表格 —— 从 columns 派生每列宽度，行高随密度（small/medium/large）
const SKELETON_ROWS = 14
const SKELETON_ROW_HEIGHT: Record<string, number> = { small: 40, medium: 48, large: 56 }
// 单元格内骨架条占列宽的比例（按列位置循环，模拟不同长度的内容）
const SKELETON_CELL_FILL = ['62%', '48%', '80%', '55%', '70%', '45%', '75%', '58%', '66%', '50%']

const skeletonRowHeight = computed(() => SKELETON_ROW_HEIGHT[settings.density.value] ?? 40)

const skeletonColumns = computed(() => {
  // 前缀列（展开/多选/序号）不在 columns 里——它们由表格面板按开关插入，骨架屏照同一套开关补上
  const prefix: Array<{ key: string, width: string, control: boolean, fill: string }> = []
  if (renderExpand.value) {
    prefix.push({ key: '__expand__', width: '40px', control: true, fill: '' })
  }
  if (settings.selectable.value) {
    prefix.push({ key: '__select__', width: '40px', control: true, fill: '' })
  }
  if (settings.showIndex.value) {
    prefix.push({ key: '__index__', width: '60px', control: false, fill: '40%' })
  }
  return [
    ...prefix,
    ...columns.value.map((col, i) => ({
      key: col.key,
      // 按真实列宽，无宽度则 flex 平分剩余空间
      width: col.width === undefined ? '' : `${col.width}px`,
      control: false,
      fill: SKELETON_CELL_FILL[i % SKELETON_CELL_FILL.length] as string,
    })),
  ]
})

/**
 * 密度：用户偏好存的是 small/medium/large（后端 PagePreference 里也是这套词），
 * 组件库用的是 sm/md/lg。在此换算，不改存储词汇。
 */
const tableDensity = computed<'sm' | 'md' | 'lg'>(() => {
  const density = settings.density.value
  return density === 'small' ? 'sm' : density === 'large' ? 'lg' : 'md'
})
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full" :class="{ 'xh-schema-fullscreen': isFullscreen }">
    <!-- 搜索面板：与表格同款卡片容器；overflow 放开，高级条件浮层才不被卡片裁掉 -->
    <XhCardRoot
      v-if="searchFields.length || advancedFields.length"
      variant="outline"
      style="overflow: visible"
    >
      <XhCardBody class="xh-schema-card__body">
        <SchemaSearchPanel
          :advanced-fields="advancedFields"
          :common-fields="searchFields"
          :model="filters"
          @reset="reset"
          @search="search"
        >
          <template #settings>
            <SchemaSearchSettings
              :settings="searchSettings.settings.value"
              @move="searchSettings.move"
              @reset="searchSettings.resetDefault"
              @toggle-pin="searchSettings.togglePin"
              @toggle-visible="searchSettings.toggleVisible"
              @save="onSaveSearchSettings"
            />
          </template>
        </SchemaSearchPanel>
      </XhCardBody>
    </XhCardRoot>

    <!-- 操作工具栏：页面级操作按钮 + 内置工具（刷新/导入/导出/列设置/全屏） -->
    <XhCardRoot variant="outline" style="overflow: visible">
      <XhCardBody class="xh-schema-card__body xh-schema-card__body--toolbar">
        <SchemaActionPanel :actions="schema.actions ?? []" @action="onPageAction">
          <template #toolbar>
            <!-- 页面自定义工具栏项 -->
            <slot name="toolbar" :reload="reload" />
            <XIconButton
              icon="lucide:refresh-cw"
              :label="t('common.actions.refresh')"
              @click="reload"
            />
            <XIconButton
              v-if="canImport"
              icon="lucide:upload"
              :label="t('component.schema_page.import_csv')"
              @click="importVisible = true"
            />
            <!-- 导出按钮：仅在页面声明 exportPermission 且用户有该权限时显示（精准门控）；
                 已登记导出 Provider 的页面额外提供「提交到导出中心」异步入口，否则本地同步 CSV -->
            <template v-if="effectiveExportFields.length && canExportPermitted">
              <XhMenuRoot
                v-if="canSubmitExport"
                trigger-as-child
                :collection="exportMenuOptions"
                @select="(details: { value: string }) => onExportSelect(details.value)"
              >
                <template #trigger>
                  <button type="button" class="xh-icon-btn" :aria-label="t('component.schema_page.export_csv')">
                    <Icon icon="lucide:download" />
                  </button>
                </template>
              </XhMenuRoot>
              <!-- 未登记页面：维持本地同步 CSV 导出 -->
              <XIconButton
                v-else
                icon="lucide:download"
                :label="t('component.schema_page.export_csv')"
                :loading="exporting"
                @click="exportCsv"
              />
            </template>
            <SchemaTableSettings
              :columns="settings.columns.value"
              :density="settings.density.value"
              :table-style="settings.style.value"
              :selectable="settings.selectable.value"
              :show-index="settings.showIndex.value"
              @move="settings.move"
              @reset="onResetTableSettings"
              @set-density="settings.setDensity"
              @set-fixed="settings.setFixed"
              @set-width="onColumnWidthInput"
              @set-style="settings.setStyle"
              @set-selectable="settings.setSelectable"
              @set-show-index="settings.setShowIndex"
              @cycle-sort="onCycleSort"
              @toggle-visible="settings.toggleVisible"
              @save="onSaveTableSettings"
            />
            <XIconButton
              :icon="isFullscreen ? 'lucide:minimize' : 'lucide:maximize'"
              :label="isFullscreen ? t('component.schema_page.exit_fullscreen') : t('component.schema_page.enter_fullscreen')"
              @click="toggleFullscreen"
            />
          </template>
        </SchemaActionPanel>
      </XhCardBody>
    </XhCardRoot>

    <!-- 表格容器：定高卡片（flex-1 + height:0），卡片体成为定高 flex 列，滚动只发生在表格内部 -->
    <XhCardRoot class="flex-1" variant="outline" style="height: 0">
      <XhCardBody class="xh-schema-card__body xh-schema-card__body--table">
        <!-- 列表骨架屏：列宽/行高对应真实表格，逐行逐列，形似即将加载出来的数据 -->
        <XhSkeletonRoot v-if="!firstLoaded" class="xh-table-skeleton" aria-hidden="true">
          <div class="xh-skel-row xh-skel-row--head" :style="{ height: `${skeletonRowHeight}px` }">
            <div
              v-for="col in skeletonColumns"
              :key="`h-${col.key}`"
              class="xh-skel-cell"
              :style="col.width ? { flex: `0 0 ${col.width}` } : { flex: '1 1 0' }"
            >
              <XhSkeletonBone v-if="!col.control" class="xh-skel-bar" style="inline-size: 52%; block-size: 13px" />
            </div>
          </div>
          <div
            v-for="row in SKELETON_ROWS"
            :key="row"
            class="xh-skel-row"
            :style="{ height: `${skeletonRowHeight}px` }"
          >
            <div
              v-for="col in skeletonColumns"
              :key="`r${row}-${col.key}`"
              class="xh-skel-cell"
              :style="col.width ? { flex: `0 0 ${col.width}` } : { flex: '1 1 0' }"
            >
              <XhSkeletonBone v-if="col.control" class="xh-skel-bar xh-skel-bar--square" style="inline-size: 16px; block-size: 16px" />
              <XhSkeletonBone v-else class="xh-skel-bar" :style="{ inlineSize: col.fill, blockSize: '15px' }" />
            </div>
          </div>
        </XhSkeletonRoot>
        <template v-else>
          <!-- 表格：列表/树形两种模式（树形不分页、按 childrenKey 展开） -->
          <SchemaTablePanel
            v-model:checked-keys="checkedKeys"
            :columns="columns"
            :data="rows"
            :density="tableDensity"
            :striped="settings.style.value.striped"
            :bordered="settings.style.value.bordered"
            :single-line="settings.style.value.singleLine"
            :show-index="settings.showIndex.value"
            :loading="loading"
            :page="page"
            :page-size="pageSize"
            :row-key="schema.rowKey ?? 'basicId'"
            :selectable="settings.selectable.value"
            :sorts="sorts"
            :total="total"
            :tree="!!schema.tree"
            :children-key="schema.tree?.childrenKey ?? 'children'"
            :default-expand-all="schema.tree?.defaultExpandAll ?? true"
            :remount-key="tableRemountKey"
            :peek-fields="peekFields"
            :render-expand="renderExpand"
            @sort="changeSort"
            @update:page="changePage"
            @update:page-size="changePageSize"
            @resize-column="onColumnResize"
          >
            <!-- 批量浮条：放在页脚，选中后不挤压表格空间 -->
            <template v-if="checkedKeys.length" #footer-actions>
              <div class="xh-batch-bar">
                <span class="xh-batch-bar__count">{{ t('component.schema_page.selected_count_prefix') }} <strong>{{ checkedKeys.length }}</strong> {{ t('component.schema_page.selected_count_suffix') }}</span>
                <XhButton variant="ghost" size="sm" @click="clearSelection">
                  {{ t('component.schema_page.clear_selection') }}
                </XhButton>
                <XhButton
                  v-if="canBatchStatus"
                  size="sm"
                  variant="solid"
                  tone="success"
                  :loading="batchStatusUpdating"
                  @click="handleBatchStatus(true)"
                >
                  {{ t('component.schema_page.batch_enable') }}
                </XhButton>
                <XhButton
                  v-if="canBatchStatus"
                  size="sm"
                  variant="solid"
                  tone="warning"
                  :loading="batchStatusUpdating"
                  @click="handleBatchStatus(false)"
                >
                  {{ t('component.schema_page.batch_disable') }}
                </XhButton>
                <XhButton
                  v-if="canBatchRemove"
                  size="sm"
                  variant="solid"
                  tone="danger"
                  :loading="batchRemoving"
                  @click="handleBatchRemove"
                >
                  {{ t('component.schema_page.batch_delete') }}
                </XhButton>
                <XhButton
                  v-for="action in batchActions"
                  :key="action.key"
                  size="sm"
                  variant="outline"
                  :tone="toneOfActionType(action.type)"
                  @click="onBatchAction(action.key)"
                >
                  {{ action.title }}
                </XhButton>
              </div>
            </template>
          </SchemaTablePanel>
        </template>
      </XhCardBody>
    </XhCardRoot>

    <!-- 内置导入对话框（模板下载/解析/预校验/批量创建） -->
    <SchemaImportDialog
      v-if="canImport"
      v-model:show="importVisible"
      :create="schema.resource.create!"
      :fields="importFields"
      :page-code="schema.pageCode"
      :resource-code="schema.resourceCode"
      @finished="onImportFinished"
    />

    <!-- 默认插槽：承载页面自有弹窗/抽屉 -->
    <slot :reload="reload" />
  </div>
</template>

<style scoped>
/* 卡片体内边距：卡片皮肤给的是通用值，管理页三块卡片各有自己的紧凑档 */
.xh-schema-card__body {
  padding: 12px 16px;
}

.xh-schema-card__body--toolbar {
  padding: 8px 16px;
}

/* 表格卡片：卡片体成为定高 flex 列，内部滚动 */
.xh-schema-card__body--table {
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* 列表骨架屏：列宽/行高对应真实表格，单元格内边距 + 行分割线还原表格观感，填满表格区并裁剪溢出 */
.xh-table-skeleton {
  display: flex;
  flex: 1;
  flex-direction: column;
  overflow: hidden;
}

.xh-skel-row {
  display: flex;
  align-items: center;
  border-bottom: 1px solid rgba(128, 128, 128, 0.08);
}

.xh-skel-row--head {
  border-bottom: 1px solid rgba(128, 128, 128, 0.18);
}

.xh-skel-cell {
  box-sizing: border-box;
  min-width: 0;
  padding: 0 12px;
}

.xh-skel-bar {
  border-radius: var(--xh-radius-full);
}

.xh-skel-bar--square {
  border-radius: var(--xh-radius-sm);
}

.xh-batch-bar {
  display: flex;
  gap: 8px;
  align-items: center;
}

.xh-batch-bar__count {
  font-size: 13px;
  color: var(--xh-fg-default);
  white-space: nowrap;
}

.xh-batch-bar__count strong {
  font-weight: 600;
}

.xh-schema-fullscreen {
  position: fixed;
  inset: 0;
  z-index: 1000;
  color: hsl(var(--foreground));
  background: hsl(var(--background));
}
</style>
