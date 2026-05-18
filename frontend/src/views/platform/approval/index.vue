<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { ReviewDetailDto, ReviewListItemDto } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDivider,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { approvalManagementApi, AuditResult, AuditStatus, createPageRequest, EnableStatus } from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformApprovalPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ReviewDetailDto | null>(null)
const actionLoading = ref(false)
const approveVisible = ref(false)
const auditResult = ref<AuditResult>(AuditResult.Pass)
const auditComment = ref('')
const tableLoading = ref(false)
const dataList = ref<ReviewListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

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

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await approvalManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: currentPage.value,
          pageSize: pageSize.value,
        },
      }),
      keyword: normalizeNullable(queryParams.keyword),
      reviewResult: queryParams.reviewResult,
      reviewStatus: queryParams.reviewStatus,
      status: queryParams.status,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询审批流程失败')
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<ReviewListItemDto>>(() => [
  { key: 'reviewTitle', title: '审批标题', minWidth: 220, ellipsis: { tooltip: true } },
  { key: 'reviewCode', title: '审批编码', minWidth: 160, ellipsis: { tooltip: true } },
  { key: 'reviewType', title: '审批类型', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'entityType', title: '业务实体', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'entityId', title: '业务主键', minWidth: 150, ellipsis: { tooltip: true } },
  {
    key: 'reviewStatus',
    title: '审批状态',
    width: 120,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: reviewStatusTag(row.reviewStatus), bordered: false }, () => getOptionLabel(reviewStatusOptions, row.reviewStatus))
    },
  },
  {
    key: 'reviewResult',
    title: '审批结果',
    width: 120,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: reviewResultTag(row.reviewResult), bordered: false }, () => row.reviewResult === null || row.reviewResult === undefined ? '未出结果' : getOptionLabel(reviewResultOptions, row.reviewResult))
    },
  },
  {
    key: 'status',
    title: '启停状态',
    width: 100,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: statusTag(row.status), bordered: false }, () => getOptionLabel(statusOptions, row.status))
    },
  },
  { key: 'priority', title: '优先级', minWidth: 86, sorter: true },
  { key: 'reviewLevel', title: '审批级别', minWidth: 100 },
  { key: 'currentLevel', title: '当前级别', minWidth: 110 },
  { key: 'submitUserId', title: '提交人', minWidth: 110 },
  {
    key: 'submitTime',
    title: '提交时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.submitTime))
    },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 180,
    render(row) {
      return h(NSpace, { size: 4 }, () => [
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleDetail(row) }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })),
            }),
          default: () => '详情',
        }),
        ...(row.reviewStatus === AuditStatus.Pending || row.reviewStatus === AuditStatus.InProgress
          ? [
              h(NTooltip, null, {
                trigger: () =>
                  h(NButton, { ariaLabel: '通过', circle: true, quaternary: true, size: 'small', type: 'success', onClick: () => { handleDetail(row); openApproveDialog(AuditResult.Pass) } }, {
                    icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:check' })),
                  }),
                default: () => '通过',
              }),
              h(NTooltip, null, {
                trigger: () =>
                  h(NButton, { ariaLabel: '拒绝', circle: true, quaternary: true, size: 'small', type: 'error', onClick: () => { handleDetail(row); openApproveDialog(AuditResult.Reject) } }, {
                    icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:x' })),
                  }),
                default: () => '拒绝',
              }),
            ]
          : []),
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              ariaLabel: '启停',
              circle: true,
              quaternary: true,
              size: 'small',
              type: row.status === EnableStatus.Enabled ? 'warning' : 'success',
              onClick: () => handleToggleStatus(row),
            }, {
              icon: () => h(NIcon, { icon: row.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play' }),
            }),
          default: () => row.status === EnableStatus.Enabled ? '停用' : '启用',
        }),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () =>
            h(NButton, { ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error', loading: actionLoading.value }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })),
            }),
          default: () => '确定删除该审批？删除后不可恢复。',
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
  queryParams.reviewResult = undefined
  queryParams.reviewStatus = undefined
  queryParams.status = undefined
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
    fetchData()
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
    fetchData()
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
    fetchData()
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
    fetchData()
  }
  catch {
    message.error('删除失败')
  }
  finally {
    actionLoading.value = false
  }
}

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
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
