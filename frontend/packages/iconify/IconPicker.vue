<script lang="ts" setup>
import { useFieldControl, XhButton, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFlex, XhGridItem, XhGridRoot, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XInput from '../components/common/XInput.vue'
import { Icon } from './icon'
import { ICON_SET_META, loadIconNames } from './offline'
import { useScrollStrip } from './scroll-strip'

defineOptions({ name: 'IconPicker' })

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  placeholder: '',
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

// 字段接线落在真正可聚焦的触发钮上，标签的 for 才指得到
const fieldControl = useFieldControl()

interface Props {
  modelValue?: string | null
  placeholder?: string
}

const { t } = useI18n()

/** 未显式传入 placeholder 时回退到 i18n 默认文案 */
const placeholderText = computed(() => props.placeholder || t('component.icon_picker.select_placeholder'))

const visible = ref(false)
const activePrefix = ref('lucide')
const searchKeyword = ref('')
const iconNames = ref<string[]>([])
const loading = ref(false)
const iconNamesCache: Record<string, string[]> = {}

const displayIcons = computed(() => {
  const kw = searchKeyword.value.trim().toLowerCase()
  if (!kw) {
    return iconNames.value
  }
  return iconNames.value.filter(name => name.toLowerCase().includes(kw))
})

const currentIconId = computed(() => {
  const v = props.modelValue?.trim()
  if (!v) {
    return ''
  }
  return v.includes(':')
    ? v
    : `lucide:${v}`
})

async function loadIcons() {
  const prefix = activePrefix.value
  if (iconNamesCache[prefix]) {
    iconNames.value = iconNamesCache[prefix]
    loading.value = false
    return
  }
  loading.value = true
  try {
    const names = await loadIconNames(prefix)
    iconNamesCache[prefix] = names
    if (activePrefix.value === prefix) {
      iconNames.value = names
    }
  }
  finally {
    if (activePrefix.value === prefix) {
      loading.value = false
    }
  }
}

watch(activePrefix, loadIcons, { immediate: true })
watch(visible, (v) => {
  if (v) {
    loadIcons()
  }
})

/**
 * 图标集的页签带：一行排不下时横向滚，滚动条藏起来交给两端的箭头与滚轮（与顶栏水平菜单、标签栏同一套手感）
 * 弹窗打开、换了图标集之后，把选中的那一枚页签滚进可视区
 */
const setsViewport = ref<HTMLElement | null>(null)
const {
  overflowing: setsOverflowing,
  atStart: setsAtStart,
  atEnd: setsAtEnd,
  scrollToward: scrollSets,
  reveal: revealSet,
} = useScrollStrip(setsViewport)

async function revealActiveSet() {
  await nextTick()
  revealSet(setsViewport.value?.querySelector('[role="tab"][aria-selected="true"]'))
}

watch([setsViewport, activePrefix], revealActiveSet, { flush: 'post' })

function handleSelect(name: string) {
  const id = `${activePrefix.value}:${name}`
  emit('update:modelValue', id)
  visible.value = false
}

function handleTabChange(prefix: string | null) {
  if (prefix) {
    activePrefix.value = prefix
  }
}

function openPicker() {
  visible.value = true
  searchKeyword.value = ''
}

function handleClear() {
  emit('update:modelValue', '')
  visible.value = false
}
</script>

<template>
  <div class="icon-picker">
    <XhButton v-bind="fieldControl" variant="outline" size="sm" full-width class="icon-picker-trigger" @click="openPicker">
      <Icon v-if="currentIconId" :icon="currentIconId" width="20" />
      <span v-else class="icon-picker-placeholder">{{ placeholderText }}</span>
    </XhButton>

    <XhDialogRoot v-model:open="visible">
      <XhDialogContent class="icon-picker-modal" style="--xh-dialog-max-w: 560px">
        <XhDialogHeader>
          <XhDialogTitle>{{ t('component.icon_picker.modal_title') }}</XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody>
          <XhFlex class="mb-3" justify="between">
            <XInput
              v-model:value="searchKeyword"
              :placeholder="t('component.icon_picker.search_placeholder')"
              clearable
              style="flex: 1"
            >
              <template #prefix>
                <Icon icon="lucide:search" width="16" />
              </template>
            </XInput>
            <XhButton v-if="currentIconId" size="sm" variant="ghost" @click="handleClear">
              {{ t('component.icon_picker.clear') }}
            </XhButton>
          </XhFlex>

          <!-- 每个图标集一个页签，标签与面板都按图标集清单展开；图标集多、弹窗窄，页签带放不下时横向滚：
               滚动条藏起来，两端各一枚翻页箭头、滚轮折成横向，到头的一侧箭头置灰
               一个图标集上千个图标：面板内容只在选中时渲染、选走即卸，否则每个面板都铺一整份网格 -->
          <XhTabsRoot :value="activePrefix" variant="line" lazy-mount unmount-on-exit @update:value="handleTabChange">
            <div class="icon-picker-sets">
              <XhButton
                v-show="setsOverflowing"
                class="icon-picker-sets__arrow"
                variant="ghost"
                size="sm"
                icon-only
                :disabled="setsAtStart"
                :aria-label="t('component.icon_picker.sets_prev')"
                @click="scrollSets('start')"
              >
                <Icon icon="lucide:chevrons-left" width="14" />
              </XhButton>
              <div ref="setsViewport" class="icon-picker-sets__viewport">
                <XhTabsList>
                  <XhTabsTrigger
                    v-for="meta in ICON_SET_META"
                    :key="meta.prefix"
                    :value="meta.prefix"
                  >
                    {{ meta.name }}
                  </XhTabsTrigger>
                  <XhTabsIndicator />
                </XhTabsList>
              </div>
              <XhButton
                v-show="setsOverflowing"
                class="icon-picker-sets__arrow"
                variant="ghost"
                size="sm"
                icon-only
                :disabled="setsAtEnd"
                :aria-label="t('component.icon_picker.sets_next')"
                @click="scrollSets('end')"
              >
                <Icon icon="lucide:chevrons-right" width="14" />
              </XhButton>
            </div>
            <XhTabsContent
              v-for="meta in ICON_SET_META"
              :key="meta.prefix"
              :value="meta.prefix"
            >
              <div class="xh-scroll-area" style="max-height: 320px">
                <div v-if="loading" class="icon-picker-loading">
                  {{ t('common.loading') }}
                </div>
                <XhEmptyStateRoot v-else-if="!displayIcons.length" size="sm">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:search-x" width="28" height="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.no_result') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('component.icon_picker.empty') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
                <XhGridRoot v-else cols="6" gap="sm">
                  <!-- 网格格子只管排布，可点的是格子里的原生按钮：键盘能停靠、Enter / Space 能选 -->
                  <XhGridItem
                    v-for="name in displayIcons"
                    :key="name"
                    class="icon-picker-item"
                    :class="{ 'is-selected': currentIconId === `${meta.prefix}:${name}` }"
                  >
                    <button
                      type="button"
                      class="icon-picker-cell"
                      :aria-label="`${meta.prefix}:${name}`"
                      :aria-pressed="currentIconId === `${meta.prefix}:${name}`"
                      @click="handleSelect(name)"
                    >
                      <span class="icon-picker-icon" aria-hidden="true">
                        <Icon :icon="`${meta.prefix}:${name}`" width="22" height="22" />
                      </span>
                      <span class="icon-picker-name">{{ meta.prefix }}:{{ name }}</span>
                    </button>
                  </XhGridItem>
                </XhGridRoot>
              </div>
            </XhTabsContent>
          </XhTabsRoot>
        </XhDialogBody>
      </XhDialogContent>
    </XhDialogRoot>
  </div>
</template>

<style scoped>
/* 触发钮是表单里的一枚字段：与 sm 档的输入框同高（钮的 size 给），内容靠起始端排；
   内衬取输入框 sm 档的那一档，占位文字才与相邻输入框的文字起点对齐 */
.icon-picker-trigger {
  --xh-button-px: var(--xh-control-px-sm);

  justify-content: flex-start;
  font-weight: var(--xh-font-weight-regular);
}

.icon-picker-placeholder {
  color: var(--xh-fg-subtle);
}

/* 页签带一行：箭头 + 滚动区；滚动区藏掉滚动条，滚动交给两端箭头与滚轮 */
.icon-picker-sets {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  min-inline-size: 0;
}

.icon-picker-sets__viewport {
  flex: 1;
  min-inline-size: 0;
  overflow: auto hidden;
  scrollbar-width: none;
  overscroll-behavior-x: contain;
}

.icon-picker-sets__viewport::-webkit-scrollbar {
  display: none;
}

/* 页签带自己不裁、不滚：按内容一行摆开，由外面的滚动区带着走；排得下时仍铺满，底线才连得上 */
.icon-picker-sets__viewport :deep([data-scope='tabs'][data-part='list']) {
  inline-size: max-content;
  min-inline-size: 100%;
  overflow: visible;
}

.icon-picker-sets__arrow {
  flex: none;
}

.icon-picker-loading {
  padding: var(--xh-space-8);
  text-align: center;
  color: var(--xh-fg-muted);
}

.icon-picker-grid {
  padding: var(--xh-space-2) 0;
}

.icon-picker-item {
  border-radius: var(--xh-shape-inset);
  transition: background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

/* 格子里的按钮铺满整格，去掉原生按钮的底与边，悬停 / 选中的面仍画在格子上；图标边长 22px 由两档间距相加 */
.icon-picker-cell {
  --icon-picker-glyph: calc(var(--xh-space-5) + var(--xh-space-0_5));

  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--xh-space-1);
  inline-size: 100%;
  padding: var(--xh-space-1_5) var(--xh-space-1);
  border: 0;
  border-radius: inherit;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
}

/* 键盘焦点环与组件库同一道：2px 画在格子盒内，环宽、偏移与颜色都取令牌 */
.icon-picker-cell:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.icon-picker-icon {
  width: var(--icon-picker-glyph);
  height: var(--icon-picker-glyph);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.icon-picker-icon :deep(svg) {
  width: var(--icon-picker-glyph);
  height: var(--icon-picker-glyph);
}

.icon-picker-name {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.icon-picker-item:hover {
  background: var(--xh-bg-subtle-hover);
}

.icon-picker-item.is-selected {
  background: color-mix(in srgb, var(--xh-fg-brand) 15%, transparent);
  color: var(--xh-fg-brand);
}
</style>
