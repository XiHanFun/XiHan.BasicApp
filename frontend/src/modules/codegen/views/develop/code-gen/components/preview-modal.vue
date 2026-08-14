<script setup lang="ts">
import type {
  CodeGenArtifactDto,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import {
  NButton,
  NEmpty,
  NModal,
  NScrollbar,
  NSpace,
  NSpin,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XCodeEditor } from '~/components'
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
const message = useMessage()

const previewLoading = ref(false)
const artifacts = ref<CodeGenArtifactDto[]>([])
const activeIndex = ref(0)

const activeArtifact = computed(() => artifacts.value[activeIndex.value] ?? null)

/**
 * 产物按写入策略分组：自动文件（总是覆盖）与手动文件（仅首次创建）。
 * 保留在扁平数组中的原始下标，供选中态使用。
 */
const artifactGroups = computed(() => {
  const entries = artifacts.value.map((artifact, index) => ({ artifact, index }))
  return [
    {
      key: ArtifactWriteMode.AlwaysOverwrite,
      label: t('develop.code_gen.preview.group_generated'),
      hint: t('develop.code_gen.preview.group_generated_hint'),
      items: entries.filter(entry => entry.artifact.writeMode !== ArtifactWriteMode.WriteOnce),
    },
    {
      key: ArtifactWriteMode.WriteOnce,
      label: t('develop.code_gen.preview.group_manual'),
      hint: t('develop.code_gen.preview.group_manual_hint'),
      items: entries.filter(entry => entry.artifact.writeMode === ArtifactWriteMode.WriteOnce),
    },
  ].filter(group => group.items.length > 0)
})

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
      message.error(result.message || t('develop.code_gen.preview.preview_failed'))
      artifacts.value = []
      return
    }
    artifacts.value = result.artifacts ?? []
    activeIndex.value = 0
  }
  catch (error) {
    message.error((error as Error)?.message || t('develop.code_gen.preview.preview_failed'))
    artifacts.value = []
  }
  finally {
    previewLoading.value = false
  }
}
</script>

<template>
  <NModal
    :auto-focus="false"
    :bordered="false"
    preset="card"
    :show="show"
    style="width: 96vw; max-width: 1840px"
    :title="modalTitle"
    @update:show="emit('update:show', $event)"
  >
    <NSpin :show="previewLoading">
      <div class="gen">
        <div class="gen__tree">
          <NScrollbar style="max-height: 76vh">
            <div v-for="group in artifactGroups" :key="group.key" class="gen__group">
              <div class="gen__group-title" :title="group.hint">
                {{ group.label }}
                <span class="gen__group-count">{{ group.items.length }}</span>
              </div>
              <ul class="gen__file-list">
                <li
                  v-for="entry in group.items"
                  :key="`${entry.artifact.relativePath}/${entry.artifact.fileName}`"
                  class="gen__file"
                  :class="{ 'gen__file--active': entry.index === activeIndex }"
                  :title="`${entry.artifact.relativePath}/${entry.artifact.fileName}`"
                  @click="activeIndex = entry.index"
                >
                  <div class="gen__file-name">
                    {{ entry.artifact.fileName }}
                    <NTag v-if="group.key === ArtifactWriteMode.WriteOnce" :bordered="false" size="tiny" type="success">
                      {{ t('develop.code_gen.preview.badge_manual') }}
                    </NTag>
                  </div>
                  <div class="gen__file-path">
                    {{ entry.artifact.relativePath }}
                  </div>
                </li>
              </ul>
            </div>
          </NScrollbar>
        </div>
        <div class="gen__content">
          <NEmpty v-if="!activeArtifact" :description="t('develop.code_gen.preview.empty')" />
          <XCodeEditor
            v-else
            :value="activeArtifact.content"
            :file-name="activeArtifact.fileName"
            copyable
            height="76vh"
            readonly
          />
        </div>
      </div>
    </NSpin>

    <template #footer>
      <NSpace justify="space-between">
        <span class="gen__hint">{{ t('develop.code_gen.preview.total_files', { count: artifacts.length }) }}</span>
        <NButton @click="emit('update:show', false)">
          {{ t('common.actions.close') }}
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>

<style scoped>
.gen {
  display: flex;
  gap: 12px;
  min-height: 480px;
}

.gen__tree {
  flex: 0 0 360px;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  overflow: hidden;
}

.gen__file-list {
  margin: 0;
  padding: 4px;
  list-style: none;
}

.gen__file {
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
}

.gen__file:hover {
  background: hsl(var(--muted));
}

.gen__file--active {
  background: hsl(var(--primary) / 0.1);
}

.gen__group + .gen__group {
  margin-top: 8px;
  border-top: 1px solid hsl(var(--border));
}

.gen__group-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 8px 10px 4px;
  font-size: 11px;
  font-weight: 600;
  color: var(--text-secondary);
}

.gen__group-count {
  font-weight: 400;
  opacity: 0.7;
}

.gen__file-name {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.gen__file-path {
  font-size: 11px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.gen__content {
  flex: 1;
  min-width: 0;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  padding: 8px 12px;
}

.gen__hint {
  font-size: 12px;
  color: var(--text-secondary);
}
</style>
