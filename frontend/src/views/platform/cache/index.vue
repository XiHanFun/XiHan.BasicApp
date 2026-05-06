<script setup lang="ts">
import {
  NButton,
  NCard,
  NEmpty,
  NIcon,
  NInput,
  NSpace,
  NSpin,
  NTag,
  NTooltip,
  NTree,
  useDialog,
  useMessage,
} from 'naive-ui'
import type { TreeOption } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { cacheApi } from '@/api'
import { Icon, XJsonViewer } from '~/components'

defineOptions({ name: 'SystemCachePage' })

const message = useMessage()
const dialog = useDialog()

// 查询状态
const keyPattern = ref('*')
const loadingKeys = ref(false)
const loadingValue = ref(false)

// 缓存键列表
const cacheKeys = ref<string[]>([])
const selectedKey = ref<string | null>(null)
const selectedValue = ref<string | null>(null)

// 将扁平键列表构建为树结构（按 : 分隔）
const treeData = computed<TreeOption[]>(() => {
  const root: TreeOption[] = []
  const map = new Map<string, TreeOption>()

  for (const key of cacheKeys.value) {
    const parts = key.split(':')
    let parentChildren = root
    let pathAccum = ''

    for (let i = 0; i < parts.length; i++) {
      const segment = parts[i]!
      pathAccum = pathAccum ? `${pathAccum}:${segment}` : segment
      const isLeaf = i === parts.length - 1

      if (!map.has(pathAccum)) {
        const node: TreeOption = {
          key: isLeaf ? key : `__group__${pathAccum}`,
          label: segment,
          isLeaf,
          children: isLeaf ? undefined : [],
        }
        map.set(pathAccum, node)
        parentChildren.push(node)
      }

      const current = map.get(pathAccum)!
      if (!isLeaf) {
        parentChildren = current.children as TreeOption[]
      }
    }
  }

  return root
})

const keyCount = computed(() => cacheKeys.value.length)

async function handleSearch() {
  loadingKeys.value = true
  selectedKey.value = null
  selectedValue.value = null
  try {
    const keys = await cacheApi.getKeys(keyPattern.value.trim() || '*')
    cacheKeys.value = keys?.sort() ?? []
  }
  catch {
    message.error('查询缓存键失败')
  }
  finally {
    loadingKeys.value = false
  }
}

async function handleSelectNode(keys: Array<string | number>, option: Array<TreeOption | null>) {
  const node = option[0]
  if (!node?.isLeaf) {
    selectedKey.value = null
    selectedValue.value = null
    return
  }

  const key = String(keys[0])
  selectedKey.value = key
  loadingValue.value = true
  try {
    const value = await cacheApi.getString(key)
    selectedValue.value = value ?? null
  }
  catch {
    message.error('获取缓存值失败')
    selectedValue.value = null
  }
  finally {
    loadingValue.value = false
  }
}

function handleDeleteSelected() {
  if (!selectedKey.value) {
    return
  }

  const key = selectedKey.value
  dialog.warning({
    title: '确认删除',
    content: `确定删除缓存键「${key}」？`,
    positiveText: '删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await cacheApi.remove(key)
        message.success('删除成功')
        cacheKeys.value = cacheKeys.value.filter(k => k !== key)
        selectedKey.value = null
        selectedValue.value = null
      }
      catch {
        message.error('删除失败')
      }
    },
  })
}

function handleDeleteByPattern() {
  const pattern = keyPattern.value.trim() || '*'
  dialog.warning({
    title: '按模式删除',
    content: `确定删除匹配「${pattern}」的所有缓存键？此操作不可恢复。`,
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        const count = await cacheApi.removeByPattern(pattern)
        message.success(`已删除 ${count} 个缓存键`)
        await handleSearch()
      }
      catch {
        message.error('批量删除失败')
      }
    },
  })
}

// 格式化显示值（尝试解析 JSON）
const displayValue = computed(() => {
  if (selectedValue.value === null) {
    return selectedKey.value ? 'null (键不存在或值为空)' : '请选择一个缓存键'
  }
  try {
    const parsed = JSON.parse(selectedValue.value)
    return JSON.stringify(parsed, null, 2)
  }
  catch {
    return selectedValue.value
  }
})

