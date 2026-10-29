<script lang="ts" setup>
import type { DelegationRow, DelegationStatus, DelegationTargetKind } from './use-delegations'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, deleteConfirmText, renderTag, SchemaPage, XCombobox, XDatePicker, XEditModal, XInput, XSegmented } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'
import { canRevoke, DELEGATION_STATUSES, delegationName, delegationsResource, delegationTarget, revokeDelegation, useDelegationEditor } from './use-delegations'

defineOptions({ name: 'AccessDelegationsView' })

const { t } = useI18n()
const resource = delegationsResource()
const editor = useDelegationEditor()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: DelegationStatus): string {
  switch (status) {
    case 'Pending': return t('access.delegations.statuses.pending')
    case 'Expired': return t('access.delegations.statuses.expired')
    case 'Revoked': return t('access.delegations.statuses.revoked')
    default: return t('access.delegations.statuses.active')
  }
}

const STATUS_TONE: Record<DelegationStatus, 'danger' | 'neutral' | 'success' | 'warning'> = {
  Pending: 'warning',
  Active: 'success',
  Expired: 'neutral',
  Revoked: 'danger',
}

/** 4.x 的「{from} 至 {to}」：没有生效时间即创建即生效 */
function validityRange(row: DelegationRow): string {
  return t('access.delegations.validity_range', { from: row.effectiveAt ? formatDate(row.effectiveAt) : '-', to: formatDate(row.expiresAt) })
}

