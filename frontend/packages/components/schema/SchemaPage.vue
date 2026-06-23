<script setup lang="ts">
import type { DataTableColumn, DropdownOption } from 'naive-ui'
import type { ActionSchema, ListFieldSchema, PageSchema, SchemaActionPayload } from './types'
import type { ApiId } from '~/types/contracts'
import { NButton, NCard, NDropdown, NIcon, NSkeleton, NTooltip, useDialog, useMessage } from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { islandStart } from '~/composables/useDynamicIsland'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAppStore } from '~/stores'
import SchemaActionPanel from './SchemaActionPanel.vue'
import SchemaImportDialog from './SchemaImportDialog.vue'
import SchemaSearchPanel from './SchemaSearchPanel.vue'
import SchemaSearchSettings from './SchemaSearchSettings.vue'
import SchemaTablePanel from './SchemaTablePanel.vue'
import SchemaTableSettings from './SchemaTableSettings.vue'
import { toColumns, toExportFields, toImportFields } from './selectors'
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
const dialog = useDialog()
const message = useMessage()

const firstLoaded = ref(false)
const checkedKeys = ref<Array<string | number>>([])

const table = useSchemaTable<Row>(props.schema)
const { loading, rows, total, page, pageSize, filters, sortField, sortOrder, search, reset, changePage, changePageSize, changeSort, remove } = table

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

/** 是否存在批量能力（批量操作或内置批量删除）—— 作为「多选」默认开关 */
const autoSelectable = (props.schema.actions ?? []).some(a => a.scope === 'batch' && (!a.permission || hasPermission(a.permission)))
  || (!!props.schema.batchRemovable && !!props.schema.resource.remove && (!props.schema.removePermission || hasPermission(props.schema.removePermission)))
  || (!!props.schema.resource.updateStatus && (!props.schema.statusPermission || hasPermission(props.schema.statusPermission)))

/** 列设置（显隐/顺序/固定/密度/风格/多选/序号/列宽，按 pageCode 持久化） */
const settings = useTableSettings(props.schema.pageCode, columnFields, { defaultSelectable: autoSelectable })

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
    sortField: sortField.value,
    sortOrder: sortOrder.value,
    pageSize: pageSize.value,
  })
}

