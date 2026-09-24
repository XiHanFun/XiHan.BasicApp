<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '~/stores'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'ClockWidget' })

const { t } = useI18n()
const appStore = useAppStore()
const now = ref(new Date())
let timer: number | undefined

onMounted(() => {
  timer = window.setInterval(() => (now.value = new Date()), 1000)
})
onUnmounted(() => {
  if (timer)
    window.clearInterval(timer)
})

// 时间与日期都跟随应用语言：原先时间写死 zh-CN、日期用浏览器默认，
// 两者不一致，且日语界面下会显示中文格式
const time = computed(() => now.value.toLocaleTimeString(appStore.locale, { hour12: false }))
const date = computed(() =>
  now.value.toLocaleDateString(appStore.locale, { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' }),
)
const greeting = computed(() => {
  const h = now.value.getHours()
  if (h < 11)
    return t('workbench.widgets.greeting_morning')
  if (h < 13)
    return t('workbench.widgets.greeting_noon')
  if (h < 18)
    return t('workbench.widgets.greeting_afternoon')
  return t('workbench.widgets.greeting_evening')
})
</script>

<template>
  <WidgetCard icon="lucide:clock" :title="t('workbench.widgets.clock.title')">
    <div class="flex h-full flex-col items-center justify-center gap-1 text-center">
      <!-- 字号随小组件宽度流式缩放（cqi = 容器内宽 1%）：窄栅格下不再溢出出现横向滚动条 -->
      <div class="font-mono text-[length:clamp(1.25rem,18cqi,2.25rem)] font-bold leading-tight tabular-nums text-foreground">
        {{ time }}
      </div>
      <div class="text-sm text-muted-foreground">
        {{ date }}
      </div>
      <div class="mt-1 text-sm font-medium text-[hsl(var(--primary))]">
        {{ greeting }}
      </div>
    </div>
  </WidgetCard>
</template>
