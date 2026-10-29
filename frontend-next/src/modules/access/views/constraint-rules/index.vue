<script lang="ts" setup>
import type { ConditionGroupModel } from '../policies/access-policy'
import type { ConditionAttribute } from '../roles/permission-condition'
import type { ConstraintItemModel, ConstraintRuleRow, ConstraintTargetType, ConstraintType, ConstraintViolationAction } from './constraint-rule'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XCombobox, XDatePicker, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { constraintRules as constraintRulesApi, permissions as permissionsApi, policies as policiesApi, roles as rolesApi, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'
import { formatDate } from '~/utils'
import { findIncomplete, incompleteMessage, toConditions, toGroups } from '../policies/access-policy'
import AccessPolicyConditionEditor from '../policies/components/AccessPolicyConditionEditor.vue'
import { describeCondition } from '../roles/condition-labels'
import {
  actionLabel,
  actionTone,
  CONSTRAINT_ACTIONS,
  CONSTRAINT_STATUSES,
  CONSTRAINT_TARGET_TYPES,
  CONSTRAINT_TYPES,
  constraintRuleResource,
  emptyItem,
  findItemProblem,
  itemProblemMessage,
  statusLabel,
  targetTypeLabel,
  targetTypesOf,
  toItemInputs,
  toItemModels,
  typeHint,
  typeLabel,
  usesMaxAllowed,
} from './constraint-rule'

defineOptions({ name: 'AccessConstraintRulesView' })

const { t } = useI18n()
const resource = constraintRuleResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)
const catalog = ref<ConditionAttribute[]>([])

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as ConstraintRuleRow
const typeOptions = computed<SelectOption[]>(() => CONSTRAINT_TYPES.map(type => ({ label: typeLabel(t, type), value: type })))
const targetTypeOptions = computed<SelectOption[]>(() => CONSTRAINT_TARGET_TYPES.map(type => ({ label: targetTypeLabel(t, type), value: type })))
const actionOptions = computed<SelectOption[]>(() => CONSTRAINT_ACTIONS.map(action => ({ label: actionLabel(t, action), value: action })))
const statusOptions = computed<SelectOption[]>(() => CONSTRAINT_STATUSES.map(status => ({ label: statusLabel(t, status), value: status })))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('access.constraint_rules.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('access.constraint_rules.keyword_placeholder'), width: 240, order: 0 },
  { key: 'code', title: t('access.constraint_rules.fields.code'), dataType: 'string', sortable: true, minWidth: 170, order: 1 },
  { key: 'name', title: t('access.constraint_rules.fields.name'), dataType: 'string', sortable: true, minWidth: 170, order: 2 },
  { key: 'type', title: t('access.constraint_rules.fields.type'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: typeOptions.value, searchPlaceholder: t('access.constraint_rules.type_placeholder'), width: 130, order: 3, render: row => typeLabel(t, asRow(row).type) },
  { key: 'targetType', title: t('access.constraint_rules.fields.target_type'), dataType: 'enum', searchable: true, searchMultiple: true, options: targetTypeOptions.value, searchPlaceholder: t('access.constraint_rules.target_type_placeholder'), width: 100, order: 4, render: row => targetTypeLabel(t, asRow(row).targetType) },
  { key: 'violationAction', title: t('access.constraint_rules.fields.violation_action'), dataType: 'enum', searchable: true, searchMultiple: true, options: actionOptions.value, searchPlaceholder: t('access.constraint_rules.action_placeholder'), width: 100, order: 5, render: row => renderTag(actionLabel(t, asRow(row).violationAction), actionTone(asRow(row).violationAction)) },
  { key: 'itemCount', title: t('access.constraint_rules.fields.item_count'), dataType: 'number', width: 92, order: 6, render: row => String(asRow(row).items.length) },
  { key: 'priority', title: t('access.constraint_rules.fields.priority'), dataType: 'number', sortable: true, width: 88, order: 7 },
  { key: 'status', title: t('access.constraint_rules.fields.status'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: statusOptions.value, searchPlaceholder: t('access.constraint_rules.status_placeholder'), width: 88, order: 8, render: row => renderTag(statusLabel(t, asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'neutral') },
  { key: 'isEffective', title: t('access.constraint_rules.fields.is_effective'), dataType: 'boolean', width: 92, order: 9, render: row => renderTag(asRow(row).isEffective ? t('access.constraint_rules.effective_yes') : t('access.constraint_rules.effective_no'), asRow(row).isEffective ? 'success' : 'neutral') },
  { key: 'description', title: t('access.constraint_rules.fields.description'), dataType: 'string', minWidth: 200, order: 10 },
  { key: 'createdAt', title: t('access.constraint_rules.fields.created_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.constraint-rules',
  pageName: t('access.constraint_rules.title'),
  batchRemovable: true,
  removePermission: 'access.constraint-rules.delete',
  statusPermission: 'access.constraint-rules.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('access.constraint_rules.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'access.constraint-rules.create' },
    { key: 'view', title: t('access.constraint_rules.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'access.constraint-rules.update' },
    { key: 'toggle', title: t('access.constraint_rules.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), permission: 'access.constraint-rules.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'access.constraint-rules.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      void openCreate()
      break
    case 'view':
      if (row)
        void openDetail(row)
      break
    case 'edit':
      if (row)
        void openEdit(row)
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
const detail = ref<ConstraintRuleRow | null>(null)

async function openDetail(row: ConstraintRuleRow) {
  try {
    detail.value = await constraintRulesApi.get({ id: row.id })
    detailVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.constraint_rules.msg_load_detail_failed'))
  }
}

// ── 目标候选（按目标类型远程检索；角色只列启用的） ────────────────
interface TargetOption { value: string, label: string, description?: string }
const targetOptions = ref<TargetOption[]>([])
const targetLoading = ref(false)
let targetSeq = 0

/** 编辑回填的目标先放进候选：远程候选还没回来时也念得出名字 */
const seededOptions = ref<TargetOption[]>([])
function mergeOptions(seed: TargetOption[], next: TargetOption[]) {
  const seen = new Set(next.map(option => option.value))
  return [...next, ...seed.filter(option => !seen.has(option.value))]
}

// ── 新建 / 编辑 ─────────────────────────────────────────────────
interface RuleFormModel {
  id: null | string
  code: string
  name: string
  type: ConstraintType
  targetType: ConstraintTargetType
  maxAllowed: null | number
  violationAction: ConstraintViolationAction
  priority: null | number
  status: 'Active' | 'Disabled'
  effectiveAt: null | number
  expiresAt: null | number
  description: string
  remark: string
}

function emptyForm(): RuleFormModel {
  return { id: null, code: '', name: '', type: 'Ssd', targetType: 'Role', maxAllowed: 1, violationAction: 'Deny', priority: 0, status: 'Active', effectiveAt: null, expiresAt: null, description: '', remark: '' }
}

const modalVisible = ref(false)
const submitting = ref(false)
const submitError = ref<null | string>(null)
const form = reactive<RuleFormModel>(emptyForm())
const items = ref<ConstraintItemModel[]>([])
const groups = ref<ConditionGroupModel[]>([])
const modalTitle = computed(() => (form.id ? t('access.constraint_rules.edit_title') : t('access.constraint_rules.create_title')))
const formTargetTypeOptions = computed(() => targetTypeOptions.value.filter(option => targetTypesOf(form.type).includes(option.value as ConstraintTargetType)))

async function ensureCatalog(): Promise<boolean> {
  if (catalog.value.length > 0)
    return true
  try {
    catalog.value = [...(await policiesApi.attributes({})).items]
    return true
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.constraint_rules.msg_load_attributes_failed'))
    return false
  }
}

function openCreate() {
  Object.assign(form, emptyForm())
  items.value = [emptyItem(), emptyItem()]
  groups.value = []
  seededOptions.value = []
  submitError.value = null
  modalVisible.value = true
  void searchTargets()
}

async function openEdit(row: ConstraintRuleRow) {
  try {
    const current = await constraintRulesApi.get({ id: row.id })
    if (current.type === 'Condition' && !await ensureCatalog())
      return
    Object.assign(form, {
      id: current.id,
      code: current.code,
      name: current.name,
      type: current.type,
      targetType: current.targetType,
      maxAllowed: current.maxAllowed,
      violationAction: current.violationAction,
      priority: current.priority,
      status: current.status,
      effectiveAt: current.effectiveAt ? new Date(current.effectiveAt).getTime() : null,
      expiresAt: current.expiresAt ? new Date(current.expiresAt).getTime() : null,
      description: current.description ?? '',
      remark: current.remark ?? '',
    })
    items.value = toItemModels(current.items)
    groups.value = toGroups(current.conditions, catalog.value)
    seededOptions.value = current.items.map(item => ({ value: item.targetId, label: item.targetName ?? item.targetId, description: item.targetCode ?? undefined }))
    targetOptions.value = [...seededOptions.value]
    submitError.value = null
    modalVisible.value = true
    void searchTargets()
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.constraint_rules.msg_load_detail_failed'))
  }
}

/** 换约束类型：目标类型不再适用即换成第一个可用的；换成条件类先取属性目录 */
async function changeType(value: unknown) {
  form.type = value as ConstraintType
  if (!targetTypesOf(form.type).includes(form.targetType))
    changeTargetType(targetTypesOf(form.type)[0]!)
  if (form.type === 'Condition')
    await ensureCatalog()
}

/** 换目标类型：已选的目标不再适用，清掉重选 */
function changeTargetType(value: unknown) {
  const next = value as ConstraintTargetType
  if (next === form.targetType)
    return
  form.targetType = next
  items.value = items.value.map(item => ({ ...item, targetId: '' }))
  seededOptions.value = []
  targetOptions.value = []
  void searchTargets()
}

function addItem() {
  items.value = [...items.value, emptyItem(items.value.at(-1)?.groupNo ?? 0)]
}

function removeItem(index: number) {
  items.value = items.value.filter((_, position) => position !== index)
}

/** 4.x 的提交前校验：缺什么先提示，不发请求 */
function validate(): boolean {
  const code = form.code.trim()
  if (!code || /\s/.test(code)) {
    toast.warning(t('access.constraint_rules.warn_code'))
    return false
  }
  if (!form.name.trim()) {
    toast.warning(t('access.constraint_rules.warn_name'))
    return false
  }
  if (form.effectiveAt != null && form.expiresAt != null && form.effectiveAt >= form.expiresAt) {
    toast.warning(t('access.constraint_rules.warn_time_order'))
    return false
  }
  const problem = findItemProblem(form.type, items.value)
  if (problem) {
    toast.warning(itemProblemMessage(t, problem))
    return false
  }
  if (form.type === 'Condition') {
    const incomplete = findIncomplete(groups.value, catalog.value)
    if (incomplete) {
      toast.warning(incompleteMessage(t, incomplete))
      return false
    }
  }
  return true
}

async function onSubmit() {
  if (!validate())
    return
  const body = {
    code: form.code.trim(),
    name: form.name.trim(),
    type: form.type,
    targetType: form.targetType,
    items: toItemInputs(items.value),
    maxAllowed: usesMaxAllowed(form.type) ? (form.maxAllowed ?? 1) : 1,
    conditions: form.type === 'Condition' ? toConditions(groups.value, catalog.value) : null,
    violationAction: form.violationAction,
    priority: form.priority ?? 0,
    status: form.status,
    effectiveAt: form.effectiveAt != null ? new Date(form.effectiveAt).toISOString() : null,
    expiresAt: form.expiresAt != null ? new Date(form.expiresAt).toISOString() : null,
    description: form.description.trim() || null,
    remark: form.remark.trim() || null,
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await constraintRulesApi.update({ id: form.id, ...body })
    else
      await constraintRulesApi.create(body)
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

/** 按表单当前的目标类型检索候选 */
async function searchTargets(keyword = '') {
  const seq = ++targetSeq
  targetLoading.value = true
  try {
    const kw = keyword.trim() || null
    let next: TargetOption[]
    switch (form.targetType) {
      case 'Permission': {
        const all = (await permissionsApi.list({})).items.map(item => ({ value: item.code, label: item.displayName, description: item.code }))
        next = kw ? all.filter(option => option.label.toLowerCase().includes(kw.toLowerCase()) || option.value.includes(kw)) : all
        break
      }
      case 'User': {
        const page = await usersApi.list({ page: { index: 1, size: 50 }, filters: [], sorts: [], keyword: kw })
        next = page.items.map(user => ({ value: user.id, label: user.displayName, description: `@${user.userName}` }))
        break
      }
      default: {
        const page = await rolesApi.list({ page: { index: 1, size: 50 }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [], keyword: kw })
        next = page.items.map(role => ({ value: role.id, label: role.name, description: role.code }))
        break
      }
    }
    if (seq === targetSeq)
      targetOptions.value = mergeOptions(seededOptions.value, next)
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.constraint_rules.msg_load_targets_failed'))
  }
  finally {
    if (seq === targetSeq)
      targetLoading.value = false
  }
}

// ── 启停 / 删除 ─────────────────────────────────────────────────
async function toggleStatus(row: ConstraintRuleRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

async function removeRow(row: ConstraintRuleRow) {
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
        <XhDrawerBody v-if="detail" class="xh-overlay-body rule-detail">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue><code>{{ detail.code }}</code></XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ typeLabel(t, detail.type) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.target_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ targetTypeLabel(t, detail.targetType) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.violation_action') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ actionLabel(t, detail.violationAction) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="usesMaxAllowed(detail.type)">
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.max_allowed') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.maxAllowed }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.priority') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.priority }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ statusLabel(t, detail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.effective_range') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.effectiveAt ? formatDate(detail.effectiveAt) : '-' }} ~ {{ detail.expiresAt ? formatDate(detail.expiresAt) : '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.description || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('access.constraint_rules.fields.remark') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.remark || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <table class="xh-detail-table">
            <caption class="rule-detail__caption">
              {{ t('access.constraint_rules.items_title', { count: detail.items.length }) }}
            </caption>
            <thead>
              <tr>
                <th>{{ t('access.constraint_rules.fields.group') }}</th>
                <th>{{ t('access.constraint_rules.fields.target') }}</th>
                <th>{{ t('access.constraint_rules.fields.remark') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in detail.items" :key="item.targetId">
                <td>{{ item.groupNo }}</td>
                <td>{{ item.targetName ?? t('access.constraint_rules.target_missing') }} <code class="rule-detail__muted">{{ item.targetCode ?? item.targetId }}</code></td>
                <td>{{ item.remark || '-' }}</td>
              </tr>
            </tbody>
          </table>
          <table v-if="detail.conditions.length > 0" class="xh-detail-table">
            <caption class="rule-detail__caption">
              {{ t('access.policies.editor.title') }}
            </caption>
            <tbody>
              <tr v-for="(condition, index) in detail.conditions" :key="index">
                <td>{{ condition.groupNo + 1 }}</td>
                <td>{{ describeCondition(t, condition) }}</td>
              </tr>
            </tbody>
          </table>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 新建 / 编辑 -->
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId" width="1080px">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="constraint-rule-form" @submit="onSubmit">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :max-length="100" :placeholder="t('access.constraint_rules.ph_code')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="200" :placeholder="t('access.constraint_rules.ph_name')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.type" name="type" :options="typeOptions" @update:value="changeType" />
          </XhFieldControl>
          <XhFieldDescription>{{ typeHint(t, form.type) }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.target_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.targetType" name="targetType" :options="formTargetTypeOptions" @update:value="changeTargetType" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.violation_action') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.violationAction" name="violationAction" :options="actionOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="usesMaxAllowed(form.type)">
          <XhFieldLabel>{{ t('access.constraint_rules.fields.max_allowed') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.maxAllowed" name="maxAllowed" :min="1" :max="10000" />
          </XhFieldControl>
          <XhFieldDescription>{{ form.type === 'Cardinality' ? t('access.constraint_rules.hint_max_cardinality') : t('access.constraint_rules.hint_max_group') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.priority') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.priority" name="priority" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.effective_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="form.effectiveAt" clearable show-time />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.constraint_rules.fields.expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="form.expiresAt" clearable show-time />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('access.constraint_rules.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.description" name="description" clearable type="textarea" :rows="2" :max-length="1000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('access.constraint_rules.fields.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>

      <section class="rule-items" data-testid="constraint-rule-items" :aria-label="t('access.constraint_rules.items_title', { count: items.length })">
        <div class="rule-items__head">
          <span class="rule-items__title">{{ t('access.constraint_rules.items_title', { count: items.length }) }}</span>
          <span class="rule-items__hint">{{ typeHint(t, form.type) }}</span>
        </div>
        <div class="rule-items__scroll">
          <div class="rule-items__grid">
            <span class="rule-items__col">{{ t('access.constraint_rules.fields.group') }}</span>
            <span class="rule-items__col">{{ t('access.constraint_rules.fields.target') }}</span>
            <span class="rule-items__col">{{ t('access.constraint_rules.fields.remark') }}</span>
            <span aria-hidden="true" />
            <div v-for="(item, index) in items" :key="item.key" class="rule-item">
              <XNumberInput v-model:value="item.groupNo" :min="0" :max="99" :aria-label="t('access.constraint_rules.fields.group')" />
              <XCombobox
                class="rule-items__combobox"
                remote
                :options="targetOptions"
                :value="item.targetId || null"
                :loading="targetLoading"
                :placeholder="t('access.constraint_rules.ph_target')"
                @search="searchTargets"
                @update:value="value => (item.targetId = typeof value === 'string' ? value : '')"
              />
              <XInput v-model:value="item.remark" clearable :max-length="500" :aria-label="t('access.constraint_rules.fields.remark')" />
              <XhButton variant="ghost" size="sm" tone="danger" :aria-label="t('access.constraint_rules.remove_item')" @click="removeItem(index)">
                <XhButtonIndicator>
                  <Icon icon="lucide:x" width="16" />
                </XhButtonIndicator>
              </XhButton>
            </div>
          </div>
        </div>
        <XhButton variant="subtle" size="sm" @click="addItem">
          <XhButtonIndicator>
            <Icon icon="lucide:plus" width="16" />
          </XhButtonIndicator>
          <XhButtonLabel>{{ t('access.constraint_rules.add_item') }}</XhButtonLabel>
        </XhButton>
      </section>

      <AccessPolicyConditionEditor v-if="form.type === 'Condition'" v-model:groups="groups" :catalog="catalog" :platform="false" :show-phase="false" />
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.rule-detail {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.rule-detail__caption {
  padding-block-end: var(--xh-space-1);
  font-weight: var(--xh-font-weight-medium);
  text-align: start;
}

.rule-detail__muted,
.rule-items__hint,
.rule-items__col {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.rule-items {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--xh-space-2);
  margin-block: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.rule-items__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
  inline-size: 100%;
}

.rule-items__title {
  font-weight: var(--xh-font-weight-medium);
}

/* 窄屏放不下各列的下限时，表格在自己的框里横向滚 */
.rule-items__scroll {
  inline-size: 100%;
  overflow-x: auto;
}

.rule-items__grid {
  display: grid;
  grid-template-columns: minmax(6em, 1fr) minmax(16em, 3fr) minmax(10em, 2fr) auto;
  gap: var(--xh-space-2);
  align-items: center;
}

.rule-item {
  display: contents;
}

.rule-items__combobox {
  inline-size: 100%;
}
</style>
