<script setup lang="ts">
import type { FloatButtonEdgePosition, FloatButtonPosition } from '@xihan-ui/headless'
import { useFullscreen } from '@vueuse/core'
import { XhFloatButtonList, XhFloatButtonRoot, XhFloatButtonTrigger } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import { WIDGET_FLOAT_TOOLBAR_POSITION_KEY } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useLayoutBridgeStore } from '~/stores'
import { LocalStorage } from '~/utils'
import { useWidgetPlacement } from '../composables'

/**
 * 悬浮工具组：命令面板、语言、时区、主题、全屏与偏好设置里，位置落到「悬浮」的都收进这一组。
 * 落位与顶栏共用 useWidgetPlacement 判定，同一个工具不会两处同时出现；一个都没有时整组不渲染。
 *
 * 拖动与贴边交给组件库（draggable）：按住触发器可拖到任意处，松手贴向左右两边里近的那条，
 * 起拖时收起整组、拖完补派的点击不开合，展开组恒朝页面中间长。位置按「贴哪条边 + 中心在视口高的比例」记，
 * 视口尺寸变了仍落在同一侧、同一比例上；默认贴右边、停在视口四分之三高处（不压分页、页脚与回到顶部）。
 */
defineOptions({ name: 'AppFloatToolbar' })

/** 默认落点：贴行尾一侧，触发器中心在视口高度的四分之三处 */
const DEFAULT_POSITION: FloatButtonEdgePosition = { edge: 'inline-end', ratio: 0.75 }

const { t } = useI18n()
const layoutBridgeStore = useLayoutBridgeStore()
const placement = useWidgetPlacement()
const { isDark, toggleThemeWithTransition } = useTheme()
const { isFullscreen, toggle: toggleFullscreen } = useFullscreen()

const open = ref(false)
const translations = computed(() => ({ trigger: t('header.toolbar.float_tools') }))

// ---- 位置 ----
/** 贴左右两边的位置才作数：组件库按缺省 snap（inline）只会落在这两条边上 */
function isInlineEdgePosition(value: unknown): value is FloatButtonEdgePosition {
  if (typeof value !== 'object' || value === null) {
    return false
  }
  const { edge, ratio } = value as Partial<FloatButtonEdgePosition>
  return (edge === 'inline-start' || edge === 'inline-end')
    && typeof ratio === 'number' && Number.isFinite(ratio) && ratio >= 0 && ratio <= 1
}

function restorePosition(): FloatButtonEdgePosition {
  const saved = LocalStorage.get<unknown>(WIDGET_FLOAT_TOOLBAR_POSITION_KEY)
  return isInlineEdgePosition(saved) ? saved : DEFAULT_POSITION
}

const position = ref<FloatButtonPosition>(restorePosition())
// 组件库只在拖动落定后写回一次，这里随之记到本机
watch(position, next => LocalStorage.set(WIDGET_FLOAT_TOOLBAR_POSITION_KEY, next))

const edgePosition = computed(() => isInlineEdgePosition(position.value) ? position.value : DEFAULT_POSITION)
/** 语言、时区菜单朝屏幕里面弹：贴左边往右弹，贴右边往左弹；上半屏往下展开、下半屏往上展开 */
const menuPlacement = computed(() =>
  `${edgePosition.value.edge === 'inline-start' ? 'right' : 'left'}-${edgePosition.value.ratio < 0.5 ? 'start' : 'end'}` as const)

// ---- 动作 ----
/** 点了动作就收起整组；语言与时区是菜单，选完由菜单自己收起，组保持展开以便看到结果 */
function run(action: () => void) {
  open.value = false
  action()
}

function toggleTheme(event: MouseEvent) {
  open.value = false
  toggleThemeWithTransition(event)
}

/**
 * Iconify 的 svg 只认 1em，而 ui.css 按 Action Control 档位给按钮里的图标定的尺寸不是 floating 档；
 * 悬浮组里的图标一律接组件库下发的字形尺，与触发器兜底字形同一把尺
 */
const glyphStyle = { inlineSize: 'var(--xh-icon-size)', blockSize: 'var(--xh-icon-size)' }
</script>

