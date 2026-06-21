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
import { h, ref, watch } from 'vue'
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
    message.error('加载列配置失败')
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

const columns: DataTableColumns<CodeGenTableColumnListItemDto> = [
  { key: 'columnName', title: '列名', minWidth: 140, fixed: 'left', ellipsis: { tooltip: true } },
  {
    key: 'columnComment',
    title: '列说明',
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
  { key: 'columnType', title: '物理类型', width: 110, ellipsis: { tooltip: true } },
  {
    key: 'cSharpType',
    title: 'C# 类型',
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
    title: '属性名',
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
  { key: 'isRequired', title: '必填', width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isRequired') },
  { key: 'isList', title: '列表', width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isList') },
  { key: 'isInsert', title: '新增', width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isInsert') },
  { key: 'isEdit', title: '编辑', width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isEdit') },
  { key: 'isQuery', title: '查询', width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isQuery') },
  {
    key: 'queryType',
    title: '查询方式',
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
    title: '显示类型',
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
    title: '字典类型',
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
]

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
    message.success('保存成功')
    emit('saved')
    emit('update:show', false)
  }
  catch {
    message.error('保存失败')
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
    title="列配置"
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
          取消
        </NButton>
        <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
          保存
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>
