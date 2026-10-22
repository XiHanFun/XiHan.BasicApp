<script setup lang="ts">
import type { GrantConditionTarget } from './permission-condition'
import type {
  ApiId,
  PermissionConditionAttributeDto,
  PermissionConditionBatchTrialItemDto,
  PermissionConditionBatchTrialResultDto,
  PermissionConditionListItemDto,
} from '@/api'
import type { XDataTableColumn } from '~/components'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhEmptyStateDescription,
  XhEmptyStateIndicator,
  XhEmptyStateRoot,
  XhEmptyStateTitle,
  XhFieldControl,
  XhFieldDescription,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhSpinner,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, h, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  ConditionOperator,
  ConfigDataType,
  permissionConditionApi,
  permissionConditionInsightApi,
  PermissionConditionTrialVerdict,
  ValidityStatus,
} from '@/api'
import { CONDITION_OPERATOR_OPTIONS, CONFIG_DATA_TYPE_OPTIONS } from '@/constants'
import { Icon, XDataTable, XEditModal, XInput, XNumberInput, XSegmented, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import {
  composeAttributeName,
  defaultValueType,
  groupConditions,
  nextGroupNumber,
  requiresValue,
  resolveAttribute,
  semanticsKey,
  valueHintKey,
  valueTypeOptionsFor,
  verdictTone,
} from './permission-condition'

/**
 * 一条授权上的条件：组内且、组间或，可增改、启停、删除，并按这条授权试算影响到的人。
 * 角色授权与用户直授共用；能做什么由调用方按按钮码给出，后端照样校验
 */
defineOptions({ name: 'PermissionConditionPanel' })

const props = defineProps<{
  target: GrantConditionTarget
  can: { create: boolean, update: boolean, status: boolean, delete: boolean, trial: boolean }
}>()

const emit = defineEmits<{
  /** 条件有增删改或启停，调用方据此刷新「有条件」的计数 */
  changed: []
}>()

interface ConditionFormModel {
  basicId: ApiId | null
  conditionGroup: number
  attribute: string
  familySuffix: string
  operator: ConditionOperator
  negated: 'plain' | 'negated'
  valueType: ConfigDataType
  conditionValue: string
  description: string
}

const { t } = useI18n()
const operatorOptions = useEnumOptions('ConditionOperator', CONDITION_OPERATOR_OPTIONS)
const valueTypeOptions = useEnumOptions('ConfigDataType', CONFIG_DATA_TYPE_OPTIONS)
const formId = useId()

const conditions = ref<PermissionConditionListItemDto[]>([])
const catalog = ref<PermissionConditionAttributeDto[]>([])
const loading = ref(false)
const formVisible = ref(false)
const submitting = ref(false)
const formNote = ref<string | null>(null)
const form = ref<ConditionFormModel>(emptyForm(0))
const batchResult = ref<PermissionConditionBatchTrialResultDto | null>(null)
const batchLoading = ref(false)

const groups = computed(() => groupConditions(conditions.value))
const attributeOptions = computed(() => catalog.value.map(item => ({
  value: item.name,
  label: `${item.isRowAttribute ? t('identity.condition.row_prefix') : ''}${item.displayName || item.name} · ${item.name}`,
})))
const selectedAttribute = computed(() => catalog.value.find(item => item.name === form.value.attribute) ?? null)
const formOperatorOptions = computed(() => (selectedAttribute.value?.operators ?? Object.values(ConditionOperator))
  .map(operator => ({ value: operator, label: getOptionLabel(operatorOptions.value, operator) })))
const formValueTypeOptions = computed(() => (selectedAttribute.value ? valueTypeOptionsFor(selectedAttribute.value.kind) : Object.values(ConfigDataType))
  .map(type => ({ value: type, label: getOptionLabel(valueTypeOptions.value, type) })))
const valueHint = computed(() => selectedAttribute.value
  ? t(`identity.condition.${valueHintKey(selectedAttribute.value.kind, form.value.operator)}`)
  : '')
const negateOptions = computed(() => [
  { value: 'plain', label: t('identity.condition.negate_plain') },
  { value: 'negated', label: t('identity.condition.negate_negated') },
])
const batchDenied = computed(() => batchResult.value?.items.filter(item => item.verdict === PermissionConditionTrialVerdict.Denied).length ?? 0)
const batchColumns = computed<XDataTableColumn<PermissionConditionBatchTrialItemDto>[]>(() => [
  { key: 'userName', title: t('identity.condition.th_user'), render: row => row.userName || String(row.userId) },
  {
    key: 'verdict',
    title: t('identity.condition.th_verdict'),
    render: row => h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: verdictTone(row.verdict) }, () => h(XhTagLabel, () => t(`identity.condition.verdict.${row.verdict}`))),
  },
])

