<script setup lang="ts">
import { useId } from 'vue'
import { NotificationContentFormat } from '~/types/enums'
import MdEditor from './MdEditor.vue'

defineOptions({ name: 'NotificationContent' })

const props = defineProps<{
  /** 正文内容 */
  content?: string | null
  /** 正文格式，为空/未知时按纯文本渲染（纯文本账号通知若误当 Markdown，会因首段 <p> 外边距出现整行空白） */
  format?: NotificationContentFormat
}>()

// md-editor-v3 要求每个实例 editorId 唯一（收件箱等会同时渲染多条，复用同一 id 会串扰/渲染异常）
const editorId = `notif-md-${useId()}`
</script>

<template>
  <!-- 仅显式 Markdown 才走预览渲染；缺省(undefined)与 Text 一律走纯文本兜底，避免纯文本被当 Markdown 渲出整行空白 -->
  <!-- 包一层 div：class 落在外层，:deep(.md-editor) 才能命中 MdPreview 根(否则 class 与 .md-editor 同元素，选择器不匹配，底色去不掉) -->
  <div v-if="props.format === NotificationContentFormat.Markdown" class="notif-content-md">
    <MdEditor preview-only :editor-id="editorId" :model-value="props.content ?? ''" />
  </div>
  <!-- eslint-disable-next-line vue/no-v-html -->
  <div v-else-if="props.format === NotificationContentFormat.Html" class="notif-content-html" v-html="props.content ?? ''" />
  <pre v-else class="notif-content-text">{{ props.content }}</pre>
</template>

<style scoped>
/* 让 Markdown 预览融入承载容器（收件箱/详情/弹窗）：去掉编辑器自带底色与外边距，跟随上下文背景 */
.notif-content-md :deep(.md-editor),
.notif-content-md :deep(.md-editor-preview-wrapper),
.notif-content-md :deep(.md-editor-preview) {
  background-color: transparent;
}

.notif-content-md :deep(.md-editor-preview-wrapper) {
  padding: 0;
}

.notif-content-text {
  margin: 0;
  font-family: inherit;
  white-space: pre-line;
  word-break: break-word;
}

.notif-content-html {
  word-break: break-word;
}
</style>
