<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NCard, NColorPicker, NInputNumber, NRadioButton, NRadioGroup, NSpace } from 'naive-ui'

defineOptions({ name: 'PreferenceAppearanceTab' })

const props = defineProps<PreferenceAppearanceTabProps>()
const emit = defineEmits<{
  themeModeChange: [value: 'light' | 'dark' | 'auto']
}>()

const appStore = props.appStore

interface PreferenceAppearanceTabProps {
  appStore: ReturnType<typeof useAppStore>
  themeMode: string
  themePresets: string[]
}
</script>

<template>
  <div class="space-y-5">
    <NCard size="small">
      <div class="mb-2 font-medium">
        主题
      </div>
      <NRadioGroup
        :value="props.themeMode"
        @update:value="value => emit('themeModeChange', value as 'light' | 'dark' | 'auto')"
      >
        <NSpace>
          <NRadioButton value="light">
            浅色
          </NRadioButton>
          <NRadioButton value="dark">
            深色
          </NRadioButton>
          <NRadioButton value="auto">
            跟随系统
          </NRadioButton>
        </NSpace>
      </NRadioGroup>
    </NCard>

    <NCard size="small">
      <div class="mb-2 font-medium">
        内置主题
      </div>
      <div class="grid grid-cols-4 gap-2">
        <button
          v-for="color in props.themePresets"
          :key="color"
          type="button"
          class="relative h-10 rounded border"
          :class="
            appStore.themeColor === color
              ? 'border-blue-500 ring-2 ring-blue-200'
              : 'border-gray-200'
          "
          :style="{ backgroundColor: color }"
          @click="appStore.setThemeColor(color)"
        >
          <Icon
            v-if="appStore.themeColor === color"
            icon="lucide:check"
            width="14"
            class="absolute right-1 top-1 text-white"
          />
        </button>
      </div>
      <div class="mt-2">
        <NColorPicker
          :value="appStore.themeColor"
          :modes="['hex']"
          @update:value="value => appStore.setThemeColor(value)"
        />
      </div>
    </NCard>

    <NCard size="small">
      <div class="mb-2 font-medium">
        圆角
      </div>
      <NRadioGroup :value="appStore.uiRadius" @update:value="value => appStore.setUiRadius(value)">
        <NSpace>
          <NRadioButton :value="0">
            0
          </NRadioButton>
          <NRadioButton :value="0.25">
            0.25
          </NRadioButton>
          <NRadioButton :value="0.5">
            0.5
          </NRadioButton>
          <NRadioButton :value="0.75">
            0.75
          </NRadioButton>
          <NRadioButton :value="1">
            1
          </NRadioButton>
        </NSpace>
      </NRadioGroup>
    </NCard>

    <NCard size="small">
      <div class="mb-2 font-medium">
        字体大小
      </div>
      <NInputNumber
        :value="appStore.fontSize"
        :min="12"
        :max="20"
        @update:value="value => value !== null && appStore.setFontSize(value)"
      />
    </NCard>
  </div>
</template>
