<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysConfig } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { createConfigApi, deleteConfigApi, getConfigPageApi, updateConfigApi } from '~/api'
import {
  CONFIG_DATA_TYPE_OPTIONS,
  CONFIG_TYPE_OPTIONS,
  DEFAULT_PAGE_SIZE,
  STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemConfigPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysConfig[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  configType: undefined as number | undefined,
  status: undefined as number | undefined,
})

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
    const result = await getConfigPageApi(queryParams)
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
    if (formData.value.id) {
      await updateConfigApi(formData.value.id, formData.value)
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
                onPositiveClick: () => handleDelete(row.id),
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
    <NCard :bordered="false">
      <div class="flex flex-wrap items-center gap-3">
        <NInput
          v-model:value="queryParams.keyword"
          placeholder="搜索配置名称/配置键"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.configType"
          :options="CONFIG_TYPE_OPTIONS"
          placeholder="配置类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          style="width: 120px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.configType = undefined
              queryParams.status = undefined
              queryParams.page = 1
              fetchData()
            }
          "
        >
          重置
        </NButton>
        <NButton class="ml-auto" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增配置
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.id"
        :pagination="false"
        :scroll-x="1300"
        size="small"
        striped
      />
      <div class="mt-4 flex justify-end">
        <NPagination
          v-model:page="queryParams.page"
          v-model:page-size="queryParams.pageSize"
          :item-count="total"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="fetchData"
          @update:page-size="
            () => {
              queryParams.page = 1
              fetchData()
            }
          "
        />
      </div>
    </NCard>

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
          <NSelect v-model:value="formData.configType" :options="CONFIG_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="数据类型" path="dataType">
          <NSelect v-model:value="formData.dataType" :options="CONFIG_DATA_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
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
