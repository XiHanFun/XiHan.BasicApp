<script setup lang="ts">
import type { WorkflowBusinessTypeDto } from '../../../../api'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput, XSelect } from '~/components'

/**
 * 流程编码的编辑器（设计器属性面板「编码」一行）：先选要关联的审批业务，再看或改编码。
 *
 * 流程编码即业务编码，选了业务就把编码改成业务编码；编辑已有草稿时编码与业务只读——
 * 后端更新草稿沿用原编码，改了也会被还原。
 */
defineOptions({ name: 'FlowCodeEditor' })

const props = defineProps<{
  code?: string
  /** 属性面板这一行的名称，作编码输入框的可访问名 */
  label?: string
  /** 编辑已有草稿：编码与业务只读 */
  locked: boolean
  readonly: boolean
  businessTypes: readonly WorkflowBusinessTypeDto[]
}>()

const emit = defineEmits<{
  'update:code': [code: string]
  'commit': []
  'bind': [business: WorkflowBusinessTypeDto]
}>()

const { t } = useI18n()

const boundBusiness = computed(() => props.businessTypes.find(item => item.code === props.code?.trim()) ?? null)
const businessOptions = computed(() => props.businessTypes.map(item => ({ label: `${item.name}（${item.code}）`, value: item.code })))
const disabled = computed(() => props.locked || props.readonly)

function onBusiness(code: string | number | (string | number)[] | null) {
  const business = typeof code === 'string' ? props.businessTypes.find(item => item.code === code) : undefined
  if (business)
    emit('bind', business)
}
</script>

<template>
  <div class="flow-code-editor">
    <XSelect
      size="sm"
      :value="boundBusiness?.code ?? null"
      :options="businessOptions"
      :disabled="disabled"
      :placeholder="t('workflow.designer.flow_business_placeholder')"
      :aria-label="t('workflow.designer.flow_business')"
      @update:value="onBusiness"
    />
    <p v-if="boundBusiness" class="flow-code-editor__hint">
      {{ t('workflow.designer.flow_business_tip', { name: boundBusiness.name, outcomes: boundBusiness.outcomes.join(' / ') }) }}
    </p>
    <XInput
      size="sm"
      :value="code ?? ''"
      :read-only="disabled"
      :placeholder="t('workflow.designer.flow_code_placeholder')"
      :aria-label="label"
      @update:value="(raw: string | number | (string | number)[] | null) => emit('update:code', String(raw ?? ''))"
      @blur="emit('commit')"
    />
  </div>
</template>

<style scoped>
.flow-code-editor {
  --xh-control-w: 100%;

  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
}

.flow-code-editor__hint {
  margin: 0;
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}
</style>
