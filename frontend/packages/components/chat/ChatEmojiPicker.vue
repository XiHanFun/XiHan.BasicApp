<script setup lang="ts">
import i18nEn from '@emoji-mart/data/i18n/en.json'
import i18nZh from '@emoji-mart/data/i18n/zh.json'
import data from '@emoji-mart/data/sets/15/native.json'
import { Picker } from 'emoji-mart'
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'

defineOptions({ name: 'ChatEmojiPicker' })

const emit = defineEmits<{
  select: [emoji: string]
}>()

const { locale } = useI18n()
const { isDark } = useTheme()

const containerRef = ref<HTMLDivElement>()

let picker: HTMLElement | null = null

/** emoji-mart Picker 是一次性配置的自定义元素：主题/语言变化时整体重建（皮肤固定 native 系统渲染） */
function buildPicker() {
  const host = containerRef.value
  if (!host) {
    return
  }
  picker?.remove()
  picker = new Picker({
    data,
    i18n: locale.value.toLowerCase().startsWith('zh') ? i18nZh : i18nEn,
    set: 'native',
    theme: isDark.value ? 'dark' : 'light',
    skinTonePosition: 'search',
    previewPosition: 'none',
    navPosition: 'top',
    emojiButtonSize: 32,
    emojiSize: 22,
    maxFrequentRows: 2,
    onEmojiSelect: (emoji: { native?: string }) => {
      if (emoji?.native) {
        emit('select', emoji.native)
      }
    },
  }) as unknown as HTMLElement
  host.append(picker)
}

watch([isDark, locale], () => buildPicker())

onMounted(buildPicker)

onBeforeUnmount(() => {
  picker?.remove()
  picker = null
})
</script>

<template>
  <div class="chat-emoji-picker">
    <div ref="containerRef" class="chat-emoji-picker__host" />
  </div>
</template>

<style scoped>
.chat-emoji-picker {
  width: 356px;
  max-width: calc(100vw - 24px);
  padding: 4px;
  border: 1px solid hsl(var(--border));
  border-radius: 10px;
  background: hsl(var(--card));
  box-shadow:
    0 8px 30px hsl(var(--foreground) / 8%),
    0 2px 8px hsl(var(--foreground) / 4%);
}

.chat-emoji-picker__host :deep(em-emoji-picker) {
  width: 100%;
  height: 320px;
}
</style>
