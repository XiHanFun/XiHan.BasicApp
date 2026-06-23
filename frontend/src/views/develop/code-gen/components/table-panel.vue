<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  ApiId,
  CodeGenTableListItemDto,
  GenStatus,
  TemplateType,
} from '@/api'
import {
  NButton,
  NDataTable,
  NIcon,
  NInput,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTableApi,
  createPageRequest,
  EnableStatus,
  GEN_STATUS_OPTIONS,
  GenStatus as GenStatusEnum,
  TEMPLATE_TYPE_OPTIONS,
} from '@/api'
import { Icon } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { getOptionLabel } from '~/utils'
import ColumnConfigModal from './column-config-modal.vue'
import GenerateModal from './generate-modal.vue'
import ImportTableModal from './import-table-modal.vue'
import RuntimeDataModal from './runtime-data-modal.vue'
import TableEditModal from './table-edit-modal.vue'

defineOptions({ name: 'CodeGenTablePanel' })

const { t } = useI18n()
const message = useMessage()

const loading = ref(false)
const list = ref<CodeGenTableListItemDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const queryParams = reactive({
  keyword: '',
  templateType: null as TemplateType | null,
  genStatus: null as GenStatus | null,
  status: null as EnableStatus | null,
})

const importVisible = ref(false)
const editVisible = ref(false)
const columnVisible = ref(false)
const generateVisible = ref(false)
const runtimeVisible = ref(false)
const currentTableId = ref<ApiId | null>(null)
const currentTableName = ref('')

async function fetchData() {
  loading.value = true
  try {
    const result = await codeGenTableApi.page({
      ...createPageRequest({ page: { pageIndex: page.value, pageSize: pageSize.value } }),
      genStatus: queryParams.genStatus ?? undefined,
      keyword: queryParams.keyword?.trim() || undefined,
      status: queryParams.status ?? undefined,
      templateType: queryParams.templateType ?? undefined,
    })
    list.value = result.items
    total.value = result.page.totalCount
  }
  catch {
    message.error(t('develop.code_gen.table.query_failed'))
    list.value = []
    total.value = 0
  }
  finally {
    loading.value = false
  }
}

function handleSearch() {
  page.value = 1
  fetchData()
}

function handlePageChange(value: number) {
  page.value = value
  fetchData()
}

function handlePageSizeChange(value: number) {
  pageSize.value = value
  page.value = 1
  fetchData()
}

function genStatusTagType(status: GenStatus) {
  if (status === GenStatusEnum.Generated) {
    return 'success'
  }
  if (status === GenStatusEnum.Failed) {
    return 'error'
  }
  return 'default'
}

const columns = computed<DataTableColumns<CodeGenTableListItemDto>>(() => [
  { key: 'tableName', title: t('develop.code_gen.table.col_table_name'), minWidth: 170, fixed: 'left', ellipsis: { tooltip: true } },
  { key: 'className', title: t('develop.code_gen.table.col_class_name'), minWidth: 150, ellipsis: { tooltip: true } },
  { key: 'tableComment', title: t('develop.code_gen.table.col_table_comment'), minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'moduleName', title: t('develop.code_gen.table.col_module'), width: 120, ellipsis: { tooltip: true } },
  {
    key: 'templateType',
    title: t('develop.code_gen.table.col_template_type'),
    width: 100,
    render: (row: CodeGenTableListItemDto) => getOptionLabel(TEMPLATE_TYPE_OPTIONS, row.templateType),
  },
  {
    key: 'genStatus',
    title: t('develop.code_gen.table.col_gen_status'),
    width: 100,
    align: 'center',
    render: (row: CodeGenTableListItemDto) =>
      h(NTag, {
        size: 'small',
        round: true,
        bordered: false,
        type: genStatusTagType(row.genStatus),
      }, () => getOptionLabel(GEN_STATUS_OPTIONS, row.genStatus)),
  },
  {
    key: 'status',
    title: t('develop.code_gen.table.col_status'),
    width: 72,
    align: 'center',
    render: (row: CodeGenTableListItemDto) =>
      h(NTag, {
        size: 'small',
        round: true,
        bordered: false,
        type: row.status === EnableStatus.Enabled ? 'success' : 'error',
      }, () => getOptionLabel(STATUS_OPTIONS, row.status)),
  },
  { key: 'lastGenTime', title: t('develop.code_gen.table.col_last_gen'), minWidth: 170 },
  {
    key: 'actions',
    title: t('develop.code_gen.common.actions'),
    width: 288,
    fixed: 'right',
    align: 'center',
    render: (row: CodeGenTableListItemDto) =>
      h(NSpace, { size: 4, justify: 'center', wrap: false }, () => [
        h(NButton, {
          circle: true,
          quaternary: true,
          size: 'small',
          title: t('develop.code_gen.table.action_runtime'),
          onClick: () => handleRuntime(row),
        }, {
          icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:database' })),
        }),
        h(NButton, {
          quaternary: true,
          size: 'small',
          type: 'primary',
          onClick: () => handleGenerate(row),
        }, () => t('develop.code_gen.table.action_generate')),
        h(NButton, {
          quaternary: true,
          size: 'small',
          onClick: () => handleColumns(row),
        }, () => t('develop.code_gen.table.action_columns')),
        h(NButton, {
          quaternary: true,
          size: 'small',
          onClick: () => handleEdit(row),
        }, () => t('develop.code_gen.common.edit')),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () => h(NButton, {
            quaternary: true,
            size: 'small',
            type: 'error',
          }, () => t('develop.code_gen.common.delete')),
          default: () => t('develop.code_gen.table.confirm_delete'),
        }),
      ]),
  },
])

