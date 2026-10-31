<script setup lang="ts">
import type { CacheNamespace, CacheToast, CacheTreeOption } from './cache'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhButtonPrefix, XhCardContent, XhCardHeader, XhCardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XInput, XSegmented, XSelect, XTooltip, XTree } from '~/components'
import { dialog, toast } from '~/composables'
import { usePermission } from '~/hooks'
import { errorText } from '../jobs/job'
import {
  batchDeleteToast,
  buildKeyTree,
  canEditEntry,
  collectGroupKeys,
  deleteByPrefix,
  deleteKeys,
  EXPAND_ALL_LIMIT,
  formatSize,
  goneKeys,
  isJson,
  keyPrefix,
  layerLabel,
  leafKeys,
  loadNamespaces,
  namespaceOptions,
  prettyJson,
  readOnlyReason,
  singleDeleteToast,
  slidingText,
  timeToLiveText,
  useCacheEntry,
  useCacheKeys,
} from './cache'

defineOptions({ name: 'MaintenanceCachesView' })

const { t } = useI18n()
const { hasPermission } = usePermission()

/** 删除（单个、批量、按前缀）与改写各有权限；都没有时只读浏览 */
const canDelete = computed(() => hasPermission('maintenance.caches.delete'))
const canUpdate = computed(() => hasPermission('maintenance.caches.update'))

/**
 * 卡片内容容器样式：让内容区成为定高 flex 列，
 * 使内部滚动壳能撑满并收敛——与 SchemaPage 的做法一致。
 */
const cardContentStyle = {
  display: 'flex',
  flex: '1',
  flexDirection: 'column',
  minHeight: '0',
  padding: 'var(--xh-space-2) var(--xh-space-3)',
  overflow: 'hidden',
} as const

// ── 命名空间与前缀（4.x 的键模式：6.0 只认前缀，先选命名空间、再按需补子前缀） ──
const namespaces = ref<CacheNamespace[]>([])
const loadingNamespaces = ref(false)
const namespacePrefix = ref<null | string>(null)
const subPrefix = ref('')
const namespaceSelectOptions = computed(() => namespaceOptions(t, namespaces.value))

/** 当前要查（或按前缀删）的完整前缀；没有可选的命名空间时为 null */
const searchPrefix = computed(() => (namespacePrefix.value === null ? null : keyPrefix(namespacePrefix.value, subPrefix.value)))

// ── 键值状态（右侧详情；左侧选中时要读当前键，故先声明） ──
const entryState = useCacheEntry()
const { key: detailKey, entry, loading: loadingValue } = entryState
const draft = ref('')
const format = ref<'json' | 'text'>('text')
const editing = ref(false)
const saving = ref(false)

// ── 键列表（左侧） ──
const keyList = useCacheKeys()
const { keys: cacheKeys, objectKeys, continuation, loading: loadingKeys, appending } = keyList
const selectedKeys = ref<string[]>([])
const expandedKeys = ref<string[]>([])

const treeData = computed(() => buildKeyTree(cacheKeys.value))
const keyCount = computed(() => cacheKeys.value.length)
const hasMore = computed(() => continuation.value !== null)
/** 还有下一页时计数后带 +：列表只是已加载的部分 */
const keyCountText = computed(() => (hasMore.value ? `${keyCount.value}+` : String(keyCount.value)))
const selectedCount = computed(() => selectedKeys.value.length)

function renderTreeLabel(option: Record<string, unknown>) {
  const node = option as unknown as CacheTreeOption
  if (!node.children?.length) {
    const leaf = h('span', { class: 'cache-tree-leaf', title: node.value }, String(node.label ?? ''))
    if (!objectKeys.value.has(node.value))
      return leaf
    // 进程内对象缓存的条目（不在字节存储里）标出来；键长时省略号收起，标签始终露出
    return h('span', { class: 'cache-tree-leaf-row' }, [
      leaf,
      h(XhTagRoot, { class: 'cache-tree-leaf-tag', variant: 'subtle', size: 'sm', tone: 'neutral' }, () => h(XhTagLabel, () => t('maintenance.caches.object_tag'))),
    ])
  }
  return h('span', { class: 'cache-tree-group' }, [
    h('span', null, String(node.label ?? '')),
    // 键数用徽标显示（与顶部「缓存键」计数同款），不再用括号
    h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'info' }, () => h(XhTagLabel, () => String(node.leafCount ?? 0))),
  ])
}

