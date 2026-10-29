<script setup lang="ts">
import { createStreamRenderer } from '@xihan-ui/markdown'
import { computed, onBeforeUnmount, useId } from 'vue'
import { NotificationContentFormat } from '~/types/enums'

defineOptions({ name: 'NotificationContent' })

const props = defineProps<{
  /** 正文内容 */
  content?: string | null
  /** 正文格式，为空/未知时按纯文本渲染（纯文本账号通知若误当 Markdown，会因首段 <p> 外边距出现整行空白） */
  format?: NotificationContentFormat
}>()

// 组件库的 Markdown 部件只渲染解析好的块：正文一次给全（非流式），由 @xihan-ui/markdown 解析；脚注锚点按实例分前缀，收件箱同时渲染多条也不串
const renderer = createStreamRenderer({ idPrefix: `notif-${useId()}-` })
const blocks = computed(() => props.format === NotificationContentFormat.Markdown ? renderer.render(props.content ?? '', { ended: true }) : [])
onBeforeUnmount(() => renderer.dispose())
</script>

<template>
  <!-- 仅显式 Markdown 才走渲染（组件库的 Markdown 部件，非流式）；缺省(undefined)与 Text 一律走纯文本兜底，避免纯文本被当 Markdown 渲出整行空白 -->
  <div v-if="props.format === NotificationContentFormat.Markdown" class="notif-content-md">
    <XhMarkdownStreamRoot :blocks="blocks" :streaming="false" :caret="false">
      <XhMarkdownStreamContent />
    </XhMarkdownStreamRoot>
  </div>
  <!-- eslint-disable-next-line vue/no-v-html -->
  <div v-else-if="props.format === NotificationContentFormat.Html" class="notif-content-html" v-html="props.content ?? ''" />
  <pre v-else class="notif-content-text">{{ props.content }}</pre>
</template>

<style scoped>
.notif-content-text {
  margin: 0;
  font-family: inherit;
  white-space: pre-line;
  word-break: break-word;
}
.notif-content-html,
.notif-content-md {
  word-break: break-word;
}
</style>
