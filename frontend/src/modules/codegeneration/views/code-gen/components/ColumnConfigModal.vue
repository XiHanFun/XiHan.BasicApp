<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  代码生成 / 列配置弹窗
  整体保存一张表的列配置：基类的列（主键、租户、审计、软删与并发戳）不可编辑；选项来源切换时清掉别的来源的配置
  列注释取自库结构（同步结构以库为准），只显示不编辑
-->
<script setup lang="ts">
import type { ColumnRow, DictSelectorType, HtmlType, QueryType, TableRow } from './use-tables'
import type { XDataTableColumn } from '~/components'
import type { SelectOption } from '~/types'
import { XhCheckbox, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDataTable, XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { codegenTableColumns, codegenTables, dicts } from '~/contracts'
import { DICT_SELECTOR_TYPES, enumKey, HTML_TYPES, QUERY_TYPES } from './use-tables'

defineOptions({ name: 'CodegenColumnConfigModal' })

const props = defineProps<{
  show: boolean
  tableId: null | string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'saved': []
}>()

const { t } = useI18n()

const loading = ref(false)
const submitLoading = ref(false)
const rows = ref<ColumnRow[]>([])

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

function options<T extends string>(group: string, values: readonly T[]): SelectOption[] {
  return values.map(value => ({
    label: t(enumKey(group, value)),
    value,
  }))
}

const queryTypeOptions = computed(() => options('query_type', QUERY_TYPES))
const htmlTypeOptions = computed(() => options('html_type', HTML_TYPES))
const dictSelectorOptions = computed(() => options('dict_selector', DICT_SELECTOR_TYPES))

/** 系统字典的候选（按编码存，下拉显示「名称（编码）」） */
const dictOptions = ref<SelectOption[]>([])

async function loadDicts() {
  try {
    // 字典是配置型数据，一页取完（页大小上限 500）
    const result = await dicts.list({
      page: { index: 1, size: 500 },
      filters: [],
      sorts: [],
      keyword: null,
    })
    dictOptions.value = result.items.map(dict => ({ label: `${dict.name}（${dict.code}）`, value: dict.code }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.column.load_dicts_failed'))
    dictOptions.value = []
  }
}

/** 关联的候选表（全部表配置，一次取全） */
const tables = ref<TableRow[]>([])

async function loadTables() {
  try {
    tables.value = (await codegenTables.options({})).items
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.column.load_tables_failed'))
    tables.value = []
  }
}

/** 关联表的候选：关联树只能选树表；已选的表不在列表里时也列出来并标明 */
function relationTableOptionsFor(row: ColumnRow): SelectOption[] {
  const candidates = tables.value
    .filter(table => row.dictSelectorType !== 'TreeSelector' || table.templateType === 'Tree')
    .map(table => ({
      label: table.tableComment
        ? `${table.tableComment}（${table.tableName}）`
        : table.tableName,
      value: table.id,
    }))
  const current = row.relationTableId
  if (!current || candidates.some(option => option.value === current))
    return candidates
  return [
    {
      label: t('codegeneration.code_gen.column.relation_table_not_found', { id: current }),
      value: current,
    },
    ...candidates,
  ]
}

/** 关联表的列（显示列的候选），按表缓存 */
const relationColumns = reactive(new Map<string, ColumnRow[]>())

async function ensureRelationColumns(tableId: null | string | undefined) {
  if (!tableId || relationColumns.has(tableId))
    return
  relationColumns.set(tableId, [])
  try {
    relationColumns.set(tableId, (await codegenTableColumns.list({ tableId })).items)
  }
  catch (error) {
    relationColumns.delete(tableId)
    toast.danger((error as Error).message || t('codegeneration.code_gen.column.load_failed'))
  }
}

/** 显示列的候选：关联表的字符业务列；已选的列不在其中时也列出来 */
function relationLabelOptionsFor(row: ColumnRow): SelectOption[] {
  const candidates = (row.relationTableId
    ? relationColumns.get(row.relationTableId) ?? []
    : [])
    .filter(column => !column.isBase && column.cSharpType?.replace('?', '') === 'string')
    .map(column => ({
      label: column.columnComment
        ? `${column.columnComment}（${column.columnName}）`
        : column.columnName,
      value: column.columnName,
    }))
  const current = row.relationLabelColumn
  if (!current || candidates.some(option => option.value === current))
    return candidates
  return [
    { label: current, value: current },
    ...candidates,
  ]
}

/**
 * 本列可选的字典
 * 已填的编码不在列表里（字典被删或写错）时也列出并标明，免得下拉显示成空
 */
function dictOptionsFor(row: ColumnRow): SelectOption[] {
  const current = row.dictCode?.trim()
  if (!current || dictOptions.value.some(option => option.value === current))
    return dictOptions.value
  return [
    {
      label: t('codegeneration.code_gen.column.dict_not_found', { code: current }),
      value: current,
    },
    ...dictOptions.value,
  ]
}

async function loadColumns() {
  if (!props.tableId)
    return
  loading.value = true
  try {
    rows.value = (await codegenTableColumns.list({ tableId: props.tableId })).items
    // 已配了关联的列，先把关联表的列取回来，显示列下拉才有候选
    for (const row of rows.value)
      void ensureRelationColumns(row.relationTableId)
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.column.load_failed'))
    rows.value = []
  }
  finally {
    loading.value = false
  }
}

type BooleanField = 'isRequired' | 'isUnique' | 'isList' | 'isInsert' | 'isEdit' | 'isQuery'

/** 勾选列的表头文案：单元格里的复选框没有可见文字，可及名取「表头 · 列名」 */
const BOOLEAN_TITLES: Record<BooleanField, string> = {
  isRequired: 'codegeneration.code_gen.column.col_required',
  isUnique: 'codegeneration.code_gen.column.col_unique',
  isList: 'codegeneration.code_gen.column.col_list',
  isInsert: 'codegeneration.code_gen.column.col_insert',
  isEdit: 'codegeneration.code_gen.column.col_edit',
  isQuery: 'codegeneration.code_gen.column.col_query',
}

/**
 * 基类的列不可编辑
 * 它们不进任何产物，放开编辑只会让人以为配置生效了；判定由后端给出，与模板同源
 */
function isLocked(row: ColumnRow): boolean {
  return row.isBase
}

function renderCheckbox(row: ColumnRow, field: BooleanField) {
  return h(XhCheckbox, {
    'checked': row[field],
    'disabled': isLocked(row),
    'aria-label': `${t(BOOLEAN_TITLES[field])} · ${row.columnName}`,
    'onUpdate:checked': (value: boolean) => {
      row[field] = value
    },
  })
}

/**
 * 按选项来源渲染字典码、枚举全名、常量 JSON 或关联表
 * 几种来源互斥，只编辑生效的那个
 */
function renderSelectorValue(row: ColumnRow) {
  if (row.dictSelectorType === 'DictSelector') {
    return h(XSelect, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.dictCode || null,
      'options': dictOptionsFor(row),
      'clearable': true,
      'placeholder': t('codegeneration.code_gen.column.col_dict_code_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        row.dictCode = (raw as null | string) ?? null
      },
    })
  }
  if (row.dictSelectorType === 'EnumSelector') {
    return h(XInput, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.enumTypeName ?? '',
      'placeholder': t('codegeneration.code_gen.column.col_enum_type_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        row.enumTypeName = raw as string
      },
    })
  }
  if (row.dictSelectorType === 'TableSelector' || row.dictSelectorType === 'TreeSelector') {
    const isTree = row.dictSelectorType === 'TreeSelector'
    return h('div', { class: 'relation-cell' }, [
      h(XSelect, {
        'size': 'sm',
        'disabled': isLocked(row),
        'value': row.relationTableId ?? null,
        'options': relationTableOptionsFor(row),
        'clearable': true,
        'placeholder': t('codegeneration.code_gen.column.relation_table_placeholder'),
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          row.relationTableId = raw == null
            ? null
            : String(raw)
          // 换了关联的表，原来的显示列不再成立
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
        'placeholder': isTree
          ? t('codegeneration.code_gen.column.relation_label_tree_placeholder')
          : t('codegeneration.code_gen.column.relation_label_placeholder'),
        'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
          row.relationLabelColumn = (raw as null | string) ?? null
        },
      }),
    ])
  }
  if (row.dictSelectorType === 'ConstSelector') {
    return h(XInput, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.constValues ?? '',
      'placeholder': t('codegeneration.code_gen.column.col_const_values_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        row.constValues = raw as string
      },
    })
  }
  return h('span', { class: 'empty-cell' }, '—')
}

