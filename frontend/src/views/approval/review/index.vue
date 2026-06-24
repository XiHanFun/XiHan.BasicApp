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
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { approvalManagementApi, AuditResult, AuditStatus, createPageRequest, EnableStatus, querySortsFromSchema } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformApprovalPage' })

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const { t } = useI18n()
const message = useMessage()
const statusOptions = STATUS_OPTIONS

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const reviewStatusOptions = computed(() => [
  { label: t('approval.review.status_pending'), value: AuditStatus.Pending },
  { label: t('approval.review.status_in_progress'), value: AuditStatus.InProgress },
  { label: t('approval.review.status_approved'), value: AuditStatus.Approved },
  { label: t('approval.review.status_rejected'), value: AuditStatus.Rejected },
  { label: t('approval.review.status_withdrawn'), value: AuditStatus.Withdrawn },
])

const reviewResultOptions = computed(() => [
  { label: t('approval.review.result_pass'), value: AuditResult.Pass },
  { label: t('approval.review.result_reject'), value: AuditResult.Reject },
  { label: t('approval.review.result_return'), value: AuditResult.Return },
])

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
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('approval.review.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('approval.review.keyword_placeholder'), width: 240, order: 0 },
  // 常用搜索 + 列
  {
    key: 'reviewStatus',
    title: t('approval.review.review_status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    dictionaryCode: 'AuditStatus',
    options: reviewStatusOptions.value,
    searchPlaceholder: t('approval.review.review_status_placeholder'),
    width: 120,
    order: 1,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: reviewStatusTag(r.reviewStatus) }, () => getOptionLabel(reviewStatusOptions.value, r.reviewStatus))
    },
  },
  {
    key: 'reviewResult',
    title: t('approval.review.review_result'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    dictionaryCode: 'AuditResult',
    options: reviewResultOptions.value,
    searchPlaceholder: t('approval.review.review_result_placeholder'),
    width: 120,
    order: 2,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: reviewResultTag(r.reviewResult) }, () => (r.reviewResult === null || r.reviewResult === undefined ? t('approval.review.no_result') : getOptionLabel(reviewResultOptions.value, r.reviewResult)))
    },
  },
  {
    key: 'status',
    title: t('approval.review.enable_status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions,
    searchPlaceholder: t('approval.review.enable_status_placeholder'),
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: statusTag(r.status) }, () => getOptionLabel(statusOptions, r.status))
    },
  },
  // 仅列（不搜索）
  { key: 'reviewTitle', title: t('approval.review.review_title'), dataType: 'string', sortable: true, minWidth: 220, order: 10 },
  { key: 'reviewCode', title: t('approval.review.review_code'), dataType: 'string', sortable: true, minWidth: 160, order: 11 },
  { key: 'reviewType', title: t('approval.review.review_type'), dataType: 'string', sortable: true, minWidth: 130, order: 12 },
  { key: 'entityType', title: t('approval.review.entity_type'), dataType: 'string', minWidth: 130, order: 13 },
  { key: 'entityId', title: t('approval.review.entity_id'), dataType: 'string', minWidth: 150, order: 14 },
  { key: 'priority', title: t('approval.review.priority'), dataType: 'number', sortable: true, width: 90, order: 15 },
  { key: 'reviewLevel', title: t('approval.review.review_level'), dataType: 'number', sortable: true, width: 100, order: 16 },
  { key: 'currentLevel', title: t('approval.review.current_level'), dataType: 'number', sortable: true, width: 110, order: 17 },
  { key: 'submitUserId', title: t('approval.review.submit_user'), dataType: 'string', minWidth: 110, order: 18 },
  { key: 'submitTime', title: t('approval.review.submit_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 19 },
  { key: 'createdTime', title: t('approval.review.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 20 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.approval',
  exportPermission: 'saas:review:export',
  pageName: t('approval.review.page_name'),
  batchRemovable: true,
  removePermission: 'saas:review:delete',
  rowKey: 'basicId',
  scrollX: 2000,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return approvalManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sortField, params.sortOrder) },
        }),
        keyword: toStr(f.keyword),
        reviewStatus: (f.reviewStatus as AuditStatus | undefined) ?? undefined,
        reviewResult: (f.reviewResult as AuditResult | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => approvalManagementApi.delete(id),
  },
  actions: [
    { key: 'view', title: t('approval.review.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'approve', title: t('approval.review.action_approve'), scope: 'row', type: 'success', visible: row => canAuditRow(row as unknown as ReviewListItemDto) },
    { key: 'reject', title: t('approval.review.action_reject'), scope: 'row', type: 'error', visible: row => canAuditRow(row as unknown as ReviewListItemDto) },
    { key: 'toggle', title: t('approval.review.action_toggle'), scope: 'row' },
    { key: 'delete', title: t('approval.review.action_delete'), scope: 'row', type: 'error' },
  ],
}))

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
    message.error(t('approval.review.err_load_detail'))
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
    message.success(auditResult.value === AuditResult.Pass ? t('approval.review.msg_passed') : auditResult.value === AuditResult.Reject ? t('approval.review.msg_rejected') : t('approval.review.msg_returned'))
    approveVisible.value = false
    detailVisible.value = false
    reload()
  }
  catch {
    message.error(t('approval.review.err_audit'))
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
    message.success(t('approval.review.msg_withdrawn'))
    detailVisible.value = false
    reload()
  }
  catch {
    message.error(t('approval.review.err_withdraw'))
  }
  finally {
    actionLoading.value = false
  }
}

