<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NInputNumber, NRadioButton, NRadioGroup, NSlider, NSpace, NSwitch } from 'naive-ui'
import LayoutPreviewSvg from './LayoutPreviewSvg.vue'

defineOptions({ name: 'PreferenceLayoutTab' })

const props = defineProps<PreferenceLayoutTabProps>()

const emit = defineEmits<{
  layoutModeChange: [value: string]
  contentModeChange: [value: 'fixed' | 'fluid']
}>()

interface LayoutPreset {
  key: string
  label: string
}

interface PreferenceLayoutTabProps {
  appStore: ReturnType<typeof useAppStore>
  layoutMode: string
  contentMode: 'fixed' | 'fluid'
  layoutPresets: ReadonlyArray<LayoutPreset>
}

const appStore = props.appStore
</script>

<template>
  <div class="space-y-4">
    <NCard size="small" :bordered="false">
      <div class="section-title">
        布局
      </div>
      <div class="grid grid-cols-3 gap-2">
        <button
          v-for="item in props.layoutPresets"
          :key="item.key"
          type="button"
          class="layout-preset-card"
          :class="props.layoutMode === item.key ? 'is-active' : ''"
          @click="emit('layoutModeChange', item.key)"
        >
          <div class="layout-preset-preview">
            <LayoutPreviewSvg :type="item.key" />
          </div>
          <div class="mt-1.5 text-xs">
            {{ item.label }}
          </div>
        </button>
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        内容
      </div>
      <div class="mb-3 grid grid-cols-2 gap-2">
        <button
          type="button"
          class="layout-preset-card"
          :class="props.contentMode === 'fluid' ? 'is-active' : ''"
          @click="emit('contentModeChange', 'fluid')"
        >
          <div class="layout-preset-preview">
            <LayoutPreviewSvg type="content-fluid" />
          </div>
          <div class="mt-1.5 text-xs">
            流式
          </div>
        </button>
        <button
          type="button"
          class="layout-preset-card"
          :class="props.contentMode === 'fixed' ? 'is-active' : ''"
          @click="emit('contentModeChange', 'fixed')"
        >
          <div class="layout-preset-preview">
            <LayoutPreviewSvg type="content-fixed" />
          </div>
          <div class="mt-1.5 text-xs">
            定宽
          </div>
        </button>
      </div>
      <div class="mb-1 text-xs font-medium text-[hsl(var(--muted-foreground))]">
        内容最大宽度
      </div>
      <NSlider v-model:value="appStore.contentMaxWidth" :min="960" :max="1600" :step="10" />
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        顶栏
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>模式</span>
        <NRadioGroup v-model:value="appStore.headerMode">
          <NSpace>
            <NRadioButton value="fixed">
              固定
            </NRadioButton>
            <NRadioButton value="static">
              静态
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
      <div class="flex items-center justify-between">
        <span>菜单位置</span>
        <NRadioGroup v-model:value="appStore.headerMenuAlign">
          <NSpace>
            <NRadioButton value="left">
              左侧
            </NRadioButton>
            <NRadioButton value="center">
              居中
            </NRadioButton>
            <NRadioButton value="right">
              右侧
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        导航菜单
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>导航菜单风格</span>
        <NRadioGroup v-model:value="appStore.navigationStyle">
          <NSpace>
            <NRadioButton value="rounded">
              圆润
            </NRadioButton>
            <NRadioButton value="plain">
              朴素
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
      <div class="flex items-center justify-between">
        <span>导航菜单分离</span>
        <NSwitch v-model:value="appStore.navigationSplit" />
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        侧边栏
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>显示侧边栏</span>
        <NSwitch v-model:value="appStore.sidebarShow" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>折叠菜单</span>
        <NSwitch v-model:value="appStore.sidebarCollapsed" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>鼠标悬停展开</span>
        <NSwitch v-model:value="appStore.sidebarExpandOnHover" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>自动激活子菜单</span>
        <NSwitch v-model:value="appStore.sidebarAutoActivateChild" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>显示折叠按钮</span>
        <NSwitch v-model:value="appStore.sidebarCollapseButton" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>折叠显示菜单名</span>
        <NSwitch v-model:value="appStore.sidebarCollapsedShowTitle" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>固定按钮</span>
        <NSwitch v-model:value="appStore.sidebarFixedButton" />
      </div>
      <div class="flex items-center justify-between">
        <span>侧边栏宽度</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            v-model:value="appStore.sidebarWidth"
            :min="180"
            :max="320"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 130px"
          />
          <span class="unit-label">px</span>
        </div>
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        面包屑导航
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>开启面包屑</span>
        <NSwitch v-model:value="appStore.breadcrumbEnabled" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>仅有一个时隐藏</span>
        <NSwitch v-model:value="appStore.breadcrumbHideOnlyOne" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>显示面包屑图标</span>
        <NSwitch v-model:value="appStore.breadcrumbShowIcon" />
      </div>
      <div class="flex items-center justify-between">
        <span>显示首页按钮</span>
        <NSwitch v-model:value="appStore.breadcrumbShowHome" />
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        标签栏
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用标签栏</span>
        <NSwitch v-model:value="appStore.tabbarEnabled" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>持久化标签页</span>
        <NSwitch v-model:value="appStore.tabbarPersist" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>访问历史记录</span>
        <NSwitch v-model:value="appStore.tabbarVisitHistory" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>最大标签数</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            v-model:value="appStore.tabbarMaxCount"
            :min="0"
            :max="30"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 120px"
          />
          <span class="unit-label">个</span>
        </div>
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用拖拽排序</span>
        <NSwitch v-model:value="appStore.tabbarDraggable" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>显示更多按钮</span>
        <NSwitch v-model:value="appStore.tabbarShowMore" />
      </div>
      <div class="flex items-center justify-between">
        <span>显示最大化按钮</span>
        <NSwitch v-model:value="appStore.tabbarShowMaximize" />
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.section-title {
  margin-bottom: 10px;
  font-weight: 600;
  font-size: 13px;
  color: hsl(var(--foreground));
}

.unit-label {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  flex-shrink: 0;
}

.layout-preset-card {
  border: 1.5px solid hsl(var(--border));
  border-radius: 10px;
  background: hsl(var(--card));
  color: hsl(var(--foreground));
  padding: 8px;
  text-align: center;
  transition: all 0.2s ease;
}

.layout-preset-card:hover {
  transform: translateY(-1px);
  border-color: color-mix(in srgb, hsl(var(--primary)) 45%, hsl(var(--border)));
  background: hsl(var(--accent));
}

.layout-preset-card.is-active {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 2px color-mix(in srgb, hsl(var(--primary)) 30%, transparent);
}

.layout-preset-preview {
  display: flex;
  height: 64px;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  overflow: hidden;
  background: hsl(var(--background));
}
</style>
