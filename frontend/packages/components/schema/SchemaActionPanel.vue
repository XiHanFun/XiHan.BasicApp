<script setup lang="ts" generic="TRow extends object">
import type { DropdownOption } from 'naive-ui'
import type { ActionSchema } from './types'
import { NButton, NDropdown, NIcon } from 'naive-ui'
import { computed } from 'vue'
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
    <NButton
      v-for="action in primaryActions"
      :key="action.key"
      size="small"
      :type="action.type ?? 'default'"
      @click="emit('action', action.key)"
    >
      <template v-if="action.icon" #icon>
        <NIcon><Icon :icon="action.icon" /></NIcon>
      </template>
      {{ action.title }}
    </NButton>

    <NDropdown v-if="moreOptions.length" :options="moreOptions" trigger="click" @select="onMoreSelect">
      <NButton size="small">
        更多
        <template #icon>
          <NIcon><Icon icon="lucide:chevron-down" /></NIcon>
        </template>
      </NButton>
    </NDropdown>

    <div class="flex-1" />

    <!-- 右侧工具栏插槽：刷新 / 密度 / 全屏 / 列设置 -->
    <slot name="toolbar" />
  </div>
</template>
