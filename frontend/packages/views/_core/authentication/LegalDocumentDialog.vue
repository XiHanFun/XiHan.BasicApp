<script lang="ts" setup>
import type { LegalDocumentKind } from './legal'
import { XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XMdEditor } from '~/components'
import { loadLegalDocument } from './legal'

defineOptions({ name: 'LegalDocumentDialog' })

const props = defineProps<{
  /** 要显示的文书 */
  kind: LegalDocumentKind
}>()

const open = defineModel<boolean>('open', { required: true })

const { t, locale } = useI18n()

/** md-editor-v3 的预览实例要求 id 唯一 */
const editorId = `legal-doc-${useId()}`

const title = computed(() => (props.kind === 'privacy-policy' ? t('page.auth.privacy_policy') : t('page.auth.terms_of_service')))

const content = ref('')
const status = ref<'loading' | 'ready' | 'failed'>('loading')

/** 连点两份文书或打开时切语言，只认最后一次请求的结果，免得旧正文盖掉新正文 */
let latestRequest = 0

watch(
  () => (open.value ? [props.kind, locale.value] as const : null),
  async (target) => {
    if (!target) {
      return
    }
    const request = ++latestRequest
    status.value = 'loading'
    try {
      const text = await loadLegalDocument(...target)
      if (request === latestRequest) {
        content.value = text
        status.value = 'ready'
      }
    }
    catch {
      if (request === latestRequest) {
        status.value = 'failed'
      }
    }
  },
  { immediate: true },
)
</script>

<template>
  <XhDialogRoot v-model:open="open">
    <!-- 长文阅读：铺到视口的大部分（组件库最大档只有 48rem），一屏能读更多、少翻几屏。
         内容会传送到 body 下、带不上本组件的 scoped 标记，宽度只能写在行内 -->
    <XhDialogContent style="--xh-dialog-max-w: 85vw">
      <XhDialogTitle>{{ title }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="legal-dialog__body">
        <div v-if="status === 'loading'" class="legal-dialog__state">
          <XhSpinner size="md" />
        </div>
        <p v-else-if="status === 'failed'" class="legal-dialog__state" role="alert">
          {{ t('page.auth.legal_load_failed') }}
        </p>
        <XMdEditor v-else preview-only :editor-id="editorId" :model-value="content" />
      </div>
      <div class="flex justify-end">
        <XhButton size="sm" variant="solid" @click="open = false">
          {{ t('common.actions.close') }}
        </XhButton>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 正文在弹窗里滚动，标题与关闭钮留在原位 */
.legal-dialog__body {
  max-block-size: 75vh;
  overflow-y: auto;
}

.legal-dialog__state {
  display: flex;
  align-items: center;
  justify-content: center;
  min-block-size: 30vh;
  margin: 0;
  color: var(--xh-fg-muted);
}

/* 预览融入弹窗：去掉编辑器自带的底色与内边距，跟随弹窗背景 */
.legal-dialog__body :deep(.md-editor),
.legal-dialog__body :deep(.md-editor-preview-wrapper),
.legal-dialog__body :deep(.md-editor-preview) {
  background-color: transparent;
}

.legal-dialog__body :deep(.md-editor-preview-wrapper) {
  padding: 0;
}
</style>
