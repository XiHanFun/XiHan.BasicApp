<!-- Markdown 编辑器封装模块：统一编辑、预览、主题以及图片上传交互。 -->
<script setup lang="ts">
import type { ToolbarNames } from 'md-editor-v3'
import { MdEditor as Editor, MdPreview } from 'md-editor-v3'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '~/stores'
import 'md-editor-v3/lib/style.css'

defineOptions({ name: 'XMdEditor' })

const props = withDefaults(defineProps<{
  /** 预览模式，不显示编辑区 */
  previewOnly?: boolean
  /** 编辑器主题，不传则跟随系统 */
  theme?: 'light' | 'dark'
  /** 预览主题 */
  previewTheme?: 'default' | 'github' | 'vuepress' | 'mk-cute' | 'smart-blue' | 'cyanosis'
  /** 代码高亮主题 */
  codeTheme?: 'atom' | 'a11y' | 'github' | 'gradient' | 'kimbie' | 'paraiso' | 'qtcreator' | 'stackoverflow'
  /** 编辑器语言，不传则跟随应用语言（md-editor-v3 仅内置中英，其余语言回退 en-US） */
  language?: 'zh-CN' | 'en-US'
  /** 占位文本 */
  placeholder?: string
  /** 是否显示代码行号 */
  showCodeRowNumber?: boolean
  /** 编辑器唯一 id，存在多个编辑器时须唯一 */
  editorId?: string
  /** 不显示的工具栏项 */
  toolbarsExclude?: ToolbarNames[]
  /** 是否禁用 */
  disabled?: boolean
}>(), {
  previewOnly: false,
  theme: undefined,
  previewTheme: 'default',
  codeTheme: 'atom',
  language: undefined,
  placeholder: undefined,
  showCodeRowNumber: true,
  editorId: 'x-md-editor',
  toolbarsExclude: () => [],
  disabled: false,
})

const emit = defineEmits<{
  /** 保存快捷键触发 */
  save: [value: string]
  /** 图片上传 */
  uploadImg: [files: File[], callback: (urls: string[]) => void]
}>()

const modelValue = defineModel<string>({ default: '' })

const { t } = useI18n()
const appStore = useAppStore()

/** 跟随系统暗色模式，外部可通过 theme prop 强制覆盖 */
const resolvedTheme = computed(() => props.theme ?? (appStore.isDark ? 'dark' : 'light'))

/** 占位文本：外部未传时回落到 i18n 默认值 */
const resolvedPlaceholder = computed(() => props.placeholder ?? t('component.md_editor.placeholder'))

// 编辑器语言：外部未传时跟随应用语言。md-editor-v3 只内置 zh-CN / en-US 两套界面文案，
// 中文语系（zh-CN / zh-TW）统一用 zh-CN——繁中用户读简中工具栏比读英文更顺；
// 其余语言回退 en-US——回退中文会让中文工具栏混进外语界面。
const resolvedLanguage = computed<'zh-CN' | 'en-US'>(
  () => props.language ?? (appStore.locale.startsWith('zh') ? 'zh-CN' : 'en-US'),
)

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
    :theme="resolvedTheme"
    :preview-theme="props.previewTheme"
    :code-theme="props.codeTheme"
    :language="resolvedLanguage"
    :show-code-row-number="props.showCodeRowNumber"
  />
  <Editor
    v-else
    v-model="modelValue"
    :editor-id="props.editorId"
    :theme="resolvedTheme"
    :preview-theme="props.previewTheme"
    :code-theme="props.codeTheme"
    :language="resolvedLanguage"
    :placeholder="resolvedPlaceholder"
    :show-code-row-number="props.showCodeRowNumber"
    :toolbars-exclude="props.toolbarsExclude"
    :disabled="props.disabled"
    @save="handleSave"
    @upload-img="handleUploadImg"
  />
</template>
