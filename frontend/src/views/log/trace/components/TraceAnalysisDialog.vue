<script setup lang="ts">
import type { TraceResult } from '../trace-analysis'
import type { TraceLogType, TraceTimelineItemDto } from '@/api'
import { XhCartesianChartRoot, XhDialogCloseTrigger, XhDialogContent, XhDialogDescription, XhDialogRoot, XhDialogTitle, XhSankeyChartRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { buildTraceFlow, buildTraceTimeBuckets, TRACE_RESULTS } from '../trace-analysis'

/**
 * 链路分析：同一批追踪结果从两个方向看。
 * 流向（桑基图）：日志类型 → 模块 → 结果，看流量去了哪里、失败出在哪一段；
 * 时间分布（堆叠柱）：按时间桶数条数，结果分色，看什么时候密集、什么时候开始出错。
 */
defineOptions({ name: 'TraceAnalysisDialog' })

const props = defineProps<{
  show: boolean
  items: readonly TraceTimelineItemDto[]
  truncated: boolean
  logTypeLabel: (type: TraceLogType) => string
  resultLabel: (result: TraceResult) => string
}>()

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
}>()

const { t } = useI18n()

/** 模块列最多留几个节点，其余并成「其他模块」 */
const MAX_MODULES = 10

/** 结果的语气色：同一张图只用语气色，不混分类色 */
const RESULT_TONES: Record<TraceResult, 'success' | 'info' | 'warning' | 'danger'> = {
  success: 'success',
  info: 'info',
  warning: 'warning',
  error: 'danger',
}

const flow = computed(() => buildTraceFlow(props.items, {
  logType: props.logTypeLabel,
  result: props.resultLabel,
  otherModules: t('log.trace.analysis_other_modules'),
  groupLogType: t('log.trace.analysis_group_log_type'),
  groupModule: t('log.trace.analysis_group_module'),
  groupResult: t('log.trace.analysis_group_result'),
}, MAX_MODULES))

const timeline = computed(() => buildTraceTimeBuckets(props.items))

/** 只画出现过的结果：全是成功时图例里不必挂三个空系列 */
const timeSeries = computed(() => TRACE_RESULTS
  .filter(result => timeline.value.buckets.some(bucket => bucket[result] > 0))
  .map(result => ({
    mark: 'bar' as const,
    x: ['start', 'end'] as const,
    y: result,
    name: props.resultLabel(result),
    tone: RESULT_TONES[result],
    stack: 'result',
  })))

/** 跨天时刻度带上月日，否则只写时分 */
const timeAxis = computed(() => {
  const buckets = timeline.value.buckets
  const crossesDays = buckets.length > 0 && buckets[0]!.start.toDateString() !== buckets[buckets.length - 1]!.start.toDateString()
  const format: Intl.DateTimeFormatOptions = crossesDays
    ? { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }
    : { hour: '2-digit', minute: '2-digit' }
  return { format }
})

const bucketText = computed(() => {
  const minutes = timeline.value.bucketMinutes
  if (minutes % 1440 === 0) {
    return t('log.trace.analysis_bucket_days', { n: minutes / 1440 })
  }
  if (minutes % 60 === 0) {
    return t('log.trace.analysis_bucket_hours', { n: minutes / 60 })
  }
  return t('log.trace.analysis_bucket_minutes', { n: minutes })
})
</script>

<template>
  <XhDialogRoot :open="show" @update:open="(open: boolean) => emit('update:show', open)">
    <!-- 宽度写在内容部件上：它随定位层渲染，页面的作用域样式够不到 -->
    <XhDialogContent style="--xh-dialog-max-w: min(96vw, 72rem)">
      <XhDialogTitle>{{ t('log.trace.analysis_title') }}</XhDialogTitle>
      <XhDialogDescription>
        {{ t('log.trace.analysis_basis', { total: items.length }) }}
        <template v-if="truncated">
          {{ t('log.trace.truncated') }}
        </template>
      </XhDialogDescription>
      <XhDialogCloseTrigger />

      <div class="trace-analysis__body">
        <section class="trace-analysis__section">
          <XhSankeyChartRoot
            class="trace-analysis__flow"
            :nodes="flow.nodes"
            :links="flow.links"
            node-sort="input"
          >
            <template #caption>
              {{ t('log.trace.analysis_flow_caption') }}
            </template>
          </XhSankeyChartRoot>
          <p class="trace-analysis__hint">
            {{ t('log.trace.analysis_flow_hint') }}
          </p>
        </section>

        <section class="trace-analysis__section">
          <XhCartesianChartRoot
            class="trace-analysis__time"
            :data="timeline.buckets"
            :series="timeSeries"
            :x-axis="timeAxis"
          >
            <template #caption>
              {{ t('log.trace.analysis_time_caption', { bucket: bucketText }) }}
            </template>
          </XhCartesianChartRoot>
        </section>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.trace-analysis__body {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-5);
}

.trace-analysis__section {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-inline-size: 0;
}

/* 流向图节点多（类型、十来个模块、结果三列），比缺省图高一些，名字才排得开 */
.trace-analysis__flow {
  --xh-sankey-chart-height: calc(var(--xh-chart-height) * 1.3);
}

.trace-analysis__time {
  --xh-cartesian-chart-height: calc(var(--xh-chart-height) * 0.8);
}

.trace-analysis__hint {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}
</style>
