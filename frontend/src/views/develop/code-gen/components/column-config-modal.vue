<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  ApiId,
  CodeGenTableColumnListItemDto,
  CodeGenTableColumnUpdateDto,
  HtmlType,
  QueryType,
} from '@/api'
import {
  NButton,
  NCheckbox,
  NDataTable,
  NInput,
  NModal,
  NSelect,
  NSpace,
  useMessage,
} from 'naive-ui'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTableColumnApi,
  HTML_TYPE_OPTIONS,
  QUERY_TYPE_OPTIONS,
} from '@/api'

defineOptions({ name: 'CodeGenColumnConfigModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'saved': []
}>()

const { t } = useI18n()
const message = useMessage()

const loading = ref(false)
const submitLoading = ref(false)
const rows = ref<CodeGenTableColumnListItemDto[]>([])

watch(
  () => props.show,
  (visible) => {
    if (visible && props.tableId) {
      void loadColumns()
    }
  },
)

async function loadColumns() {
  if (!props.tableId) {
    return
  }
  loading.value = true
  try {
    rows.value = await codeGenTableColumnApi.getByTable(props.tableId)
  }
  catch {
    message.error(t('develop.code_gen.column.load_failed'))
    rows.value = []
  }
  finally {
    loading.value = false
  }
}

type BooleanColumnField = 'isRequired' | 'isList' | 'isInsert' | 'isEdit' | 'isQuery'

function renderCheckbox(row: CodeGenTableColumnListItemDto, field: BooleanColumnField) {
  return h(NCheckbox, {
    'checked': row[field],
    'onUpdate:checked': (value: boolean) => {
      row[field] = value
    },
  })
}

const columns = computed<DataTableColumns<CodeGenTableColumnListItemDto>>(() => [
  { key: 'columnName', title: t('develop.code_gen.column.col_column_name'), minWidth: 140, fixed: 'left', ellipsis: { tooltip: true } },
  {
    key: 'columnComment',
    title: t('develop.code_gen.column.col_column_comment'),
    minWidth: 160,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(NInput, {
        'size': 'small',
        'value': row.columnComment ?? '',
        'onUpdate:value': (value: string) => {
          row.columnComment = value
        },
      }),
  },
  { key: 'columnType', title: t('develop.code_gen.column.col_column_type'), width: 110, ellipsis: { tooltip: true } },
  {
    key: 'cSharpType',
    title: t('develop.code_gen.column.col_csharp_type'),
    width: 130,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(NInput, {
        'size': 'small',
        'value': row.cSharpType ?? '',
        'onUpdate:value': (value: string) => {
          row.cSharpType = value
        },
      }),
  },
  {
    key: 'cSharpProperty',
    title: t('develop.code_gen.column.col_csharp_property'),
    width: 140,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(NInput, {
        'size': 'small',
        'value': row.cSharpProperty ?? '',
        'onUpdate:value': (value: string) => {
          row.cSharpProperty = value
        },
      }),
  },
  { key: 'isRequired', title: t('develop.code_gen.column.col_required'), width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isRequired') },
  { key: 'isList', title: t('develop.code_gen.column.col_list'), width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isList') },
  { key: 'isInsert', title: t('develop.code_gen.column.col_insert'), width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isInsert') },
  { key: 'isEdit', title: t('develop.code_gen.column.col_edit'), width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isEdit') },
  { key: 'isQuery', title: t('develop.code_gen.column.col_query'), width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isQuery') },
  {
    key: 'queryType',
    title: t('develop.code_gen.column.col_query_type'),
    width: 130,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(NSelect, {
        'size': 'small',
        'value': row.queryType,
        'options': QUERY_TYPE_OPTIONS,
        'onUpdate:value': (value: QueryType) => {
          row.queryType = value
        },
      }),
  },
  {
    key: 'htmlType',
    title: t('develop.code_gen.column.col_html_type'),
    width: 150,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(NSelect, {
        'size': 'small',
        'value': row.htmlType,
        'options': HTML_TYPE_OPTIONS,
        'onUpdate:value': (value: HtmlType) => {
          row.htmlType = value
        },
      }),
  },
  {
    key: 'dictType',
    title: t('develop.code_gen.column.col_dict_type'),
    width: 130,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(NInput, {
        'size': 'small',
        'value': row.dictType ?? '',
        'onUpdate:value': (value: string) => {
          row.dictType = value
        },
      }),
  },
])

async function handleSubmit() {
  if (!props.tableId) {
    return
  }
  submitLoading.value = true
  try {
    const payload: CodeGenTableColumnUpdateDto[] = rows.value.map(row => ({
      basicId: row.basicId,
      columnComment: row.columnComment,
      cSharpType: row.cSharpType,
      cSharpProperty: row.cSharpProperty,
      tsType: row.tsType,
      isRequired: row.isRequired,
      isList: row.isList,
      isInsert: row.isInsert,
      isEdit: row.isEdit,
      isQuery: row.isQuery,
      queryType: row.queryType,
      htmlType: row.htmlType,
      dictType: row.dictType,
      defaultValue: null,
      regexPattern: null,
      validationMessage: null,
      sort: row.sort,
      status: row.status,
    }))
    await codeGenTableColumnApi.batchSave({ tableId: props.tableId, columns: payload })
    message.success(t('develop.code_gen.common.save_success'))
    emit('saved')
    emit('update:show', false)
  }
  catch {
    message.error(t('develop.code_gen.common.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <NModal
    :auto-focus="false"
    :bordered="false"
    preset="card"
    :show="show"
    style="width: 96vw; max-width: 1280px"
    :title="t('develop.code_gen.column.title')"
    @update:show="emit('update:show', $event)"
  >
    <NDataTable
      :columns="columns"
      :data="rows"
      :loading="loading"
      max-height="60vh"
      :row-key="(row: CodeGenTableColumnListItemDto) => row.basicId"
      :scroll-x="1560"
      size="small"
    />

    <template #footer>
      <NSpace justify="end">
        <NButton @click="emit('update:show', false)">
          {{ t('develop.code_gen.common.cancel') }}
        </NButton>
        <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
          {{ t('develop.code_gen.common.save') }}
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>
