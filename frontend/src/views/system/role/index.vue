<script setup lang="ts">
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type {
  PageResult,
  RoleCreateDto,
  RoleListItemDto,
  RoleManagementDetailDto,
  RoleUpdateDto,
  ValidityStatus,
} from '@/api'
import {
  NButton,
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
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import {
  createPageRequest,
  DataPermissionScope,
  EnableStatus,
  roleManagementApi,
  RoleType,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
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

const statusOptions = STATUS_OPTIONS
const roleTypeOptions = ROLE_TYPE_OPTIONS
const dataScopeOptions = DATA_SCOPE_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS
const permissionActionOptions = PERMISSION_ACTION_OPTIONS

const globalOptions = [
  { label: '全局角色', value: 1 },
  { label: '租户角色', value: 0 },
]

const maintainableRoleTypeOptions = [
  { label: '业务角色', value: RoleType.Business },
  { label: '自定义角色', value: RoleType.Custom },
]

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadRole() {
  void schemaPageRef.value?.reload()
}

// ── 过滤值清洗辅助 ──────────────────────────────────────────────
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  if (v == null || v === '') {
    return undefined
  }
  return Number(v) === 1
}

function canMaintainRole(row: RoleListItemDto) {
  return !row.isGlobal && row.roleType !== RoleType.System
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不展示）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索角色名称/编码', width: 240, order: 0 },
  { key: 'roleName', title: '角色名称', dataType: 'string', sortable: true, minWidth: 150, order: 1 },
  { key: 'roleCode', title: '角色编码', dataType: 'string', minWidth: 150, order: 2 },
  { key: 'roleDescription', title: '描述', dataType: 'string', minWidth: 220, order: 3 },
  {
    key: 'roleType',
    title: '角色类型',
    dataType: 'enum',
    searchable: true,
    options: roleTypeOptions,
    searchPlaceholder: '角色类型',
    minWidth: 110,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(roleTypeOptions, (row as unknown as RoleListItemDto).roleType)),
  },
  {
    key: 'isGlobal',
    title: '全局',
    dataType: 'boolean',
    searchable: true,
    options: globalOptions,
    searchPlaceholder: '全局',
    width: 82,
    order: 5,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as RoleListItemDto).isGlobal ? 'warning' : 'default', bordered: false }, () => (row as unknown as RoleListItemDto).isGlobal ? '是' : '否'),
  },
  {
    key: 'dataScope',
    title: '数据范围',
    dataType: 'enum',
    searchable: true,
    options: dataScopeOptions,
    searchPlaceholder: '数据范围',
    minWidth: 130,
    order: 6,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(dataScopeOptions, (row as unknown as RoleListItemDto).dataScope)),
  },
  { key: 'maxMembers', title: '成员上限', dataType: 'number', minWidth: 100, order: 7 },
  { key: 'sort', title: '排序', dataType: 'number', sortable: true, minWidth: 80, order: 8 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 82,
    order: 9,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? '启用' : '禁用'),
  },
  {
    key: 'createdTime',
    title: '创建时间',
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 10,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate((row as unknown as RoleListItemDto).createdTime)),
  },
]

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema: PageSchema = {
  pageCode: 'system.role',
  pageName: '角色管理',
  rowKey: 'basicId',
  scrollX: 1600,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return roleManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword) ?? null,
        // RoleType / DataPermissionScope / EnableStatus 均为后端字符串枚举，原样透传即可
        roleType: (f.roleType as RoleType | undefined) || undefined,
        dataScope: (f.dataScope as DataPermissionScope | undefined) || undefined,
        isGlobal: toBool(f.isGlobal),
        status: (f.status as EnableStatus | undefined) || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => roleManagementApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增角色', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'edit', title: '编辑', scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
    { key: 'toggle', title: '启用/停用', scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
    { key: 'delete', title: '删除', scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as RoleListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

// ── 表单 / 详情（保留页面自有逻辑） ─────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<RoleManagementDetailDto | null>(null)
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
  return getOptionLabel(statusOptions, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions, value)
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
    reloadRole()
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
  reloadRole()
}

async function handleToggleStatus(row: RoleListItemDto) {
  await roleManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用角色' : '前端启用角色',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  reloadRole()
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
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
          <NSelect v-model:value="roleForm.status" :options="statusOptions" />
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
  </SchemaPage>
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
