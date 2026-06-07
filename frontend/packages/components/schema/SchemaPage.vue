<script setup lang="ts">
import type { DataTableColumn, DropdownOption } from 'naive-ui'
import type { ApiId } from '~/types/contracts'
import type { ActionSchema, ListFieldSchema, PageSchema, SchemaActionPayload } from './types'
import { NButton, NCard, NDropdown, NIcon, NSkeleton, NTooltip, useDialog, useMessage } from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import SchemaActionPanel from './SchemaActionPanel.vue'
import SchemaSearchPanel from './SchemaSearchPanel.vue'
import SchemaSearchSettings from './SchemaSearchSettings.vue'
import SchemaTablePanel from './SchemaTablePanel.vue'
import SchemaTableSettings from './SchemaTableSettings.vue'
import { toColumns, toExportFields } from './selectors'
import { useSchemaDictionaries } from './useSchemaDictionaries'
import { useSchemaExport } from './useSchemaExport'
import { useSchemaTable } from './useSchemaTable'
import { useSearchSettings } from './useSearchSettings'
import { useTableSettings } from './useTableSettings'
import { useViewManager } from './useViewManager'

defineOptions({ name: 'SchemaPage' })

// 行类型在框架边界放宽：页面侧以 PageSchema<ConcreteDto> 定义时保有完整类型安全；
// 此处用宽松行类型规避 Vue 泛型组件 prop 协变限制（具名 DTO 无索引签名，不兼容 Record<string, unknown>）。
// eslint-disable-next-line ts/no-explicit-any
type Row = Record<string, any>

const props = defineProps<{
  /** 页面单一事实源 */
  schema: PageSchema<Row>
}>()

const emit = defineEmits<{
  /** 操作事件（页面级/行级/批量级统一上抛，由页面处理具体逻辑） */
  action: [payload: SchemaActionPayload<Row>]
}>()

const { hasPermission } = usePermission()
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
    if (field.options?.length || !field.dictionaryCode) {
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

/** 是否存在批量能力（批量操作或内置批量删除）—— 作为「多选」默认开关 */
const autoSelectable = (props.schema.actions ?? []).some(a => a.scope === 'batch' && (!a.permission || hasPermission(a.permission)))
  || (!!props.schema.batchRemovable && !!props.schema.resource.remove)

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

/** 表格设置：调整即时生效，点击「保存」才落库（本地 + 后端） */
function onSaveTableSettings() {
  settings.save()
  message.success('表格设置已保存')
}

/** 搜索设置：调整即时生效，点击「保存」才落库（本地 + 后端） */
function onSaveSearchSettings() {
  searchSettings.save()
  message.success('搜索设置已保存')
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
    title: '操作',
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
const canBatchRemove = computed(() => !!props.schema.batchRemovable && !!props.schema.resource.remove)
const batchRemoving = ref(false)

function handleBatchRemove() {
  const targets = selectedRows.value
  const removeFn = props.schema.resource.remove
  if (targets.length === 0 || !removeFn) {
    return
  }
  const rowKey = props.schema.rowKey ?? 'basicId'
  dialog.warning({
    title: '批量删除',
    content: `确定删除选中的 ${targets.length} 条记录？此操作不可恢复。`,
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

function reload() {
  return table.load()
}

/** 导出字段（exportable + 权限） */
const exportFields = computed(() => toExportFields(resolvedSchema.value, hasPermission))

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
  fields: () => exportFields.value,
  fileName: () => props.schema.pageCode,
  fetchRows: fetchExportRows,
})

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
          <NTooltip v-if="exportFields.length">
            <template #trigger>
              <NButton circle quaternary size="small" aria-label="导出" :loading="exporting" @click="exportCsv">
                <template #icon>
                  <NIcon><Icon icon="lucide:download" /></NIcon>
                </template>
              </NButton>
            </template>
            导出（CSV）
          </NTooltip>
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
        <!-- 批量浮条 -->
        <div v-if="checkedKeys.length" class="xh-batch-toolbar">
          <span class="text-sm">已选择 {{ checkedKeys.length }} 条</span>
          <NButton quaternary size="small" @click="clearSelection">
            清空选择
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
          @sort="changeSort"
          @update:page="changePage"
          @update:page-size="changePageSize"
          @resize-column="onColumnResize"
        />
      </template>
    </NCard>

    <!-- 默认插槽：承载页面自有弹窗/抽屉 -->
    <slot :reload="reload" />
  </div>
</template>

<style scoped>
.xh-batch-toolbar {
  display: flex;
  flex-shrink: 0;
  gap: 12px;
  align-items: center;
  padding: 8px 12px;
  margin-bottom: 12px;
  border-radius: 8px;
  background: rgb(var(--primary) / 0.08);
}

.xh-schema-fullscreen {
  position: fixed;
  inset: 0;
  z-index: 1000;
  background: var(--n-color, #fff);
}
</style>
