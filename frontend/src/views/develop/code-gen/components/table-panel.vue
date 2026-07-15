<script setup lang="ts">
import type { ApiId, CodeGenTableListItemDto, GenStatus, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { NTag, useDialog, useMessage } from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTableApi,
  createPageRequest,
  EnableStatus,
  GEN_STATUS_OPTIONS,
  GenStatus as GenStatusEnum,
  querySortsFromSchema,
  TEMPLATE_TYPE_OPTIONS,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage } from '~/components'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import ColumnConfigModal from './column-config-modal.vue'
import GenerateModal from './generate-modal.vue'
import ImportTableModal from './import-table-modal.vue'
import RuntimeDataModal from './runtime-data-modal.vue'
import TableEditModal from './table-edit-modal.vue'

defineOptions({ name: 'CodeGenTablePanel' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const importVisible = ref(false)
const editVisible = ref(false)
const columnVisible = ref(false)
const generateVisible = ref(false)
const runtimeVisible = ref(false)
const currentTableId = ref<ApiId | null>(null)
const currentTableName = ref('')

function genStatusTagType(status: GenStatus) {
  if (status === GenStatusEnum.Generated) {
    return 'success'
  }
  if (status === GenStatusEnum.Failed) {
    return 'error'
  }
  return 'default'
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
    options: TEMPLATE_TYPE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.table.filter_template_type'),
    width: 110,
    order: 5,
    render: row => getOptionLabel(TEMPLATE_TYPE_OPTIONS, (row as unknown as CodeGenTableListItemDto).templateType),
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
    order: 6,
    render: (row) => {
      const r = row as unknown as CodeGenTableListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: genStatusTagType(r.genStatus) }, () => getOptionLabel(GEN_STATUS_OPTIONS, r.genStatus))
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
    order: 7,
    render: (row) => {
      const r = row as unknown as CodeGenTableListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusEnumOptions.value, r.status))
    },
  },
  { key: 'lastGenTime', title: t('develop.code_gen.table.col_last_gen'), dataType: 'datetime', minWidth: 170, sortable: true, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.table',
  pageName: t('develop.code_gen.tabs.table'),
  rowKey: 'basicId',
  scrollX: 1448,
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
    { key: 'generate', title: t('develop.code_gen.table.action_generate'), scope: 'row', type: 'primary', icon: 'lucide:play' },
    { key: 'columns', title: t('develop.code_gen.table.action_columns'), scope: 'row', icon: 'lucide:table-2' },
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
    case 'generate':
      if (row) {
        currentTableId.value = row.basicId
        currentTableName.value = row.tableName
        generateVisible.value = true
      }
      break
    case 'columns':
      if (row) {
        currentTableId.value = row.basicId
        columnVisible.value = true
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

function handleDelete(row: CodeGenTableListItemDto) {
  dialog.warning({
    title: t('common.actions.delete'),
    content: t('develop.code_gen.table.confirm_delete'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await codeGenTableApi.delete(row.basicId)
        message.success(t('common.messages.delete_success'))
        reload()
      }
      catch {
        message.error(t('common.messages.delete_failed'))
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
    <GenerateModal
      v-model:show="generateVisible"
      :table-id="currentTableId"
      :table-name="currentTableName"
      @generated="reload"
    />
    <RuntimeDataModal
      v-model:show="runtimeVisible"
      :table-id="currentTableId"
      :table-name="currentTableName"
    />
  </SchemaPage>
</template>
