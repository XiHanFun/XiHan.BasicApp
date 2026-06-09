<script setup lang="ts" generic="TRow extends object">
import type { DropdownOption } from 'naive-ui'
import type { ActionSchema } from './types'
import { NButton, NDropdown, NIcon, NTooltip } from 'naive-ui'
import { computed } from 'vue'
import { useIsMobile } from '~/composables'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaActionPanel' })

const props = withDefaults(defineProps<{
  /** 页面级操作（scope='page'） */
  actions: ActionSchema<TRow>[]
  /** 超过该数量的按钮收入「更多」，默认 4 */
  maxButtons?: number
}>(), {
  maxButtons: 4,
})

const emit = defineEmits<{
  action: [key: string]
}>()

const { hasPermission } = usePermission()

/** 小屏断点（max-width:767px） */
const { isMobile } = useIsMobile()

/** 小屏且按钮含图标时转为圆形纯图标按钮（无图标的按钮兜底保留文字） */
function isIconOnly(action: ActionSchema<TRow>): boolean {
  return isMobile.value && !!action.icon
}

/** 有权限的页面操作 */
const permitted = computed(() =>
  props.actions.filter(a => a.scope === 'page' && (!a.permission || hasPermission(a.permission))),
)

/** 直接展示的按钮 */
const primaryActions = computed(() => permitted.value.slice(0, props.maxButtons))

/** 收入「更多」的操作 */
const moreActions = computed(() => permitted.value.slice(props.maxButtons))

const moreOptions = computed<DropdownOption[]>(() =>
  moreActions.value.map(a => ({ key: a.key, label: a.title })),
)

function onMoreSelect(key: string) {
  emit('action', key)
}
</script>

<template>
  <div class="flex flex-wrap gap-2 items-center">
    <NTooltip
      v-for="action in primaryActions"
      :key="action.key"
      :disabled="!isIconOnly(action)"
    >
      <template #trigger>
        <NButton
          size="small"
          :circle="isIconOnly(action)"
          :type="action.type ?? 'default'"
          :aria-label="action.title"
          @click="emit('action', action.key)"
        >
          <template v-if="action.icon" #icon>
            <NIcon><Icon :icon="action.icon" /></NIcon>
          </template>
          <template v-if="!isIconOnly(action)">
            {{ action.title }}
          </template>
        </NButton>
      </template>
      {{ action.title }}
    </NTooltip>

    <NDropdown v-if="moreOptions.length" :options="moreOptions" trigger="click" @select="onMoreSelect">
      <NButton size="small" :circle="isMobile" aria-label="更多">
        <template #icon>
          <NIcon><Icon :icon="isMobile ? 'lucide:ellipsis' : 'lucide:chevron-down'" /></NIcon>
        </template>
        <template v-if="!isMobile">
          更多
        </template>
      </NButton>
    </NDropdown>

    <div class="flex-1" />

    <!-- 右侧工具栏插槽：刷新 / 密度 / 全屏 / 列设置 -->
    <slot name="toolbar" />
  </div>
</template>
