<script setup lang="ts">
import type { OrganizationDirectory } from '../../shared/workflow-directory'
import type { AssigneeRule } from './assignee-rules'
import { XhButton, XhButtonIndicator, XhButtonLabel } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XCombobox, XInput, XNumberInput, XSelect } from '~/components'
import { loadOrganizationDirectory } from '../../shared/workflow-directory'
import WorkflowUserPicker from '../../shared/WorkflowUserPicker.vue'
import { ASSIGNEE_RULE_TYPE_MAP, ASSIGNEE_RULE_TYPES, createRule, isRuleComplete, MAX_LEADER_LEVEL, readRules } from './assignee-rules'

/**
 * 受理人规则编辑器：一组 `{ type, values }`，按顺序解析并合并（依次审批按此顺序排队）。
 * 角色 / 部门 / 岗位按编码选择，用户按主键选择，主管按层级，表达式引用流程变量。
 */
defineOptions({ name: 'AssigneeRulesEditor' })

const props = defineProps<{
  value?: unknown
  /** 规则组名称（行内控件的可访问名称前缀） */
  label: string
  /** 表达式规则可引用的流程变量名（提示用） */
  variables?: string[]
}>()

const emit = defineEmits<{
  'update:value': [value: AssigneeRule[] | null]
}>()

const { t } = useI18n()

const rules = computed(() => readRules(props.value))
const directory = ref<OrganizationDirectory | null>(null)
const directoryFailed = ref(false)

const typeOptions = computed(() => ASSIGNEE_RULE_TYPES.map(meta => ({
  label: t(`workflow.designer.rule.${meta.labelKey}`),
  value: meta.type,
})))

const expressionPlaceholder = computed(() => props.variables?.length
  ? t('workflow.designer.rule.expression_placeholder_vars', { vars: props.variables.join(', ') })
  : t('workflow.designer.rule.expression_placeholder'))

function emitRules(next: AssigneeRule[]) {
  emit('update:value', next.length > 0 ? next : null)
}

function updateRule(index: number, patch: Partial<AssigneeRule>) {
  emitRules(rules.value.map((rule, current) => current === index ? { ...rule, ...patch } : rule))
}

function changeType(index: number, type: string | number | (string | number)[] | null) {
  if (typeof type !== 'string' || !type)
    return
  emitRules(rules.value.map((rule, current) => current === index ? createRule(type) : rule))
}

function removeRule(index: number) {
  emitRules(rules.value.filter((_, current) => current !== index))
}

function addRule() {
  emitRules([...rules.value, createRule('Role')])
}

function toValues(value: string | number | (string | number)[] | null): string[] {
  if (value === null || value === undefined)
    return []
  return (Array.isArray(value) ? value : [value]).map(item => String(item))
}

function editorOf(rule: AssigneeRule) {
  return ASSIGNEE_RULE_TYPE_MAP[rule.type]?.editor ?? 'unknown'
}

function organizationOptions(rule: AssigneeRule) {
  const editor = editorOf(rule)
  if (!directory.value)
    return []
  if (editor === 'roles')
    return directory.value.roles
  if (editor === 'positions')
    return directory.value.positions
  return directory.value.departments
}

onMounted(async () => {
  try {
    directory.value = await loadOrganizationDirectory()
  }
  catch {
    directoryFailed.value = true
  }
})
</script>