async function handleToggleStatus(row: ReviewListItemDto) {
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await approvalManagementApi.updateStatus({ basicId: row.basicId, status: newStatus })
    message.success(newStatus === EnableStatus.Enabled ? t('approval.review.msg_enabled') : t('approval.review.msg_disabled'))
    reload()
  }
  catch {
    message.error(t('approval.review.err_update_status'))
  }
}

async function handleDelete(row: ReviewListItemDto) {
  try {
    await approvalManagementApi.delete(row.basicId)
    message.success(t('approval.review.msg_deleted'))
    reload()
  }
  catch {
    message.error(t('approval.review.err_delete'))
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
      <NDrawerContent closable :title="t('approval.review.detail_title')">
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('approval.review.loading') }}
        </div>
        <NDescriptions v-else-if="detailData" :column="1" bordered label-placement="left" size="small">
          <NDescriptionsItem :label="t('approval.review.review_title')">
            {{ detailData.reviewTitle }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_code')">
            {{ detailData.reviewCode }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_type')">
            {{ detailData.reviewType }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.entity_type')">
            {{ detailData.entityType || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.entity_id')">
            {{ detailData.entityId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_status')">
            <NTag :type="reviewStatusTag(detailData.reviewStatus)" round size="small">
              {{ getOptionLabel(reviewStatusOptions, detailData.reviewStatus) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_result')">
            <NTag v-if="detailData.reviewResult !== null && detailData.reviewResult !== undefined" :type="reviewResultTag(detailData.reviewResult)" round size="small">
              {{ getOptionLabel(reviewResultOptions, detailData.reviewResult) }}
            </NTag>
            <span v-else>{{ t('approval.review.no_result') }}</span>
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.enable_status')">
            <NTag :type="statusTag(detailData.status)" round size="small">
              {{ getOptionLabel(statusOptions, detailData.status) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_level')">
            {{ t('approval.review.level_unit', { current: detailData.currentLevel, total: detailData.reviewLevel }) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.priority')">
            {{ detailData.priority }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.submit_user')">
            {{ detailData.submitUserId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.current_review_user')">
            {{ detailData.currentReviewUserId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_user_ids')">
            {{ detailData.reviewUserIds || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.submit_time')">
            {{ formatDate(detailData.submitTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_start_time')">
            {{ formatNullableDate(detailData.reviewStartTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_end_time')">
            {{ formatNullableDate(detailData.reviewEndTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_description')">
            {{ detailData.reviewDescription || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.review_content')">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.reviewContent || '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.business_data')">
            <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.businessData || '-' }}</pre>
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.created_time')">
            {{ formatNullableDate(detailData.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.modified_time')">
            {{ formatNullableDate(detailData.modifiedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('approval.review.remark')">
            {{ detailData.remark || '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div v-else class="py-8 text-center text-gray-400">
          {{ t('approval.review.empty_detail') }}
        </div>

        <template v-if="detailData && !detailLoading" #footer>
          <NDivider style="margin: 12px 0" />
          <div class="text-sm font-medium mb-2">
            {{ t('approval.review.review_operation') }}
          </div>
          <NSpace justify="start" :size="8">
            <NButton type="success" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Pass)">
              <template #icon>
                <NIcon><Icon icon="lucide:check" /></NIcon>
              </template>
              {{ t('approval.review.btn_pass') }}
            </NButton>
            <NButton type="error" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Reject)">
              <template #icon>
                <NIcon><Icon icon="lucide:x" /></NIcon>
              </template>
              {{ t('approval.review.btn_reject') }}
            </NButton>
            <NButton type="warning" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Return)">
              <template #icon>
                <NIcon><Icon icon="lucide:corner-down-left" /></NIcon>
              </template>
              {{ t('approval.review.btn_return') }}
            </NButton>
            <NPopconfirm @positive-click="handleWithdraw">
              <template #trigger>
                <NButton type="default" :disabled="!canWithdraw()" :loading="actionLoading">
                  <template #icon>
                    <NIcon><Icon icon="lucide:undo-2" /></NIcon>
                  </template>
                  {{ t('approval.review.btn_withdraw') }}
                </NButton>
              </template>
              {{ t('approval.review.withdraw_confirm') }}
            </NPopconfirm>
          </NSpace>
        </template>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="approveVisible" :width="420">
      <NDrawerContent closable :title="auditResult === AuditResult.Pass ? t('approval.review.approve_dialog_pass') : auditResult === AuditResult.Reject ? t('approval.review.approve_dialog_reject') : t('approval.review.approve_dialog_return')">
        <NSpace vertical>
          <NInput
            v-model:value="auditComment"
            :placeholder="t('approval.review.comment_placeholder')"
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
            {{ auditResult === AuditResult.Pass ? t('approval.review.confirm_pass') : auditResult === AuditResult.Reject ? t('approval.review.confirm_reject') : t('approval.review.confirm_return') }}
          </NButton>
        </NSpace>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>
