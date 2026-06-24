<script setup lang="ts">
import type {
  ApiId,
  PageResult,
  TenantCreateDto,
  TenantDetailDto,
  TenantListItemDto,
  TenantMemberListItemDto,
  TenantMemberStatusUpdateDto,
  TenantMemberUpdateDto,
  TenantUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDatePicker,
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
import { useI18n } from 'vue-i18n'
import {
  createDefaultQueryBehavior,
  createPageRequest,
  querySortsFromSchema,
  TenantConfigStatus,
  TenantIsolationMode,
  tenantManagementApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  TenantStatus,
  ValidityStatus,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import {
  MEMBER_INVITE_STATUS_OPTIONS,
  MEMBER_TYPE_OPTIONS,
  TENANT_CONFIG_STATUS_OPTIONS,
  TENANT_ISOLATION_MODE_OPTIONS,
  TENANT_STATUS_OPTIONS,
  VALIDITY_STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformTenantPage' })

interface TenantFormModel extends TenantCreateDto {
  basicId?: ApiId
  tenantStatus?: TenantStatus
}

const message = useMessage()
const { t } = useI18n()

const tenantStatusOptions = TENANT_STATUS_OPTIONS
const configStatusOptions = TENANT_CONFIG_STATUS_OPTIONS
const isolationModeOptions = TENANT_ISOLATION_MODE_OPTIONS
const memberTypeOptions = MEMBER_TYPE_OPTIONS
const inviteStatusOptions = MEMBER_INVITE_STATUS_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS

const expiredOptions = computed(() => [
  { label: t('tenant.list.yes'), value: 1 },
  { label: t('tenant.list.no'), value: 0 },
])

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadTenant() {
  void schemaPageRef.value?.reload()
}

// ── 弹窗/表单状态(保留全部增删改) ───────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<TenantStatus | null>(null)
const tenantForm = ref<TenantFormModel>(createDefaultForm())
const modalTitle = computed(() => (tenantForm.value.basicId ? t('tenant.list.edit_title') : t('tenant.list.add_title')))

const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<TenantDetailDto | null>(null)

const memberLoading = ref(false)
const memberError = ref(false)
const members = ref<TenantMemberListItemDto[]>([])
const memberEditVisible = ref(false)
const memberEditLoading = ref(false)
const editingMember = ref<TenantMemberUpdateDto | null>(null)
const editingMemberId = ref<ApiId | null>(null)
const memberStatusVisible = ref(false)
const memberStatusLoading = ref(false)
const editingMemberStatusId = ref<ApiId | null>(null)
const editingMemberStatus = ref<ValidityStatus>(ValidityStatus.Valid)

function getTenantStatusTagType(status: TenantStatus) {
  if (status === TenantStatus.Normal) {
    return 'success'
  }
  if (status === TenantStatus.Disabled) {
    return 'error'
  }
  return 'warning'
}

// ── 字段单一事实源:列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索(不作为列)
  { key: 'keyword', title: t('tenant.list.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('tenant.list.keyword_placeholder'), width: 250, order: 0 },
  // 常用搜索 + 列
  {
    key: 'tenantName',
    title: t('tenant.list.tenant_name'),
    dataType: 'string',
    sortable: true,
    minWidth: 160,
    order: 1,
  },
  { key: 'tenantCode', title: t('tenant.list.tenant_code'), dataType: 'string', sortable: true, minWidth: 150, order: 2 },
  { key: 'tenantShortName', title: t('tenant.list.tenant_short_name'), dataType: 'string', sortable: true, minWidth: 130, order: 3 },
  { key: 'domain', title: t('tenant.list.domain'), dataType: 'string', sortable: true, minWidth: 180, order: 4 },
  {
    key: 'isolationMode',
    title: t('tenant.list.isolation_mode'),
    dataType: 'enum',
    dictionaryCode: 'TenantIsolationMode',
    options: isolationModeOptions,
    minWidth: 120,
    order: 5,
  },
  { key: 'editionId', title: t('tenant.list.edition'), dataType: 'string', minWidth: 100, order: 6 },
  {
    key: 'tenantStatus',
    title: t('tenant.list.tenant_status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    dictionaryCode: 'TenantStatus',
    options: tenantStatusOptions,
    searchPlaceholder: t('tenant.list.tenant_status_placeholder'),
    width: 100,
    order: 7,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: getTenantStatusTagType(r.tenantStatus) }, () => getOptionLabel(tenantStatusOptions, r.tenantStatus))
    },
  },
  {
    key: 'configStatus',
    title: t('tenant.list.config_status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    dictionaryCode: 'TenantConfigStatus',
    options: configStatusOptions,
    searchPlaceholder: t('tenant.list.config_status_placeholder'),
    minWidth: 110,
    order: 8,
  },
  {
    key: 'isExpired',
    title: t('tenant.list.is_expired'),
    dataType: 'boolean',
    options: expiredOptions.value,
    width: 82,
    order: 9,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.isExpired ? 'error' : 'success' }, () => (r.isExpired ? t('tenant.list.yes') : t('tenant.list.no')))
    },
  },
  { key: 'userLimit', title: t('tenant.list.user_limit'), dataType: 'number', sortable: true, minWidth: 100, order: 10 },
  { key: 'storageLimit', title: t('tenant.list.storage_limit'), dataType: 'number', sortable: true, minWidth: 120, order: 11 },
  { key: 'sort', title: t('tenant.list.sort'), dataType: 'number', sortable: true, minWidth: 80, order: 12 },
  { key: 'expirationTime', title: t('tenant.list.expiration_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 13 },
  { key: 'createdTime', title: t('tenant.list.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 14 },
  // 仅高级搜索(不作为列)
  { key: 'editionIdFilter', title: t('tenant.list.edition_id'), dataType: 'string', visible: false, advancedSearch: true, searchPlaceholder: t('tenant.list.edition_id_filter_placeholder'), order: 20 },
  { key: 'expirationTimeStart', title: t('tenant.list.expiration_time_start'), dataType: 'date', visible: false, advancedSearch: true, searchPlaceholder: t('tenant.list.expiration_time_start'), order: 21 },
  { key: 'expirationTimeEnd', title: t('tenant.list.expiration_time_end'), dataType: 'date', visible: false, advancedSearch: true, searchPlaceholder: t('tenant.list.expiration_time_end'), order: 22 },
])

/** 过滤值辅助:trim 字符串 / 时间戳转 yyyy-MM-dd */
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toDate(v: unknown): string | null {
  if (v == null || v === '') {
    return null
  }
  if (typeof v === 'number') {
    return formatDate(new Date(v).toISOString(), 'YYYY-MM-DD')
  }
  return String(v)
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.tenant',
  exportPermission: 'saas:tenant:export',
  pageName: t('tenant.list.page_name'),
  rowKey: 'basicId',
  scrollX: 2000,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return tenantManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts) },
        }),
        keyword: toStr(f.keyword) ?? null,
        tenantStatus: (f.tenantStatus as TenantStatus | undefined) ?? undefined,
        configStatus: (f.configStatus as TenantConfigStatus | undefined) ?? undefined,
        editionId: toStr(f.editionIdFilter) ?? null,
        expirationTimeStart: toDate(f.expirationTimeStart),
        expirationTimeEnd: toDate(f.expirationTimeEnd),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: t('tenant.list.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('tenant.list.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('tenant.list.edit'), scope: 'row' },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as TenantListItemDto | undefined
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
  }
}

