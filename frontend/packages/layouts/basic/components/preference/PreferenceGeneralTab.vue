<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NInput, NSelect, NSwitch } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocale } from '~/hooks'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceGeneralTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
const { t } = useI18n()
const { setLocale } = useLocale()

const transitionItems = computed(() => [
  { value: 'fade', label: t('preference.general.animation.fade') },
  { value: 'slide-left', label: t('preference.general.animation.slide_left') },
  { value: 'slide-up', label: t('preference.general.animation.slide_up') },
  { value: 'slide-down', label: t('preference.general.animation.slide_down') },
])

const localeOptions = [
  { label: '简体中文', value: 'zh-CN' },
  { label: 'English', value: 'en-US' },
]
</script>

<template>
  <div class="space-y-4">
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.general.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.general.language') }}</span>
        <NSelect
          v-model:value="appStore.locale"
          :options="localeOptions"
          size="small"
          style="width: 110px"
          @update:value="v => setLocale(String(v))"
        />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.dynamic_title') }}</span>
          <PrefTip :content="t('preference.general.dynamic_title_tip')" />
        </div>
        <NSwitch v-model:value="appStore.dynamicTitle" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.watermark') }}</span>
          <PrefTip :content="t('preference.general.watermark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.watermarkEnabled" />
      </div>
      <div v-if="appStore.watermarkEnabled" class="pref-row mb-2">
        <span>{{ t('preference.general.watermark_content') }}</span>
        <NInput
          v-model:value="appStore.watermarkText"
          size="small"
          style="width: 150px"
          :placeholder="t('preference.general.watermark_text')"
        />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.check_updates') }}</span>
          <PrefTip :content="t('preference.general.check_updates_tip')" />
        </div>
        <NSwitch v-model:value="appStore.enableCheckUpdates" />
      </div>
    </NCard>

    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.general.animation.title') }}
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.animation.transition_progress') }}</span>
          <PrefTip :content="t('preference.general.animation.transition_progress_tip')" />
        </div>
        <NSwitch v-model:value="appStore.transitionProgress" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.animation.transition_loading') }}</span>
          <PrefTip :content="t('preference.general.animation.transition_loading_tip')" />
        </div>
        <NSwitch v-model:value="appStore.transitionLoading" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.animation.theme_animation') }}</span>
          <PrefTip :content="t('preference.general.animation.theme_animation_tip')" />
        </div>
        <NSwitch v-model:value="appStore.themeAnimationEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.general.animation.transition_enable') }}</span>
          <PrefTip :content="t('preference.general.animation.transition_enable_tip')" />
        </div>
        <NSwitch v-model:value="appStore.transitionEnable" />
      </div>
      <div class="transition-grid" :class="{ 'pointer-events-none opacity-40': !appStore.transitionEnable }">
        <div
          v-for="item in transitionItems"
          :key="item.value"
          class="transition-item"
          :class="{ 'is-active': appStore.transitionName === item.value }"
          @click="appStore.transitionEnable && (appStore.transitionName = item.value)"
        >
          <div class="transition-preview">
            <div class="preview-block" :class="`anim-${item.value}`" />
          </div>
          <span class="item-label">{{ item.label }}</span>
        </div>
      </div>
    </NCard>
  </div>
</template>

<style scoped>
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
  border-radius: var(--radius);
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
  border-radius: var(--radius);
  background: hsl(var(--primary) / 0.7);
}

/* 淡入淡出 */
@keyframes anim-fade {
  0%,
  100% {
    opacity: 0;
  }
  40%,
  60% {
    opacity: 1;
  }
}
.anim-fade {
  animation: anim-fade 2s ease-in-out infinite;
}

/* 左右滑动 */
@keyframes anim-slide-left {
  0% {
    transform: translateX(120%);
    opacity: 0;
  }
  30%,
  70% {
    transform: translateX(0);
    opacity: 1;
  }
  100% {
    transform: translateX(-120%);
    opacity: 0;
  }
}
.anim-slide-left {
  animation: anim-slide-left 2.2s ease-in-out infinite;
}

/* 向上滑入 */
@keyframes anim-slide-up {
  0% {
    transform: translateY(120%);
    opacity: 0;
  }
  30%,
  70% {
    transform: translateY(0);
    opacity: 1;
  }
  100% {
    transform: translateY(-120%);
    opacity: 0;
  }
}
.anim-slide-up {
  animation: anim-slide-up 2.2s ease-in-out infinite;
}

/* 向下滑入 */
@keyframes anim-slide-down {
  0% {
    transform: translateY(-120%);
    opacity: 0;
  }
  30%,
  70% {
    transform: translateY(0);
    opacity: 1;
  }
  100% {
    transform: translateY(120%);
    opacity: 0;
  }
}
.anim-slide-down {
  animation: anim-slide-down 2.2s ease-in-out infinite;
}
</style>
