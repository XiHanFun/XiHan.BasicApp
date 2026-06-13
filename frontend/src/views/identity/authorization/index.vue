<script setup lang="ts">
import type {
  ApiId,
  PageResult,
  PermissionDelegationCreateDto,
  PermissionDelegationListItemDto,
  PermissionRequestListItemDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDatePicker,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NRadioButton,
  NRadioGroup,
  NSelect,
  NSpace,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, ref } from 'vue'
import {
  createPageRequest,
  DelegationStatus,
  permissionApi,
  permissionDelegationApi,
  permissionRequestApi,
  PermissionRequestStatus,
  roleApi,
  userManagementApi,
} from '@/api'
import { SchemaPage } from '~/components'
import { DELEGATION_STATUS_OPTIONS, PERMISSION_REQUEST_STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemAuthorizationPage' })

interface NumericSelectOption {
  label: string
  value: ApiId
}

interface DelegationFormModel {
  delegateeUserId: ApiId | null
  delegationReason: string | null
  delegatorUserId: ApiId | null
  effectiveTime: number | null
  expirationTime: number | null
  permissionId: ApiId | null
  roleId: ApiId | null
  targetKind: 'role' | 'permission'
}

const message = useMessage()

const activeTab = ref('request')
const requestStatusOptions = PERMISSION_REQUEST_STATUS_OPTIONS
const delegationStatusOptions = DELEGATION_STATUS_OPTIONS

const requestPageRef = ref<{ reload: () => Promise<void> } | null>(null)
const delegationPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadRequest() {
  void requestPageRef.value?.reload()
}
function reloadDelegation() {
  void delegationPageRef.value?.reload()
}

function toStr(value: unknown) {
  return (value as string | undefined)?.trim() || undefined
}
function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '—'
}

const REQUEST_STATUS_TYPE: Record<PermissionRequestStatus, 'default' | 'info' | 'success' | 'warning' | 'error'> = {
  [PermissionRequestStatus.Pending]: 'warning',
  [PermissionRequestStatus.Approved]: 'success',
  [PermissionRequestStatus.Rejected]: 'error',
  [PermissionRequestStatus.Withdrawn]: 'default',
  [PermissionRequestStatus.Expired]: 'default',
}
const DELEGATION_STATUS_TYPE: Record<DelegationStatus, 'default' | 'info' | 'success' | 'warning' | 'error'> = {
  [DelegationStatus.Pending]: 'warning',
  [DelegationStatus.Active]: 'success',
  [DelegationStatus.Expired]: 'default',
  [DelegationStatus.Revoked]: 'error',
}

// ── 候选选项加载（用户 / 角色 / 权限） ──────────────────────────
const userOptions = ref<NumericSelectOption[]>([])
const roleOptions = ref<NumericSelectOption[]>([])
const permissionOptions = ref<NumericSelectOption[]>([])
const userLoading = ref(false)
const roleLoading = ref(false)
const permissionLoading = ref(false)

function mergeOptions(current: NumericSelectOption[], next: NumericSelectOption[]) {
  const map = new Map<ApiId, NumericSelectOption>()
  for (const option of current) {
    map.set(option.value, option)
  }
  for (const option of next) {
    map.set(option.value, option)
  }
  return [...map.values()]
}

async function loadUserOptions(keyword = '') {
  userLoading.value = true
  try {
    const result = await userManagementApi.page({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
      keyword: toStr(keyword),
    })
    userOptions.value = mergeOptions(
      userOptions.value,
      result.items.map(u => ({ label: `${u.realName || u.nickName || u.userName} (@${u.userName})`, value: u.basicId })),
    )
  }
  catch {
    message.error('加载用户候选失败')
  }
  finally {
    userLoading.value = false
  }
}

async function loadRoleOptions(keyword = '') {
  roleLoading.value = true
  try {
    const roles = await roleApi.enabledList({ limit: 50, keyword: normalizeNullable(keyword) })
    roleOptions.value = mergeOptions(roleOptions.value, roles.map(r => ({ label: `${r.roleName} (${r.roleCode})`, value: r.basicId })))
  }
  catch {
    message.error('加载角色候选失败')
  }
  finally {
    roleLoading.value = false
  }
}

