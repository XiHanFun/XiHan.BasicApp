<script setup lang="ts">
import type {
  ApiId,
  PageResult,
  PermissionDelegationCreateDto,
  PermissionDelegationListItemDto,
  PermissionRequestListItemDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
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
import { DELEGATION_STATUS_OPTIONS, PERMISSION_REQUEST_STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XDatePicker, XEditModal, XInput, XSegmented, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemAuthorizationPage' })

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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

const activeTab = ref('request')
const requestStatusOptions = useEnumOptions('PermissionRequestStatus', PERMISSION_REQUEST_STATUS_OPTIONS)
const delegationStatusOptions = useEnumOptions('DelegationStatus', DELEGATION_STATUS_OPTIONS)

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

const REQUEST_STATUS_TYPE: Record<PermissionRequestStatus, 'neutral' | 'info' | 'success' | 'warning' | 'danger'> = {
  [PermissionRequestStatus.Pending]: 'warning',
  [PermissionRequestStatus.Approved]: 'success',
  [PermissionRequestStatus.Rejected]: 'danger',
  [PermissionRequestStatus.Withdrawn]: 'neutral',
  [PermissionRequestStatus.Expired]: 'neutral',
}
const DELEGATION_STATUS_TYPE: Record<DelegationStatus, 'neutral' | 'info' | 'success' | 'warning' | 'danger'> = {
  [DelegationStatus.Pending]: 'warning',
  [DelegationStatus.Active]: 'success',
  [DelegationStatus.Expired]: 'neutral',
  [DelegationStatus.Revoked]: 'danger',
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
  catch (error) {
    toast.error((error as Error)?.message || t('identity.authorization.msg_load_user_failed'))
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
  catch (error) {
    toast.error((error as Error)?.message || t('identity.authorization.msg_load_role_failed'))
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
  catch (error) {
    toast.error((error as Error)?.message || t('identity.authorization.msg_load_permission_failed'))
  }
  finally {
    permissionLoading.value = false
  }
}

// ── 申请审批 ────────────────────────────────────────────────────
const requestFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.authorization.req_col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.authorization.req_keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'requestUserDisplayName',
    title: t('identity.authorization.req_col_applicant'),
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
    title: t('identity.authorization.req_col_target'),
    dataType: 'string',
    minWidth: 160,
    order: 2,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return r.permissionName || r.roleName || '—'
    },
  },
  { key: 'requestReason', title: t('identity.authorization.req_col_reason'), dataType: 'string', minWidth: 200, order: 3 },
  {
    key: 'requestStatus',
    title: t('identity.authorization.req_col_status'),
    dataType: 'enum',
    searchable: true,
    dictionaryCode: 'PermissionRequestStatus',
    options: requestStatusOptions.value,
    searchPlaceholder: t('identity.authorization.req_status_placeholder'),
    width: 100,
    order: 4,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return h(XhTagRoot, { variant: 'outline', tone: REQUEST_STATUS_TYPE[r.requestStatus] ?? 'neutral' }, () => h(XhTagLabel, () => getOptionLabel(requestStatusOptions.value, r.requestStatus)))
    },
  },
  {
    key: 'expectedExpirationTime',
    title: t('identity.authorization.req_col_expected_validity'),
    dataType: 'string',
    minWidth: 190,
    order: 5,
    render: (row) => {
      const r = row as unknown as PermissionRequestListItemDto
      return t('identity.authorization.validity_range', { from: formatNullableDate(r.expectedEffectiveTime), to: formatNullableDate(r.expectedExpirationTime) })
    },
  },
  { key: 'createdTime', title: t('identity.authorization.req_col_create_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
])

function isPending(row: unknown) {
  return (row as PermissionRequestListItemDto).requestStatus === PermissionRequestStatus.Pending
}

const requestSchema = computed<PageSchema>(() => ({
  pageCode: 'system.authorization.request',
  exportPermission: 'identity.authorization.export',
  pageName: t('identity.authorization.req_page_name'),
  rowKey: 'basicId',
  fields: requestFields.value,
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
    { key: 'approve', title: t('identity.authorization.req_action_approve'), scope: 'row', type: 'primary', visible: isPending },
    { key: 'reject', title: t('identity.authorization.req_action_reject'), scope: 'row', visible: isPending },
    { key: 'delete', title: t('identity.authorization.req_action_delete'), scope: 'row' },
  ],
}))

