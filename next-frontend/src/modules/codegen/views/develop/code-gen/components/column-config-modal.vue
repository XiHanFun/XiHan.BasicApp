<script setup lang="ts">
import type {
  CodeGenTableColumnListItemDto,
  CodeGenTableColumnUpdateDto,
  HtmlType,
  QueryType,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import type { XDataTableColumn } from '~/components'
import { XhBadge, XhCheckbox } from '@xihan-ui/vue'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDataTable, XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import {
  codeGenTableColumnApi,
  DICT_SELECTOR_TYPE_OPTIONS,
  DictSelectorType,
  HTML_TYPE_OPTIONS,
  QUERY_TYPE_OPTIONS,
} from '../../../../api'

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
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.column.load_failed'))
    rows.value = []
  }
  finally {
    loading.value = false
  }
}

type BooleanColumnField = 'isRequired' | 'isList' | 'isInsert' | 'isEdit' | 'isQuery'

/**
 * 基类托管列（主键/租户/审计/软删）的生成配置一律不可编辑。
 * 全部模板渲染时都跳过这些列，改了也不会进入任何产物，放开编辑只会让人以为配置生效了。
 * 判定由后端 GeneratedColumnNames 给出，与模板同源。
 */
function isLocked(row: CodeGenTableColumnListItemDto) {
  return row.isBaseColumn
}

function renderCheckbox(row: CodeGenTableColumnListItemDto, field: BooleanColumnField) {
  return h(XhCheckbox, {
    'checked': row[field],
    'disabled': isLocked(row),
    'onUpdate:checked': (value: boolean) => {
      row[field] = value
    },
  })
}

/** 字典取值列：按 dictSelectorType 渲染字典码 / 枚举全名 / 常量 JSON（互斥，仅生效项可编辑） */
function renderDictValue(row: CodeGenTableColumnListItemDto) {
  if (row.dictSelectorType === DictSelectorType.DictSelector) {
    return h(XInput, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.dictCode ?? '',
      'placeholder': t('develop.code_gen.column.col_dict_code_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        const value = raw as string
        row.dictCode = value
      },
    })
  }
  if (row.dictSelectorType === DictSelectorType.EnumSelector) {
    return h(XInput, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.enumTypeName ?? '',
      'placeholder': t('develop.code_gen.column.col_enum_type_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        const value = raw as string
        row.enumTypeName = value
      },
    })
  }
  if (row.dictSelectorType === DictSelectorType.ConstSelector) {
    return h(XInput, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.constValues ?? '',
      'placeholder': t('develop.code_gen.column.col_const_values_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        const value = raw as string
        row.constValues = value
      },
    })
  }
  return h('span', { style: 'color: var(--text-secondary)' }, '—')
}

const columns = computed<XDataTableColumn<CodeGenTableColumnListItemDto>[]>(() => [
  {
    key: 'columnName',
    title: t('develop.code_gen.column.col_column_name'),
    minWidth: 180,
    fixed: 'left',
    ellipsis: true,
    render: (row: CodeGenTableColumnListItemDto) =>
      h('div', { class: 'col-name' }, [
        h('span', null, row.columnName),
        isLocked(row)
          ? h(XhBadge, { variant: 'subtle', size: 'sm', tone: 'neutral' }, () => t('develop.code_gen.column.base_column'))
          : null,
      ]),
  },
  {
    key: 'columnComment',
    title: t('develop.code_gen.column.col_column_comment'),
    minWidth: 160,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(XInput, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.columnComment ?? '',
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          const value = raw as string
          row.columnComment = value
        },
      }),
  },
  { key: 'columnType', title: t('develop.code_gen.column.col_column_type'), width: 110, ellipsis: true },
  {
    key: 'cSharpType',
    title: t('develop.code_gen.column.col_csharp_type'),
    width: 130,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(XInput, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.cSharpType ?? '',
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          const value = raw as string
          row.cSharpType = value
        },
      }),
  },
  {
    key: 'cSharpProperty',
    title: t('develop.code_gen.column.col_csharp_property'),
    width: 140,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(XInput, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.cSharpProperty ?? '',
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          const value = raw as string
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
      h(XSelect, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.queryType,
        'options': QUERY_TYPE_OPTIONS,
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          const value = raw as QueryType
          row.queryType = value
        },
      }),
  },
  {
    key: 'htmlType',
    title: t('develop.code_gen.column.col_html_type'),
    width: 150,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(XSelect, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.htmlType,
        'options': HTML_TYPE_OPTIONS,
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          const value = raw as HtmlType
          row.htmlType = value
        },
      }),
  },
  {
    key: 'dictSelectorType',
    title: t('develop.code_gen.column.col_dict_selector'),
    width: 120,
    render: (row: CodeGenTableColumnListItemDto) =>
      h(XSelect, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.dictSelectorType ?? null,
        'options': DICT_SELECTOR_TYPE_OPTIONS,
        'clearable': true,
        'placeholder': t('develop.code_gen.column.col_dict_selector_placeholder'),
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          const value = raw as DictSelectorType | null
          // 切换选择器类型时清空其它取值，保持三分互斥
          row.dictSelectorType = value
          row.dictCode = null
          row.enumTypeName = null
          row.constValues = null
        },
      }),
  },
  {
    key: 'dictValue',
    title: t('develop.code_gen.column.col_dict_value'),
    width: 200,
    render: (row: CodeGenTableColumnListItemDto) => renderDictValue(row),
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
      dictSelectorType: row.dictSelectorType,
      dictCode: row.dictCode,
      enumTypeName: row.enumTypeName,
      constValues: row.constValues,
      defaultValue: null,
      regexPattern: null,
      validationMessage: null,
      sort: row.sort,
      status: row.status,
    }))
    await codeGenTableColumnApi.batchSave({ tableId: props.tableId, columns: payload })
    toast.success(t('common.messages.save_success'))
    emit('saved')
    emit('update:show', false)
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('develop.code_gen.column.title')"
    :width="1280"
    :loading="submitLoading"
    @update:show="emit('update:show', $event)"
    @save="handleSubmit"
  >
    <XDataTable
      :columns="columns"
      :data="rows"
      :loading="loading"
      max-height="60vh"
      :row-key="(row: CodeGenTableColumnListItemDto) => row.basicId"
      :scroll-x="1770"
      size="sm"
    />
  </XEditModal>
</template>

<style scoped>
.col-name {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}
</style>
