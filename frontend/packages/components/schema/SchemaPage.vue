<script setup lang="ts" generic="TRow extends Record<string, unknown>">
import type { DataTableColumn, DropdownOption } from 'naive-ui'
import type { ActionSchema, PageSchema } from './types'
import { NButton, NCard, NDropdown, NIcon, NSkeleton } from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import SchemaActionPanel from './SchemaActionPanel.vue'
import SchemaSearchPanel from './SchemaSearchPanel.vue'
import SchemaTablePanel from './SchemaTablePanel.vue'
import { toAdvancedFields, toColumns, toSearchFields } from './selectors'
import { useSchemaTable } from './useSchemaTable'

defineOptions({ name: 'SchemaPage' })

const props = defineProps<{
  /** 页面单一事实源 */
  schema: PageSchema<TRow>
}>()

const emit = defineEmits<{
  /** 操作事件（页面级/行级/批量级统一上抛，由页面处理具体逻辑） */
  action: [payload: { key: string, scope: 'page' | 'row' | 'batch', row?: TRow, rows?: TRow[] }]
}>()

const { hasPermission } = usePermission()

const firstLoaded = ref(false)
const checkedKeys = ref<Array<string | number>>([])

const table = useSchemaTable<TRow>(props.schema)
const { loading, rows, total, page, pageSize, filters, search, reset, changePage, changePageSize, changeSort, remove } = table

const searchFields = computed(() => toSearchFields(props.schema, hasPermission))
const advancedFields = computed(() => toAdvancedFields(props.schema, hasPermission))

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

/** 列：schema 派生列 + 行操作列 */
const columns = computed<DataTableColumn<TRow>[]>(() => {
  const base = toColumns(props.schema, hasPermission)
  if (rowActions.value.length === 0) {
    return base
  }
  const actionColumn = {
    key: '__actions__',
    title: '操作',
    width: 90,
    fixed: 'right',
    render: (row: TRow) => renderRowActions(row),
  } as DataTableColumn<TRow>
  return [...base, actionColumn]
})

function visibleRowActions(row: TRow): ActionSchema<TRow>[] {
  return rowActions.value.filter(a => !a.visible || a.visible(row))
}

function renderRowActions(row: TRow) {
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
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <!-- 搜索面板 -->
    <SchemaSearchPanel
      v-if="searchFields.length"
      :fields="searchFields"
      :has-advanced="advancedFields.length > 0"
      :model="filters"
      @open-advanced="emit('action', { key: '__advanced__', scope: 'page' })"
      @reset="reset"
      @search="search"
    />

    <NCard class="flex-1" style="height: 0">
      <NSkeleton v-if="!firstLoaded" :height="48" :repeat="5" text style="padding: 16px" />
      <template v-else>
        <!-- 操作面板 -->
        <div class="mb-3">
          <SchemaActionPanel :actions="schema.actions ?? []" @action="onPageAction">
            <template #toolbar>
              <slot name="toolbar" :reload="reload" />
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
</style>
