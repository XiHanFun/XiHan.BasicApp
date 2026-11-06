<!--
  代码生成 / 预览产物（照 4.x preview-modal）：左边按路径组成的目录树，右边只读的代码编辑器
  接口取自后端契约（codegen-tables.preview）：只渲染、不打包、不记历史
  与 4.x 不同：产物按后端给的归属分栏（4.x 按模板编码猜），多一栏登记片段；路径带 backend / frontend / _registration 前缀
-->
<script setup lang="ts">
import type { Artifact, ArtifactSide, ArtifactTreeNode } from './use-templates'
import { XhButton, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFlex, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XCodeEditor, XSegmented, XTree } from '~/components'
import { toast } from '~/composables'
import { codegenTables } from '~/contracts'
import { buildArtifactTree, fileIconOf } from './use-templates'

defineOptions({ name: 'CodegenPreviewModal' })

const props = defineProps<{
  show: boolean
  tableId: null | string
  tableName?: string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

const { t } = useI18n()

const loading = ref(false)
const artifacts = ref<Artifact[]>([])
const activeIndex = ref(0)
const activeArtifact = computed(() => artifacts.value[activeIndex.value] ?? null)

type SideFilter = 'all' | ArtifactSide
const activeSide = ref<SideFilter>('all')
const sideOptions = computed(() => [
  {
    label: t('codegeneration.code_gen.preview.side_all'),
    value: 'all',
  },
  {
    label: t('codegeneration.code_gen.preview.side_backend'),
    value: 'Backend',
  },
  {
    label: t('codegeneration.code_gen.preview.side_frontend'),
    value: 'Frontend',
  },
  {
    label: t('codegeneration.code_gen.preview.side_registration'),
    value: 'Registration',
  },
])

/** 当前分栏下的产物（带原下标，选中态与内容区据此定位） */
const visible = computed(() => artifacts.value
  .map((artifact, index) => ({ artifact, index }))
  .filter(entry => activeSide.value === 'all' || entry.artifact.side === activeSide.value))
const tree = computed(() => buildArtifactTree(visible.value))
const fileName = computed(() => activeArtifact.value?.path.split('/').at(-1) ?? null)

/** 展开态：缺省全展开，切换分栏后按新树重置 */
const expandedKeys = ref<string[]>([])
function directoryKeys(node: ArtifactTreeNode): string[] {
  return node.children
    ? [
        node.value,
        ...node.children.flatMap(directoryKeys),
      ]
    : []
}

// 树重建（首次加载或切换分栏）：全展开，选中项落到当前分栏的第一个文件
watch(tree, (nodes) => {
  expandedKeys.value = nodes.flatMap(directoryKeys)
  const first = visible.value[0]
  if (first && !visible.value.some(entry => entry.index === activeIndex.value))
    activeIndex.value = first.index
}, { immediate: true })

const selectedKeys = computed(() => [`file:${activeIndex.value}`])

function onSelect(keys: string[]) {
  const file = keys.find(key => key.startsWith('file:'))
  if (file)
    activeIndex.value = Number(file.slice('file:'.length))
}

/** 目录图标随展开态开合，文件按扩展名取图标，只首次写的带「不覆盖」标记（照 4.x） */
function renderLabel(node: Record<string, unknown>) {
  const item = node as unknown as ArtifactTreeNode
  const icon = item.children
    ? (expandedKeys.value.includes(item.value)
        ? 'lucide:folder-open'
        : 'lucide:folder')
    : fileIconOf(item.label)
  return h('span', { class: 'preview__node' }, [
    h(Icon, { class: 'preview__node-icon', icon }),
    h('span', null, item.label),
    item.writeOnce
      ? h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'success' }, () => h(XhTagLabel, () => t('codegeneration.code_gen.preview.badge_manual')))
      : null,
  ])
}

const title = computed(() => (props.tableName
  ? `${t('codegeneration.code_gen.preview.title')} · ${props.tableName}`
  : t('codegeneration.code_gen.preview.title')))

watch(() => props.show, (open) => {
  if (open && props.tableId) {
    void load()
  }
  else if (!open) {
    artifacts.value = []
    activeIndex.value = 0
    activeSide.value = 'all'
  }
})

