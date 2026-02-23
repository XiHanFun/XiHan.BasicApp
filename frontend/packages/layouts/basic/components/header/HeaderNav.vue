<script setup lang="ts">
import type { DropdownOption, MenuOption } from 'naive-ui'
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NBreadcrumb, NBreadcrumbItem, NButton, NDropdown, NFlex, NIcon, NMenu } from 'naive-ui'

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

/** 最后一项（当前页）判断 */
function isLastItem(isHome: boolean, index?: number): boolean {
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

    <!-- 面包屑导航（对标 vben：lg 断点以上才显示） -->
    <NBreadcrumb
      v-if="
        props.appStore.breadcrumbEnabled
          && !props.showTopMenu
          && !(props.appStore.breadcrumbHideOnlyOne
            && props.breadcrumbs.length === 0
            && !props.appStore.breadcrumbShowHome)
      "
      class="hidden lg:flex"
      :class="props.appStore.breadcrumbStyle === 'background'
        ? 'rounded-md bg-[hsl(var(--muted))] px-2 py-1'
        : ''"
    >
      <!-- Home -->
      <NBreadcrumbItem v-if="props.appStore.breadcrumbShowHome">
        <template #separator>
          <NIcon size="12" class="crumb-sep">
            <Icon icon="lucide:chevron-right" />
          </NIcon>
        </template>
        <NFlex
          align="center"
          :size="4"
          class="crumb-item"
          :class="isLastItem(true) ? 'crumb-item--active' : 'crumb-item--link'"
          @click="!isLastItem(true) && emit('homeClick')"
        >
          <NIcon size="14">
            <Icon icon="lucide:house" />
          </NIcon>
          <span>Home</span>
        </NFlex>
      </NBreadcrumbItem>

      <!-- 路由层级 -->
      <NBreadcrumbItem
        v-for="(item, index) in props.breadcrumbs"
        :key="item.path"
      >
        <template #separator>
          <NIcon size="12" class="crumb-sep">
            <Icon icon="lucide:chevron-right" />
          </NIcon>
        </template>

        <!-- 有同级页面：可展开下拉 -->
        <NDropdown
          v-if="item.siblings.length > 1"
          :options="item.siblings"
          @select="key => emit('breadcrumbSelect', String(key))"
        >
          <NFlex
            align="center"
            :size="4"
            class="crumb-item"
            :class="isLastItem(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
          >
            <NIcon v-if="props.appStore.breadcrumbShowIcon && item.icon" size="14">
              <Icon :icon="item.icon!" />
            </NIcon>
            <span>{{ item.title }}</span>
          </NFlex>
        </NDropdown>

        <!-- 无同级页面：纯展示 -->
        <NFlex
          v-else
          align="center"
          :size="4"
          class="crumb-item"
          :class="isLastItem(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
          @click="!isLastItem(false, index) && emit('breadcrumbSelect', item.path)"
        >
          <NIcon v-if="props.appStore.breadcrumbShowIcon && item.icon" size="14">
            <Icon :icon="item.icon!" />
          </NIcon>
          <span>{{ item.title }}</span>
        </NFlex>
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
/* 面包屑条目基础样式 */
.crumb-item {
  height: 22px;
  padding: 0 4px;
  border-radius: 4px;
  font-size: 13px;
  line-height: 1;
  white-space: nowrap;
  animation: crumb-enter 0.3s cubic-bezier(0.76, 0, 0.24, 1);
  transition:
    color 0.2s ease,
    background 0.15s ease;
}

/* 可点击项：muted 色 + hover 效果 */
.crumb-item--link {
  color: var(--n-item-text-color);
  cursor: pointer;
}

.crumb-item--link:hover {
  color: var(--n-item-text-color-hover);
  background: hsl(var(--accent) / 0.6);
}

/* 当前页（最后一项）：前景色，不可点击 */
.crumb-item--active {
  color: var(--n-text-color);
  font-weight: 500;
  cursor: default;
  pointer-events: none;
}

/* 分隔符图标 */
.crumb-sep {
  opacity: 0.4;
}

/* 进入动画（对标 vben breadcrumb-transition） */
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
