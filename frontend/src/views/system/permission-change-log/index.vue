<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { PermissionChangeLogListItemDto } from '@/api'
import { NButton, NIcon, NSelect, NTag, useMessage } from 'naive-ui'
import { reactive, ref } from 'vue'
import {
  createPageRequest,
  permissionChangeLogApi,
  PermissionChangeType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemPermissionChangeLogPage' })

interface LogGridResult {
  items: PermissionChangeLogListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<PermissionChangeLogListItemDto>>()

const queryParams = reactive({
  changeType: undefined as PermissionChangeType | undefined,
  keyword: '',
})

const changeTypeOptions = [
  { label: '角色授予权限', value: PermissionChangeType.RoleGrantPermission },
  { label: '角色撤销权限', value: PermissionChangeType.RoleRevokePermission },
  { label: '用户直授权限', value: PermissionChangeType.UserGrantPermission },
  { label: '用户撤销直授权限', value: PermissionChangeType.UserRevokePermission },
  { label: '用户分配角色', value: PermissionChangeType.UserAssignRole },
  { label: '用户移除角色', value: PermissionChangeType.UserRemoveRole },
  { label: '用户直授权限拒绝', value: PermissionChangeType.UserDenyPermission },
  { label: '角色权限拒绝', value: PermissionChangeType.RoleDenyPermission },
]

function changeTypeTagType(value: PermissionChangeType) {
  switch (value) {
    case PermissionChangeType.RoleGrantPermission:
    case PermissionChangeType.UserGrantPermission:
    case PermissionChangeType.UserAssignRole:
      return 'success'
    case PermissionChangeType.RoleRevokePermission:
    case PermissionChangeType.UserRevokePermission:
    case PermissionChangeType.UserRemoveRole:
      return 'warning'
    case PermissionChangeType.UserDenyPermission:
    case PermissionChangeType.RoleDenyPermission:
      return 'error'
    default:
      return 'default'
  }
}

function handleQueryApi(
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
): Promise<LogGridResult> {
  return permissionChangeLogApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      changeType: queryParams.changeType,
      keyword: queryParams.keyword?.trim() || undefined,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询权限变更日志失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<PermissionChangeLogListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      {
        field: 'changeType',
        slots: { default: 'col_change_type' },
        title: '变更类型',
        width: 140,
      },
      { field: 'operatorUserId', minWidth: 120, showOverflow: 'tooltip', title: '操作人主键' },
      { field: 'targetUserId', minWidth: 120, showOverflow: 'tooltip', title: '目标用户主键' },
      { field: 'targetRoleId', minWidth: 120, showOverflow: 'tooltip', title: '目标角色主键' },
      { field: 'permissionId', minWidth: 120, showOverflow: 'tooltip', title: '权限主键' },
      { field: 'changeReason', minWidth: 180, showOverflow: 'tooltip', title: '变更原因' },
      { field: 'description', minWidth: 220, showOverflow: 'tooltip', title: '变更描述' },
      { field: 'operationIp', minWidth: 130, showOverflow: 'tooltip', title: '操作 IP' },
      { field: 'traceId', minWidth: 180, showOverflow: 'tooltip', title: '链路追踪 ID' },
      {
        field: 'changeTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '变更时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
    ],
    id: 'sys_permission_change_log',
    name: '权限变更日志',
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
  queryParams.changeType = undefined
  xGrid.value?.commitProxy('reload')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索链路/原因/描述"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.changeType"
          :options="changeTypeOptions"
          clearable
          placeholder="变更类型"
          style="width: 150px"
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
        <template #col_change_type="{ row }">
          <NTag :type="changeTypeTagType(row.changeType)" round size="small">
            {{ getOptionLabel(changeTypeOptions, row.changeType) }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
