<script setup lang="ts">
import type { ChartRow } from '@xihan-ui/headless'
import { XhHierarchyChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { CURRENCY_COMPACT, useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { CATEGORIES, itemsOf, products } from './demo-data'

/**
 * 品类构成：品类 → 子类的销售额层级，可在矩形树、旭日、冰柱与圆堆积四种画法间切换，点块下钻、点根返回。
 */
defineOptions({ name: 'CategoryMixWidget' })

type Layout = 'treemap' | 'sunburst' | 'icicle' | 'pack'

const { t, locale } = useI18n()
const label = useDemoLabels()

const layout = ref<Layout>('treemap')
const layoutOptions = computed(() => [
  { value: 'treemap' as const, label: t('workbench.charts.layout_treemap') },
  { value: 'sunburst' as const, label: t('workbench.charts.layout_sunburst') },
  { value: 'icicle' as const, label: t('workbench.charts.layout_icicle') },
  { value: 'pack' as const, label: t('workbench.charts.layout_pack') },
])

const productRows = products(new Date())

const tree = computed<ChartRow>(() => ({
  name: t('workbench.charts.all_categories'),
  children: CATEGORIES.map(category => ({
    name: label.category(category),
    children: itemsOf(category).map(item => ({
      name: label.item(item),
      value: productRows.filter(row => row.item === item).reduce((sum, row) => sum + row.sales, 0),
    })),
  })),
}))
</script>

<template>
  <ChartWidget icon="lucide:layout-grid" :title="t('workbench.widgets.category_mix.title')" size="lg">
    <template #extra>
      <XSegmented v-model:value="layout" :options="layoutOptions" />
    </template>
    <XhHierarchyChartRoot
      :data="tree"
      :layout="layout"
      :format="CURRENCY_COMPACT"
      :locale="locale"
      :aria-label="t('workbench.widgets.category_mix.desc')"
    />
  </ChartWidget>
</template>
