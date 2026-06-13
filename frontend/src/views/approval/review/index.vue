<script setup lang="ts">
import type { PageResult, ReviewDetailDto, ReviewListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
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
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, ref } from 'vue'
import { approvalManagementApi, AuditResult, AuditStatus, createPageRequest, EnableStatus } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformApprovalPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const statusOptions = STATUS_OPTIONS

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

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

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

/** 过滤值辅助：trim 字符串 */
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不作为列）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索标题/编码/业务', width: 240, order: 0 },
  // 常用搜索 + 列
  {
    key: 'reviewStatus',
    title: '审批状态',
    dataType: 'enum',
    searchable: true,
    options: reviewStatusOptions,
    searchPlaceholder: '审批状态',
    width: 120,
    order: 1,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: reviewStatusTag(r.reviewStatus) }, () => getOptionLabel(reviewStatusOptions, r.reviewStatus))
    },
  },
  {
    key: 'reviewResult',
    title: '审批结果',
    dataType: 'enum',
    searchable: true,
    options: reviewResultOptions,
    searchPlaceholder: '审批结果',
    width: 120,
    order: 2,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: reviewResultTag(r.reviewResult) }, () => (r.reviewResult === null || r.reviewResult === undefined ? '未出结果' : getOptionLabel(reviewResultOptions, r.reviewResult)))
    },
  },
  {
    key: 'status',
    title: '启停状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '启停状态',
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: statusTag(r.status) }, () => getOptionLabel(statusOptions, r.status))
    },
  },
  // 仅列（不搜索）
  { key: 'reviewTitle', title: '审批标题', dataType: 'string', minWidth: 220, order: 10 },
  { key: 'reviewCode', title: '审批编码', dataType: 'string', minWidth: 160, order: 11 },
  { key: 'reviewType', title: '审批类型', dataType: 'string', minWidth: 130, order: 12 },
  { key: 'entityType', title: '业务实体', dataType: 'string', minWidth: 130, order: 13 },
  { key: 'entityId', title: '业务主键', dataType: 'string', minWidth: 150, order: 14 },
  { key: 'priority', title: '优先级', dataType: 'number', sortable: true, width: 90, order: 15 },
  { key: 'reviewLevel', title: '审批级别', dataType: 'number', width: 100, order: 16 },
  { key: 'currentLevel', title: '当前级别', dataType: 'number', width: 110, order: 17 },
  { key: 'submitUserId', title: '提交人', dataType: 'string', minWidth: 110, order: 18 },
  { key: 'submitTime', title: '提交时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 19 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 20 },
]

const schema: PageSchema = {
  pageCode: 'platform.approval',
  exportPermission: 'saas:review:export',
  pageName: '审批中心',
  rowKey: 'basicId',
  scrollX: 2000,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return approvalManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        reviewStatus: (f.reviewStatus as AuditStatus | undefined) ?? undefined,
        reviewResult: (f.reviewResult as AuditResult | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => approvalManagementApi.delete(id),
  },
  actions: [
    { key: 'view', title: '查看流程', scope: 'row', icon: 'lucide:eye' },
    { key: 'approve', title: '通过', scope: 'row', type: 'success', visible: row => canAuditRow(row as unknown as ReviewListItemDto) },
    { key: 'reject', title: '拒绝', scope: 'row', type: 'error', visible: row => canAuditRow(row as unknown as ReviewListItemDto) },
    { key: 'toggle', title: '启用/停用', scope: 'row' },
    { key: 'delete', title: '删除', scope: 'row', type: 'error' },
  ],
}

function canAuditRow(row: ReviewListItemDto) {
  return row.reviewStatus === AuditStatus.Pending || row.reviewStatus === AuditStatus.InProgress
}

// ── 详情抽屉 + 审批/驳回/撤回（保留页面自有逻辑） ───────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<ReviewDetailDto | null>(null)
const actionLoading = ref(false)
const approveVisible = ref(false)
const auditResult = ref<AuditResult>(AuditResult.Pass)
const auditComment = ref('')

function canAudit() {
  if (!detailData.value)
    return false
  const status = detailData.value.reviewStatus
  return status === AuditStatus.Pending || status === AuditStatus.InProgress
}

function canWithdraw() {
  if (!detailData.value)
    return false
  const status = detailData.value.reviewStatus
  return status !== AuditStatus.Approved && status !== AuditStatus.Rejected && status !== AuditStatus.Withdrawn
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

/** 行级快捷审批：先取详情再打开审批弹窗 */
async function handleQuickAudit(row: ReviewListItemDto, result: AuditResult) {
  await handleDetail(row)
  if (detailData.value)
    openApproveDialog(result)
}

async function handleAudit() {
  if (!detailData.value)
    return
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
  if (!detailData.value)
    return
  actionLoading.value = true
  try {
    await approvalManagementApi.withdraw({ basicId: detailData.value.basicId })
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
  try {
    await approvalManagementApi.updateStatus({ basicId: row.basicId, status: newStatus })
    message.success(newStatus === EnableStatus.Enabled ? '已启用' : '已停用')
    reload()
  }
  catch {
    message.error('更新状态失败')
  }
}

async function handleDelete(row: ReviewListItemDto) {
  try {
    await approvalManagementApi.delete(row.basicId)
    message.success('已删除')
    reload()
  }
  catch {
    message.error('删除失败')
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ReviewListItemDto | undefined
  switch (payload.key) {
    case 'view':
      if (row)
        void handleDetail(row)
      break
    case 'approve':
      if (row)
        void handleQuickAudit(row, AuditResult.Pass)
      break
    case 'reject':
      if (row)
        void handleQuickAudit(row, AuditResult.Reject)
      break
    case 'toggle':
      if (row)
        void handleToggleStatus(row)
      break
    case 'delete':
      if (row)
        void handleDelete(row)
      break
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
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
          <div class="text-sm font-medium mb-2">
            审批操作
          </div>
          <NSpace justify="start" :size="8">
            <NButton type="success" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Pass)">
              <template #icon>
                <NIcon><Icon icon="lucide:check" /></NIcon>
              </template>
              通过
            </NButton>
            <NButton type="error" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Reject)">
              <template #icon>
                <NIcon><Icon icon="lucide:x" /></NIcon>
              </template>
              拒绝
            </NButton>
            <NButton type="warning" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Return)">
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
              <NIcon><Icon :icon="auditResult === AuditResult.Pass ? 'lucide:check' : auditResult === AuditResult.Reject ? 'lucide:x' : 'lucide:corner-down-left'" /></NIcon>
            </template>
            确认{{ auditResult === AuditResult.Pass ? '通过' : auditResult === AuditResult.Reject ? '拒绝' : '退回' }}
          </NButton>
        </NSpace>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>
