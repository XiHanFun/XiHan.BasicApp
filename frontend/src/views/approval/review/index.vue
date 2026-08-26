<script setup lang="ts">
import type { Tone } from '@xihan-ui/kernel'
import type { PageResult, ReviewDetailDto, ReviewListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFlex, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmDescription, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTrigger, XhSeparator, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { approvalManagementApi, AuditResult, AuditStatus, createPageRequest, EnableStatus, querySortsFromSchema } from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XInput, XJsonBlock } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformApprovalPage' })

const { t } = useI18n()
const statusOptions = STATUS_OPTIONS
const enableStatusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

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

function reviewStatusTag(status: AuditStatus): Tone {
  switch (status) {
    case AuditStatus.Approved:
      return 'success'
    case AuditStatus.Rejected:
      return 'danger'
    case AuditStatus.InProgress:
      return 'warning'
    case AuditStatus.Withdrawn:
      return 'neutral'
    default:
      return 'info'
  }
}

function reviewResultTag(result?: AuditResult | null): Tone {
  switch (result) {
    case AuditResult.Pass:
      return 'success'
    case AuditResult.Reject:
      return 'danger'
    case AuditResult.Return:
      return 'warning'
    default:
      return 'neutral'
  }
}

function statusTag(status: EnableStatus): Tone {
  return status === EnableStatus.Enabled ? 'success' : 'danger'
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
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'AuditStatus',
    options: reviewStatusOptions.value,
    searchPlaceholder: t('approval.review.review_status_placeholder'),
    width: 120,
    order: 1,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(XhTagRoot, { variant: 'solid', tone: reviewStatusTag(r.reviewStatus) }, () => h(XhTagLabel, () => getOptionLabel(reviewStatusOptions.value, r.reviewStatus)))
    },
  },
  {
    key: 'reviewResult',
    title: t('approval.review.review_result'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'AuditResult',
    options: reviewResultOptions.value,
    searchPlaceholder: t('approval.review.review_result_placeholder'),
    width: 120,
    order: 2,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(XhTagRoot, { variant: 'solid', tone: reviewResultTag(r.reviewResult) }, () => h(XhTagLabel, () => (r.reviewResult === null || r.reviewResult === undefined ? t('approval.review.no_result') : getOptionLabel(reviewResultOptions.value, r.reviewResult))))
    },
  },
  {
    key: 'status',
    title: t('approval.review.enable_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions,
    searchPlaceholder: t('approval.review.enable_status_placeholder'),
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as ReviewListItemDto
      return h(XhTagRoot, { variant: 'solid', tone: statusTag(r.status) }, () => h(XhTagLabel, () => getOptionLabel(enableStatusOptions.value, r.status)))
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
  { key: 'submitTime', title: t('approval.review.submit_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 19 },
  { key: 'createdTime', title: t('approval.review.created_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 20 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.approval',
  exportPermission: 'saas:review:export',
  pageName: t('approval.review.page_name'),
  batchRemovable: true,
  removePermission: 'saas:review:delete',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return approvalManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 区间(submitTime/createdTime)/多选(reviewStatus/reviewResult/status) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword),
        // reviewStatus/reviewResult/status 改为多选，经 conditions.filters In 下发（不再走 DTO 单值字段）
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
  catch (error) {
    toast.error((error as Error)?.message || t('approval.review.err_load_detail'))
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
    toast.success(auditResult.value === AuditResult.Pass ? t('approval.review.msg_passed') : auditResult.value === AuditResult.Reject ? t('approval.review.msg_rejected') : t('approval.review.msg_returned'))
    approveVisible.value = false
    detailVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('approval.review.err_audit'))
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
    toast.success(t('approval.review.msg_withdrawn'))
    detailVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('approval.review.err_withdraw'))
  }
  finally {
    actionLoading.value = false
  }
}

async function handleToggleStatus(row: ReviewListItemDto) {
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await approvalManagementApi.updateStatus({ basicId: row.basicId, status: newStatus })
    toast.success(newStatus === EnableStatus.Enabled ? t('approval.review.msg_enabled') : t('approval.review.msg_disabled'))
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('approval.review.err_update_status'))
  }
}

