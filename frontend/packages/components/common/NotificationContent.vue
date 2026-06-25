<script setup lang="ts">
import { NotificationContentFormat } from '~/types/enums'
import MdEditor from './MdEditor.vue'

defineOptions({ name: 'NotificationContent' })

const props = defineProps<{
  /** 正文内容 */
  content?: string | null
  /** 正文格式，为空时按 Markdown 渲染 */
  format?: NotificationContentFormat
}>()
</script>

<template>
  <MdEditor
    v-if="props.format === undefined || props.format === NotificationContentFormat.Markdown"
    preview-only
    :model-value="props.content ?? ''"
  />
  <!-- eslint-disable-next-line vue/no-v-html -->
  <div v-else-if="props.format === NotificationContentFormat.Html" v-html="props.content ?? ''" />
  <pre v-else style="white-space: pre-wrap; margin: 0; font-family: inherit">{{ props.content }}</pre>
</template>
