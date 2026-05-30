<script setup lang="ts">
import type { DataTableColumn, DropdownOption } from 'naive-ui'
import type { ActionSchema, ListFieldSchema, PageSchema, SchemaActionPayload } from './types'
import { NButton, NCard, NDropdown, NIcon, NSkeleton, NTooltip } from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import SchemaActionPanel from './SchemaActionPanel.vue'
import SchemaSearchPanel from './SchemaSearchPanel.vue'
import SchemaSearchSettings from './SchemaSearchSettings.vue'
import SchemaTablePanel from './SchemaTablePanel.vue'
import SchemaTableSettings from './SchemaTableSettings.vue'
import SchemaViewManager from './SchemaViewManager.vue'
import { toColumns } from './selectors'
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

const firstLoaded = ref(false)
const checkedKeys = ref<Array<string | number>>([])

const table = useSchemaTable<Row>(props.schema)
const { loading, rows, total, page, pageSize, filters, sortField, sortOrder, search, reset, changePage, changePageSize, changeSort, remove } = table

/** 搜索字段池：所有 searchable 或 advancedSearch 且有权限的字段（保持 schema order） */
const searchPool = computed<ListFieldSchema[]>(() =>
  [...props.schema.fields]
    .filter(f => (f.searchable || f.advancedSearch) && (!f.permission || hasPermission(f.permission)))
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
)

/** 搜索设置（固定/排序，按 pageCode 持久化）→ 派生常用/高级字段 */
const searchSettings = useSearchSettings(props.schema.pageCode, searchPool)
const searchFields = searchSettings.commonFields
const advancedFields = searchSettings.advancedFields

/** 表格可选列字段（可见 + 有权限），作为列设置来源 */
const columnFields = computed<ListFieldSchema[]>(() =>
  props.schema.fields.filter(f => f.visible !== false && (!f.permission || hasPermission(f.permission))),
)

/** 列设置（显隐/顺序/固定/密度，按 pageCode 持久化） */
const settings = useTableSettings(props.schema.pageCode, columnFields)

/** 局部全屏 */
const isFullscreen = ref(false)
function toggleFullscreen() {
  isFullscreen.value = !isFullscreen.value
}

/** 视图管理（个人视图，按 pageCode 持久化） */
const viewManager = useViewManager(props.schema.pageCode)

/** 保存当前列表状态为视图 */
function saveView(name: string) {
  viewManager.addView(name, {
    filters: { ...filters },
    sortField: sortField.value,
    sortOrder: sortOrder.value,
    pageSize: pageSize.value,
  })
}

/** 应用视图：落地快照到表格状态并刷新 */
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
  const base = toColumns(props.schema, hasPermission, {
    visibleKeys: settings.visibleKeys.value,
    columnOrder: settings.columnOrder.value,
    fixedMap: settings.fixedMap.value,
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

function reload() {
  return table.load()
}

onMounted(async () => {
  await table.load()
  firstLoaded.value = true
})

defineExpose({ reload, remove, clearSelection, filters })
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full" :class="{ 'xh-schema-fullscreen': isFullscreen }">
    <!-- 搜索面板（含内部滑入的高级条件 + 搜索设置） -->
    <SchemaSearchPanel
      v-if="searchFields.length || advancedFields.length"
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
        />
      </template>
    </SchemaSearchPanel>

    <!-- 搜索方案（个人视图） -->
    <div class="flex justify-end">
      <SchemaViewManager
        :active-code="viewManager.activeCode.value"
        :views="viewManager.views.value"
        @apply="(code: string) => applyView(code)"
        @remove="(code: string) => viewManager.removeView(code)"
        @save="(name: string) => saveView(name)"
        @set-default="(code: string) => viewManager.setDefault(code)"
      />
    </div>

    <NCard class="flex-1" style="height: 0">
      <NSkeleton v-if="!firstLoaded" :height="48" :repeat="5" text style="padding: 16px" />
      <template v-else>
        <!-- 操作面板 -->
        <div class="mb-3">
          <SchemaActionPanel :actions="schema.actions ?? []" @action="onPageAction">
            <template #toolbar>
              <!-- 页面自定义工具栏项 -->
              <slot name="toolbar" :reload="reload" />
              <!-- 内置工具栏：刷新 / 列设置 / 全屏 -->
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
              <SchemaTableSettings
                :columns="settings.columns.value"
                :density="settings.density.value"
                @move="settings.move"
                @reset="settings.resetDefault"
                @set-density="settings.setDensity"
                @set-fixed="settings.setFixed"
                @toggle-visible="settings.toggleVisible"
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
        </div>

        <!-- 批量浮条 -->
        <div v-if="checkedKeys.length" class="xh-batch-toolbar">
          <span class="text-sm">已选择 {{ checkedKeys.length }} 条</span>
          <NButton quaternary size="small" @click="clearSelection">
            清空选择
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

        <!-- 表格 -->
        <SchemaTablePanel
          v-model:checked-keys="checkedKeys"
          :columns="columns"
          :data="rows"
          :density="settings.density.value"
          :loading="loading"
          :page="page"
          :page-size="pageSize"
          :row-key="schema.rowKey ?? 'basicId'"
          :scroll-x="schema.scrollX"
          :selectable="batchActions.length > 0"
          :total="total"
          @sort="changeSort"
          @update:page="changePage"
          @update:page-size="changePageSize"
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
