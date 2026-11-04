<script setup lang="ts">
import { createStreamRenderer } from '@xihan-ui/markdown'
import DOMPurify from 'dompurify'
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

// 通知的写权限租户也有，HTML 正文渲染前净化：脚本、事件处理器与 javascript: 链接由 DOMPurify 去掉，再去掉能改全局样式的 <style> 与能钓鱼的表单控件；行内样式留着（与 4.x 一样能设颜色），固定定位由 contain 关在正文框里
const safeHtml = computed(() => props.format === NotificationContentFormat.Html
  ? DOMPurify.sanitize(props.content ?? '', { USE_PROFILES: { html: true }, FORBID_TAGS: ['style', 'form', 'input', 'button', 'textarea', 'select'] })
  : '')
</script>

<template>
  <!-- 仅显式 Markdown 才走渲染（组件库的 Markdown 部件，非流式）；缺省(undefined)与 Text 一律走纯文本兜底，避免纯文本被当 Markdown 渲出整行空白 -->
  <div v-if="props.format === NotificationContentFormat.Markdown" class="notif-content-md">
    <XhMarkdownStreamRoot :blocks="blocks" :streaming="false" :caret="false">
      <XhMarkdownStreamContent />
    </XhMarkdownStreamRoot>
  </div>
  <!-- eslint-disable-next-line vue/no-v-html -->
  <div v-else-if="props.format === NotificationContentFormat.Html" class="notif-content-html" v-html="safeHtml" />
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
.notif-content-html {
  contain: paint;
}
</style>
