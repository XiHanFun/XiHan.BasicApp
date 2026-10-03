<script setup lang="ts">
import type { SupplyNode, SupplyNodeKind } from './demo-data'
import { XhGraphChartRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XSegmented } from '~/components'
import { useDemoLabels } from './chart-helpers'
import ChartWidget from './ChartWidget.vue'
import { supplyNetwork } from './demo-data'

/**
 * 供应网络：供应商 → 中央仓 → 区域仓 → 门店的供货关系（带箭头），外加供应商直供区域仓、区域仓跨区补货的线路。
 * 节点面积是吞吐量、连线粗细是供货量，按节点类型分组着色；可在力导、环形、树与径向树四种布局间切换，
 * 树与径向树只画以中央仓为根的配送树（中央仓 → 区域仓 → 本区门店）。节点可拖动，画布可缩放。
 */
defineOptions({ name: 'SupplyNetworkWidget' })

type Layout = 'force' | 'circular' | 'tree' | 'radial-tree'

const { t, locale } = useI18n()
const label = useDemoLabels()

const layout = ref<Layout>('force')
const layoutOptions = computed(() => [
  { value: 'force' as const, label: t('workbench.charts.layout_force') },
  { value: 'circular' as const, label: t('workbench.charts.layout_circular') },
  { value: 'tree' as const, label: t('workbench.charts.layout_tree') },
  { value: 'radial-tree' as const, label: t('workbench.charts.layout_radial') },
])

const network = supplyNetwork(new Date())

const GROUP_KEYS: Record<SupplyNodeKind, string> = {
  supplier: 'workbench.charts.group_supplier',
  hub: 'workbench.charts.group_hub',
  warehouse: 'workbench.charts.group_warehouse',
  store: 'workbench.charts.group_store',
}

function nodeName(node: SupplyNode): string {
  switch (node.kind) {
    case 'supplier':
      return t('workbench.charts.supplier', { n: node.key })
    case 'hub':
      return t('workbench.charts.hub')
    case 'warehouse':
      return t('workbench.charts.warehouse', { region: label.region(node.key) })
    case 'store': {
      const [region, index] = node.key.split('-')
      return t('workbench.charts.store_node', { region: label.region(region!), n: index })
    }
  }
}

// 树形布局要求每个节点只有一个上级：只画配送树，供应商、直供与跨区补货留给力导和环形
const treeLayout = computed(() => layout.value === 'tree' || layout.value === 'radial-tree')

const nodes = computed(() => network.nodes
  .filter(node => !treeLayout.value || node.kind !== 'supplier')
  .map(node => ({
    id: node.id,
    name: nodeName(node),
    group: t(GROUP_KEYS[node.kind]),
    value: node.value,
  })))

const links = computed(() => network.links
  .filter(link => !treeLayout.value || link.tree)
  .map(({ source, target, value }) => ({ source, target, value })))
</script>

<template>
  <ChartWidget icon="lucide:share-2" :title="t('workbench.widgets.supply_network.title')" size="lg">
    <template #extra>
      <XSegmented v-model:value="layout" :options="layoutOptions" />
    </template>
    <XhGraphChartRoot
      :key="treeLayout ? 'tree' : 'network'"
      :nodes="nodes"
      :links="links"
      :layout="layout"
      :root="treeLayout ? 'hub' : undefined"
      directed
      draggable-nodes
      zoom
      :locale="locale"
      :aria-label="t('workbench.widgets.supply_network.desc')"
    />
  </ChartWidget>
</template>
