<script setup lang="ts">
import type { WidgetData } from './widget-data'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { workbench as workbenchApi } from '~/contracts'
import { Icon } from '~/iconify'
import { useFormatters } from './charts/chart-helpers'
import { errorText, viewerOffset } from './widget-data'
import WidgetCard from './WidgetCard.vue'

/**
 * 今日统计：各包登记的单值指标（今日登录、今日访问、今日异常、在线用户……），只列调用方看得到的，
 * 后端加了新指标这里自动多一格。较昨日的变化写在悬停提示里。
 */
defineOptions({ name: 'StatsWidget' })

const { t } = useI18n()
const formatters = useFormatters()

/** 认得的指标给图标与颜色（颜色只为彼此区分、不表语义，取组件库的色相色阶）；其余用通用图标 */
const METRIC_META: Record<string, { icon: string, color: string }> = {
  'auditing.logins-today': { icon: 'lucide:log-in', color: 'var(--xh-color-blue-500)' },
  'auditing.requests-today': { icon: 'lucide:activity', color: 'var(--xh-color-green-500)' },
  'auditing.exceptions-today': { icon: 'lucide:bug', color: 'var(--xh-color-amber-500)' },
  'identity.online-users': { icon: 'lucide:users', color: 'var(--xh-color-purple-500)' },
}
const FALLBACK_META = { icon: 'lucide:gauge', color: 'var(--xh-color-blue-500)' }

const metrics = ref<Array<{ key: string, data: WidgetData | null }>>([])
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const { items } = await workbenchApi.widgets({})
    // 认得的指标按登记顺序排在前（登录、访问、异常、在线），其余按键名跟在后面
    const order = Object.keys(METRIC_META)
    const rank = (key: string) => (order.includes(key) ? order.indexOf(key) : order.length)
    const keys = items.filter(item => item.kind === 'Metric').map(item => item.key).sort((a, b) => rank(a) - rank(b) || a.localeCompare(b))
    const offset = viewerOffset()
    metrics.value = await Promise.all(keys.map(async key => ({
      key,
      data: await workbenchApi.widgetData({ key, offset }).catch(() => null),
    })))
  }
  catch (cause) {
    error.value = errorText(cause)
  }
  finally {
    loading.value = false
  }
})

/** 指标键 → 文案键：点与连字符都换成下划线 */
function labelOf(key: string): string {
  return t(`workbench.metrics.${key.replace(/[.-]/g, '_')}`)
}

const statCards = computed(() => metrics.value.map(({ key, data }) => {
  const meta = METRIC_META[key] ?? FALLBACK_META
  const value = data?.value
  const previous = data?.previous
  return {
    key,
    label: labelOf(key),
    value: value == null ? '-' : formatters.value.number(value),
    hint: value == null || previous == null ? undefined : t('workbench.charts.vs_yesterday_count', { value: formatters.value.number(previous) }),
    icon: meta.icon,
    color: meta.color,
  }
}))
</script>

<template>
  <WidgetCard icon="lucide:gauge" :title="t('workbench.widgets.stats.title')">
    <div v-if="loading" class="stats-placeholder" role="status">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="error || !statCards.length" class="stats-placeholder">
      {{ error ? t('common.messages.load_failed') : t('common.no_data') }}
    </div>
    <!-- 按小组件自身宽度排布：极窄单列横排；中等两列、图标在上；宽到每格放得下横排时再横排，足够宽四列一行 -->
    <div v-else class="grid grid-cols-1 gap-2 @[14rem]:grid-cols-2 @2xl:grid-cols-4 @2xl:gap-3">
      <div
        v-for="stat in statCards"
        :key="stat.key"
        :title="stat.hint"
        class="flex min-w-0 items-center gap-3 rounded-xl border border-border/60 bg-background px-3 py-2.5 @[14rem]:flex-col @[14rem]:items-start @[14rem]:gap-2 @md:flex-row @md:items-center @md:gap-3"
      >
        <div
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl"
          :style="{ backgroundColor: `color-mix(in oklab, ${stat.color} 9%, transparent)` }"
        >
          <Icon :icon="stat.icon" width="20" height="20" :style="{ color: stat.color }" />
        </div>
        <div class="flex min-w-0 max-w-full flex-col gap-0.5">
          <span class="text-xl font-bold leading-tight text-foreground tabular-nums">{{ stat.value }}</span>
          <span class="truncate text-xs text-muted-foreground">{{ stat.label }}</span>
        </div>
      </div>
    </div>
  </WidgetCard>
</template>

<style scoped>
.stats-placeholder {
  padding-block: var(--xh-space-6);
  text-align: center;
  color: var(--xh-fg-muted);
}
</style>
