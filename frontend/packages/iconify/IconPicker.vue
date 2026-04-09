<script lang="ts" setup>
import {
  NButton,
  NEmpty,
  NGrid,
  NGridItem,
  NInput,
  NModal,
  NScrollbar,
  NSpace,
  NTabPane,
  NTabs,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { Icon } from '~/iconify'
import { ICON_SET_META, loadIconNames } from './offline'

defineOptions({ name: 'IconPicker' })

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  placeholder: '选择图标',
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

interface Props {
  modelValue?: string
  placeholder?: string
}

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

function handleTabChange(prefix: string) {
  activePrefix.value = prefix
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
    <NButton quaternary block class="icon-picker-trigger" @click="openPicker">
      <Icon v-if="currentIconId" :icon="currentIconId" width="20" />
      <span v-else class="icon-picker-placeholder">{{ placeholder }}</span>
    </NButton>

    <NModal
      v-model:show="visible"
      preset="card"
      title="选择图标"
      class="icon-picker-modal"
      style="width: 560px; max-width: 95vw"
      :bordered="false"
      @after-enter="loadIcons"
    >
      <div class="icon-picker-body">
        <NSpace class="mb-3" justify="space-between">
          <NInput
            v-model:value="searchKeyword"
            placeholder="搜索图标名称..."
            clearable
            style="flex: 1"
          >
            <template #prefix>
              <Icon icon="lucide:search" width="16" />
            </template>
          </NInput>
          <NButton v-if="currentIconId" size="small" quaternary @click="handleClear">
            清除
          </NButton>
        </NSpace>

        <NTabs type="line" :value="activePrefix" @update:value="handleTabChange">
          <NTabPane
            v-for="meta in ICON_SET_META"
            :key="meta.prefix"
            :name="meta.prefix"
            :tab="meta.name"
          >
            <NScrollbar style="max-height: 320px">
              <div v-if="loading" class="icon-picker-loading">
                加载中...
              </div>
              <NEmpty v-else-if="!displayIcons.length" description="暂无匹配图标" />
              <NGrid v-else :cols="6" :x-gap="8" :y-gap="8" class="icon-picker-grid">
                <NGridItem
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
                </NGridItem>
              </NGrid>
            </NScrollbar>
          </NTabPane>
        </NTabs>
      </div>
    </NModal>
  </div>
</template>

<style scoped>
.icon-picker-trigger {
  justify-content: flex-start;
  min-height: 34px;
}

.icon-picker-placeholder {
  color: var(--n-placeholder-color);
  font-size: 14px;
}

.icon-picker-body {
  padding: 4px 0;
}

.icon-picker-loading {
  padding: 40px;
  text-align: center;
  color: var(--n-text-color-3);
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
  color: var(--n-text-color-3);
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
