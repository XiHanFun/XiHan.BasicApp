<script setup lang="ts">
import i18nEn from '@emoji-mart/data/i18n/en.json'
import i18nZh from '@emoji-mart/data/i18n/zh.json'
import data from '@emoji-mart/data/sets/15/all.json'
import appleSheet from 'emoji-datasource-apple/img/apple/sheets-256/32.png'
import facebookSheet from 'emoji-datasource-facebook/img/facebook/sheets-256/32.png'
import googleSheet from 'emoji-datasource-google/img/google/sheets-256/32.png'
import twitterSheet from 'emoji-datasource-twitter/img/twitter/sheets-256/32.png'
import { Picker } from 'emoji-mart'
import { NSelect } from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_EMOJI_SET_STORAGE_KEY } from '~/constants'
import { useTheme } from '~/hooks'
import { LocalStorage } from '~/utils'

defineOptions({ name: 'ChatEmojiPicker' })

const emit = defineEmits<{
  select: [emoji: string]
}>()

type EmojiSet = 'apple' | 'facebook' | 'google' | 'native' | 'twitter'

// 图片皮肤的精灵图本地打包（emoji-datasource 32px 整图，按所选皮肤懒加载；离线部署不走 CDN）
const SPRITESHEETS: Record<Exclude<EmojiSet, 'native'>, string> = {
  apple: appleSheet,
  facebook: facebookSheet,
  google: googleSheet,
  twitter: twitterSheet,
}

const { locale, t } = useI18n()
const { isDark } = useTheme()

const containerRef = ref<HTMLDivElement>()
const currentSet = ref<EmojiSet>(LocalStorage.get<EmojiSet>(CHAT_EMOJI_SET_STORAGE_KEY) ?? 'native')

const setOptions = computed(() => [
  { label: t('chat.composer.emoji_set_native'), value: 'native' },
  { label: 'Apple', value: 'apple' },
  { label: 'Google', value: 'google' },
  { label: 'Twitter', value: 'twitter' },
  { label: 'Facebook', value: 'facebook' },
])

let picker: HTMLElement | null = null

/** emoji-mart Picker 是一次性配置的自定义元素：皮肤/主题/语言变化时整体重建 */
function buildPicker() {
  const host = containerRef.value
  if (!host) {
    return
  }
  picker?.remove()
  picker = new Picker({
    data,
    i18n: locale.value.toLowerCase().startsWith('zh') ? i18nZh : i18nEn,
    set: currentSet.value,
    theme: isDark.value ? 'dark' : 'light',
    skinTonePosition: 'search',
    previewPosition: 'none',
    navPosition: 'top',
    emojiButtonSize: 32,
    emojiSize: 22,
    maxFrequentRows: 2,
    getSpritesheetURL: (set: string) => SPRITESHEETS[set as Exclude<EmojiSet, 'native'>] ?? '',
    onEmojiSelect: (emoji: { native?: string }) => {
      if (emoji?.native) {
        emit('select', emoji.native)
      }
    },
  }) as unknown as HTMLElement
  host.append(picker)
}

watch(currentSet, (set) => {
  LocalStorage.set(CHAT_EMOJI_SET_STORAGE_KEY, set)
  buildPicker()
})

watch([isDark, locale], () => buildPicker())

onMounted(buildPicker)

onBeforeUnmount(() => {
  picker?.remove()
  picker = null
})
</script>

<template>
  <div class="chat-emoji-picker">
    <div class="flex items-center justify-between px-1 pt-1.5 pb-1">
      <span class="text-xs text-muted-foreground">{{ t('chat.composer.emoji') }}</span>
      <NSelect
        v-model:value="currentSet"
        size="tiny"
        :options="setOptions"
        :consistent-menu-width="false"
        style="width: 112px"
      />
    </div>
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