function emptyForm(group: number): ConditionFormModel {
  return {
    basicId: null,
    conditionGroup: group,
    attribute: '',
    familySuffix: '',
    operator: ConditionOperator.Equals,
    negated: 'plain',
    valueType: ConfigDataType.String,
    conditionValue: '',
    description: '',
  }
}

async function load() {
  loading.value = true
  try {
    const [list, attributes] = await Promise.all([
      props.target.kind === 'role'
        ? permissionConditionApi.rolePermissionConditions(props.target.bindingId)
        : permissionConditionApi.userPermissionConditions(props.target.bindingId),
      permissionConditionInsightApi.attributes(props.target.permissionCode),
    ])
    conditions.value = list
    catalog.value = attributes
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_load_failed'))
  }
  finally {
    loading.value = false
  }
}

watch(() => props.target.key, () => {
  batchResult.value = null
  void load()
}, { immediate: true })

/** 换属性后，运算符与值类型不再适用的改成可用的第一项 */
watch(selectedAttribute, (attribute) => {
  if (!attribute) {
    return
  }
  if (!attribute.operators.includes(form.value.operator)) {
    form.value.operator = attribute.operators[0] ?? ConditionOperator.Equals
  }
  if (!valueTypeOptionsFor(attribute.kind).includes(form.value.valueType)) {
    form.value.valueType = defaultValueType(attribute.kind)
  }
})

function attributeLabel(name: string) {
  const { attribute, familySuffix } = resolveAttribute(catalog.value, name)
  if (!attribute) {
    return name
  }
  const display = attribute.displayName || attribute.name
  return attribute.isFamily ? `${display} · ${familySuffix}` : display
}

function openCreate(group?: number) {
  form.value = emptyForm(group ?? nextGroupNumber(conditions.value))
  formNote.value = null
  formVisible.value = true
}

function openEdit(item: PermissionConditionListItemDto) {
  const resolution = resolveAttribute(catalog.value, item.attributeName)
  form.value = {
    basicId: item.basicId,
    conditionGroup: item.conditionGroup,
    attribute: resolution.attribute?.name ?? '',
    familySuffix: resolution.familySuffix ?? '',
    operator: item.operator,
    negated: item.isNegated ? 'negated' : 'plain',
    valueType: item.valueType,
    conditionValue: item.conditionValue,
    description: item.description ?? '',
  }
  formNote.value = !resolution.attribute
    ? t('identity.condition.unregistered_attribute', { name: item.attributeName })
    : resolution.isAlias
      ? t('identity.condition.legacy_alias', { from: item.attributeName, to: resolution.attribute.name })
      : resolution.attribute.isLegacy
        ? t('identity.condition.legacy_attribute', { name: item.attributeName })
        : null
  formVisible.value = true
}

async function submit() {
  const attribute = selectedAttribute.value
  if (!attribute) {
    toast.warning(t('identity.condition.msg_pick_attribute'))
    return
  }
  if (attribute.isFamily && !form.value.familySuffix.trim()) {
    toast.warning(t('identity.condition.msg_family_name'))
    return
  }
  if (requiresValue(form.value.operator) && !form.value.conditionValue.trim()) {
    toast.warning(t('identity.condition.msg_value_required'))
    return
  }

  const payload = {
    attributeName: composeAttributeName(attribute, form.value.familySuffix),
    conditionGroup: form.value.conditionGroup,
    conditionValue: requiresValue(form.value.operator) ? form.value.conditionValue.trim() : '',
    description: form.value.description.trim() || null,
    isNegated: form.value.negated === 'negated',
    operator: form.value.operator,
    rolePermissionId: props.target.kind === 'role' ? props.target.bindingId : null,
    userPermissionId: props.target.kind === 'user' ? props.target.bindingId : null,
    valueType: form.value.valueType,
  }
  submitting.value = true
  try {
    if (form.value.basicId) {
      await permissionConditionApi.update({ ...payload, basicId: form.value.basicId })
    }
    else {
      await permissionConditionApi.create({ ...payload, status: ValidityStatus.Valid })
    }
    toast.success(t('identity.condition.msg_saved'))
    formVisible.value = false
    await load()
    emit('changed')
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_save_failed'))
  }
  finally {
    submitting.value = false
  }
}

