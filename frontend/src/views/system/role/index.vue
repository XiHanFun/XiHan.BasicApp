<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysRole } from '@/api/modules/role'
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
import { roleApi } from '@/api'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemRolePage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return roleApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysRole>({
  id: 'sys_role',
  name: '角色管理',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'roleName', title: '角色名称', minWidth: 150, showOverflow: 'tooltip', sortable: true },
    { field: 'roleCode', title: '角色编码', minWidth: 150, showOverflow: 'tooltip' },
    { field: 'roleDescription', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
    { field: 'roleType', title: '角色类型', width: 100 },
    { field: 'dataScope', title: '数据范围', width: 100 },
    { field: 'sort', title: '排序', width: 70 },
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
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

// ==================== CRUD ====================

const modalVisible = ref(false)
const modalTitle = ref('新增角色')
const submitLoading = ref(false)
const formData = ref<Partial<SysRole>>({})

function resetForm() {
  formData.value = { roleName: '', roleCode: '', roleDescription: '', status: 1, sort: 100 }
}

function handleAdd() {
  modalTitle.value = '新增角色'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysRole) {
  modalTitle.value = '编辑角色'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await roleApi.delete(id)
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
      await roleApi.update(formData.value.basicId, formData.value)
    }
    else {
      await roleApi.create(formData.value)
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
          placeholder="搜索角色名称/编码"
          clearable
          style="width: 240px"
          @keyup.enter="handleSearch"
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
            新增角色
          </NButton>
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
              确认删除该角色？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 480px" :auto-focus="false">
      <NForm :model="formData" label-placement="left" label-width="80px">
        <NFormItem label="角色名称" path="roleName">
          <NInput v-model:value="formData.roleName" placeholder="请输入角色名称" />
        </NFormItem>
        <NFormItem label="角色编码" path="roleCode">
          <NInput v-model:value="formData.roleCode" :disabled="!!formData.basicId" placeholder="如: admin, editor" />
        </NFormItem>
        <NFormItem label="描述" path="roleDescription">
          <NInput v-model:value="formData.roleDescription" type="textarea" :rows="3" placeholder="角色描述" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" style="width: 100%" />
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