/** 键不多时整棵树展开；重查时键多就全收起，加载更多时保留用户的展开 */
function syncExpanded(reset: boolean) {
  if (cacheKeys.value.length <= EXPAND_ALL_LIMIT) {
    expandedKeys.value = collectGroupKeys(treeData.value)
  }
  else if (reset) {
    expandedKeys.value = []
  }
}

async function loadKeys() {
  const prefix = searchPrefix.value
  if (prefix === null) {
    return
  }
  try {
    if (await keyList.load(prefix)) {
      selectedKeys.value = []
      syncExpanded(true)
    }
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.caches.query_keys_failed'))
  }
}

async function handleLoadMore() {
  try {
    if (await keyList.loadMore()) {
      syncExpanded(false)
    }
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.caches.query_keys_failed'))
  }
}

async function handleSearch() {
  resetDetail()
  await loadKeys()
}

/** 换命名空间即重查；子前缀是相对上一个命名空间写的，一并清掉 */
function changeNamespace(value: unknown) {
  namespacePrefix.value = typeof value === 'string' ? value : null
  subPrefix.value = ''
  void handleSearch()
}

async function loadNamespaceList() {
  loadingNamespaces.value = true
  try {
    namespaces.value = await loadNamespaces()
    namespacePrefix.value = namespaces.value[0]?.prefix ?? null
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.caches.load_namespaces_failed'))
  }
  finally {
    loadingNamespaces.value = false
  }
}

/**
 * 选择变化：级联收敛后 keys 仅含叶子，分组键（若有）在此滤掉。
 * 本次只新增一个键时把它加载到右侧详情；勾目录一次进来一批，右侧不动。
 */
function handleSelect(keys: string[]) {
  const leaves = leafKeys(keys)
  const previous = new Set(selectedKeys.value)
  const added = leaves.filter(key => !previous.has(key))
  selectedKeys.value = leaves
  const only = added.length === 1 ? added[0] : undefined
  if (only && only !== detailKey.value) {
    void loadValue(only)
  }
}

function clearSelection() {
  selectedKeys.value = []
}

// ── 键值（右侧） ──
/** 值按 UTF-8 解出的文本；不是文本（二进制）为 null */
const valueText = computed(() => entry.value?.text ?? null)
/** 不能改写的原因：二进制值，或鉴权相关的只读命名空间 */
const reason = computed(() => (entry.value ? readOnlyReason(entry.value) : null))
const canEdit = computed(() => canEditEntry(entry.value, canUpdate.value))
const displayValue = computed(() => {
  if (valueText.value === null) {
    return ''
  }
  return format.value === 'json' ? prettyJson(valueText.value) : valueText.value
})

function resetDetail() {
  entryState.reset()
  draft.value = ''
  editing.value = false
}

async function loadValue(key: string) {
  editing.value = false
  try {
    await entryState.open(key)
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.caches.get_value_failed'))
  }
  // 连点几个键时只有最后一个落到详情；晚到的旧响应不改草稿与视图
  if (detailKey.value === key) {
    const value = entry.value?.text ?? ''
    draft.value = value
    format.value = isJson(value) ? 'json' : 'text'
  }
}

function reloadValue() {
  if (detailKey.value) {
    void loadValue(detailKey.value)
  }
}

async function handleCopy() {
  if (valueText.value === null) {
    return
  }
  try {
    await navigator.clipboard.writeText(displayValue.value)
    toast.success(t('maintenance.caches.copied'))
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.caches.copy_failed'))
  }
}

function startEdit() {
  draft.value = valueText.value ?? ''
  format.value = 'text'
  editing.value = true
}

function cancelEdit() {
  draft.value = valueText.value ?? ''
  editing.value = false
}

