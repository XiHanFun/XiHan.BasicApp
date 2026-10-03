<script setup lang="ts">
import { XhRadarChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { SCORES, storeScores } from './demo-data'

/**
 * 门店对比：旗舰店、社区店与奥莱店的六项经营评分（0–100，共用一把尺）叠在一张雷达上，比的是长板与短板的形状。
 * 网格可在多边形与圆形之间切换，评分线用平滑曲线连。
 */
defineOptions({ name: 'StoreRadarWidget' })

type Shape = 'polygon' | 'circle'

const { t, locale } = useI18n()
const label = useDemoLabels()

const shape = ref<Shape>('polygon')
const shapeOptions = computed(() => [
  { value: 'polygon' as const, label: t('workbench.charts.shape_polygon') },
  { value: 'circle' as const, label: t('workbench.charts.shape_circle') },
])

const scores = storeScores(new Date())
const indicators = computed(() => SCORES.map(score => ({ key: score, label: label.score(score), min: 0, max: 100 })))
const rows = computed(() => scores.map(({ store, ...values }) => ({ name: label.store(store), ...values })))
</script>

<template>
  <ChartWidget icon="lucide:radar" :title="t('workbench.widgets.store_radar.title')">
    <template #extra>
      <XSegmented v-model:value="shape" :options="shapeOptions" />
    </template>
    <XhRadarChartRoot
      :data="rows"
      name-field="name"
      :indicators="indicators"
      :shape="shape"
      curve="catmull-rom"
      scale="shared"
      :rings="4"
      ring-labels
      area
      :locale="locale"
      :aria-label="t('workbench.widgets.store_radar.desc')"
    />
  </ChartWidget>
</template>
