<script lang="ts" setup>
import type { CloseStatus, PermissionRequestRow, RequestTargetKind } from '../use-permission-requests'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { actionConfirmText, renderTag, SchemaPage, XCombobox, XDatePicker, XEditModal, XInput, XSegmented } from '~/components'
import { toast } from '~/composables'
import { PermissionRequestStatus as REQUEST_STATUSES } from '~/contracts'
import { useAuthStore } from '~/stores'
import {
  canClose,
  canResubmit,
  canTrace,
  canWithdraw,
  expectedValidity,
  permissionRequestsResource,
  REQUEST_STATUS_TONE,
  requestName,
  requestStatusLabel,
  requestTarget,
  usePermissionRequestCloser,
  usePermissionRequestEditor,
  withdrawPermissionRequest,
} from '../use-permission-requests'
import { workflowProgressText, workflowProgressTone } from '../workflow-progress'

/**
 * 授权申请页的「申请」页签（照 4.x）：为自己申请角色或权限，提交即进入审批流；审批在审批中心办理，这里看进度、跳过去看轨迹
 * 另补上 4.x 后端有、前端没接的「修改重提」与「手工驳回 / 撤回」（W2）
 */
defineOptions({ name: 'AccessPermissionRequestsTab' })

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const resource = permissionRequestsResource()
const editor = usePermissionRequestEditor()
const closer = usePermissionRequestCloser()

/** 弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const closeFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

/** 当前用户：自己的申请可撤回、修改重提，别人的申请可手工处理 */
function me(): null | string {
  return auth.user?.id ?? null
}

const statusOptions = computed<SelectOption[]>(() => REQUEST_STATUSES.map(status => ({
  label: requestStatusLabel(t, status),
  value: status,
})))
const targetKindOptions = computed(() => [
  {
    value: 'role',
    label: t('access.permission_requests.kind_role'),
  },
  {
    value: 'permission',
    label: t('access.permission_requests.kind_permission'),
  },
])
const closeStatusOptions = computed(() => [
  {
    value: 'Rejected',
    label: t('access.permission_requests.close_rejected'),
  },
  {
    value: 'Withdrawn',
    label: t('access.permission_requests.close_withdrawn'),
  },
])

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的） ──
const asRow = (row: object) => row as PermissionRequestRow

/** 审批进度：有流程时出标签，没有流程或审批未启用时出文字（4.x 同） */
function progressCell(row: PermissionRequestRow) {
  const text = workflowProgressText(t, row.workflow, row.approvalEnabled)
  return row.approvalEnabled && row.workflow
    ? renderTag(text, workflowProgressTone(row.workflow))
    : text
}