/** 保存：过期设置沿用原项，以返回的缓存项刷新详情（4.x 保存后重新取值） */
async function handleSave() {
  if (!detailKey.value || !draft.value) {
    return
  }
  saving.value = true
  try {
    await entryState.save(draft.value)
    toast.success(t('maintenance.caches.saved'))
    editing.value = false
    format.value = isJson(valueText.value ?? '') ? 'json' : 'text'
  }
  catch (error) {
    toast.danger(errorText(error) || t('common.messages.save_failed'))
  }
  finally {
    saving.value = false
  }
}

function showToast(result: CacheToast) {
  toast[result.tone](result.message)
}

/**
 * 删掉之后：列表里拿掉这些键（续页标记不动，已加载的后几页不丢），选中与详情里的一并清掉。
 * 4.x 删完整表重查；6.0 按续页分页，重查会丢掉已加载的后几页。
 */
function afterDelete(gone: readonly string[]) {
  const set = new Set(gone)
  keyList.discard(gone)
  selectedKeys.value = selectedKeys.value.filter(key => !set.has(key))
  if (detailKey.value && set.has(detailKey.value)) {
    resetDetail()
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
    title: t('maintenance.caches.confirm_delete_title'),
    content: t('maintenance.caches.confirm_delete_content', { key }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const outcome = await deleteKeys([key])
        showToast(singleDeleteToast(t, outcome))
        afterDelete(goneKeys(outcome))
      }
      catch (error) {
        toast.danger(errorText(error) || t('common.messages.delete_failed'))
      }
    },
  })
}

function handleBatchDelete() {
  const targets = [...selectedKeys.value]
  if (targets.length === 0) {
    return
  }
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('maintenance.caches.batch_delete_title'),
    content: t('maintenance.caches.batch_delete_content', { count: targets.length }),
    okText: t('maintenance.caches.confirm_delete_btn'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const outcome = await deleteKeys(targets)
        showToast(batchDeleteToast(t, outcome))
        afterDelete(goneKeys(outcome))
      }
      catch (error) {
        toast.danger(errorText(error) || t('common.messages.delete_failed'))
        // 超过 100 个分批发，前几批可能已经删掉：重查一遍以实际为准
        resetDetail()
        await loadKeys()
      }
    },
  })
}

function handleDeleteByPrefix() {
  const prefix = searchPrefix.value
  if (prefix === null) {
    return
  }
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('maintenance.caches.delete_by_prefix_title'),
    content: t('maintenance.caches.delete_by_prefix_content', { prefix }),
    okText: t('maintenance.caches.confirm_delete_btn'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const count = await deleteByPrefix(prefix)
        toast.success(t('maintenance.caches.deleted_by_prefix', { count }))
        resetDetail()
        await loadKeys()
      }
      catch (error) {
        toast.danger(errorText(error) || t('maintenance.caches.delete_by_prefix_failed'))
      }
    },
  })
}

onMounted(async () => {
  await loadNamespaceList()
  await loadKeys()
})
</script>