async function reviewRequest(row: PermissionRequestListItemDto, approved: boolean) {
  try {
    if (approved) {
      // 审批通过：后端走审批流并自动为申请人授予角色/权限
      await permissionRequestApi.approve({ basicId: row.basicId, remark: t('identity.authorization.req_approve_remark') })
    }
    else {
      await permissionRequestApi.reject({ basicId: row.basicId, remark: t('identity.authorization.req_reject_remark') })
    }
    toast.success(approved ? t('identity.authorization.req_approved') : t('identity.authorization.req_rejected'))
    reloadRequest()
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('common.messages.operation_failed'))
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
  toast.success(t('common.messages.delete_success'))
  reloadRequest()
}

// ── 委托 ────────────────────────────────────────────────────────
const delegationFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.authorization.del_col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.authorization.del_keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'delegatorDisplayName',
    title: t('identity.authorization.del_col_delegator'),
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
    title: t('identity.authorization.del_col_delegatee'),
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
    title: t('identity.authorization.del_col_target'),
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
    title: t('identity.authorization.del_col_status'),
    dataType: 'enum',
    searchable: true,
    dictionaryCode: 'DelegationStatus',
    options: delegationStatusOptions.value,
    searchPlaceholder: t('identity.authorization.del_status_placeholder'),
    width: 100,
    order: 4,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return h(XhTagRoot, { variant: 'outline', tone: DELEGATION_STATUS_TYPE[r.delegationStatus] ?? 'neutral' }, () => h(XhTagLabel, () => getOptionLabel(delegationStatusOptions.value, r.delegationStatus)))
    },
  },
  {
    key: 'expirationTime',
    title: t('identity.authorization.del_col_validity'),
    dataType: 'string',
    minWidth: 190,
    order: 5,
    render: (row) => {
      const r = row as unknown as PermissionDelegationListItemDto
      return t('identity.authorization.validity_range', { from: formatNullableDate(r.effectiveTime), to: formatNullableDate(r.expirationTime) })
    },
  },
  { key: 'createdTime', title: t('identity.authorization.del_col_create_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
])

function canRevoke(row: unknown) {
  const status = (row as PermissionDelegationListItemDto).delegationStatus
  return status === DelegationStatus.Active || status === DelegationStatus.Pending
}

const delegationSchema = computed<PageSchema>(() => ({
  pageCode: 'system.authorization.delegation',
  pageName: t('identity.authorization.del_page_name'),
  rowKey: 'basicId',
  fields: delegationFields.value,
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
    { key: 'create', title: t('identity.authorization.del_action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'revoke', title: t('identity.authorization.del_action_revoke'), scope: 'row', visible: canRevoke },
    { key: 'delete', title: t('identity.authorization.del_action_delete'), scope: 'row' },
  ],
}))

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
      remark: t('identity.authorization.del_revoke_remark'),
    })
    toast.success(t('identity.authorization.del_revoked'))
    reloadDelegation()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('identity.authorization.del_revoke_failed'))
  }
}

async function handleDeleteDelegation(row: PermissionDelegationListItemDto) {
  await permissionDelegationApi.delete(row.basicId)
  toast.success(t('common.messages.delete_success'))
  reloadDelegation()
}