<template>
  <div class="flex flex-col gap-2">
    <!-- 一条规则两列：左列是类型与取值两个控件，上下等宽；右列是删除钮，对着整条规则垂直居中 -->
    <div
      v-for="(rule, index) in rules"
      :key="index"
      class="assignee-rule"
    >
      <div class="assignee-rule__fields">
        <XSelect
          size="sm"
          :value="rule.type"
          :options="typeOptions"
          :aria-label="t('workflow.designer.rule.type_label', { group: label, index: index + 1 })"
          @update:value="(value: string | number | (string | number)[] | null) => changeType(index, value)"
        />

        <WorkflowUserPicker
          v-if="editorOf(rule) === 'users'"
          multiple
          :value="rule.values"
          :invalid="!isRuleComplete(rule)"
          :placeholder="t('workflow.designer.rule.users_placeholder')"
          :aria-label="t('workflow.designer.rule.value_label', { group: label, index: index + 1 })"
          @update:value="(value: string | string[] | null) => updateRule(index, { values: toValues(value) })"
        />
        <XCombobox
          v-else-if="editorOf(rule) === 'roles' || editorOf(rule) === 'departments' || editorOf(rule) === 'positions'"
          multiple
          clearable
          :value="rule.values"
          :options="organizationOptions(rule)"
          :loading="!directory && !directoryFailed"
          :invalid="!isRuleComplete(rule)"
          :placeholder="t(`workflow.designer.rule.${editorOf(rule)}_placeholder`)"
          :empty="directoryFailed ? t('workflow.designer.rule.directory_failed') : undefined"
          :aria-label="t('workflow.designer.rule.value_label', { group: label, index: index + 1 })"
          @update:value="(value: string | number | (string | number)[] | null) => updateRule(index, { values: toValues(value) })"
        />
        <div v-else-if="editorOf(rule) === 'level'" class="flex items-center gap-2 text-xs text-muted-foreground">
          <span>{{ t('workflow.designer.rule.level_prefix') }}</span>
          <XNumberInput
            class="w-24"
            size="sm"
            :value="Number(rule.values[0] ?? 1)"
            :min="1"
            :max="MAX_LEADER_LEVEL"
            :precision="0"
            :invalid="!isRuleComplete(rule)"
            :aria-label="t('workflow.designer.rule.value_label', { group: label, index: index + 1 })"
            @update:value="(value: number | null) => updateRule(index, { values: value === null ? [] : [String(value)] })"
          />
          <span>{{ t(rule.type === 'StarterLeaderChain' ? 'workflow.designer.rule.level_chain_suffix' : 'workflow.designer.rule.level_suffix') }}</span>
        </div>
        <XInput
          v-else-if="editorOf(rule) === 'expression'"
          size="sm"
          class="font-mono"
          :value="rule.values[0] ?? ''"
          :invalid="!isRuleComplete(rule)"
          :placeholder="expressionPlaceholder"
          :aria-label="t('workflow.designer.rule.value_label', { group: label, index: index + 1 })"
          @update:value="(raw: string | number | (string | number)[] | null) => updateRule(index, { values: raw ? [String(raw)] : [] })"
        />
        <p v-else-if="editorOf(rule) === 'none'" class="text-xs text-muted-foreground">
          {{ t('workflow.designer.rule.starter_hint') }}
        </p>
        <p v-else class="text-xs text-muted-foreground">
          {{ t('workflow.designer.rule.custom_hint', { values: rule.values.join(', ') || '—' }) }}
        </p>
      </div>
      <XhButton
        variant="ghost"
        size="sm"
        icon-only
        :aria-label="t('workflow.designer.rule.remove', { index: index + 1 })"
        :title="t('workflow.designer.rule.remove', { index: index + 1 })"
        @click="removeRule(index)"
      >
        <Icon icon="lucide:x" />
      </XhButton>
    </div>

    <!-- 新增是这一组的次要动作：品牌色文字钮贴左排，不再铺成一整条灰底，抢不过规则本身 -->
    <XhButton class="xh-link-button self-start" variant="ghost" tone="brand" size="sm" @click="addRule">
      <XhButtonIndicator>
        <Icon icon="lucide:plus" />
      </XhButtonIndicator>
      <XhButtonLabel>{{ t('workflow.designer.rule.add') }}</XhButtonLabel>
    </XhButton>
  </div>
</template>

<style scoped>
/* 规则卡：surface 圆角的细描边面。左列铺满、右列按删除钮的宽度收，两列之间一档间距 */
.assignee-rule {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: var(--xh-space-1);
  padding: var(--xh-space-2);
  border: var(--xh-stroke-thin) solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
}

/* 两个控件都铺满左列，上下等宽：控件不在 Field 里时缺省 16rem 宽，经公开槽改成随列宽 */
.assignee-rule__fields {
  --xh-control-w: 100%;

  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
  min-inline-size: 0;
}
</style>
