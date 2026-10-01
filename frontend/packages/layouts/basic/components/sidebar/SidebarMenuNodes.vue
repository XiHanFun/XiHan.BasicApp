<script setup lang="ts">
import type { MenuLinkResolver } from './menu-links'
import type { AppMenuOption } from '~/types'
import {
  XhSideNavBranch,
  XhSideNavBranchContent,
  XhSideNavBranchIndicator,
  XhSideNavBranchText,
  XhSideNavBranchTrigger,
  XhSideNavItem,
  XhSideNavLink,
  XhSideNavLinkText,
} from '@xihan-ui/vue'
import { RouterLink } from 'vue-router'
import { VNodeRender } from '~/components'
import { Icon } from '~/iconify'

/**
 * 侧栏菜单条目的递归渲染。
 *
 * 菜单树深度不定，SFC 里递归要靠组件自引用——本组件按 name 自引用，
 * 每层只管「有子级就出分支、没有就出链接」。
 * label / icon 允许是渲染函数（角标、外链图标），故一律经 VNodeRender 出。
 *
 * 叶子是真链接：link 部件 as-child 借 RouterLink（外链借原生 <a target=_blank>）渲染，
 * 点击、Enter、中键与 Ctrl+点击都走浏览器的链接语义；组件库只管选中与键盘，跳转归链接本身。
 */
defineOptions({ name: 'SidebarMenuNodes' })

const props = defineProps<{
  nodes: AppMenuOption[]
  /** 叶子的去处：站内交给路由，外链走新标签 */
  linkOf: MenuLinkResolver
}>()

/** 叶子链接借用的元素与属性：外链是原生新标签链接，站内是 RouterLink */
function linkBinding(key: string) {
  const target = props.linkOf(key)
  return target.href === undefined
    ? { is: RouterLink, attrs: { to: target.to } }
    : { is: 'a', attrs: { href: target.href, target: '_blank', rel: 'noopener noreferrer' } }
}
</script>

<template>
  <template v-for="node in nodes" :key="node.key">
    <XhSideNavBranch v-if="node.children?.length" :value="node.key">
      <XhSideNavBranchTrigger class="sidebar-menu__row">
        <span v-if="node.icon" class="sidebar-menu__icon" aria-hidden="true">
          <VNodeRender :content="node.icon()" />
        </span>
        <XhSideNavBranchText class="sidebar-menu__label">
          <VNodeRender v-if="typeof node.label === 'function'" :content="node.label()" />
          <template v-else>
            {{ node.label }}
          </template>
        </XhSideNavBranchText>
        <XhSideNavBranchIndicator class="sidebar-menu__arrow">
          <!-- 展开态的 90° 旋转由皮肤按 data-state 给，这里只出图标 -->
          <Icon icon="lucide:chevron-right" width="14" height="14" />
        </XhSideNavBranchIndicator>
      </XhSideNavBranchTrigger>
      <XhSideNavBranchContent>
        <SidebarMenuNodes :nodes="node.children" :link-of="linkOf" />
      </XhSideNavBranchContent>
    </XhSideNavBranch>

    <!-- 叶子：链接是列表里的一条，外面必须套 item（li），否则 <a> 直接挂在 <ul> 下 -->
    <XhSideNavItem v-else class="sidebar-menu__row">
      <XhSideNavLink :value="node.key" as-child>
        <component :is="linkBinding(node.key).is" v-bind="linkBinding(node.key).attrs">
          <span v-if="node.icon" class="sidebar-menu__icon" aria-hidden="true">
            <VNodeRender :content="node.icon()" />
          </span>
          <XhSideNavLinkText class="sidebar-menu__label">
            <VNodeRender v-if="typeof node.label === 'function'" :content="node.label()" />
            <template v-else>
              {{ node.label }}
            </template>
          </XhSideNavLinkText>
        </component>
      </XhSideNavLink>
    </XhSideNavItem>
  </template>
</template>
