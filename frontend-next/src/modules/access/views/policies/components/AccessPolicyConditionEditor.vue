<script setup lang="ts">
import type { ConditionAttribute, ConditionOperator } from '../../roles/permission-condition'
import type { ConditionGroupModel, ConditionRowModel } from '../access-policy'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XInput, XSelect } from '~/components'
import { attributeLabel, operatorLabel, valueHint } from '../../roles/condition-labels'
import { CONDITION_OPERATORS, requiresValue } from '../../roles/permission-condition'
import { emptyRow, newKey, phaseLabel, phaseOf } from '../access-policy'

/**
 * 访问策略的条件编辑（4.x 同）：组内全部满足这组才满足，任一组满足策略就命中；时机按用到的属性推出。
 */
defineOptions({ name: 'AccessPolicyConditionEditor' })

const props = withDefaults(defineProps<{
  catalog: readonly ConditionAttribute[]
  /** 是否平台侧（只有平台策略分认证前时机） */
  platform: boolean
  /** 是否显示时机（约束规则的条件不分时机） */
  showPhase?: boolean
}>(), { showPhase: true })

const groups = defineModel<ConditionGroupModel[]>('groups', { required: true })

const { t } = useI18n()

const attributeOptions = computed(() => props.catalog.map(item => ({ value: item.name, label: `${attributeLabel(t, item.name)} · ${item.name}` })))
const phase = computed(() => phaseOf(groups.value, props.catalog, props.platform))

function attributeOf(row: ConditionRowModel) {
  return props.catalog.find(item => item.name === row.attribute) ?? null
}

/** 选了属性只列它可用的运算符，没选列出全部 */
function operatorOptionsFor(row: ConditionRowModel) {
  return (attributeOf(row)?.operators ?? CONDITION_OPERATORS).map(operator => ({ value: operator, label: operatorLabel(t, operator) }))
}

function hintFor(row: ConditionRowModel) {
  const attribute = attributeOf(row)
  return attribute ? valueHint(t, attribute.kind, row.operator) : t('access.policies.editor.pick_attribute_first')
}

function onAttributeChange(row: ConditionRowModel, value: unknown) {
  row.attribute = typeof value === 'string' ? value : ''
  row.familySuffix = ''
  const operators = attributeOf(row)?.operators ?? []
  if (operators.length > 0 && !operators.includes(row.operator))
    row.operator = operators[0]!
}

function addGroup() {
  groups.value = [...groups.value, { key: newKey(), rows: [emptyRow()] }]
}

function removeGroup(index: number) {
  groups.value = groups.value.filter((_, position) => position !== index)
}

function addRow(group: ConditionGroupModel) {
  group.rows.push(emptyRow())
}

function removeRow(group: ConditionGroupModel, index: number) {
  group.rows.splice(index, 1)
}
</script>

