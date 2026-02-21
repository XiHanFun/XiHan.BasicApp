<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NInput, NRadioButton, NRadioGroup, NSpace, NSwitch } from 'naive-ui'

defineOptions({ name: 'PreferenceGeneralTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
</script>

<template>
  <div class="space-y-4">
    <NCard size="small">
      <div class="mb-2 font-medium">
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

    <NCard size="small">
      <div class="mb-2 font-medium">
        动画
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>页面切换动画</span>
        <NSwitch v-model:value="appStore.transitionEnable" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span>主题切换动画</span>
        <NSwitch v-model:value="appStore.themeAnimationEnabled" />
      </div>
      <NRadioGroup v-model:value="appStore.transitionName">
        <NSpace>
          <NRadioButton value="fade">
            淡入淡出
          </NRadioButton>
          <NRadioButton value="slide-left">
            左右滑动
          </NRadioButton>
          <NRadioButton value="zoom">
            缩放
          </NRadioButton>
        </NSpace>
      </NRadioGroup>
    </NCard>

    <NCard size="small">
      <div class="mb-2 font-medium">
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

    <NCard size="small">
      <div class="mb-2 font-medium">
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

    <NCard size="small">
      <div class="mb-2 font-medium">
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