async function loadPermissionOptions(keyword = '') {
  permissionLoading.value = true
  try {
    const perms = await permissionApi.availableGlobal({ limit: 50, keyword: normalizeNullable(keyword) })
    permissionOptions.value = mergeOptions(permissionOptions.value, perms.map(p => ({ label: `${p.permissionName} (${p.permissionCode})`, value: p.basicId })))
  }
  catch {
    message.error('加载权限候选失败')
  }
  finally {
    permissionLoading.value = false
  }
}

// ── 申请审批 ────────────────────────────────────────────────────
const requestFields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索申请人 / 原因', width: 240, order: 0 },
  {
    key: 'requestUserDisplayName',
    title: '申请人',
    dataType: 'string',
    minWidth: 140,
    order: 1,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return r.requestUserDisplayName || String(r.requestUserId)
    },
  },
  {
    key: 'target',
    title: '申请对象',
    dataType: 'string',
    minWidth: 160,
    order: 2,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return r.permissionName || r.roleName || '—'
    },
  },
  { key: 'requestReason', title: '申请原因', dataType: 'string', minWidth: 200, order: 3 },
  {
    key: 'requestStatus',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: requestStatusOptions,
    searchPlaceholder: '全部状态',
    width: 100,
    order: 4,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return h(
        NTag,
        { size: 'small', bordered: false, type: REQUEST_STATUS_TYPE[r.requestStatus] ?? 'default', style: { fontSize: '11px' } },
        () => getOptionLabel(requestStatusOptions, r.requestStatus),
      )
    },
  },
  {
    key: 'expectedExpirationTime',
    title: '期望有效期',
    dataType: 'string',
    minWidth: 190,
    order: 5,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return `${formatNullableDate(r.expectedEffectiveTime)} 至 ${formatNullableDate(r.expectedExpirationTime)}`
    },
  },
  { key: 'createdTime', title: '申请时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
]

function isPending(row: unknown) {
  return (row as PermissionRequestListItemDto).requestStatus === PermissionRequestStatus.Pending
}

const requestSchema: PageSchema = {
  pageCode: 'system.authorization.request',
  exportPermission: 'saas:permission-request:export',
  pageName: '授权申请',
  rowKey: 'basicId',
  scrollX: 1300,
  fields: requestFields,
  resource: {
    page: (params) => {
      const { keyword, requestStatus } = params.filters
      return permissionRequestApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(keyword),
        requestStatus: requestStatus as PermissionRequestStatus | undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => permissionRequestApi.delete(id),
  },
  actions: [
    { key: 'approve', title: '通过', scope: 'row', type: 'primary', visible: isPending },
    { key: 'reject', title: '驳回', scope: 'row', visible: isPending },
    { key: 'delete', title: '删除', scope: 'row' },
  ],
}

async function reviewRequest(row: PermissionRequestListItemDto, approved: boolean) {
  try {
    if (approved) {
      // 审批通过：后端走审批流并自动为申请人授予角色/权限
      await permissionRequestApi.approve({ basicId: row.basicId, remark: '审批通过' })
    }
    else {
      await permissionRequestApi.reject({ basicId: row.basicId, remark: '审批驳回' })
    }
    message.success(approved ? '已通过并授权' : '已驳回')
    reloadRequest()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
}

function onRequestAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as PermissionRequestListItemDto | undefined
  switch (payload.key) {
    case 'approve':
      if (row) {
        void reviewRequest(row, true)
      }
      break
    case 'reject':
      if (row) {
        void reviewRequest(row, false)
      }
      break
    case 'delete':
      if (row) {
        void handleDeleteRequest(row)
      }
      break
  }
}

async function handleDeleteRequest(row: PermissionRequestListItemDto) {
  await permissionRequestApi.delete(row.basicId)
  message.success('删除成功')
  reloadRequest()
}

// ── 委托 ────────────────────────────────────────────────────────
const delegationFields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索委托人 / 接收人', width: 240, order: 0 },
  {
    key: 'delegatorDisplayName',
    title: '委托人',
    dataType: 'string',
    minWidth: 130,
    order: 1,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return r.delegatorDisplayName || String(r.delegatorUserId)
    },
  },
  {
    key: 'delegateeDisplayName',
    title: '接收人',
    dataType: 'string',
    minWidth: 130,
    order: 2,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return r.delegateeDisplayName || String(r.delegateeUserId)
    },
  },
  {
    key: 'target',
    title: '委托对象',
    dataType: 'string',
    minWidth: 160,
    order: 3,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return r.permissionName || r.roleName || '—'
    },
  },
  {
    key: 'delegationStatus',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: delegationStatusOptions,
    searchPlaceholder: '全部状态',
    width: 100,
    order: 4,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return h(
        NTag,
        { size: 'small', bordered: false, type: DELEGATION_STATUS_TYPE[r.delegationStatus] ?? 'default', style: { fontSize: '11px' } },
        () => getOptionLabel(delegationStatusOptions, r.delegationStatus),
      )
    },
  },
  {
    key: 'expirationTime',
    title: '有效期',
    dataType: 'string',
    minWidth: 190,
    order: 5,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return `${formatNullableDate(r.effectiveTime)} 至 ${formatNullableDate(r.expirationTime)}`
    },
  },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
]

