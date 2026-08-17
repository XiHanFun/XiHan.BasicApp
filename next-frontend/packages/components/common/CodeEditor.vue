<script setup lang="ts">
import type * as Monaco from 'monaco-editor'
import * as monaco from 'monaco-editor'
import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker'
import CssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker'
import HtmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker'
import JsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker'
import TsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '~/stores'

declare global {
  interface Window {
    MonacoEnvironment?: Monaco.Environment
  }
}

defineOptions({ name: 'XCodeEditor' })

const props = withDefaults(defineProps<{
  /** 代码内容（v-model:value） */
  value?: string | null
  /** 显式指定 monaco 语言（优先于扩展名推断） */
  language?: string | null
  /** 文件名（据扩展名推断语言） */
  fileName?: string | null
  /** 只读（预览用） */
  readonly?: boolean
  /** 编辑器高度 */
  height?: string
  /** 是否显示复制按钮 */
  copyable?: boolean
}>(), {
  value: '',
  language: null,
  fileName: null,
  readonly: false,
  height: '320px',
  copyable: false,
})

const emit = defineEmits<{
  'update:value': [value: string]
}>()

// 离线安全：把 monaco 的 web worker 随应用打包（?worker），不走 CDN。
// 首次挂载编辑器前执行；本组件异步懒加载，故仅在真正用到编辑器时才付出代价。
window.MonacoEnvironment = {
  getWorker(_workerId, label) {
    if (label === 'json') {
      return new JsonWorker()
    }
    if (label === 'css' || label === 'scss' || label === 'less') {
      return new CssWorker()
    }
    if (label === 'html' || label === 'handlebars' || label === 'razor') {
      return new HtmlWorker()
    }
    if (label === 'typescript' || label === 'javascript') {
      return new TsWorker()
    }
    return new EditorWorker()
  },
}

/** 文件扩展名 → monaco 语言 id（未列出的走 plaintext，如 .sbn/.scriban 模板） */
const EXTENSION_LANGUAGE_MAP: Record<string, string> = {
  cs: 'csharp',
  ts: 'typescript',
  tsx: 'typescript',
  js: 'javascript',
  mjs: 'javascript',
  cjs: 'javascript',
  vue: 'html',
  html: 'html',
  xml: 'xml',
  json: 'json',
  sql: 'sql',
  css: 'css',
  scss: 'scss',
  less: 'less',
  md: 'markdown',
}

const { t } = useI18n()
const appStore = useAppStore()
const containerRef = useTemplateRef<HTMLElement>('container')
const copied = ref(false)

const resolvedLanguage = computed(() => {
  if (props.language) {
    return props.language
  }
  const name = props.fileName ?? ''
  const dot = name.lastIndexOf('.')
  if (dot < 0) {
    return 'plaintext'
  }
  return EXTENSION_LANGUAGE_MAP[name.slice(dot + 1).toLowerCase()] ?? 'plaintext'
})

let instance: Monaco.editor.IStandaloneCodeEditor | null = null
// 外部 setValue 会触发 onDidChangeModelContent，需抑制以免回环把光标顶回开头
let suppressChange = false
let copyTimer: ReturnType<typeof setTimeout> | null = null

function currentTheme() {
  return appStore.isDark ? 'vs-dark' : 'vs'
}

onMounted(() => {
  if (!containerRef.value) {
    return
  }

  instance = monaco.editor.create(containerRef.value, {
    value: props.value ?? '',
    language: resolvedLanguage.value,
    theme: currentTheme(),
    readOnly: props.readonly,
    automaticLayout: true,
    minimap: { enabled: false },
    fontSize: 12,
    lineHeight: 20,
    tabSize: 2,
    insertSpaces: true,
    scrollBeyondLastLine: false,
    lineNumbersMinChars: 3,
    fontFamily: 'ui-monospace, \'SFMono-Regular\', Consolas, \'Liberation Mono\', Menlo, monospace',
    wordWrap: 'off',
    fixedOverflowWidgets: true,
    scrollbar: { alwaysConsumeMouseWheel: false },
  })

  instance.onDidChangeModelContent(() => {
    if (suppressChange || !instance) {
      return
    }
    emit('update:value', instance.getValue())
  })

  // 若初次挂载时容器不可见（弹窗动画中）尺寸为 0，下一帧强制重新布局
  void nextTick(() => instance?.layout())
})

watch(() => props.value, (next) => {
  if (!instance) {
    return
  }
  const value = next ?? ''
  if (value !== instance.getValue()) {
    suppressChange = true
    try {
      instance.setValue(value)
    }
    finally {
      // 无论 setValue 是否抛错都复位，避免抑制标志卡死导致后续编辑不再 emit（静默丢数据）
      suppressChange = false
    }
  }
})

watch(resolvedLanguage, (language) => {
  const model = instance?.getModel()
  if (model) {
    monaco.editor.setModelLanguage(model, language)
  }
})

watch(() => props.readonly, (readonly) => {
  instance?.updateOptions({ readOnly: readonly })
})

watch(() => appStore.isDark, () => {
  monaco.editor.setTheme(currentTheme())
})

onBeforeUnmount(() => {
  if (copyTimer) {
    clearTimeout(copyTimer)
    copyTimer = null
  }
  instance?.getModel()?.dispose()
  instance?.dispose()
  instance = null
})

function markCopied() {
  copied.value = true
  if (copyTimer) {
    clearTimeout(copyTimer)
  }
  copyTimer = setTimeout(() => {
    copied.value = false
  }, 1500)
}

// 兜底复制：非安全上下文（http 且非 localhost）navigator.clipboard 为 undefined，降级到 execCommand
function fallbackCopy(text: string) {
  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.style.position = 'fixed'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.select()
  try {
    return document.execCommand('copy')
  }
  finally {
    document.body.removeChild(textarea)
  }
}

async function handleCopy() {
  const code = instance?.getValue() ?? props.value ?? ''
  if (!code) {
    return
  }
  try {
    if (navigator.clipboard) {
      await navigator.clipboard.writeText(code)
      markCopied()
      return
    }
    if (fallbackCopy(code)) {
      markCopied()
    }
  }
  catch {
    // 现代剪贴板 API 被拒时再试 execCommand 兜底
    if (fallbackCopy(code)) {
      markCopied()
    }
  }
}
</script>

<template>
  <div class="xh-editor" :style="{ height }">
    <button v-if="copyable" type="button" class="xh-editor__copy" @click="handleCopy">
      {{ copied ? t('component.code_editor.copy_success') : t('component.code_editor.copy') }}
    </button>
    <div ref="container" class="xh-editor__body" />
  </div>
</template>

<style scoped>
.xh-editor {
  position: relative;
  width: 100%;
  overflow: hidden;
  border: 1px solid hsl(var(--border));
  border-radius: 6px;
}

.xh-editor__body {
  width: 100%;
  height: 100%;
}

.xh-editor__copy {
  position: absolute;
  top: 6px;
  right: 18px;
  z-index: 2;
  padding: 2px 8px;
  font-size: 12px;
  color: var(--text-secondary);
  cursor: pointer;
  background: hsl(var(--background) / 0.85);
  border: 1px solid hsl(var(--border));
  border-radius: 4px;
  opacity: 0;
  transition: opacity 0.15s;
}

/* 默认隐藏、hover 编辑器时才浮现：避免常驻遮挡首行内容与右侧滚动条 */
.xh-editor:hover .xh-editor__copy {
  opacity: 1;
}

.xh-editor__copy:hover {
  color: hsl(var(--primary));
}
</style>
