<script setup lang="ts">
import { XInput } from '~/components'

/**
 * 流程元数据在设计器属性面板里的一行（编码、名称、分类、描述）：只显示当前值，不给改；
 * 这几项在流程定义列表的「编辑」里改，设计器只管节点、连线、启动变量与执行设置。
 */
defineOptions({ name: 'FlowMetadataField' })

defineProps<{
  value?: string
  /** 属性面板这一行的名称，作输入框的可访问名 */
  label?: string
  /** 按多行显示（描述） */
  multiline?: boolean
  /** 显示在值下面的提示，一条一行 */
  hints?: readonly string[]
}>()
</script>

<template>
  <div class="flow-metadata-field">
    <XInput
      size="sm"
      :value="value ?? ''"
      :type="multiline ? 'textarea' : 'text'"
      :autosize="multiline ? { minRows: 2, maxRows: 6 } : undefined"
      read-only
      :aria-label="label"
    />
    <p v-for="hint in hints" :key="hint" class="flow-metadata-field__hint">
      {{ hint }}
    </p>
  </div>
</template>

<style scoped>
.flow-metadata-field {
  --xh-control-w: 100%;

  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
}

.flow-metadata-field__hint {
  margin: 0;
  font-size: var(--xh-text-caption-size);
  color: var(--xh-fg-muted);
}
</style>