<template>
  <div class="cache-page">
    <div class="cache-body">
      <!-- 左侧：键树 -->
      <XhCardRoot class="cache-tree-card">
        <XhCardHeader>
          <div class="cache-card-header">
            <Icon icon="lucide:database-backup" width="15" />
            <span>{{ t('maintenance.caches.cache_keys') }}</span>
            <XhTagRoot v-if="keyCount > 0" variant="subtle" size="sm" tone="info">
              <XhTagLabel>
                {{ keyCountText }}
              </XhTagLabel>
            </XhTagRoot>
          </div>
        </XhCardHeader>
        <XhCardContent :style="cardContentStyle">
          <!-- 命名空间独占一行（选项文本带包名与只读标记，并排会被截断）；下一行是 4.x 的输入 + 搜索，输入框前缀处写出所选命名空间 -->
          <div class="cache-tree-toolbar">
            <XSelect
              class="cache-namespace-select"
              :value="namespacePrefix"
              :options="namespaceSelectOptions"
              :loading="loadingNamespaces"
              :placeholder="t('maintenance.caches.namespace_placeholder')"
              :aria-label="t('maintenance.caches.namespace')"
              @update:value="changeNamespace"
            />
            <div class="cache-tree-search">
              <XInput
                v-model:value="subPrefix"
                class="cache-prefix-input"
                size="sm"
                :placeholder="t('maintenance.caches.sub_prefix_placeholder')"
                :aria-label="t('maintenance.caches.sub_prefix')"
                :disabled="namespacePrefix === null"
                clearable
                @enter="handleSearch"
              >
                <template v-if="namespacePrefix" #prefix>
                  <span class="cache-prefix-text">{{ namespacePrefix }}</span>
                </template>
              </XInput>
              <XTooltip :content="t('maintenance.caches.search_by_prefix')">
                <XhButton
                  icon-only
                  variant="subtle"
                  size="sm"
                  tone="brand"
                  :aria-label="t('maintenance.caches.search_by_prefix')"
                  :loading="loadingKeys && !appending"
                  :disabled="namespacePrefix === null"
                  @click="handleSearch"
                >
                  <XhButtonIndicator />
                  <XhButtonPrefix><Icon icon="lucide:search" /></XhButtonPrefix>
                </XhButton>
              </XTooltip>
            </div>
          </div>

          <!-- 滚动区：相对壳 + 绝对内胆（脱离文档流，树高不撑页面），树在内部滚动 -->
          <div class="cache-scroll-host">
            <div class="cache-scroll-body">
              <div class="xh-loading-stage" :class="{ 'is-loading': (loadingKeys && !appending) || loadingNamespaces }">
                <div class="xh-loading-stage__veil">
                  <XhSpinner />
                </div>
                <div v-if="cacheKeys.length === 0 && !loadingKeys && !loadingNamespaces" class="cache-empty">
                  <XhEmptyStateRoot size="sm">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:inbox" width="28" height="28" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>
                      {{ namespaces.length === 0 ? t('maintenance.caches.no_namespaces') : t('maintenance.caches.empty_keys') }}
                    </XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                </div>
                <!-- 点行只管选中（可删除时勾目录即连同整枝），展开一律交给箭头 -->
                <XTree
                  v-else-if="cacheKeys.length > 0"
                  v-model:expanded-keys="expandedKeys"
                  :selected-keys="selectedKeys"
                  :data="treeData"
                  :aria-label="t('maintenance.caches.cache_keys')"
                  :multiple="canDelete"
                  :cascade="canDelete"
                  checked-strategy="child"
                  :render-label="renderTreeLabel"
                  @update:selected-keys="handleSelect"
                />
                <!-- 续页：Redis 的一页可能是空的，空态下也要能往下翻 -->
                <div v-if="hasMore" class="cache-load-more">
                  <XhButton size="sm" variant="ghost" :loading="loadingKeys && appending" :disabled="loadingKeys" @click="handleLoadMore">
                    <XhButtonIndicator />
                    <XhButtonPrefix><Icon icon="lucide:chevrons-down" /></XhButtonPrefix>
                    <XhButtonLabel>{{ t('maintenance.caches.load_more') }}</XhButtonLabel>
                  </XhButton>
                </div>
              </div>
            </div>
          </div>

          <!-- 选中/批量操作条（始终一行，不占树空间） -->
          <div v-if="canDelete" class="cache-batch-bar">
            <span v-if="selectedCount > 0" class="cache-batch-count">
              {{ t('maintenance.caches.selected') }} <strong>{{ selectedCount }}</strong> {{ t('maintenance.caches.count_unit') }}
            </span>
            <span v-else class="cache-batch-hint">{{ t('maintenance.caches.multi_select_hint') }}</span>
            <div class="cache-batch-actions">
              <XhButton v-if="selectedCount > 0" size="sm" variant="ghost" @click="clearSelection">
                {{ t('maintenance.caches.clear') }}
              </XhButton>
              <XhButton v-if="selectedCount > 0" variant="subtle" size="sm" tone="danger" @click="handleBatchDelete">
                {{ t('maintenance.caches.delete_selected') }}
              </XhButton>
              <XhButton v-else size="sm" variant="ghost" tone="warning" :disabled="searchPrefix === null" @click="handleDeleteByPrefix">
                {{ t('maintenance.caches.delete_by_prefix') }}
              </XhButton>
            </div>
          </div>
        </XhCardContent>
      </XhCardRoot>

      <!-- 右侧：键值 -->
      <XhCardRoot class="cache-detail-card">
        <XhCardHeader>
          <div v-if="detailKey" class="cache-detail-header">
            <span class="cache-detail-key" :title="detailKey">{{ detailKey }}</span>
            <div class="cache-detail-actions">
              <XhTagRoot v-if="entry && entry.size !== null && entry.size !== undefined" variant="subtle" size="sm">
                <XhTagLabel>
                  {{ formatSize(entry.size) }}
                </XhTagLabel>
              </XhTagRoot>
              <XhTagRoot v-if="reason === 'read-only'" variant="subtle" size="sm" tone="warning">
                <XhTagLabel>
                  {{ t('maintenance.caches.read_only') }}
                </XhTagLabel>
              </XhTagRoot>
              <XSegmented v-if="valueText !== null && !editing" v-model:value="format" :options="[{ value: 'text', label: 'Text' }, { value: 'json', label: 'Json' }]" size="sm" :aria-label="t('maintenance.caches.cache_content')" />
              <XTooltip v-if="valueText !== null" :content="t('common.actions.copy')">
                <XhButton icon-only size="sm" variant="ghost" :aria-label="t('common.actions.copy')" @click="handleCopy">
                  <span><Icon icon="lucide:copy" /></span>
                </XhButton>
              </XTooltip>
              <XTooltip :content="t('common.actions.refresh')">
                <XhButton icon-only size="sm" variant="ghost" :aria-label="t('common.actions.refresh')" @click="reloadValue">
                  <span><Icon icon="lucide:refresh-cw" /></span>
                </XhButton>
              </XTooltip>
              <XhButton v-if="canEdit && !editing" variant="subtle" size="sm" @click="startEdit">
                <span><Icon icon="lucide:pencil-line" /></span>
                {{ t('maintenance.caches.edit') }}
              </XhButton>
              <XTooltip v-if="canDelete" :content="t('maintenance.caches.delete_this_key')">
                <XhButton icon-only size="sm" variant="ghost" tone="danger" :aria-label="t('maintenance.caches.delete_this_key')" @click="handleDeleteCurrent">
                  <span><Icon icon="lucide:trash-2" /></span>
                </XhButton>
              </XTooltip>
            </div>
          </div>
          <div v-else class="cache-card-header">
            <Icon icon="lucide:file-json" width="15" />
            <span>{{ t('maintenance.caches.cache_content') }}</span>
          </div>
        </XhCardHeader>
        <XhCardContent :style="cardContentStyle">
          <!-- 滚动区：相对壳 + 绝对内胆，详情在内部滚动 -->
          <div class="cache-scroll-host">
            <div class="cache-scroll-body">
              <div class="xh-loading-stage" :class="{ 'is-loading': loadingValue }">
                <div class="xh-loading-stage__veil">
                  <XhSpinner />
                </div>
                <div v-if="!detailKey" class="cache-empty">
                  <XhEmptyStateRoot>
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:mouse-pointer-click" width="28" height="28" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('maintenance.caches.select_key_hint_title') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('maintenance.caches.select_key_hint') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                </div>
                <div v-else-if="!entry" class="cache-empty">
                  <XhEmptyStateRoot v-if="!loadingValue">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:search-x" width="28" height="28" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('maintenance.caches.key_not_exist') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                </div>
                <template v-else>
                  <!-- 过期设置：剩余存活与滑动过期（保存时沿用原项） -->
                  <XhDescriptionsRoot class="cache-meta" :columns="2" size="sm" variant="outline">
                    <XhDescriptionsItem>
                      <XhDescriptionsLabel>{{ t('maintenance.caches.time_to_live') }}</XhDescriptionsLabel>
                      <XhDescriptionsValue>{{ timeToLiveText(t, entry) }}</XhDescriptionsValue>
                    </XhDescriptionsItem>
                    <XhDescriptionsItem>
                      <XhDescriptionsLabel>{{ t('maintenance.caches.sliding') }}</XhDescriptionsLabel>
                      <XhDescriptionsValue>{{ slidingText(t, entry) }}</XhDescriptionsValue>
                    </XhDescriptionsItem>
                    <XhDescriptionsItem>
                      <XhDescriptionsLabel>{{ t('maintenance.caches.layer') }}</XhDescriptionsLabel>
                      <XhDescriptionsValue>{{ layerLabel(t, entry.layer) }}</XhDescriptionsValue>
                    </XhDescriptionsItem>
                    <XhDescriptionsItem v-if="entry.typeName">
                      <XhDescriptionsLabel>{{ t('maintenance.caches.type_name') }}</XhDescriptionsLabel>
                      <XhDescriptionsValue>
                        <code class="cache-type-name" :title="entry.typeName">{{ entry.typeName }}</code>
                      </XhDescriptionsValue>
                    </XhDescriptionsItem>
                  </XhDescriptionsRoot>
                  <p v-if="reason === 'read-only'" class="cache-hint">
                    {{ t('maintenance.caches.read_only_hint') }}
                  </p>
                  <p v-else-if="reason === 'object' || reason === 'unrenderable'" class="cache-hint">
                    {{ t('maintenance.caches.object_hint') }}
                  </p>
                  <!-- 二进制值：不能预览也不能改写，仍可删除 -->
                  <div v-if="reason === 'binary' || reason === 'unrenderable'" class="cache-empty">
                    <XhEmptyStateRoot>
                      <XhEmptyStateIndicator>
                        <Icon icon="lucide:binary" width="28" height="28" />
                      </XhEmptyStateIndicator>
                      <XhEmptyStateTitle>{{ t('maintenance.caches.binary_title') }}</XhEmptyStateTitle>
                      <XhEmptyStateDescription>{{ reason === 'binary' ? t('maintenance.caches.binary_hint') : t('maintenance.caches.unrenderable_hint') }}</XhEmptyStateDescription>
                    </XhEmptyStateRoot>
                  </div>
                  <!-- 编辑态：文本域 + 保存/取消 -->
                  <template v-else-if="editing">
                    <XhFieldRoot class="cache-value-field" required>
                      <XhFieldLabel>{{ t('maintenance.caches.value') }}</XhFieldLabel>
                      <XhFieldControl>
                        <XInput
                          v-model:value="draft"
                          type="textarea"
                          class="cache-value-editor"
                          :placeholder="t('maintenance.caches.value_placeholder')"
                        />
                      </XhFieldControl>
                    </XhFieldRoot>
                    <div class="cache-edit-actions">
                      <XhButton variant="subtle" size="sm" @click="cancelEdit">
                        {{ t('common.actions.cancel') }}
                      </XhButton>
                      <XhButton variant="subtle" size="sm" tone="brand" :loading="saving" :disabled="!draft" @click="handleSave">
                        <XhButtonIndicator />
                        <XhButtonLabel>{{ t('common.actions.save') }}</XhButtonLabel>
                      </XhButton>
                    </div>
                  </template>
                  <!-- 预览态：只读 -->
                  <pre v-else class="cache-value-pre">{{ displayValue }}</pre>
                </template>
              </div>
            </div>
          </div>
        </XhCardContent>
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
  padding: var(--xh-space-3);
  overflow: hidden;
}

