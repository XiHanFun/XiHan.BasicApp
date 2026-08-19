<script lang="ts" setup>
import type { AppDropdownOption } from '~/types'
import {
  XhContextMenuContent,
  XhContextMenuItem,
  XhContextMenuItemText,
  XhContextMenuPositioner,
  XhContextMenuRoot,
  XhContextMenuSeparator,
  XhContextMenuSub,
  XhContextMenuSubTrigger,
  XhContextMenuTrigger,
  XhMenuContent,
  XhMenuItem,
  XhMenuPositioner,
} from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { VNodeRender } from '~/components'

/**
 * 标签页右键菜单。
 *
 * 开合与落点由父组件给（它在标签上监听右键并记下坐标），所以这里不接管右键事件。
 * 菜单钉在坐标上，坐标经实例命令 openAt 交进去，开合随之走命令而非受控 open。
 * 摆的是部件而非交 collection：带 children 的条目要渲染成二级子菜单，代铺那条路只吃扁平条目。
 */
interface Props {
  show: boolean
  x: number
  y: number
  options: AppDropdownOption[]
}

const props = defineProps<Props>()

const emit = defineEmits<{
  select: [key: string]
  close: []
}>()

const rootRef = ref<{ openAt: (x: number, y: number) => void, setOpen: (open: boolean) => void } | null>(null)

watch(
  () => [props.show, props.x, props.y] as const,
  ([show, x, y]) => {
    if (show) {
      rootRef.value?.openAt(x, y)
    }
    else {
      rootRef.value?.setOpen(false)
    }
  },
)
</script>

<template>
  <XhContextMenuRoot
    ref="rootRef"
    placement="bottom-start"
    @update:open="(open: boolean) => !open && emit('close')"
    @select="(details: { value: string }) => emit('select', details.value)"
  >
    <XhContextMenuTrigger>
      <!-- 触发插槽的占位：菜单钉在 openAt 交进去的坐标上，不靠它定位 -->
      <span class="tabbar-context-anchor" aria-hidden="true" />
    </XhContextMenuTrigger>
    <XhContextMenuPositioner>
      <!-- 条目一次列全：皮肤给的 20rem 上限装不下本菜单，放开到视口余量（库仍按落定侧的剩余空间收口） -->
      <XhContextMenuContent style="--xh-context-menu-max-h: 100vh">
        <template v-for="option in options" :key="option.key">
          <XhContextMenuSeparator v-if="option.type === 'divider'" />

          <!-- 图标与文字是条目的兄弟节点，排布交给条目皮肤；item-text 撑满剩余宽度，箭头随之顶到末端 -->
          <XhContextMenuSub v-else-if="option.children?.length" :value="option.key">
            <XhContextMenuSubTrigger>
              <VNodeRender v-if="option.icon" :content="option.icon()" />
              <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
              <template v-else>
                {{ option.label }}
              </template>
              <span aria-hidden="true" class="tabbar-context-arrow">›</span>
            </XhContextMenuSubTrigger>
            <XhMenuPositioner>
              <XhMenuContent>
                <XhMenuItem
                  v-for="child in option.children"
                  :key="child.key"
                  :value="child.key"
                  :disabled="child.disabled"
                >
                  <VNodeRender v-if="child.icon" :content="child.icon()" />
                  <VNodeRender v-if="typeof child.label === 'function'" :content="child.label()" />
                  <template v-else>
                    {{ child.label }}
                  </template>
                </XhMenuItem>
              </XhMenuContent>
            </XhMenuPositioner>
          </XhContextMenuSub>

          <XhContextMenuItem v-else :value="option.key" :disabled="option.disabled">
            <VNodeRender v-if="option.icon" :content="option.icon()" />
            <XhContextMenuItemText>
              <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
              <template v-else>
                {{ option.label }}
              </template>
            </XhContextMenuItemText>
          </XhContextMenuItem>
        </template>
      </XhContextMenuContent>
    </XhContextMenuPositioner>
  </XhContextMenuRoot>
</template>

<style scoped>
/* 触发插槽的占位：菜单钉在 openAt 交进去的坐标上，不靠它定位 */
.tabbar-context-anchor {
  position: fixed;
  inset: 0 auto auto 0;
  inline-size: 0;
  block-size: 0;
  pointer-events: none;
}

/* 子菜单触发器里没有撑满宽度的 item-text，箭头自己顶到条目末端 */
.tabbar-context-arrow {
  margin-inline-start: auto;
  padding-inline-start: 12px;
  color: var(--xh-fg-muted);
}
</style>
