<script setup lang="ts">
import { XhButton, XhFlex } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { XInput } from '~/components'

/**
 * 连线条件的编辑器（设计器属性面板「条件」一行）：条件表达式，加上源节点常见办理结果的一键填入。
 */
defineOptions({ name: 'EdgeConditionEditor' })

const props = defineProps<{
  value?: string
  /** 属性面板这一行的名称，作输入框的可访问名 */
  label?: string
  readonly: boolean
  /** 源节点的常见结果，点一下把条件填成 outcome == '<结果>' */
  outcomes: readonly string[]
}>()

const emit = defineEmits<{
  'update:value': [value: string | undefined]
  'commit': []
}>()

const { t } = useI18n()

function fill(outcome: string) {
  emit('update:value', `outcome == '${outcome}'`)
  emit('commit')
}
</script>

<template>
  <div class="edge-condition-editor">
    <XInput
      :value="props.value ?? ''"
      type="textarea"
      size="sm"
      class="font-mono"
      :autosize="{ minRows: 2, maxRows: 4 }"
      :read-only="readonly"
      :aria-label="label"
      :placeholder="t('workflow.designer.edge_condition_placeholder')"
      @update:value="(raw: string | number | (string | number)[] | null) => emit('update:value', raw ? String(raw) : undefined)"
      @blur="emit('commit')"
    />
    <!-- 可选结果点一下填进条件：是动作就用按钮，标签没有按钮语义、键盘也够不着 -->
    <XhFlex v-if="outcomes.length > 0 && !readonly" gap="xs" wrap>
      <XhButton v-for="outcome in outcomes" :key="outcome" variant="subtle" size="sm" @click="fill(outcome)">
        {{ outcome }}
      </XhButton>
    </XhFlex>
  </div>
</template>

<style scoped>
.edge-condition-editor {
  --xh-control-w: 100%;

  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
}
</style>
