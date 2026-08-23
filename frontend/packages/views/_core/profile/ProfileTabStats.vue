<script lang="ts" setup>
import type { UserActivity } from '~/types'
import { XhHeatmapRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'
/** 热力图逐日数据点：组件库按 ISO 日期串铺网格，缺的日子自动算 0 */
interface HeatmapPoint { date: string, count: number }

defineOptions({ name: 'ProfileTabStats' })

const { apis } = useAppContext()
const { t, locale } = useI18n()

const loading = ref(false)
const activity = ref<UserActivity | null>(null)

async function loadActivity() {
  loading.value = true
  try {
    activity.value = await apis.getActivityApi()
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.stats.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

onMounted(loadActivity)

/** 在线时长（秒）→ 友好显示 */
function formatOnline(seconds: number): string {
  if (!seconds || seconds <= 0) {
    return t('component.profile.stats.duration_zero')
  }
  const h = Math.floor(seconds / 3600)
  const m = Math.round((seconds % 3600) / 60)
  if (h > 0) {
    return m > 0
      ? t('component.profile.stats.duration_hours_minutes', { hours: h, minutes: m })
      : t('component.profile.stats.duration_hours', { hours: h })
  }
  return t('component.profile.stats.duration_minutes', { minutes: m })
}

/** 本月四大指标卡 */
const statCards = computed(() => {
  const m = activity.value?.thisMonth
  return [
    { key: 'login', label: t('component.profile.stats.card_login'), icon: 'lucide:log-in', value: m?.loginCount ?? 0, tone: 'brand' },
    { key: 'access', label: t('component.profile.stats.card_access'), icon: 'lucide:eye', value: m?.accessCount ?? 0, tone: 'sky' },
    { key: 'operation', label: t('component.profile.stats.card_operation'), icon: 'lucide:mouse-pointer-click', value: m?.operationCount ?? 0, tone: 'brand' },
    { key: 'online', label: t('component.profile.stats.card_online'), icon: 'lucide:clock', value: formatOnline(m?.onlineTime ?? 0), tone: 'amber', isText: true },
  ]
})

/** 今日 / 本周 概要 */
const periodSummary = computed(() => {
  const a = activity.value
  return [
    { key: 'today', label: t('component.profile.stats.period_today'), period: a?.today },
    { key: 'week', label: t('component.profile.stats.period_week'), period: a?.thisWeek },
  ]
})

/** 操作趋势原始序列，覆盖近一年 */
const trend = computed(() => activity.value?.trend ?? [])

function dateKey(d: Date): string {
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${day}`
}

/** 逐日操作量。同一天可能有多条趋势记录，按日累加 */
const countByDate = computed(() => {
  const map = new Map<string, number>()
  for (const point of trend.value) {
    const key = point.date.slice(0, 10)
    map.set(key, (map.get(key) ?? 0) + point.operationCount)
  }
  return map
})

/** 近一年的区间端点（本地零点） */
const heatRange = computed(() => {
  const end = new Date()
  end.setHours(0, 0, 0, 0)
  const start = new Date(end)
  start.setFullYear(start.getFullYear() - 1)
  start.setDate(start.getDate() + 1)
  return { start: dateKey(start), end: dateKey(end) }
})

const heatmapData = computed<HeatmapPoint[]>(() =>
  [...countByDate.value].map(([date, count]) => ({ date, count })),
)

/**
 * 五档的下界，升序。用分位而不是固定阈值，稀疏数据也能拉开层次。
 * 首档钉在 1：只要当天有操作就得着色，否则会和「这天没干活」画成同一格。
 */
const heatThresholds = computed(() => {
  const positives = [...countByDate.value.values()].filter(v => v > 0).sort((a, b) => a - b)
  if (positives.length === 0) {
    return [1, 2, 3, 4]
  }
  const at = (ratio: number) => positives[Math.min(positives.length - 1, Math.floor(positives.length * ratio))] ?? 1
  return [1, at(0.25) + 1, at(0.5) + 1, at(0.75) + 1]
})

/**
 * 一格的读数。悬停详情条与该格的可及名字共用这一句，两处才不会各说各的。
 */
function heatReadout(details: { date: string, count: number } | null): string {
  return details
    ? t('component.profile.stats.heat_cell_label', { date: details.date, count: details.count })
    : ''
}

/**
 * 热力图的文案。库的缺省是英文网格名 + 硬编码中文图例两端，两个方向都会串味，
 * 六条里除矩阵形态那条外全部给出（这里是日历形态）。
 */
const heatTranslations = computed(() => ({
  gridLabel: t('component.profile.stats.heat_grid_label'),
  cellLabel: heatReadout,
  legendLabel: t('component.profile.stats.heat_legend_label'),
  legendLow: t('component.profile.stats.heat_legend_low'),
  legendHigh: t('component.profile.stats.heat_legend_high'),
}))

/** 热力图底部摘要 */
const heatActiveDays = computed(() => trend.value.filter(t => t.operationCount > 0).length)
const heatTotalOps = computed(() => trend.value.reduce((sum, t) => sum + t.operationCount, 0))

/** 最近活动时间行 */
const recentTimes = computed(() => {
  const a = activity.value
  return [
    { key: 'login', label: t('component.profile.stats.last_login'), value: a?.lastLoginTime ? formatDate(a.lastLoginTime) : '—' },
    { key: 'access', label: t('component.profile.stats.last_access'), value: a?.lastAccessTime ? formatDate(a.lastAccessTime) : '—' },
    { key: 'operation', label: t('component.profile.stats.last_operation'), value: a?.lastOperationTime ? formatDate(a.lastOperationTime) : '—' },
  ]
})
</script>

<template>
  <div class="pf-tab-body">
    <div class="xh-loading-stage">
      <div v-if="loading && !activity" class="xh-loading-stage__veil">
        <XhSpinner />
      </div>
      <!-- 本月概览 -->
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              <Icon icon="lucide:calendar-range" width="16" />
              <span>{{ t('component.profile.stats.section_month_overview') }}</span>
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.stats.section_month_overview_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-stat-grid">
            <div
              v-for="card in statCards"
              :key="card.key"
              class="pf-stat-card"
              :data-tone="card.tone"
            >
              <span class="pf-stat-card__icon">
                <Icon :icon="card.icon" width="18" />
              </span>
              <div class="pf-stat-card__body">
                <div class="pf-stat-card__value">
                  {{ card.value }}
                </div>
                <div class="pf-stat-card__label">
                  {{ card.label }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- 操作趋势热力图 -->
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              <Icon icon="lucide:activity" width="16" />
              <span>{{ t('component.profile.stats.section_trend') }}</span>
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.stats.section_trend_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-heat">
            <!-- 横向自滚与聚焦环留位都在组件库的根规则里，外面不再套滚动容器 -->
            <XhHeatmapRoot
              :value="heatmapData"
              :start-date="heatRange.start"
              :end-date="heatRange.end"
              :thresholds="heatThresholds"
              :first-day-of-week="0"
              :locale="locale"
              :translations="heatTranslations"
            >
              <!-- 写了这个插槽才铺出详情条；同一句也是该格的可及名字 -->
              <template #tooltip="details">
                {{ heatReadout(details) }}
              </template>
            </XhHeatmapRoot>
            <div class="pf-heat__foot">
              {{ t('component.profile.stats.heat_foot', { ops: heatTotalOps, days: heatActiveDays }) }}
            </div>
          </div>
        </div>
      </section>

      <!-- 活跃概要 -->
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              <Icon icon="lucide:gauge" width="16" />
              <span>{{ t('component.profile.stats.section_activity') }}</span>
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.stats.section_activity_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-summary-grid">
            <div v-for="item in periodSummary" :key="item.key" class="pf-period">
              <div class="pf-period__title">
                {{ item.label }}
              </div>
              <div class="pf-period__row">
                <span>{{ t('component.profile.stats.stat_login') }}</span><b>{{ item.period?.loginCount ?? 0 }}</b>
              </div>
              <div class="pf-period__row">
                <span>{{ t('component.profile.stats.stat_access') }}</span><b>{{ item.period?.accessCount ?? 0 }}</b>
              </div>
              <div class="pf-period__row">
                <span>{{ t('component.profile.stats.stat_operation') }}</span><b>{{ item.period?.operationCount ?? 0 }}</b>
              </div>
              <div class="pf-period__row">
                <span>{{ t('component.profile.stats.stat_online') }}</span><b>{{ formatOnline(item.period?.onlineTime ?? 0) }}</b>
              </div>
            </div>
            <div class="pf-period">
              <div class="pf-period__title">
                {{ t('component.profile.stats.recent_activity') }}
              </div>
              <div v-for="row in recentTimes" :key="row.key" class="pf-period__row">
                <span>{{ row.label }}</span><b>{{ row.value }}</b>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
/* 概览卡片网格：一行四列自适应、紧凑横向卡片（图标左、数值+标签右） */
.pf-stat-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 10px;
}

.pf-stat-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  border: 1px solid hsl(var(--border) / 70%);
  border-radius: var(--radius);
  background: transparent;
}

.pf-stat-card__icon {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 9px;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.pf-stat-card__body {
  min-width: 0;
}

.pf-stat-card__value {
  font-size: 18px;
  font-weight: 700;
  line-height: 1.2;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
}

.pf-stat-card__label {
  font-size: 12.5px;
  color: var(--text-secondary);
  margin-top: 2px;
}

/* 操作趋势热力图容器 */
.pf-heat {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.pf-heat__foot {
  font-size: 12.5px;
  color: var(--text-secondary);
}

/* 活跃概要（今日 / 本周 / 最近活动，按宽度自适应列数） */
.pf-summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 12px;
}

/* 周期概要 */
.pf-period {
  padding: 12px 14px;
  border: 1px solid hsl(var(--border) / 70%);
  border-radius: var(--radius-sm, 8px);
  background: transparent;
}

.pf-period__title {
  font-size: 12.5px;
  font-weight: 600;
  color: hsl(var(--primary));
  margin-bottom: 6px;
}

.pf-period__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  font-size: 12.5px;
  color: var(--text-secondary);
  padding: 3px 0;
}

.pf-period__row b {
  font-weight: 600;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.pf-period__row b {
  color: var(--text-primary);
  font-weight: 600;
}

@media (max-width: 768px) {
  .pf-stat-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .pf-summary-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .pf-stat-grid {
    grid-template-columns: 1fr;
  }
}
</style>
