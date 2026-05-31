<script lang="ts" setup>
import type { UserActivity } from '~/types'
import { NEmpty, NSpin, useMessage } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabStats' })

const message = useMessage()
const { apis } = useAppContext()

const loading = ref(false)
const activity = ref<UserActivity | null>(null)

async function loadActivity() {
  loading.value = true
  try {
    activity.value = await apis.getActivityApi()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载活跃度统计失败')
  }
  finally {
    loading.value = false
  }
}

onMounted(loadActivity)

/** 在线时长（秒）→ 友好显示 */
function formatOnline(seconds: number): string {
  if (!seconds || seconds <= 0) {
    return '0 分钟'
  }
  const h = Math.floor(seconds / 3600)
  const m = Math.round((seconds % 3600) / 60)
  if (h > 0) {
    return m > 0 ? `${h} 小时 ${m} 分` : `${h} 小时`
  }
  return `${m} 分钟`
}

/** 本月四大指标卡 */
const statCards = computed(() => {
  const m = activity.value?.thisMonth
  return [
    { key: 'login', label: '本月登录', icon: 'lucide:log-in', value: m?.loginCount ?? 0, tone: 'primary' },
    { key: 'access', label: '本月访问', icon: 'lucide:eye', value: m?.accessCount ?? 0, tone: 'sky' },
    { key: 'operation', label: '本月操作', icon: 'lucide:mouse-pointer-click', value: m?.operationCount ?? 0, tone: 'primary' },
    { key: 'online', label: '本月在线', icon: 'lucide:clock', value: formatOnline(m?.onlineTime ?? 0), tone: 'amber', isText: true },
  ]
})

/** 今日 / 本周 概要 */
const periodSummary = computed(() => {
  const a = activity.value
  return [
    { key: 'today', label: '今日', period: a?.today },
    { key: 'week', label: '本周', period: a?.thisWeek },
  ]
})

/** 近 7 日趋势（按操作量取柱高，归一化到 100%） */
const trend = computed(() => activity.value?.trend ?? [])
const trendMax = computed(() => Math.max(1, ...trend.value.map(t => t.operationCount)))
const hasTrend = computed(() => trend.value.some(t => t.operationCount > 0 || t.accessCount > 0))

function barHeight(value: number): number {
  return Math.max(4, Math.round((value / trendMax.value) * 100))
}

/** 趋势日期 → 简短星期/日 */
function shortDay(date: string): string {
  const d = new Date(date)
  if (Number.isNaN(d.getTime())) {
    return date.slice(5)
  }
  return `${d.getMonth() + 1}/${d.getDate()}`
}

/** 最近活动时间行 */
const recentTimes = computed(() => {
  const a = activity.value
  return [
    { key: 'login', label: '最后登录', value: a?.lastLoginTime ? formatDate(a.lastLoginTime) : '—' },
    { key: 'access', label: '最后访问', value: a?.lastAccessTime ? formatDate(a.lastAccessTime) : '—' },
    { key: 'operation', label: '最后操作', value: a?.lastOperationTime ? formatDate(a.lastOperationTime) : '—' },
  ]
})
</script>