function validateDelegation() {
  const form = delegationForm.value
  if (!form.delegatorUserId) {
    toast.warning(t('identity.authorization.msg_delegator_required'))
    return false
  }
  if (!form.delegateeUserId) {
    toast.warning(t('identity.authorization.msg_delegatee_required'))
    return false
  }
  if (form.targetKind === 'role' && !form.roleId) {
    toast.warning(t('identity.authorization.msg_role_required'))
    return false
  }
  if (form.targetKind === 'permission' && !form.permissionId) {
    toast.warning(t('identity.authorization.msg_permission_required'))
    return false
  }
  if (!form.expirationTime) {
    toast.warning(t('identity.authorization.msg_expiration_required'))
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
    toast.success(t('identity.authorization.msg_created'))
    delegationModalVisible.value = false
    reloadDelegation()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('identity.authorization.msg_create_failed'))
  }
  finally {
    delegationSubmitting.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line">
      <XhTabsList>
        <XhTabsTrigger value="request">
          {{ t('identity.authorization.tab_request') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="delegation">
          {{ t('identity.authorization.tab_delegation') }}
        </XhTabsTrigger>
      </XhTabsList>
      <XhTabsContent value="request">
        <SchemaPage ref="requestPageRef" :schema="requestSchema" @action="onRequestAction" />
      </XhTabsContent>
      <XhTabsContent value="delegation">
        <SchemaPage ref="delegationPageRef" :schema="delegationSchema" @action="onDelegationAction">
          <XEditModal
            v-model:show="delegationModalVisible"
            :title="t('identity.authorization.modal_title')"
            :loading="delegationSubmitting"
            :form-id="editFormId"
          >
            <XhFormRoot
              :id="editFormId"
              v-model:values="delegationForm"
              validate-on="blur"
              class="xh-edit-form-grid"
              @submit="submitDelegation"
            >
              <XhFormFieldGroup value="delegatorUserId">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_delegator') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XSelect
                      v-model:value="delegationForm.delegatorUserId"
                      clearable
                      :options="userOptions"
                      :placeholder="t('identity.authorization.ph_delegator')"
                      @focus="loadUserOptions()"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="delegateeUserId">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_delegatee') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XSelect
                      v-model:value="delegationForm.delegateeUserId"
                      clearable
                      :options="userOptions"
                      :placeholder="t('identity.authorization.ph_delegatee')"
                      @focus="loadUserOptions()"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="targetKind">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_target_kind') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XSegmented v-model:value="delegationForm.targetKind" :options="[{ value: 'role', label: t('identity.authorization.kind_role') }, { value: 'permission', label: t('identity.authorization.kind_permission') }]" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup v-if="delegationForm.targetKind === 'role'" value="roleId">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_role') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XSelect
                      v-model:value="delegationForm.roleId"
                      clearable
                      :options="roleOptions"
                      :placeholder="t('identity.authorization.ph_role')"
                      @focus="loadRoleOptions()"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup v-else value="permissionId">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_permission') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XSelect
                      v-model:value="delegationForm.permissionId"
                      clearable
                      :options="permissionOptions"
                      :placeholder="t('identity.authorization.ph_permission')"
                      @focus="loadPermissionOptions()"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="effectiveTime">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_effective_time') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XDatePicker v-model:value="delegationForm.effectiveTime" clearable type="datetime" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="expirationTime">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_expiration_time') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XDatePicker v-model:value="delegationForm.expirationTime" clearable type="datetime" />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
              <XhFormFieldGroup value="delegationReason" class="xh-span-2">
                <XhFieldRoot>
                  <XhFieldLabel>{{ t('identity.authorization.label_reason') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XInput
                      v-model:value="delegationForm.delegationReason"
                      clearable
                      :placeholder="t('identity.authorization.ph_reason')"
                      :rows="2"
                      type="textarea"
                    />
                  </XhFieldControl>
                  <XhFieldErrorText />
                </XhFieldRoot>
              </XhFormFieldGroup>
            </XhFormRoot>
          </XEditModal>
        </SchemaPage>
      </XhTabsContent>
    </XhTabsRoot>
  </div>
</template>

<style scoped>
.auth-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 定高表格卡片，分页贴底）。
   页面主体被 NTabs 包裹时高度链在 tab pane 处断裂，这里补全传递链，
   使其与用户管理等根级 SchemaPage 页面布局一致。 */
.auth-page :deep([data-scope='tabs'][data-part='root']) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* 标签条与下方 SchemaPage 内容（p-3 = 12px）左对齐，并留出上方呼吸空间 */
.auth-page :deep([data-scope='tabs'][data-part='list']) {
  padding: 8px 12px 0;
}

.auth-page :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  height: 0;
}

.auth-page :deep([data-scope='tabs'][data-part='content']) {
  padding: 0;
}
</style>
