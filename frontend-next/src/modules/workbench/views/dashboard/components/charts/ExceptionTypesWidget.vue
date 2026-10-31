<script setup lang="ts">
import { XhPieChartRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useWidgetData } from '../widget-data'
import { COUNT_COMPACT } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 异常类型：近 30 天异常按异常类名的占比，只列最多的几种，其余并成「其他」；类名只写最后一段，完整名在提示里。
 */
defineOptions({ name: 'ExceptionTypesWidget' })

const DAYS = 30

const { t, locale } = useI18n()
const { data, loading, error } = useWidgetData('auditing.exception-types', DAYS)

const rows = computed(() => (data.value?.slices ?? []).map(slice => ({
  name: slice.key === 'others' ? t('workbench.charts.others') : (slice.key.split('.').at(-1) || slice.key),
  count: slice.value,
})))
</script>

<template>
  <ChartWidget
    icon="lucide:bug"
    :title="t('workbench.widgets.exception_types.title')"
    :loading="loading"
    :error="error"
    :empty="!rows.length"
  >
    <XhPieChartRoot
      :data="rows"
      name-field="name"
      value-field="count"
      variant="pie"
      labels="inside"
      label-content="share"
      :format="COUNT_COMPACT"
      :locale="locale"
      :aria-label="t('workbench.widgets.exception_types.desc')"
    />
  </ChartWidget>
</template>
