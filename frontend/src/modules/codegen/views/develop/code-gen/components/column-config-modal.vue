<script setup lang="ts">
import type {
  CodeGenTableColumnListItemDto,
  CodeGenTableColumnUpdateDto,
  CodeGenTableListItemDto,
  HtmlType,
  QueryType,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import type { XDataTableColumn } from '~/components'
import { XhCheckbox, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, dictApi } from '@/api'
import { XDataTable, XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import {
  codeGenTableApi,
  codeGenTableColumnApi,
  DICT_SELECTOR_TYPE_OPTIONS,
  DictSelectorType,
  HTML_TYPE_OPTIONS,
  QUERY_TYPE_OPTIONS,
  TemplateType,
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
      void loadDicts()
      void loadTables()
    }
  },
)

/** 字典选择器的候选字典（字典管理里的字典；按编码存，下拉显示「名称（编码）」） */
const dictOptions = ref<{ label: string, value: string }[]>([])

async function loadDicts() {
  try {
    // 分页上限即 500：字典是配置型数据，一页取完
    const result = await dictApi.page({ ...createPageRequest({ page: { pageIndex: 1, pageSize: 500 } }) })
    dictOptions.value = result.items.map(dict => ({ label: `${dict.dictName}（${dict.dictCode}）`, value: dict.dictCode }))
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('develop.code_gen.column.load_dicts_failed'))
    dictOptions.value = []
  }
}

/** 关联选择器的候选表（全部表配置，一次取全） */
const tables = ref<CodeGenTableListItemDto[]>([])

async function loadTables() {
  try {
    tables.value = await codeGenTableApi.options()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('develop.code_gen.column.load_tables_failed'))
    tables.value = []
  }
}

/** 关联的表可选项：关联树只能选树表；已选的表不在列表里时也列出来并标明 */
function relationTableOptionsFor(row: CodeGenTableColumnListItemDto) {
  const candidates = tables.value
    .filter(table => row.dictSelectorType !== DictSelectorType.TreeSelector || table.templateType === TemplateType.Tree)
    .map(table => ({ label: table.tableComment ? `${table.tableComment}（${table.tableName}）` : table.tableName, value: table.basicId }))
  const current = row.relationTableId
  if (!current || candidates.some(option => option.value === current)) {
    return candidates
  }
  return [{ label: t('develop.code_gen.column.relation_table_not_found', { id: current }), value: current }, ...candidates]
}

/** 关联表的列（显示列候选），按表缓存 */
const relationColumns = reactive(new Map<ApiId, CodeGenTableColumnListItemDto[]>())

async function ensureRelationColumns(tableId: ApiId | null | undefined) {
  if (!tableId || relationColumns.has(tableId)) {
    return
  }
  relationColumns.set(tableId, [])
  try {
    relationColumns.set(tableId, await codeGenTableColumnApi.getByTable(tableId))
  }
  catch (error) {
    relationColumns.delete(tableId)
    toast.danger((error as Error)?.message || t('develop.code_gen.column.load_failed'))
  }
}

/** 显示列可选项：关联表的文本业务列；已选的列不在其中时也列出来 */
function relationLabelOptionsFor(row: CodeGenTableColumnListItemDto) {
  const candidates = (row.relationTableId ? relationColumns.get(row.relationTableId) ?? [] : [])
    .filter(column => !column.isBaseColumn && column.cSharpType?.replace('?', '') === 'string')
    .map(column => ({ label: column.columnComment ? `${column.columnComment}（${column.columnName}）` : column.columnName, value: column.columnName }))
  const current = row.relationLabelColumn
  if (!current || candidates.some(option => option.value === current)) {
    return candidates
  }
  return [{ label: current, value: current }, ...candidates]
}

/** 本列可选的字典：已填的编码不在列表里（字典被删或编码写错）时也列出来并标明，免得下拉显示成空 */
function dictOptionsFor(row: CodeGenTableColumnListItemDto) {
  const current = row.dictCode?.trim()
  if (!current || dictOptions.value.some(option => option.value === current)) {
    return dictOptions.value
  }
  return [{ label: t('develop.code_gen.column.dict_not_found', { code: current }), value: current }, ...dictOptions.value]
}

