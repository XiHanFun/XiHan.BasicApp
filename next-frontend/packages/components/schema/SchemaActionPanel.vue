<script setup lang="ts" generic="TRow extends object">
import type { MenuNode } from '@xihan-ui/headless'
import type { Tone } from '@xihan-ui/kernel'
import type { ActionSchema } from './types'
import { XhButton, XhMenuRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useIsMobile } from '~/composables'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import XIconButton from '../common/XIconButton.vue'

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

const { t } = useI18n()

/** 小屏断点（max-width:767px） */
const { isMobile } = useIsMobile()

/** 小屏且按钮含图标时转为圆形纯图标按钮（无图标的按钮兜底保留文字） */
function isIconOnly(action: ActionSchema<TRow>): boolean {
  return isMobile.value && !!action.icon
}

/** 操作 Schema 的 type 到组件库 tone 轴的换算 */
function toneOf(type: ActionSchema<TRow>['type']): Tone {
  switch (type) {
    case 'primary':
      return 'brand'
    case 'error':
      return 'danger'
    case 'info':
    case 'success':
    case 'warning':
      return type
    default:
      return 'neutral'
  }
}

/** 有权限的页面操作 */
const permitted = computed(() =>
  props.actions.filter(a => a.scope === 'page' && (!a.permission || hasPermission(a.permission))),
)

/** 直接展示的按钮 */
const primaryActions = computed(() => permitted.value.slice(0, props.maxButtons))

/** 收入「更多」的操作 */
const moreActions = computed(() => permitted.value.slice(props.maxButtons))

const moreOptions = computed<MenuNode[]>(() =>
  moreActions.value.map(a => ({ value: a.key, label: a.title })),
)
</script>

<template>
  <div class="flex flex-wrap gap-2 items-center">
    <template v-for="action in primaryActions" :key="action.key">
      <!-- 小屏收成纯图标钮，文案退到提示里；宽屏照常出文字 -->
      <XIconButton
        v-if="isIconOnly(action)"
        :icon="action.icon!"
        :label="action.title"
        @click="emit('action', action.key)"
      />
      <XhButton
        v-else
        size="sm"
        :variant="action.type && action.type !== 'default' ? 'solid' : 'outline'"
        :tone="toneOf(action.type)"
        :aria-label="action.title"
        @click="emit('action', action.key)"
      >
        <Icon v-if="action.icon" :icon="action.icon" />
        {{ action.title }}
      </XhButton>
    </template>

    <XhMenuRoot
      v-if="moreOptions.length"
      class="schema-action-more"
      :collection="moreOptions"
      @select="(details: { value: string }) => emit('action', details.value)"
    >
      <template #trigger>
        <Icon :icon="isMobile ? 'lucide:ellipsis' : 'lucide:chevron-down'" />
        <span v-if="!isMobile">{{ t('component.schema_page.more') }}</span>
      </template>
    </XhMenuRoot>

    <div class="flex-1" />

    <!-- 右侧工具栏插槽：刷新 / 密度 / 全屏 / 列设置 -->
    <slot name="toolbar" />
  </div>
</template>

<style scoped>
/* 「更多」的触发器按普通描边按钮打扮，与左侧一排操作钮同高 */
.schema-action-more :deep([data-scope='menu'][data-part='trigger']) {
  display: inline-flex;
  gap: var(--xh-control-gap-sm);
  align-items: center;
  block-size: var(--xh-control-h-sm);
  padding-inline: var(--xh-control-px-sm);
  border: var(--xh-stroke-thin) solid var(--xh-border-control);
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: var(--xh-fg-default);
  font: inherit;
  cursor: pointer;
}

.schema-action-more :deep([data-scope='menu'][data-part='trigger']:hover) {
  background: var(--xh-bg-subtle-hover);
}
</style>
