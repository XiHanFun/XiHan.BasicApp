<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { CrudSearchField } from '~/components'
import type { SysConfig } from '~/types'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPopconfirm,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { createConfigApi, deleteConfigApi, getConfigPageApi, updateConfigApi } from '@/api'
import { CrudEnumSelect, CrudProTable, CrudQueryPanel } from '~/components'
import {
  CONFIG_DATA_TYPE_OPTIONS,
  CONFIG_TYPE_OPTIONS,
  DEFAULT_PAGE_SIZE,
  STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemConfigPage' })

interface QueryFormModel {
  keyword: string
  configType?: number
  status?: number
}

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysConfig[]>([])
const total = ref(0)

const queryForm = ref<QueryFormModel>({
  keyword: '',
  configType: undefined,
  status: undefined,
})

const pager = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
})

const searchFields: CrudSearchField[] = [
  {
    key: 'keyword',
    type: 'input',
    placeholder: '搜索配置名称/配置键',
    width: 260,
  },
  {
    key: 'configType',
    type: 'select',
    options: CONFIG_TYPE_OPTIONS,
    placeholder: '配置类型',
    width: 140,
  },
  {
    key: 'status',
    type: 'select',
    options: STATUS_OPTIONS,
    placeholder: '状态',
    width: 120,
  },
]

const modalVisible = ref(false)
const modalTitle = ref('新增配置')
const submitLoading = ref(false)

const formData = ref<Partial<SysConfig>>({
  configName: '',
  configKey: '',
  configValue: '',
  configType: 0,
  dataType: 0,
  status: 1,
  remark: '',
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getConfigPageApi({
      page: pager.page,
      pageSize: pager.pageSize,
      ...queryForm.value,
    })
    tableData.value = result.items
    total.value = result.total
  }
  catch {
    message.error('获取配置列表失败')
  }
  finally {
    loading.value = false
  }
}

function handleSearch() {
  pager.page = 1
  fetchData()
}

function handleReset() {
  pager.page = 1
  fetchData()
}

function handlePageChange(page: number) {
  pager.page = page
  fetchData()
}

function handlePageSizeChange(pageSize: number) {
  pager.page = 1
  pager.pageSize = pageSize
  fetchData()
}

function handleAdd() {
  modalTitle.value = '新增配置'
  formData.value = {
    configName: '',
    configKey: '',
    configValue: '',
    configType: 0,
    dataType: 0,
    status: 1,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysConfig) {
  modalTitle.value = '编辑配置'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteConfigApi(id)
    message.success('删除成功')
    fetchData()
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateConfigApi(formData.value.basicId, formData.value)
    }
    else {
      await createConfigApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}

const columns: DataTableColumns<SysConfig> = [
  {
    title: '配置名称',
    key: 'configName',
    width: 180,
  },
  {
    title: '配置键',
    key: 'configKey',
    width: 210,
    render: row =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.configKey }),
  },
  {
    title: '配置值',
    key: 'configValue',
    minWidth: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '配置类型',
    key: 'configType',
    width: 120,
    render: row => getOptionLabel(CONFIG_TYPE_OPTIONS, row.configType),
  },
  {
    title: '数据类型',
    key: 'dataType',
    width: 120,
    render: row => getOptionLabel(CONFIG_DATA_TYPE_OPTIONS, row.dataType),
  },
  {
    title: '状态',
    key: 'status',
    width: 100,
    render: row =>
      h(
        NTag,
        { type: row.status === 1 ? 'success' : 'error', size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: row => formatDate(row.createTime ?? ''),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    fixed: 'right',
    render: row =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () => [
            h(
              NButton,
              {
                size: 'small',
                type: 'primary',
                ghost: true,
                onClick: () => handleEdit(row),
              },
              { default: () => '编辑' },
            ),
            h(
              NPopconfirm,
              {
                onPositiveClick: () => handleDelete(row.basicId),
              },
              {
                default: () => '确认删除该配置？',
                trigger: () =>
                  h(
                    NButton,
                    { size: 'small', type: 'error', ghost: true },
                    { default: () => '删除' },
                  ),
              },
            ),
          ],
        },
      ),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <CrudQueryPanel
      v-model="queryForm"
      title="配置查询"
      :fields="searchFields"
      @search="handleSearch"
      @reset="handleReset"
    >
      <template #actions>
        <NButton type="primary" @click="handleAdd">
          新增配置
        </NButton>
      </template>
    </CrudQueryPanel>

    <CrudProTable
      storage-key="system_config_table_columns"
      :columns="columns"
      :data="tableData"
      :loading="loading"
      :row-key="(row) => row.basicId"
      :pagination="{ page: pager.page, pageSize: pager.pageSize, total }"
      :scroll-x="1300"
      max-height="calc(100vh - 330px)"
      @refresh="fetchData"
      @update:page="handlePageChange"
      @update:page-size="handlePageSizeChange"
    >
      <template #toolbar-left>
        <span class="text-xs text-gray-400">共 {{ total }} 条</span>
      </template>
    </CrudProTable>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 620px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="配置名称" path="configName">
          <NInput v-model:value="formData.configName" placeholder="请输入配置名称" />
        </NFormItem>
        <NFormItem label="配置键" path="configKey">
          <NInput v-model:value="formData.configKey" placeholder="如: system.theme.color" />
        </NFormItem>
        <NFormItem label="配置值" path="configValue">
          <NInput
            v-model:value="formData.configValue"
            type="textarea"
            :rows="3"
            placeholder="请输入配置值"
          />
        </NFormItem>
        <NFormItem label="配置类型" path="configType">
          <CrudEnumSelect v-model="formData.configType" :options="CONFIG_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="数据类型" path="dataType">
          <CrudEnumSelect v-model="formData.dataType" :options="CONFIG_DATA_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <CrudEnumSelect v-model="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
