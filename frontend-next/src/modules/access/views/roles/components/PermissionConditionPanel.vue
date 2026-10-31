<script setup lang="ts">
import type { ConditionBatchTrial } from '../condition-trial'
import type { ConditionAttribute, ConditionOperator, GrantConditionTarget, PermissionConditionItem } from '../permission-condition'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhButtonIndicator, XhButtonLabel, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XEditModal, XInput, XNumberInput, XSegmented, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { permissionConditions as conditionsApi } from '~/contracts'
import { attributeLabel, operatorLabel, valueHint } from '../condition-labels'
import { verdictLabel, verdictTone } from '../condition-trial'
import { composeAttributeName, CONDITION_OPERATORS, groupConditions, nextGroupNumber, requiresValue, resolveAttribute } from '../permission-condition'

/**
 * 一条授权上的条件：组内且、组间或，可增改、启停、删除。角色授权与用户直授共用；能做什么由调用方按权限码给出，后端照样校验。
 */
defineOptions({ name: 'PermissionConditionPanel' })

const props = defineProps<{
  target: GrantConditionTarget
  conditions: readonly PermissionConditionItem[]
  catalog: readonly ConditionAttribute[]
  can: { create: boolean, update: boolean, delete: boolean, trial: boolean }
}>()

const emit = defineEmits<{
  /** 条件有增删改或启停，调用方据此重新取一次 */
  changed: []
}>()

interface ConditionFormModel {
  id: null | string
  groupNo: number
  attribute: string
  familySuffix: string
  operator: ConditionOperator
  negated: 'negated' | 'plain'
  value: string
  description: string
  status: PermissionConditionItem['status']
}

const { t } = useI18n()
const formId = useId()

const formVisible = ref(false)
const submitting = ref(false)
const formNote = ref<null | string>(null)
const form = ref<ConditionFormModel>(emptyForm(0))
const batchResult = ref<ConditionBatchTrial | null>(null)
const batchLoading = ref(false)
const batchDenied = computed(() => batchResult.value?.items.filter(item => item.verdict === 'Denied').length ?? 0)

const groups = computed(() => groupConditions(props.conditions))
const attributeOptions = computed(() => props.catalog.map(item => ({ value: item.name, label: `${attributeLabel(t, item.name)} · ${item.name}` })))
const selectedAttribute = computed(() => props.catalog.find(item => item.name === form.value.attribute) ?? null)
const formOperatorOptions = computed(() => (selectedAttribute.value?.operators ?? CONDITION_OPERATORS).map(operator => ({ value: operator, label: operatorLabel(t, operator) })))
const formValueHint = computed(() => selectedAttribute.value ? valueHint(t, selectedAttribute.value.kind, form.value.operator) : '')
const negateOptions = computed(() => [
  { value: 'plain', label: t('access.conditions.negate_plain') },
  { value: 'negated', label: t('access.conditions.negate_negated') },
])

function emptyForm(group: number): ConditionFormModel {
  return { id: null, groupNo: group, attribute: '', familySuffix: '', operator: 'Equals', negated: 'plain', value: '', description: '', status: 'Active' }
}

/** 换了一条授权，上一条的影响试算不再适用 */
watch(() => props.target.key, () => {
  batchResult.value = null
})

/** 换属性后，运算符不再适用的改成可用的第一项 */
watch(selectedAttribute, (attribute) => {
  if (attribute && !attribute.operators.includes(form.value.operator))
    form.value.operator = attribute.operators[0] ?? 'Equals'
})

function nameOf(item: PermissionConditionItem): string {
  const { attribute, familySuffix } = resolveAttribute(props.catalog, item.attributeName)
  if (!attribute)
    return item.attributeName
  return attribute.isFamily ? `${attributeLabel(t, attribute.name)} · ${familySuffix}` : attributeLabel(t, attribute.name)
}

function openCreate(group?: number) {
  form.value = emptyForm(group ?? nextGroupNumber(props.conditions))
  formNote.value = null
  formVisible.value = true
}

