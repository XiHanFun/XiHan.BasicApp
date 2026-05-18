<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { RoleCreateDto, RoleListItemDto, RoleManagementDetailDto, RoleUpdateDto } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NTabPane,
  NTabs,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import {
  createPageRequest,
  DataPermissionScope,
  EnableStatus,
  roleManagementApi,
  RoleType,
  ValidityStatus,
} from '@/api'
import { Icon } from '~/components'
import {
  DATA_SCOPE_OPTIONS,
  PERMISSION_ACTION_OPTIONS,
  ROLE_TYPE_OPTIONS,
  STATUS_OPTIONS,
  VALIDITY_STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemRolePage' })

interface RoleFormModel extends RoleCreateDto {
  basicId?: RoleListItemDto['basicId']
}

const message = useMessage()

const tableLoading = ref(false)
const dataList = ref<RoleListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

const roleTypeOptions = ROLE_TYPE_OPTIONS

const maintainableRoleTypeOptions = [
  { label: '业务角色', value: RoleType.Business },
  { label: '自定义角色', value: RoleType.Custom },
]

const dataScopeOptions = DATA_SCOPE_OPTIONS

const validityStatusOptions = VALIDITY_STATUS_OPTIONS

const permissionActionOptions = PERMISSION_ACTION_OPTIONS

const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<RoleManagementDetailDto | null>(null)
const roleForm = ref<RoleFormModel>(createDefaultRoleForm())

const modalTitle = computed(() => (roleForm.value.basicId ? '编辑角色' : '新增角色'))

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

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

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }

  return value ? '是' : '否'
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(STATUS_OPTIONS, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions, value)
}

function canMaintainRole(row: RoleListItemDto) {
  return !row.isGlobal && row.roleType !== RoleType.System
}

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await roleManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: currentPage.value,
          pageSize: pageSize.value,
        },
      }),
      dataScope: queryParams.dataScope,
      isGlobal: toOptionalBoolean(queryParams.isGlobal),
      keyword: queryParams.keyword.trim() || null,
      roleType: queryParams.roleType,
      status: queryParams.status,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询角色失败')
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<RoleListItemDto>>(() => [
  { key: 'roleName', title: '角色名称', minWidth: 150, ellipsis: { tooltip: true }, sorter: true },
  { key: 'roleCode', title: '角色编码', minWidth: 150, ellipsis: { tooltip: true } },
  { key: 'roleDescription', title: '描述', minWidth: 220, ellipsis: { tooltip: true } },
  {
    key: 'roleType',
    title: '角色类型',
    minWidth: 110,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(roleTypeOptions, row.roleType))
    },
  },
  {
    key: 'isGlobal',
    title: '全局',
    width: 82,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.isGlobal ? 'warning' : 'default', bordered: false }, () => row.isGlobal ? '是' : '否')
    },
  },
  {
    key: 'dataScope',
    title: '数据范围',
    minWidth: 130,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(dataScopeOptions, row.dataScope))
    },
  },
  { key: 'maxMembers', title: '成员上限', minWidth: 100 },
  { key: 'sort', title: '排序', minWidth: 80, sorter: true },
  {
    key: 'status',
    title: '状态',
    width: 82,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => row.status === EnableStatus.Enabled ? '启用' : '禁用')
    },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 156,
    render(row) {
      return h(NSpace, { size: 'small' }, () => [
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, { ariaLabel: '查看详情', circle: true, quaternary: true, size: 'small', onClick: () => handleView(row) }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })),
            }),
          default: () => '查看详情',
        }),
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, { disabled: !canMaintainRole(row), ariaLabel: '编辑', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleEdit(row) }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })),
            }),
          default: () => '编辑',
        }),
        h(NPopconfirm, { disabled: !canMaintainRole(row), onPositiveClick: () => handleToggleStatus(row) }, {
          trigger: () =>
            h(NButton, { disabled: !canMaintainRole(row), ariaLabel: '停用或启用', circle: true, quaternary: true, size: 'small', type: 'warning' }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check' })),
            }),
          default: () => '确认更新角色状态？',
        }),
        h(NPopconfirm, { disabled: !canMaintainRole(row), onPositiveClick: () => handleDelete(row) }, {
          trigger: () =>
            h(NButton, { disabled: !canMaintainRole(row), ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error' }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })),
            }),
          default: () => '确认删除该角色？',
        }),
      ])
    },
  },
])

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  queryParams.roleType = undefined
  queryParams.dataScope = undefined
  queryParams.isGlobal = undefined
  currentPage.value = 1
  fetchData()
}

