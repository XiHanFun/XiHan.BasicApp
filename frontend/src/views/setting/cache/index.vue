<script setup lang="ts">
import type { TreeOption } from 'naive-ui'
import {
  NButton,
  NCard,
  NEmpty,
  NIcon,
  NInput,
  NRadioButton,
  NRadioGroup,
  NSpin,
  NTag,
  NTooltip,
  NTree,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { cacheApi } from '@/api'
import { Icon } from '~/components'
import { usePermission } from '~/hooks'

defineOptions({ name: 'PlatformCachePage' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()
const { hasPermission } = usePermission()

/** 维护权限（写入/删除）；无则只读浏览 */
const canManage = computed(() => hasPermission('saas:cache:clear'))

/**
 * NCard 内容容器样式：经 content-style prop 直传内联样式，不依赖 naive 内部 class（class 名随版本/前缀变动会失效）。
 * 让内容区成为定高 flex 列，使内部滚动壳能撑满并收敛——与 SchemaPage 的做法一致。
 */
const cardContentStyle = {
  display: 'flex',
  flex: '1',
  flexDirection: 'column',
  minHeight: '0',
  padding: '8px 12px',
  overflow: 'hidden',
} as const

// ── 键列表（左侧） ──
const keyPattern = ref('*')
const loadingKeys = ref(false)
const cacheKeys = ref<string[]>([])
const selectedKeys = ref<string[]>([])
const expandedKeys = ref<string[]>([])

// ── 键值（右侧） ──
const detailKey = ref<null | string>(null)
const loadingValue = ref(false)
const rawValue = ref<null | string>(null)
const draft = ref('')
const format = ref<'json' | 'text'>('text')
const editing = ref(false)
const saving = ref(false)

const GROUP_PREFIX = '__group__'

interface CacheTreeOption extends TreeOption {
  leafCount?: number
}

/**
 * 扁平键 → 树（按 : 分组）。叶子与分组分槽存储：
 * 同一路径既是某键又是更深键前缀（如 a:b 与 a:b:c 并存）时不互相覆盖、不丢键。
 * 分组节点不可选（selectable:false），仅叶子参与选择，避免多选时混入分组。
 */
const treeData = computed<CacheTreeOption[]>(() => {
  const root: CacheTreeOption[] = []
  const groupMap = new Map<string, CacheTreeOption>()
  const leafMap = new Map<string, CacheTreeOption>()

  const ensureGroup = (pathAccum: string, segment: string, parent: CacheTreeOption[]): CacheTreeOption => {
    let node = groupMap.get(pathAccum)
    if (!node) {
      node = { key: `${GROUP_PREFIX}${pathAccum}`, label: segment, isLeaf: false, selectable: false, children: [], leafCount: 0 }
      groupMap.set(pathAccum, node)
      parent.push(node)
    }
    return node
  }

  for (const key of cacheKeys.value) {
    const parts = key.split(':')
    let parentChildren = root
    let pathAccum = ''

    for (let i = 0; i < parts.length - 1; i++) {
      const segment = parts[i]!
      pathAccum = pathAccum ? `${pathAccum}:${segment}` : segment
      const group = ensureGroup(pathAccum, segment, parentChildren)
      group.leafCount = (group.leafCount ?? 0) + 1
      parentChildren = group.children as CacheTreeOption[]
    }

    const leafSegment = parts[parts.length - 1]!
    if (!leafMap.has(key)) {
      const leaf: CacheTreeOption = { key, label: leafSegment, isLeaf: true }
      leafMap.set(key, leaf)
      parentChildren.push(leaf)
    }
  }

  return root
})

function renderTreeLabel({ option }: { option: TreeOption }) {
  const node = option as CacheTreeOption
  if (node.isLeaf) {
    return h('span', { class: 'cache-tree-leaf', title: String(node.key) }, String(node.label ?? ''))
  }
  return h('span', { class: 'cache-tree-group' }, [
    h('span', null, String(node.label ?? '')),
    // 键数用徽标显示（与顶部「缓存键」计数同款 NTag），不再用括号
    h(
      NTag,
      { size: 'tiny', type: 'info', bordered: false, round: true },
      { default: () => String(node.leafCount ?? 0) },
    ),
  ])
}

const keyCount = computed(() => cacheKeys.value.length)
const selectedCount = computed(() => selectedKeys.value.length)

function collectGroupKeys(nodes: CacheTreeOption[]): string[] {
  const keys: string[] = []
  const walk = (list: CacheTreeOption[]) => {
    for (const node of list) {
      if (!node.isLeaf) {
        keys.push(String(node.key))
        walk((node.children ?? []) as CacheTreeOption[])
      }
    }
  }
  walk(nodes)
  return keys
}

async function loadKeys() {
  loadingKeys.value = true
  try {
    const keys = await cacheApi.getKeys(keyPattern.value.trim() || '*')
    cacheKeys.value = keys?.sort() ?? []
    selectedKeys.value = []
    expandedKeys.value = cacheKeys.value.length <= 100 ? collectGroupKeys(treeData.value) : []
  }
  catch {
    message.error(t('setting.cache.query_keys_failed'))
  }
  finally {
    loadingKeys.value = false
  }
}

async function handleSearch() {
  resetDetail()
  await loadKeys()
}

function resetDetail() {
  detailKey.value = null
  rawValue.value = null
  draft.value = ''
  editing.value = false
}

/**
 * 选择变化：分组不可选，keys 仅含叶子。
 * 单击 = 替换为单选；Ctrl/⌘ 点击 = 累加；Shift 点击 = 范围选（Naive 原生）。
 * 本次点击的叶子（meta.node）加载到右侧详情。
 */
function handleSelect(
  keys: Array<number | string>,
  _option: Array<null | TreeOption>,
  meta: { action: 'select' | 'unselect', node: null | TreeOption },
) {
  selectedKeys.value = keys.map(String)
  if (meta.action === 'select' && meta.node?.isLeaf) {
    void loadValue(String(meta.node.key))
  }
}

async function loadValue(key: string) {
  detailKey.value = key
  editing.value = false
  loadingValue.value = true
  try {
    const value = await cacheApi.getString(key)
    rawValue.value = value ?? null
    draft.value = value ?? ''
    format.value = isJson(value ?? '') ? 'json' : 'text'
  }
  catch {
    message.error(t('setting.cache.get_value_failed'))
    rawValue.value = null
  }
  finally {
    loadingValue.value = false
  }
}

function reloadValue() {
  if (detailKey.value) {
    void loadValue(detailKey.value)
  }
}

function isJson(text: string): boolean {
  const trimmed = text.trim()
  if (!trimmed || (!trimmed.startsWith('{') && !trimmed.startsWith('['))) {
    return false
  }
  try {
    JSON.parse(trimmed)
    return true
  }
  catch {
    return false
  }
}

/** 美化后的展示值（Json 视图）；非法 JSON 回退原文 */
const prettyValue = computed(() => {
  if (rawValue.value === null) {
    return ''
  }
  try {
    return JSON.stringify(JSON.parse(rawValue.value), null, 2)
  }
  catch {
    return rawValue.value
  }
})

const displayValue = computed(() => (format.value === 'json' ? prettyValue.value : (rawValue.value ?? '')))

/** 字节大小（人类可读） */
const sizeText = computed(() => {
  if (rawValue.value === null) {
    return ''
  }
  const bytes = new TextEncoder().encode(rawValue.value).length
  if (bytes < 1024) {
    return `${bytes} B`
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }
  return `${(bytes / 1024 / 1024).toFixed(2)} MB`
})

async function handleCopy() {
  if (rawValue.value === null) {
    return
  }
  try {
    await navigator.clipboard.writeText(displayValue.value)
    message.success(t('setting.cache.copied'))
  }
  catch {
    message.error(t('setting.cache.copy_failed'))
  }
}

function startEdit() {
  draft.value = rawValue.value ?? ''
  format.value = 'text'
  editing.value = true
}

function cancelEdit() {
  draft.value = rawValue.value ?? ''
  editing.value = false
}

async function handleSave() {
  if (!detailKey.value) {
    return
  }
  saving.value = true
  try {
    await cacheApi.updateString(detailKey.value, draft.value)
    message.success(t('setting.cache.saved'))
    editing.value = false
    reloadValue()
  }
  catch {
    message.error(t('setting.cache.save_failed_key_protected'))
  }
  finally {
    saving.value = false
  }
}

function handleDeleteCurrent() {
  const key = detailKey.value
  if (!key) {
    return
  }
  dialog.warning({
    title: t('setting.cache.confirm_delete_title'),
    content: t('setting.cache.confirm_delete_content', { key }),
    positiveText: t('setting.common.delete'),
    negativeText: t('setting.common.cancel'),
    onPositiveClick: async () => {
      try {
        await cacheApi.remove(key)
        message.success(t('setting.common.delete_success'))
        resetDetail()
        await loadKeys()
      }
      catch {
        message.error(t('setting.common.delete_failed'))
      }
    },
  })
}

function clearSelection() {
  selectedKeys.value = []
}

function handleBatchDelete() {
  const targets = [...selectedKeys.value]
  if (targets.length === 0) {
    return
  }
  dialog.warning({
    title: t('setting.cache.batch_delete_title'),
    content: t('setting.cache.batch_delete_content', { count: targets.length }),
    positiveText: t('setting.cache.confirm_delete_btn'),
    negativeText: t('setting.common.cancel'),
    onPositiveClick: async () => {
      const results = await Promise.allSettled(targets.map(key => cacheApi.remove(key)))
      const failed = results.filter(result => result.status === 'rejected').length
      if (failed === 0) {
        message.success(t('setting.cache.batch_deleted', { count: targets.length }))
      }
      else {
        message.warning(t('setting.cache.batch_delete_partial', { success: targets.length - failed, failed }))
      }
      if (detailKey.value && targets.includes(detailKey.value)) {
        resetDetail()
      }
      await loadKeys()
    },
  })
}

function handleDeleteByPattern() {
  const pattern = keyPattern.value.trim() || '*'
  dialog.warning({
    title: t('setting.cache.delete_by_pattern_title'),
    content: t('setting.cache.delete_by_pattern_content', { pattern }),
    positiveText: t('setting.cache.confirm_delete_btn'),
    negativeText: t('setting.common.cancel'),
    onPositiveClick: async () => {
      try {
        const count = await cacheApi.removeByPattern(pattern)
        message.success(t('setting.cache.deleted_by_pattern', { count }))
        resetDetail()
        await loadKeys()
      }
      catch {
        message.error(t('setting.cache.delete_by_pattern_failed'))
      }
    },
  })
}

onMounted(loadKeys)
</script>

<template>
  <div class="cache-page">
    <div class="cache-body">
      <!-- 左侧：键树 -->
      <NCard
        :bordered="false"
        size="small"
        class="cache-tree-card"
        :content-style="cardContentStyle"
      >
        <template #header>
          <div class="cache-card-header">
            <Icon icon="lucide:database-backup" width="15" />
            <span>{{ t('setting.cache.cache_keys') }}</span>
            <NTag v-if="keyCount > 0" size="tiny" type="info" :bordered="false" round>
              {{ keyCount }}
            </NTag>
          </div>
        </template>

        <div class="cache-tree-toolbar">
          <NInput
            v-model:value="keyPattern"
            size="small"
            :placeholder="t('setting.cache.key_pattern_placeholder')"
            clearable
            @keydown.enter="handleSearch"
          >
            <template #prefix>
              <NIcon :size="14">
                <Icon icon="lucide:search" />
              </NIcon>
            </template>
          </NInput>
          <NTooltip>
            <template #trigger>
              <NButton size="small" type="primary" :loading="loadingKeys" @click="handleSearch">
                <template #icon>
                  <NIcon><Icon icon="lucide:search" /></NIcon>
                </template>
              </NButton>
            </template>
            {{ t('setting.cache.search_by_pattern') }}
          </NTooltip>
        </div>

        <!-- 滚动区：相对壳 + 绝对内胆（脱离文档流，树高不撑页面），树在内部滚动 -->
        <div class="cache-scroll-host">
          <div class="cache-scroll-body">
            <NSpin class="cache-scroll-spin" :show="loadingKeys" size="small">
              <div v-if="cacheKeys.length === 0 && !loadingKeys" class="cache-empty">
                <NEmpty :description="t('setting.cache.empty_keys')" />
              </div>
              <NTree
                v-else
                v-model:selected-keys="selectedKeys"
                v-model:expanded-keys="expandedKeys"
                :data="treeData"
                block-line
                :multiple="canManage"
                selectable
                :render-label="renderTreeLabel"
                @update:selected-keys="handleSelect"
              />
            </NSpin>
          </div>
        </div>

        <!-- 选中/批量操作条（始终一行，不占树空间） -->
        <div v-if="canManage" class="cache-batch-bar">
          <span v-if="selectedCount > 0" class="cache-batch-count">
            {{ t('setting.cache.selected') }} <strong>{{ selectedCount }}</strong> {{ t('setting.cache.count_unit') }}
          </span>
          <span v-else class="cache-batch-hint">{{ t('setting.cache.multi_select_hint') }}</span>
          <div class="cache-batch-actions">
            <NButton v-if="selectedCount > 0" size="tiny" quaternary @click="clearSelection">
              {{ t('setting.cache.clear') }}
            </NButton>
            <NButton v-if="selectedCount > 0" size="tiny" type="error" @click="handleBatchDelete">
              {{ t('setting.cache.delete_selected') }}
            </NButton>
            <NButton v-else size="tiny" quaternary type="warning" @click="handleDeleteByPattern">
              {{ t('setting.cache.delete_by_pattern') }}
            </NButton>
          </div>
        </div>
      </NCard>

      <!-- 右侧：键值 -->
      <NCard
        :bordered="false"
        size="small"
        class="cache-detail-card"
        :content-style="cardContentStyle"
      >
        <template #header>
          <div v-if="detailKey" class="cache-detail-header">
            <span class="cache-detail-key" :title="detailKey">{{ detailKey }}</span>
          </div>
          <div v-else class="cache-card-header">
            <Icon icon="lucide:file-json" width="15" />
            <span>{{ t('setting.cache.cache_content') }}</span>
          </div>
        </template>

        <template v-if="detailKey" #header-extra>
          <div class="cache-detail-actions">
            <NTag v-if="sizeText" size="tiny" :bordered="false">
              {{ sizeText }}
            </NTag>
            <NRadioGroup v-if="!editing" v-model:value="format" size="small">
              <NRadioButton value="text">
                Text
              </NRadioButton>
              <NRadioButton value="json">
                Json
              </NRadioButton>
            </NRadioGroup>
            <NTooltip>
              <template #trigger>
                <NButton size="tiny" quaternary @click="handleCopy">
                  <template #icon>
                    <NIcon><Icon icon="lucide:copy" /></NIcon>
                  </template>
                </NButton>
              </template>
              {{ t('setting.common.copy') }}
            </NTooltip>
            <NTooltip>
              <template #trigger>
                <NButton size="tiny" quaternary @click="reloadValue">
                  <template #icon>
                    <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
                  </template>
                </NButton>
              </template>
              {{ t('setting.common.refresh') }}
            </NTooltip>
            <NButton v-if="canManage && !editing" size="tiny" @click="startEdit">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil-line" /></NIcon>
              </template>
              {{ t('setting.cache.edit') }}
            </NButton>
            <NTooltip v-if="canManage">
              <template #trigger>
                <NButton size="tiny" quaternary type="error" @click="handleDeleteCurrent">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              {{ t('setting.cache.delete_this_key') }}
            </NTooltip>
          </div>
        </template>

        <!-- 滚动区：相对壳 + 绝对内胆，详情在内部滚动 -->
        <div class="cache-scroll-host">
          <div class="cache-scroll-body">
            <NSpin class="cache-scroll-spin" :show="loadingValue" size="small">
              <div v-if="!detailKey" class="cache-empty">
                <NEmpty :description="t('setting.cache.select_key_hint')" />
              </div>
              <div v-else-if="rawValue === null" class="cache-empty">
                <NEmpty :description="t('setting.cache.key_not_exist')" />
              </div>
              <template v-else>
                <!-- 编辑态：文本域 + 保存/取消 -->
                <template v-if="editing">
                  <NInput
                    v-model:value="draft"
                    type="textarea"
                    class="cache-value-editor"
                    :placeholder="t('setting.cache.value_placeholder')"
                  />
                  <div class="cache-edit-actions">
                    <NButton size="small" @click="cancelEdit">
                      {{ t('setting.common.cancel') }}
                    </NButton>
                    <NButton size="small" type="primary" :loading="saving" @click="handleSave">
                      {{ t('setting.common.save') }}
                    </NButton>
                  </div>
                </template>
                <!-- 预览态：只读 -->
                <pre v-else class="cache-value-pre">{{ displayValue }}</pre>
              </template>
            </NSpin>
          </div>
        </div>
      </NCard>
    </div>
  </div>
</template>

<style scoped>
.cache-page {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  height: 100%;
  padding: 12px;
  overflow: hidden;
}

/*
 * 左右行容器：flex:1 + height:0 收敛到视口剩余高度（同 SchemaPage 表格区 class="flex-1" style="height:0"）。
 * height:0 是关键——给 flex item 一个确定的 0 起点，避免 flex-basis 退化为内容高度（树自然高）反向撑破父链。
 */
.cache-body {
  display: flex;
  flex: 1;
  gap: 12px;
  height: 0;
  /* 固定值兜底：极端布局模式下高度链失效时，左右栏仍有最小可用高度而不是塌成 0 */
  min-height: 360px;
  /* page 锁定高度后，行容器在其内不被子（树/详情）撑破，迫使滚动发生在左右两栏内部 */
  overflow: hidden;
}

/* ── 左侧键树卡片 ── */
.cache-tree-card {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  width: 460px;
  min-width: 360px;
  overflow: hidden;
}

.cache-tree-toolbar {
  display: flex;
  flex-shrink: 0;
  gap: 8px;
  margin-bottom: 8px;
}

/*
 * 滚动区：相对壳 + 绝对内胆。
 * 内胆 position:absolute 脱离文档流，对外固有高度为 0——树/详情再高也不会经 flex-basis
 * 把内容高度灌进框架的内容高度链（该链为内容驱动 + 视口下限），页面恒收敛于视口，
 * 滚动只发生在内胆里。
 */
.cache-scroll-host {
  position: relative;
  flex: 1;
  min-height: 0;
}

.cache-scroll-body {
  position: absolute;
  inset: 0;
  overflow: auto;
}

/* NSpin 占满内胆（内容少时空态居中/编辑器撑满），内容多时自然撑高、由内胆滚动 */
.cache-scroll-spin {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

.cache-scroll-spin :deep(.n-spin-content) {
  display: flex;
  flex: 1;
  flex-direction: column;
}

.cache-tree-leaf {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
}

.cache-tree-group {
  display: inline-flex;
  gap: 4px;
  align-items: center;
  font-size: 13px;
}

.cache-batch-bar {
  display: flex;
  flex-shrink: 0;
  gap: 8px;
  align-items: center;
  justify-content: space-between;
  padding-top: 8px;
  margin-top: 8px;
  border-top: 1px solid var(--n-border-color, rgb(239 239 245));
}

.cache-batch-count {
  font-size: 12px;
  color: var(--text-secondary, rgb(118 124 130));
}

.cache-batch-count strong {
  color: var(--n-text-color);
}

.cache-batch-hint {
  font-size: 12px;
  color: var(--text-secondary, rgb(150 154 160));
}

.cache-batch-actions {
  display: flex;
  gap: 6px;
  align-items: center;
}

/* ── 右侧详情卡片 ── */
.cache-detail-card {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
}

.cache-card-header {
  display: flex;
  gap: 6px;
  align-items: center;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.cache-detail-header {
  display: flex;
  min-width: 0;
}

.cache-detail-key {
  overflow: hidden;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 13px;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cache-detail-actions {
  display: flex;
  gap: 8px;
  align-items: center;
}

.cache-value-pre {
  flex: 1;
  min-height: 0;
  padding: 12px;
  margin: 0;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  line-height: 1.6;
  word-break: break-all;
  white-space: pre-wrap;
  background: var(--n-action-color, rgb(250 250 252));
  border-radius: 6px;
}

.cache-value-editor {
  flex: 1;
  min-height: 0;
}

.cache-value-editor :deep(textarea) {
  height: 100% !important;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
}

.cache-edit-actions {
  display: flex;
  flex-shrink: 0;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 8px;
}

.cache-empty {
  display: flex;
  flex: 1;
  align-items: center;
  justify-content: center;
  min-height: 200px;
}

@media (max-width: 768px) {
  .cache-body {
    flex-direction: column;
  }

  /* 绝对内胆不再有固有高度，小屏给树卡显式高度，详情占余下空间 */
  .cache-tree-card {
    flex: none;
    width: 100%;
    min-width: 0;
    height: 320px;
  }
}
</style>