function toggleStatus(item: PermissionConditionListItemDto) {
  const enable = item.status !== ValidityStatus.Valid
  void dialog.confirm({
    badge: 'warning',
    title: enable ? t('identity.condition.confirm_enable_title') : t('identity.condition.confirm_disable_title'),
    content: enable ? t('identity.condition.confirm_enable_content') : t('identity.condition.confirm_disable_content'),
    okText: enable ? t('identity.condition.action_enable') : t('identity.condition.action_disable'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await permissionConditionApi.updateStatus({ basicId: item.basicId, status: enable ? ValidityStatus.Valid : ValidityStatus.Invalid })
        toast.success(t('identity.condition.msg_status_updated'))
        await load()
        emit('changed')
      }
      catch (error) {
        toast.danger((error as Error).message || t('identity.condition.msg_status_failed'))
      }
    },
  })
}

function remove(item: PermissionConditionListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('identity.condition.confirm_delete_title'),
    content: t('identity.condition.confirm_delete_content', { name: attributeLabel(item.attributeName) }),
    okText: t('identity.condition.action_delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await permissionConditionApi.delete(item.basicId)
        toast.success(t('identity.condition.msg_deleted'))
        await load()
        emit('changed')
      }
      catch (error) {
        toast.danger((error as Error).message || t('identity.condition.msg_delete_failed'))
      }
    },
  })
}

async function runBatchTrial() {
  batchLoading.value = true
  try {
    batchResult.value = await permissionConditionInsightApi.batchTrial(props.target.kind === 'role'
      ? { rolePermissionId: props.target.bindingId }
      : { userPermissionId: props.target.bindingId })
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_trial_failed'))
  }
  finally {
    batchLoading.value = false
  }
}
</script>

