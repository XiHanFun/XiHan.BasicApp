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
/* 顶级入口的观感对齐旧版顶栏：大档控件高（40px）、左右 10px、圆角 6px、正文字号。
   几何与各态配色走组件库的公开槽：展开中的入口套品牌淡底、指向当前页的链接染品牌字，
   悬停 / 按下 / 焦点环由 Collection Item 家族按状态给，这里不再直接盖 background / color。
   无子级的入口（link）与触发器同一副几何，取值直接引触发器那几支 */
.header-top-menu {
  --xh-navigation-menu-font-size: var(--xh-text-body-size);
  --xh-navigation-menu-trigger-h: var(--xh-control-h-lg);
  --xh-navigation-menu-trigger-px: 10px;
  --xh-navigation-menu-trigger-radius: 6px;
  --xh-navigation-menu-trigger-bg-active: hsl(var(--primary) / 15%);
  --xh-navigation-menu-link-font-size: var(--xh-navigation-menu-font-size);
  --xh-navigation-menu-link-px: var(--xh-navigation-menu-trigger-px);
  --xh-navigation-menu-link-py: 0;
  --xh-navigation-menu-link-radius: var(--xh-navigation-menu-trigger-radius);
  --xh-navigation-menu-link-fg-current: hsl(var(--primary));
}

/* 链接只有内衬槽没有高度槽，高度直接给 */
.header-top-menu :deep([data-scope='navigation-menu'][data-part='link']) {
  block-size: var(--xh-navigation-menu-trigger-h);
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
