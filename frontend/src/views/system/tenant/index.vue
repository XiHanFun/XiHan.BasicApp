<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysTenant } from '~/types'
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
import {
  changeTenantStatusApi,
  createTenantApi,
  deleteTenantApi,
  getTenantPageApi,
  updateTenantApi,
} from '~/api'
import {
  DEFAULT_PAGE_SIZE,
  STATUS_OPTIONS,
  TENANT_ISOLATION_MODE_OPTIONS,
  TENANT_STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel, getStatusType } from '~/utils'

defineOptions({ name: 'SystemTenantPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysTenant[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  status: undefined as number | undefined,
  tenantStatus: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增租户')
const submitLoading = ref(false)

const formData = ref<Partial<SysTenant>>({
  tenantCode: '',
  tenantName: '',
  tenantShortName: '',
  contactPerson: '',
  contactPhone: '',
  contactEmail: '',
  isolationMode: 0,
  tenantStatus: 0,
  status: 1,
  expireTime: '',
  remark: '',
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getTenantPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  }
  catch {
    message.error('获取租户列表失败')
  }
  finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增租户'
  formData.value = {
    tenantCode: '',
    tenantName: '',
    tenantShortName: '',
    contactPerson: '',
    contactPhone: '',
    contactEmail: '',
    isolationMode: 0,
    tenantStatus: 0,
    status: 1,
    expireTime: '',
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysTenant) {
  modalTitle.value = '编辑租户'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteTenantApi(id)
    message.success('删除成功')
    fetchData()
  }
  catch {
    message.error('删除失败')
  }
}

async function handleChangeTenantStatus(row: SysTenant) {
  const nextStatus = row.tenantStatus === 0 ? 1 : 0
  try {
    await changeTenantStatusApi(row.basicId, nextStatus)
    row.tenantStatus = nextStatus
    message.success('租户状态更新成功')
  }
  catch {
    message.error('租户状态更新失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateTenantApi(formData.value.basicId, formData.value)
    }
    else {
      await createTenantApi(formData.value)
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

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  queryParams.tenantStatus = undefined
  queryParams.page = 1
  fetchData()
}

const columns: DataTableColumns<SysTenant> = [
  {
    title: '租户编码',
    key: 'tenantCode',
    width: 150,
    render: row =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.tenantCode }),
  },
  {
    title: '租户名称',
    key: 'tenantName',
    width: 180,
  },
  {
    title: '租户简称',
    key: 'tenantShortName',
    width: 140,
  },
  {
    title: '联系人',
    key: 'contactPerson',
    width: 120,
  },
  {
    title: '联系电话',
    key: 'contactPhone',
    width: 140,
  },
  {
    title: '隔离模式',
    key: 'isolationMode',
    width: 120,
    render: row => getOptionLabel(TENANT_ISOLATION_MODE_OPTIONS, row.isolationMode ?? 0),
  },
  {
    title: '租户状态',
    key: 'tenantStatus',
    width: 100,
    render: row =>
      h(
        NTag,
        { type: row.tenantStatus === 0 ? 'success' : 'warning', size: 'small', round: true },
        { default: () => getOptionLabel(TENANT_STATUS_OPTIONS, row.tenantStatus ?? 0) },
      ),
  },
  {
    title: '启用状态',
    key: 'status',
    width: 100,
    render: row =>
      h(
        NTag,
        { type: getStatusType(row.status ?? 1), size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '到期时间',
    key: 'expireTime',
    width: 180,
    render: row => formatDate(row.expireTime ?? row.expiredTime ?? ''),
  },
  {
    title: '操作',
    key: 'actions',
    width: 220,
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
                type: row.tenantStatus === 0 ? 'warning' : 'success',
                ghost: true,
                onClick: () => handleChangeTenantStatus(row),
              },
              { default: () => (row.tenantStatus === 0 ? '暂停' : '恢复') },
            ),
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
                default: () => '确认删除该租户？',
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
          placeholder="搜索租户名称/编码/联系人"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="启用状态"
          style="width: 120px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.tenantStatus"
          :options="TENANT_STATUS_OPTIONS"
          placeholder="租户状态"
          style="width: 130px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton @click="handleReset">重置</NButton>
        <NButton class="ml-auto" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增租户
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :pagination="false"
        :row-key="(row) => row.basicId"
        :scroll-x="1500"
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
        <NFormItem label="租户编码" path="tenantCode">
          <NInput
            v-model:value="formData.tenantCode"
            :disabled="!!formData.basicId"
            placeholder="请输入租户编码"
          />
        </NFormItem>
        <NFormItem label="租户名称" path="tenantName">
          <NInput v-model:value="formData.tenantName" placeholder="请输入租户名称" />
        </NFormItem>
        <NFormItem label="租户简称" path="tenantShortName">
          <NInput v-model:value="formData.tenantShortName" placeholder="请输入租户简称" />
        </NFormItem>
        <NFormItem label="联系人" path="contactPerson">
          <NInput v-model:value="formData.contactPerson" placeholder="请输入联系人" />
        </NFormItem>
        <NFormItem label="联系电话" path="contactPhone">
          <NInput v-model:value="formData.contactPhone" placeholder="请输入联系电话" />
        </NFormItem>
        <NFormItem label="联系邮箱" path="contactEmail">
          <NInput v-model:value="formData.contactEmail" placeholder="请输入联系邮箱" />
        </NFormItem>
        <NFormItem v-if="!formData.basicId" label="隔离模式" path="isolationMode">
          <NSelect v-model:value="formData.isolationMode" :options="TENANT_ISOLATION_MODE_OPTIONS" />
        </NFormItem>
        <NFormItem label="启用状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="到期时间" path="expireTime">
          <NInput
            v-model:value="formData.expireTime"
            placeholder="如: 2026-12-31T23:59:59+08:00"
          />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">确认</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