function handlePageChange(page: number) {
  currentPage.value = page
  fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  fetchData()
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

async function handleView(row: RoleListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await roleManagementApi.detailView(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到角色详情')
    }
  }
  catch {
    message.error('加载角色详情失败')
  }
  finally {
    detailLoading.value = false
  }
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

      await roleManagementApi.update(updateInput)
      if (editingStatus.value !== roleForm.value.status) {
        await roleManagementApi.updateStatus({
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

      await roleManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: RoleListItemDto) {
  await roleManagementApi.delete(row.basicId)
  message.success('删除成功')
  fetchData()
}

async function handleToggleStatus(row: RoleListItemDto) {
  await roleManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用角色' : '前端启用角色',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  fetchData()
}

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <NInput
        v-model:value="queryParams.keyword"
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

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <div style="padding:12px 16px;flex-shrink:0;">
        <NButton size="small" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>
          新增角色
        </NButton>
      </div>

      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row: RoleListItemDto) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />

      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页</div>
        <NPagination :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

    <NDrawer v-model:show="detailVisible" :width="900">
      <NDrawerContent closable title="角色详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无角色详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="角色名称">
                    {{ currentDetail.role.roleName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="角色编码">
                    {{ currentDetail.role.roleCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="角色类型">
                    {{ getOptionLabel(roleTypeOptions, currentDetail.role.roleType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="数据范围">
                    {{ getOptionLabel(dataScopeOptions, currentDetail.role.dataScope) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="全局角色">
                    {{ formatBoolean(currentDetail.role.isGlobal) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="状态">
                    {{ formatStatus(currentDetail.role.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="成员上限">
                    {{ currentDetail.role.maxMembers }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="排序">
                    {{ currentDetail.role.sort }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="描述">
                    {{ formatNullable(currentDetail.role.roleDescription) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="备注">
                    {{ formatNullable(currentDetail.role.remark) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.role.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="聚合时间">
                    {{ formatNullableDate(currentDetail.generatedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="permissions" :tab="`权限 (${currentDetail.permissions.length})`">
                <table v-if="currentDetail.permissions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>权限</th>
                      <th>编码</th>
                      <th>动作</th>
                      <th>状态</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.permissions" :key="item.basicId">
                      <td>{{ formatNullable(item.permissionName) }}</td>
                      <td>{{ formatNullable(item.permissionCode) }}</td>
                      <td>{{ getOptionLabel(permissionActionOptions, item.permissionAction) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无权限分配" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="dataScopes" :tab="`数据范围 (${currentDetail.dataScopes.length})`">
                <table v-if="currentDetail.dataScopes.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>部门</th>
                      <th>编码</th>
                      <th>包含子部门</th>
                      <th>状态</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.dataScopes" :key="item.basicId">
                      <td>{{ formatNullable(item.departmentName) }}</td>
                      <td>{{ formatNullable(item.departmentCode) }}</td>
                      <td>{{ formatBoolean(item.includeChildren) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无角色数据范围" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="ancestors" :tab="`祖先链 (${currentDetail.ancestors.length})`">
                <table v-if="currentDetail.ancestors.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>上级角色</th>
                      <th>编码</th>
                      <th>深度</th>
                      <th>状态</th>
                      <th>路径</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.ancestors" :key="item.basicId">
                      <td>{{ formatNullable(item.ancestorRoleName) }}</td>
                      <td>{{ formatNullable(item.ancestorRoleCode) }}</td>
                      <td>{{ item.depth }}</td>
                      <td>{{ formatStatus(item.ancestorStatus) }}</td>
                      <td>{{ formatNullable(item.path) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无祖先角色" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="descendants" :tab="`后代链 (${currentDetail.descendants.length})`">
                <table v-if="currentDetail.descendants.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>下级角色</th>
                      <th>编码</th>
                      <th>深度</th>
                      <th>状态</th>
                      <th>路径</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.descendants" :key="item.basicId">
                      <td>{{ formatNullable(item.descendantRoleName) }}</td>
                      <td>{{ formatNullable(item.descendantRoleCode) }}</td>
                      <td>{{ item.depth }}</td>
                      <td>{{ formatStatus(item.descendantStatus) }}</td>
                      <td>{{ formatNullable(item.path) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无后代角色" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="grantedUsers" :tab="`授权用户 (${currentDetail.grantedUsers.length})`">
                <table v-if="currentDetail.grantedUsers.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>用户</th>
                      <th>状态</th>
                      <th>已过期</th>
                      <th>授权原因</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.grantedUsers" :key="item.basicId">
                      <td>{{ formatNullable(item.realName || item.nickName || item.userName) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatBoolean(item.isExpired) }}</td>
                      <td>{{ formatNullable(item.grantReason) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无授权用户" style="padding: 40px 0" />
              </NTabPane>
            </NTabs>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

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

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-detail-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.xh-detail-table th,
.xh-detail-table td {
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  text-align: left;
  vertical-align: top;
}

.xh-detail-table th {
  background: var(--n-merged-th-color);
  font-weight: 500;
}
</style>