function createDefaultForm(): TenantFormModel {
  return {
    adminEmail: null,
    adminPassword: null,
    adminUserName: null,
    domain: null,
    editionId: null,
    expirationTime: null,
    isolationMode: TenantIsolationMode.Field,
    logo: null,
    remark: null,
    sort: 100,
    storageLimit: null,
    tenantCode: '',
    tenantName: '',
    tenantShortName: null,
    tenantStatus: TenantStatus.Normal,
    userLimit: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
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
  return value ? t('tenant.list.yes') : t('tenant.list.no')
}

function handleAdd() {
  editingStatus.value = null
  tenantForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: TenantListItemDto) {
  editingStatus.value = row.tenantStatus
  tenantForm.value = {
    basicId: row.basicId,
    domain: row.domain ?? null,
    editionId: row.editionId ?? null,
    expirationTime: row.expirationTime ?? null,
    isolationMode: row.isolationMode,
    logo: row.logo ?? null,
    remark: null,
    sort: row.sort,
    storageLimit: row.storageLimit ?? null,
    tenantCode: row.tenantCode,
    tenantName: row.tenantName,
    tenantShortName: row.tenantShortName ?? null,
    tenantStatus: row.tenantStatus,
    userLimit: row.userLimit ?? null,
  }
  modalVisible.value = true
}

async function handleView(row: TenantListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  members.value = []
  try {
    currentDetail.value = await tenantManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning(t('tenant.list.detail_not_found'))
    }
  }
  catch {
    message.error(t('tenant.list.detail_load_failed'))
  }
  finally {
    detailLoading.value = false
  }
  loadMembers()
}