function canRevoke(row: unknown) {
  const status = (row as PermissionDelegationListItemDto).delegationStatus
  return status === DelegationStatus.Active || status === DelegationStatus.Pending
}

const delegationSchema: PageSchema = {
  pageCode: 'system.authorization.delegation',
  pageName: '权限委托',
  rowKey: 'basicId',
  scrollX: 1300,
  fields: delegationFields,
  resource: {
    page: (params) => {
      const { keyword, delegationStatus } = params.filters
      return permissionDelegationApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(keyword),
        delegationStatus: delegationStatus as DelegationStatus | undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => permissionDelegationApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增委托', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'revoke', title: '撤销', scope: 'row', visible: canRevoke },
    { key: 'delete', title: '删除', scope: 'row' },
  ],
}

const delegationModalVisible = ref(false)
const delegationSubmitting = ref(false)
const delegationForm = ref<DelegationFormModel>(createDelegationForm())

function createDelegationForm(): DelegationFormModel {
  return {
    delegateeUserId: null,
    delegationReason: null,
    delegatorUserId: null,
    effectiveTime: null,
    expirationTime: null,
    permissionId: null,
    roleId: null,
    targetKind: 'role',
  }
}

function onDelegationAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as PermissionDelegationListItemDto | undefined
  switch (payload.key) {
    case 'create':
      openDelegationModal()
      break
    case 'revoke':
      if (row) {
        void revokeDelegation(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDeleteDelegation(row)
      }
      break
  }
}

function openDelegationModal() {
  delegationForm.value = createDelegationForm()
  delegationModalVisible.value = true
  void loadUserOptions()
  void loadRoleOptions()
  void loadPermissionOptions()
}

async function revokeDelegation(row: PermissionDelegationListItemDto) {
  try {
    await permissionDelegationApi.updateStatus({
      basicId: row.basicId,
      delegationStatus: DelegationStatus.Revoked,
      remark: '前端撤销委托',
    })
    message.success('已撤销')
    reloadDelegation()
  }
  catch {
    message.error('撤销失败')
  }
}

async function handleDeleteDelegation(row: PermissionDelegationListItemDto) {
  await permissionDelegationApi.delete(row.basicId)
  message.success('删除成功')
  reloadDelegation()
}

function validateDelegation() {
  const form = delegationForm.value
  if (!form.delegatorUserId) {
    message.warning('请选择委托人')
    return false
  }
  if (!form.delegateeUserId) {
    message.warning('请选择接收人')
    return false
  }
  if (form.targetKind === 'role' && !form.roleId) {
    message.warning('请选择委托角色')
    return false
  }
  if (form.targetKind === 'permission' && !form.permissionId) {
    message.warning('请选择委托权限')
    return false
  }
  if (!form.expirationTime) {
    message.warning('请选择到期时间')
    return false
  }
  return true
}

async function submitDelegation() {
  if (!validateDelegation()) {
    return
  }
  const form = delegationForm.value
  delegationSubmitting.value = true
  try {
    const input: PermissionDelegationCreateDto = {
      delegatorUserId: form.delegatorUserId!,
      delegateeUserId: form.delegateeUserId!,
      delegationReason: normalizeNullable(form.delegationReason),
      effectiveTime: form.effectiveTime ? new Date(form.effectiveTime).toISOString() : null,
      expirationTime: new Date(form.expirationTime!).toISOString(),
      permissionId: form.targetKind === 'permission' ? form.permissionId : null,
      roleId: form.targetKind === 'role' ? form.roleId : null,
    }
    await permissionDelegationApi.create(input)
    message.success('已创建委托')
    delegationModalVisible.value = false
    reloadDelegation()
  }
  catch {
    message.error('创建失败')
  }
  finally {
    delegationSubmitting.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <NTabs v-model:value="activeTab" animated type="line">
      <NTabPane name="request" tab="授权申请">
        <SchemaPage ref="requestPageRef" :schema="requestSchema" @action="onRequestAction" />
      </NTabPane>
      <NTabPane name="delegation" tab="权限委托">
        <SchemaPage ref="delegationPageRef" :schema="delegationSchema" @action="onDelegationAction">
          <NModal
            v-model:show="delegationModalVisible"
            :auto-focus="false"
            :bordered="false"
            preset="card"
            title="新增权限委托"
            style="width: 640px; max-width: 92vw"
          >
            <NForm :model="delegationForm" class="xh-edit-form-grid" label-placement="top">
              <NFormItem label="委托人" path="delegatorUserId">
                <NSelect
                  v-model:value="delegationForm.delegatorUserId"
                  clearable
                  filterable
                  :loading="userLoading"
                  :options="userOptions"
                  placeholder="搜索并选择委托人"
                  remote
                  @focus="loadUserOptions()"
                  @search="(kw: string) => loadUserOptions(kw)"
                />
              </NFormItem>
              <NFormItem label="接收人" path="delegateeUserId">
                <NSelect
                  v-model:value="delegationForm.delegateeUserId"
                  clearable
                  filterable
                  :loading="userLoading"
                  :options="userOptions"
                  placeholder="搜索并选择接收人"
                  remote
                  @focus="loadUserOptions()"
                  @search="(kw: string) => loadUserOptions(kw)"
                />
              </NFormItem>
              <NFormItem label="委托类型" path="targetKind">
                <NRadioGroup v-model:value="delegationForm.targetKind">
                  <NRadioButton value="role">
                    角色
                  </NRadioButton>
                  <NRadioButton value="permission">
                    权限
                  </NRadioButton>
                </NRadioGroup>
              </NFormItem>
              <NFormItem v-if="delegationForm.targetKind === 'role'" label="委托角色" path="roleId">
                <NSelect
                  v-model:value="delegationForm.roleId"
                  clearable
                  filterable
                  :loading="roleLoading"
                  :options="roleOptions"
                  placeholder="搜索并选择角色"
                  remote
                  @focus="loadRoleOptions()"
                  @search="(kw: string) => loadRoleOptions(kw)"
                />
              </NFormItem>
              <NFormItem v-else label="委托权限" path="permissionId">
                <NSelect
                  v-model:value="delegationForm.permissionId"
                  clearable
                  filterable
                  :loading="permissionLoading"
                  :options="permissionOptions"
                  placeholder="搜索并选择权限"
                  remote
                  @focus="loadPermissionOptions()"
                  @search="(kw: string) => loadPermissionOptions(kw)"
                />
              </NFormItem>
              <NFormItem label="生效时间" path="effectiveTime">
                <NDatePicker v-model:value="delegationForm.effectiveTime" clearable style="width: 100%" type="datetime" />
              </NFormItem>
              <NFormItem label="到期时间" path="expirationTime">
                <NDatePicker v-model:value="delegationForm.expirationTime" clearable style="width: 100%" type="datetime" />
              </NFormItem>
              <NFormItem label="委托原因" path="delegationReason">
                <NInput
                  v-model:value="delegationForm.delegationReason"
                  clearable
                  placeholder="请输入委托原因"
                  :rows="2"
                  type="textarea"
                />
              </NFormItem>
            </NForm>

            <template #footer>
              <NSpace justify="end">
                <NButton @click="delegationModalVisible = false">
                  取消
                </NButton>
                <NButton :loading="delegationSubmitting" type="primary" @click="submitDelegation">
                  保存
                </NButton>
              </NSpace>
            </template>
          </NModal>
        </SchemaPage>
      </NTabPane>
    </NTabs>
  </div>
</template>

<style scoped>
.auth-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 定高表格卡片，分页贴底）。
   页面主体被 NTabs 包裹时高度链在 tab pane 处断裂，这里补全传递链，
   使其与用户管理等根级 SchemaPage 页面布局一致。 */
.auth-page :deep(.n-tabs) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* 标签条与下方 SchemaPage 内容（p-3 = 12px）左对齐，并留出上方呼吸空间 */
.auth-page :deep(.n-tabs-nav) {
  padding: 8px 12px 0;
}

.auth-page :deep(.n-tabs-pane-wrapper) {
  flex: 1;
  height: 0;
}

.auth-page :deep(.n-tab-pane) {
  height: 100%;
  padding: 0;
}
</style>
