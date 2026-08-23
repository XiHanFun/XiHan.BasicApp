<!--
  模拟打印明细表编辑器。
  职责：按数据源列定义编辑明细行，提供稳定行标识，并确保内部 UUID 不进入最终打印数据。
-->
<script setup lang="ts">
import type { PrintSampleFormField, PrintTableColumn } from '~/printing'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { computed, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import {
  clonePrintSampleRecord,
  getPrintSampleValue,
  setPrintSampleValue,
} from '~/printing'
import ValueEditor from './PrintSampleValueEditor.vue'

defineOptions({ name: 'PrintSampleTableEditor' })

const props = defineProps<{
  field: PrintSampleFormField
  value: unknown
}>()

const emit = defineEmits<{
  'update:value': [value: Record<string, unknown>[]]
}>()

interface EditableTableRow {
  /** 仅用于 Vue 渲染稳定性的内存标识，不写入 data。 */
  id: string
  data: Record<string, unknown>
}

const { t } = useI18n()
const rows = shallowRef<EditableTableRow[]>([])
let lastEmittedValue: Record<string, unknown>[] | null = null
const columns = computed(() => props.field.columns ?? [])

watch(
  () => props.value,
  (value) => {
    if (value === lastEmittedValue) {
      lastEmittedValue = null
      return
    }
    rows.value = Array.isArray(value)
      ? value.filter(isRecord).map(data => ({ id: crypto.randomUUID(), data: clonePlainRecord(data) }))
      : []
  },
  { immediate: true },
)

/**
 * 把列元数据转换为通用单值编辑器字段。
 * @param column 明细列定义。
 * @returns 不可变的表单字段视图。
 */
function toColumnFormField(column: PrintTableColumn): PrintSampleFormField {
  return {
    key: column.field,
    label: column.title,
    kind: 'text',
    inputType: column.inputType,
    placeholder: column.placeholder,
    registered: true,
  }
}

/**
 * 更新一个单元格并发布不含内部 UUID 的新数组。
 * @param rowIndex 行索引。
 * @param column 列定义。
 * @param value 新单元格值。
 * @returns 无返回值。
 */
function updateCell(rowIndex: number, column: PrintTableColumn, value: unknown): void {
  const target = rows.value[rowIndex]
  if (!target)
    return
  const nextData = clonePrintSampleRecord(target.data)
  setPrintSampleValue(nextData, column.field, value)
  rows.value = rows.value.map((row, index) => index === rowIndex ? { ...row, data: nextData } : row)
  publishRows()
}

/** 新增一条按列类型初始化的空白明细。 */
function addRow(): void {
  const data: Record<string, unknown> = {}
  for (const column of columns.value) {
    const value = column.inputType === 'boolean'
      ? false
      : column.inputType === 'number'
        ? null
        : ''
    setPrintSampleValue(data, column.field, value)
  }
  rows.value = [...rows.value, { id: crypto.randomUUID(), data }]
  publishRows()
}

/** 复制指定明细并生成新的稳定标识，避免两行共享嵌套对象。 */
function duplicateRow(rowIndex: number): void {
  const source = rows.value[rowIndex]
  if (!source)
    return
  const duplicate = { id: crypto.randomUUID(), data: clonePrintSampleRecord(source.data) }
  rows.value = [
    ...rows.value.slice(0, rowIndex + 1),
    duplicate,
    ...rows.value.slice(rowIndex + 1),
  ]
  publishRows()
}

/** 删除指定明细；明细允许为空，以支持无明细打印场景。 */
function removeRow(rowIndex: number): void {
  rows.value = rows.value.filter((_, index) => index !== rowIndex)
  publishRows()
}

/** 发布纯数据数组，并记录引用以避免父组件回写触发无意义的 UUID 重建。 */
function publishRows(): void {
  lastEmittedValue = rows.value.map(row => clonePrintSampleRecord(row.data))
  emit('update:value', lastEmittedValue)
}

/** 克隆可能来自 Vue 响应式对象的表格行；只接受打印所需的 JSON 风格值。 */
function clonePlainRecord(record: Record<string, unknown>): Record<string, unknown> {
  const plain = { ...record }
  return clonePrintSampleRecord(plain)
}

/** 判断明细行是否为普通对象。 */
function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
}
</script>

<template>
  <section class="sample-table-editor">
    <div class="sample-table-toolbar">
      <span>{{ t('setting.print_template.sample_table_rows', { count: rows.length }) }}</span>
      <XhButton size="sm" tone="brand" variant="subtle" @click="addRow">
        <span><Icon icon="tabler:plus" /></span>
        {{ t('setting.print_template.sample_add_row') }}
      </XhButton>
    </div>

    <div v-if="rows.length" class="sample-table-scroll">
      <table>
        <thead>
          <tr>
            <th v-for="column in columns" :key="column.field">
              {{ column.title }}
            </th>
            <th class="sample-row-actions-heading">
              {{ t('setting.print_template.sample_row_actions') }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, rowIndex) in rows" :key="row.id">
            <td v-for="column in columns" :key="column.field">
              <ValueEditor
                :field="toColumnFormField(column)"
                :value="getPrintSampleValue(row.data, column.field)"
                @update:value="updateCell(rowIndex, column, $event)"
              />
            </td>
            <td class="sample-row-actions">
              <XhButton
                variant="ghost"
                data-circle
                size="sm"
                :title="t('setting.print_template.sample_duplicate_row')"
                @click="duplicateRow(rowIndex)"
              >
                <span><Icon icon="tabler:copy" /></span>
              </XhButton>
              <XhButton
                variant="ghost"
                data-circle
                size="sm"
                tone="danger"
                :title="t('setting.print_template.sample_delete_row')"
                @click="removeRow(rowIndex)"
              >
                <span><Icon icon="tabler:trash" /></span>
              </XhButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <XhEmptyStateRoot v-else size="sm">
      <XhEmptyStateIcon>
        <Icon icon="lucide:inbox" width="28" height="28" />
      </XhEmptyStateIcon>
      <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('setting.print_template.sample_table_empty') }}</XhEmptyStateDescription>
    </XhEmptyStateRoot>
  </section>
</template>

<style scoped>
.sample-table-editor {
  padding: 12px;
  border: 1px solid rgba(148, 163, 184, 0.28);
  border-radius: 8px;
  background: rgba(248, 250, 252, 0.7);
}

.sample-table-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 10px;
  color: #475569;
  font-size: 12px;
}

.sample-table-scroll {
  overflow-x: auto;
}

table {
  width: 100%;
  min-width: 620px;
  border-collapse: collapse;
}

th,
td {
  min-width: 150px;
  padding: 7px;
  border: 1px solid rgba(148, 163, 184, 0.24);
  text-align: left;
  vertical-align: middle;
}

th {
  background: rgba(226, 232, 240, 0.62);
  color: #334155;
  font-size: 12px;
  font-weight: 650;
}

.sample-row-actions-heading,
.sample-row-actions {
  width: 80px;
  min-width: 80px;
  text-align: center;
  white-space: nowrap;
}

:global(.dark) .sample-table-editor {
  background: rgba(15, 23, 42, 0.58);
}

:global(.dark) th {
  background: rgba(51, 65, 85, 0.72);
  color: #e2e8f0;
}
</style>