<template>
  <div class="condition-panel">
    <p class="condition-panel__tip">
      {{ t(`identity.condition.${semanticsKey(target.action)}`) }}{{ t('identity.condition.rules_tip') }}
    </p>

    <div class="condition-panel__toolbar">
      <XhButton v-if="can.create" variant="subtle" tone="brand" size="sm" :disabled="loading" @click="openCreate()">
        <XhButtonIndicator>
          <Icon icon="lucide:plus" width="14" />
        </XhButtonIndicator>
        <XhButtonLabel>{{ t('identity.condition.action_add_group') }}</XhButtonLabel>
      </XhButton>
      <XhButton v-if="can.trial" variant="subtle" size="sm" :loading="batchLoading" :disabled="loading" @click="runBatchTrial">
        <XhButtonIndicator>
          <Icon icon="lucide:users-round" width="14" />
        </XhButtonIndicator>
        <XhButtonLabel>{{ t('identity.condition.action_batch_trial') }}</XhButtonLabel>
      </XhButton>
    </div>

    <div v-if="loading" class="condition-panel__loading">
      <XhSpinner />
    </div>

    <template v-else-if="groups.length">
      <template v-for="(group, index) in groups" :key="group.group">
        <p v-if="index > 0" class="condition-panel__or">
          {{ t('identity.condition.or') }}
        </p>
        <section class="condition-group" :aria-label="t('identity.condition.group_title', { group: group.group })">
          <header class="condition-group__header">
            <span class="condition-group__title">{{ t('identity.condition.group_title', { group: group.group }) }}</span>
            <span class="condition-group__hint">{{ t('identity.condition.group_hint') }}</span>
            <XhButton v-if="can.create" variant="ghost" size="sm" class="condition-group__add" @click="openCreate(group.group)">
              <XhButtonLabel>{{ t('identity.condition.action_add_condition') }}</XhButtonLabel>
            </XhButton>
          </header>
          <table class="xh-detail-table">
            <thead>
              <tr>
                <th>{{ t('identity.condition.th_attribute') }}</th>
                <th>{{ t('identity.condition.th_operator') }}</th>
                <th>{{ t('identity.condition.th_value') }}</th>
                <th>{{ t('identity.condition.th_status') }}</th>
                <th>{{ t('identity.condition.th_actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in group.items" :key="item.basicId" :class="{ 'condition-row--off': item.status !== ValidityStatus.Valid }">
                <td>
                  <span class="condition-row__attribute">{{ attributeLabel(item.attributeName) }}</span>
                  <code class="condition-row__name">{{ item.attributeName }}</code>
                </td>
                <td>
                  {{ item.isNegated ? t('identity.condition.negated_prefix') : '' }}{{ getOptionLabel(operatorOptions, item.operator) }}
                </td>
                <td>
                  <code v-if="requiresValue(item.operator)">{{ item.conditionValue }}</code>
                </td>
                <td>
                  <XhTagRoot variant="subtle" size="sm" :tone="item.status === ValidityStatus.Valid ? 'success' : 'neutral'">
                    <XhTagLabel>{{ item.status === ValidityStatus.Valid ? t('identity.condition.status_on') : t('identity.condition.status_off') }}</XhTagLabel>
                  </XhTagRoot>
                </td>
                <td class="condition-row__actions">
                  <XhButton v-if="can.update" variant="ghost" size="sm" @click="openEdit(item)">
                    <XhButtonLabel>{{ t('identity.condition.action_edit') }}</XhButtonLabel>
                  </XhButton>
                  <XhButton v-if="can.status" variant="ghost" size="sm" @click="toggleStatus(item)">
                    <XhButtonLabel>{{ item.status === ValidityStatus.Valid ? t('identity.condition.action_disable') : t('identity.condition.action_enable') }}</XhButtonLabel>
                  </XhButton>
                  <XhButton v-if="can.delete" variant="ghost" size="sm" tone="danger" @click="remove(item)">
                    <XhButtonLabel>{{ t('identity.condition.action_delete') }}</XhButtonLabel>
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
      <XhEmptyStateTitle>{{ t('identity.condition.empty_title') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('identity.condition.empty_description') }}</XhEmptyStateDescription>
    </XhEmptyStateRoot>

    <section v-if="batchResult" class="condition-panel__batch" :aria-label="t('identity.condition.batch_title')">
      <header class="condition-group__header">
        <span class="condition-group__title">{{ t('identity.condition.batch_title') }}</span>
        <span class="condition-group__hint">{{ t('identity.condition.batch_summary', { total: batchResult.totalUsers, denied: batchDenied }) }}</span>
      </header>
      <p class="condition-panel__tip">
        {{ t('identity.condition.batch_tip') }}
        <template v-if="batchResult.truncated">
          {{ t('identity.condition.batch_truncated', { count: batchResult.items.length }) }}
        </template>
      </p>
      <XDataTable :columns="batchColumns" :data="batchResult.items" row-key="userId" size="sm" :max-height="320" />
    </section>

    <XEditModal
      v-model:show="formVisible"
      :title="form.basicId ? t('identity.condition.form_title_edit') : t('identity.condition.form_title_create')"
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
      <XhFormRoot :id="formId" v-model:values="form" class="xh-edit-form-grid" @submit="submit">
        <XhFormFieldGroup name="attribute">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_attribute') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.attribute" :options="attributeOptions" :placeholder="t('identity.condition.ph_attribute')" />
            </XhFieldControl>
            <XhFieldDescription v-if="selectedAttribute?.isMultiValued">
              {{ t('identity.condition.multi_hint') }}
            </XhFieldDescription>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="selectedAttribute?.isFamily" name="familySuffix">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_family_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.familySuffix" clearable :placeholder="t('identity.condition.ph_family_name')" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="operator">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_operator') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.operator" :options="formOperatorOptions" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="negated">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_negate') }}</XhFieldLabel>
            <XhFieldControl>
              <XSegmented v-model:value="form.negated" :options="negateOptions" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="requiresValue(form.operator)" name="conditionValue">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_value') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.conditionValue" clearable />
            </XhFieldControl>
            <XhFieldDescription v-if="valueHint">
              {{ valueHint }}
            </XhFieldDescription>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="valueType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_value_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.valueType" :options="formValueTypeOptions" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="conditionGroup">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_group') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.conditionGroup" :min="0" />
            </XhFieldControl>
            <XhFieldDescription>{{ t('identity.condition.group_field_hint') }}</XhFieldDescription>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="description">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.condition.label_description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.description" clearable />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
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

.condition-panel__loading {
  display: flex;
  justify-content: center;
  padding-block: var(--xh-space-6);
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

.condition-group,
.condition-panel__batch {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
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
