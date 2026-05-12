<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ReviewDetailDto, ReviewListItemDto } from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDivider,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NPopconfirm,
  NSelect,
  NSpace,
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
const detailData = ref<ReviewDetailDto | null>(null)
const actionLoading = ref(false)
const approveVisible = ref(false)
const auditResult = ref<AuditResult>(AuditResult.Pass)
const auditComment = ref('')

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

const canAudit = () => {
  if (!detailData.value) return false
  const status = detailData.value.reviewStatus
  return status === AuditStatus.Pending || status === AuditStatus.InProgress
}

const canWithdraw = () => {
  if (!detailData.value) return false
  const status = detailData.value.reviewStatus
  return status !== AuditStatus.Approved && status !== AuditStatus.Rejected && status !== AuditStatus.Withdrawn
}

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
        width: 180,
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

function reload() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  reload()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.reviewResult = undefined
  queryParams.reviewStatus = undefined
  queryParams.status = undefined
  reload()
}

async function handleDetail(row: ReviewListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await approvalManagementApi.detail(row.basicId) ?? null
  }
  catch {
    message.error('加载审批详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

function openApproveDialog(result: AuditResult) {
  auditResult.value = result
  auditComment.value = ''
  approveVisible.value = true
}

async function handleAudit() {
  if (!detailData.value) return
  actionLoading.value = true
  try {
    await approvalManagementApi.audit({
      basicId: detailData.value.basicId,
      reviewResult: auditResult.value,
      reviewComment: auditComment.value.trim() || undefined,
    })
    message.success(auditResult.value === AuditResult.Pass ? '已通过' : auditResult.value === AuditResult.Reject ? '已拒绝' : '已退回')
    approveVisible.value = false
    detailVisible.value = false
    reload()
  }
  catch {
    message.error('审核操作失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleWithdraw() {
  if (!detailData.value) return
  actionLoading.value = true
  try {
    await approvalManagementApi.withdraw({
      basicId: detailData.value.basicId,
    })
    message.success('已撤回')
    detailVisible.value = false
    reload()
  }
  catch {
    message.error('撤回失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleToggleStatus(row: ReviewListItemDto) {
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  actionLoading.value = true
  try {
    await approvalManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? '已启用' : '已停用')
    reload()
  }
  catch {
    message.error('更新状态失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleDelete(row: ReviewListItemDto) {
  actionLoading.value = true
  try {
    await approvalManagementApi.delete(row.basicId)
    message.success('已删除')
    reload()
  }
  catch {
    message.error('删除失败')
  }
  finally {
    actionLoading.value = false
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
        <template #empty>
          <div class="py-12 text-center text-gray-400">
            暂无审批数据
          </div>
        </template>
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
          <NSpace :size="4">
            <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton
              v-if="row.reviewStatus === AuditStatus.Pending || row.reviewStatus === AuditStatus.InProgress"
              aria-label="通过"
              circle
              quaternary
              size="small"
              type="success"
              @click="handleDetail(row); openApproveDialog(AuditResult.Pass)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:check" /></NIcon>
              </template>
            </NButton>
            <NButton
              v-if="row.reviewStatus === AuditStatus.Pending || row.reviewStatus === AuditStatus.InProgress"
              aria-label="拒绝"
              circle
              quaternary
              size="small"
              type="error"
              @click="handleDetail(row); openApproveDialog(AuditResult.Reject)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:x" /></NIcon>
              </template>
            </NButton>
            <NButton
              aria-label="启停"
              circle
              quaternary
              size="small"
              :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
              @click="handleToggleStatus(row)"
            >
              <template #icon>
                <NIcon :icon="row.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" />
              </template>
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton
                  aria-label="删除"
                  circle
                  quaternary
                  size="small"
                  type="error"
                  :loading="actionLoading"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确定删除该审批？删除后不可恢复。
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="660">
      <NDrawerContent closable title="审批详情">
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          正在加载...
        </div>
        <NDescriptions v-else-if="detailData" :column="1" bordered label-placement="left" size="small">
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
            <NTag :type="reviewStatusTag(detailData.reviewStatus)" round size="small">
              {{ getOptionLabel(reviewStatusOptions, detailData.reviewStatus) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem label="审批结果">
            <NTag v-if="detailData.reviewResult !== null && detailData.reviewResult !== undefined" :type="reviewResultTag(detailData.reviewResult)" round size="small">
              {{ getOptionLabel(reviewResultOptions, detailData.reviewResult) }}
            </NTag>
            <span v-else>未出结果</span>
          </NDescriptionsItem>
          <NDescriptionsItem label="启停状态">
            <NTag :type="statusTag(detailData.status)" round size="small">
              {{ getOptionLabel(statusOptions, detailData.status) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem label="审批级别">
            {{ detailData.currentLevel }} / {{ detailData.reviewLevel }} 级
          </NDescriptionsItem>
          <NDescriptionsItem label="优先级">
            {{ detailData.priority }}
          </NDescriptionsItem>
          <NDescriptionsItem label="提交人">
            {{ detailData.submitUserId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="当前审批人">
            {{ detailData.currentReviewUserId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批人列表">
            {{ detailData.reviewUserIds || '-' }}
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
          <NDescriptionsItem label="审批描述">
            {{ detailData.reviewDescription || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="审批内容">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.reviewContent || '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem label="业务数据">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.businessData || '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatNullableDate(detailData.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ formatNullableDate(detailData.modifiedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ detailData.remark || '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div v-else class="py-8 text-center text-gray-400">
          暂无审批详情
        </div>

        <template v-if="detailData && !detailLoading" #footer>
          <NDivider style="margin: 12px 0" />
          <div class="text-sm font-medium mb-2">审批操作</div>
          <NSpace justify="start" :size="8">
            <NButton
              type="success"
              :disabled="!canAudit()"
              :loading="actionLoading"
              @click="openApproveDialog(AuditResult.Pass)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:check" /></NIcon>
              </template>
              通过
            </NButton>
            <NButton
              type="error"
              :disabled="!canAudit()"
              :loading="actionLoading"
              @click="openApproveDialog(AuditResult.Reject)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:x" /></NIcon>
              </template>
              拒绝
            </NButton>
            <NButton
              type="warning"
              :disabled="!canAudit()"
              :loading="actionLoading"
              @click="openApproveDialog(AuditResult.Return)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:corner-down-left" /></NIcon>
              </template>
              退回修改
            </NButton>
            <NPopconfirm @positive-click="handleWithdraw">
              <template #trigger>
                <NButton type="default" :disabled="!canWithdraw()" :loading="actionLoading">
                  <template #icon>
                    <NIcon><Icon icon="lucide:undo-2" /></NIcon>
                  </template>
                  撤回
                </NButton>
              </template>
              确定撤回该审批？
            </NPopconfirm>
          </NSpace>
        </template>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="approveVisible" :width="420">
      <NDrawerContent closable :title="auditResult === AuditResult.Pass ? '通过审批' : auditResult === AuditResult.Reject ? '拒绝审批' : '退回修改'">
        <NSpace vertical>
          <NInput
            v-model:value="auditComment"
            placeholder="审批意见（可选）"
            type="textarea"
            :autosize="{ minRows: 2, maxRows: 6 }"
          />
          <NButton
            block
            :type="auditResult === AuditResult.Pass ? 'success' : auditResult === AuditResult.Reject ? 'error' : 'warning'"
            :loading="actionLoading"
            @click="handleAudit"
          >
            <template #icon>
              <NIcon :icon="auditResult === AuditResult.Pass ? 'lucide:check' : auditResult === AuditResult.Reject ? 'lucide:x' : 'lucide:corner-down-left'" />
            </template>
            确认{{ auditResult === AuditResult.Pass ? '通过' : auditResult === AuditResult.Reject ? '拒绝' : '退回' }}
          </NButton>
        </NSpace>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