function openEdit(item: PermissionConditionItem) {
  const resolution = resolveAttribute(props.catalog, item.attributeName)
  form.value = {
    id: item.id,
    groupNo: item.groupNo,
    attribute: resolution.attribute?.name ?? '',
    familySuffix: resolution.familySuffix ?? '',
    operator: item.operator,
    negated: item.isNegated ? 'negated' : 'plain',
    value: item.value ?? '',
    description: item.description ?? '',
    status: item.status,
  }
  formNote.value = !resolution.attribute
    ? t('access.conditions.unregistered_attribute', { name: item.attributeName })
    : resolution.isAlias
      ? t('access.conditions.legacy_alias', { from: item.attributeName, to: resolution.attribute.name })
      : null
  formVisible.value = true
}

/** 修改是整体覆盖：启停也带上其余字段 */
function updateBody(item: PermissionConditionItem, status: PermissionConditionItem['status']) {
  return { id: item.id, attributeName: item.attributeName, operator: item.operator, groupNo: item.groupNo, isNegated: item.isNegated, value: item.value ?? null, description: item.description ?? null, status }
}

async function submit() {
  const attribute = selectedAttribute.value
  if (!attribute) {
    toast.warning(t('access.conditions.msg_pick_attribute'))
    return
  }
  if (attribute.isFamily && !form.value.familySuffix.trim()) {
    toast.warning(t('access.conditions.msg_family_name'))
    return
  }
  if (requiresValue(form.value.operator) && !form.value.value.trim()) {
    toast.warning(t('access.conditions.msg_value_required'))
    return
  }

  const body = {
    attributeName: composeAttributeName(attribute, form.value.familySuffix),
    operator: form.value.operator,
    groupNo: form.value.groupNo,
    isNegated: form.value.negated === 'negated',
    value: requiresValue(form.value.operator) ? form.value.value.trim() : null,
    description: form.value.description.trim() || null,
    status: form.value.status,
  }
  submitting.value = true
  try {
    if (form.value.id)
      await conditionsApi.update({ ...body, id: form.value.id })
    else
      await conditionsApi.create({ ...body, subjectKind: props.target.subjectKind, subjectId: props.target.subjectId, permission: props.target.permission, action: props.target.action })
    toast.success(t('access.conditions.msg_saved'))
    formVisible.value = false
    emit('changed')
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.conditions.msg_save_failed'))
  }
  finally {
    submitting.value = false
  }
}

function toggleStatus(item: PermissionConditionItem) {
  const enable = item.status !== 'Active'
  void dialog.confirm({
    badge: 'warning',
    title: enable ? t('access.conditions.confirm_enable_title') : t('access.conditions.confirm_disable_title'),
    content: enable ? t('access.conditions.confirm_enable_content') : t('access.conditions.confirm_disable_content'),
    okText: enable ? t('access.conditions.action_enable') : t('access.conditions.action_disable'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await conditionsApi.update(updateBody(item, enable ? 'Active' : 'Disabled'))
        toast.success(t('access.conditions.msg_status_updated'))
        emit('changed')
      }
      catch (error) {
        toast.danger((error as Error).message || t('access.conditions.msg_status_failed'))
      }
    },
  })
}

/** 影响试算：这条授权影响到的人此刻各自的结论（不带请求情形，最多 200 人） */
async function runBatchTrial() {
  batchLoading.value = true
  try {
    batchResult.value = await conditionsApi.batchTrial({ subjectKind: props.target.subjectKind, subjectId: props.target.subjectId, permission: props.target.permission })
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.conditions.msg_trial_failed'))
  }
  finally {
    batchLoading.value = false
  }
}

function remove(item: PermissionConditionItem) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('access.conditions.confirm_delete_title'),
    content: t('access.conditions.confirm_delete_content', { name: nameOf(item) }),
    okText: t('access.conditions.action_delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const result = await conditionsApi.delete({ ids: [item.id] })
        if (result.failed.length > 0)
          throw new Error(result.failed.map(f => f.code).join('; '))
        toast.success(t('access.conditions.msg_deleted'))
        emit('changed')
      }
      catch (error) {
        toast.danger((error as Error).message || t('access.conditions.msg_delete_failed'))
      }
    },
  })
}
</script>

