<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { RoleCreateDto, RoleListItemDto, RoleUpdateDto } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  createPageRequest,
  DataPermissionScope,
  EnableStatus,
  roleApi,
  RoleType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemRolePage' })

interface RoleGridResult {
  items: RoleListItemDto[]
  total: number
}

interface RoleFormModel extends RoleCreateDto {
  basicId?: RoleListItemDto['basicId']
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<RoleListItemDto>>()

const queryParams = reactive({
  dataScope: undefined as DataPermissionScope | undefined,
  isGlobal: undefined as number | undefined,
  keyword: '',
  roleType: undefined as RoleType | undefined,
  status: undefined as EnableStatus | undefined,
})

const globalOptions = [
  { label: '全局角色', value: 1 },
  { label: '租户角色', value: 0 },
]

const roleTypeOptions = [
  { label: '系统角色', value: RoleType.System },
  { label: '业务角色', value: RoleType.Business },
  { label: '自定义角色', value: RoleType.Custom },
]

const maintainableRoleTypeOptions = [
  { label: '业务角色', value: RoleType.Business },
  { label: '自定义角色', value: RoleType.Custom },
]

const dataScopeOptions = [
  { label: '仅本人', value: DataPermissionScope.SelfOnly },
  { label: '本部门', value: DataPermissionScope.DepartmentOnly },
  { label: '本部门及以下', value: DataPermissionScope.DepartmentAndChildren },
  { label: '全部数据', value: DataPermissionScope.All },
  { label: '自定义', value: DataPermissionScope.Custom },
]

const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const roleForm = ref<RoleFormModel>(createDefaultRoleForm())

const modalTitle = computed(() => (roleForm.value.basicId ? '编辑角色' : '新增角色'))

function createDefaultRoleForm(): RoleFormModel {
  return {
    dataScope: DataPermissionScope.SelfOnly,
    maxMembers: 0,
    remark: null,
    roleCode: '',
    roleDescription: null,
    roleName: '',
    roleType: RoleType.Custom,
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
}

function canMaintainRole(row: RoleListItemDto) {
  return !row.isGlobal && row.roleType !== RoleType.System
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<RoleGridResult> {
  return roleApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      dataScope: queryParams.dataScope,
      isGlobal: toOptionalBoolean(queryParams.isGlobal),
      keyword: queryParams.keyword.trim() || null,
      roleType: queryParams.roleType,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询角色失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<RoleListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'roleName', minWidth: 150, showOverflow: 'tooltip', sortable: true, title: '角色名称' },
      { field: 'roleCode', minWidth: 150, showOverflow: 'tooltip', title: '角色编码' },
      { field: 'roleDescription', minWidth: 220, showOverflow: 'tooltip', title: '描述' },
      {
        field: 'roleType',
        formatter: ({ cellValue }) => getOptionLabel(roleTypeOptions, cellValue),
        minWidth: 110,
        title: '角色类型',
      },
      {
        field: 'isGlobal',
        slots: { default: 'col_global' },
        title: '全局',
        width: 82,
      },
      {
        field: 'dataScope',
        formatter: ({ cellValue }) => getOptionLabel(dataScopeOptions, cellValue),
        minWidth: 130,
        title: '数据范围',
      },
      { field: 'maxMembers', minWidth: 100, title: '成员上限' },
      { field: 'sort', minWidth: 80, sortable: true, title: '排序' },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 82,
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 180,
      },
    ],
    id: 'sys_role',
    name: '角色管理',
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
  queryParams.roleType = undefined
  queryParams.dataScope = undefined
  queryParams.isGlobal = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  editingStatus.value = null
  roleForm.value = createDefaultRoleForm()
  modalVisible.value = true
}

