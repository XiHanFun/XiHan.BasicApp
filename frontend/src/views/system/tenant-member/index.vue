<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  DateTimeString,
  TenantMemberListItemDto,
  TenantMemberUpdateDto,
} from '@/api'
import {
  NButton,
  NDatePicker,
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
  tenantMemberApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  ValidityStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemTenantMemberPage' })

interface TenantMemberGridResult {
  items: TenantMemberListItemDto[]
  total: number
}

interface TenantMemberFormModel extends TenantMemberUpdateDto {
  inviteStatus: TenantMemberInviteStatus
  status: ValidityStatus
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<TenantMemberListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<ValidityStatus | null>(null)
const editingInviteStatus = ref<TenantMemberInviteStatus | null>(null)
const memberForm = ref<TenantMemberFormModel>(createDefaultForm())

const queryParams = reactive({
  inviteStatus: null as TenantMemberInviteStatus | null,
  keyword: '',
  memberType: null as TenantMemberType | null,
  status: null as ValidityStatus | null,
  userId: null as ApiId | null,
})

const memberTypeOptions = [
  { label: '所有者', value: TenantMemberType.Owner },
  { label: '管理员', value: TenantMemberType.Admin },
  { label: '成员', value: TenantMemberType.Member },
  { label: '外部成员', value: TenantMemberType.External },
  { label: '访客', value: TenantMemberType.Guest },
  { label: '顾问', value: TenantMemberType.Consultant },
  { label: '平台管理员', value: TenantMemberType.PlatformAdmin },
]

const maintainableMemberTypeOptions = memberTypeOptions.filter(option => option.value !== TenantMemberType.PlatformAdmin)

const inviteStatusOptions = [
  { label: '待接受', value: TenantMemberInviteStatus.Pending },
  { label: '已接受', value: TenantMemberInviteStatus.Accepted },
  { label: '已拒绝', value: TenantMemberInviteStatus.Rejected },
  { label: '已撤销', value: TenantMemberInviteStatus.Revoked },
  { label: '已过期', value: TenantMemberInviteStatus.Expired },
]

const validityStatusOptions = [
  { label: '有效', value: ValidityStatus.Valid },
  { label: '无效', value: ValidityStatus.Invalid },
]

const modalTitle = computed(() => `编辑租户成员 #${memberForm.value.basicId}`)

function createDefaultForm(): TenantMemberFormModel {
  return {
    basicId: 0,
    displayName: null,
    effectiveTime: null,
    expirationTime: null,
    inviteRemark: null,
    inviteStatus: TenantMemberInviteStatus.Pending,
    memberType: TenantMemberType.Member,
    remark: null,
    status: ValidityStatus.Valid,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function normalizeId(value: ApiId | null) {
  return value && value > 0 ? value : null
}

function toDateInputValue(value?: DateTimeString | null) {
  return value ? value.slice(0, 10) : null
}

function isOwner(row: TenantMemberListItemDto) {
  return row.memberType === TenantMemberType.Owner
}

function canToggleStatus(row: TenantMemberListItemDto) {
  return !(isOwner(row) && row.status === ValidityStatus.Valid)
}

function canRevoke(row: TenantMemberListItemDto) {
  return !isOwner(row) && row.inviteStatus !== TenantMemberInviteStatus.Revoked
}

function getValidityTagType(status: ValidityStatus) {
  return status === ValidityStatus.Valid ? 'success' : 'error'
}

function getInviteTagType(status: TenantMemberInviteStatus) {
  if (status === TenantMemberInviteStatus.Accepted) {
    return 'success'
  }

  if (status === TenantMemberInviteStatus.Rejected || status === TenantMemberInviteStatus.Revoked) {
    return 'error'
  }

  if (status === TenantMemberInviteStatus.Expired) {
    return 'default'
  }

  return 'warning'
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<TenantMemberGridResult> {
  return tenantMemberApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      inviteStatus: queryParams.inviteStatus,
      keyword: normalizeNullable(queryParams.keyword),
      memberType: queryParams.memberType,
      status: queryParams.status,
      userId: normalizeId(queryParams.userId),
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询租户成员失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<TenantMemberListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'displayName', minWidth: 150, showOverflow: 'tooltip', title: '显示名称' },
      { field: 'userId', minWidth: 120, title: '用户主键' },
      {
        field: 'memberType',
        formatter: ({ cellValue }) => getOptionLabel(memberTypeOptions, cellValue),
        minWidth: 120,
        title: '成员类型',
      },
      {
        field: 'inviteStatus',
        slots: { default: 'col_invite_status' },
        title: '邀请状态',
        width: 100,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '成员状态',
        width: 100,
      },
      {
        field: 'isExpired',
        slots: { default: 'col_expired' },
        title: '过期',
        width: 82,
      },
      {
        field: 'effectiveTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '生效时间',
      },
      {
        field: 'expirationTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '失效时间',
      },
      {
        field: 'lastActiveTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        title: '最后活跃',
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
        width: 220,
      },
    ],
    id: 'sys_tenant_member',
    name: '租户成员',
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
  queryParams.inviteStatus = null
  queryParams.keyword = ''
  queryParams.memberType = null
  queryParams.status = null
  queryParams.userId = null
  xGrid.value?.commitProxy('reload')
}

function handleEdit(row: TenantMemberListItemDto) {
  editingStatus.value = row.status
  editingInviteStatus.value = row.inviteStatus
  memberForm.value = {
    basicId: row.basicId,
    displayName: row.displayName ?? null,
    effectiveTime: toDateInputValue(row.effectiveTime),
    expirationTime: toDateInputValue(row.expirationTime),
    inviteRemark: null,
    inviteStatus: row.inviteStatus,
    memberType: row.memberType,
    remark: null,
    status: row.status,
  }
  modalVisible.value = true
}

function validateForm() {
  if (memberForm.value.basicId <= 0) {
    message.warning('租户成员主键无效')
    return false
  }

  if (memberForm.value.memberType === TenantMemberType.PlatformAdmin) {
    message.warning('平台管理员身份不能通过租户成员页维护')
    return false
  }

  if (memberForm.value.expirationTime && memberForm.value.effectiveTime
    && memberForm.value.expirationTime <= memberForm.value.effectiveTime) {
    message.warning('失效时间必须晚于生效时间')
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
    const updateInput: TenantMemberUpdateDto = {
      basicId: memberForm.value.basicId,
      displayName: normalizeNullable(memberForm.value.displayName),
      effectiveTime: memberForm.value.effectiveTime,
      expirationTime: memberForm.value.expirationTime,
      inviteRemark: normalizeNullable(memberForm.value.inviteRemark),
      memberType: memberForm.value.memberType,
      remark: normalizeNullable(memberForm.value.remark),
    }

    await tenantMemberApi.update(updateInput)

    if (editingInviteStatus.value !== memberForm.value.inviteStatus) {
      await tenantMemberApi.updateInviteStatus({
        basicId: memberForm.value.basicId,
        inviteRemark: normalizeNullable(memberForm.value.inviteRemark),
        inviteStatus: memberForm.value.inviteStatus,
      })
    }

    if (
      editingStatus.value !== memberForm.value.status
      && memberForm.value.inviteStatus !== TenantMemberInviteStatus.Revoked
      && memberForm.value.inviteStatus !== TenantMemberInviteStatus.Expired
    ) {
      await tenantMemberApi.updateStatus({
        basicId: memberForm.value.basicId,
        remark: normalizeNullable(memberForm.value.remark),
        status: memberForm.value.status,
      })
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

async function handleToggleStatus(row: TenantMemberListItemDto) {
  if (!canToggleStatus(row)) {
    message.warning('租户所有者成员关系不能直接停用')
    return
  }

  await tenantMemberApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === ValidityStatus.Valid ? '前端停用租户成员' : '前端启用租户成员',
    status: row.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid,
  })
  message.success('成员状态已更新')
  xGrid.value?.commitProxy('query')
}

async function handleRevoke(row: TenantMemberListItemDto) {
  await tenantMemberApi.revoke(row.basicId)
  message.success('成员已撤销')
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
          placeholder="搜索显示名称/备注"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NInputNumber v-model:value="queryParams.userId" clearable placeholder="用户主键" style="width: 120px" />
        <NSelect
          v-model:value="queryParams.memberType"
          :options="memberTypeOptions"
          clearable
          placeholder="成员类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.inviteStatus"
          :options="inviteStatusOptions"
          clearable
          placeholder="邀请状态"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="validityStatusOptions"
          clearable
          placeholder="成员状态"
          style="width: 120px"
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
        <template #toolbar_buttons />

        <template #col_invite_status="{ row }">
          <NTag :type="getInviteTagType(row.inviteStatus)" round size="small">
            {{ getOptionLabel(inviteStatusOptions, row.inviteStatus) }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="getValidityTagType(row.status)" round size="small">
            {{ getOptionLabel(validityStatusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_expired="{ row }">
          <NTag :type="row.isExpired ? 'error' : 'default'" round size="small">
            {{ row.isExpired ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" text type="primary" @click="handleEdit(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
              编辑
            </NButton>

            <NPopconfirm :disabled="!canToggleStatus(row)" @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton :disabled="!canToggleStatus(row)" size="small" text type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === ValidityStatus.Valid ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === ValidityStatus.Valid ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新成员状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canRevoke(row)" @positive-click="handleRevoke(row)">
              <template #trigger>
                <NButton :disabled="!canRevoke(row)" size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:user-x" /></NIcon>
                  </template>
                  撤销
                </NButton>
              </template>
              确认撤销该租户成员？
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
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="memberForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="显示名称" path="displayName">
          <NInput v-model:value="memberForm.displayName" clearable placeholder="请输入成员显示名称" />
        </NFormItem>
        <NFormItem label="成员类型" path="memberType">
          <NSelect v-model:value="memberForm.memberType" :options="maintainableMemberTypeOptions" />
        </NFormItem>
        <NFormItem label="成员状态" path="status">
          <NSelect v-model:value="memberForm.status" :options="validityStatusOptions" />
        </NFormItem>
        <NFormItem label="邀请状态" path="inviteStatus">
          <NSelect v-model:value="memberForm.inviteStatus" :options="inviteStatusOptions" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="memberForm.effectiveTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="失效时间" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="memberForm.expirationTime"
            clearable
            style="width: 100%"
            type="date"
            value-format="yyyy-MM-dd"
          />
        </NFormItem>
        <NFormItem label="邀请备注" path="inviteRemark">
          <NInput v-model:value="memberForm.inviteRemark" clearable placeholder="请输入邀请备注" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="memberForm.remark" clearable placeholder="请输入备注" />
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