/*
 * 左右行容器：flex:1 + height:0 收敛到视口剩余高度（同 SchemaPage 表格区 class="flex-1" style="height:0"）。
 * height:0 是关键——给 flex item 一个确定的 0 起点，避免 flex-basis 退化为内容高度（树自然高）反向撑破父链。
 */
.cache-body {
  display: flex;
  flex: 1;
  gap: var(--xh-space-3);
  height: 0;
  /* 固定值兜底：极端布局模式下高度链失效时，左右栏仍有最小可用高度而不是塌成 0 */
  min-height: 22.5rem;
  /* page 锁定高度后，行容器在其内不被子（树/详情）撑破，迫使滚动发生在左右两栏内部 */
  overflow: hidden;
}

/* ── 左侧键树卡片 ── */
.cache-tree-card {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  width: 28.75rem;
  min-width: 22.5rem;
  overflow: hidden;
}

.cache-tree-toolbar {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  gap: var(--xh-space-2);
  margin-bottom: var(--xh-space-2);
}

.cache-namespace-select {
  inline-size: 100%;
}

.cache-tree-search {
  display: flex;
  gap: var(--xh-space-2);
}

.cache-prefix-input {
  flex: 1;
  min-inline-size: 0;
}

/* 输入框前缀处的命名空间前缀：等宽、次级色，与输入的子前缀连读 */
.cache-prefix-text {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
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

/* 列 render 的 h() 节点不带本页 scope，样式须经 :deep() 才能命中 */
:deep(.cache-tree-leaf) {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
}

/* 对象缓存的叶子：铺满节点宽度，键过长省略、标签不收缩 */
:deep(.cache-tree-leaf-row) {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
  min-width: 0;
}

:deep(.cache-tree-leaf-row .cache-tree-leaf) {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

:deep(.cache-tree-leaf-tag) {
  flex-shrink: 0;
}

:deep(.cache-tree-group) {
  display: inline-flex;
  gap: var(--xh-space-1);
  align-items: center;
  font-size: var(--xh-font-size-sm);
}

.cache-load-more {
  display: flex;
  flex-shrink: 0;
  justify-content: center;
  padding-block: var(--xh-space-2);
}

.cache-batch-bar {
  display: flex;
  flex-shrink: 0;
  gap: var(--xh-space-2);
  align-items: center;
  justify-content: space-between;
  padding-top: var(--xh-space-2);
  margin-top: var(--xh-space-2);
  border-top: 1px solid var(--xh-border-default);
}

.cache-batch-count {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.cache-batch-count strong {
  color: var(--xh-fg-default);
}

.cache-batch-hint {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-subtle);
}

.cache-batch-actions {
  display: flex;
  gap: var(--xh-space-1_5);
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
  gap: var(--xh-space-1_5);
  align-items: center;
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
}

.cache-detail-header {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  min-width: 0;
}

.cache-detail-key {
  min-width: 0;
  overflow: hidden;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cache-detail-actions {
  display: flex;
  flex-shrink: 0;
  gap: var(--xh-space-2);
  align-items: center;
}

.cache-meta {
  flex-shrink: 0;
  margin-bottom: var(--xh-space-2);
}

.cache-type-name {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  word-break: break-all;
}

.cache-hint {
  flex-shrink: 0;
  margin: 0 0 var(--xh-space-2);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.cache-value-pre {
  flex: 1;
  min-height: 0;
  padding: var(--xh-space-3);
  margin: 0;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  line-height: var(--xh-leading-relaxed);
  word-break: break-all;
  white-space: pre-wrap;
  background: var(--xh-bg-subtle);
  border-radius: var(--xh-shape-inset);
}

/* 字段（标签 + 编辑区）吃掉详情的余量 */
.cache-value-field {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
}

.cache-value-editor {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
}

/* 视觉盒是组件库的 control 部件：吃掉编辑区的余量，并当 textarea 的定位参照 */
.cache-value-editor :deep([data-scope='text-field'][data-part='control']) {
  position: relative;
  flex: 1;
  min-block-size: 0;
}

/* textarea 的高由 rows 定死，百分比和 align-items: stretch 都拿不动它；
   直接把 textarea 铺满盒子，行内衬补回盒子自己那份，字不贴着描边 */
.cache-value-editor :deep(textarea) {
  position: absolute;
  inset: 0;
  padding-inline: var(--xh-control-px-sm);
  resize: none;
  font-family: var(--xh-font-family-mono);
}

.cache-edit-actions {
  display: flex;
  flex-shrink: 0;
  gap: var(--xh-space-2);
  justify-content: flex-end;
  margin-top: var(--xh-space-2);
}

.cache-empty {
  display: flex;
  flex: 1;
  align-items: center;
  justify-content: center;
  min-height: 12.5rem;
}

@media (max-width: 48rem) {
  .cache-body {
    flex-direction: column;
  }

  /* 绝对内胆不再有固有高度，小屏给树卡显式高度，详情占余下空间 */
  .cache-tree-card {
    flex: none;
    width: 100%;
    min-width: 0;
    height: 20rem;
  }
}
</style>
