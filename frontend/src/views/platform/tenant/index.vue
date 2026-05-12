<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  TenantCreateDto,
  TenantDetailDto,
  TenantListItemDto,
  TenantMemberListItemDto,
  TenantMemberStatusUpdateDto,
  TenantMemberUpdateDto,
  TenantUpdateDto,
} from '@/api'
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
import { computed, reactive, ref } from 'vue'
import {
  createDefaultQueryBehavior,
  createPageRequest,
  TenantConfigStatus,
  TenantIsolationMode,
  TenantMemberInviteStatus,
  TenantMemberType,
  tenantManagementApi,
  TenantStatus,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import {
  MEMBER_INVITE_STATUS_OPTIONS,
  MEMBER_TYPE_OPTIONS,
  TENANT_CONFIG_STATUS_OPTIONS,
  TENANT_ISOLATION_MODE_OPTIONS,
  TENANT_STATUS_OPTIONS,
  VALIDITY_STATUS_OPTIONS,
} from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformTenantPage' })

interface TenantGridResult {
  items: TenantListItemDto[]
  total: number
}

interface TenantFormModel extends TenantCreateDto {
  basicId?: ApiId
  tenantStatus?: TenantStatus
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<TenantListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<TenantStatus | null>(null)
const tenantForm = ref<TenantFormModel>(createDefaultForm())
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<TenantDetailDto | null>(null)

const memberTypeOptions = MEMBER_TYPE_OPTIONS

const inviteStatusOptions = MEMBER_INVITE_STATUS_OPTIONS

const validityStatusOptions = VALIDITY_STATUS_OPTIONS

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

const queryParams = reactive({
  configStatus: undefined as TenantConfigStatus | undefined,
  editionId: undefined as ApiId | null | undefined,
  expireTimeEnd: null as string | null,
  expireTimeStart: null as string | null,
  keyword: '',
  tenantStatus: undefined as TenantStatus | undefined,
})

const tenantStatusOptions = TENANT_STATUS_OPTIONS

const configStatusOptions = TENANT_CONFIG_STATUS_OPTIONS

const isolationModeOptions = TENANT_ISOLATION_MODE_OPTIONS

const modalTitle = computed(() => (tenantForm.value.basicId ? '编辑租户' : '新增租户'))

function createDefaultForm(): TenantFormModel {
  return {
    domain: null,
    editionId: null,
    expireTime: null,
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

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<TenantGridResult> {
  return tenantManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      configStatus: queryParams.configStatus,
      editionId: queryParams.editionId ?? null,
      expireTimeEnd: queryParams.expireTimeEnd,
      expireTimeStart: queryParams.expireTimeStart,
      keyword: normalizeNullable(queryParams.keyword),
      tenantStatus: queryParams.tenantStatus,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询租户失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<TenantListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'tenantName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '租户名称' },
      { field: 'tenantCode', minWidth: 150, showOverflow: 'tooltip', title: '租户编码' },
      { field: 'tenantShortName', minWidth: 130, showOverflow: 'tooltip', title: '简称' },
      { field: 'domain', minWidth: 180, showOverflow: 'tooltip', title: '域名' },
      {
        field: 'isolationMode',
        formatter: ({ cellValue }) => getOptionLabel(isolationModeOptions, cellValue),
        minWidth: 120,
        title: '隔离模式',
      },
      { field: 'editionId', minWidth: 100, title: '版本' },
      {
        field: 'tenantStatus',
        slots: { default: 'col_tenant_status' },
        title: '租户状态',
        width: 100,
      },
      {
        field: 'configStatus',
        formatter: ({ cellValue }) => getOptionLabel(configStatusOptions, cellValue),
        minWidth: 110,
        title: '配置状态',
      },
      {
        field: 'isExpired',
        slots: { default: 'col_expired' },
        title: '过期',
        width: 82,
      },
      { field: 'userLimit', minWidth: 100, title: '用户上限' },
      { field: 'storageLimit', minWidth: 120, title: '存储上限' },
      { field: 'sort', minWidth: 80, sortable: true, title: '排序' },
      {
        field: 'expireTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '到期时间',
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
        width: 106,
      },
    ],
    id: 'sys_tenant',
    name: '租户管理',
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

function getTenantStatusTagType(status: TenantStatus) {
  if (status === TenantStatus.Normal) {
    return 'success'
  }

  if (status === TenantStatus.Disabled) {
    return 'error'
  }

  return 'warning'
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.tenantStatus = undefined
  queryParams.configStatus = undefined
  queryParams.editionId = undefined
  queryParams.expireTimeStart = null
  queryParams.expireTimeEnd = null
  xGrid.value?.commitProxy('reload')
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
    expireTime: row.expireTime ?? null,
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
      message.warning('未查询到租户详情')
    }
  }
  catch {
    message.error('加载租户详情失败')
  }
  finally {
    detailLoading.value = false
  }
  loadMembers()
}

async function loadMembers() {
  if (!currentDetail.value) return
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
    message.error('加载成员列表失败')
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
  if (!editingMember.value || !editingMemberId.value) return
  memberEditLoading.value = true
  try {
    await tenantManagementApi.members.update(editingMember.value)
    message.success('成员资料更新成功')
    memberEditVisible.value = false
    await loadMembers()
  }
  catch {
    message.error('成员资料更新失败')
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
  if (!editingMemberStatusId.value) return
  memberStatusLoading.value = true
  try {
    const input: TenantMemberStatusUpdateDto = {
      basicId: editingMemberStatusId.value,
      status: editingMemberStatus.value,
    }
    await tenantManagementApi.members.updateStatus(input)
    message.success('成员状态更新成功')
    memberStatusVisible.value = false
    await loadMembers()
  }
  catch {
    message.error('成员状态更新失败')
  }
  finally {
    memberStatusLoading.value = false
  }
}

function getInviteStatusTagType(status: TenantMemberInviteStatus) {
  if (status === TenantMemberInviteStatus.Accepted) return 'success'
  if (status === TenantMemberInviteStatus.Pending) return 'info'
  if (status === TenantMemberInviteStatus.Rejected) return 'error'
  if (status === TenantMemberInviteStatus.Revoked) return 'warning'
  return 'default'
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) return '-'
  return value ? '是' : '否'
}

function validateForm() {
  if (!tenantForm.value.tenantName.trim()) {
    message.warning('请输入租户名称')
    return false
  }

  if (!tenantForm.value.basicId && !tenantForm.value.tenantCode.trim()) {
    message.warning('请输入租户编码')
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
        expireTime: tenantForm.value.expireTime,
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
          reason: '前端更新租户状态',
          tenantStatus: tenantForm.value.tenantStatus,
        })
      }
    }
    else {
      const createInput: TenantCreateDto = {
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expireTime: tenantForm.value.expireTime,
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
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索租户名称/编码/域名"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.tenantStatus"
          :options="tenantStatusOptions"
          clearable
          placeholder="租户状态"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.configStatus"
          :options="configStatusOptions"
          clearable
          placeholder="配置状态"
          style="width: 120px"
        />
        <NInput
          v-model:value="queryParams.editionId"
          clearable
          placeholder="版本 ID"
          style="width: 120px"
        />
        <NDatePicker
          v-model:formatted-value="queryParams.expireTimeStart"
          clearable
          placeholder="到期开始"
          style="width: 160px"
          type="date"
          value-format="yyyy-MM-dd"
        />
        <NDatePicker
          v-model:formatted-value="queryParams.expireTimeEnd"
          clearable
          placeholder="到期结束"
          style="width: 160px"
          type="date"
          value-format="yyyy-MM-dd"
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
            新增租户
          </NButton>
        </template>

        <template #col_tenant_status="{ row }">
          <NTag :type="getTenantStatusTagType(row.tenantStatus)" round size="small">
            {{ getOptionLabel(tenantStatusOptions, row.tenantStatus) }}
          </NTag>
        </template>

        <template #col_expired="{ row }">
          <NTag :type="row.isExpired ? 'error' : 'success'" round size="small">
            {{ row.isExpired ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton aria-label="查看详情" circle quaternary size="small" @click="handleView(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="800">
      <NDrawerContent closable title="租户详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无租户详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="租户名称">
                    {{ currentDetail.tenantName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="租户编码">
                    {{ currentDetail.tenantCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="简称">
                    {{ formatNullable(currentDetail.tenantShortName) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="域名">
                    {{ formatNullable(currentDetail.domain) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="租户状态">
                    <NTag :type="getTenantStatusTagType(currentDetail.tenantStatus)" round size="small">
                      {{ getOptionLabel(tenantStatusOptions, currentDetail.tenantStatus) }}
                    </NTag>
                  </NDescriptionsItem>
                  <NDescriptionsItem label="配置状态">
                    <NTag :type="currentDetail.configStatus === TenantConfigStatus.Configured ? 'success' : currentDetail.configStatus === TenantConfigStatus.Failed ? 'error' : 'warning'" round size="small">
                      {{ getOptionLabel(configStatusOptions, currentDetail.configStatus) }}
                    </NTag>
                  </NDescriptionsItem>
                  <NDescriptionsItem label="隔离模式">
                    {{ getOptionLabel(isolationModeOptions, currentDetail.isolationMode) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="版本 ID">
                    {{ formatNullable(currentDetail.editionId) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="用户上限">
                    {{ formatNullable(currentDetail.userLimit) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="存储上限(MB)">
                    {{ formatNullable(currentDetail.storageLimit) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="排序">
                    {{ currentDetail.sort }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="已过期">
                    {{ formatBoolean(currentDetail.isExpired) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="到期时间">
                    {{ formatNullableDate(currentDetail.expireTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="修改时间">
                    {{ formatNullableDate(currentDetail.modifiedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="members" tab="成员">
                <NSpin :show="memberLoading">
                  <div v-if="memberError" class="xh-detail-empty">
                    <NEmpty description="加载成员失败">
                      <template #extra>
                        <NButton size="small" @click="loadMembers">重试</NButton>
                      </template>
                    </NEmpty>
                  </div>
                  <NEmpty v-else-if="!memberLoading && members.length === 0" class="xh-detail-empty" description="暂无成员" />
                  <table v-else class="xh-detail-table">
                    <thead>
                      <tr>
                        <th>用户ID</th>
                        <th>显示名</th>
                        <th>成员类型</th>
                        <th>邀请状态</th>
                        <th>状态</th>
                        <th>加入时间</th>
                        <th>操作</th>
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
                            <NButton size="tiny" @click="handleEditMember(item)">编辑资料</NButton>
                            <NButton size="tiny" type="warning" @click="handleChangeMemberStatus(item)">变更状态</NButton>
                          </NSpace>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </NSpin>
              </NTabPane>

              <NTabPane name="config" tab="配置信息">
                <NDescriptions :column="1" bordered size="small">
                  <NDescriptionsItem label="Logo">
                    {{ formatNullable(currentDetail.logo) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="域名">
                    {{ formatNullable(currentDetail.domain) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建人 ID">
                    {{ formatNullable(currentDetail.createdId) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="最后修改人 ID">
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
        <NFormItem label="租户名称" path="tenantName">
          <NInput v-model:value="tenantForm.tenantName" clearable placeholder="请输入租户名称" />
        </NFormItem>
        <NFormItem label="租户编码" path="tenantCode">
          <NInput
            v-model:value="tenantForm.tenantCode"
            :disabled="Boolean(tenantForm.basicId)"
            clearable
            placeholder="如: demo"
          />
        </NFormItem>
        <NFormItem label="简称" path="tenantShortName">
          <NInput v-model:value="tenantForm.tenantShortName" clearable placeholder="请输入租户简称" />
        </NFormItem>
        <NFormItem label="域名" path="domain">
          <NInput v-model:value="tenantForm.domain" clearable placeholder="如: demo.example.com" />
        </NFormItem>
        <NFormItem label="隔离模式" path="isolationMode">
          <NSelect v-model:value="tenantForm.isolationMode" :options="isolationModeOptions" />
        </NFormItem>
        <NFormItem label="版本 ID" path="editionId">
          <NInput v-model:value="tenantForm.editionId" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="用户上限" path="userLimit">
          <NInputNumber v-model:value="tenantForm.userLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="存储上限" path="storageLimit">
          <NInputNumber v-model:value="tenantForm.storageLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="tenantForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="tenantForm.basicId" label="租户状态" path="tenantStatus">
          <NSelect v-model:value="tenantForm.tenantStatus" :options="tenantStatusOptions" />
        </NFormItem>
        <NFormItem label="到期时间" path="expireTime">
          <NDatePicker
            v-model:formatted-value="tenantForm.expireTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem label="Logo" path="logo">
          <NInput v-model:value="tenantForm.logo" clearable placeholder="请输入 Logo 地址" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="tenantForm.remark"
            clearable
            placeholder="请输入备注"
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

    <!-- 编辑成员资料 -->
    <NModal
      v-model:show="memberEditVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 520px; max-width: 92vw"
      title="编辑成员资料"
    >
      <NForm v-if="editingMember" :model="editingMember" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="显示名" path="displayName">
          <NInput v-model:value="editingMember.displayName" clearable placeholder="请输入显示名" />
        </NFormItem>
        <NFormItem label="成员类型" path="memberType">
          <NSelect v-model:value="editingMember.memberType" :options="memberTypeOptions" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="editingMember.effectiveTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem label="过期时间" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="editingMember.expirationTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem label="邀请备注" path="inviteRemark">
          <NInput
            v-model:value="editingMember.inviteRemark"
            clearable
            placeholder="请输入邀请备注"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="editingMember.remark"
            clearable
            placeholder="请输入备注"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="memberEditVisible = false">取消</NButton>
          <NButton :loading="memberEditLoading" type="primary" @click="handleSaveMember">保存</NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 变更成员状态 -->
    <NModal
      v-model:show="memberStatusVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 360px; max-width: 92vw"
      title="变更成员状态"
    >
      <NForm label-placement="top">
        <NFormItem label="状态">
          <NSelect v-model:value="editingMemberStatus" :options="validityStatusOptions" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="memberStatusVisible = false">取消</NButton>
          <NButton :loading="memberStatusLoading" type="primary" @click="handleSaveMemberStatus">保存</NButton>
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
