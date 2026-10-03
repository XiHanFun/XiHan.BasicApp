<script setup lang="ts">
import type { ProgressThreshold } from '@xihan-ui/headless'
import { XhProgress } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormatters } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { targets } from './demo-data'

/**
 * 目标达成：进度组件的几种形态。
 * - 月度销售：指针仪表，量程到 120%，100% 处画目标刻度，落后 / 追赶 / 达标三段色带；
 * - 毛利率：填充仪表，量程 0–50%，30% 处画目标刻度；
 * - 好评率：环形；
 * - 季度 OKR 按关键结果分格，大促备货带流动条纹，年度预算用缓冲段画出已批复未执行的部分。
 */
defineOptions({ name: 'TargetProgressWidget' })

const { t } = useI18n()
const formatters = useFormatters()
const goal = targets(new Date())

const salesZones = computed<ProgressThreshold[]>(() => [
  { value: 80, tone: 'danger', label: t('workbench.charts.zone_behind') },
  { value: 100, tone: 'warning', label: t('workbench.charts.zone_catching') },
  { value: 120, tone: 'success', label: t('workbench.charts.zone_reached') },
])

const marginZones = computed<ProgressThreshold[]>(() => [
  { value: 20, tone: 'danger', label: t('workbench.charts.zone_low') },
  { value: 30, tone: 'warning', label: t('workbench.charts.zone_normal') },
  { value: 50, tone: 'success', label: t('workbench.charts.zone_good') },
])

const percent = (value: number) => formatters.value.percent(value / 100)
</script>

<template>
  <ChartWidget icon="lucide:target" :title="t('workbench.widgets.target_progress.title')">
    <div class="target-layout">
      <div class="gauge-grid">
        <div class="gauge">
          <XhProgress
            variant="dashboard"
            semantics="meter"
            :value="goal.salesRate"
            :max="120"
            :target="100"
            :thresholds="salesZones"
            indicator="needle"
            scale
            :aria-label="t('workbench.charts.goal_sales')"
          >
            <strong class="gauge-value">{{ percent(goal.salesRate) }}</strong>
          </XhProgress>
          <span class="gauge-label">{{ t('workbench.charts.goal_sales') }}</span>
          <span class="gauge-detail">{{ t('workbench.charts.goal_target', { value: formatters.currency(goal.monthTarget) }) }}</span>
        </div>
        <div class="gauge">
          <XhProgress
            variant="dashboard"
            semantics="meter"
            :value="goal.margin"
            :max="50"
            :target="goal.marginTarget"
            :thresholds="marginZones"
            scale
            :aria-label="t('workbench.charts.goal_margin')"
          >
            <strong class="gauge-value">{{ percent(goal.margin) }}</strong>
          </XhProgress>
          <span class="gauge-label">{{ t('workbench.charts.goal_margin') }}</span>
          <span class="gauge-detail">{{ t('workbench.charts.goal_target', { value: percent(goal.marginTarget) }) }}</span>
        </div>
        <div class="gauge">
          <XhProgress variant="circle" :value="goal.rating" :aria-label="t('workbench.charts.goal_rating')">
            <strong class="gauge-value">{{ percent(goal.rating) }}</strong>
          </XhProgress>
          <span class="gauge-label">{{ t('workbench.charts.goal_rating') }}</span>
          <span class="gauge-detail">{{ t('workbench.charts.goal_reviews', { n: formatters.number(goal.reviews) }) }}</span>
        </div>
      </div>

      <div class="line-list">
        <div class="line-item">
          <div class="line-head">
            <span class="gauge-label">{{ t('workbench.charts.goal_okr') }}</span>
            <span class="gauge-detail">{{ t('workbench.charts.goal_okr_detail', { done: goal.okrDone, total: goal.okrTotal }) }}</span>
          </div>
          <XhProgress
            :value="goal.okrDone"
            :max="goal.okrTotal"
            :steps="goal.okrTotal"
            :value-text="t('workbench.charts.goal_okr_detail', { done: goal.okrDone, total: goal.okrTotal })"
            :aria-label="t('workbench.charts.goal_okr')"
          />
        </div>
        <div class="line-item">
          <div class="line-head">
            <span class="gauge-label">{{ t('workbench.charts.goal_stocking') }}</span>
            <span class="gauge-detail">{{ percent(goal.stocking) }}</span>
          </div>
          <XhProgress :value="goal.stocking" striped :aria-label="t('workbench.charts.goal_stocking')" />
        </div>
        <div class="line-item">
          <div class="line-head">
            <span class="gauge-label">{{ t('workbench.charts.goal_budget') }}</span>
            <span class="gauge-detail">
              {{ t('workbench.charts.goal_budget_detail', { used: percent(goal.budgetUsed), approved: percent(goal.budgetApproved) }) }}
            </span>
          </div>
          <XhProgress :value="goal.budgetUsed" :buffer="goal.budgetApproved" :aria-label="t('workbench.charts.goal_budget')" />
        </div>
      </div>
    </div>
  </ChartWidget>
</template>

<style scoped>
/* 宽时仪表与进度条左右并排，窄时上下排 */
.target-layout {
  display: grid;
  gap: var(--xh-space-5);
}

@container (min-width: 48rem) {
  .target-layout {
    grid-template-columns: minmax(0, 3fr) minmax(0, 2fr);
    align-items: center;
  }
}

.gauge-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(8rem, 1fr));
  gap: var(--xh-space-4);
  justify-items: center;
}

.gauge {
  display: grid;
  justify-items: center;
  gap: var(--xh-space-1);
  min-inline-size: 0;
}

.gauge-value {
  font-size: var(--xh-font-size-lg);
  font-weight: var(--xh-font-weight-semibold);
}

.gauge-label {
  font-weight: var(--xh-font-weight-medium);
}

.gauge-detail {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-secondary-size);
  white-space: nowrap;
}

.line-list {
  display: grid;
  gap: var(--xh-space-4);
}

.line-item {
  display: grid;
  gap: var(--xh-space-2);
}

.line-head {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: var(--xh-space-2);
}
</style>