<template>
  <XhFloatButtonRoot
    v-if="placement.hasFloating.value"
    v-model:open="open"
    v-model:position="position"
    draggable
    :translations="translations"
  >
    <XhFloatButtonTrigger>
      <!-- 收起时是螺母（快捷工具），展开后换成关闭，读得出再按一下就收起 -->
      <Icon :icon="open ? 'lucide:x' : 'lucide:bolt'" :style="glyphStyle" />
    </XhFloatButtonTrigger>
    <XhFloatButtonList>
      <button
        v-if="placement.search.value === 'floating'"
        type="button"
        :aria-label="t('header.search.placeholder')"
        :title="t('header.search.placeholder')"
        @click="run(() => layoutBridgeStore.requestOpenGlobalSearch())"
      >
        <Icon icon="lucide:search" :style="glyphStyle" />
      </button>
      <LocaleSwitcher v-if="placement.language.value === 'floating'" variant="dropdown" apply :placement="menuPlacement">
        <button
          type="button"
          class="float-menu-item"
          data-xh-material="frosted"
          :aria-label="t('header.toolbar.switch_language')"
          :title="t('header.toolbar.switch_language')"
        >
          <Icon icon="lucide:languages" :style="glyphStyle" />
        </button>
      </LocaleSwitcher>
      <TimezoneSwitcher v-if="placement.timezone.value === 'floating'" variant="dropdown" apply :placement="menuPlacement">
        <button
          type="button"
          class="float-menu-item"
          data-xh-material="frosted"
          :aria-label="t('header.toolbar.switch_timezone')"
          :title="t('header.toolbar.switch_timezone')"
        >
          <Icon icon="lucide:clock-3" :style="glyphStyle" />
        </button>
      </TimezoneSwitcher>
      <button
        v-if="placement.theme.value === 'floating'"
        type="button"
        :aria-label="isDark ? t('header.toolbar.theme_to_light') : t('header.toolbar.theme_to_dark')"
        :title="isDark ? t('header.toolbar.theme_to_light') : t('header.toolbar.theme_to_dark')"
        @click="toggleTheme"
      >
        <span class="icon-swap">
          <Transition name="icon-swap">
            <Icon :key="isDark ? 'sun' : 'moon'" :icon="isDark ? 'lucide:sun' : 'lucide:moon'" :style="glyphStyle" />
          </Transition>
        </span>
      </button>
      <button
        v-if="placement.fullscreen.value === 'floating'"
        type="button"
        :aria-label="isFullscreen ? t('header.toolbar.fullscreen_exit') : t('header.toolbar.fullscreen_enter')"
        :title="isFullscreen ? t('header.toolbar.fullscreen_exit') : t('header.toolbar.fullscreen_enter')"
        @click="run(toggleFullscreen)"
      >
        <span class="icon-swap">
          <Transition name="icon-swap">
            <Icon :key="isFullscreen ? 'minimize' : 'maximize'" :icon="isFullscreen ? 'lucide:minimize' : 'lucide:maximize'" :style="glyphStyle" />
          </Transition>
        </span>
      </button>
      <button
        v-if="placement.preference.value === 'floating'"
        type="button"
        :aria-label="t('header.toolbar.preferences')"
        :title="t('header.toolbar.preferences')"
        @click="run(() => layoutBridgeStore.requestOpenPreferenceDrawer())"
      >
        <!-- 与顶栏的偏好设置按钮同一个图标 -->
        <Icon icon="lucide:settings-2" :style="glyphStyle" />
      </button>
    </XhFloatButtonList>
  </XhFloatButtonRoot>
</template>

<style scoped>
/* 语言与时区是菜单：菜单触发器带着自己的 data-scope，悬浮组皮肤只给不带 data-scope 的原生按钮画面，
   这两颗的面改从材质配方取（data-xh-material="frosted"，即缺省 outline 档同一份磨砂面），
   这里只补与原生动作项同一张表的交互阶梯 */
.float-menu-item {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  font: inherit;
  cursor: pointer;
  transition:
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    scale var(--xh-motion-duration-release) var(--xh-motion-ease-release);
}

.float-menu-item:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

/* 菜单展开时停在悬停档，读得出是哪一颗弹的菜单 */
.float-menu-item[data-state='open'] {
  background-color: var(--xh-bg-subtle-opaque);
}

@media (hover: hover) {
  .float-menu-item:hover {
    background-color: var(--xh-bg-subtle-opaque);
  }
}

.float-menu-item:active {
  background-color: var(--xh-bg-subtle-hover-opaque);
  scale: var(--xh-motion-scale-press);
  transition-duration: var(--xh-motion-duration-press);
  transition-timing-function: var(--xh-motion-ease-press);
}
</style>
