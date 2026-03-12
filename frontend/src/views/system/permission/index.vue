<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysPermission } from '~/types'
import { Icon } from '~/iconify'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import {
  createPermissionApi,
  deletePermissionApi,
  getPermissionPageApi,
  updatePermissionApi,
} from '@/api'
import { DEFAULT_PAGE_SIZE, STATUS_OPTIONS } from '~/constants'
import { formatDate, getStatusType } from '~/utils'

defineOptions({ name: 'SystemPermissionPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysPermission[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增权限')
const submitLoading = ref(false)

const formData = ref<Partial<SysPermission>>({
  permissionCode: '',
  permissionName: '',
  permissionDescription: '',
  resourceId: 1,
  operationId: 1,
  isRequireAudit: false,
  priority: 0,
  sort: 100,
  status: 1,
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getPermissionPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  }
  catch {
    message.error('获取权限列表失败')
  }
  finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增权限'
  formData.value = {
    permissionCode: '',
    permissionName: '',
    permissionDescription: '',
    resourceId: 1,
    operationId: 1,
    isRequireAudit: false,
    priority: 0,
    sort: 100,
    status: 1,
  }
  modalVisible.value = true
}

function handleEdit(row: SysPermission) {
  modalTitle.value = '编辑权限'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deletePermissionApi(id)
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
      await updatePermissionApi(formData.value.basicId, formData.value)
    }
    else {
      await createPermissionApi(formData.value)
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
  queryParams.page = 1
  fetchData()
}

const columns: DataTableColumns<SysPermission> = [
  {
    title: '权限名称',
    key: 'permissionName',
    width: 190,
  },
  {
    title: '权限编码',
    key: 'permissionCode',
    width: 220,
    render: row =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.permissionCode }),
  },
  {
    title: '资源/操作',
    key: 'resource',
    width: 150,
    render: row => `${row.resourceId}/${row.operationId}`,
  },
  {
    title: '审计',
    key: 'isRequireAudit',
    width: 80,
    align: 'center',
    render: row =>
      h(
        NTag,
        { type: row.isRequireAudit ? 'warning' : 'default', size: 'small', round: true },
        { default: () => (row.isRequireAudit ? '需要' : '不需要') },
      ),
  },
  {
    title: '优先级',
    key: 'priority',
    width: 80,
    align: 'center',
  },
  {
    title: '排序',
    key: 'sort',
    width: 80,
    align: 'center',
  },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render: row =>
      h(
        NTag,
        { type: getStatusType(row.status ?? 1), size: 'small', round: true },
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
                default: () => '确认删除该权限？',
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
          placeholder="搜索权限名称/编码"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
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
        <NButton @click="handleReset">
          重置
        </NButton>
        <NButton class="ml-auto" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增权限
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.basicId"
        :pagination="false"
        :scroll-x="1320"
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
      <NForm :model="formData" label-placement="left" label-width="100px">
        <NFormItem label="权限编码" path="permissionCode">
          <NInput v-model:value="formData.permissionCode" placeholder="如: system:user:list" />
        </NFormItem>
        <NFormItem label="权限名称" path="permissionName">
          <NInput v-model:value="formData.permissionName" placeholder="请输入权限名称" />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-3">
          <NFormItem label="资源ID" path="resourceId">
            <NInputNumber v-model:value="formData.resourceId" :min="1" class="w-full" />
          </NFormItem>
          <NFormItem label="操作ID" path="operationId">
            <NInputNumber v-model:value="formData.operationId" :min="1" class="w-full" />
          </NFormItem>
          <NFormItem label="优先级" path="priority">
            <NInputNumber v-model:value="formData.priority" :min="0" class="w-full" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="formData.sort" :min="0" class="w-full" />
          </NFormItem>
        </div>
        <NFormItem label="需要审计" path="isRequireAudit">
          <NSwitch v-model:value="formData.isRequireAudit" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="权限描述" path="permissionDescription">
          <NInput
            v-model:value="formData.permissionDescription"
            type="textarea"
            :rows="3"
            placeholder="请输入权限描述"
          />
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
