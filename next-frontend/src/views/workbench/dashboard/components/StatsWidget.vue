<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { loadDashboardSummary } from './summary'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'StatsWidget' })

const { t } = useI18n()

// 今日统计（后端 DashboardSummary 真实数据；接口失败静默清零）
const accessCount = ref(0)
const operationCount = ref(0)
const loginCount = ref(0)
const apiCallCount = ref(0)

const statCards = computed(() => [
  { key: 'access', label: t('workbench.dashboard.stat_access'), value: accessCount.value, icon: 'lucide:mouse-pointer-click', color: '#3b82f6' },
  { key: 'operation', label: t('workbench.dashboard.stat_operation'), value: operationCount.value, icon: 'lucide:activity', color: '#22c55e' },
  { key: 'login', label: t('workbench.dashboard.stat_login'), value: loginCount.value, icon: 'lucide:log-in', color: '#8b5cf6' },
  { key: 'api', label: t('workbench.dashboard.stat_api'), value: apiCallCount.value, icon: 'lucide:webhook', color: '#f59e0b' },
])

onMounted(async () => {
  try {
    const summary = await loadDashboardSummary()
    accessCount.value = summary.statistics.accessCount
    operationCount.value = summary.statistics.operationCount
    loginCount.value = summary.statistics.loginCount
    apiCallCount.value = summary.statistics.apiCallCount
  }
  catch {
    // 静默回退零值
  }
})
</script>

<template>
  <WidgetCard icon="lucide:gauge" :title="t('workbench.widgets.stats.title')">
    <div class="grid grid-cols-2 gap-3 xl:grid-cols-4">
      <div
        v-for="stat in statCards"
        :key="stat.key"
        class="flex items-center gap-3 rounded-xl border border-border/60 bg-background px-4 py-3"
      >
        <div
          class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl"
          :style="{ backgroundColor: `${stat.color}18` }"
        >
          <Icon :icon="stat.icon" width="22" height="22" :style="{ color: stat.color }" />
        </div>
        <div class="flex min-w-0 flex-col gap-0.5">
          <span class="text-xl font-bold leading-tight text-foreground tabular-nums">{{ stat.value }}</span>
          <span class="truncate text-xs text-muted-foreground">{{ stat.label }}</span>
        </div>
      </div>
    </div>
  </WidgetCard>
</template>
