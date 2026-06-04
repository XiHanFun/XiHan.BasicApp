<script lang="ts" setup>
import type { HeatmapData } from 'naive-ui/es/heatmap'
import type { UserActivity } from '~/types'
import { NSpin, useMessage } from 'naive-ui'
// naive-ui 2.44.1 主入口尚未导出 Heatmap，子模块直引（无 exports 限制，可正常解析）
import { NHeatmap } from 'naive-ui/es/heatmap'
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

/** 操作趋势：转换为 naive-ui Heatmap 的 { timestamp(ms), value } 数据，覆盖近一年连续日序列 */
const trend = computed(() => activity.value?.trend ?? [])

function dateKey(d: Date): string {
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${day}`
}

const heatmapData = computed<HeatmapData>(() => {
  const countByDate = new Map<string, number>()
  for (const point of trend.value) {
    const key = point.date.slice(0, 10)
    countByDate.set(key, (countByDate.get(key) ?? 0) + point.operationCount)
  }

  // 近一年连续日序列（本地零点），值取自真实操作量，无记录的日子为 0
  const end = new Date()
  end.setHours(0, 0, 0, 0)
  const start = new Date(end)
  start.setFullYear(start.getFullYear() - 1)
  start.setDate(start.getDate() + 1)

  const data: HeatmapData = []
  const cursor = new Date(start)
  while (cursor.getTime() <= end.getTime()) {
    data.push({ timestamp: cursor.getTime(), value: countByDate.get(dateKey(cursor)) ?? 0 })
    cursor.setDate(cursor.getDate() + 1)
  }
  return data
})

/** 热力图底部摘要 */
const heatActiveDays = computed(() => trend.value.filter(t => t.operationCount > 0).length)
const heatTotalOps = computed(() => trend.value.reduce((sum, t) => sum + t.operationCount, 0))

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
      <!-- 本月概览 -->
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              <Icon icon="lucide:calendar-range" width="16" />
              <span>本月概览</span>
            </div>
            <div class="pf-section__desc">
              本月累计的登录、访问、操作次数与在线时长。
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
              <div class="pf-stat-card__value">
                {{ card.value }}
              </div>
              <div class="pf-stat-card__label">
                {{ card.label }}
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
              <span>操作趋势</span>
            </div>
            <div class="pf-section__desc">
              近一年每日操作活跃度，颜色越深表示当日操作越多。
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-heat">
            <div class="pf-heat__scroll">
              <NHeatmap
                :data="heatmapData"
                :first-day-of-week="0"
                size="small"
                :show-week-labels="true"
                :show-month-labels="true"
                :show-color-indicator="true"
                :tooltip="true"
                :fill-calendar-leading="true"
              />
            </div>
            <div class="pf-heat__foot">
              近一年共 {{ heatTotalOps }} 次操作 · {{ heatActiveDays }} 天活跃
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
              <span>活跃概要</span>
            </div>
            <div class="pf-section__desc">
              今日 / 本周活跃数据与最近活动时间。
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
            <div class="pf-period">
              <div class="pf-period__title">
                最近活动
              </div>
              <div v-for="t in recentTimes" :key="t.key" class="pf-period__row pf-period__row--stack">
                <span>{{ t.label }}</span><b>{{ t.value }}</b>
              </div>
            </div>
          </div>
        </div>
      </section>
    </NSpin>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
/* 概览卡片网格 */
.pf-stat-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
}

.pf-stat-card {
  position: relative;
  overflow: hidden;
  padding: 14px 16px;
  border-radius: var(--radius);
  background: hsl(var(--muted) / 28%);
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
  background: hsl(var(--primary) / 12%);
  color: hsl(var(--primary));
}

.pf-stat-card[data-tone='amber'] .pf-stat-card__icon {
  background: hsl(var(--primary) / 14%);
  color: hsl(var(--primary));
}

.pf-stat-card__value {
  font-size: 22px;
  font-weight: 700;
  line-height: 1.1;
  color: var(--text-primary);
}

.pf-stat-card__label {
  font-size: 12.5px;
  color: var(--text-secondary);
  margin-top: 5px;
}

/* 操作趋势热力图（naive-ui Heatmap 容器） */
.pf-heat {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.pf-heat__scroll {
  overflow-x: auto;
  padding-bottom: 4px;
}

.pf-heat__foot {
  font-size: 12.5px;
  color: var(--text-secondary);
}

/* 活跃概要三列（今日 / 本周 / 最近活动） */
.pf-summary-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}

.pf-period__row--stack {
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
}

.pf-period__row--stack b {
  font-size: 12px;
  font-weight: 500;
  color: var(--text-primary);
}

/* 周期概要 */
.pf-period {
  padding: 12px 14px;
  border-radius: var(--radius-sm, 8px);
  background: hsl(var(--muted) / 28%);
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
