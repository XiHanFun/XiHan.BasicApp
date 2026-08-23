<script setup lang="ts">
import type { AppMenuOption } from '~/types'
import {
  XhSideNavBranch,
  XhSideNavBranchContent,
  XhSideNavBranchIndicator,
  XhSideNavBranchText,
  XhSideNavLink,
  XhSideNavLinkText,
} from '@xihan-ui/vue'
import { VNodeRender } from '~/components'
import { Icon } from '~/iconify'

/**
 * 侧栏菜单条目的递归渲染。
 *
 * 菜单树深度不定，SFC 里递归要靠组件自引用——本组件按 name 自引用，
 * 每层只管「有子级就出分支、没有就出链接」。
 * label / icon 允许是渲染函数（角标、外链图标），故一律经 VNodeRender 出。
 */
defineOptions({ name: 'SidebarMenuNodes' })

defineProps<{
  nodes: AppMenuOption[]
}>()
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
        <SidebarMenuNodes :nodes="node.children" />
      </XhSideNavBranchContent>
    </XhSideNavBranch>

    <XhSideNavLink v-else :value="node.key" class="sidebar-menu__row">
      <span v-if="node.icon" class="sidebar-menu__icon" aria-hidden="true">
        <VNodeRender :content="node.icon()" />
      </span>
      <XhSideNavLinkText class="sidebar-menu__label">
        <VNodeRender v-if="typeof node.label === 'function'" :content="node.label()" />
        <template v-else>
          {{ node.label }}
        </template>
      </XhSideNavLinkText>
    </XhSideNavLink>
  </template>
</template>