async function load() {
  if (!props.tableId)
    return
  loading.value = true
  try {
    artifacts.value = (await codegenTables.preview({ id: props.tableId })).files
    activeIndex.value = 0
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.preview.preview_failed'))
    artifacts.value = []
  }
  finally {
    loading.value = false
  }
}
</script>

<template>
  <XhDialogRoot :open="show" @update:open="(open: boolean) => emit('update:show', open)">
    <XhDialogContent style="--xh-dialog-max-w: min(96vw, 115rem)">
      <XhDialogHeader>
        <XhDialogTitle>{{ title }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody class="xh-overlay-body">
        <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <div class="preview">
            <div class="preview__tree">
              <div class="preview__tree-head">
                <XSegmented v-model:value="activeSide" :options="sideOptions" size="sm" />
              </div>
              <XhEmptyStateRoot v-if="tree.length === 0" class="preview__tree-empty" size="sm">
                <XhEmptyStateIndicator><Icon icon="lucide:inbox" /></XhEmptyStateIndicator>
                <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                <XhEmptyStateDescription>{{ t('codegeneration.code_gen.preview.side_empty') }}</XhEmptyStateDescription>
              </XhEmptyStateRoot>
              <XTree
                v-else
                :aria-label="t('codegeneration.code_gen.preview.file_tree')"
                :data="tree"
                :render-label="renderLabel"
                :expanded-keys="expandedKeys"
                :selected-keys="selectedKeys"
                @update:expanded-keys="(keys: string[]) => (expandedKeys = keys)"
                @update:selected-keys="onSelect"
              />
            </div>
            <div class="preview__content">
              <XhEmptyStateRoot v-if="!activeArtifact" class="preview__content-empty">
                <XhEmptyStateIndicator><Icon icon="lucide:inbox" /></XhEmptyStateIndicator>
                <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                <XhEmptyStateDescription>{{ t('codegeneration.code_gen.preview.empty') }}</XhEmptyStateDescription>
              </XhEmptyStateRoot>
              <XCodeEditor v-else :value="activeArtifact.content" :file-name="fileName" copyable height="100%" readonly />
            </div>
          </div>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhFlex class="flex-1" justify="between">
          <span class="preview__hint">{{ t('codegeneration.code_gen.preview.total_files', { count: artifacts.length }) }}</span>
          <XhButton variant="subtle" @click="emit('update:show', false)">
            {{ t('common.actions.close') }}
          </XhButton>
        </XhFlex>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 两栏的唯一高度真源：短屏改用视口高扣掉弹窗标题、页脚与内外边距的余量（照 4.x） */
.preview {
  display: flex;
  gap: var(--xh-space-3);
  block-size: min(76vh, calc(100vh - 14rem));
}

.preview__tree {
  /* 树自带最大高，不放开就是一块矮框加大片空白 */
  --xh-tree-max-h: 100%;

  display: flex;
  flex: 0 0 22.5rem;
  flex-direction: column;
  min-block-size: 0;
  border: var(--xh-stroke-thin) solid var(--xh-border-subtle);
  border-radius: var(--xh-shape-surface);
}

.preview__tree-head {
  flex-shrink: 0;
  padding: var(--xh-space-2);
  border-bottom: var(--xh-stroke-thin) solid var(--xh-border-subtle);
}

.preview__tree-empty {
  flex: 1;
  min-block-size: 0;
  padding: var(--xh-space-8) 0;
}

/* 树分外层 root 与内层 tree 两级，两级都让出高度，滚动落在内层 */
.preview__tree :deep(.x-tree) {
  flex: 1;
  min-block-size: 0;
  padding: var(--xh-space-1);
}

.preview__tree :deep([data-scope='tree'][data-part='tree']) {
  flex: 1;
  min-block-size: 0;
}

:deep(.preview__node) {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
}

:deep(.preview__node-icon) {
  color: var(--xh-fg-muted);
}

.preview__content {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-inline-size: 0;
  min-block-size: 0;
  padding: var(--xh-space-2) var(--xh-space-3);
  border: var(--xh-stroke-thin) solid var(--xh-border-subtle);
  border-radius: var(--xh-shape-surface);
}

.preview__content-empty {
  flex: 1;
  min-block-size: 0;
}

.preview__hint {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}
</style>