async function handleDelete(row: ReviewListItemDto) {
  try {
    await approvalManagementApi.delete(row.basicId)
    toast.success(t('approval.review.msg_deleted'))
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('approval.review.err_delete'))
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
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 660px">
        <XhDrawerTitle>{{ t('approval.review.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div v-if="detailLoading" class="py-8 text-center text-gray-400">
          {{ t('approval.review.loading') }}
        </div>
        <XhDescriptionsRoot v-else-if="detailData" :columns="1" bordered placement="left" size="sm">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_title') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.reviewTitle }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_code') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.reviewCode }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_type') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.reviewType }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.entity_type') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.entityType || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.entity_id') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.entityId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <XhTagRoot variant="subtle" :tone="reviewStatusTag(detailData.reviewStatus)" size="sm">
                <XhTagLabel>
                  {{ getOptionLabel(reviewStatusOptions, detailData.reviewStatus) }}
                </XhTagLabel>
              </XhTagRoot>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_result') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <XhTagRoot v-if="detailData.reviewResult !== null && detailData.reviewResult !== undefined" variant="subtle" :tone="reviewResultTag(detailData.reviewResult)" size="sm">
                <XhTagLabel>
                  {{ getOptionLabel(reviewResultOptions, detailData.reviewResult) }}
                </XhTagLabel>
              </XhTagRoot>
              <span v-else>{{ t('approval.review.no_result') }}</span>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.enable_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <XhTagRoot variant="subtle" :tone="statusTag(detailData.status)" size="sm">
                <XhTagLabel>
                  {{ getOptionLabel(enableStatusOptions, detailData.status) }}
                </XhTagLabel>
              </XhTagRoot>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_level') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ t('approval.review.level_unit', { current: detailData.currentLevel, total: detailData.reviewLevel }) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.priority') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.priority }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.submit_user') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.submitUserId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.current_review_user') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.currentReviewUserId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_user_ids') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.reviewUserIds || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.submit_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDate(detailData.submitTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_start_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(detailData.reviewStartTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_end_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(detailData.reviewEndTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_description') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.reviewDescription || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.review_content') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <pre class="m-0 whitespace-pre-wrap break-all">{{ detailData.reviewContent || '-' }}</pre>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.business_data') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <XJsonBlock :raw="detailData.businessData" :default-expanded-depth="2" max-height="18rem" />
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.created_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(detailData.createdTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.modified_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatNullableDate(detailData.modifiedTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('approval.review.remark') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detailData.remark || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>
        <div v-else class="py-8 text-center text-gray-400">
          {{ t('approval.review.empty_detail') }}
        </div>

        <!-- 审批操作条排在详情之后，抽屉内容区的末尾 -->
        <template v-if="detailData && !detailLoading">
          <XhSeparator style="margin: 12px 0" />
          <div class="text-sm font-medium mb-2">
            {{ t('approval.review.review_operation') }}
          </div>
          <XhFlex justify="start" gap="sm">
            <XhButton tone="success" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Pass)">
              <span><Icon icon="lucide:check" /></span>
              {{ t('approval.review.btn_pass') }}
            </XhButton>
            <XhButton tone="danger" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Reject)">
              <span><Icon icon="lucide:x" /></span>
              {{ t('approval.review.btn_reject') }}
            </XhButton>
            <XhButton tone="warning" :disabled="!canAudit()" :loading="actionLoading" @click="openApproveDialog(AuditResult.Return)">
              <span><Icon icon="lucide:corner-down-left" /></span>
              {{ t('approval.review.btn_return') }}
            </XhButton>
            <XhPopconfirmRoot @confirm="handleWithdraw">
              <XhPopconfirmTrigger
                class="xh-linklike-trigger"
                :disabled="!canWithdraw()"
                :data-loading="actionLoading || undefined"
              >
                <span><Icon icon="lucide:undo-2" /></span>
                {{ t('approval.review.btn_withdraw') }}
              </XhPopconfirmTrigger>
              <XhPopconfirmPositioner>
                <XhPopconfirmContent>
                  <XhPopconfirmDescription>{{ t('approval.review.withdraw_confirm') }}</XhPopconfirmDescription>
                  <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                  <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                </XhPopconfirmContent>
              </XhPopconfirmPositioner>
            </XhPopconfirmRoot>
          </XhFlex>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XhDrawerRoot v-model:open="approveVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 420px">
        <XhDrawerTitle>{{ auditResult === AuditResult.Pass ? t('approval.review.approve_dialog_pass') : auditResult === AuditResult.Reject ? t('approval.review.approve_dialog_reject') : t('approval.review.approve_dialog_return') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <XhFlex direction="column" gap="md">
          <XInput
            v-model:value="auditComment"
            :placeholder="t('approval.review.comment_placeholder')"
            type="textarea"
            :autosize="{ minRows: 2, maxRows: 6 }"
          />
          <XhButton
            block
            :tone="auditResult === AuditResult.Pass ? 'success' : auditResult === AuditResult.Reject ? 'danger' : 'warning'"
            :loading="actionLoading"
            @click="handleAudit"
          >
            <span><Icon :icon="auditResult === AuditResult.Pass ? 'lucide:check' : auditResult === AuditResult.Reject ? 'lucide:x' : 'lucide:corner-down-left'" /></span>
            {{ auditResult === AuditResult.Pass ? t('approval.review.confirm_pass') : auditResult === AuditResult.Reject ? t('approval.review.confirm_reject') : t('approval.review.confirm_return') }}
          </XhButton>
        </XhFlex>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>