function textInput(row: ColumnRow, field: 'cSharpType' | 'cSharpProperty') {
  return h(XInput, {
    'size': 'sm',
    'disabled': isLocked(row),
    'value': row[field] ?? '',
    'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
      row[field] = raw as string
    },
  })
}

const columns = computed<XDataTableColumn<ColumnRow>[]>(() => [
  {
    key: 'columnName',
    title: t('codegeneration.code_gen.column.col_column_name'),
    minWidth: 180,
    fixed: 'left',
    ellipsis: true,
    render: row => h('div', { class: 'col-name' }, [
      h('span', null, row.columnName),
      isLocked(row)
        ? h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => t('codegeneration.code_gen.column.base_column')))
        : null,
    ]),
  },
  {
    key: 'columnComment',
    title: t('codegeneration.code_gen.column.col_column_comment'),
    minWidth: 160,
    ellipsis: true,
  },
  {
    key: 'columnType',
    title: t('codegeneration.code_gen.column.col_column_type'),
    width: 110,
    ellipsis: true,
  },
  {
    key: 'cSharpType',
    title: t('codegeneration.code_gen.column.col_csharp_type'),
    width: 130,
    render: row => textInput(row, 'cSharpType'),
  },
  {
    key: 'cSharpProperty',
    title: t('codegeneration.code_gen.column.col_csharp_property'),
    width: 140,
    render: row => textInput(row, 'cSharpProperty'),
  },
  {
    key: 'isRequired',
    title: t('codegeneration.code_gen.column.col_required'),
    width: 60,
    align: 'center',
    render: row => renderCheckbox(row, 'isRequired'),
  },
  {
    key: 'isUnique',
    title: t('codegeneration.code_gen.column.col_unique'),
    width: 60,
    align: 'center',
    render: row => renderCheckbox(row, 'isUnique'),
  },
  {
    key: 'isList',
    title: t('codegeneration.code_gen.column.col_list'),
    width: 60,
    align: 'center',
    render: row => renderCheckbox(row, 'isList'),
  },
  {
    key: 'isInsert',
    title: t('codegeneration.code_gen.column.col_insert'),
    width: 60,
    align: 'center',
    render: row => renderCheckbox(row, 'isInsert'),
  },
  {
    key: 'isEdit',
    title: t('codegeneration.code_gen.column.col_edit'),
    width: 60,
    align: 'center',
    render: row => renderCheckbox(row, 'isEdit'),
  },
  {
    key: 'isQuery',
    title: t('codegeneration.code_gen.column.col_query'),
    width: 60,
    align: 'center',
    render: row => renderCheckbox(row, 'isQuery'),
  },
  {
    key: 'queryType',
    title: t('codegeneration.code_gen.column.col_query_type'),
    width: 130,
    render: row => h(XSelect, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.queryType,
      'options': queryTypeOptions.value,
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        row.queryType = raw as QueryType
      },
    }),
  },
  {
    key: 'htmlType',
    title: t('codegeneration.code_gen.column.col_html_type'),
    width: 150,
    render: row => h(XSelect, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.htmlType,
      'options': htmlTypeOptions.value,
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        row.htmlType = raw as HtmlType
      },
    }),
  },
  {
    key: 'dictSelectorType',
    title: t('codegeneration.code_gen.column.col_dict_selector'),
    width: 120,
    render: row => h(XSelect, {
      'size': 'sm',
      'disabled': isLocked(row),
      'value': row.dictSelectorType ?? null,
      'options': dictSelectorOptions.value,
      'clearable': true,
      'placeholder': t('codegeneration.code_gen.column.col_dict_selector_placeholder'),
      'onUpdate:value': (raw: string | number | (string | number)[] | null) => {
        // 换选项来源时清掉别的取值，保持互斥
        row.dictSelectorType = (raw as DictSelectorType | null) ?? null
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
    title: t('codegeneration.code_gen.column.col_dict_value'),
    width: 320,
    render: row => renderSelectorValue(row),
  },
])

async function handleSubmit() {
  if (!props.tableId)
    return
  submitLoading.value = true
  try {
    await codegenTableColumns.save({
      tableId: props.tableId,
      columns: rows.value.map(row => ({
        id: row.id,
        concurrencyStamp: row.concurrencyStamp,
        cSharpType: row.cSharpType ?? null,
        cSharpProperty: row.cSharpProperty ?? null,
        tsType: row.tsType ?? null,
        isRequired: row.isRequired,
        isUnique: row.isUnique,
        isList: row.isList,
        isInsert: row.isInsert,
        isEdit: row.isEdit,
        isQuery: row.isQuery,
        queryType: row.queryType,
        htmlType: row.htmlType,
        dictSelectorType: row.dictSelectorType ?? null,
        dictCode: row.dictCode ?? null,
        enumTypeName: row.enumTypeName ?? null,
        constValues: row.constValues ?? null,
        relationTableId: row.relationTableId ?? null,
        relationLabelColumn: row.relationLabelColumn ?? null,
        // 界面没有这几项的入口，原样交回，免得保存时把缺省值、正则与校验提示写成空
        defaultValue: row.defaultValue ?? null,
        minValue: row.minValue ?? null,
        maxValue: row.maxValue ?? null,
        regexPattern: row.regexPattern ?? null,
        validationMessage: row.validationMessage ?? null,
        order: row.order,
        status: row.status,
      })),
    })
    toast.success(t('common.messages.save_success'))
    emit('saved')
    emit('update:show', false)
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('codegeneration.code_gen.column.title')"
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
      :row-key="(row: ColumnRow) => row.id"
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
  gap: var(--xh-space-1);
  min-width: 0;
}

:deep(.empty-cell) {
  color: var(--xh-fg-muted);
}
</style>
