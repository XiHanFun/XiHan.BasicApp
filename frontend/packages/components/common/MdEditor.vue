<script setup lang="ts">
import { MdEditor as Editor, MdPreview } from 'md-editor-v3'
import 'md-editor-v3/lib/style.css'

defineOptions({ name: 'XMdEditor' })

const props = withDefaults(defineProps<{
  /** 预览模式，不显示编辑区 */
  previewOnly?: boolean
  /** 编辑器主题 */
  theme?: 'light' | 'dark'
  /** 预览主题 */
  previewTheme?: 'default' | 'github' | 'vuepress' | 'mk-cute' | 'smart-blue' | 'cyanosis'
  /** 代码高亮主题 */
  codeTheme?: 'atom' | 'a11y' | 'github' | 'gradient' | 'kimbie' | 'paraiso' | 'qtcreator' | 'stackoverflow'
  /** 编辑器语言 */
  language?: 'zh-CN' | 'en-US'
  /** 占位文本 */
  placeholder?: string
  /** 是否显示代码行号 */
  showCodeRowNumber?: boolean
  /** 编辑器唯一 id，存在多个编辑器时须唯一 */
  editorId?: string
  /** 不显示的工具栏项 */
  toolbarsExclude?: string[]
  /** 是否禁用 */
  disabled?: boolean
}>(), {
  previewOnly: false,
  theme: 'light',
  previewTheme: 'default',
  codeTheme: 'atom',
  language: 'zh-CN',
  placeholder: '请输入 Markdown 内容...',
  showCodeRowNumber: true,
  editorId: 'x-md-editor',
  toolbarsExclude: () => [],
  disabled: false,
})

const modelValue = defineModel<string>({ default: '' })

const emit = defineEmits<{
  /** 保存快捷键触发 */
  save: [value: string]
  /** 图片上传 */
  uploadImg: [files: File[], callback: (urls: string[]) => void]
}>()

function handleSave(val: string) {
  emit('save', val)
}

function handleUploadImg(files: File[], callback: (urls: string[]) => void) {
  emit('uploadImg', files, callback)
}
</script>

<template>
  <MdPreview
    v-if="props.previewOnly"
    :model-value="modelValue"
    :editor-id="props.editorId"
    :theme="props.theme"
    :preview-theme="props.previewTheme"
    :code-theme="props.codeTheme"
    :language="props.language"
    :show-code-row-number="props.showCodeRowNumber"
  />
  <Editor
    v-else
    v-model="modelValue"
    :editor-id="props.editorId"
    :theme="props.theme"
    :preview-theme="props.previewTheme"
    :code-theme="props.codeTheme"
    :language="props.language"
    :placeholder="props.placeholder"
    :show-code-row-number="props.showCodeRowNumber"
    :toolbars-exclude="props.toolbarsExclude"
    :disabled="props.disabled"
    @save="handleSave"
    @upload-img="handleUploadImg"
  />
</template>