async function loadMembers() {
  if (!currentDetail.value) {
    return
  }
  memberLoading.value = true
  memberError.value = false
  try {
    const result = await tenantManagementApi.members.page({
      ...createPageRequest({
        behavior: createDefaultQueryBehavior({ ignoreTenant: true }),
        page: { pageIndex: 1, pageSize: 200 },
      }),
    })
    members.value = result.items
  }
  catch {
    memberError.value = true
    members.value = []
    message.error(t('tenant.list.member_list_load_failed'))
  }
  finally {
    memberLoading.value = false
  }
}

function handleEditMember(row: TenantMemberListItemDto) {
  editingMemberId.value = row.basicId
  editingMember.value = {
    basicId: row.basicId,
    displayName: row.displayName ?? null,
    effectiveTime: row.effectiveTime ?? null,
    expirationTime: row.expirationTime ?? null,
    inviteRemark: null,
    memberType: row.memberType,
    remark: null,
  }
  memberEditVisible.value = true
}

async function handleSaveMember() {
  if (!editingMember.value || !editingMemberId.value) {
    return
  }
  memberEditLoading.value = true
  try {
    await tenantManagementApi.members.update(editingMember.value)
    message.success(t('tenant.list.member_update_success'))
    memberEditVisible.value = false
    await loadMembers()
  }
  catch {
    message.error(t('tenant.list.member_update_failed'))
  }
  finally {
    memberEditLoading.value = false
  }
}

function handleChangeMemberStatus(row: TenantMemberListItemDto) {
  editingMemberStatusId.value = row.basicId
  editingMemberStatus.value = row.status
  memberStatusVisible.value = true
}

async function handleSaveMemberStatus() {
  if (!editingMemberStatusId.value) {
    return
  }
  memberStatusLoading.value = true
  try {
    const input: TenantMemberStatusUpdateDto = {
      basicId: editingMemberStatusId.value,
      status: editingMemberStatus.value,
    }
    await tenantManagementApi.members.updateStatus(input)
    message.success(t('tenant.list.member_status_update_success'))
    memberStatusVisible.value = false
    await loadMembers()
  }
  catch {
    message.error(t('tenant.list.member_status_update_failed'))
  }
  finally {
    memberStatusLoading.value = false
  }
}

function getInviteStatusTagType(status: TenantMemberInviteStatus) {
  if (status === TenantMemberInviteStatus.Accepted) {
    return 'success'
  }
  if (status === TenantMemberInviteStatus.Pending) {
    return 'info'
  }
  if (status === TenantMemberInviteStatus.Rejected) {
    return 'error'
  }
  if (status === TenantMemberInviteStatus.Revoked) {
    return 'warning'
  }
  return 'default'
}

