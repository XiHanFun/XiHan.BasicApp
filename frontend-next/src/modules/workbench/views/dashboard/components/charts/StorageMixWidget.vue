<script setup lang="ts">
import type { ChartRow } from '@xihan-ui/headless'
import { XhHierarchyChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useWidgetData } from '../widget-data'
import { byteScale } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'

/**
 * 存储占用：文件类型 → 扩展名的字节数层级（临时文件不算），可在矩形树、旭日、冰柱与圆堆积四种画法间切换，点块下钻、点根返回。
 * 后端给「类型/扩展名」的构成，这里拆成两层；单位按最大的一块挑（KB、MB 或 GB）。
 */
defineOptions({ name: 'StorageMixWidget' })

type Layout = 'treemap' | 'sunburst' | 'icicle' | 'pack'

const FILE_KINDS = ['Image', 'Document', 'Video', 'Audio', 'Archive', 'Other'] as const

const { t, locale } = useI18n()
const layout = ref<Layout>('treemap')
const layoutOptions = computed(() => [
  { value: 'treemap' as const, label: t('workbench.charts.layout_treemap') },
  { value: 'sunburst' as const, label: t('workbench.charts.layout_sunburst') },
  { value: 'icicle' as const, label: t('workbench.charts.layout_icicle') },
  { value: 'pack' as const, label: t('workbench.charts.layout_pack') },
])
const { data, loading, error } = useWidgetData('files.storage-mix')

const slices = computed(() => data.value?.slices ?? [])
const scale = computed(() => byteScale(Math.max(0, ...slices.value.map(slice => slice.value))))

const tree = computed<ChartRow>(() => {
  const kinds = new Map<string, ChartRow[]>()
  for (const slice of slices.value) {
    const [kind = 'Other', extension = ''] = slice.key.split('/')
    const children = kinds.get(kind) ?? []
    children.push({ name: extension ? `.${extension}` : t('workbench.charts.no_extension'), value: slice.value / scale.value.divisor })
    kinds.set(kind, children)
  }
  return {
    name: t('workbench.charts.all_files'),
    children: [...kinds].map(([kind, children]) => ({
      name: (FILE_KINDS as readonly string[]).includes(kind) ? t(`workbench.charts.file_kind.${kind}`) : kind,
      children,
    })),
  }
})
</script>

<template>
  <ChartWidget
    icon="lucide:hard-drive"
    :title="t('workbench.widgets.storage_mix.title')"
    size="lg"
    :loading="loading"
    :error="error"
    :empty="!slices.length"
  >
    <template #extra>
      <XSegmented v-model:value="layout" :options="layoutOptions" />
    </template>
    <XhHierarchyChartRoot
      :data="tree"
      :layout="layout"
      :format="scale.format"
      :locale="locale"
      :aria-label="t('workbench.widgets.storage_mix.desc')"
    />
  </ChartWidget>
</template>