/** 应用方案：落地快照到表格状态并刷新 */
function applyView(code: string) {
  const snapshot = viewManager.applyView(code)
  if (!snapshot) {
    return
  }
  for (const key of Object.keys(filters)) {
    delete filters[key]
  }
  Object.assign(filters, snapshot.filters)
  sortField.value = snapshot.sortField
  sortOrder.value = snapshot.sortOrder
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
const columns = computed<DataTableColumn<Row>[]>(() => {
  const base = toColumns(resolvedSchema.value, hasPermission, {
    visibleKeys: settings.visibleKeys.value,
    columnOrder: settings.columnOrder.value,
    fixedMap: settings.fixedMap.value,
    widthMap: settings.widthMap.value,
  })
  if (rowActions.value.length === 0) {
    return base
  }
  const actionColumn = {
    key: '__actions__',
    title: t('component.schema_page.actions_column'),
    width: 90,
    fixed: 'right',
    render: (row: Row) => renderRowActions(row),
  } as unknown as DataTableColumn<Row>
  return [...base, actionColumn]
})

function visibleRowActions(row: Row): ActionSchema<Row>[] {
  return rowActions.value.filter(a => !a.visible || a.visible(row))
}

function renderRowActions(row: Row) {
  const options: DropdownOption[] = visibleRowActions(row).map(a => ({
    key: a.key,
    label: a.title,
    disabled: a.disabled ? a.disabled(row) : false,
  }))
  if (options.length === 0) {
    return h('span', { class: 'text-foreground/30' }, '-')
  }
  return h(
    NDropdown,
    {
      options,
      trigger: 'click',
      onSelect: (key: string) => emit('action', { key, scope: 'row', row }),
    },
    {
      default: () =>
        h(NButton, { quaternary: true, size: 'small' }, {
          default: () => '更多',
          icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:chevron-down' })),
        }),
    },
  )
}

function onPageAction(key: string) {
  emit('action', { key, scope: 'page' })
}

function onBatchAction(key: string) {
  emit('action', { key, scope: 'batch', rows: selectedRows.value })
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
  dialog.warning({
    title: t('component.schema_page.batch_delete_title'),
    content: t('component.schema_page.batch_delete_content', { count: targets.length }),
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      batchRemoving.value = true
      try {
        const results = await Promise.allSettled(
          targets.map(row => removeFn((row as Record<string, unknown>)[rowKey] as ApiId)),
        )
        const failed = results.filter(r => r.status === 'rejected').length
        if (failed === 0) {
          message.success(`已删除 ${targets.length} 条`)
        }
        else {
          message.warning(`删除完成：成功 ${targets.length - failed} 条，失败 ${failed} 条`)
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
  const label = enabled ? '启用' : '停用'
  dialog.warning({
    title: t('component.schema_page.batch_action_title', { label }),
    content: t('component.schema_page.batch_action_content', { label, count: targets.length }),
    positiveText: `确认${label}`,
    negativeText: '取消',
    onPositiveClick: async () => {
      batchStatusUpdating.value = true
      try {
        const results = await Promise.allSettled(
          targets.map(row => updateFn((row as Record<string, unknown>)[rowKey] as ApiId, enabled)),
        )
        const failed = results.filter(r => r.status === 'rejected').length
        if (failed === 0) {
          message.success(`已${label} ${targets.length} 条`)
        }
        else {
          message.warning(`${label}完成：成功 ${targets.length - failed} 条，失败 ${failed} 条`)
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
      sortField: sortField.value,
      sortOrder: sortOrder.value,
      filters: { ...filters },
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

const exportMenuOptions = computed(() => [
  { key: 'center:1', label: t('component.schema_page.export_results') },
  { key: 'center:0', label: t('component.schema_page.export_current_page') },
  { key: 'center:2', label: t('component.schema_page.export_all') },
  { key: 'divider', type: 'divider' },
  { key: 'local', label: t('component.schema_page.export_csv_local') },
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
    sortField: sortField.value,
    sortOrder: sortOrder.value,
    filters: { ...filters },
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
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full" :class="{ 'xh-schema-fullscreen': isFullscreen }">
    <!-- 搜索面板：用 NCard 包裹（与表格同款容器），高级条件浮层在卡片内对齐 -->
    <NCard
      v-if="searchFields.length || advancedFields.length"
      size="small"
      :content-style="{ padding: '12px 16px' }"
      :style="{ overflow: 'visible' }"
    >
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
    </NCard>

    <!-- 操作工具栏：页面级操作按钮 + 内置工具（刷新/列设置/全屏） -->
    <NCard size="small" :content-style="{ padding: '8px 16px' }">
      <SchemaActionPanel :actions="schema.actions ?? []" @action="onPageAction">
        <template #toolbar>
          <!-- 页面自定义工具栏项 -->
          <slot name="toolbar" :reload="reload" />
          <!-- 内置工具：刷新 / 列设置 / 全屏 -->
          <NTooltip>
            <template #trigger>
              <NButton circle quaternary size="small" aria-label="刷新" @click="reload">
                <template #icon>
                  <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
                </template>
              </NButton>
            </template>
            刷新
          </NTooltip>
          <NTooltip v-if="canImport">
            <template #trigger>
              <NButton circle quaternary size="small" aria-label="导入" @click="importVisible = true">
                <template #icon>
                  <NIcon><Icon icon="lucide:upload" /></NIcon>
                </template>
              </NButton>
            </template>
            导入（CSV）
          </NTooltip>
          <!-- 导出按钮：仅在页面声明 exportPermission 且用户有该权限时显示（精准门控）；
               已登记导出 Provider 的页面额外提供「提交到导出中心」异步入口，否则本地同步 CSV -->
          <template v-if="effectiveExportFields.length && canExportPermitted">
            <!-- 已登记导出 Provider 的页面：提供「提交到导出中心」异步入口 + 本地同步 CSV 兜底 -->
            <NDropdown v-if="canSubmitExport" trigger="click" :options="exportMenuOptions" @select="onExportSelect">
              <NButton circle quaternary size="small" aria-label="导出" :loading="exporting || submittingExport">
                <template #icon>
                  <NIcon><Icon icon="lucide:download" /></NIcon>
                </template>
              </NButton>
            </NDropdown>
            <!-- 未登记页面：维持本地同步 CSV 导出 -->
            <NTooltip v-else>
              <template #trigger>
                <NButton circle quaternary size="small" aria-label="导出" :loading="exporting" @click="exportCsv">
                  <template #icon>
                    <NIcon><Icon icon="lucide:download" /></NIcon>
                  </template>
                </NButton>
              </template>
              导出（CSV）
            </NTooltip>
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
            @toggle-visible="settings.toggleVisible"
            @save="onSaveTableSettings"
          />
          <NTooltip>
            <template #trigger>
              <NButton circle quaternary size="small" aria-label="全屏" @click="toggleFullscreen">
                <template #icon>
                  <NIcon><Icon :icon="isFullscreen ? 'lucide:minimize' : 'lucide:maximize'" /></NIcon>
                </template>
              </NButton>
            </template>
            {{ isFullscreen ? '退出全屏' : '全屏' }}
          </NTooltip>
        </template>
      </SchemaActionPanel>
    </NCard>

    <!-- 表格容器：定高卡片（flex-1 + height:0），content 成为定高 flex 列，滚动只发生在表格内部 -->
    <NCard
      class="flex-1"
      style="height: 0"
      :content-style="{ height: '100%', display: 'flex', flexDirection: 'column', padding: '12px 16px' }"
    >
      <NSkeleton v-if="!firstLoaded" :height="48" :repeat="5" text style="padding: 16px" />
      <template v-else>
        <!-- 表格：列表/树形两种模式（树形不分页、按 childrenKey 展开） -->
        <SchemaTablePanel
          v-model:checked-keys="checkedKeys"
          :columns="columns"
          :data="rows"
          :density="settings.density.value"
          :striped="settings.style.value.striped"
          :bordered="settings.style.value.bordered"
          :single-line="settings.style.value.singleLine"
          :show-index="settings.showIndex.value"
          :loading="loading"
          :page="page"
          :page-size="pageSize"
          :row-key="schema.rowKey ?? 'basicId'"
          :scroll-x="schema.scrollX"
          :selectable="settings.selectable.value"
          :total="total"
          :tree="!!schema.tree"
          :children-key="schema.tree?.childrenKey ?? 'children'"
          :default-expand-all="schema.tree?.defaultExpandAll ?? true"
          :remount-key="tableRemountKey"
          :peek-fields="peekFields"
          @sort="changeSort"
          @update:page="changePage"
          @update:page-size="changePageSize"
          @resize-column="onColumnResize"
        >
          <!-- 批量浮条：放在页脚，选中后不挤压表格空间 -->
          <template v-if="checkedKeys.length" #footer-actions>
            <div class="xh-batch-bar">
              <span class="xh-batch-bar__count">已选择 <strong>{{ checkedKeys.length }}</strong> 条</span>
              <NButton quaternary size="small" @click="clearSelection">
                清空选择
              </NButton>
              <NButton
                v-if="canBatchStatus"
                size="small"
                type="success"
                :loading="batchStatusUpdating"
                @click="handleBatchStatus(true)"
              >
                批量启用
              </NButton>
              <NButton
                v-if="canBatchStatus"
                size="small"
                type="warning"
                :loading="batchStatusUpdating"
                @click="handleBatchStatus(false)"
              >
                批量停用
              </NButton>
              <NButton
                v-if="canBatchRemove"
                size="small"
                type="error"
                :loading="batchRemoving"
                @click="handleBatchRemove"
              >
                批量删除
              </NButton>
              <NButton
                v-for="action in batchActions"
                :key="action.key"
                size="small"
                :type="action.type ?? 'default'"
                @click="onBatchAction(action.key)"
              >
                {{ action.title }}
              </NButton>
            </div>
          </template>
        </SchemaTablePanel>
      </template>
    </NCard>

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
.xh-batch-bar {
  display: flex;
  gap: 8px;
  align-items: center;
}

.xh-batch-bar__count {
  font-size: 13px;
  color: var(--n-text-color);
  white-space: nowrap;
}

.xh-batch-bar__count strong {
  font-weight: 600;
  color: var(--n-text-color);
}

.xh-schema-fullscreen {
  position: fixed;
  inset: 0;
  z-index: 1000;
  background: var(--n-color, #fff);
}
</style>
