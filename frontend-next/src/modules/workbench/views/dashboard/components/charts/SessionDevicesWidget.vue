<script setup lang="ts">
import { XhPieChartRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useWidgetData } from '../widget-data'
import { COUNT_COMPACT, useFormatters } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 在线设备：当前一侧有效会话按设备类型（桌面、手机、平板、爬虫）的占比，环形、环心写会话总数。
 */
defineOptions({ name: 'SessionDevicesWidget' })

const DEVICES = ['Desktop', 'Mobile', 'Tablet', 'Bot', 'unknown'] as const

const { t, locale } = useI18n()
const formatters = useFormatters()
const { data, loading, error } = useWidgetData('identity.session-devices')

const rows = computed(() => (data.value?.slices ?? []).map(slice => ({
  name: (DEVICES as readonly string[]).includes(slice.key) ? t(`workbench.charts.device.${slice.key}`) : slice.key,
  sessions: slice.value,
})))
const total = computed(() => rows.value.reduce((sum, row) => sum + row.sessions, 0))
</script>

<template>
  <ChartWidget
    icon="lucide:monitor-smartphone"
    :title="t('workbench.widgets.session_devices.title')"
    :loading="loading"
    :error="error"
    :empty="!rows.length"
  >
    <XhPieChartRoot
      class="share-pie"
      :data="rows"
      name-field="name"
      value-field="sessions"
      variant="donut"
      labels="outside"
      label-content="share"
      :format="COUNT_COMPACT"
      :locale="locale"
      :aria-label="t('workbench.widgets.session_devices.desc')"
    >
      <template #center>
        <span class="center-value">{{ formatters.number(total) }}</span>
        <span class="center-label">{{ t('workbench.charts.sessions') }}</span>
      </template>
    </XhPieChartRoot>
  </ChartWidget>
</template>

<style scoped>
/*
 * 环心的宽度由组件库按环孔大小限定，小组件窄时环孔跟着变小：
 * 环心铺满这个限宽并作为尺寸容器，数字不折行、字号按环心宽度缩放；放不下说明文字时只留数字，数字也放不下时整块收起
 */
.share-pie :deep([data-part='center']) {
  container-type: inline-size;
  inline-size: 100%;
}

.center-value,
.center-label {
  white-space: nowrap;
}

.center-value {
  font-size: clamp(var(--xh-font-size-xs), 20cqi, var(--xh-font-size-lg));
  font-weight: var(--xh-font-weight-semibold);
}

.center-label {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-secondary-size);
}

@container (inline-size < 5rem) {
  .center-label {
    display: none;
  }
}

@container (inline-size < 3.5rem) {
  .center-value {
    display: none;
  }
}
</style>
