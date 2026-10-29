<script lang="ts" setup>
import type { ConditionAttribute } from '../roles/permission-condition'
import type { AccessPolicyAction, AccessPolicyConditionInput, AccessPolicyRow, AccessPolicyTemplate, ConditionGroupModel } from './access-policy'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhButtonLabel, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XDatePicker, XEditModal, XInput, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { policies as policiesApi } from '~/contracts'
import { usePermission } from '~/hooks'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { describeCondition } from '../roles/condition-labels'
import {
  ACCESS_POLICY_ACTIONS,
  ACCESS_POLICY_PHASES,
  ACCESS_POLICY_STATUSES,
  accessPolicyResource,
  actionLabel,
  actionTone,
  codeFromTemplate,
  findIncomplete,
  incompleteMessage,
  phaseLabel,
  statusLabel,
  templateText,
  toConditions,
  toGroups,
  usesPlaceholderNetwork,
} from './access-policy'
import AccessPolicyConditionEditor from './components/AccessPolicyConditionEditor.vue'
import AccessPolicyTrialDrawer from './components/AccessPolicyTrialDrawer.vue'

defineOptions({ name: 'AccessPoliciesView' })

const { t } = useI18n()
const auth = useAuthStore()
const { hasPermission } = usePermission()
const resource = accessPolicyResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)
const isPlatform = computed(() => auth.tenantId === null)
const catalog = ref<ConditionAttribute[]>([])

function reload() {
  void schemaPageRef.value?.reload()
}

async function ensureCatalog(): Promise<boolean> {
  if (catalog.value.length > 0)
    return true
  try {
    catalog.value = [...(await policiesApi.attributes({})).items]
    return true
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.msg_load_attributes_failed'))
    return false
  }
}

const asRow = (row: object) => row as AccessPolicyRow

