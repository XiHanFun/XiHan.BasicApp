<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysTenant } from '~/api'
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
import { tenantApi } from '@/api'
import { STATUS_OPTIONS, TENANT_ISOLATION_MODE_OPTIONS, TENANT_STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemTenantPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return tenantApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysTenant>({
  id: 'sys_tenant',
  name: '租户管理',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'tenantName', title: '租户名称', minWidth: 160, showOverflow: 'tooltip', sortable: true },
    { field: 'tenantCode', title: '租户编码', minWidth: 130, showOverflow: 'tooltip' },
    { field: 'contactPerson', title: '联系人', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'contactPhone', title: '联系电话', minWidth: 130, showOverflow: 'tooltip' },
    { field: 'contactEmail', title: '联系邮箱', minWidth: 180, showOverflow: 'tooltip' },
    { field: 'domain', title: '域名', minWidth: 160, showOverflow: 'tooltip' },
    {
      field: 'isolationMode',
      title: '隔离模式',
      width: 120,
      formatter: ({ cellValue }) => getOptionLabel(TENANT_ISOLATION_MODE_OPTIONS, cellValue),
    },
    {
      field: 'tenantStatus',
      title: '租户状态',
      width: 100,
      slots: { default: 'col_tenantStatus' },
    },
    { field: 'userLimit', title: '用户上限', width: 90 },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    { field: 'expireTime', title: '到期时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue) },
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
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增租户')
const submitLoading = ref(false)
const formData = ref<Partial<SysTenant>>({})

function resetForm() {
  formData.value = {
    tenantName: '',
    tenantCode: '',
    contactPerson: '',
    contactPhone: '',
    contactEmail: '',
    isolationMode: 0,
    status: 1,
    remark: '',
  }
}

function handleAdd() {
  modalTitle.value = '新增租户'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysTenant) {
  modalTitle.value = '编辑租户'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await tenantApi.delete(id)
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
      await tenantApi.update(formData.value.basicId, formData.value)
    }
    else {
      await tenantApi.create(formData.value)
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
        <vxe-input v-model="queryParams.keyword" placeholder="搜索租户名称/编码/联系人" clearable style="width: 280px" @keyup.enter="handleSearch" />
        <NSelect v-model:value="queryParams.status" :options="STATUS_OPTIONS" placeholder="状态" clearable style="width: 120px" />
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
            新增租户
          </NButton>
        </template>
        <template #col_tenantStatus="{ row }">
          <NTag :type="row.tenantStatus === 0 ? 'success' : row.tenantStatus === 3 ? 'error' : 'warning'" size="small">
            {{ getOptionLabel(TENANT_STATUS_OPTIONS, row.tenantStatus) }}
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
              确认删除该租户？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 560px" :auto-focus="false">
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="租户名称" path="tenantName">
          <NInput v-model:value="formData.tenantName" placeholder="请输入租户名称" />
        </NFormItem>
        <NFormItem label="租户编码" path="tenantCode">
          <NInput v-model:value="formData.tenantCode" placeholder="如: tenant_001" />
        </NFormItem>
        <NFormItem label="联系人" path="contactPerson">
          <NInput v-model:value="formData.contactPerson" placeholder="联系人姓名" />
        </NFormItem>
        <NFormItem label="联系电话" path="contactPhone">
          <NInput v-model:value="formData.contactPhone" placeholder="联系电话" />
        </NFormItem>
        <NFormItem label="联系邮箱" path="contactEmail">
          <NInput v-model:value="formData.contactEmail" placeholder="联系邮箱" />
        </NFormItem>
        <NFormItem label="隔离模式" path="isolationMode">
          <NSelect v-model:value="formData.isolationMode" :options="TENANT_ISOLATION_MODE_OPTIONS" />
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