async function loadColumns() {
  if (!props.tableId) {
    return
  }
  loading.value = true
  try {
    rows.value = await codeGenTableColumnApi.getByTable(props.tableId)
    // 已配了关联的列，把关联表的列先取回来，显示列下拉才有候选
    for (const row of rows.value) {
      void ensureRelationColumns(row.relationTableId)
    }
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('develop.code_gen.column.load_failed'))
    rows.value = []
  }
  finally {
    loading.value = false
  }
}

type BooleanColumnField = 'isRequired' | 'isUnique' | 'isList' | 'isInsert' | 'isEdit' | 'isQuery'

/** 勾选列的表头文案键：单元格里的复选框没有可见文字，可及名取「表头 · 列名」 */
const BOOLEAN_COLUMN_TITLE_KEYS: Record<BooleanColumnField, string> = {
  isRequired: 'develop.code_gen.column.col_required',
  isUnique: 'develop.code_gen.column.col_unique',
  isList: 'develop.code_gen.column.col_list',
  isInsert: 'develop.code_gen.column.col_insert',
  isEdit: 'develop.code_gen.column.col_edit',
  isQuery: 'develop.code_gen.column.col_query',
}

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
    'aria-label': `${t(BOOLEAN_COLUMN_TITLE_KEYS[field])} · ${row.columnName}`,
    'onUpdate:checked': (value: boolean) => {
      row[field] = value
    },
  })
}

/** 字典取值列：按 dictSelectorType 渲染字典码 / 枚举全名 / 常量 JSON（互斥，仅生效项可编辑） */
function renderDictValue(row: CodeGenTableColumnListItemDto) {
  if (row.dictSelectorType === DictSelectorType.DictSelector) {
    return h(XSelect, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.dictCode || null,
      'options': dictOptionsFor(row),
      'clearable': true,
      'placeholder': t('develop.code_gen.column.col_dict_code_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        row.dictCode = (raw as string | null) ?? null
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
  if (row.dictSelectorType === DictSelectorType.TableSelector || row.dictSelectorType === DictSelectorType.TreeSelector) {
    const isTree = row.dictSelectorType === DictSelectorType.TreeSelector
    return h('div', { class: 'relation-cell' }, [
      h(XSelect, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.relationTableId ?? null,
        'options': relationTableOptionsFor(row),
        'clearable': true,
        'placeholder': t('develop.code_gen.column.relation_table_placeholder'),
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          row.relationTableId = raw == null ? null : String(raw)
          // 换了关联的表，原显示列不再成立
          row.relationLabelColumn = null
          void ensureRelationColumns(row.relationTableId)
        },
      }),
      h(XSelect, {
        'size': 'sm',
        'disabled': isLocked(row) || !row.relationTableId,
        'value': row.relationLabelColumn || null,
        'options': relationLabelOptionsFor(row),
        'clearable': true,
        'placeholder': t(isTree ? 'develop.code_gen.column.relation_label_tree_placeholder' : 'develop.code_gen.column.relation_label_placeholder'),
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          row.relationLabelColumn = (raw as string | null) ?? null
        },
      }),
    ])
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
          ? h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => t('develop.code_gen.column.base_column')))
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
  { key: 'isUnique', title: t('develop.code_gen.column.col_unique'), width: 60, align: 'center', render: (row: CodeGenTableColumnListItemDto) => renderCheckbox(row, 'isUnique') },
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
          // 切换选项来源时清空其它取值，保持互斥
          row.dictSelectorType = value
          row.dictCode = null
          row.enumTypeName = null
          row.constValues = null
          row.relationTableId = null
          row.relationLabelColumn = null
        },
      }),
  },
  {
    key: 'dictValue',
    title: t('develop.code_gen.column.col_dict_value'),
    width: 320,
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
      isUnique: row.isUnique,
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
      relationTableId: row.relationTableId ?? null,
      relationLabelColumn: row.relationLabelColumn ?? null,
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
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
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
    />
  </XEditModal>
</template>

<style scoped>
/* 关联表与显示列两个下拉并排；列 render 的 h() 不带页面 scope，样式要穿透 */
:deep(.relation-cell) {
  display: flex;
  gap: var(--xh-space-1);
  min-width: 0;
}

:deep(.relation-cell > *) {
  flex: 1;
  min-width: 0;
}

:deep(.col-name) {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}
</style>
