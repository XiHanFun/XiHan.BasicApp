<script setup lang="ts">
import type { useAppStore } from '~/stores'
import {
  NCard,
  NInput,
  NInputNumber,
  NRadioButton,
  NRadioGroup,
  NSelect,
  NSpace,
  NSwitch,
} from 'naive-ui'
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

const tabbarStyleOptions = [
  { label: '谷歌', value: 'chrome' },
  { label: '简约', value: 'plain' },
  { label: '圆润', value: 'rounded' },
]

const preferencePositionOptions = [
  { label: '自动', value: 'auto' },
  { label: '左侧固定', value: 'fixed-left' },
  { label: '右侧固定', value: 'fixed-right' },
]
</script>

<template>
  <div class="space-y-4">
    <!-- 布局 -->
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

    <!-- 内容 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        内容
      </div>
      <div class="grid grid-cols-2 gap-2">
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
    </NCard>

    <!-- 侧边栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        侧边栏
      </div>
      <div class="pref-row">
        <span>显示侧边栏</span>
        <NSwitch v-model:value="appStore.sidebarShow" />
      </div>
      <div class="pref-row">
        <span>折叠菜单</span>
        <NSwitch v-model:value="appStore.sidebarCollapsed" />
      </div>
      <div class="pref-row">
        <span :class="{ 'text-[hsl(var(--muted-foreground))]': !appStore.sidebarCollapsed }">
          鼠标悬停展开
        </span>
        <NSwitch
          v-model:value="appStore.sidebarExpandOnHover"
          :disabled="!appStore.sidebarCollapsed"
        />
      </div>
      <div class="pref-row">
        <span>折叠显示菜单名</span>
        <NSwitch v-model:value="appStore.sidebarCollapsedShowTitle" />
      </div>
      <div class="pref-row">
        <span :class="{ 'text-[hsl(var(--muted-foreground))]': !appStore.sidebarCollapsed }">
          自动激活子菜单
        </span>
        <NSwitch
          v-model:value="appStore.sidebarAutoActivateChild"
          :disabled="!appStore.sidebarCollapsed"
        />
      </div>
      <div class="pref-row">
        <span>显示按钮</span>
        <div class="flex gap-1">
          <button
            type="button"
            class="btn-toggle"
            :class="{ 'is-active': appStore.sidebarCollapseButton }"
            @click="appStore.sidebarCollapseButton = !appStore.sidebarCollapseButton"
          >
            折叠按钮
          </button>
          <button
            type="button"
            class="btn-toggle"
            :class="{ 'is-active': appStore.sidebarFixedButton }"
            @click="appStore.sidebarFixedButton = !appStore.sidebarFixedButton"
          >
            固定按钮
          </button>
        </div>
      </div>
      <div class="pref-row">
        <span>宽度</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            v-model:value="appStore.sidebarWidth"
            :min="180"
            :max="320"
            size="small"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 130px"
          />
          <span class="unit-label">px</span>
        </div>
      </div>
    </NCard>

    <!-- 顶栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        顶栏
      </div>
      <div class="pref-row">
        <span>显示顶栏</span>
        <NSwitch v-model:value="appStore.headerShow" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.headerShow }">
        <span>模式</span>
        <NRadioGroup v-model:value="appStore.headerMode" size="small" :disabled="!appStore.headerShow">
          <NSpace :size="0">
            <NRadioButton value="fixed">
              固定
            </NRadioButton>
            <NRadioButton value="static">
              静态
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.headerShow }">
        <span>菜单位置</span>
        <NRadioGroup v-model:value="appStore.headerMenuAlign" size="small" :disabled="!appStore.headerShow">
          <NSpace :size="0">
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

    <!-- 导航菜单 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        导航菜单
      </div>
      <div class="pref-row">
        <span>导航菜单风格</span>
        <NRadioGroup v-model:value="appStore.navigationStyle" size="small">
          <NSpace :size="0">
            <NRadioButton value="rounded">
              圆润
            </NRadioButton>
            <NRadioButton value="plain">
              朴素
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
      <div class="pref-row">
        <span>导航菜单分离</span>
        <NSwitch v-model:value="appStore.navigationSplit" />
      </div>
      <div class="pref-row">
        <span>侧边导航菜单手风琴模式</span>
        <NSwitch v-model:value="appStore.navigationAccordion" />
      </div>
    </NCard>

    <!-- 面包屑导航 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        面包屑导航
      </div>
      <div class="pref-row">
        <span>开启面包屑导航</span>
        <NSwitch v-model:value="appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>仅有一个时隐藏</span>
        <NSwitch v-model:value="appStore.breadcrumbHideOnlyOne" :disabled="!appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>显示面包屑图标</span>
        <NSwitch v-model:value="appStore.breadcrumbShowIcon" :disabled="!appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>显示首页按钮</span>
        <NSwitch v-model:value="appStore.breadcrumbShowHome" :disabled="!appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>面包屑风格</span>
        <NRadioGroup v-model:value="appStore.breadcrumbStyle" size="small" :disabled="!appStore.breadcrumbEnabled">
          <NSpace :size="0">
            <NRadioButton value="normal">
              常规
            </NRadioButton>
            <NRadioButton value="background">
              背景
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
    </NCard>

    <!-- 标签栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        标签栏
      </div>
      <div class="pref-row">
        <span>启用标签栏</span>
        <NSwitch v-model:value="appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>持久化标签页</span>
        <NSwitch v-model:value="appStore.tabbarPersist" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>访问历史记录</span>
        <NSwitch v-model:value="appStore.tabbarVisitHistory" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>最大标签数</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            v-model:value="appStore.tabbarMaxCount"
            :min="0"
            :max="30"
            size="small"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 120px"
            :disabled="!appStore.tabbarEnabled"
          />
          <span class="unit-label">个</span>
        </div>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>启用拖拽排序</span>
        <NSwitch v-model:value="appStore.tabbarDraggable" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>启用纵向滚轮响应</span>
        <NSwitch v-model:value="appStore.tabbarScrollResponse" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>点击鼠标中键关闭标签页</span>
        <NSwitch v-model:value="appStore.tabbarMiddleClickClose" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>显示标签栏图标</span>
        <NSwitch v-model:value="appStore.tabbarShowIcon" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>显示更多按钮</span>
        <NSwitch v-model:value="appStore.tabbarShowMore" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>显示最大化按钮</span>
        <NSwitch v-model:value="appStore.tabbarShowMaximize" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>标签页风格</span>
        <NSelect
          v-model:value="appStore.tabbarStyle"
          :options="tabbarStyleOptions"
          size="small"
          style="width: 100px"
          :disabled="!appStore.tabbarEnabled"
        />
      </div>
    </NCard>

    <!-- 小部件 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        小部件
      </div>
      <div class="pref-row">
        <span>启用全局搜索</span>
        <NSwitch v-model:value="appStore.searchEnabled" />
      </div>
      <div class="pref-row">
        <span>启用主题切换</span>
        <NSwitch v-model:value="appStore.widgetThemeToggle" />
      </div>
      <div class="pref-row">
        <span>启用语言切换</span>
        <NSwitch v-model:value="appStore.widgetLanguageToggle" />
      </div>
      <div class="pref-row">
        <span>启用全屏</span>
        <NSwitch v-model:value="appStore.widgetFullscreen" />
      </div>
      <div class="pref-row">
        <span>启用通知</span>
        <NSwitch v-model:value="appStore.widgetNotification" />
      </div>
      <div class="pref-row">
        <span>启用锁屏</span>
        <NSwitch v-model:value="appStore.widgetLockScreen" />
      </div>
      <div class="pref-row">
        <span>启用侧栏切换</span>
        <NSwitch v-model:value="appStore.widgetSidebarToggle" />
      </div>
      <div class="pref-row">
        <span>启用刷新</span>
        <NSwitch v-model:value="appStore.widgetRefresh" />
      </div>
      <div class="pref-row">
        <span>偏好设置位置</span>
        <NSelect
          v-model:value="appStore.widgetPreferencePosition"
          :options="preferencePositionOptions"
          size="small"
          style="width: 110px"
        />
      </div>
    </NCard>

    <!-- 底栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        底栏
      </div>
      <div class="pref-row">
        <span>显示底栏</span>
        <NSwitch v-model:value="appStore.footerEnable" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.footerEnable }">
        <span>固定在底部</span>
        <NSwitch v-model:value="appStore.footerFixed" :disabled="!appStore.footerEnable" />
      </div>
    </NCard>

    <!-- 版权 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        版权
      </div>
      <div class="pref-row">
        <span>启用版权</span>
        <NSwitch v-model:value="appStore.copyrightEnable" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>公司名</span>
        <NInput
          v-model:value="appStore.copyrightName"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>公司主页</span>
        <NInput
          v-model:value="appStore.copyrightSite"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>日期</span>
        <NInput
          v-model:value="appStore.copyrightDate"
          size="small"
          style="width: 90px"
          :input-props="{ style: 'text-align: right' }"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>ICP 备案号</span>
        <NInput
          v-model:value="appStore.copyrightIcp"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          placeholder="选填"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>ICP 网站链接</span>
        <NInput
          v-model:value="appStore.copyrightIcpUrl"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          placeholder="选填"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.section-title {
  margin-bottom: 4px;
  padding: 0 6px;
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

/* 折叠/固定按钮的多选 toggle */
.btn-toggle {
  padding: 3px 10px;
  font-size: 12px;
  border-radius: 4px;
  border: 1.5px solid hsl(var(--border));
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-toggle:hover {
  border-color: hsl(var(--primary) / 0.5);
  color: hsl(var(--foreground));
}

.btn-toggle.is-active {
  border-color: hsl(var(--primary));
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  font-weight: 500;
}
</style>