<template>
  <div class="policy-conditions" data-testid="policy-condition-editor">
    <div class="policy-conditions__head">
      <span class="policy-conditions__title">{{ t('access.policies.editor.title') }}</span>
      <XhTagRoot v-if="showPhase" variant="subtle" size="sm" :tone="phase === 'BeforeAuthentication' ? 'info' : 'warning'">
        <XhTagLabel>{{ phaseLabel(t, phase) }}</XhTagLabel>
      </XhTagRoot>
    </div>
    <p v-if="showPhase" class="policy-conditions__hint">
      {{ platform ? t('access.policies.editor.hint_platform') : t('access.policies.editor.hint_tenant') }}
    </p>

    <template v-for="(group, groupIndex) in groups" :key="group.key">
      <div v-if="groupIndex > 0" class="policy-conditions__or">
        {{ t('access.policies.editor.or') }}
      </div>
      <section class="policy-group" :aria-label="t('access.policies.editor.group_title', { index: groupIndex + 1 })">
        <div class="policy-group__head">
          <span class="policy-group__title">{{ t('access.policies.editor.group_title', { index: groupIndex + 1 }) }}</span>
          <span class="policy-group__hint">{{ t('access.policies.editor.group_hint') }}</span>
          <XhButton variant="ghost" size="sm" tone="danger" @click="removeGroup(groupIndex)">
            <XhButtonLabel>{{ t('access.policies.editor.remove_group') }}</XhButtonLabel>
          </XhButton>
        </div>
        <div class="policy-group__scroll">
          <div class="policy-group__grid">
            <span class="policy-group__col">{{ t('access.policies.editor.attribute') }}</span>
            <span class="policy-group__col">{{ t('access.policies.editor.operator') }}</span>
            <span class="policy-group__col">{{ t('access.policies.editor.negate') }}</span>
            <span class="policy-group__col">{{ t('access.policies.editor.value') }}</span>
            <span aria-hidden="true" />
            <div v-for="(row, rowIndex) in group.rows" :key="row.key" class="policy-row">
              <div class="policy-row__attribute">
                <XSelect
                  :value="row.attribute || null"
                  :options="attributeOptions"
                  :placeholder="t('access.policies.editor.attribute_placeholder')"
                  :aria-label="t('access.policies.editor.attribute')"
                  @update:value="value => onAttributeChange(row, value)"
                />
                <XInput
                  v-if="attributeOf(row)?.isFamily"
                  v-model:value="row.familySuffix"
                  :placeholder="t('access.policies.editor.family_suffix')"
                  :aria-label="t('access.policies.editor.family_suffix')"
                />
              </div>
              <XSelect
                :value="row.operator"
                :options="operatorOptionsFor(row)"
                :aria-label="t('access.policies.editor.operator')"
                @update:value="value => (row.operator = value as ConditionOperator)"
              />
              <XhSwitch
                :checked="row.isNegated"
                :aria-label="t('access.policies.editor.negate')"
                @update:checked="(value: boolean) => (row.isNegated = value)"
              />
              <XInput
                v-model:value="row.value"
                :disabled="!requiresValue(row.operator)"
                :placeholder="hintFor(row)"
                :aria-label="t('access.policies.editor.value')"
              />
              <XhButton variant="ghost" size="sm" tone="danger" :aria-label="t('access.policies.editor.remove_condition')" @click="removeRow(group, rowIndex)">
                <XhButtonIndicator>
                  <Icon icon="lucide:x" width="16" />
                </XhButtonIndicator>
              </XhButton>
            </div>
          </div>
        </div>
        <XhButton variant="subtle" size="sm" @click="addRow(group)">
          <XhButtonIndicator>
            <Icon icon="lucide:plus" width="16" />
          </XhButtonIndicator>
          <XhButtonLabel>{{ t('access.policies.editor.add_condition') }}</XhButtonLabel>
        </XhButton>
      </section>
    </template>

    <XhButton variant="outline" size="sm" @click="addGroup">
      <XhButtonIndicator>
        <Icon icon="lucide:list-plus" width="16" />
      </XhButtonIndicator>
      <XhButtonLabel>{{ t('access.policies.editor.add_group') }}</XhButtonLabel>
    </XhButton>
  </div>
</template>

<style scoped>
.policy-conditions {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  margin-block-start: var(--xh-space-1);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.policy-conditions__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.policy-conditions__title {
  font-weight: var(--xh-font-weight-medium);
}

.policy-conditions__hint {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.policy-conditions__or {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-align: center;
}

.policy-group {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px dashed var(--xh-border-default);
  border-radius: var(--xh-radius-sm);
}

.policy-group__head {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  inline-size: 100%;
}

.policy-group__title {
  font-weight: var(--xh-font-weight-medium);
}

.policy-group__hint {
  flex: 1;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

/* 窄屏放不下各列的下限时，表格在自己的框里横向滚 */
.policy-group__scroll {
  inline-size: 100%;
  overflow-x: auto;
}

.policy-group__grid {
  display: grid;
  grid-template-columns: minmax(14em, 2fr) minmax(8em, 1fr) auto minmax(12em, 2fr) auto;
  gap: var(--xh-space-2);
  align-items: center;
}

.policy-group__col {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.policy-row {
  display: contents;
}

.policy-row__attribute {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  min-inline-size: 0;
}

.policy-group__grid :is([data-scope='select'], [data-scope='text-field'])[data-part='root'] {
  inline-size: 100%;
  min-inline-size: 0;
}
</style>
