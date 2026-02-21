<script setup lang="ts">
import { computed, ref } from 'vue'
import { Icon } from '@iconify/vue'
import { NButton, NColorPicker, NDrawer, NDrawerContent, NRadioButton, NRadioGroup, NSpace, NSwitch } from 'naive-ui'
import { LAYOUT_MODE_OPTIONS } from '~/constants'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppPreferenceDrawer' })

const appStore = useAppStore()
const visible = ref(false)

const themeMode = computed({
  get: () => appStore.themeMode,
  set: (value: 'light' | 'dark' | 'auto') => appStore.setTheme(value),
})

const layoutMode = computed({
  get: () => appStore.layoutMode,
  set: (value: string) => appStore.setLayoutMode(value),
})

const themeColor = computed({
  get: () => appStore.themeColor,
  set: (value: string) => appStore.setThemeColor(value),
})

const tabbarEnabled = computed({
  get: () => appStore.tabbarEnabled,
  set: (value: boolean) => appStore.setTabbarEnabled(value),
})

const breadcrumbEnabled = computed({
  get: () => appStore.breadcrumbEnabled,
  set: (value: boolean) => appStore.setBreadcrumbEnabled(value),
})

const searchEnabled = computed({
  get: () => appStore.searchEnabled,
  set: (value: boolean) => appStore.setSearchEnabled(value),
})

const themeAnimationEnabled = computed({
  get: () => appStore.themeAnimationEnabled,
  set: (value: boolean) => appStore.setThemeAnimationEnabled(value),
})
</script>

<template>
  <NButton quaternary circle size="small" @click="visible = true">
    <template #icon>
      <Icon icon="lucide:settings-2" width="18" />
    </template>
  </NButton>

  <NDrawer v-model:show="visible" :width="360" placement="right">
    <NDrawerContent title="界面偏好" closable>
      <div class="space-y-6">
        <div>
          <div class="mb-2 text-sm text-gray-500">主题模式</div>
          <NRadioGroup v-model:value="themeMode">
            <NSpace>
              <NRadioButton value="light">浅色</NRadioButton>
              <NRadioButton value="dark">深色</NRadioButton>
              <NRadioButton value="auto">跟随系统</NRadioButton>
            </NSpace>
          </NRadioGroup>
        </div>

        <div>
          <div class="mb-2 text-sm text-gray-500">布局模式</div>
          <NRadioGroup v-model:value="layoutMode">
            <NSpace vertical>
              <NRadioButton
                v-for="item in LAYOUT_MODE_OPTIONS"
                :key="item.value"
                :value="item.value"
              >
                {{ item.label }}
              </NRadioButton>
            </NSpace>
          </NRadioGroup>
        </div>

        <div>
          <div class="mb-2 text-sm text-gray-500">主题色</div>
          <NColorPicker v-model:value="themeColor" :modes="['hex']" />
        </div>

        <div class="flex items-center justify-between">
          <span class="text-sm text-gray-600 dark:text-gray-300">显示标签页</span>
          <NSwitch v-model:value="tabbarEnabled" />
        </div>
        <div class="flex items-center justify-between">
          <span class="text-sm text-gray-600 dark:text-gray-300">显示面包屑</span>
          <NSwitch v-model:value="breadcrumbEnabled" />
        </div>
        <div class="flex items-center justify-between">
          <span class="text-sm text-gray-600 dark:text-gray-300">显示顶部搜索</span>
          <NSwitch v-model:value="searchEnabled" />
        </div>
        <div class="flex items-center justify-between">
          <span class="text-sm text-gray-600 dark:text-gray-300">主题切换动画</span>
          <NSwitch v-model:value="themeAnimationEnabled" />
        </div>
      </div>
    </NDrawerContent>
  </NDrawer>
</template>
