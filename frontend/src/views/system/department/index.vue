<script lang="ts" setup>
import type { VxeGridInstance } from 'vxe-table'
import type { SysDepartment } from '@/api/modules/department'
import type { SysUser } from '@/api/modules/user'
import {
  NButton,
  NCascader,
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
import { computed, onMounted, ref } from 'vue'
import { departmentApi, userApi } from '@/api'
import { DEPARTMENT_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemDepartmentPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const loading = ref(false)
const tableData = ref<SysDepartment[]>([])
const userOptions = ref<Array<{ label: string, value: string }>>([])

const treeOptions = computed(() => {
  const items = tableData.value
  const map = new Map<string, SysDepartment[]>()
  for (const item of items) {
    const pid = item.parentId || ''
    if (!map.has(pid)) {
      map.set(pid, [])
    }
    map.get(pid)!.push(item)
  }
  function toNodes(parentId: string): any[] {
    return (map.get(parentId) ?? []).map(c => ({
      label: c.departmentName,
      value: c.basicId,
      children: map.has(c.basicId) ? toNodes(c.basicId) : undefined,
    }))
  }
  return [{ label: '顶级部门', value: '', children: toNodes('') }]
})

async function fetchData() {
  try {
    loading.value = true
    const result = await departmentApi.page({ page: 1, pageSize: 9999 })
    tableData.value = (result.items ?? []).map((item: SysDepartment) => ({
      ...item,
      basicId: String(item.basicId ?? ''),
      parentId: item.parentId ? String(item.parentId) : '',
    }))
  }
  catch {
    message.error('获取部门列表失败')
  }
  finally {
    loading.value = false
  }
}

async function fetchUserOptions() {
  try {
    const result = await userApi.page({ page: 1, pageSize: 9999, status: 1 })
    userOptions.value = (result.items ?? []).map((user: SysUser) => ({
      value: String(user.basicId),
      label: user.realName || user.nickName || user.userName,
    }))
  }
  catch {
    userOptions.value = []
    message.error('获取负责人列表失败')
  }
}

const options = useVxeTable<SysDepartment>({
  id: 'sys_department',
  name: '部门管理',
  data: [],
  columns: [
    { field: 'departmentName', title: '部门名称', minWidth: 220, treeNode: true, showOverflow: 'tooltip' },
    { field: 'departmentCode', title: '部门编码', minWidth: 140, showOverflow: 'tooltip' },
    {
      field: 'departmentType',
      title: '类型',
      width: 100,
      formatter: ({ cellValue }) => getOptionLabel(DEPARTMENT_TYPE_OPTIONS, cellValue),
    },
    { field: 'leader', title: '负责人', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'phone', title: '电话', minWidth: 130, showOverflow: 'tooltip' },
    { field: 'email', title: '邮箱', minWidth: 180, showOverflow: 'tooltip' },
    { field: 'address', title: '地址', minWidth: 180, showOverflow: 'tooltip' },
    { field: 'sort', title: '排序', width: 70 },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    {
      field: 'actions',
      title: '操作',
      width: 200,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  pagerConfig: { enabled: false },
  treeConfig: {
    transform: true,
    rowField: 'basicId',
    parentField: 'parentId',
    expandAll: false,
  },
  toolbarConfig: {
    slots: { buttons: 'toolbar_buttons' },
    refresh: true,
    zoom: true,
    custom: true,
  },
})

const modalVisible = ref(false)
const modalTitle = ref('新增部门')
const submitLoading = ref(false)
const formData = ref<Partial<SysDepartment>>({})

function resetForm() {
  formData.value = {
    parentId: '',
    departmentName: '',
    departmentCode: '',
    departmentType: 6,
    leaderId: undefined,
    phone: '',
    email: '',
    sort: 0,
    status: 1,
    remark: '',
  }
}

function handleAdd(parentId?: string) {
  modalTitle.value = '新增部门'
  resetForm()
  if (parentId)
    formData.value.parentId = parentId
  modalVisible.value = true
}

function handleEdit(row: SysDepartment) {
  modalTitle.value = '编辑部门'
  formData.value = {
    ...row,
    leaderId: row.leaderId ?? undefined,
  }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await departmentApi.delete(id)
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
      await departmentApi.update(formData.value.basicId, formData.value)
    }
    else {
      await departmentApi.create(formData.value)
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

onMounted(async () => {
  await Promise.all([fetchData(), fetchUserOptions()])
})
</script>

<template>
  <div class="h-full flex flex-col">
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options" :data="tableData" :loading="loading">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd()">
            新增部门
          </NButton>
          <NButton size="small" class="ml-2" @click="fetchData">
            刷新
          </NButton>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="info" text @click="handleAdd(row.basicId)">
              新增子部门
            </NButton>
            <NButton size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该部门？子部门也会一并删除。
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 560px" :auto-focus="false">
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="上级部门" path="parentId">
          <NCascader v-model:value="formData.parentId" :options="treeOptions" check-strategy="child" placeholder="无则为顶级" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="部门名称" path="departmentName">
          <NInput v-model:value="formData.departmentName" placeholder="请输入部门名称" />
        </NFormItem>
        <NFormItem label="部门编码" path="departmentCode">
          <NInput v-model:value="formData.departmentCode" placeholder="请输入部门编码" />
        </NFormItem>
        <NFormItem label="部门类型" path="departmentType">
          <NSelect v-model:value="formData.departmentType" :options="DEPARTMENT_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="负责人" path="leaderId">
          <NSelect
            v-model:value="formData.leaderId"
            :options="userOptions"
            placeholder="请选择负责人"
            clearable
            filterable
          />
        </NFormItem>
        <NFormItem label="联系电话" path="phone">
          <NInput v-model:value="formData.phone" placeholder="联系电话" />
        </NFormItem>
        <NFormItem label="邮箱" path="email">
          <NInput v-model:value="formData.email" placeholder="邮箱" />
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
