<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NInput, NSwitch } from 'naive-ui'

defineOptions({ name: 'PreferenceGeneralTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore

const transitionItems = [
  { value: 'fade', label: '淡入淡出' },
  { value: 'slide-left', label: '左右滑动' },
  { value: 'slide-up', label: '向上滑入' },
  { value: 'slide-down', label: '向下滑入' },
]
</script>

<template>
  <div class="space-y-4">
    <NCard size="small" :bordered="false">
      <div class="section-title">
        通用
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用全局搜索</span>
        <NSwitch v-model:value="appStore.searchEnabled" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>动态标题</span>
        <NSwitch v-model:value="appStore.dynamicTitle" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>水印</span>
        <NSwitch v-model:value="appStore.watermarkEnabled" />
      </div>
      <NInput
        v-if="appStore.watermarkEnabled"
        v-model:value="appStore.watermarkText"
        class="mb-2"
        placeholder="水印文案"
      />
      <div class="mb-2 flex items-center justify-between">
        <span>定时检查更新</span>
        <NSwitch v-model:value="appStore.enableCheckUpdates" />
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        动画
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>主题切换动画</span>
        <NSwitch v-model:value="appStore.themeAnimationEnabled" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>页面切换动画</span>
        <NSwitch v-model:value="appStore.transitionEnable" />
      </div>
      <div class="transition-grid">
        <div
          v-for="item in transitionItems"
          :key="item.value"
          class="transition-item"
          :class="{ 'is-active': appStore.transitionName === item.value }"
          @click="appStore.transitionName = item.value"
        >
          <div class="transition-preview">
            <div class="preview-block" :class="`anim-${item.value}`" />
          </div>
          <span class="item-label">{{ item.label }}</span>
        </div>
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        小部件
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用主题切换</span>
        <NSwitch v-model:value="appStore.widgetThemeToggle" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用语言切换</span>
        <NSwitch v-model:value="appStore.widgetLanguageToggle" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用全屏</span>
        <NSwitch v-model:value="appStore.widgetFullscreen" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用通知</span>
        <NSwitch v-model:value="appStore.widgetNotification" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用刷新</span>
        <NSwitch v-model:value="appStore.widgetRefresh" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用侧栏切换</span>
        <NSwitch v-model:value="appStore.widgetSidebarToggle" />
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        底栏 & 版权
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>显示底栏</span>
        <NSwitch v-model:value="appStore.footerEnable" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>固定在底部</span>
        <NSwitch v-model:value="appStore.footerFixed" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>启用版权</span>
        <NSwitch v-model:value="appStore.copyrightEnable" />
      </div>
      <NInput v-model:value="appStore.copyrightCompany" class="mb-2" placeholder="公司名" />
      <NInput v-model:value="appStore.copyrightSite" placeholder="公司主页" />
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        辅助模式
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>灰色模式</span>
        <NSwitch v-model:value="appStore.grayscaleEnabled" />
      </div>
      <div class="flex items-center justify-between">
        <span>色弱模式</span>
        <NSwitch v-model:value="appStore.colorWeaknessEnabled" />
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

/* 过渡动画预览选择器 */
.transition-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
  margin-top: 4px;
}

.transition-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  cursor: pointer;
}

.transition-preview {
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: 6px;
  border: 2px solid hsl(var(--border));
  background: hsl(var(--muted));
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: border-color 0.18s ease;
}

.transition-item.is-active .transition-preview {
  border-color: hsl(var(--primary));
}

.item-label {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  text-align: center;
  white-space: nowrap;
}

.transition-item.is-active .item-label {
  color: hsl(var(--primary));
  font-weight: 500;
}

/* 预览内部的小方块 */
.preview-block {
  width: 40%;
  height: 40%;
  border-radius: 4px;
  background: hsl(var(--primary) / 0.7);
}

/* 淡入淡出 */
@keyframes anim-fade {
  0%, 100% { opacity: 0; }
  40%, 60% { opacity: 1; }
}
.anim-fade {
  animation: anim-fade 2s ease-in-out infinite;
}

/* 左右滑动 */
@keyframes anim-slide-left {
  0% { transform: translateX(120%); opacity: 0; }
  30%, 70% { transform: translateX(0); opacity: 1; }
  100% { transform: translateX(-120%); opacity: 0; }
}
.anim-slide-left {
  animation: anim-slide-left 2.2s ease-in-out infinite;
}

/* 向上滑入 */
@keyframes anim-slide-up {
  0% { transform: translateY(120%); opacity: 0; }
  30%, 70% { transform: translateY(0); opacity: 1; }
  100% { transform: translateY(-120%); opacity: 0; }
}
.anim-slide-up {
  animation: anim-slide-up 2.2s ease-in-out infinite;
}

/* 向下滑入 */
@keyframes anim-slide-down {
  0% { transform: translateY(-120%); opacity: 0; }
  30%, 70% { transform: translateY(0); opacity: 1; }
  100% { transform: translateY(120%); opacity: 0; }
}
.anim-slide-down {
  animation: anim-slide-down 2.2s ease-in-out infinite;
}
</style>