const actionOptions = computed<SelectOption[]>(() => ACCESS_POLICY_ACTIONS.map(action => ({ label: actionLabel(t, action), value: action })))
const statusOptions = computed<SelectOption[]>(() => ACCESS_POLICY_STATUSES.map(status => ({ label: statusLabel(t, status), value: status })))
const phaseOptions = computed<SelectOption[]>(() => ACCESS_POLICY_PHASES.map(phase => ({ label: phaseLabel(t, phase), value: phase })))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('access.policies.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('access.policies.keyword_placeholder'), width: 240, order: 0 },
  { key: 'code', title: t('access.policies.fields.code'), dataType: 'string', sortable: true, minWidth: 190, order: 1 },
  { key: 'name', title: t('access.policies.fields.name'), dataType: 'string', sortable: true, minWidth: 180, order: 2 },
  { key: 'action', title: t('access.policies.fields.action'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: actionOptions.value, searchPlaceholder: t('access.policies.action_placeholder'), width: 100, order: 3, render: row => renderTag(actionLabel(t, asRow(row).action), actionTone(asRow(row).action)) },
  { key: 'phase', title: t('access.policies.fields.phase'), dataType: 'enum', options: phaseOptions.value, width: 110, order: 4, render: row => phaseLabel(t, asRow(row).phase) },
  { key: 'priority', title: t('access.policies.fields.priority'), dataType: 'number', sortable: true, width: 88, order: 5 },
  { key: 'status', title: t('access.policies.fields.status'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: statusOptions.value, searchPlaceholder: t('access.policies.status_placeholder'), width: 88, order: 6, render: row => renderTag(statusLabel(t, asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'neutral') },
  { key: 'isEffective', title: t('access.policies.fields.is_effective'), dataType: 'boolean', width: 92, order: 7, render: row => renderTag(asRow(row).isEffective ? t('access.policies.effective_yes') : t('access.policies.effective_no'), asRow(row).isEffective ? 'success' : 'neutral') },
  { key: 'conditionCount', title: t('access.policies.fields.condition_count'), dataType: 'number', width: 88, order: 8, render: row => String(asRow(row).conditions.length) },
  { key: 'effectiveAt', title: t('access.policies.fields.effective_at'), dataType: 'datetime', minWidth: 170, order: 9 },
  { key: 'expiresAt', title: t('access.policies.fields.expires_at'), dataType: 'datetime', minWidth: 170, order: 10 },
  { key: 'description', title: t('access.policies.fields.description'), dataType: 'string', minWidth: 220, order: 11 },
  { key: 'modifiedAt', title: t('access.policies.fields.modified_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 12 },
])

function switchAction(key: string, title: string, icon: string, action: AccessPolicyAction, confirmKey: (name: string) => string) {
  return {
    key,
    title,
    scope: 'row' as const,
    icon,
    tone: action === 'Deny' ? 'warning' as const : undefined,
    confirm: true,
    confirmText: (row: object) => confirmKey(asRow(row).name),
    permission: 'access.policies.update',
    visible: (row: object) => asRow(row).action !== action,
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.policies',
  pageName: t('access.policies.title'),
  batchRemovable: true,
  removePermission: 'access.policies.delete',
  statusPermission: 'access.policies.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('access.policies.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'access.policies.create' },
    { key: 'from-template', title: t('access.policies.actions.from_template'), scope: 'page', icon: 'lucide:layout-template', permission: 'access.policies.create' },
    { key: 'view', title: t('access.policies.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'access.policies.update' },
    { key: 'trial', title: t('access.policies.actions.trial'), scope: 'row', icon: 'lucide:flask-conical', permission: 'access.policies.trial' },
    switchAction('enable-blocking', t('access.policies.actions.enable_blocking'), 'lucide:shield-x', 'Deny', name => t('access.policies.confirm_enable_blocking', { name })),
    switchAction('set-allow', t('access.policies.actions.set_allow'), 'lucide:shield-check', 'Allow', name => t('access.policies.confirm_set_allow', { name })),
    switchAction('set-log-only', t('access.policies.actions.set_log_only'), 'lucide:notebook-pen', 'LogOnly', name => t('access.policies.confirm_set_log_only', { name })),
    { key: 'toggle', title: t('access.policies.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), permission: 'access.policies.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'access.policies.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      void openCreate(null)
      break
    case 'from-template':
      void openTemplates()
      break
    case 'view':
      if (row)
        void openDetail(row)
      break
    case 'edit':
      if (row)
        void openEdit(row)
      break
    case 'trial':
      if (row)
        openTrial({ title: row.name, policyId: row.id, conditions: null, action: row.action })
      break
    case 'enable-blocking':
      if (row)
        void changeAction(row, 'Deny')
      break
    case 'set-allow':
      if (row)
        void changeAction(row, 'Allow')
      break
    case 'set-log-only':
      if (row)
        void changeAction(row, 'LogOnly')
      break
    case 'toggle':
      if (row)
        void toggleStatus(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

// ── 详情 ────────────────────────────────────────────────────────
const detailVisible = ref(false)
const detail = ref<AccessPolicyRow | null>(null)

async function openDetail(row: AccessPolicyRow) {
  try {
    detail.value = await policiesApi.get({ id: row.id })
    detailVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.msg_load_detail_failed'))
  }
}

// ── 模板 ────────────────────────────────────────────────────────
const templatesVisible = ref(false)
const templates = ref<AccessPolicyTemplate[]>([])

async function openTemplates() {
  try {
    if (templates.value.length === 0)
      templates.value = [...(await policiesApi.templates({})).items]
    templatesVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.msg_load_templates_failed'))
  }
}

function useTemplate(template: AccessPolicyTemplate) {
  templatesVisible.value = false
  void openCreate(template)
}

// ── 新建 / 编辑 ─────────────────────────────────────────────────
interface PolicyFormModel {
  id: null | string
  code: string
  name: string
  description: string
  priority: null | number
  effectiveAt: null | number
  expiresAt: null | number
  templateCode: null | string
  remark: string
}

function emptyForm(): PolicyFormModel {
  return { id: null, code: '', name: '', description: '', priority: 100, effectiveAt: null, expiresAt: null, templateCode: null, remark: '' }
}

const modalVisible = ref(false)
const submitting = ref(false)
const submitError = ref<null | string>(null)
const form = reactive<PolicyFormModel>(emptyForm())
const groups = ref<ConditionGroupModel[]>([])
const modalTitle = computed(() => (form.id ? t('access.policies.edit_title') : t('access.policies.create_title')))
const draftConditions = computed(() => toConditions(groups.value, catalog.value))
const placeholderNetwork = computed(() => usesPlaceholderNetwork(draftConditions.value))

async function openCreate(template: AccessPolicyTemplate | null) {
  if (!await ensureCatalog())
    return
  const text = template ? templateText(t, template) : null
  Object.assign(form, emptyForm(), template && text
    ? { code: codeFromTemplate(template.code), name: text.name, description: text.description, priority: template.priority, templateCode: template.code }
    : {})
  groups.value = toGroups(template?.conditions ?? [], catalog.value)
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: AccessPolicyRow) {
  if (!await ensureCatalog())
    return
  try {
    const current = await policiesApi.get({ id: row.id })
    Object.assign(form, {
      id: current.id,
      code: current.code,
      name: current.name,
      description: current.description ?? '',
      priority: current.priority,
      effectiveAt: current.effectiveAt ? new Date(current.effectiveAt).getTime() : null,
      expiresAt: current.expiresAt ? new Date(current.expiresAt).getTime() : null,
      templateCode: current.templateCode ?? null,
      remark: current.remark ?? '',
    })
    groups.value = toGroups(current.conditions, catalog.value)
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.msg_load_detail_failed'))
  }
}

/** 4.x 的提交前校验：缺什么先提示，不发请求 */
function validate(): boolean {
  const code = form.code.trim()
  if (!code) {
    toast.warning(t('access.policies.warn_code_required'))
    return false
  }
  if (/\s/.test(code)) {
    toast.warning(t('access.policies.warn_code_whitespace'))
    return false
  }
  if (!form.name.trim()) {
    toast.warning(t('access.policies.warn_name_required'))
    return false
  }
  if (form.effectiveAt != null && form.expiresAt != null && form.effectiveAt >= form.expiresAt) {
    toast.warning(t('access.policies.warn_time_order'))
    return false
  }
  const incomplete = findIncomplete(groups.value, catalog.value)
  if (incomplete) {
    toast.warning(incompleteMessage(t, incomplete))
    return false
  }
  return true
}

async function onSubmit() {
  if (!validate())
    return
  const shared = {
    code: form.code.trim(),
    name: form.name.trim(),
    description: form.description.trim() || null,
    priority: form.priority ?? 100,
    effectiveAt: form.effectiveAt != null ? new Date(form.effectiveAt).toISOString() : null,
    expiresAt: form.expiresAt != null ? new Date(form.expiresAt).toISOString() : null,
    templateCode: form.templateCode,
    remark: form.remark.trim() || null,
    conditions: draftConditions.value,
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await policiesApi.update({ id: form.id, ...shared })
    else
      await policiesApi.create(shared)
    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = error instanceof ApiError ? error.detail : String(error)
  }
  finally {
    submitting.value = false
  }
}

// ── 试运行 ──────────────────────────────────────────────────────
const trialVisible = ref(false)
const trialTarget = ref<{ title: string, policyId: null | string, conditions: AccessPolicyConditionInput[] | null, action: AccessPolicyAction }>({ title: '', policyId: null, conditions: null, action: 'LogOnly' })

function openTrial(target: typeof trialTarget.value) {
  trialTarget.value = target
  trialVisible.value = true
}

/** 草稿按拒绝来试：结果说的是启用拦截后会不会拦下 */
function trialDraft() {
  const incomplete = findIncomplete(groups.value, catalog.value)
  if (incomplete) {
    toast.warning(incompleteMessage(t, incomplete))
    return
  }
  openTrial({ title: form.name.trim() || t('access.policies.draft'), policyId: null, conditions: draftConditions.value, action: 'Deny' })
}

// ── 改动作 / 启停 / 删除 ────────────────────────────────────────
async function changeAction(row: AccessPolicyRow, action: AccessPolicyAction) {
  try {
    await policiesApi.setAction({ id: row.id, action })
    toast.success(t('access.policies.msg_action_changed'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

async function toggleStatus(row: AccessPolicyRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

async function removeRow(row: AccessPolicyRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 详情 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 820px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ detail?.name }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody v-if="detail" class="xh-overlay-body policy-detail">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue><code>{{ detail.code }}</code></XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.action') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ actionLabel(t, detail.action) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.phase') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ phaseLabel(t, detail.phase) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.priority') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.priority }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ statusLabel(t, detail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.is_effective') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.isEffective ? t('access.policies.effective_yes') : t('access.policies.effective_no') }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.effective_range') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.effectiveAt ? formatDate(detail.effectiveAt) : '-' }} ~ {{ detail.expiresAt ? formatDate(detail.expiresAt) : '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.template') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.templateCode || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.description || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.policies.fields.remark') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.remark || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <table class="xh-detail-table">
            <caption class="policy-detail__caption">
              {{ t('access.policies.editor.title') }}
            </caption>
            <thead>
              <tr>
                <th>{{ t('access.policies.fields.group') }}</th>
                <th>{{ t('access.policies.fields.condition') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(condition, index) in detail.conditions" :key="index">
                <td>{{ condition.groupNo + 1 }}</td>
                <td>{{ describeCondition(t, condition) }} <code class="policy-detail__muted">{{ condition.attributeName }}</code></td>
              </tr>
            </tbody>
          </table>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 从模板新建 -->
    <XhDrawerRoot v-model:open="templatesVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('access.policies.template_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body policy-templates">
          <p class="policy-templates__tip">
            {{ t('access.policies.template_tip') }}
          </p>
          <article v-for="template in templates" :key="template.code" class="policy-template">
            <header class="policy-template__head">
              <span class="policy-template__name">{{ templateText(t, template).name }}</span>
              <XhTagRoot variant="subtle" size="sm" :tone="actionTone(template.action)">
                <XhTagLabel>{{ t('access.policies.template_suggested', { action: actionLabel(t, template.action) }) }}</XhTagLabel>
              </XhTagRoot>
              <XhTagRoot variant="outline" size="sm">
                <XhTagLabel>{{ phaseLabel(t, template.phase) }}</XhTagLabel>
              </XhTagRoot>
            </header>
            <p class="policy-template__description">
              {{ templateText(t, template).description }}
            </p>
            <div>
              <XhButton variant="subtle" size="sm" tone="brand" @click="useTemplate(template)">
                <XhButtonLabel>{{ t('access.policies.template_use') }}</XhButtonLabel>
              </XhButton>
            </div>
          </article>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 新建 / 编辑 -->
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId" width="1080px">
      <XhAlertRoot v-if="!form.id" tone="info" variant="subtle" class="policy-form__alert">
        <XhAlertIndicator>
          <Icon icon="lucide:info" width="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>{{ t('access.policies.hint_log_only') }}</XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhAlertRoot v-if="placeholderNetwork" tone="warning" variant="subtle" class="policy-form__alert">
        <XhAlertIndicator>
          <Icon icon="lucide:triangle-alert" width="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>{{ t('access.policies.hint_placeholder_network') }}</XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="access-policy-form" @submit="onSubmit">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.policies.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :disabled="Boolean(form.id)" :max-length="100" :placeholder="t('access.policies.ph_code')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.policies.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="200" :placeholder="t('access.policies.ph_name')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.policies.fields.priority') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.priority" name="priority" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.policies.fields.template') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput :value="form.templateCode ?? '-'" name="templateCode" disabled />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.policies.fields.effective_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="form.effectiveAt" clearable show-time />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.policies.fields.expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="form.expiresAt" clearable show-time />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('access.policies.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.description" name="description" clearable type="textarea" :rows="2" :max-length="1000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('access.policies.fields.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('access.policies.ph_remark')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
      <AccessPolicyConditionEditor v-model:groups="groups" :catalog="catalog" :platform="isPlatform" />
      <div v-if="hasPermission('access.policies.trial')" class="policy-form__trial">
        <XhButton variant="outline" size="sm" @click="trialDraft">
          <XhButtonLabel>{{ t('access.policies.trial_draft') }}</XhButtonLabel>
        </XhButton>
      </div>
    </XEditModal>

    <AccessPolicyTrialDrawer
      v-model:open="trialVisible"
      :title="trialTarget.title"
      :policy-id="trialTarget.policyId"
      :conditions="trialTarget.conditions"
      :action="trialTarget.action"
    />
  </SchemaPage>
</template>

<style scoped>
.policy-detail,
.policy-templates {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.policy-detail__caption {
  padding-block-end: var(--xh-space-1);
  font-weight: var(--xh-font-weight-medium);
  text-align: start;
}

.policy-detail__muted,
.policy-templates__tip,
.policy-template__description {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.policy-template {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.policy-template__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.policy-template__name {
  font-weight: var(--xh-font-weight-medium);
}

.policy-form__alert {
  margin-block-end: var(--xh-space-2);
}

.policy-form__trial {
  display: flex;
  justify-content: flex-end;
  margin-block-start: var(--xh-space-2);
}
</style>
