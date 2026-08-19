<script setup lang="ts">
import type { LayoutBreadcrumbItem } from '../../contracts'
import type { useAppStore } from '~/stores'
import { XhBreadcrumbItem, XhBreadcrumbLink, XhBreadcrumbList, XhBreadcrumbRoot, XhBreadcrumbSeparator } from '@xihan-ui/vue'
import { computed } from 'vue'
import { XDropdown } from '~/components'
import { Icon } from '~/iconify'

defineOptions({ name: 'HeaderNav' })

const props = defineProps<{
  appStore: ReturnType<typeof useAppStore>
  breadcrumbs: LayoutBreadcrumbItem[]
}>()

const emit = defineEmits<{
  breadcrumbSelect: [path: string]
  homeClick: []
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
    class="flex items-center"
    :class="appStore.breadcrumbStyle === 'background' ? 'rounded-md bg-muted px-2 py-1' : ''"
  >
    <XhBreadcrumbList class="flex items-center">
      <XhBreadcrumbItem v-if="appStore.breadcrumbShowHome">
        <XhBreadcrumbLink
          class="crumb-item"
          :class="isLast(true) ? 'crumb-item--active' : 'crumb-item--link'"
          :current="isLast(true)"
          :tabindex="isLast(true) ? undefined : 0"
          @click="!isLast(true) && emit('homeClick')"
          @keydown.enter="!isLast(true) && emit('homeClick')"
        >
          <Icon
            v-if="appStore.breadcrumbShowIcon"
            icon="lucide:house"
            width="14"
            height="14"
            class="crumb-icon"
          />
          <span>Home</span>
        </XhBreadcrumbLink>
      </XhBreadcrumbItem>
      <XhBreadcrumbSeparator v-if="appStore.breadcrumbShowHome && !isLast(true)">
        <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
      </XhBreadcrumbSeparator>

      <template v-for="(item, index) in breadcrumbs" :key="item.path">
        <XhBreadcrumbItem>
          <!-- 有同级去处：点它出下拉，可横向跳到兄弟节点 -->
          <XDropdown
            v-if="item.siblings.length > 1"
            :options="item.siblings"
            placement="bottom-start"
            @select="(key: string) => emit('breadcrumbSelect', key)"
          >
            <XhBreadcrumbLink
              class="crumb-item"
              :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
              :current="isLast(false, index)"
              :tabindex="isLast(false, index) ? undefined : 0"
            >
              <Icon
                v-if="appStore.breadcrumbShowIcon && item.icon"
                :icon="resolveIcon(item.icon!)"
                width="14"
                height="14"
                class="crumb-icon"
              />
              <span>{{ item.title }}</span>
            </XhBreadcrumbLink>
          </XDropdown>

          <XhBreadcrumbLink
            v-else
            class="crumb-item"
            :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
            :current="isLast(false, index)"
            :tabindex="isLast(false, index) ? undefined : 0"
            @click="!isLast(false, index) && emit('breadcrumbSelect', item.path)"
            @keydown.enter="!isLast(false, index) && emit('breadcrumbSelect', item.path)"
          >
            <Icon
              v-if="appStore.breadcrumbShowIcon && item.icon"
              :icon="resolveIcon(item.icon!)"
              width="14"
              height="14"
              class="crumb-icon"
            />
            <span>{{ item.title }}</span>
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
    color 0.2s ease,
    background 0.15s ease;
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

.crumb-sep {
  display: block;
  flex-shrink: 0;
  opacity: 0.4;
}
</style>
