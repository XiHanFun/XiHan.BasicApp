<!--
  代码生成 / 表配置页签（照 4.x table-panel）
  接口取自后端契约（codegen-tables.*、codegen-table-columns.*）：导入库表、编辑表配置、列配置、预览产物、同步表结构与删除；生成与运行时数据随后续几笔补上
-->
<script setup lang="ts">
import type { GenerationStatus, TableRow } from './use-tables'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { codegenTables } from '~/contracts'
import ColumnConfigModal from './ColumnConfigModal.vue'
import ImportTableModal from './ImportTableModal.vue'
import PreviewModal from './PreviewModal.vue'
import RuntimeDataModal from './RuntimeDataModal.vue'
import TableEditModal from './TableEditModal.vue'
import { downloadPackage } from './use-histories'
import { DELIVERY_TYPES, enumKey, GENERATION_STATUSES, tablesResource, TEMPLATE_TYPES } from './use-tables'

defineOptions({ name: 'CodegenTablePanel' })

const { t } = useI18n()
const resource = tablesResource()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const importVisible = ref(false)
const editVisible = ref(false)
const columnVisible = ref(false)
const previewVisible = ref(false)
const runtimeVisible = ref(false)
const currentTableName = ref<string | undefined>()
const currentTableId = ref<null | string>(null)
/** 生成中不再接受第二次点击（照 4.x） */
const generating = ref(false)

const asRow = (row: object) => row as TableRow

function generationTone(status: GenerationStatus) {
  if (status === 'Generated')
    return 'success'
  return status === 'Failed' ? 'danger' : 'neutral'
}

function options<T extends string>(group: string, values: readonly T[]): SelectOption[] {
  return values.map(value => ({ label: t(enumKey(group, value)), value }))
}

