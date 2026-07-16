<script setup lang="ts">
import type { DesignerNodeData } from './transform'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/components'
import { useDiagramNode } from '~/diagram'
import { ACTIVITY_MAP } from './catalog'

/**
 * 工作流活动节点（经 ~/diagram 的 registerVueShape 注册的 Vue 形状）
 *
 * 数据来自 useDiagramNode（updateNodeData 全量替换后自动重渲染），零图引擎依赖。
 */

defineOptions({ name: 'WorkflowActivityNode' })

const { t } = useI18n()

const { id, data } = useDiagramNode<DesignerNodeData>({ activityType: '', name: '', properties: {} })

const meta = computed(() => ACTIVITY_MAP[data.value.activityType])
const typeLabel = computed(() => meta.value ? t(`workflow.designer.activity.${meta.value.labelKey}`) : data.value.activityType)
const icon = computed(() => meta.value?.icon ?? 'lucide:box')
</script>

<template>
  <div
    class="h-full w-full rounded-lg border-2 bg-white px-3 py-2 shadow-sm dark:bg-gray-800"
    :class="data.__selected ? 'border-primary' : 'border-gray-200 dark:border-gray-600'"
  >
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