function handleEdit(row: RoleListItemDto) {
  editingStatus.value = row.status
  roleForm.value = {
    basicId: row.basicId,
    dataScope: row.dataScope,
    maxMembers: row.maxMembers,
    remark: null,
    roleCode: row.roleCode,
    roleDescription: row.roleDescription,
    roleName: row.roleName,
    roleType: row.roleType,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

function validateRoleForm() {
  if (!roleForm.value.roleName.trim()) {
    message.warning('请输入角色名称')
    return false
  }

  if (!roleForm.value.basicId && !roleForm.value.roleCode.trim()) {
    message.warning('请输入角色编码')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateRoleForm()) {
    return
  }

  submitLoading.value = true

  try {
    if (roleForm.value.basicId) {
      const updateInput: RoleUpdateDto = {
        basicId: roleForm.value.basicId,
        dataScope: roleForm.value.dataScope,
        maxMembers: roleForm.value.maxMembers,
        remark: roleForm.value.remark,
        roleDescription: roleForm.value.roleDescription,
        roleName: roleForm.value.roleName.trim(),
        roleType: roleForm.value.roleType,
        sort: roleForm.value.sort,
      }

      await roleApi.update(updateInput)
      if (editingStatus.value !== roleForm.value.status) {
        await roleApi.updateStatus({
          basicId: roleForm.value.basicId,
          remark: roleForm.value.remark,
          status: roleForm.value.status,
        })
      }
    }
    else {
      const createInput: RoleCreateDto = {
        dataScope: roleForm.value.dataScope,
        maxMembers: roleForm.value.maxMembers,
        remark: roleForm.value.remark,
        roleCode: roleForm.value.roleCode.trim(),
        roleDescription: roleForm.value.roleDescription,
        roleName: roleForm.value.roleName.trim(),
        roleType: roleForm.value.roleType,
        sort: roleForm.value.sort,
        status: roleForm.value.status,
      }

      await roleApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: RoleListItemDto) {
  await roleApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: RoleListItemDto) {
  await roleApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用角色' : '前端启用角色',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索角色名称/编码"
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.roleType"
          :options="roleTypeOptions"
          clearable
          placeholder="角色类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.dataScope"
          :options="dataScopeOptions"
          clearable
          placeholder="数据范围"
          style="width: 140px"
        />
        <NSelect
          v-model:value="queryParams.isGlobal"
          :options="globalOptions"
          clearable
          placeholder="全局"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          clearable
          placeholder="状态"
          style="width: 110px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增角色
          </NButton>
        </template>

        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'warning' : 'default'" round size="small">
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ row.status === EnableStatus.Enabled ? '启用' : '禁用' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton
              :disabled="!canMaintainRole(row)"
              size="small"
              text
              type="primary"
              @click="handleEdit(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
              编辑
            </NButton>

            <NPopconfirm
              :disabled="!canMaintainRole(row)"
              @positive-click="handleToggleStatus(row)"
            >
              <template #trigger>
                <NButton :disabled="!canMaintainRole(row)" size="small" text type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === EnableStatus.Enabled ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新角色状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainRole(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainRole(row)" size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确认删除该角色？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="roleForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="角色名称" path="roleName">
          <NInput v-model:value="roleForm.roleName" clearable placeholder="请输入角色名称" />
        </NFormItem>
        <NFormItem label="角色编码" path="roleCode">
          <NInput
            v-model:value="roleForm.roleCode"
            clearable
            :disabled="Boolean(roleForm.basicId)"
            placeholder="如: business_admin"
          />
        </NFormItem>
        <NFormItem label="角色类型" path="roleType">
          <NSelect v-model:value="roleForm.roleType" :options="maintainableRoleTypeOptions" />
        </NFormItem>
        <NFormItem label="数据范围" path="dataScope">
          <NSelect v-model:value="roleForm.dataScope" :options="dataScopeOptions" />
        </NFormItem>
        <NFormItem label="成员上限" path="maxMembers">
          <NInputNumber v-model:value="roleForm.maxMembers" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="roleForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="roleForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="roleForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="描述" path="roleDescription">
          <NInput
            v-model:value="roleForm.roleDescription"
            clearable
            placeholder="请输入角色描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
