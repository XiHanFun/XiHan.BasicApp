<script setup lang="ts">
import type {
  CodeGenArtifactDto,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import { XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFlex, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XCodeEditor, XSegmented, XTree } from '~/components'
import { toast } from '~/composables'
import {
  ArtifactWriteMode,
  codeGenerationApi,
} from '../../../../api'

defineOptions({ name: 'CodeGenPreviewModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
  tableName?: string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

const { t } = useI18n()

const previewLoading = ref(false)
const artifacts = ref<CodeGenArtifactDto[]>([])
const activeIndex = ref(0)

const activeArtifact = computed(() => artifacts.value[activeIndex.value] ?? null)

/** 产物归属：按模板编码前缀判定，二阶产物（菜单/权限接线）随后端 */
type ArtifactSide = 'all' | 'backend' | 'frontend'

const activeSide = ref<ArtifactSide>('all')

const sideOptions = computed(() => [
  { label: t('develop.code_gen.preview.side_all'), value: 'all' as const },
  { label: t('develop.code_gen.preview.side_backend'), value: 'backend' as const },
  { label: t('develop.code_gen.preview.side_frontend'), value: 'frontend' as const },
])

function sideOf(artifact: CodeGenArtifactDto): Exclude<ArtifactSide, 'all'> {
  return artifact.templateCode?.toLowerCase().includes('frontend') ? 'frontend' : 'backend'
}

/** 当前分栏下的产物（保留原始下标，选中态与内容区据此定位） */
const visibleArtifacts = computed(() =>
  artifacts.value
    .map((artifact, index) => ({ artifact, index }))
    .filter(entry => activeSide.value === 'all' || sideOf(entry.artifact) === activeSide.value),
)

/** 文件类型图标：仅用离线预载的 lucide / mdi 图标集 */
function fileIconOf(fileName: string): string {
  const ext = fileName.slice(fileName.lastIndexOf('.') + 1).toLowerCase()
  switch (ext) {
    case 'cs':
      return 'mdi:language-csharp'
    case 'ts':
      return 'mdi:language-typescript'
    case 'vue':
      return 'mdi:vuejs'
    case 'json':
      return 'mdi:code-json'
    case 'md':
      return 'mdi:language-markdown'
    case 'sql':
      return 'mdi:database'
    default:
      return 'lucide:file'
  }
}

/** 目录树节点：目录以路径前缀为键，文件以产物下标为键 */
interface ArtifactTreeNode {
  value: string
  label: string
  /** 文件节点携带的产物下标；目录节点为 undefined */
  index?: number
  /** 文件节点的类型图标；目录节点由展开态决定 */
  icon?: string
  writeOnce?: boolean
  children?: ArtifactTreeNode[]
}

/**
 * 按 relativePath 组织成目录树：路径逐段建目录，产物挂到末级目录下。
 * 目录先于文件、各自按名称排序，保证同一份产物每次打开顺序一致。
 */
const artifactTree = computed<ArtifactTreeNode[]>(() => {
  const roots: ArtifactTreeNode[] = []
  const dirCache = new Map<string, ArtifactTreeNode>()

  function ensureDir(segments: string[]): ArtifactTreeNode[] {
    let siblings = roots
    let prefix = ''
    for (const segment of segments) {
      prefix = prefix ? `${prefix}/${segment}` : segment
      let dir = dirCache.get(prefix)
      if (!dir) {
        dir = { value: `dir:${prefix}`, label: segment, children: [] }
        dirCache.set(prefix, dir)
        siblings.push(dir)
      }
      siblings = dir.children!
    }
    return siblings
  }

  visibleArtifacts.value.forEach(({ artifact, index }) => {
    // relativePath 是「目录/文件名」（无目录时就等于文件名），取最后一个斜杠之前的部分建目录
    const path = (artifact.relativePath ?? '').replace(/\\/g, '/')
    const lastSlash = path.lastIndexOf('/')
    const dirSegments = lastSlash >= 0
      ? path.slice(0, lastSlash).split('/').map(segment => segment.trim()).filter(Boolean)
      : []

    ensureDir(dirSegments).push({
      value: `file:${index}`,
      label: artifact.fileName,
      index,
      icon: fileIconOf(artifact.fileName),
      writeOnce: artifact.writeMode === ArtifactWriteMode.WriteOnce,
    })
  })

  function sort(nodes: ArtifactTreeNode[]): ArtifactTreeNode[] {
    nodes.sort((a, b) => {
      const aIsDir = a.children !== undefined
      const bIsDir = b.children !== undefined
      if (aIsDir !== bIsDir) {
        return aIsDir ? -1 : 1
      }
      return a.label.localeCompare(b.label)
    })
    nodes.forEach(node => node.children && sort(node.children))
    return nodes
  }

  return sort(roots)
})

/** 展开态：默认全展开，切换分栏后按新树重置；用户手动折叠的结果在同一棵树内保留 */
const expandedKeys = ref<string[]>([])

function collectDirKeys(node: ArtifactTreeNode): string[] {
  if (!node.children) {
    return []
  }
  return [node.value, ...node.children.flatMap(collectDirKeys)]
}

function handleExpand(keys: string[]) {
  expandedKeys.value = keys
}

// 树重建（首次加载或切换分栏）：全展开，并把选中项落到当前分栏内第一个文件
watch(artifactTree, (tree) => {
  expandedKeys.value = tree.flatMap(collectDirKeys)

  const visible = visibleArtifacts.value
  const first = visible[0]
  if (!first) {
    return
  }
  if (!visible.some(entry => entry.index === activeIndex.value)) {
    activeIndex.value = first.index
  }
}, { immediate: true })

const selectedKeys = computed(() => [`file:${activeIndex.value}`])

function handleSelect(keys: string[]) {
  const fileKey = keys.find(key => key.startsWith('file:'))
  if (fileKey) {
    activeIndex.value = Number(fileKey.slice('file:'.length))
  }
}

/**
 * 节点标签：目录图标随展开态切换开合，文件按扩展名取类型图标，
 * 右侧带「不覆盖」标记。
 */
function renderNodeLabel(node: Record<string, unknown>) {
  const item = node as unknown as ArtifactTreeNode
  const icon = item.children
    ? (expandedKeys.value.includes(item.value) ? 'lucide:folder-open' : 'lucide:folder')
    : item.icon ?? 'lucide:file'
  return h('span', { class: 'gen__node' }, [
    h(Icon, { class: 'gen__node-icon', icon }),
    h('span', null, item.label),
    item.writeOnce
      ? h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'success' }, () => h(XhTagLabel, () => t('develop.code_gen.preview.badge_manual')))
      : null,
  ])
}

const modalTitle = computed(() =>
  props.tableName
    ? `${t('develop.code_gen.preview.title')} · ${props.tableName}`
    : t('develop.code_gen.preview.title'),
)

watch(
  () => props.show,
  (visible) => {
    if (visible && props.tableId) {
      void loadPreview()
    }
    else if (!visible) {
      artifacts.value = []
      activeIndex.value = 0
    }
  },
)

async function loadPreview() {
  if (!props.tableId) {
    return
  }
  previewLoading.value = true
  try {
    const result = await codeGenerationApi.preview({ tableId: props.tableId })
    if (!result.success) {
      toast.error(result.message || t('develop.code_gen.preview.preview_failed'))
      artifacts.value = []
      return
    }
    artifacts.value = result.artifacts ?? []
    activeIndex.value = 0
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.preview.preview_failed'))
    artifacts.value = []
  }
  finally {
    previewLoading.value = false
  }
}
</script>

<template>
  <XhDialogRoot
    :open="show"
    @update:open="(open: boolean) => emit('update:show', open)"
  >
    <XhDialogContent style="--xh-dialog-max-w: min(96vw, 1840px)">
      <XhDialogTitle>{{ modalTitle }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': previewLoading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div class="gen">
          <div class="gen__tree">
            <div class="gen__tree-head">
              <XSegmented v-model:value="activeSide" :options="sideOptions" size="sm" />
            </div>
            <XhEmptyStateRoot v-if="artifactTree.length === 0" class="gen__tree-empty" size="sm">
              <XhEmptyStateIcon><Icon icon="lucide:inbox" /></XhEmptyStateIcon>
              <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('develop.code_gen.preview.side_empty') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <XTree
              v-else
              :data="artifactTree"
              :render-label="renderNodeLabel"
              :expanded-keys="expandedKeys"
              :selected-keys="selectedKeys"
              @update:expanded-keys="handleExpand"
              @update:selected-keys="handleSelect"
            />
          </div>
          <div class="gen__content">
            <XhEmptyStateRoot v-if="!activeArtifact" class="gen__content-empty">
              <XhEmptyStateIcon><Icon icon="lucide:inbox" /></XhEmptyStateIcon>
              <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('develop.code_gen.preview.empty') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <XCodeEditor
              v-else
              :value="activeArtifact.content"
              :file-name="activeArtifact.fileName"
              copyable
              height="100%"
              readonly
            />
          </div>
        </div>
      </div>

      <div class="xh-dialog-footer">
        <XhFlex justify="between">
          <span class="gen__hint">{{ t('develop.code_gen.preview.total_files', { count: artifacts.length }) }}</span>
          <XhButton @click="emit('update:show', false)">
            {{ t('common.actions.close') }}
          </XhButton>
        </XhFlex>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 两栏的唯一高度真源：短屏改用视口高扣掉弹窗标题、页脚与内外边距的余量 */
.gen {
  display: flex;
  gap: 12px;
  block-size: min(76vh, calc(100vh - 220px));
}

.gen__tree {
  /* 树自带 24rem 的最大高，不放开就是一块矮框加大片空白 */
  --xh-tree-max-h: 100%;

  display: flex;
  flex: 0 0 360px;
  flex-direction: column;
  min-block-size: 0;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
}

.gen__tree-head {
  flex-shrink: 0;
  padding: 8px;
  border-bottom: 1px solid hsl(var(--border));
}

/* 无产物时占位撑满剩余高度，框不塌回内容高 */
.gen__tree-empty {
  flex: 1;
  min-block-size: 0;
  padding: 32px 0;
}

/* 树分外层 root 与内层 tree 两级，两级都让出高度，滚动落在内层 */
.gen__tree :deep(.x-tree) {
  flex: 1;
  min-block-size: 0;
  padding: 6px;
}

.gen__tree :deep([data-scope='tree'][data-part='tree']) {
  flex: 1;
  min-block-size: 0;
}

.gen__node-icon {
  font-size: 15px;
  color: var(--text-secondary);
}

.gen__content {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
  min-block-size: 0;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  padding: 8px 12px;
}

/* 未选中文件时占位撑满剩余高度，框不塌回内容高 */
.gen__content-empty {
  flex: 1;
  min-block-size: 0;
}

.gen__hint {
  font-size: 12px;
  color: var(--text-secondary);
}
</style>
