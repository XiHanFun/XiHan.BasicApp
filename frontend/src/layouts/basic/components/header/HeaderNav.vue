<script setup lang="ts">
import type { DropdownOption, MenuOption } from 'naive-ui'
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NBreadcrumb, NBreadcrumbItem, NButton, NDropdown, NFlex, NIcon, NMenu, NTag } from 'naive-ui'

defineOptions({ name: 'HeaderNav' })

const props = defineProps<HeaderNavProps>()

const emit = defineEmits<{
  sidebarToggle: []
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
</script>

<template>
  <div
    class="flex min-w-0 flex-1 items-center gap-2"
    :class="props.appStore.headerMenuAlign === 'center' ? 'mx-auto' : ''"
  >
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
            :icon="
              props.appStore.sidebarCollapsed ? 'lucide:panel-left-open' : 'lucide:panel-left-close'
            "
            width="18"
          />
        </NIcon>
      </template>
    </NButton>

    <NBreadcrumb
      v-if="
        props.appStore.breadcrumbEnabled
          && !props.showTopMenu
          && !(props.appStore.breadcrumbHideOnlyOne && props.breadcrumbs.length <= 1 && !props.appStore.breadcrumbShowHome)
      "
      separator=">"
      class="hidden sm:flex"
      :class="
        props.appStore.breadcrumbStyle === 'background'
          ? 'rounded-md bg-[hsl(var(--muted))] px-2 py-1'
          : ''
      "
    >
      <NBreadcrumbItem v-if="props.appStore.breadcrumbShowHome">
        <NFlex align="center" :size="6" class="cursor-pointer" @click="emit('homeClick')">
          <NIcon size="14"><Icon icon="lucide:house" /></NIcon>
          <span>Home</span>
        </NFlex>
      </NBreadcrumbItem>
      <NBreadcrumbItem v-for="item in props.breadcrumbs" :key="item.path">
        <NDropdown
          v-if="item.siblings.length > 1"
          :options="item.siblings"
          @select="key => emit('breadcrumbSelect', String(key))"
        >
          <NTag size="small" round :bordered="false" class="cursor-pointer">
            <NFlex align="center" :size="6">
              <NIcon v-if="props.appStore.breadcrumbShowIcon && item.icon" size="14">
                <Icon :icon="item.icon!" />
              </NIcon>
              <span>{{ item.title }}</span>
            </NFlex>
          </NTag>
        </NDropdown>
        <NFlex v-else align="center" :size="6" class="rounded px-1.5 py-0.5">
          <NIcon v-if="props.appStore.breadcrumbShowIcon && item.icon" size="14">
            <Icon :icon="item.icon!" />
          </NIcon>
          <span>{{ item.title }}</span>
        </NFlex>
      </NBreadcrumbItem>
    </NBreadcrumb>

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