const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('access.permission_requests.fields.keyword'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('access.permission_requests.keyword_placeholder'),
    width: 240,
    order: 0,
  },
  {
    key: 'requestUserName',
    title: t('access.permission_requests.fields.applicant'),
    dataType: 'string',
    minWidth: 140,
    order: 1,
    render: row => asRow(row).requestUserName ?? asRow(row).requestUserId,
  },
  {
    key: 'target',
    title: t('access.permission_requests.fields.target'),
    dataType: 'string',
    minWidth: 160,
    order: 2,
    render: row => requestTarget(asRow(row)),
  },
  {
    key: 'reason',
    title: t('access.permission_requests.fields.reason'),
    dataType: 'string',
    minWidth: 200,
    order: 3,
  },
  {
    key: 'status',
    title: t('access.permission_requests.fields.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: statusOptions.value,
    searchPlaceholder: t('access.permission_requests.status_placeholder'),
    width: 100,
    order: 4,
    render: row => renderTag(requestStatusLabel(t, asRow(row).status), REQUEST_STATUS_TONE[asRow(row).status]),
  },
  {
    key: 'workflow',
    title: t('access.permission_requests.fields.progress'),
    dataType: 'string',
    minWidth: 200,
    ellipsis: true,
    order: 4.5,
    render: row => progressCell(asRow(row)),
  },
  {
    key: 'expectedExpiresAt',
    title: t('access.permission_requests.fields.expected_validity'),
    dataType: 'string',
    minWidth: 300,
    order: 5,
    render: row => expectedValidity(t, asRow(row)),
  },
  {
    key: 'createdAt',
    title: t('access.permission_requests.fields.created_at'),
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 6,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.permission-requests',
  pageName: t('access.permission_requests.tab'),
  exportPermission: 'access.permission-requests.read',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'create',
      title: t('access.permission_requests.actions.create'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:plus',
      permission: 'access.permission-requests.create',
    },
    // 审批在审批中心办理；这里只跳过去看进度与轨迹（发起人、审批人或有流程查看权的人可看）
    {
      key: 'trace',
      title: t('access.permission_requests.actions.trace'),
      scope: 'row',
      icon: 'lucide:route',
      visible: row => canTrace(asRow(row)),
    },
    {
      key: 'resubmit',
      title: t('access.permission_requests.actions.resubmit'),
      scope: 'row',
      icon: 'lucide:pencil',
      visible: row => canResubmit(asRow(row), me()),
      permission: 'access.permission-requests.update',
    },
    {
      key: 'withdraw',
      title: t('access.permission_requests.actions.withdraw'),
      scope: 'row',
      icon: 'lucide:undo-2',
      tone: 'danger',
      confirm: true,
      confirmText: row => actionConfirmText(t, t('access.permission_requests.actions.withdraw'), requestName(asRow(row))),
      visible: row => canWithdraw(asRow(row), me()),
      permission: 'access.permission-requests.withdraw',
    },
    {
      key: 'close',
      title: t('access.permission_requests.actions.close'),
      scope: 'row',
      icon: 'lucide:ban',
      tone: 'danger',
      visible: row => canClose(asRow(row), me()),
      permission: 'access.permission-requests.status',
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'create':
      editor.start().catch((error: unknown) => toast.danger((error as Error).message || t('access.permission_requests.msg_load_candidates_failed')))
      break
    case 'trace':
      if (row?.workflowInstanceId) {
        void router.push({
          path: '/workflow/approvals',
          query: { instanceId: row.workflowInstanceId },
        })
      }
      break
    case 'resubmit':
      if (row)
        editor.start(row).catch((error: unknown) => toast.danger((error as Error).message || t('access.permission_requests.msg_load_candidates_failed')))
      break
    case 'withdraw':
      if (row)
        void withdraw(row)
      break
    case 'close':
      if (row)
        closer.start(row)
      break
  }
}

async function withdraw(row: PermissionRequestRow) {
  try {
    await withdrawPermissionRequest(row.id)
    toast.success(t('access.permission_requests.msg_withdrawn'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.permission_requests.msg_withdraw_failed'))
  }
}

/** 4.x 的提交前校验：缺什么先提示，不发请求 */
function validate(): boolean {
  const form = editor.form
  if (form.targetKind === 'role' && !form.roleId) {
    toast.warning(t('access.permission_requests.msg_role_required'))
    return false
  }
  if (form.targetKind === 'permission' && !form.permissionCode) {
    toast.warning(t('access.permission_requests.msg_permission_required'))
    return false
  }
  if (!form.reason.trim()) {
    toast.warning(t('access.permission_requests.msg_reason_required'))
    return false
  }
  return true
}

async function onSubmit() {
  if (!validate())
    return
  const resubmitting = editor.editingId.value !== null
  if (await editor.submit()) {
    toast.success(resubmitting
      ? t('access.permission_requests.msg_resubmitted')
      : t('access.permission_requests.msg_submitted'))
    reload()
  }
}

async function onClose() {
  const status = closer.status.value
  if (await closer.submit()) {
    toast.success(status === 'Rejected'
      ? t('access.permission_requests.msg_rejected')
      : t('access.permission_requests.msg_withdrawn'))
    reload()
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="editor.open.value"
      :title="editor.editingId.value ? t('access.permission_requests.resubmit_title') : t('access.permission_requests.create_title')"
      :loading="editor.submitting.value"
      :save-text="editor.editingId.value ? t('access.permission_requests.btn_resubmit') : t('access.permission_requests.btn_submit')"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="permission-request-form" @submit="onSubmit">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.permission_requests.fields.target_kind') }}</XhFieldLabel>
          <XhFieldControl>
            <XSegmented :value="editor.form.targetKind" :options="targetKindOptions" @update:value="value => editor.form.targetKind = value as RequestTargetKind" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.form.targetKind === 'role'" :invalid="!!editor.fieldError('roleId')" required>
          <XhFieldLabel>{{ t('access.permission_requests.fields.role') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="req-combobox"
              :options="editor.roleOptions.value"
              :value="editor.form.roleId || null"
              :loading="editor.targetLoading.value"
              :placeholder="t('access.permission_requests.ph_role')"
              @update:value="value => editor.form.roleId = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('roleId')">
            {{ editor.fieldError('roleId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-else :invalid="!!editor.fieldError('permissionCode')" required>
          <XhFieldLabel>{{ t('access.permission_requests.fields.permission') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="req-combobox"
              :options="editor.permissionOptions.value"
              :value="editor.form.permissionCode || null"
              :loading="editor.targetLoading.value"
              :placeholder="t('access.permission_requests.ph_permission')"
              @update:value="value => editor.form.permissionCode = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('permissionCode')">
            {{ editor.fieldError('permissionCode') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('expectedEffectiveAt')">
          <XhFieldLabel>{{ t('access.permission_requests.fields.expected_effective_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="editor.form.expectedEffectiveAt" clearable show-time />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('expectedEffectiveAt')">
            {{ editor.fieldError('expectedEffectiveAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('expectedExpiresAt')">
          <XhFieldLabel>{{ t('access.permission_requests.fields.expected_expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="editor.form.expectedExpiresAt" clearable show-time />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('expectedExpiresAt')">
            {{ editor.fieldError('expectedExpiresAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('reason')" required>
          <XhFieldLabel>{{ t('access.permission_requests.fields.reason') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.reason" name="reason" type="textarea" :rows="2" clearable :placeholder="t('access.permission_requests.ph_reason')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('reason')">
            {{ editor.fieldError('reason') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('remark')">
          <XhFieldLabel>{{ t('access.permission_requests.fields.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.remark" name="remark" type="textarea" :rows="2" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('remark')">
            {{ editor.fieldError('remark') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="closer.open.value"
      :title="t('access.permission_requests.close_title')"
      :loading="closer.submitting.value"
      :save-text="t('common.actions.confirm')"
      save-tone="danger"
      :form-id="closeFormId"
    >
      <XhFormRoot :id="closeFormId" class="xh-edit-form-grid" data-testid="permission-request-close-form" @submit="onClose">
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('access.permission_requests.fields.request') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput :value="closer.target.value ? requestName(closer.target.value) : ''" read-only />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('access.permission_requests.fields.close_status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSegmented :value="closer.status.value" :options="closeStatusOptions" @update:value="value => closer.status.value = value as CloseStatus" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('access.permission_requests.fields.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="closer.remark.value" name="remark" type="textarea" :rows="2" clearable :placeholder="t('access.permission_requests.ph_close_remark')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="closer.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ closer.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
/* 组合框默认按内容定宽，这里与字段一样铺满 */
.req-combobox {
  inline-size: 100%;
}
</style>