<template>
  <div class="condition-panel">
    <p class="condition-panel__tip">
      {{ target.action === 'Deny' ? t('access.conditions.semantics_deny') : t('access.conditions.semantics_grant') }}{{ t('access.conditions.rules_tip') }}
    </p>

    <div v-if="can.create || can.trial" class="condition-panel__toolbar">
      <XhButton v-if="can.create" variant="subtle" tone="brand" size="sm" @click="openCreate()">
        <XhButtonIndicator>
          <Icon icon="lucide:plus" width="14" />
        </XhButtonIndicator>
        <XhButtonLabel>{{ t('access.conditions.action_add_group') }}</XhButtonLabel>
      </XhButton>
      <XhButton v-if="can.trial" variant="subtle" size="sm" :loading="batchLoading" @click="runBatchTrial">
        <XhButtonIndicator>
          <Icon icon="lucide:users-round" width="14" />
        </XhButtonIndicator>
        <XhButtonLabel>{{ t('access.conditions.action_batch_trial') }}</XhButtonLabel>
      </XhButton>
    </div>

    <template v-if="groups.length">
      <template v-for="(group, index) in groups" :key="group.group">
        <p v-if="index > 0" class="condition-panel__or">
          {{ t('access.conditions.or') }}
        </p>
        <section class="condition-group" :aria-label="t('access.conditions.group_title', { group: group.group })">
          <header class="condition-group__header">
            <span class="condition-group__title">{{ t('access.conditions.group_title', { group: group.group }) }}</span>
            <span class="condition-group__hint">{{ t('access.conditions.group_hint') }}</span>
            <XhButton v-if="can.create" variant="ghost" size="sm" class="condition-group__add" @click="openCreate(group.group)">
              <XhButtonLabel>{{ t('access.conditions.action_add_condition') }}</XhButtonLabel>
            </XhButton>
          </header>
          <table class="xh-detail-table">
            <thead>
              <tr>
                <th>{{ t('access.conditions.th_attribute') }}</th>
                <th>{{ t('access.conditions.th_operator') }}</th>
                <th>{{ t('access.conditions.th_value') }}</th>
                <th>{{ t('access.conditions.th_status') }}</th>
                <th>{{ t('access.conditions.th_actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in group.items" :key="item.id" :class="{ 'condition-row--off': item.status !== 'Active' }">
                <td>
                  <span class="condition-row__attribute">{{ nameOf(item) }}</span>
                  <code class="condition-row__name">{{ item.attributeName }}</code>
                </td>
                <td>
                  {{ item.isNegated ? t('access.conditions.negated_prefix') : '' }}{{ operatorLabel(t, item.operator) }}
                </td>
                <td>
                  <code v-if="requiresValue(item.operator)">{{ item.value }}</code>
                </td>
                <td>
                  <XhTagRoot variant="subtle" size="sm" :tone="item.status === 'Active' ? 'success' : 'neutral'">
                    <XhTagLabel>{{ item.status === 'Active' ? t('access.conditions.status_on') : t('access.conditions.status_off') }}</XhTagLabel>
                  </XhTagRoot>
                </td>
                <td class="condition-row__actions">
                  <XhButton v-if="can.update" variant="ghost" size="sm" @click="openEdit(item)">
                    <XhButtonLabel>{{ t('access.conditions.action_edit') }}</XhButtonLabel>
                  </XhButton>
                  <XhButton v-if="can.update" variant="ghost" size="sm" @click="toggleStatus(item)">
                    <XhButtonLabel>{{ item.status === 'Active' ? t('access.conditions.action_disable') : t('access.conditions.action_enable') }}</XhButtonLabel>
                  </XhButton>
                  <XhButton v-if="can.delete" variant="ghost" size="sm" tone="danger" @click="remove(item)">
                    <XhButtonLabel>{{ t('access.conditions.action_delete') }}</XhButtonLabel>
                  </XhButton>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </template>
    </template>

    <XhEmptyStateRoot v-else class="condition-panel__empty">
      <XhEmptyStateIndicator>
        <Icon icon="lucide:filter" width="28" />
      </XhEmptyStateIndicator>
      <XhEmptyStateTitle>{{ t('access.conditions.empty_title') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('access.conditions.empty_description') }}</XhEmptyStateDescription>
    </XhEmptyStateRoot>

    <section v-if="batchResult" class="condition-group" :aria-label="t('access.conditions.batch_title')">
      <header class="condition-group__header">
        <span class="condition-group__title">{{ t('access.conditions.batch_title') }}</span>
        <span class="condition-group__hint">{{ t('access.conditions.batch_summary', { total: batchResult.totalUsers, denied: batchDenied }) }}</span>
      </header>
      <p class="condition-panel__tip">
        {{ t('access.conditions.batch_tip') }}
        <template v-if="batchResult.truncated">
          {{ t('access.conditions.batch_truncated', { count: batchResult.items.length }) }}
        </template>
      </p>
      <table v-if="batchResult.items.length" class="xh-detail-table">
        <thead>
          <tr>
            <th>{{ t('access.conditions.th_user') }}</th>
            <th>{{ t('access.conditions.th_verdict') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in batchResult.items" :key="item.userId">
            <td>{{ item.userName }}</td>
            <td>
              <XhTagRoot variant="subtle" size="sm" :tone="verdictTone(item.verdict)">
                <XhTagLabel>{{ verdictLabel(t, item.verdict) }}</XhTagLabel>
              </XhTagRoot>
            </td>
          </tr>
        </tbody>
      </table>
    </section>

    <XEditModal
      v-model:show="formVisible"
      :title="form.id ? t('access.conditions.form_title_edit') : t('access.conditions.form_title_create')"
      :loading="submitting"
      :form-id="formId"
    >
      <XhAlertRoot v-if="formNote" tone="warning" class="mb-3">
        <XhAlertIndicator>
          <Icon icon="lucide:triangle-alert" width="16" height="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>{{ formNote }}</XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhFormRoot :id="formId" class="xh-edit-form-grid" data-testid="condition-form" @submit="submit">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.conditions.label_attribute') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.attribute" name="attribute" :options="attributeOptions" :placeholder="t('access.conditions.ph_attribute')" />
          </XhFieldControl>
          <XhFieldDescription v-if="selectedAttribute?.isMultiValued">
            {{ t('access.conditions.multi_hint') }}
          </XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot v-if="selectedAttribute?.isFamily" required>
          <XhFieldLabel>{{ t('access.conditions.label_family_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.familySuffix" name="familySuffix" clearable :placeholder="t('access.conditions.ph_family_name')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('access.conditions.label_operator') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.operator" name="operator" :options="formOperatorOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.conditions.label_negate') }}</XhFieldLabel>
          <XhFieldControl>
            <XSegmented v-model:value="form.negated" :options="negateOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="requiresValue(form.operator)" class="xh-span-2" required>
          <XhFieldLabel>{{ t('access.conditions.label_value') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.value" name="value" clearable />
          </XhFieldControl>
          <XhFieldDescription v-if="formValueHint">
            {{ formValueHint }}
          </XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.conditions.label_group') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.groupNo" name="groupNo" :min="0" :max="99" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('access.conditions.group_field_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.conditions.label_description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.description" name="description" clearable />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormRoot>
    </XEditModal>
  </div>
</template>

<style scoped>
.condition-panel {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  min-block-size: 0;
}

.condition-panel__tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.condition-panel__toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
}

.condition-panel__or {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-align: center;
}

.condition-panel__empty {
  padding-block: var(--xh-space-6);
}

.condition-group {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-inset);
}

.condition-group__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.condition-group__title {
  font-weight: var(--xh-font-weight-semibold);
}

.condition-group__hint {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.condition-group__add {
  margin-inline-start: auto;
}

.condition-row__attribute {
  display: block;
}

.condition-row__name {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.condition-row__actions {
  white-space: nowrap;
}

.condition-row--off {
  color: var(--xh-fg-muted);
}
</style>