<template>
  <div class="pf-tab-body">
    <NSpin :show="loading && !activity">
      <!-- 本月概览卡片 -->
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
          <div class="pf-stat-card__value">
            {{ card.value }}
          </div>
          <div class="pf-stat-card__label">
            {{ card.label }}
          </div>
        </div>
      </div>

      <!-- 趋势 + 概要 -->
      <div class="pf-stat-cols">
        <!-- 近 7 日趋势 -->
        <div class="pf-panel">
          <div class="pf-panel__header">
            <Icon icon="lucide:bar-chart-3" width="16" />
            <span>近 7 日操作趋势</span>
          </div>
          <div class="pf-panel__body">
            <div v-if="hasTrend" class="pf-bars">
              <div
                v-for="point in trend"
                :key="point.date"
                class="pf-bar-col"
              >
                <div class="pf-bar-val">
                  {{ point.operationCount }}
                </div>
                <div class="pf-bar" :style="{ height: `${barHeight(point.operationCount)}%` }" />
                <div class="pf-bar-label">
                  {{ shortDay(point.date) }}
                </div>
              </div>
            </div>
            <NEmpty v-else description="暂无趋势数据" class="pf-stat-empty" />
          </div>
        </div>

        <!-- 周期概要 + 最近活动 -->
        <div class="pf-panel">
          <div class="pf-panel__header">
            <Icon icon="lucide:activity" width="16" />
            <span>活跃概要</span>
          </div>
          <div class="pf-panel__body">
            <div class="pf-period-grid">
              <div v-for="item in periodSummary" :key="item.key" class="pf-period">
                <div class="pf-period__title">
                  {{ item.label }}
                </div>
                <div class="pf-period__row">
                  <span>登录</span><b>{{ item.period?.loginCount ?? 0 }}</b>
                </div>
                <div class="pf-period__row">
                  <span>访问</span><b>{{ item.period?.accessCount ?? 0 }}</b>
                </div>
                <div class="pf-period__row">
                  <span>操作</span><b>{{ item.period?.operationCount ?? 0 }}</b>
                </div>
                <div class="pf-period__row">
                  <span>在线</span><b>{{ formatOnline(item.period?.onlineTime ?? 0) }}</b>
                </div>
              </div>
            </div>

            <div class="pf-recent">
              <div v-for="t in recentTimes" :key="t.key" class="pf-recent__row">
                <span class="pf-recent__label">{{ t.label }}</span>
                <span class="pf-recent__value">{{ t.value }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </NSpin>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
/* 概览卡片 */
.pf-stat-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
}

.pf-stat-card {
  position: relative;
  overflow: hidden;
  padding: 16px 18px;
  border-radius: var(--radius);
  background: var(--bg-card, var(--bg-surface));
  border: 1px solid var(--border-color);
}

.pf-stat-card__icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  border-radius: 9px;
  margin-bottom: 12px;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.pf-stat-card[data-tone='sky'] .pf-stat-card__icon {
  background: hsl(200 80% 45% / 12%);
  color: hsl(200 80% 42%);
}

.pf-stat-card[data-tone='amber'] .pf-stat-card__icon {
  background: hsl(35 90% 50% / 14%);
  color: hsl(35 80% 42%);
}

.pf-stat-card__value {
  font-size: 24px;
  font-weight: 700;
  line-height: 1.1;
  color: var(--text-primary);
}

.pf-stat-card__label {
  font-size: 12.5px;
  color: var(--text-secondary);
  margin-top: 5px;
}

/* 两栏 */
.pf-stat-cols {
  display: grid;
  grid-template-columns: 1.5fr 1fr;
  gap: 12px;
  margin-top: 12px;
}

.pf-panel {
  border-radius: var(--radius);
  background: var(--bg-card, var(--bg-surface));
  border: 1px solid var(--border-color);
}

.pf-panel__header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 14px 18px;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  border-bottom: 1px solid var(--border-color);
}

.pf-panel__body {
  padding: 18px;
}

/* 柱状图 */
.pf-bars {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  height: 168px;
}

.pf-bar-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-end;
  gap: 6px;
  height: 100%;
}

.pf-bar-val {
  font-size: 11px;
  font-weight: 600;
  color: var(--text-secondary);
}

.pf-bar {
  width: 100%;
  max-width: 34px;
  border-radius: 6px 6px 0 0;
  background: linear-gradient(180deg, hsl(var(--primary)), hsl(var(--primary) / 65%));
  transition: height 0.7s cubic-bezier(0.22, 1, 0.36, 1);
}

.pf-bar-label {
  font-size: 11px;
  color: var(--text-secondary);
}

.pf-stat-empty {
  padding: 40px 0;
}

/* 周期概要 */
.pf-period-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.pf-period {
  padding: 12px 14px;
  border-radius: var(--radius-sm, 8px);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
}

.pf-period__title {
  font-size: 12.5px;
  font-weight: 600;
  color: hsl(var(--primary));
  margin-bottom: 8px;
}

.pf-period__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12.5px;
  color: var(--text-secondary);
  padding: 3px 0;
}

.pf-period__row b {
  color: var(--text-primary);
  font-weight: 600;
}

/* 最近活动 */
.pf-recent {
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px solid var(--border-color);
}

.pf-recent__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 5px 0;
  font-size: 13px;
}

.pf-recent__label {
  color: var(--text-secondary);
}

.pf-recent__value {
  color: var(--text-primary);
  font-weight: 500;
}

@media (max-width: 900px) {
  .pf-stat-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .pf-stat-cols {
    grid-template-columns: 1fr;
  }
}
</style>
