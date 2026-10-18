<script setup lang="ts">
import type { FlowDocument, FlowNodeStatus, XhFlowViewer } from '~/designers/flow'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createXihanWorkflow, ensureFlowViewer, xihanWorkflowTexts } from '~/designers/flow'
import { openDefinition } from './definition-codec'

/**
 * 工作流只读图查看器：XiHan 流程查看器（<xh-flow-viewer>）装上工作流引擎预设
 *
 * 用途：定义只读预览、实例运行轨迹（statuses 给节点运行态着色）。只能平移、缩放与点选，不能编辑。
 */

const props = defineProps<{
  definitionJson: string
  /** 节点运行态（nodeId → 状态），用于实例轨迹着色 */
  statuses?: Record<string, FlowNodeStatus | null>
}>()

const { t, locale } = useI18n()

ensureFlowViewer()

const viewerRef = ref<XhFlowViewer | null>(null)

const extensions = computed(() => [createXihanWorkflow({ texts: xihanWorkflowTexts(locale.value) })])

const document = computed<FlowDocument | null>(() => {
  try {
    return openDefinition(props.definitionJson)
  }
  catch {
    return null
  }
})

watch([viewerRef, extensions, document], ([viewer, list, schema]) => {
  if (!viewer || !schema)
    return
  viewer.extensions = list
  viewer.schema = schema
}, { immediate: true })

watch([viewerRef, () => props.statuses], ([viewer, statuses]) => {
  if (viewer)
    viewer.statuses = statuses ?? {}
}, { immediate: true, deep: true })
</script>

<template>
  <div class="relative h-full w-full">
    <xh-flow-viewer v-if="document" ref="viewerRef" class="workflow-graph-view" />
    <div v-else class="absolute inset-0 flex items-center justify-center text-sm text-gray-400">
      {{ t('workflow.designer.err_parse') }}
    </div>
  </div>
</template>

<style scoped>
/* 查看器铺满自身，宿主给它定宽高 */
.workflow-graph-view {
  display: block;
  inline-size: 100%;
  block-size: 100%;
}
</style>
