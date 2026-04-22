<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysDepartment } from '@/api/modules/department'
import type { SysPermission } from '@/api/modules/permission'
import type { SysRole } from '@/api/modules/role'
import type { SysUser } from '@/api/modules/user'
import {
  NButton,
  NDivider,
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
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { departmentApi, permissionApi, roleApi, userApi } from '@/api'
import { toId } from '@/api/helpers'
import { XSystemQueryPanel } from '~/components'
import { useEnumService, useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemUserPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const DEFAULT_GENDER_OPTIONS = [
  { label: '未知', value: 0 },
  { label: '男', value: 1 },
  { label: '女', value: 2 },
]
const DEFAULT_STATUS_OPTIONS = [
  { label: '禁用', value: 0 },
  { label: '启用', value: 1 },
]
const genderOptions = ref<Array<{ label: string, value: any, disabled?: boolean }>>(DEFAULT_GENDER_OPTIONS)
const statusOptions = ref<Array<{ label: string, value: any, disabled?: boolean }>>(DEFAULT_STATUS_OPTIONS)
const { ensureBatch, toSelectOptions, getLabel: getEnumLabel } = useEnumService()

// 全量角色列表：下拉选项与表格展示名称映射
const allRoles = ref<SysRole[]>([])
const roleOptions = computed(() =>
  allRoles.value.map(r => ({ label: r.roleName, value: r.basicId })),
)
const roleIdToName = computed(() => new Map(allRoles.value.map(r => [r.basicId, r.roleName])))
const allPermissions = ref<SysPermission[]>([])
const permissionOptions = computed(() =>
  allPermissions.value.map(item => ({ label: item.permissionName, value: item.basicId })),
)
const allDepartments = ref<Array<{ basicId: string, departmentName: string, fullName: string }>>([])
const departmentOptions = computed(() =>
  allDepartments.value.map(item => ({ label: item.fullName, value: item.basicId })),
)
const deptIdToName = computed(() => new Map(allDepartments.value.map(item => [item.basicId, item.departmentName])))

function flattenDepartmentTree(
  nodes: SysDepartment[],
  parentNames: string[] = [],
): Array<{ basicId: string, departmentName: string, fullName: string }> {
  const result: Array<{ basicId: string, departmentName: string, fullName: string }> = []
  for (const node of nodes) {
    const currentNames = [...parentNames, node.departmentName]
    result.push({
      basicId: node.basicId,
      departmentName: node.departmentName,
      fullName: currentNames.join(' / '),
    })
    if (node.children?.length) {
      result.push(...flattenDepartmentTree(node.children, currentNames))
    }
  }
  return result
}

onMounted(async () => {
  try {
    const [roles, permissions, departments] = await Promise.all([
      roleApi.list(),
      permissionApi.list(),
      departmentApi.tree(),
      ensureBatch(['UserGender', 'YesOrNo']),
    ])
    allRoles.value = roles
    allPermissions.value = permissions
    allDepartments.value = flattenDepartmentTree(departments)
    genderOptions.value = toSelectOptions('UserGender', DEFAULT_GENDER_OPTIONS)
    statusOptions.value = toSelectOptions('YesOrNo', DEFAULT_STATUS_OPTIONS)
  }
  catch {
    message.error('加载角色、权限或部门列表失败')
  }
})

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return userApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysUser>(
  {
    id: 'sys_user',
    name: '用户管理',
    columns: [
      { type: 'checkbox', width: 40, fixed: 'left' },
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'userName',
        title: '用户名',
        minWidth: 130,
        showOverflow: 'tooltip',
        sortable: true,
      },
      { field: 'nickName', title: '昵称', minWidth: 130, showOverflow: 'tooltip' },
      { field: 'realName', title: '真实姓名', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'email', title: '邮箱', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'phone', title: '手机号', minWidth: 130, showOverflow: 'tooltip' },
      {
        field: 'lastLoginTime',
        title: '最后登录',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      { field: 'lastLoginIp', title: '最后登录IP', minWidth: 130, showOverflow: 'tooltip' },
      {
        field: 'gender',
        title: '性别',
        width: 70,
        formatter: ({ cellValue }) => getEnumLabel('UserGender', cellValue, '未知'),
      },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'roles',
        title: '角色',
        minWidth: 160,
        slots: { default: 'col_roles' },
      },
      {
        field: 'mainDepartmentId',
        title: '主部门',
        minWidth: 140,
        slots: { default: 'col_department' },
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
        width: 220,
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
    checkboxConfig: { range: true, reserve: true },
    rowConfig: { keyField: 'basicId' },
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

// ==================== CRUD ====================

const modalVisible = ref(false)
const modalTitle = ref('新增用户')
const submitLoading = ref(false)
type UserForm = Partial<SysUser & { password?: string, roleIds: string[] }>
interface UserDepartmentForm {
  departmentIds: string[]
  mainDepartmentId?: string
}

const formData = ref<UserForm & { permissionIds: string[] } & UserDepartmentForm>({
  roleIds: [],
  permissionIds: [],
  departmentIds: [],
  mainDepartmentId: undefined,
})
const selectedDepartmentOptions = computed(() => {
  const selectedIds = new Set(formData.value.departmentIds ?? [])
  return departmentOptions.value.filter(item => selectedIds.has(item.value))
})

function resetForm() {
  formData.value = {
    userName: '',
    nickName: '',
    email: '',
    phone: '',
    gender: 0,
    status: 1,
    password: '',
    roleIds: [],
    permissionIds: [],
    departmentIds: [],
    mainDepartmentId: undefined,
  }
}

function handleAdd() {
  modalTitle.value = '新增用户'
  resetForm()
  modalVisible.value = true
}

async function handleEdit(row: SysUser) {
  modalTitle.value = '编辑用户'
  formData.value = {
    ...row,
    password: '',
    roleIds: [],
    permissionIds: [],
    departmentIds: row.departmentIds ? [...row.departmentIds] : [],
    mainDepartmentId: row.mainDepartmentId,
  }
  modalVisible.value = true
  try {
    const [roleIds, permissionIds, departments] = await Promise.all([
      userApi.getUserRoles(row.basicId),
      userApi.getUserPermissions(row.basicId),
      userApi.getUserDepartments(row.basicId),
    ])
    formData.value.roleIds = [...roleIds]
    formData.value.permissionIds = [...permissionIds]
    formData.value.departmentIds = [...departments.departmentIds]
    formData.value.mainDepartmentId = departments.mainDepartmentId
    if (!formData.value.mainDepartmentId && formData.value.departmentIds.length > 0) {
      formData.value.mainDepartmentId = formData.value.departmentIds[0]
    }
  }
  catch {
    formData.value.roleIds = []
    formData.value.permissionIds = []
    formData.value.departmentIds = []
    formData.value.mainDepartmentId = undefined
    message.error('加载用户授权信息失败')
  }
}

async function handleDelete(id: string) {
  try {
    await userApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleToggleStatus(row: SysUser) {
  const newStatus = row.status === 1 ? 0 : 1
  try {
    await userApi.changeStatus(row.basicId, newStatus)
    message.success('状态更新成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('状态更新失败')
  }
}

const resetPwdVisible = ref(false)
const resetPwdUserId = ref('')
const resetPwdValue = ref('')

function handleResetPwd(row: SysUser) {
  resetPwdUserId.value = row.basicId
  resetPwdValue.value = ''
  resetPwdVisible.value = true
}

async function confirmResetPwd() {
  if (!resetPwdValue.value) {
    message.warning('请输入新密码')
    return
  }
  try {
    await userApi.resetPassword(resetPwdUserId.value, resetPwdValue.value)
    message.success('密码重置成功')
    resetPwdVisible.value = false
  }
  catch {
    message.error('密码重置失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    const roleIds = formData.value.roleIds ?? []
    const permissionIds = formData.value.permissionIds ?? []
    const departmentIds = formData.value.departmentIds ?? []
    let mainDepartmentId = formData.value.mainDepartmentId
    if (departmentIds.length === 0) {
      mainDepartmentId = undefined
    }
    else if (!mainDepartmentId || !departmentIds.includes(mainDepartmentId)) {
      mainDepartmentId = departmentIds[0]
    }
    let userId = formData.value.basicId
    if (userId) {
      await userApi.update(userId, formData.value)
    }
    else {
      const created = await userApi.create(formData.value)
      const createdRecord = created as unknown as Record<string, unknown>
      const createdData = createdRecord?.data as Record<string, unknown> | undefined
      userId = toId(
        createdRecord?.basicId
        ?? createdRecord?.BasicId
        ?? createdData?.basicId
        ?? createdData?.BasicId,
      )
    }
    if (userId) {
      await userApi.assignRoles(userId, roleIds)
      await userApi.assignPermissions(userId, permissionIds)
      await userApi.assignDepartments(userId, departmentIds, mainDepartmentId)
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

/** 表格单元格：角色 ID 或名称解析为展示名 */
function roleLabelForCell(value: string) {
  return roleIdToName.value.get(value) ?? '未知角色'
}

/** 表格单元格：部门 ID 解析为展示名 */
function departmentLabelForCell(value?: string) {
  if (!value) {
    return '—'
  }
  return deptIdToName.value.get(value) ?? '未知部门'
}

watch(
  () => formData.value.departmentIds,
  (value) => {
    const ids = value ?? []
    if (ids.length === 0) {
      formData.value.mainDepartmentId = undefined
      return
    }
    if (!formData.value.mainDepartmentId || !ids.includes(formData.value.mainDepartmentId)) {
      formData.value.mainDepartmentId = ids[0]
    }
  },
  { deep: true },
)
</script>

<template>
  <div class="flex flex-col h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索用户名/昵称/邮箱/手机"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
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
    </XSystemQueryPanel>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton v-access="['user:create']" type="primary" size="small" @click="handleAdd">
            新增用户
          </NButton>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ getEnumLabel('YesOrNo', row.status, row.status === 1 ? '启用' : '禁用') }}
          </NTag>
        </template>
        <template #col_roles="{ row }">
          <NSpace v-if="row.roles?.length" size="small" wrap>
            <NTag
              v-for="(rid, idx) in row.roles"
              :key="`${row.basicId}-${String(rid)}-${idx}`"
              type="info"
              size="small"
              round
            >
              {{ roleLabelForCell(String(rid)) }}
            </NTag>
          </NSpace>
          <span v-else class="text-gray-400">—</span>
        </template>
        <template #col_department="{ row }">
          <NTag v-if="row.mainDepartmentId" size="small" type="default" round>
            {{ departmentLabelForCell(row.mainDepartmentId) }}
          </NTag>
          <span v-else class="text-gray-400">—</span>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-access="['user:update']" size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NButton v-access="['user:update']" size="small" type="warning" text @click="handleToggleStatus(row)">
              {{ row.status === 1 ? '禁用' : '启用' }}
            </NButton>
            <NButton v-access="['user:update']" size="small" type="info" text @click="handleResetPwd(row)">
              重置密码
            </NButton>
            <NPopconfirm v-access="['user:delete']" @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该用户？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 520px"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="80px">
        <NFormItem label="用户名" path="userName">
          <NInput
            v-model:value="formData.userName"
            :disabled="!!formData.basicId"
            placeholder="请输入用户名"
          />
        </NFormItem>
        <NFormItem v-if="!formData.basicId" label="密码" path="password">
          <NInput
            v-model:value="formData.password"
            type="password"
            show-password-on="click"
            placeholder="请输入初始密码"
          />
        </NFormItem>
        <NFormItem label="昵称" path="nickName">
          <NInput v-model:value="formData.nickName" placeholder="请输入昵称" />
        </NFormItem>
        <NFormItem label="邮箱" path="email">
          <NInput v-model:value="formData.email" placeholder="请输入邮箱" />
        </NFormItem>
        <NFormItem label="真实姓名" path="realName">
          <NInput v-model:value="formData.realName" placeholder="请输入真实姓名" />
        </NFormItem>
        <NFormItem label="手机号" path="phone">
          <NInput v-model:value="formData.phone" placeholder="请输入手机号" />
        </NFormItem>
        <NFormItem label="性别" path="gender">
          <NSelect v-model:value="formData.gender" :options="genderOptions" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="statusOptions" />
        </NFormItem>
        <NDivider title-placement="left">
          部门归属
        </NDivider>
        <NFormItem label="部门" path="departmentIds">
          <NSelect
            v-model:value="formData.departmentIds"
            :options="departmentOptions"
            multiple
            clearable
            filterable
            placeholder="选择归属部门"
          />
        </NFormItem>
        <NFormItem label="主部门" path="mainDepartmentId">
          <NSelect
            v-model:value="formData.mainDepartmentId"
            :options="selectedDepartmentOptions"
            clearable
            :disabled="(formData.departmentIds?.length ?? 0) === 0"
            placeholder="请选择主部门"
          />
        </NFormItem>
        <NDivider title-placement="left">
          角色分配
        </NDivider>
        <NFormItem label="角色" path="roleIds" class="xh-form-full-row">
          <NSelect
            v-model:value="formData.roleIds"
            :options="roleOptions"
            multiple
            clearable
            placeholder="选择角色"
          />
        </NFormItem>
        <NDivider title-placement="left">
          用户直授权限
        </NDivider>
        <NFormItem label="权限" path="permissionIds" class="xh-form-full-row">
          <NSelect
            v-model:value="formData.permissionIds"
            :options="permissionOptions"
            multiple
            clearable
            filterable
            placeholder="选择直授权限"
          />
        </NFormItem>
        <NFormItem label="可访问租户" path="accessibleTenantIds" class="xh-form-full-row">
          <NInput
            :value="(formData.accessibleTenantIds ?? []).join(', ')"
            placeholder="当前批次暂只展示 DTO 中的可访问租户结果"
            readonly
          />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton
            v-if="!formData.basicId"
            v-access="['user:create']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
          <NButton
            v-else
            v-access="['user:update']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="resetPwdVisible"
      title="重置密码"
      preset="card"
      style="width: 400px"
      :auto-focus="false"
    >
      <NInput
        v-model:value="resetPwdValue"
        type="password"
        show-password-on="click"
        placeholder="请输入新密码"
      />
      <template #footer>
        <NSpace justify="end">
          <NButton @click="resetPwdVisible = false">
            取消
          </NButton>
          <NButton v-access="['user:update']" type="primary" @click="confirmResetPwd">
            确认重置
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
