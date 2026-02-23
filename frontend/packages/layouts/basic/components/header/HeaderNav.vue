<script setup lang="ts">
import type { DropdownOption } from 'naive-ui'
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NBreadcrumb, NBreadcrumbItem, NDropdown } from 'naive-ui'
import { computed } from 'vue'
import AppBrand from '../AppBrand.vue'
import XihanIconButton from '../XihanIconButton.vue'

defineOptions({ name: 'HeaderNav' })

const props = defineProps<HeaderNavProps>()

const emit = defineEmits<{
  sidebarToggle: []
  refresh: []
  breadcrumbSelect: [path: string]
  homeClick: []
}>()

interface BreadcrumbItem {
  title: string
  path: string
  icon?: string
  siblings: DropdownOption[]
}

interface HeaderNavProps {
  appStore: ReturnType<typeof useAppStore>
  layoutMode: string
  appTitle: string
  appLogo: string
  showTopMenu: boolean
  breadcrumbs: BreadcrumbItem[]
}

/** 面包屑完整列表（含 Home），用于判断哪个 item 是"最后一项（当前页）" */
const allCrumbs = computed(() => {
  const result: Array<{ key: string, isHome?: boolean, index?: number }> = []
  if (props.appStore.breadcrumbShowHome)
    result.push({ key: 'home', isHome: true })
  props.breadcrumbs.forEach((_, i) => result.push({ key: String(i), index: i }))
  return result
})

function isLast(isHome: boolean, index?: number): boolean {
  if (isHome)
    return props.breadcrumbs.length === 0
  return index === props.breadcrumbs.length - 1
}
</script>

<template>
  <div class="flex min-w-0 shrink-0 items-center gap-2">
    <div class="flex min-w-0 items-center gap-2">
      <AppBrand
        v-if="['mix', 'header-sidebar', 'top'].includes(props.layoutMode)"
        class="mr-1 hidden lg:flex"
        :app-title="props.appTitle"
        :app-logo="props.appLogo"
        :collapsed="false"
      />

      <!-- 侧边栏折叠切换 -->
      <XihanIconButton
        v-if="!props.showTopMenu && props.appStore.widgetSidebarToggle"
        :tooltip="props.appStore.sidebarCollapsed ? '展开侧边栏' : '收起侧边栏'"
        @mousedown.prevent
        @click="emit('sidebarToggle')"
      >
        <Icon
          :icon="props.appStore.sidebarCollapsed ? 'lucide:panel-left-open' : 'lucide:panel-left-close'"
          width="18"
          height="18"
        />
      </XihanIconButton>

      <!-- 刷新当前页 -->
      <XihanIconButton
        v-if="props.appStore.widgetRefresh"
        tooltip="刷新"
        @mousedown.prevent
        @click="emit('refresh')"
      >
        <Icon icon="lucide:refresh-cw" width="16" height="16" />
      </XihanIconButton>

      <!-- 面包屑导航（lg 以下隐藏） -->
      <NBreadcrumb
        v-if="
          props.appStore.breadcrumbEnabled
            && !props.showTopMenu
            && !(props.appStore.breadcrumbHideOnlyOne && allCrumbs.length <= 1)
        "
        class="hidden lg:flex items-center"
        :class="props.appStore.breadcrumbStyle === 'background'
          ? 'rounded-md bg-muted px-2 py-1'
          : ''"
      >
        <!-- Home 项 -->
        <NBreadcrumbItem v-if="props.appStore.breadcrumbShowHome">
          <template v-if="!isLast(true)" #separator>
            <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
          </template>
          <div
            class="crumb-item"
            :class="isLast(true) ? 'crumb-item--active' : 'crumb-item--link'"
            @click="!isLast(true) && emit('homeClick')"
          >
            <Icon
              v-if="props.appStore.breadcrumbShowIcon"
              icon="lucide:house"
              width="14"
              height="14"
              class="crumb-icon"
            />
            <span>Home</span>
          </div>
        </NBreadcrumbItem>

        <!-- 路由层级各项 -->
        <NBreadcrumbItem
          v-for="(item, index) in props.breadcrumbs"
          :key="item.path"
        >
          <template v-if="!isLast(false, index)" #separator>
            <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
          </template>

          <!-- 有同级兄弟页面 → 下拉选择 -->
          <NDropdown
            v-if="item.siblings.length > 1"
            :options="item.siblings"
            @select="key => emit('breadcrumbSelect', String(key))"
          >
            <div
              class="crumb-item"
              :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
            >
              <Icon
                v-if="props.appStore.breadcrumbShowIcon && item.icon"
                :icon="item.icon!"
                width="14"
                height="14"
                class="crumb-icon"
              />
              <span>{{ item.title }}</span>
            </div>
          </NDropdown>

          <!-- 无兄弟页面 → 普通链接 -->
          <div
            v-else
            class="crumb-item"
            :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
            @click="!isLast(false, index) && emit('breadcrumbSelect', item.path)"
          >
            <Icon
              v-if="props.appStore.breadcrumbShowIcon && item.icon"
              :icon="item.icon!"
              width="14"
              height="14"
              class="crumb-icon"
            />
            <span>{{ item.title }}</span>
          </div>
        </NBreadcrumbItem>
      </NBreadcrumb>
    </div>
  </div>
</template>

<style scoped>
/**
 * 面包屑条目
 * - inline-flex + items-center，行为可预期
 * - 不设 height，由 line-height + padding 自然撑开，避免 icon 开关引起高度抖动
 * - gap 设在容器上，icon v-if 时 gap 仍为 0（flex 单子节点无 gap）
 */
.crumb-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 13px;
  line-height: 20px;
  white-space: nowrap;
  animation: crumb-enter 0.3s cubic-bezier(0.76, 0, 0.24, 1) both;
  transition:
    color 0.2s ease,
    background 0.15s ease;
}

/** 可点击项（非末尾）：交给 Naive UI NBreadcrumb 自带 hover 处理 */
.crumb-item--link {
  cursor: pointer;
}

/** 当前页（末尾）：不可点击，颜色由 Naive UI --n-item-text-color-active 或默认文字色控制 */
.crumb-item--active {
  font-weight: 500;
  cursor: default;
  pointer-events: none;
}

/** 图标：flex-shrink:0 防止被文字压缩 */
.crumb-icon {
  flex-shrink: 0;
}

/** 分隔符图标：低透明度 */
.crumb-sep {
  display: block;
  flex-shrink: 0;
  opacity: 0.4;
}

/** 进入动画 */
@keyframes crumb-enter {
  from {
    opacity: 0;
    transform: translateX(20px) skewX(-10deg);
  }

  to {
    opacity: 1;
    transform: translateX(0) skewX(0deg);
  }
}
</style>
