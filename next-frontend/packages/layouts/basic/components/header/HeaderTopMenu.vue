<script setup lang="ts">
import type { NavigationMenuNode } from '@xihan-ui/headless'
import type { AppMenuOption } from '~/types'
import {
  XhNavigationMenuContent,
  XhNavigationMenuItem,
  XhNavigationMenuLink,
  XhNavigationMenuList,
  XhNavigationMenuRoot,
  XhNavigationMenuTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { VNodeRender } from '~/components'
import { Icon } from '~/iconify'
import HeaderTopMenuPanel from './HeaderTopMenuPanel.vue'

/**
 * 顶栏横向菜单。
 *
 * 顶级入口由导航菜单负责（键盘横向遍历、浮层落位、指示条），但整套结构是手摆的而不是喂
 * collection 让它自动铺：collection 里的 label 只能是纯串，而顶栏标签要带角标与外链图标。
 * 入口之下的整棵子树走面板自绘——组件库的 menu / menubar 都只支持两级，路由菜单可以更深。
 */
defineOptions({ name: 'HeaderTopMenu' })

const props = defineProps<{
  options: AppMenuOption[]
  /** 当前选中项 */
  activeKey?: string
}>()

const emit = defineEmits<{ select: [key: string] }>()

/**
 * collection 仍要给：它是入口身份、禁用与键盘序列的事实源。
 * 无子级的入口给 href 占位，点它由 click 拦下走路由，不让浏览器整页跳走。
 */
const entries = computed<NavigationMenuNode[]>(() =>
  props.options.map<NavigationMenuNode>(option => ({
    value: option.key,
    label: typeof option.label === 'string' ? option.label : option.key,
    ...(option.disabled ? { disabled: true } : {}),
    ...(option.children?.length ? {} : { href: '#' }),
    ...(option.key === props.activeKey ? { current: true } : {}),
  })),
)

function onLinkClick(event: MouseEvent, key: string): void {
  event.preventDefault()
  emit('select', key)
}
</script>

<template>
  <XhNavigationMenuRoot class="header-top-menu" :collection="entries">
    <XhNavigationMenuList>
      <XhNavigationMenuItem v-for="option in options" :key="option.key">
        <!-- 有子级：入口是浮层触发器，面板里铺整棵子树 -->
        <template v-if="option.children?.length">
          <XhNavigationMenuTrigger :value="option.key">
            <span class="header-top-menu__entry">
              <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
              <template v-else>{{ option.label }}</template>
              <Icon icon="lucide:chevron-down" class="header-top-menu__arrow" />
            </span>
          </XhNavigationMenuTrigger>
          <XhNavigationMenuContent :value="option.key" class="header-top-menu__panel">
            <HeaderTopMenuPanel
              :nodes="option.children"
              :active-key="activeKey"
              @select="(key: string) => emit('select', key)"
            />
          </XhNavigationMenuContent>
        </template>
        <!-- 无子级：入口即去处 -->
        <XhNavigationMenuLink
          v-else
          href="#"
          :current="option.key === activeKey"
          @click="(event: MouseEvent) => onLinkClick(event, option.key)"
        >
          <span class="header-top-menu__entry">
            <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
            <template v-else>{{ option.label }}</template>
          </span>
        </XhNavigationMenuLink>
      </XhNavigationMenuItem>
    </XhNavigationMenuList>
  </XhNavigationMenuRoot>
</template>

<style scoped>
/* 顶级入口的观感对齐旧版顶栏：40px 高、左右 10px、圆角 6px、选中套品牌淡底 */
.header-top-menu :deep([data-scope='navigation-menu'][data-part='trigger']),
.header-top-menu :deep([data-scope='navigation-menu'][data-part='link']) {
  block-size: 40px;
  padding-inline: 10px;
  border-radius: 6px;
  font-size: 14px;
}

.header-top-menu :deep([data-scope='navigation-menu'][data-part='trigger'][data-state='open']),
.header-top-menu :deep([data-scope='navigation-menu'][data-part='link'][aria-current='page']) {
  background: hsl(var(--primary) / 15%);
  color: hsl(var(--primary));
}

.header-top-menu__panel {
  min-inline-size: 180px;
  padding: 6px;
}

/* 有子级的入口带一枚下拉箭头 */
.header-top-menu__arrow {
  flex: none;
  opacity: 0.7;
}

.header-top-menu__entry {
  display: inline-flex;
  gap: 4px;
  align-items: center;
  min-inline-size: 0;
}
</style>
