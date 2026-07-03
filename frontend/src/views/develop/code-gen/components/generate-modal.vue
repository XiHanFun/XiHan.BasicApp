<script setup lang="ts">
import type { ApiId, CodeGenArtifactDto } from '@/api'
import {
  NButton,
  NEmpty,
  NModal,
  NScrollbar,
  NSpace,
  NSpin,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { codeGenerationApi, GenType } from '@/api'
import CodeHighlight from '~/components/common/CodeHighlight.vue'

defineOptions({ name: 'CodeGenGenerateModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
  tableName?: string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'generated': []
}>()

const { t } = useI18n()
const message = useMessage()

const previewLoading = ref(false)
const generating = ref(false)
const artifacts = ref<CodeGenArtifactDto[]>([])
const activeIndex = ref(0)

const activeArtifact = computed(() => artifacts.value[activeIndex.value] ?? null)

const modalTitle = computed(() =>
  props.tableName
    ? `${t('develop.code_gen.generate.title')} · ${props.tableName}`
    : t('develop.code_gen.generate.title'),
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
      message.error(result.message || t('develop.code_gen.generate.preview_failed'))
      artifacts.value = []
      return
    }
    artifacts.value = result.artifacts ?? []
    activeIndex.value = 0
  }
  catch {
    message.error(t('develop.code_gen.generate.preview_failed'))
    artifacts.value = []
  }
  finally {
    previewLoading.value = false
  }
}

function downloadZip(base64: string, fileName: string) {
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i)
  }
  const blob = new Blob([bytes], { type: 'application/zip' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
  URL.revokeObjectURL(url)
}

async function handleGenerate() {
  if (!props.tableId) {
    return
  }
  generating.value = true
  try {
    const result = await codeGenerationApi.generate({
      tableId: props.tableId,
      genType: GenType.Zip,
    })
    if (!result.success) {
      message.error(result.message || t('develop.code_gen.generate.generate_failed'))
      return
    }
    if (result.packageBase64) {
      const fileName = `${props.tableName || 'codegen'}_${Date.now()}.zip`
      downloadZip(result.packageBase64, fileName)
      message.success(t('develop.code_gen.generate.generate_success', { count: result.fileCount }))
    }
    else {
      message.warning(t('develop.code_gen.generate.no_package'))
    }
    emit('generated')
  }
  catch {
    message.error(t('develop.code_gen.generate.generate_failed'))
  }
  finally {
    generating.value = false
  }
}
</script>

<template>
  <NModal
    :auto-focus="false"
    :bordered="false"
    preset="card"
    :show="show"
    style="width: 96vw; max-width: 1200px"
    :title="modalTitle"
    @update:show="emit('update:show', $event)"
  >
    <NSpin :show="previewLoading">
      <div class="gen">
        <div class="gen__tree">
          <NScrollbar style="max-height: 60vh">
            <ul class="gen__file-list">
              <li
                v-for="(artifact, index) in artifacts"
                :key="`${artifact.relativePath}/${artifact.fileName}`"
                class="gen__file"
                :class="{ 'gen__file--active': index === activeIndex }"
                :title="`${artifact.relativePath}/${artifact.fileName}`"
                @click="activeIndex = index"
              >
                <div class="gen__file-name">
                  {{ artifact.fileName }}
                </div>
                <div class="gen__file-path">
                  {{ artifact.relativePath }}
                </div>
              </li>
            </ul>
          </NScrollbar>
        </div>
        <div class="gen__content">
          <NEmpty v-if="!activeArtifact" :description="t('develop.code_gen.generate.empty')" />
          <CodeHighlight
            v-else
            :code="activeArtifact.content"
            :file-name="activeArtifact.fileName"
            max-height="60vh"
          />
        </div>
      </div>
    </NSpin>

    <template #footer>
      <NSpace justify="space-between">
        <span class="gen__hint">{{ t('develop.code_gen.generate.total_files', { count: artifacts.length }) }}</span>
        <NSpace>
          <NButton @click="emit('update:show', false)">
            {{ t('common.actions.close') }}
          </NButton>
          <NButton :disabled="!tableId" :loading="generating" type="primary" @click="handleGenerate">
            {{ t('develop.code_gen.generate.generate_zip') }}
          </NButton>
        </NSpace>
      </NSpace>
    </template>
  </NModal>
</template>

<style scoped>
.gen {
  display: flex;
  gap: 12px;
  min-height: 320px;
}

.gen__tree {
  flex: 0 0 260px;
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

.gen__file-name {
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