const displayTitle = computed(() => {
  if (!selectedKey.value) {
    return '缓存内容'
  }
  return selectedKey.value
})

onMounted(() => {
  handleSearch()
})
</script>

<template>
  <div class="cache-page">
    <!-- 第一行：查询 + 操作 -->
    <NCard :bordered="false" size="small" class="cache-toolbar">
      <NSpace align="center" wrap>
        <NInput
          v-model:value="keyPattern"
          placeholder="输入键模式，如 auth:* 或 *"
          clearable
          style="width: 280px"
          @keydown.enter="handleSearch"
        >
          <template #prefix>
            <NIcon size="14" color="var(--text-secondary)">
              <Icon icon="lucide:search" />
            </NIcon>
          </template>
        </NInput>

        <NTooltip>
          <template #trigger>
            <NButton type="primary" size="small" :loading="loadingKeys" @click="handleSearch">
              <template #icon>
                <NIcon><Icon icon="lucide:search" /></NIcon>
              </template>
              查询
            </NButton>
          </template>
          按模式搜索缓存键
        </NTooltip>

        <NTooltip>
          <template #trigger>
            <NButton
              size="small"
              :disabled="!selectedKey"
              type="error"
              ghost
              @click="handleDeleteSelected"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:trash-2" /></NIcon>
              </template>
              删除选中
            </NButton>
          </template>
          删除当前选中的缓存键
        </NTooltip>

        <NTooltip>
          <template #trigger>
            <NButton size="small" type="warning" ghost @click="handleDeleteByPattern">
              <template #icon>
                <NIcon><Icon icon="lucide:eraser" /></NIcon>
              </template>
              按模式删除
            </NButton>
          </template>
          按当前模式批量删除缓存键
        </NTooltip>

        <NTag v-if="keyCount > 0" size="small" type="info" :bordered="false" round>
          共 {{ keyCount }} 个键
        </NTag>
      </NSpace>
    </NCard>

    <!-- 第二行：左树右详情 -->
    <div class="cache-content">
      <!-- 左侧：缓存键树 -->
      <NCard :bordered="false" size="small" class="cache-tree-card">
        <template #header>
          <div class="cache-card-header">
            <Icon icon="lucide:folder-tree" width="15" />
            <span>缓存键</span>
          </div>
        </template>
        <NSpin :show="loadingKeys" class="cache-tree-spin">
          <div v-if="cacheKeys.length === 0 && !loadingKeys" class="cache-empty">
            <NEmpty description="暂无缓存键" />
          </div>
          <NTree
            v-else
            :data="treeData"
            block-line
            :selectable="true"
            :default-expand-all="cacheKeys.length <= 100"
            @update:selected-keys="handleSelectNode"
          />
        </NSpin>
      </NCard>

      <!-- 右侧：缓存值详情 -->
      <NCard :bordered="false" size="small" class="cache-detail-card">
        <template #header>
          <div class="cache-card-header">
            <Icon icon="lucide:file-json" width="15" />
            <span>{{ displayTitle }}</span>
          </div>
        </template>
        <NSpin :show="loadingValue" class="cache-detail-spin">
          <XJsonViewer
            v-if="selectedKey"
            :title="displayTitle"
            :raw-text="displayValue"
            :max-height="0"
          />
          <div v-else class="cache-empty">
            <NEmpty description="请在左侧选择一个缓存键查看内容" />
          </div>
        </NSpin>
      </NCard>
    </div>
  </div>
</template>

<style scoped>
.cache-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 100%;
}

.cache-toolbar {
  flex-shrink: 0;
}

.cache-content {
  display: flex;
  gap: 12px;
  flex: 1;
  min-height: 0;
}

.cache-tree-card {
  width: 360px;
  min-width: 280px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.cache-tree-card :deep(.n-card__content) {
  flex: 1;
  overflow: auto;
  padding: 8px 12px !important;
}

.cache-detail-card {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.cache-detail-card :deep(.n-card__content) {
  flex: 1;
  overflow: auto;
  padding: 8px 12px !important;
}

.cache-card-header {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.cache-tree-spin,
.cache-detail-spin {
  min-height: 200px;
}

.cache-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 200px;
}

@media (max-width: 768px) {
  .cache-content {
    flex-direction: column;
  }

  .cache-tree-card {
    width: 100%;
    max-height: 300px;
  }
}
</style>
