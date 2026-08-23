<script setup lang="ts">
import type { DesignerNodeData } from './transform'
import type { DiagramNodeStatus } from '~/diagram'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/components'
import { useDiagramNode } from '~/diagram'
import { ACTIVITY_MAP } from './catalog'

/**
 * 工作流活动节点（经 ~/diagram 的 registerVueShape 注册的 Vue 形状）
 *
 * 数据来自 useDiagramNode（updateNodeData 全量替换后自动重渲染），零图引擎依赖。
 * 额外呈现两类运行态覆盖：__status（实例轨迹着色）、__highlight（校验错误红环）。
 */

defineOptions({ name: 'WorkflowActivityNode' })

const { t } = useI18n()

const { id, data } = useDiagramNode<DesignerNodeData>({ activityType: '', name: '', properties: {} })

const meta = computed(() => ACTIVITY_MAP[data.value.activityType])
const typeLabel = computed(() => meta.value ? t(`workflow.designer.activity.${meta.value.labelKey}`) : data.value.activityType)
const icon = computed(() => meta.value?.icon ?? 'lucide:box')

/** 运行态 → 边框/圆点样式（脉冲表示"进行中/等待中"） */
const STATUS_STYLE: Record<DiagramNodeStatus, { border: string, dot: string, pulse: boolean }> = {
  running: { border: 'border-blue-500', dot: 'bg-blue-500', pulse: true },
  completed: { border: 'border-green-500', dot: 'bg-green-500', pulse: false },
  faulted: { border: 'border-red-500', dot: 'bg-red-500', pulse: false },
  waiting: { border: 'border-amber-500', dot: 'bg-amber-500', pulse: true },
  canceled: { border: 'border-gray-400', dot: 'bg-gray-400', pulse: false },
  compensated: { border: 'border-purple-500', dot: 'bg-purple-500', pulse: false },
}

const statusStyle = computed(() => data.value.__status ? STATUS_STYLE[data.value.__status] : null)

const borderClass = computed(() => {
  if (statusStyle.value)
    return statusStyle.value.border
  if (data.value.__selected)
    return 'border-primary'
  return 'border-gray-200 dark:border-gray-600'
})

const ringClass = computed(() => data.value.__highlight ? 'ring-2 ring-red-500 ring-offset-1' : '')
</script>

<template>
  <div
    class="relative h-full w-full rounded-lg border-2 bg-white px-3 py-2 shadow-sm dark:bg-gray-800"
    :class="[borderClass, ringClass]"
  >
    <!-- 运行态圆点 -->
    <span
      v-if="statusStyle"
      class="absolute right-1.5 top-1.5 h-2.5 w-2.5 rounded-full"
      :class="[statusStyle.dot, statusStyle.pulse ? 'animate-pulse' : '']"
    />
    <!-- 校验错误标记 -->
    <span v-else-if="data.__highlight" class="absolute right-1 top-1 text-red-500">
      <Icon icon="lucide:alert-circle" class="text-sm" />
    </span>

    <div class="flex items-center gap-2">
      <Icon :icon="icon" class="shrink-0 text-base text-primary" />
      <div class="min-w-0">
        <div class="truncate text-sm font-medium">
          {{ data.name || id }}
        </div>
        <div class="truncate text-xs text-gray-400">
          {{ typeLabel }}
        </div>
      </div>
    </div>
    <div v-if="data.continueOnError || data.timeoutSeconds || data.retryPolicy" class="mt-1 flex gap-1 text-[10px] text-gray-400">
      <span v-if="data.retryPolicy">{{ t('workflow.designer.badge_retry') }}</span>
      <span v-if="data.timeoutSeconds">{{ t('workflow.designer.badge_timeout') }}</span>
      <span v-if="data.continueOnError">{{ t('workflow.designer.badge_continue') }}</span>
    </div>
  </div>
</template>
