<script setup lang="ts">
import type {
  CodeGenTableListItemDto,
  GenStatus,
} from '../../../../api'
import type {
  ApiId,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { downloadBlob, getOptionLabel } from '~/utils'
import {
  codeGenerationApi,
  codeGenTableApi,
  EnableStatus,
  GEN_STATUS_OPTIONS,
  GEN_TYPE_OPTIONS,
  GenStatus as GenStatusEnum,
  GenType,
  TABLE_TEMPLATE_TYPE_OPTIONS,
} from '../../../../api'
import ColumnConfigModal from './column-config-modal.vue'
import ImportTableModal from './import-table-modal.vue'
import PreviewModal from './preview-modal.vue'
import RuntimeDataModal from './runtime-data-modal.vue'
import TableEditModal from './table-edit-modal.vue'

defineOptions({ name: 'CodeGenTablePanel' })

const { t } = useI18n()

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const importVisible = ref(false)
const editVisible = ref(false)
const columnVisible = ref(false)
const previewVisible = ref(false)
const generating = ref(false)
const runtimeVisible = ref(false)
const currentTableId = ref<ApiId | null>(null)
const currentTableName = ref('')

function genStatusTagType(status: GenStatus) {
  if (status === GenStatusEnum.Generated) {
    return 'success'
  }
  if (status === GenStatusEnum.Failed) {
    return 'danger'
  }
  return 'neutral'
}

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('develop.code_gen.table.col_table_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.code_gen.table.search_placeholder'), order: 0 },
  { key: 'tableName', title: t('develop.code_gen.table.col_table_name'), dataType: 'string', minWidth: 170, fixed: 'left', sortable: true, order: 1 },
  { key: 'className', title: t('develop.code_gen.table.col_class_name'), dataType: 'string', minWidth: 150, sortable: true, order: 2 },
  { key: 'tableComment', title: t('develop.code_gen.table.col_table_comment'), dataType: 'string', minWidth: 140, sortable: true, order: 3 },
  { key: 'moduleName', title: t('develop.code_gen.table.col_module'), dataType: 'string', width: 120, sortable: true, order: 4 },
  {
    key: 'templateType',
    title: t('develop.code_gen.table.col_template_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: TABLE_TEMPLATE_TYPE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.table.filter_template_type'),
    width: 110,
    order: 5,
    render: row => getOptionLabel(TABLE_TEMPLATE_TYPE_OPTIONS, (row as unknown as CodeGenTableListItemDto).templateType),
  },
  {
    key: 'genType',
    title: t('develop.code_gen.table.col_gen_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: GEN_TYPE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.table.filter_gen_type'),
    width: 120,
    order: 6,
    render: row => getOptionLabel(GEN_TYPE_OPTIONS, (row as unknown as CodeGenTableListItemDto).genType),
  },
  {
    key: 'genStatus',
    title: t('develop.code_gen.table.col_gen_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: GEN_STATUS_OPTIONS,
    searchPlaceholder: t('develop.code_gen.table.filter_gen_status'),
    width: 110,
    order: 7,
    render: (row) => {
      const r = row as unknown as CodeGenTableListItemDto
      return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: genStatusTagType(r.genStatus) }, () => h(XhTagLabel, () => getOptionLabel(GEN_STATUS_OPTIONS, r.genStatus)))
    },
  },
  {
    key: 'status',
    title: t('develop.code_gen.table.col_status'),
    dataType: 'enum',
    dictionaryCode: 'EnableStatus',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: STATUS_OPTIONS,
    searchPlaceholder: t('develop.code_gen.table.filter_status'),
    width: 90,
    order: 8,
    render: (row) => {
      const r = row as unknown as CodeGenTableListItemDto
      return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusEnumOptions.value, r.status)))
    },
  },
  { key: 'lastGenTime', title: t('develop.code_gen.table.col_last_gen'), dataType: 'datetime', minWidth: 170, sortable: true, order: 9 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.table',
  pageName: t('develop.code_gen.tabs.table'),
  rowKey: 'basicId',
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return codeGenTableApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(templateType/genStatus/status) 等通用过滤统一走 conditions.filters In
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => codeGenTableApi.delete(id),
  },
  actions: [
    { key: 'import', title: t('develop.code_gen.table.import'), scope: 'page', type: 'primary', icon: 'lucide:database' },
    { key: 'preview', title: t('develop.code_gen.table.action_preview'), scope: 'row', icon: 'lucide:eye' },
    // 两个生成动作按表配置的生成方式二选一呈现，避免同时给出两个入口让人猜该点哪个
    {
      key: 'generate',
      title: t('develop.code_gen.table.action_generate'),
      scope: 'row',
      type: 'primary',
      icon: 'lucide:download',
      visible: row => (row as unknown as CodeGenTableListItemDto).genType === GenType.Zip,
    },
    {
      key: 'generateToDisk',
      title: t('develop.code_gen.table.action_generate_to_disk'),
      scope: 'row',
      type: 'primary',
      icon: 'lucide:folder-input',
      // 直接写服务端代码目录，且手动文件已存在时会被跳过，属不可撤销操作
      confirm: true,
      confirmText: t('develop.code_gen.table.generate_to_disk_confirm'),
      visible: row => (row as unknown as CodeGenTableListItemDto).genType === GenType.CustomPath,
    },
    { key: 'columns', title: t('develop.code_gen.table.action_columns'), scope: 'row', icon: 'lucide:table-2' },
    { key: 'sync', title: t('develop.code_gen.table.action_sync'), scope: 'row', icon: 'lucide:refresh-cw' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'runtime', title: t('develop.code_gen.table.action_runtime'), scope: 'row', icon: 'lucide:database' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as CodeGenTableListItemDto | undefined
  switch (payload.key) {
    case 'import':
      importVisible.value = true
      break
    case 'preview':
      if (row) {
        currentTableId.value = row.basicId
        currentTableName.value = row.tableName
        previewVisible.value = true
      }
      break
    case 'generate':
      if (row) {
        void handleGenerate(row)
      }
      break
    case 'generateToDisk':
      if (row) {
        void handleGenerateToDisk(row)
      }
      break
    case 'columns':
      if (row) {
        currentTableId.value = row.basicId
        columnVisible.value = true
      }
      break
    case 'sync':
      if (row) {
        handleSync(row)
      }
      break
    case 'edit':
      if (row) {
        currentTableId.value = row.basicId
        editVisible.value = true
      }
      break
    case 'runtime':
      if (row) {
        currentTableId.value = row.basicId
        currentTableName.value = row.tableName
        runtimeVisible.value = true
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

/** 把后端返回的 base64 压缩包交给浏览器下载 */
function downloadZip(base64: string, fileName: string) {
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i)
  }
  downloadBlob(new Blob([bytes], { type: 'application/zip' }), fileName)
}

/** 生成并下载：不再经预览弹窗中转，预览是独立动作 */
async function handleGenerate(row: CodeGenTableListItemDto) {
  if (generating.value) {
    return
  }
  generating.value = true
  try {
    const result = await codeGenerationApi.generate({
      tableId: row.basicId,
      genType: GenType.Zip,
    })
    if (!result.success) {
      toast.error(result.message || t('develop.code_gen.generate.generate_failed'))
      return
    }
    if (result.packageBase64) {
      downloadZip(result.packageBase64, `${row.tableName || 'codegen'}_${Date.now()}.zip`)
      toast.success(t('develop.code_gen.generate.generate_success', { count: result.fileCount }))
    }
    else {
      toast.warning(t('develop.code_gen.generate.no_package'))
    }
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.generate.generate_failed'))
  }
  finally {
    generating.value = false
  }
}

/** 生成到现有代码结构：按表配置的生成路径落盘（后端受白名单与开关门控） */
async function handleGenerateToDisk(row: CodeGenTableListItemDto) {
  if (generating.value) {
    return
  }
  generating.value = true
  try {
    const result = await codeGenerationApi.generate({
      tableId: row.basicId,
      genType: GenType.CustomPath,
    })
    if (!result.success) {
      toast.error(result.message || t('develop.code_gen.generate.write_failed'))
      return
    }
    toast.success(t('develop.code_gen.generate.write_success', {
      written: result.writtenCount,
      skipped: result.skippedPaths?.length ?? 0,
    }))
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.generate.write_failed'))
  }
  finally {
    generating.value = false
  }
}

function handleSync(row: CodeGenTableListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('develop.code_gen.table.action_sync'),
    content: t('develop.code_gen.table.sync_confirm'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const result = await codeGenerationApi.syncSchema(row.basicId)
        toast.success(t('develop.code_gen.table.sync_result', {
          added: result.addedCount,
          updated: result.updatedCount,
          removed: result.removedCount,
        }))
        reload()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('develop.code_gen.table.sync_failed'))
      }
    },
  })
}

function handleDelete(row: CodeGenTableListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('common.actions.delete'),
    content: t('develop.code_gen.table.confirm_delete'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await codeGenTableApi.delete(row.basicId)
        toast.success(t('common.messages.delete_success'))
        reload()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('common.messages.delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <ImportTableModal v-model:show="importVisible" @imported="reload" />
    <TableEditModal v-model:show="editVisible" :table-id="currentTableId" @saved="reload" />
    <ColumnConfigModal v-model:show="columnVisible" :table-id="currentTableId" @saved="reload" />
    <PreviewModal
      v-model:show="previewVisible"
      :table-id="currentTableId"
      :table-name="currentTableName"
    />
    <RuntimeDataModal
      v-model:show="runtimeVisible"
      :table-id="currentTableId"
      :table-name="currentTableName"
    />
  </SchemaPage>
</template>
