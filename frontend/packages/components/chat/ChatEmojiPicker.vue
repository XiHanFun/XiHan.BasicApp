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
  // 肤色选择已隐藏，但 emoji-mart 优先读 localStorage 里历史选过的肤色——清掉才能真正固定默认肤色
  try {
    localStorage.removeItem('emoji-mart.skin')
  }
  catch {
    // 隐私模式等场景不可用时忽略
  }
  picker?.remove()
  picker = new Picker({
    data,
    i18n: locale.value.toLowerCase().startsWith('zh') ? i18nZh : i18nEn,
    set: 'native',
    skin: 1,
    theme: isDark.value ? 'dark' : 'light',
    skinTonePosition: 'none',
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
/* em-emoji-picker 宿主默认 width:min-content（按内容自适应）：外壳贴内容宽，别强拉 100% 造成左右间隙不等 */
.chat-emoji-picker {
  width: fit-content;
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
  height: 320px;
  max-width: 100%;
  /* 外壳已有卡片阴影，去掉元素自带阴影避免双层 */
  --shadow: none;
}
</style>
