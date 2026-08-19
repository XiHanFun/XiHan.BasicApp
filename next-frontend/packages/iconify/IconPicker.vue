<script lang="ts" setup>
import { Icon } from '@iconify/vue/offline'
import { XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFlex, XhGridItem, XhGridRoot, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
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
    <XhButton variant="ghost" block class="icon-picker-trigger" @click="openPicker">
      <Icon v-if="currentIconId" :icon="currentIconId" width="20" />
      <span v-else class="icon-picker-placeholder">{{ placeholderText }}</span>
    </XhButton>

    <XhDialogRoot v-model:open="visible">
      <XhDialogContent class="icon-picker-modal" style="--xh-dialog-max-w: 560px">
        <XhDialogTitle>{{ t('component.icon_picker.modal_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>
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
              清除
            </XhButton>
          </XhFlex>

          <!-- 每个图标集一个页签，标签与面板都按图标集清单展开 -->
          <XhTabsRoot :value="activePrefix" variant="line" @update:value="handleTabChange">
            <XhTabsList>
              <XhTabsTrigger
                v-for="meta in ICON_SET_META"
                :key="meta.prefix"
                :value="meta.prefix"
              >
                {{ meta.name }}
              </XhTabsTrigger>
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
                  <XhEmptyStateIcon>
                    <Icon icon="lucide:search-x" width="28" height="28" />
                  </XhEmptyStateIcon>
                  <XhEmptyStateTitle>{{ t('common.no_result') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('component.icon_picker.empty') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
                <XhGridRoot v-else cols="6" gap="sm">
                  <XhGridItem
                    v-for="name in displayIcons"
                    :key="name"
                    class="icon-picker-item"
                    :class="{ 'is-selected': currentIconId === `${meta.prefix}:${name}` }"
                    @click="handleSelect(name)"
                  >
                    <div class="icon-picker-cell">
                      <div class="icon-picker-icon">
                        <Icon :icon="`${meta.prefix}:${name}`" width="22" height="22" />
                      </div>
                      <span class="icon-picker-name">{{ meta.prefix }}:{{ name }}</span>
                    </div>
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
.icon-picker-trigger {
  justify-content: flex-start;
  min-height: 34px;
}

.icon-picker-placeholder {
  color: hsl(var(--muted-foreground));
  font-size: 14px;
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
  cursor: pointer;
  transition: background 0.2s;
}

.icon-picker-cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 6px 4px;
  min-height: 52px;
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
