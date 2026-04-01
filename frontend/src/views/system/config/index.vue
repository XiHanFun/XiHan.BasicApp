<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysConfig } from '~/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { configApi } from '@/api'
import {
  CONFIG_DATA_TYPE_OPTIONS,
  CONFIG_TYPE_OPTIONS,
  STATUS_OPTIONS,
} from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemConfigPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  configType: undefined as number | undefined,
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return configApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    configType: queryParams.configType,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysConfig>({
  id: 'sys_config',
  name: '系统配置',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'configName', title: '配置名称', minWidth: 160, showOverflow: 'tooltip', sortable: true },
    { field: 'configGroup', title: '配置组', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'configKey', title: '配置键', minWidth: 200, showOverflow: 'tooltip', sortable: true },
    { field: 'configValue', title: '配置值', minWidth: 220, showOverflow: 'tooltip' },
    { field: 'defaultValue', title: '默认值', minWidth: 140, showOverflow: 'tooltip' },
    {
      field: 'configType',
      title: '配置类型',
      width: 120,
      formatter: ({ cellValue }) => getOptionLabel(CONFIG_TYPE_OPTIONS, cellValue),
    },
    {
      field: 'dataType',
      title: '数据类型',
      width: 120,
      formatter: ({ cellValue }) => getOptionLabel(CONFIG_DATA_TYPE_OPTIONS, cellValue),
    },
    { field: 'configDescription', title: '描述', minWidth: 160, showOverflow: 'tooltip' },
    {
      field: 'isBuiltIn',
      title: '内置',
      width: 70,
      slots: { default: 'col_builtIn' },
    },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    { field: 'createTime', title: '创建时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      title: '操作',
      width: 140,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  proxyConfig: {
    autoLoad: true,
    ajax: {
      query: ({ page }) => handleQueryApi(page),
    },
  },
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.configType = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

// ==================== 表单弹窗 ====================

const modalVisible = ref(false)
const modalTitle = ref('新增配置')
const submitLoading = ref(false)
const formData = ref<Partial<SysConfig>>({})

function resetForm() {
  formData.value = {
    configName: '',
    configKey: '',
    configValue: '',
    configType: 0,
    dataType: 0,
    status: 1,
    remark: '',
  }
}

function handleAdd() {
  modalTitle.value = '新增配置'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysConfig) {
  modalTitle.value = '编辑配置'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await configApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await configApi.update(formData.value.basicId, formData.value)
    }
    else {
      await configApi.create(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="h-full flex flex-col">
    <vxe-card class="mb-2" style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索配置名称/配置键"
          clearable
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.configType"
          :options="CONFIG_TYPE_OPTIONS"
          placeholder="配置类型"
          clearable
          style="width: 140px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          clearable
          style="width: 120px"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">
            新增配置
          </NButton>
        </template>
        <template #col_builtIn="{ row }">
          <NTag :type="row.isBuiltIn ? 'info' : 'default'" size="small">
            {{ row.isBuiltIn ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该配置？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

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
          <NInput v-model:value="formData.configValue" type="textarea" :rows="3" placeholder="请输入配置值" />
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
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="formData.remark" type="textarea" :rows="2" placeholder="备注" />
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
