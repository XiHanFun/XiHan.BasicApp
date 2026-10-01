<script setup lang="ts">
import type { LayoutBreadcrumbItem } from '../../contracts'
import type { useAppStore } from '~/stores'
import { XhBreadcrumbItem, XhBreadcrumbLink, XhBreadcrumbList, XhBreadcrumbRoot, XhBreadcrumbSeparator } from '@xihan-ui/vue'
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { XDropdown } from '~/components'
import { Icon } from '~/iconify'

defineOptions({ name: 'HeaderNav' })

const props = defineProps<{
  appStore: ReturnType<typeof useAppStore>
  breadcrumbs: LayoutBreadcrumbItem[]
}>()

/** 只剩同级下拉的选中要交出去：首页与各层链接是 RouterLink，自己跳转 */
const emit = defineEmits<{
  breadcrumbSelect: [path: string]
}>()

const allCrumbs = computed(() => {
  const result: Array<{ key: string, isHome?: boolean, index?: number }> = []
  if (props.appStore.breadcrumbShowHome)
    result.push({ key: 'home', isHome: true })
  props.breadcrumbs.forEach((_, i) => result.push({ key: String(i), index: i }))
  return result
})

const shouldShowBreadcrumb = computed(() => {
  // 没有任何面包屑项时不渲染
  if (allCrumbs.value.length === 0)
    return false
  if (props.appStore.breadcrumbHideOnlyOne && allCrumbs.value.length <= 1)
    return false
  return true
})

function resolveIcon(icon: string) {
  if (!icon)
    return icon
  return icon.includes(':') ? icon : `lucide:${icon}`
}

function isLast(isHome: boolean, index?: number): boolean {
  if (isHome)
    return props.breadcrumbs.length === 0
  return index === props.breadcrumbs.length - 1
}
</script>

<template>
  <!-- 分隔符在组件库里是独立部件，摆在两项之间；旧版靠每项的 #separator 插槽给，语义相同 -->
  <XhBreadcrumbRoot
    v-if="shouldShowBreadcrumb"
    class="flex min-w-0 items-center"
    :class="appStore.breadcrumbStyle === 'background' ? 'rounded-md bg-muted px-2 py-1' : ''"
  >
    <!-- 顶栏只有一行：皮肤缺省 flex-wrap 会在顶栏挤压时把路径折成两行，这里改为不换行，
         各项按比例压缩并在文字上出省略号（根与列表都要 min-w-0 才压得动） -->
    <XhBreadcrumbList class="flex min-w-0 flex-nowrap items-center">
      <!-- 可去的层级是真链接：link 部件 as-child 借 RouterLink 渲出带 href 的 <a>，跳转交给路由，
           Tab 可达、能新标签打开；当前页那条由部件对点击 preventDefault、退出 Tab 序列 -->
      <XhBreadcrumbItem v-if="appStore.breadcrumbShowHome">
        <XhBreadcrumbLink as-child :current="isLast(true)">
          <RouterLink
            to="/"
            class="crumb-item"
            :class="isLast(true) ? 'crumb-item--active' : 'crumb-item--link'"
          >
            <Icon
              v-if="appStore.breadcrumbShowIcon"
              icon="lucide:house"
              width="14"
              height="14"
              class="crumb-icon"
            />
            <span class="crumb-label">Home</span>
          </RouterLink>
        </XhBreadcrumbLink>
      </XhBreadcrumbItem>
      <XhBreadcrumbSeparator v-if="appStore.breadcrumbShowHome && !isLast(true)">
        <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
      </XhBreadcrumbSeparator>

      <template v-for="(item, index) in breadcrumbs" :key="item.path">
        <XhBreadcrumbItem>
          <!-- 有同级去处：点它出下拉，可横向跳到兄弟节点。它是菜单按钮而不是链接，借真 <button> 天然可聚焦；
               当前页那条也要能用键盘展开，作者属性压掉链接部件给当前页的 aria-disabled 与 tabindex=-1，
               也不挂 crumb-item--active（那条的 pointer-events: none 会让鼠标点不开） -->
          <XDropdown
            v-if="item.siblings.length > 1"
            :options="item.siblings"
            placement="bottom-start"
            @select="(key: string) => emit('breadcrumbSelect', key)"
          >
            <XhBreadcrumbLink
              as-child
              :current="isLast(false, index)"
              aria-disabled="false"
              :tabindex="0"
            >
              <button type="button" class="crumb-item crumb-item--link">
                <Icon
                  v-if="appStore.breadcrumbShowIcon && item.icon"
                  :icon="resolveIcon(item.icon!)"
                  width="14"
                  height="14"
                  class="crumb-icon"
                />
                <span class="crumb-label">{{ item.title }}</span>
              </button>
            </XhBreadcrumbLink>
          </XDropdown>

          <XhBreadcrumbLink v-else as-child :current="isLast(false, index)">
            <RouterLink
              :to="item.path"
              class="crumb-item"
              :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
            >
              <Icon
                v-if="appStore.breadcrumbShowIcon && item.icon"
                :icon="resolveIcon(item.icon!)"
                width="14"
                height="14"
                class="crumb-icon"
              />
              <span class="crumb-label">{{ item.title }}</span>
            </RouterLink>
          </XhBreadcrumbLink>
        </XhBreadcrumbItem>
        <XhBreadcrumbSeparator v-if="!isLast(false, index)">
          <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
        </XhBreadcrumbSeparator>
      </template>
    </XhBreadcrumbList>
  </XhBreadcrumbRoot>
</template>

<style scoped>
.crumb-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 14px;
  line-height: 20px;
  white-space: nowrap;
  transition:
    color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.crumb-item--link {
  cursor: pointer;
}

.crumb-item--active {
  font-weight: 500;
  cursor: default;
  pointer-events: none;
}

.crumb-icon {
  flex-shrink: 0;
}

/* 截断落在文字上：链接自身是 flex 容器，text-overflow 在它上面不生效 */
.crumb-label {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.crumb-sep {
  display: block;
  flex-shrink: 0;
  opacity: 0.4;
}
</style>