const statusOptions = computed<SelectOption[]>(() => [
  { label: t('common.statuses.enabled'), value: 'Active' },
  { label: t('common.statuses.disabled'), value: 'Disabled' },
])

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('codegeneration.code_gen.table.col_table_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('codegeneration.code_gen.table.search_placeholder'), order: 0 },
  { key: 'tableName', title: t('codegeneration.code_gen.table.col_table_name'), dataType: 'string', minWidth: 170, fixed: 'left', sortable: true, order: 1 },
  { key: 'className', title: t('codegeneration.code_gen.table.col_class_name'), dataType: 'string', minWidth: 150, sortable: true, order: 2 },
  { key: 'tableComment', title: t('codegeneration.code_gen.table.col_table_comment'), dataType: 'string', minWidth: 140, sortable: true, order: 3 },
  { key: 'moduleName', title: t('codegeneration.code_gen.table.col_module'), dataType: 'string', width: 120, sortable: true, order: 4 },
  {
    key: 'templateType',
    title: t('codegeneration.code_gen.table.col_template_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: options('template_type', TEMPLATE_TYPES),
    searchPlaceholder: t('codegeneration.code_gen.table.filter_template_type'),
    width: 110,
    order: 5,
    render: row => t(enumKey('template_type', asRow(row).templateType)),
  },
  {
    key: 'deliveryType',
    title: t('codegeneration.code_gen.table.col_gen_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: options('delivery_type', DELIVERY_TYPES),
    searchPlaceholder: t('codegeneration.code_gen.table.filter_gen_type'),
    width: 120,
    order: 6,
    render: row => t(enumKey('delivery_type', asRow(row).deliveryType)),
  },
  {
    key: 'generationStatus',
    title: t('codegeneration.code_gen.table.col_gen_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: options('generation_status', GENERATION_STATUSES),
    searchPlaceholder: t('codegeneration.code_gen.table.filter_gen_status'),
    width: 110,
    order: 7,
    render: row => renderTag(t(enumKey('generation_status', asRow(row).generationStatus)), generationTone(asRow(row).generationStatus)),
  },
  {
    key: 'status',
    title: t('codegeneration.code_gen.table.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('codegeneration.code_gen.table.filter_status'),
    width: 90,
    order: 8,
    render: row => renderTag(asRow(row).status === 'Active' ? t('common.statuses.enabled') : t('common.statuses.disabled'), asRow(row).status === 'Active' ? 'success' : 'danger'),
  },
  { key: 'lastGeneratedAt', title: t('codegeneration.code_gen.table.col_last_gen'), dataType: 'datetime', minWidth: 170, sortable: true, order: 9 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'codegeneration.code-gen.table',
  pageName: t('codegeneration.code_gen.tabs.table'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'import', title: t('codegeneration.code_gen.table.import'), scope: 'page', tone: 'brand', icon: 'lucide:database', permission: 'codegeneration.codegen-tables.import' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'codegeneration.codegen-tables.update' },
    { key: 'columns', title: t('codegeneration.code_gen.table.action_columns'), scope: 'row', icon: 'lucide:table-2', permission: 'codegeneration.codegen-table-columns.update' },
    { key: 'preview', title: t('codegeneration.code_gen.table.action_preview'), scope: 'row', icon: 'lucide:eye', permission: 'codegeneration.codegen-tables.read' },
    // 两个生成动作按表配置的交付方式二选一呈现（照 4.x），免得同时给两个入口让人猜
    { key: 'generate', title: t('codegeneration.code_gen.table.action_generate'), scope: 'row', tone: 'brand', icon: 'lucide:download', visible: row => asRow(row).deliveryType === 'Zip', disabled: () => generating.value, permission: 'codegeneration.codegen-tables.execute' },
    // 直接写开发机上的代码目录，已存在的只首次写的文件跳过，其余覆盖
    { key: 'generateToDisk', title: t('codegeneration.code_gen.table.action_generate_to_disk'), scope: 'row', tone: 'brand', icon: 'lucide:folder-input', confirm: true, confirmText: () => t('codegeneration.code_gen.table.generate_to_disk_confirm'), visible: row => asRow(row).deliveryType === 'Project', disabled: () => generating.value, permission: 'codegeneration.codegen-tables.execute' },
    { key: 'sync', title: t('codegeneration.code_gen.table.action_sync'), scope: 'row', icon: 'lucide:refresh-cw', confirm: true, confirmText: () => t('codegeneration.code_gen.table.sync_confirm'), permission: 'codegeneration.codegen-tables.import' },
    // 只读查看表里的数据，不生成代码（照 4.x）；停用的表配置由后端拒绝
    { key: 'runtime', title: t('codegeneration.code_gen.table.action_runtime'), scope: 'row', icon: 'lucide:database', permission: 'codegeneration.codegen-runtime.read' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).tableName), permission: 'codegeneration.codegen-tables.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'import':
      importVisible.value = true
      break
    case 'edit':
      if (row) {
        currentTableId.value = row.id
        editVisible.value = true
      }
      break
    case 'columns':
      if (row) {
        currentTableId.value = row.id
        columnVisible.value = true
      }
      break
    case 'preview':
      if (row) {
        currentTableId.value = row.id
        currentTableName.value = row.tableName
        previewVisible.value = true
      }
      break
    case 'generate':
      if (row)
        void handleGenerate(row)
      break
    case 'generateToDisk':
      if (row)
        void handleGenerateToDisk(row)
      break
    case 'sync':
      if (row)
        void handleSync(row)
      break
    case 'runtime':
      if (row) {
        currentTableId.value = row.id
        currentTableName.value = row.tableName
        runtimeVisible.value = true
      }
      break
    case 'delete':
      if (row)
        void handleDelete(row)
      break
  }
}

/** 生成并下载：生成之后从生成历史的下载端点取产物包（4.x 在响应里带 Base64） */
async function handleGenerate(row: TableRow) {
  if (generating.value)
    return
  generating.value = true
  try {
    const result = await codegenTables.generate({ id: row.id, deliveryType: 'Zip' })
    await downloadPackage({ id: result.historyId, tableName: row.tableName, batchNumber: null })
    toast.success(t('codegeneration.code_gen.generate.generate_success', { count: result.fileCount }))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.generate.generate_failed'))
    reload()
  }
  finally {
    generating.value = false
  }
}

/** 生成到项目：后端写进 backend-next、前端写进 frontend-next（只在开发环境打开） */
async function handleGenerateToDisk(row: TableRow) {
  if (generating.value)
    return
  generating.value = true
  try {
    const result = await codegenTables.generate({ id: row.id, deliveryType: 'Project' })
    toast.success(t('codegeneration.code_gen.generate.write_success', { written: result.writtenCount, skipped: result.skippedPaths.length, location: result.targetRoots.join('、') }))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.generate.write_failed'))
    reload()
  }
  finally {
    generating.value = false
  }
}

async function handleSync(row: TableRow) {
  try {
    const result = await codegenTables.syncSchema({ id: row.id })
    toast.success(t('codegeneration.code_gen.table.sync_result', { added: result.addedCount, updated: result.updatedCount, removed: result.removedCount }))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.table.sync_failed'))
  }
}

async function handleDelete(row: TableRow) {
  try {
    const result = await codegenTables.delete({ ids: [row.id] })
    if (result.failed.length > 0)
      throw new Error(result.failed.map(f => f.code).join('; '))
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <ImportTableModal v-model:show="importVisible" @imported="reload" />
    <TableEditModal v-model:show="editVisible" :table-id="currentTableId" @saved="reload" />
    <ColumnConfigModal v-model:show="columnVisible" :table-id="currentTableId" @saved="reload" />
    <PreviewModal v-model:show="previewVisible" :table-id="currentTableId" :table-name="currentTableName" />
    <RuntimeDataModal v-model:show="runtimeVisible" :table-id="currentTableId" :table-name="currentTableName" />
  </SchemaPage>
</template>
