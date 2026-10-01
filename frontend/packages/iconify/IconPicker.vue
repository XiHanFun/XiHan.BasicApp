<script lang="ts" setup>
import { Icon } from '@iconify/vue/offline'
import { useFieldControl, XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFlex, XhGridItem, XhGridRoot, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsNextTrigger, XhTabsPrevTrigger, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XInput from '../components/common/XInput.vue'
import { ICON_SET_META, loadIconNames } from './offline'

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
  return v.includes(':') ? v : `lucide:${v}`
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
        <XhDialogTitle>{{ t('component.icon_picker.modal_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <div class="icon-picker-body">
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

          <!-- 每个图标集一个页签，标签与面板都按图标集清单展开；图标集多、弹窗窄，标签带放不下时靠两端翻页钮挪。
               一个图标集上千个图标：面板内容只在选中时渲染、选走即卸，否则每个面板都铺一整份网格 -->
          <XhTabsRoot :value="activePrefix" variant="line" lazy-mount unmount-on-exit @update:value="handleTabChange">
            <XhTabsList>
              <XhTabsPrevTrigger />
              <XhTabsTrigger
                v-for="meta in ICON_SET_META"
                :key="meta.prefix"
                :value="meta.prefix"
              >
                {{ meta.name }}
              </XhTabsTrigger>
              <XhTabsIndicator />
              <XhTabsNextTrigger />
            </XhTabsList>
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
        </div>
      </XhDialogContent>
    </XhDialogRoot>
  </div>
</template>

<style scoped>
/* 触发钮是表单里的一枚字段：与 sm 档的输入框同高（钮的 size 给），内容靠起始端排 */
.icon-picker-trigger {
  justify-content: flex-start;
  font-weight: var(--xh-font-weight-regular);
}

.icon-picker-placeholder {
  color: var(--xh-fg-subtle);
}

.icon-picker-body {
  padding: 4px 0;
}

.icon-picker-loading {
  padding: 40px;
  text-align: center;
  color: hsl(var(--muted-foreground));
}

.icon-picker-grid {
  padding: 8px 0;
}

.icon-picker-item {
  border-radius: 6px;
  transition: background var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

/* 格子里的按钮铺满整格，去掉原生按钮的底与边，悬停 / 选中的面仍画在格子上 */
.icon-picker-cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  inline-size: 100%;
  padding: 6px 4px;
  min-height: 52px;
  border: 0;
  border-radius: inherit;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
}

/* 键盘焦点环与组件库同一条：环宽、内收偏移与颜色都取令牌 */
.icon-picker-cell:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.icon-picker-icon {
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.icon-picker-icon :deep(svg) {
  width: 22px;
  height: 22px;
}

.icon-picker-name {
  font-size: 10px;
  color: hsl(var(--muted-foreground));
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.icon-picker-item:hover {
  background: hsl(var(--accent));
}

.icon-picker-item.is-selected {
  background: hsl(var(--primary) / 0.15);
  color: hsl(var(--primary));
}
</style>
