<script setup lang="ts">
import type { DropdownOption, MenuOption } from 'naive-ui'
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NBreadcrumb, NBreadcrumbItem, NButton, NDropdown, NIcon, NMenu } from 'naive-ui'
import { computed } from 'vue'

defineOptions({ name: 'HeaderNav' })

const props = defineProps<HeaderNavProps>()

const emit = defineEmits<{
  sidebarToggle: []
  refresh: []
  breadcrumbSelect: [path: string]
  topMenuSelect: [path: string]
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
  showTopMenu: boolean
  breadcrumbs: BreadcrumbItem[]
  topMenuActive?: string
  topMenuOptions: MenuOption[]
}

/**
 * 面包屑完整列表（含 Home），用于判断哪个 item 是"最后一项（当前页）"。
 * 与 vben 的 breadcrumbs computed 对齐：统一处理 Home，统一判断 isLast。
 */
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
  <div
    class="flex min-w-0 flex-1 items-center gap-2"
    :class="props.appStore.headerMenuAlign === 'center' ? 'mx-auto' : ''"
  >
    <!-- 侧边栏折叠切换 -->
    <NButton
      v-if="!props.showTopMenu && props.appStore.widgetSidebarToggle"
      quaternary
      circle
      size="small"
      @click="emit('sidebarToggle')"
    >
      <template #icon>
        <NIcon>
          <Icon
            :icon="props.appStore.sidebarCollapsed ? 'lucide:panel-left-open' : 'lucide:panel-left-close'"
            width="18"
          />
        </NIcon>
      </template>
    </NButton>

    <!-- 刷新当前页 -->
    <NButton
      v-if="props.appStore.widgetRefresh"
      quaternary
      circle
      size="small"
      @mousedown.prevent
      @click="emit('refresh')"
    >
      <template #icon>
        <NIcon size="16">
          <Icon icon="lucide:refresh-cw" />
        </NIcon>
      </template>
    </NButton>

    <!--
      面包屑导航
      显示条件对标 vben：
        - breadcrumbEnabled
        - 非顶部菜单模式
        - hideWhenOnlyOne：只有一项（且无 home）时隐藏
        - 响应式：lg 以下隐藏（vben 用 hidden lg:block）
    -->
    <NBreadcrumb
      v-if="
        props.appStore.breadcrumbEnabled
          && !props.showTopMenu
          && !(props.appStore.breadcrumbHideOnlyOne && allCrumbs.length <= 1)
      "
      class="hidden lg:flex items-center"
      :class="props.appStore.breadcrumbStyle === 'background'
        ? 'rounded-md bg-[hsl(var(--muted))] px-2 py-1'
        : ''"
    >
      <!-- Home 项 -->
      <NBreadcrumbItem v-if="props.appStore.breadcrumbShowHome">
        <!-- 分隔符：lucide chevron-right，对标 vben ChevronRight -->
        <template v-if="!isLast(true)" #separator>
          <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
        </template>
        <div
          class="crumb-item"
          :class="isLast(true) ? 'crumb-item--active' : 'crumb-item--link'"
          @click="!isLast(true) && emit('homeClick')"
        >
          <!--
            图标受 breadcrumbShowIcon 控制（vben 同款行为）
            Home 使用 lucide:house，vben 使用 mdi:home-outline
          -->
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

        <!-- 有同级兄弟页面 → 下拉选择（对标 vben DropdownMenu） -->
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

        <!-- 无兄弟页面 → 普通链接（对标 vben BreadcrumbLink / BreadcrumbPage） -->
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

    <!-- 顶部水平菜单 -->
    <NMenu
      v-if="props.showTopMenu"
      class="hidden lg:block"
      mode="horizontal"
      :value="props.topMenuActive"
      :options="props.topMenuOptions"
      @update:value="key => emit('topMenuSelect', String(key))"
    />
  </div>
</template>

<style scoped>
/**
 * 面包屑条目：与 vben BreadcrumbLink / BreadcrumbPage 对齐
 * - inline-flex + items-center 替代 NFlex 组件，行为更可预期
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

/** 分隔符图标：低透明度，对标 vben ChevronRight */
.crumb-sep {
  display: block;
  flex-shrink: 0;
  opacity: 0.4;
}

/** 进入动画：对标 vben breadcrumb-transition（translateX + skewX）*/
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
