<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'ClockWidget' })

const { t } = useI18n()
const now = ref(new Date())
let timer: number | undefined

onMounted(() => {
  timer = window.setInterval(() => (now.value = new Date()), 1000)
})
onUnmounted(() => {
  if (timer)
    window.clearInterval(timer)
})

const time = computed(() => now.value.toLocaleTimeString('zh-CN', { hour12: false }))
const date = computed(() =>
  now.value.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' }),
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
      <div class="font-mono text-4xl font-bold tabular-nums text-foreground">
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