function handleImport() {
  importVisible.value = true
}

function handleEdit(row: CodeGenTableListItemDto) {
  currentTableId.value = row.basicId
  editVisible.value = true
}

function handleColumns(row: CodeGenTableListItemDto) {
  currentTableId.value = row.basicId
  columnVisible.value = true
}

function handleGenerate(row: CodeGenTableListItemDto) {
  currentTableId.value = row.basicId
  currentTableName.value = row.tableName
  generateVisible.value = true
}

function handleRuntime(row: CodeGenTableListItemDto) {
  currentTableId.value = row.basicId
  currentTableName.value = row.tableName
  runtimeVisible.value = true
}

async function handleDelete(row: CodeGenTableListItemDto) {
  try {
    await codeGenTableApi.delete(row.basicId)
    message.success(t('develop.code_gen.common.delete_success'))
    fetchData()
  }
  catch {
    message.error(t('develop.code_gen.common.delete_failed'))
  }
}

onMounted(fetchData)
</script>

<template>
  <div class="panel">
    <div class="panel__toolbar">
      <NInput
        v-model:value="queryParams.keyword"
        class="panel__kw"
        clearable
        :placeholder="t('develop.code_gen.table.search_placeholder')"
        size="small"
        @clear="handleSearch"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.templateType"
        class="panel__filter"
        clearable
        :options="TEMPLATE_TYPE_OPTIONS"
        :placeholder="t('develop.code_gen.table.filter_template_type')"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.genStatus"
        class="panel__filter"
        clearable
        :options="GEN_STATUS_OPTIONS"
        :placeholder="t('develop.code_gen.table.filter_gen_status')"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.status"
        class="panel__filter"
        clearable
        :options="STATUS_OPTIONS"
        :placeholder="t('develop.code_gen.table.filter_status')"
        size="small"
        @update:value="handleSearch"
      />
      <NButton size="small" type="primary" @click="handleSearch">
        {{ t('develop.code_gen.common.search') }}
      </NButton>
      <NButton class="panel__add" size="small" type="primary" @click="handleImport">
        <template #icon>
          <NIcon><Icon icon="lucide:database" /></NIcon>
        </template>
        {{ t('develop.code_gen.table.import') }}
      </NButton>
    </div>

    <div class="panel__body">
      <NDataTable
        class="panel__table"
        flex-height
        :columns="columns"
        :data="list"
        :loading="loading"
        :row-key="(row: CodeGenTableListItemDto) => row.basicId"
        :scroll-x="1448"
        size="small"
      />
    </div>

    <div class="panel__foot">
      <NPagination
        v-model:page="page"
        v-model:page-size="pageSize"
        :item-count="total"
        :page-sizes="[10, 20, 50, 100]"
        show-size-picker
        @update:page="handlePageChange"
        @update:page-size="handlePageSizeChange"
      />
    </div>

    <ImportTableModal v-model:show="importVisible" @imported="fetchData" />
    <TableEditModal v-model:show="editVisible" :table-id="currentTableId" @saved="fetchData" />
    <ColumnConfigModal v-model:show="columnVisible" :table-id="currentTableId" @saved="fetchData" />
    <GenerateModal
      v-model:show="generateVisible"
      :table-id="currentTableId"
      :table-name="currentTableName"
      @generated="fetchData"
    />
    <RuntimeDataModal
      v-model:show="runtimeVisible"
      :table-id="currentTableId"
      :table-name="currentTableName"
    />
  </div>
</template>

<style scoped>
.panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.panel__toolbar {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding-bottom: 10px;
}

.panel__kw {
  width: 220px;
}

.panel__filter {
  width: 130px;
  flex-shrink: 0;
}

.panel__add {
  margin-left: auto;
}

.panel__body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.panel__table {
  flex: 1;
  min-height: 0;
}

.panel__foot {
  display: flex;
  flex-shrink: 0;
  justify-content: flex-end;
  padding-top: 10px;
}
</style>