const statusOptions = computed<SelectOption[]>(() => DELEGATION_STATUSES.map(status => ({ label: statusLabel(status), value: status })))
const targetKindOptions = computed(() => [
  { value: 'role', label: t('access.delegations.kind_role') },
  { value: 'permission', label: t('access.delegations.kind_permission') },
])

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的） ──
const asRow = (row: object) => row as DelegationRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('access.delegations.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('access.delegations.keyword_placeholder'), width: 240, order: 0 },
  { key: 'delegatorName', title: t('access.delegations.fields.delegator'), dataType: 'string', minWidth: 160, order: 1, render: row => asRow(row).delegatorName ?? asRow(row).delegatorId },
  { key: 'delegateeName', title: t('access.delegations.fields.delegatee'), dataType: 'string', minWidth: 160, order: 2, render: row => asRow(row).delegateeName ?? asRow(row).delegateeId },
  { key: 'target', title: t('access.delegations.fields.target'), dataType: 'string', minWidth: 170, order: 3, render: row => delegationTarget(asRow(row)) },
  { key: 'status', title: t('access.delegations.fields.status'), dataType: 'enum', searchable: true, searchMultiple: true, options: statusOptions.value, searchPlaceholder: t('access.delegations.status_placeholder'), width: 100, order: 4, render: row => renderTag(statusLabel(asRow(row).status), STATUS_TONE[asRow(row).status]) },
  { key: 'expiresAt', title: t('access.delegations.fields.validity'), dataType: 'string', sortable: true, minWidth: 300, order: 5, render: row => validityRange(asRow(row)) },
  { key: 'reason', title: t('access.delegations.fields.reason'), dataType: 'text', ellipsis: true, minWidth: 160, order: 6 },
  { key: 'createdAt', title: t('access.delegations.fields.created_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.delegations',
  pageName: t('access.delegations.title'),
  exportPermission: 'access.delegations.read',
  batchRemovable: true,
  removePermission: 'access.delegations.delete',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('access.delegations.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'access.delegations.create' },
    { key: 'revoke', title: t('access.delegations.actions.revoke'), scope: 'row', icon: 'lucide:undo-2', tone: 'danger', confirm: true, confirmText: row => actionConfirmText(t, t('access.delegations.actions.revoke'), delegationName(asRow(row))), visible: row => canRevoke(asRow(row)), permission: 'access.delegations.revoke' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, delegationName(asRow(row))), permission: 'access.delegations.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.start().catch((error: unknown) => toast.danger((error as Error).message || t('access.delegations.msg_load_candidates_failed')))
      break
    case 'revoke':
      if (row)
        void revoke(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

function onSearchUsers(keyword: string) {
  editor.searchUsers(keyword).catch((error: unknown) => toast.danger((error as Error).message || t('access.delegations.msg_load_candidates_failed')))
}

async function revoke(row: DelegationRow) {
  try {
    await revokeDelegation(row.id)
    toast.success(t('access.delegations.msg_revoked'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.delegations.msg_revoke_failed'))
  }
}

async function removeRow(row: DelegationRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

/** 4.x 的提交前校验：缺什么先提示，不发请求 */
function validate(): boolean {
  const form = editor.form
  if (!form.delegatorId) {
    toast.warning(t('access.delegations.msg_delegator_required'))
    return false
  }
  if (!form.delegateeId) {
    toast.warning(t('access.delegations.msg_delegatee_required'))
    return false
  }
  if (form.targetKind === 'role' && !form.roleId) {
    toast.warning(t('access.delegations.msg_role_required'))
    return false
  }
  if (form.targetKind === 'permission' && !form.permission) {
    toast.warning(t('access.delegations.msg_permission_required'))
    return false
  }
  if (!form.expiresAt) {
    toast.warning(t('access.delegations.msg_expiration_required'))
    return false
  }
  return true
}

async function onSubmit() {
  if (!validate())
    return
  if (await editor.submit()) {
    toast.success(t('access.delegations.msg_created'))
    reload()
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="editor.open.value"
      :title="t('access.delegations.create_title')"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="delegation-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('delegatorId')" required>
          <XhFieldLabel>{{ t('access.delegations.fields.delegator') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="dlg-combobox"
              remote
              :options="editor.userOptions.value"
              :value="editor.form.delegatorId || null"
              :loading="editor.userLoading.value"
              :placeholder="t('access.delegations.ph_delegator')"
              @search="onSearchUsers"
              @update:value="value => editor.form.delegatorId = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('delegatorId')">
            {{ editor.fieldError('delegatorId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('delegateeId')" required>
          <XhFieldLabel>{{ t('access.delegations.fields.delegatee') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="dlg-combobox"
              remote
              :options="editor.userOptions.value"
              :value="editor.form.delegateeId || null"
              :loading="editor.userLoading.value"
              :placeholder="t('access.delegations.ph_delegatee')"
              @search="onSearchUsers"
              @update:value="value => editor.form.delegateeId = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('delegateeId')">
            {{ editor.fieldError('delegateeId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.delegations.fields.target_kind') }}</XhFieldLabel>
          <XhFieldControl>
            <XSegmented :value="editor.form.targetKind" :options="targetKindOptions" @update:value="value => editor.form.targetKind = value as DelegationTargetKind" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.form.targetKind === 'role'" :invalid="!!editor.fieldError('roleId')" required>
          <XhFieldLabel>{{ t('access.delegations.fields.role') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="dlg-combobox"
              :options="editor.roleOptions.value"
              :value="editor.form.roleId || null"
              :loading="editor.targetLoading.value"
              :placeholder="t('access.delegations.ph_role')"
              @update:value="value => editor.form.roleId = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('roleId')">
            {{ editor.fieldError('roleId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-else :invalid="!!editor.fieldError('permission')" required>
          <XhFieldLabel>{{ t('access.delegations.fields.permission') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="dlg-combobox"
              :options="editor.permissionOptions.value"
              :value="editor.form.permission || null"
              :loading="editor.targetLoading.value"
              :placeholder="t('access.delegations.ph_permission')"
              @update:value="value => editor.form.permission = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('permission')">
            {{ editor.fieldError('permission') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('effectiveAt')">
          <XhFieldLabel>{{ t('access.delegations.fields.effective_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="editor.form.effectiveAt" clearable show-time :placeholder="t('access.delegations.ph_effective_at')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('effectiveAt')">
            {{ editor.fieldError('effectiveAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('expiresAt')" required>
          <XhFieldLabel>{{ t('access.delegations.fields.expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="editor.form.expiresAt" clearable show-time />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('expiresAt')">
            {{ editor.fieldError('expiresAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('reason')">
          <XhFieldLabel>{{ t('access.delegations.fields.reason') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.reason" name="reason" type="textarea" :rows="2" clearable :placeholder="t('access.delegations.ph_reason')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('reason')">
            {{ editor.fieldError('reason') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
/* 组合框默认按内容定宽，这里与字段一样铺满 */
.dlg-combobox {
  inline-size: 100%;
}
</style>
