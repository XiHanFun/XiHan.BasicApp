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
import TableEditModal from './table-edit-modal.vue'

defineOptions({ name: 'CodeGenTablePanel' })

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
    message.error('查询表配置失败')
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
  { key: 'tableName', title: '表名', minWidth: 170, fixed: 'left', ellipsis: { tooltip: true } },
  { key: 'className', title: '实体类名', minWidth: 150, ellipsis: { tooltip: true } },
  { key: 'tableComment', title: '表说明', minWidth: 140, ellipsis: { tooltip: true } },
  { key: 'moduleName', title: '模块', width: 120, ellipsis: { tooltip: true } },
  {
    key: 'templateType',
    title: '模板类型',
    width: 100,
    render: (row: CodeGenTableListItemDto) => getOptionLabel(TEMPLATE_TYPE_OPTIONS, row.templateType),
  },
  {
    key: 'genStatus',
    title: '生成状态',
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
    title: '状态',
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
  { key: 'lastGenTime', title: '最近生成', minWidth: 170 },
  {
    key: 'actions',
    title: '操作',
    width: 240,
    fixed: 'right',
    align: 'center',
    render: (row: CodeGenTableListItemDto) =>
      h(NSpace, { size: 4, justify: 'center', wrap: false }, () => [
        h(NButton, {
          quaternary: true,
          size: 'small',
          type: 'primary',
          onClick: () => handleGenerate(row),
        }, () => '生成'),
        h(NButton, {
          quaternary: true,
          size: 'small',
          onClick: () => handleColumns(row),
        }, () => '列配置'),
        h(NButton, {
          quaternary: true,
          size: 'small',
          onClick: () => handleEdit(row),
        }, () => '编辑'),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () => h(NButton, {
            quaternary: true,
            size: 'small',
            type: 'error',
          }, () => '删除'),
          default: () => '确认删除该表配置？',
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

async function handleDelete(row: CodeGenTableListItemDto) {
  try {
    await codeGenTableApi.delete(row.basicId)
    message.success('删除成功')
    fetchData()
  }
  catch {
    message.error('删除失败')
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
        placeholder="搜索表名 / 类名 / 注释"
        size="small"
        @clear="handleSearch"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.templateType"
        class="panel__filter"
        clearable
        :options="TEMPLATE_TYPE_OPTIONS"
        placeholder="模板类型"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.genStatus"
        class="panel__filter"
        clearable
        :options="GEN_STATUS_OPTIONS"
        placeholder="生成状态"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.status"
        class="panel__filter"
        clearable
        :options="STATUS_OPTIONS"
        placeholder="状态"
        size="small"
        @update:value="handleSearch"
      />
      <NButton size="small" type="primary" @click="handleSearch">
        查询
      </NButton>
      <NButton class="panel__add" size="small" type="primary" @click="handleImport">
        <template #icon>
          <NIcon><Icon icon="lucide:database" /></NIcon>
        </template>
        导入数据库表
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
        :scroll-x="1400"
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
