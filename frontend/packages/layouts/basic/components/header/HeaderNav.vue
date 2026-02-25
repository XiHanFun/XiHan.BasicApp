<script setup lang="ts">
import type { LayoutBreadcrumbItem } from '../../contracts'
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NBreadcrumb, NBreadcrumbItem, NDropdown } from 'naive-ui'
import { computed } from 'vue'

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

function isLast(isHome: boolean, index?: number): boolean {
  if (isHome)
    return props.breadcrumbs.length === 0
  return index === props.breadcrumbs.length - 1
}
</script>

<template>
  <NBreadcrumb
    v-if="!(appStore.breadcrumbHideOnlyOne && allCrumbs.length <= 1)"
    class="flex items-center"
    :class="appStore.breadcrumbStyle === 'background' ? 'rounded-md bg-muted px-2 py-1' : ''"
  >
    <NBreadcrumbItem v-if="appStore.breadcrumbShowHome">
      <template v-if="!isLast(true)" #separator>
        <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
      </template>
      <div
        class="crumb-item"
        :class="isLast(true) ? 'crumb-item--active' : 'crumb-item--link'"
        @click="!isLast(true) && emit('homeClick')"
      >
        <Icon
          v-if="appStore.breadcrumbShowIcon"
          icon="lucide:house"
          width="14"
          height="14"
          class="crumb-icon"
        />
        <span>Home</span>
      </div>
    </NBreadcrumbItem>

    <NBreadcrumbItem
      v-for="(item, index) in breadcrumbs"
      :key="item.path"
    >
      <template v-if="!isLast(false, index)" #separator>
        <Icon icon="lucide:chevron-right" width="12" height="12" class="crumb-sep" />
      </template>

      <NDropdown
        v-if="item.siblings.length > 1"
        :options="item.siblings"
        @select="(key: string) => emit('breadcrumbSelect', String(key))"
      >
        <div
          class="crumb-item"
          :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
        >
          <Icon
            v-if="appStore.breadcrumbShowIcon && item.icon"
            :icon="item.icon!"
            width="14"
            height="14"
            class="crumb-icon"
          />
          <span>{{ item.title }}</span>
        </div>
      </NDropdown>

      <div
        v-else
        class="crumb-item"
        :class="isLast(false, index) ? 'crumb-item--active' : 'crumb-item--link'"
        @click="!isLast(false, index) && emit('breadcrumbSelect', item.path)"
      >
        <Icon
          v-if="appStore.breadcrumbShowIcon && item.icon"
          :icon="item.icon!"
          width="14"
          height="14"
          class="crumb-icon"
        />
        <span>{{ item.title }}</span>
      </div>
    </NBreadcrumbItem>
  </NBreadcrumb>
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
