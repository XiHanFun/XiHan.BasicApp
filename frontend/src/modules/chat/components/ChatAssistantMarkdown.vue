<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { createStreamRenderer } from '@xihan-ui/markdown'
import { computed, onBeforeUnmount, useId } from 'vue'
import { assistantMarkdownSource } from './chat-helpers'

defineOptions({ name: 'ChatAssistantMarkdown' })

const props = defineProps<{
  /** 助手回复的正文 */
  text: string
  /** 还在生成：未闭合的语法先按生长态显示，结束后定形 */
  streaming?: boolean
}>()

const renderer = createStreamRenderer({ idPrefix: `chat-md-${useId()}-` })
const blocks = computed(() => renderer.render(assistantMarkdownSource(props.text), { ended: !props.streaming }))
onBeforeUnmount(() => renderer.dispose())
</script>

<template>
  <div class="chat-assistant-md text-[13px] leading-relaxed">
    <XhMarkdownStreamRoot :blocks="blocks" :streaming="!!props.streaming" :caret="false">
      <XhMarkdownStreamContent />
    </XhMarkdownStreamRoot>
  </div>
</template>

<style scoped>
.chat-assistant-md {
  min-width: 0;
  max-width: 100%;
  overflow-x: auto;
  word-break: break-word;
  white-space: normal;
}
</style>
