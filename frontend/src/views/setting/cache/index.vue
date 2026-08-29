<script setup lang="ts">
import { XhButton, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { cacheApi } from '@/api'
import { Icon, XInput, XSegmented, XTooltip, XTree } from '~/components'
import { dialog, toast } from '~/composables'
import { usePermission } from '~/hooks'

defineOptions({ name: 'PlatformCachePage' })

const { t } = useI18n()
const { hasPermission } = usePermission()

/** 维护权限（写入/删除）；无则只读浏览 */
const canManage = computed(() => hasPermission('setting.cache.clear'))

/**
 * 卡片内容容器样式：让内容区成为定高 flex 列。
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

interface CacheTreeOption {
  value: string
  label: string
  /** 分组节点的叶子数；叶子节点没有 */
  leafCount?: number
  children?: CacheTreeOption[]
}

/**
 * 扁平键 → 树（按 : 分组）。叶子与分组分槽存储：
 * 同一路径既是某键又是更深键前缀（如 a:b 与 a:b:c 并存）时不互相覆盖、不丢键。
 * 分组键带 __group__ 前缀，选中回调据此只取叶子，避免多选时混入分组。
 */
const treeData = computed<CacheTreeOption[]>(() => {
  const root: CacheTreeOption[] = []
  const groupMap = new Map<string, CacheTreeOption>()
  const leafMap = new Map<string, CacheTreeOption>()

  const ensureGroup = (pathAccum: string, segment: string, parent: CacheTreeOption[]): CacheTreeOption => {
    let node = groupMap.get(pathAccum)
    if (!node) {
      node = { value: `${GROUP_PREFIX}${pathAccum}`, label: segment, children: [], leafCount: 0 }
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
      parentChildren = group.children!
    }

    const leafSegment = parts[parts.length - 1]!
    if (!leafMap.has(key)) {
      const leaf: CacheTreeOption = { value: key, label: leafSegment }
      leafMap.set(key, leaf)
      parentChildren.push(leaf)
    }
  }

  return root
})

function renderTreeLabel(option: Record<string, unknown>) {
  const node = option as unknown as CacheTreeOption
  if (!node.children?.length) {
    return h('span', { class: 'cache-tree-leaf', title: node.value }, String(node.label ?? ''))
  }
  return h('span', { class: 'cache-tree-group' }, [
    h('span', null, String(node.label ?? '')),
    // 键数用徽标显示（与顶部「缓存键」计数同款），不再用括号
    h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'info' }, () => h(XhTagLabel, () => String(node.leafCount ?? 0))),
  ])
}

const keyCount = computed(() => cacheKeys.value.length)
const selectedCount = computed(() => selectedKeys.value.length)