function validateForm() {
  if (!tenantForm.value.tenantName.trim()) {
    message.warning(t('tenant.list.validate_tenant_name'))
    return false
  }
  if (!tenantForm.value.basicId && !tenantForm.value.tenantCode.trim()) {
    message.warning(t('tenant.list.validate_tenant_code'))
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true
  try {
    if (tenantForm.value.basicId) {
      const updateInput: TenantUpdateDto = {
        basicId: tenantForm.value.basicId,
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expirationTime: tenantForm.value.expirationTime,
        isolationMode: tenantForm.value.isolationMode,
        logo: normalizeNullable(tenantForm.value.logo),
        remark: normalizeNullable(tenantForm.value.remark),
        sort: tenantForm.value.sort,
        storageLimit: tenantForm.value.storageLimit ?? null,
        tenantName: tenantForm.value.tenantName.trim(),
        tenantShortName: normalizeNullable(tenantForm.value.tenantShortName),
        userLimit: tenantForm.value.userLimit ?? null,
      }

      await tenantManagementApi.update(updateInput)

      if (editingStatus.value !== tenantForm.value.tenantStatus && tenantForm.value.tenantStatus !== undefined) {
        await tenantManagementApi.updateStatus({
          basicId: tenantForm.value.basicId,
          reason: t('tenant.list.status_change_reason'),
          tenantStatus: tenantForm.value.tenantStatus,
        })
      }
    }
    else {
      const createInput: TenantCreateDto = {
        adminEmail: normalizeNullable(tenantForm.value.adminEmail),
        adminPassword: normalizeNullable(tenantForm.value.adminPassword),
        adminUserName: normalizeNullable(tenantForm.value.adminUserName),
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expirationTime: tenantForm.value.expirationTime,
        isolationMode: tenantForm.value.isolationMode,
        logo: normalizeNullable(tenantForm.value.logo),
        remark: normalizeNullable(tenantForm.value.remark),
        sort: tenantForm.value.sort,
        storageLimit: tenantForm.value.storageLimit ?? null,
        tenantCode: tenantForm.value.tenantCode.trim(),
        tenantName: tenantForm.value.tenantName.trim(),
        tenantShortName: normalizeNullable(tenantForm.value.tenantShortName),
        userLimit: tenantForm.value.userLimit ?? null,
      }

      await tenantManagementApi.create(createInput)
    }

    message.success(t('tenant.list.save_success'))
    modalVisible.value = false
    reloadTenant()
  }
  catch {
    message.error(t('tenant.list.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NDrawer v-model:show="detailVisible" :width="800">
      <NDrawerContent closable :title="t('tenant.list.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" :description="t('tenant.list.detail_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" :tab="t('tenant.list.tab_overview')">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem :label="t('tenant.list.tenant_name')">
                    {{ currentDetail.tenantName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.tenant_code')">
                    {{ currentDetail.tenantCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.tenant_short_name')">
                    {{ formatNullable(currentDetail.tenantShortName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.domain')">
                    {{ formatNullable(currentDetail.domain) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.tenant_status')">
                    <NTag :type="getTenantStatusTagType(currentDetail.tenantStatus)" round size="small">
                      {{ getOptionLabel(tenantStatusOptions, currentDetail.tenantStatus) }}
                    </NTag>
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.config_status')">
                    <NTag :type="currentDetail.configStatus === TenantConfigStatus.Configured ? 'success' : currentDetail.configStatus === TenantConfigStatus.Failed ? 'error' : 'warning'" round size="small">
                      {{ getOptionLabel(configStatusOptions, currentDetail.configStatus) }}
                    </NTag>
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.isolation_mode')">
                    {{ getOptionLabel(isolationModeOptions, currentDetail.isolationMode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.edition_id')">
                    {{ formatNullable(currentDetail.editionId) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.user_limit')">
                    {{ formatNullable(currentDetail.userLimit) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.storage_limit_mb')">
                    {{ formatNullable(currentDetail.storageLimit) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.sort')">
                    {{ currentDetail.sort }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.is_expired_value')">
                    {{ formatBoolean(currentDetail.isExpired) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.expiration_time')">
                    {{ formatNullableDate(currentDetail.expirationTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.created_time')">
                    {{ formatNullableDate(currentDetail.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.modified_time')">
                    {{ formatNullableDate(currentDetail.modifiedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="members" :tab="t('tenant.list.tab_members')">
                <NSpin :show="memberLoading">
                  <div v-if="memberError" class="xh-detail-empty">
                    <NEmpty :description="t('tenant.list.member_load_failed')">
                      <template #extra>
                        <NButton size="small" @click="loadMembers">
                          {{ t('tenant.list.member_retry') }}
                        </NButton>
                      </template>
                    </NEmpty>
                  </div>
                  <NEmpty v-else-if="!memberLoading && members.length === 0" class="xh-detail-empty" :description="t('tenant.list.member_empty')" />
                  <table v-else class="xh-detail-table">
                    <thead>
                      <tr>
                        <th>{{ t('tenant.list.member_user_id') }}</th>
                        <th>{{ t('tenant.list.member_display_name') }}</th>
                        <th>{{ t('tenant.list.member_type') }}</th>
                        <th>{{ t('tenant.list.member_invite_status') }}</th>
                        <th>{{ t('tenant.list.member_status') }}</th>
                        <th>{{ t('tenant.list.member_join_time') }}</th>
                        <th>{{ t('tenant.list.member_operation') }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="item in members" :key="item.basicId">
                        <td>{{ item.userId }}</td>
                        <td>{{ formatNullable(item.displayName) }}</td>
                        <td>
                          <NTag :type="item.memberType === TenantMemberType.Owner ? 'warning' : item.memberType === TenantMemberType.Admin ? 'primary' : 'default'" round size="small">
                            {{ getOptionLabel(memberTypeOptions, item.memberType) }}
                          </NTag>
                        </td>
                        <td>
                          <NTag :type="getInviteStatusTagType(item.inviteStatus)" round size="small">
                            {{ getOptionLabel(inviteStatusOptions, item.inviteStatus) }}
                          </NTag>
                        </td>
                        <td>
                          <NTag :type="item.status === ValidityStatus.Valid ? 'success' : 'error'" round size="small">
                            {{ getOptionLabel(validityStatusOptions, item.status) }}
                          </NTag>
                        </td>
                        <td>{{ formatNullableDate(item.createdTime) }}</td>
                        <td>
                          <NSpace size="small">
                            <NButton size="tiny" @click="handleEditMember(item)">
                              {{ t('tenant.list.member_edit') }}
                            </NButton>
                            <NButton size="tiny" type="warning" @click="handleChangeMemberStatus(item)">
                              {{ t('tenant.list.member_change_status') }}
                            </NButton>
                          </NSpace>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </NSpin>
              </NTabPane>

              <NTabPane name="config" :tab="t('tenant.list.tab_config')">
                <NDescriptions :column="1" bordered size="small">
                  <NDescriptionsItem :label="t('tenant.list.logo')">
                    {{ formatNullable(currentDetail.logo) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.domain')">
                    {{ formatNullable(currentDetail.domain) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.created_id')">
                    {{ formatNullable(currentDetail.createdId) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('tenant.list.modified_id')">
                    {{ formatNullable(currentDetail.modifiedId) }}
                  </NDescriptionsItem>
                </NDescriptions>
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
      style="width: 760px; max-width: 92vw"
    >
      <NForm :model="tenantForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('tenant.list.tenant_name')" path="tenantName">
          <NInput v-model:value="tenantForm.tenantName" clearable :placeholder="t('tenant.list.tenant_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.tenant_code')" path="tenantCode">
          <NInput
            v-model:value="tenantForm.tenantCode"
            :disabled="Boolean(tenantForm.basicId)"
            clearable
            :placeholder="t('tenant.list.tenant_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.list.tenant_short_name')" path="tenantShortName">
          <NInput v-model:value="tenantForm.tenantShortName" clearable :placeholder="t('tenant.list.tenant_short_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.domain')" path="domain">
          <NInput v-model:value="tenantForm.domain" clearable :placeholder="t('tenant.list.domain_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.isolation_mode')" path="isolationMode">
          <NSelect v-model:value="tenantForm.isolationMode" :options="isolationModeOptions" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.edition_id')" path="editionId">
          <NInput v-model:value="tenantForm.editionId" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!tenantForm.basicId" :label="t('tenant.list.admin_user_name')" path="adminUserName">
          <NInput v-model:value="tenantForm.adminUserName" clearable :placeholder="t('tenant.list.admin_user_name_placeholder')" />
        </NFormItem>
        <NFormItem v-if="!tenantForm.basicId" :label="t('tenant.list.admin_email')" path="adminEmail">
          <NInput
            v-model:value="tenantForm.adminEmail"
            clearable
            :placeholder="t('tenant.list.admin_email_placeholder')"
            :input-props="{ type: 'email' }"
          />
        </NFormItem>
        <NFormItem v-if="!tenantForm.basicId" :label="t('tenant.list.admin_password')" path="adminPassword">
          <NInput
            v-model:value="tenantForm.adminPassword"
            clearable
            :placeholder="t('tenant.list.admin_password_placeholder')"
            show-password-on="click"
            type="password"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.list.user_limit')" path="userLimit">
          <NInputNumber v-model:value="tenantForm.userLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.storage_limit')" path="storageLimit">
          <NInputNumber v-model:value="tenantForm.storageLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.sort')" path="sort">
          <NInputNumber v-model:value="tenantForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="tenantForm.basicId" :label="t('tenant.list.tenant_status')" path="tenantStatus">
          <NSelect v-model:value="tenantForm.tenantStatus" :options="tenantStatusOptions" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.expiration_time')" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="tenantForm.expirationTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.list.logo')" path="logo">
          <NInput v-model:value="tenantForm.logo" clearable :placeholder="t('tenant.list.logo_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.remark')" path="remark">
          <NInput
            v-model:value="tenantForm.remark"
            clearable
            :placeholder="t('tenant.list.remark_placeholder')"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('tenant.list.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('tenant.list.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="memberEditVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 520px; max-width: 92vw"
      :title="t('tenant.list.member_edit_title')"
    >
      <NForm v-if="editingMember" :model="editingMember" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('tenant.list.member_display_name')" path="displayName">
          <NInput v-model:value="editingMember.displayName" clearable :placeholder="t('tenant.list.member_display_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.member_type')" path="memberType">
          <NSelect v-model:value="editingMember.memberType" :options="memberTypeOptions" />
        </NFormItem>
        <NFormItem :label="t('tenant.list.member_effective_time')" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="editingMember.effectiveTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.list.member_expiration_time')" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="editingMember.expirationTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.list.member_invite_remark')" path="inviteRemark">
          <NInput
            v-model:value="editingMember.inviteRemark"
            clearable
            :placeholder="t('tenant.list.member_invite_remark_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.list.member_remark')" path="remark">
          <NInput
            v-model:value="editingMember.remark"
            clearable
            :placeholder="t('tenant.list.member_remark_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="memberEditVisible = false">
            {{ t('tenant.list.cancel') }}
          </NButton>
          <NButton :loading="memberEditLoading" type="primary" @click="handleSaveMember">
            {{ t('tenant.list.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="memberStatusVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 360px; max-width: 92vw"
      :title="t('tenant.list.member_status_title')"
    >
      <NForm label-placement="top">
        <NFormItem :label="t('tenant.list.member_status')">
          <NSelect v-model:value="editingMemberStatus" :options="validityStatusOptions" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="memberStatusVisible = false">
            {{ t('tenant.list.cancel') }}
          </NButton>
          <NButton :loading="memberStatusLoading" type="primary" @click="handleSaveMemberStatus">
            {{ t('tenant.list.save') }}
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
