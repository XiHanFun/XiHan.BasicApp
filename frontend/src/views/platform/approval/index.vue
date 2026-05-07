<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ReviewDetailDto, ReviewListItemDto } from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { approvalManagementApi, AuditResult, AuditStatus, createPageRequest, EnableStatus } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformApprovalPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

interface ReviewGridResult {
  items: ReviewListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ReviewListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ReviewDetailDto | ReviewListItemDto | null>(null)

const queryParams = reactive({
  keyword: '',
  reviewResult: undefined as AuditResult | undefined,
  reviewStatus: undefined as AuditStatus | undefined,
  status: undefined as EnableStatus | undefined,
})

const reviewStatusOptions = [
  { label: '待审核', value: AuditStatus.Pending },
  { label: '审核中', value: AuditStatus.InProgress },
  { label: '审核通过', value: AuditStatus.Approved },
  { label: '审核拒绝', value: AuditStatus.Rejected },
  { label: '已撤回', value: AuditStatus.Withdrawn },
]

const reviewResultOptions = [
  { label: '通过', value: AuditResult.Pass },
  { label: '拒绝', value: AuditResult.Reject },
  { label: '退回修改', value: AuditResult.Return },
]

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function reviewStatusTag(status: AuditStatus): TagType {
  switch (status) {
    case AuditStatus.Approved:
      return 'success'
    case AuditStatus.Rejected:
      return 'error'
    case AuditStatus.InProgress:
      return 'warning'
    case AuditStatus.Withdrawn:
      return 'default'
    default:
      return 'info'
  }
}

function reviewResultTag(result?: AuditResult | null): TagType {
  switch (result) {
    case AuditResult.Pass:
      return 'success'
    case AuditResult.Reject:
      return 'error'
    case AuditResult.Return:
      return 'warning'
    default:
      return 'default'
  }
}

function statusTag(status: EnableStatus): TagType {
  return status === EnableStatus.Enabled ? 'success' : 'default'
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<ReviewGridResult> {
  return approvalManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      keyword: normalizeNullable(queryParams.keyword),
      reviewResult: queryParams.reviewResult,
      reviewStatus: queryParams.reviewStatus,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询审批流程失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<ReviewListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'reviewTitle', minWidth: 220, showOverflow: 'tooltip', title: '审批标题' },
      { field: 'reviewCode', minWidth: 160, showOverflow: 'tooltip', title: '审批编码' },
      { field: 'reviewType', minWidth: 130, showOverflow: 'tooltip', title: '审批类型' },
      { field: 'entityType', minWidth: 130, showOverflow: 'tooltip', title: '业务实体' },
      { field: 'entityId', minWidth: 150, showOverflow: 'tooltip', title: '业务主键' },
      {
        field: 'reviewStatus',
        slots: { default: 'col_review_status' },
        title: '审批状态',
        width: 120,
      },
      {
        field: 'reviewResult',
        slots: { default: 'col_review_result' },
        title: '审批结果',
        width: 120,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '启停状态',
        width: 100,
      },
      { field: 'priority', minWidth: 86, sortable: true, title: '优先级' },
      { field: 'reviewLevel', minWidth: 100, title: '审批级别' },
      { field: 'currentLevel', minWidth: 110, title: '当前级别' },
      { field: 'submitUserId', minWidth: 110, title: '提交人' },
      {
        field: 'submitTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '提交时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 86,
      },
    ],
    id: 'sys_review',
    name: '审批流程',
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
  queryParams.reviewResult = undefined
  queryParams.reviewStatus = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleDetail(row: ReviewListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  try {
    detailData.value = await approvalManagementApi.detail(row.basicId) ?? row
  }
  catch {
    detailData.value = row
    message.error('加载审批详情失败')
  }
  finally {
    detailLoading.value = false
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
          placeholder="搜索标题/编码/业务"
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.reviewStatus"
          :options="reviewStatusOptions"
          clearable
          placeholder="审批状态"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.reviewResult"
          :options="reviewResultOptions"
          clearable
          placeholder="审批结果"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="启停状态"
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
        <template #col_review_status="{ row }">
          <NTag :type="reviewStatusTag(row.reviewStatus)" round size="small">
            {{ getOptionLabel(reviewStatusOptions, row.reviewStatus) }}
          </NTag>
        </template>
        <template #col_review_result="{ row }">
          <NTag :type="reviewResultTag(row.reviewResult)" round size="small">
            {{ row.reviewResult === null || row.reviewResult === undefined ? '未出结果' : getOptionLabel(reviewResultOptions, row.reviewResult) }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="statusTag(row.status)" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="620">
      <NDrawerContent closable title="审批详情">
        <NDescriptions v-if="detailData" :column="1" bordered label-placement="left" size="small">
          <NDescriptionsItem label="审批标题">
            {{ detailData.reviewTitle }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批编码">
            {{ detailData.reviewCode }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批类型">
            {{ detailData.reviewType }}
          </NDescriptionsItem>
          <NDescriptionsItem label="业务实体">
            {{ detailData.entityType || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="业务主键">
            {{ detailData.entityId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批状态">
            {{ getOptionLabel(reviewStatusOptions, detailData.reviewStatus) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批结果">
            {{ detailData.reviewResult === null || detailData.reviewResult === undefined ? '未出结果' : getOptionLabel(reviewResultOptions, detailData.reviewResult) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="提交时间">
            {{ formatDate(detailData.submitTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="开始时间">
            {{ formatNullableDate(detailData.reviewStartTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="结束时间">
            {{ formatNullableDate(detailData.reviewEndTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批内容">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ 'reviewContent' in detailData ? detailData.reviewContent || '-' : '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem label="业务数据">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ 'businessData' in detailData ? detailData.businessData || '-' : '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ 'remark' in detailData ? detailData.remark || '-' : '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div v-else-if="detailLoading" class="py-8 text-center text-gray-400">
          正在加载...
        </div>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