function collectGroupKeys(nodes: CacheTreeOption[]): string[] {
  const keys: string[] = []
  const walk = (list: CacheTreeOption[]) => {
    for (const node of list) {
      if (node.children?.length) {
        keys.push(node.value)
        walk(node.children ?? [])
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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.cache.query_keys_failed'))
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
 * 选择变化：级联收敛后 keys 仅含叶子，分组键（若有）在此滤掉。
 * 本次只新增一个键时把它加载到右侧详情；勾目录一次进来一批，右侧不动。
 */
function handleSelect(keys: string[]) {
  // 分组键只用于展开，不参与选择
  const leaves = keys.filter(key => !key.startsWith(GROUP_PREFIX))
  const previous = new Set(selectedKeys.value)
  const added = leaves.filter(key => !previous.has(key))
  selectedKeys.value = leaves
  const only = added.length === 1 ? added[0] : undefined
  if (only && only !== detailKey.value) {
    void loadValue(only)
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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.cache.get_value_failed'))
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
    toast.success(t('setting.cache.copied'))
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.cache.copy_failed'))
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
    toast.success(t('setting.cache.saved'))
    editing.value = false
    reloadValue()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.cache.save_failed_key_protected'))
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
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('setting.cache.confirm_delete_title'),
    content: t('setting.cache.confirm_delete_content', { key }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await cacheApi.remove(key)
        toast.success(t('common.messages.delete_success'))
        resetDetail()
        await loadKeys()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('common.messages.delete_failed'))
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
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('setting.cache.batch_delete_title'),
    content: t('setting.cache.batch_delete_content', { count: targets.length }),
    okText: t('setting.cache.confirm_delete_btn'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      const results = await Promise.allSettled(targets.map(key => cacheApi.remove(key)))
      const failed = results.filter(result => result.status === 'rejected').length
      if (failed === 0) {
        toast.success(t('setting.cache.batch_deleted', { count: targets.length }))
      }
      else {
        toast.warning(t('setting.cache.batch_delete_partial', { success: targets.length - failed, failed }))
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
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('setting.cache.delete_by_pattern_title'),
    content: t('setting.cache.delete_by_pattern_content', { pattern }),
    okText: t('setting.cache.confirm_delete_btn'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const count = await cacheApi.removeByPattern(pattern)
        toast.success(t('setting.cache.deleted_by_pattern', { count }))
        resetDetail()
        await loadKeys()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('setting.cache.delete_by_pattern_failed'))
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
      <XhCardRoot class="cache-tree-card">
        <XhCardHeader>
          <div class="cache-card-header">
            <Icon icon="lucide:database-backup" width="15" />
            <span>{{ t('setting.cache.cache_keys') }}</span>
            <XhTagRoot v-if="keyCount > 0" variant="subtle" size="sm" tone="info">
              <XhTagLabel>
                {{ keyCount }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </XhCardHeader>
        <XhCardBody :style="cardContentStyle">
          <div class="cache-tree-toolbar">
            <XInput
              v-model:value="keyPattern"
              size="sm"
              :placeholder="t('setting.cache.key_pattern_placeholder')"
              clearable
              @keydown.enter="handleSearch"
            >
              <template #prefix>
                <Icon width="14" height="14" icon="lucide:search" />
              </template>
            </XInput>
            <XTooltip :content="t('setting.cache.search_by_pattern')">
              <XhButton size="sm" tone="brand" :loading="loadingKeys" @click="handleSearch">
                <span><Icon icon="lucide:search" /></span>
              </XhButton>
            </XTooltip>
          </div>

          <!-- 滚动区：相对壳 + 绝对内胆（脱离文档流，树高不撑页面），树在内部滚动 -->
          <div class="cache-scroll-host">
            <div class="cache-scroll-body">
              <div class="xh-loading-stage" :class="{ 'is-loading': loadingKeys }">
                <div class="xh-loading-stage__veil">
                  <XhSpinner />
                </div>
                <div v-if="cacheKeys.length === 0 && !loadingKeys" class="cache-empty">
                  <XhEmptyStateRoot size="sm">
                    <XhEmptyStateIcon>
                      <Icon icon="lucide:inbox" width="28" height="28" />
                    </XhEmptyStateIcon>
                    <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('setting.cache.empty_keys') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                </div>
                <!-- 管理档点目录名即勾整枝、展开归箭头；只读档没有勾选，点目录名照常展开 -->
                <XTree
                  v-else
                  v-model:expanded-keys="expandedKeys"
                  :selected-keys="selectedKeys"
                  :data="treeData"
                  :selection-mode="canManage ? 'multiple' : 'single'"
                  :cascade="canManage"
                  checked-strategy="child"
                  :expand-on-click="!canManage"
                  :render-label="renderTreeLabel"
                  @update:selected-keys="handleSelect"
                />
              </div>
            </div>
          </div>

          <!-- 选中/批量操作条（始终一行，不占树空间） -->
          <div v-if="canManage" class="cache-batch-bar">
            <span v-if="selectedCount > 0" class="cache-batch-count">
              {{ t('setting.cache.selected') }} <strong>{{ selectedCount }}</strong> {{ t('setting.cache.count_unit') }}
            </span>
            <span v-else class="cache-batch-hint">{{ t('setting.cache.multi_select_hint') }}</span>
            <div class="cache-batch-actions">
              <XhButton v-if="selectedCount > 0" size="sm" variant="ghost" @click="clearSelection">
                {{ t('setting.cache.clear') }}
              </XhButton>
              <XhButton v-if="selectedCount > 0" size="sm" tone="danger" @click="handleBatchDelete">
                {{ t('setting.cache.delete_selected') }}
              </XhButton>
              <XhButton v-else size="sm" variant="ghost" tone="warning" @click="handleDeleteByPattern">
                {{ t('setting.cache.delete_by_pattern') }}
              </XhButton>
            </div>
          </div>
        </XhCardBody>
      </XhCardRoot>

      <!-- 右侧：键值 -->
      <XhCardRoot class="cache-detail-card">
        <XhCardHeader>
          <div v-if="detailKey" class="cache-detail-header">
            <span class="cache-detail-key" :title="detailKey">{{ detailKey }}</span>
            <div class="cache-detail-actions">
              <XhTagRoot v-if="sizeText" variant="subtle" size="sm">
                <XhTagLabel>
                  {{ sizeText }}
                </XhTagLabel>
              </XhTagRoot>
              <XSegmented v-if="!editing" v-model:value="format" :options="[{ value: 'text', label: 'Text' }, { value: 'json', label: 'Json' }]" size="sm" />
              <XTooltip :content="t('common.actions.copy')">
                <XhButton size="sm" variant="ghost" @click="handleCopy">
                  <span><Icon icon="lucide:copy" /></span>
                </XhButton>
              </XTooltip>
              <XTooltip :content="t('common.actions.refresh')">
                <XhButton size="sm" variant="ghost" @click="reloadValue">
                  <span><Icon icon="lucide:refresh-cw" /></span>
                </XhButton>
              </XTooltip>
              <XhButton v-if="canManage && !editing" size="sm" @click="startEdit">
                <span><Icon icon="lucide:pencil-line" /></span>
                {{ t('setting.cache.edit') }}
              </XhButton>
              <XTooltip :content="t('setting.cache.delete_this_key')">
                <XhButton v-if="canManage" size="sm" variant="ghost" tone="danger" @click="handleDeleteCurrent">
                  <span><Icon icon="lucide:trash-2" /></span>
                </XhButton>
              </XTooltip>
            </div>
          </div>
          <div v-else class="cache-card-header">
            <Icon icon="lucide:file-json" width="15" />
            <span>{{ t('setting.cache.cache_content') }}</span>
          </div>
        </XhCardHeader>
        <XhCardBody :style="cardContentStyle">
          <!-- 滚动区：相对壳 + 绝对内胆，详情在内部滚动 -->
          <div class="cache-scroll-host">
            <div class="cache-scroll-body">
              <div class="xh-loading-stage" :class="{ 'is-loading': loadingValue }">
                <div class="xh-loading-stage__veil">
                  <XhSpinner />
                </div>
                <div v-if="!detailKey" class="cache-empty">
                  <XhEmptyStateRoot>
                    <XhEmptyStateIcon>
                      <Icon icon="lucide:mouse-pointer-click" width="28" height="28" />
                    </XhEmptyStateIcon>
                    <XhEmptyStateTitle>{{ t('setting.cache.select_key_hint_title') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('setting.cache.select_key_hint') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                </div>
                <div v-else-if="rawValue === null" class="cache-empty">
                  <XhEmptyStateRoot>
                    <XhEmptyStateIcon>
                      <Icon icon="lucide:search-x" width="28" height="28" />
                    </XhEmptyStateIcon>
                    <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('setting.cache.key_not_exist') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                </div>
                <template v-else>
                  <!-- 编辑态：文本域 + 保存/取消 -->
                  <template v-if="editing">
                    <XInput
                      v-model:value="draft"
                      type="textarea"
                      class="cache-value-editor"
                      :placeholder="t('setting.cache.value_placeholder')"
                    />
                    <div class="cache-edit-actions">
                      <XhButton size="sm" @click="cancelEdit">
                        {{ t('common.actions.cancel') }}
                      </XhButton>
                      <XhButton size="sm" tone="brand" :loading="saving" @click="handleSave">
                        {{ t('common.actions.save') }}
                      </XhButton>
                    </div>
                  </template>
                  <!-- 预览态：只读 -->
                  <pre v-else class="cache-value-pre">{{ displayValue }}</pre>
                </template>
              </div>
            </div>
          </div>
        </XhCardBody>
      </XhCardRoot>
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

/* 内胆里这一层是唯一的内容承载层：撑满内胆并成为 flex 列，
   下面的树 / pre / 编辑器才有可分配的高度。
   内容多时自然撑高，由内胆滚动 */
.cache-scroll-body > .xh-loading-stage {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

/* 树自带 24rem 的最大高（--xh-viewport-h-lg），不放开就是一块矮框加大片空白 */
.cache-tree-card .xh-loading-stage {
  --xh-tree-max-h: 100%;
}

.cache-tree-card :deep(.x-tree),
.cache-tree-card :deep([data-scope='tree'][data-part='tree']) {
  flex: 1;
  min-block-size: 0;
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
  border-top: 1px solid var(--xh-border-default);
}

.cache-batch-count {
  font-size: 12px;
  color: var(--text-secondary, rgb(118 124 130));
}

.cache-batch-count strong {
  color: hsl(var(--foreground));
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
  background: var(--xh-bg-subtle);
  border-radius: 6px;
}

.cache-value-editor {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
}

.cache-value-editor :deep(.x-input__box) {
  flex: 1;
  min-block-size: 0;
}

/* textarea 的高由 rows 定死，百分比和 align-items: stretch 都拿不动它；
   输入盒本身是定位参照，直接把 textarea 铺满它 */
.cache-value-editor :deep(textarea) {
  position: absolute;
  inset: 0;
  resize: none;
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
