<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysPermission } from '~/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { permissionApi } from '@/api'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemPermissionPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return permissionApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysPermission>(
  {
    id: 'sys_permission',
    name: '权限管理',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'permissionName',
        title: '权限名称',
        minWidth: 160,
        showOverflow: 'tooltip',
        sortable: true,
      },
      { field: 'permissionCode', title: '权限编码', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'tags', title: '标签', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'permissionDescription', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'priority', title: '优先级', width: 80 },
      {
        field: 'isRequireAudit',
        title: '需要审计',
        width: 90,
        slots: { default: 'col_audit' },
      },
      { field: 'sort', title: '排序', width: 70 },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'createTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'actions',
        title: '操作',
        width: 140,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增权限')
const submitLoading = ref(false)
const formData = ref<Partial<SysPermission>>({})

function resetForm() {
  formData.value = {
    permissionName: '',
    permissionCode: '',
    permissionDescription: '',
    groupName: '',
    sort: 0,
    status: 1,
  }
}

function handleAdd() {
  modalTitle.value = '新增权限'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysPermission) {
  modalTitle.value = '编辑权限'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await permissionApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await permissionApi.update(formData.value.basicId, formData.value)
    } else {
      await permissionApi.create(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  } catch {
    message.error('操作失败')
  } finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <vxe-card class="mb-2" style="padding: 10px 16px">
      <div class="flex flex-wrap gap-3 items-center">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索权限名称/编码"
          clearable
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          clearable
          style="width: 120px"
        />
        <NButton type="primary" size="small" @click="handleSearch">查询</NButton>
        <NButton size="small" @click="handleReset">重置</NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">新增权限</NButton>
        </template>
        <template #col_audit="{ row }">
          <NTag :type="row.isRequireAudit ? 'warning' : 'default'" size="small">
            {{ row.isRequireAudit ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleEdit(row)">编辑</NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>删除</NButton>
              </template>
              确认删除该权限？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 560px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="权限名称" path="permissionName">
          <NInput v-model:value="formData.permissionName" placeholder="请输入权限名称" />
        </NFormItem>
        <NFormItem label="权限编码" path="permissionCode">
          <NInput v-model:value="formData.permissionCode" placeholder="如: system:user:list" />
        </NFormItem>
        <NFormItem label="分组" path="groupName">
          <NInput v-model:value="formData.groupName" placeholder="权限分组名称" />
        </NFormItem>
        <NFormItem label="描述" path="permissionDescription">
          <NInput
            v-model:value="formData.permissionDescription"
            type="textarea"
            :rows="2"
            placeholder="权限描述"
          />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
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
