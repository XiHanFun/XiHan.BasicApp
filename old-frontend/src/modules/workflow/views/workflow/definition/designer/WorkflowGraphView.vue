<script setup lang="ts">
import type { DiagramApi, DiagramNodeStatus } from '~/diagram'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { registerVueShape, XDiagram } from '~/diagram'
import ActivityNode from './ActivityNode.vue'
import { ACTIVITY_SHAPE, parseDefinition } from './transform'

/**
 * 工作流只读图查看器（复用设计器的活动节点与解析逻辑）
 *
 * 用途：定义只读预览、实例运行轨迹（叠加 statuses 节点状态着色）。
 * 只读画布：禁用移动/连线/删除，仅缩放/平移/小地图。
 */

const props = defineProps<{
  definitionJson: string
  /** 节点运行态覆盖（nodeId → 状态），用于实例轨迹着色 */
  statuses?: Record<string, DiagramNodeStatus | null>
}>()

const { t } = useI18n()

// 幂等注册工作流活动形状（与设计器共用同一 shape）
registerVueShape({ shape: ACTIVITY_SHAPE, component: ActivityNode, width: 172, height: 64 })

let api: DiagramApi | null = null
const parseError = ref(false)

function applyStatuses() {
  if (api && props.statuses)
    api.setNodeStatuses(props.statuses)
}

function render() {
  if (!api)
    return
  try {
    api.load(parseDefinition(props.definitionJson).data)
    applyStatuses()
    parseError.value = false
  }
  catch {
    parseError.value = true
  }
}

function onReady(readyApi: DiagramApi) {
  api = readyApi
  render()
}

watch(() => props.definitionJson, render)
watch(() => props.statuses, applyStatuses, { deep: true })
</script>

<template>
  <div class="relative h-full w-full">
    <XDiagram class="h-full" :options="{ readonly: true, minimap: true }" @ready="onReady" />
    <div v-if="parseError" class="absolute inset-0 flex items-center justify-center text-sm text-gray-400">
      {{ t('workflow.designer.err_parse') }}
    </div>
  </div>
</template>
