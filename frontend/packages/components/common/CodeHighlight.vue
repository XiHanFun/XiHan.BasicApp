<script setup lang="ts">
import hljs from 'highlight.js/lib/core'
import csharp from 'highlight.js/lib/languages/csharp'
import css from 'highlight.js/lib/languages/css'
import javascript from 'highlight.js/lib/languages/javascript'
import json from 'highlight.js/lib/languages/json'
import markdown from 'highlight.js/lib/languages/markdown'
import sql from 'highlight.js/lib/languages/sql'
import typescript from 'highlight.js/lib/languages/typescript'
import xml from 'highlight.js/lib/languages/xml'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '~/stores'

defineOptions({ name: 'XCodeHighlight' })

const props = withDefaults(defineProps<{
  /** 代码内容 */
  code?: string | null
  /** 文件名（据扩展名推断语言） */
  fileName?: string | null
  /** 显式指定 highlight.js 语言（优先于扩展名推断） */
  language?: string | null
  /** 代码区最大高度（超出内部滚动） */
  maxHeight?: string
  /** 是否显示复制按钮 */
  copyable?: boolean
}>(), {
  code: '',
  fileName: null,
  language: null,
  maxHeight: '60vh',
  copyable: true,
})

// 仅注册代码生成实际会产出的语言，避免引入 highlight.js 全量语言包
hljs.registerLanguage('csharp', csharp)
hljs.registerLanguage('typescript', typescript)
hljs.registerLanguage('javascript', javascript)
hljs.registerLanguage('xml', xml)
hljs.registerLanguage('json', json)
hljs.registerLanguage('sql', sql)
hljs.registerLanguage('css', css)
hljs.registerLanguage('markdown', markdown)

/** 文件扩展名 → highlight.js 语言（未列出的走纯文本，如 .sbn/.scriban 模板） */
const EXTENSION_LANGUAGE_MAP: Record<string, string> = {
  cs: 'csharp',
  ts: 'typescript',
  tsx: 'typescript',
  js: 'javascript',
  mjs: 'javascript',
  cjs: 'javascript',
  vue: 'xml',
  html: 'xml',
  xml: 'xml',
  json: 'json',
  sql: 'sql',
  css: 'css',
  scss: 'css',
  less: 'css',
  md: 'markdown',
}

/** 超过此长度不做高亮，直接纯文本，避免大文件解析卡顿 */
const MAX_HIGHLIGHT_LENGTH = 120_000

const { t } = useI18n()
const appStore = useAppStore()
const isDark = computed(() => appStore.isDark)
const copied = ref(false)

const resolvedLanguage = computed(() => {
  if (props.language) {
    return props.language
  }
  const name = props.fileName ?? ''
  const dot = name.lastIndexOf('.')
  if (dot < 0) {
    return null
  }
  return EXTENSION_LANGUAGE_MAP[name.slice(dot + 1).toLowerCase()] ?? null
})

function escapeHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

const highlightedHtml = computed(() => {
  const code = props.code ?? ''
  const language = resolvedLanguage.value
  if (code.length > MAX_HIGHLIGHT_LENGTH || !language || !hljs.getLanguage(language)) {
    return escapeHtml(code)
  }
  try {
    return hljs.highlight(code, { language, ignoreIllegals: true }).value
  }
  catch {
    return escapeHtml(code)
  }
})

const languageBadge = computed(() => resolvedLanguage.value ?? 'text')

async function handleCopy() {
  const code = props.code ?? ''
  if (!code || !navigator.clipboard) {
    return
  }
  try {
    await navigator.clipboard.writeText(code)
    copied.value = true
    globalThis.setTimeout(() => {
      copied.value = false
    }, 1500)
  }
  catch {
    // 剪贴板不可用时静默降级
  }
}
</script>

<template>
  <div class="xh-code" :class="{ 'xh-code--dark': isDark }">
    <div class="xh-code__bar">
      <span class="xh-code__lang">{{ languageBadge }}</span>
      <button v-if="copyable" type="button" class="xh-code__copy" @click="handleCopy">
        {{ copied ? t('develop.code_gen.generate.copy_success') : t('develop.code_gen.generate.copy_code') }}
      </button>
    </div>
    <pre class="xh-code__pre" :style="{ maxHeight }"><code class="hljs" v-html="highlightedHtml" /></pre>
  </div>
</template>

<style scoped>
.xh-code {
  position: relative;
  border-radius: 8px;
  background: hsl(var(--muted) / 0.5);
  overflow: hidden;
}

.xh-code__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 10px;
  border-bottom: 1px solid hsl(var(--border));
  background: hsl(var(--muted) / 0.6);
}

.xh-code__lang {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.xh-code__copy {
  padding: 2px 8px;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: hsl(var(--primary));
  font-size: 11px;
  cursor: pointer;
}

.xh-code__copy:hover {
  background: hsl(var(--primary) / 0.1);
}

.xh-code__pre {
  margin: 0;
  padding: 10px 12px;
  overflow: auto;
  font-family: var(--font-mono, ui-monospace, 'SFMono-Regular', 'Consolas', monospace);
  font-size: 12px;
  line-height: 1.6;
  tab-size: 2;
}

/* 语法着色：跟随应用明/暗主题的 token 配色（highlight.js 结构类名，自定义配色不引主题 CSS） */
.xh-code :deep(.hljs-comment),
.xh-code :deep(.hljs-quote) {
  color: #6e7781;
  font-style: italic;
}

.xh-code :deep(.hljs-keyword),
.xh-code :deep(.hljs-selector-tag),
.xh-code :deep(.hljs-literal) {
  color: #cf222e;
}

.xh-code :deep(.hljs-string),
.xh-code :deep(.hljs-attr),
.xh-code :deep(.hljs-meta-string) {
  color: #0a3069;
}

.xh-code :deep(.hljs-number),
.xh-code :deep(.hljs-symbol),
.xh-code :deep(.hljs-bullet) {
  color: #0550ae;
}

.xh-code :deep(.hljs-title),
.xh-code :deep(.hljs-title.function_),
.xh-code :deep(.hljs-section) {
  color: #6639ba;
}

.xh-code :deep(.hljs-type),
.xh-code :deep(.hljs-class .hljs-title),
.xh-code :deep(.hljs-built_in) {
  color: #953800;
}

.xh-code :deep(.hljs-tag),
.xh-code :deep(.hljs-name),
.xh-code :deep(.hljs-attribute) {
  color: #116329;
}

.xh-code :deep(.hljs-meta),
.xh-code :deep(.hljs-doctag) {
  color: #6e7781;
}

/* 暗色配色 */
.xh-code--dark :deep(.hljs-comment),
.xh-code--dark :deep(.hljs-quote) {
  color: #8b949e;
}

.xh-code--dark :deep(.hljs-keyword),
.xh-code--dark :deep(.hljs-selector-tag),
.xh-code--dark :deep(.hljs-literal) {
  color: #ff7b72;
}

.xh-code--dark :deep(.hljs-string),
.xh-code--dark :deep(.hljs-attr),
.xh-code--dark :deep(.hljs-meta-string) {
  color: #a5d6ff;
}

.xh-code--dark :deep(.hljs-number),
.xh-code--dark :deep(.hljs-symbol),
.xh-code--dark :deep(.hljs-bullet) {
  color: #79c0ff;
}

.xh-code--dark :deep(.hljs-title),
.xh-code--dark :deep(.hljs-title.function_),
.xh-code--dark :deep(.hljs-section) {
  color: #d2a8ff;
}

.xh-code--dark :deep(.hljs-type),
.xh-code--dark :deep(.hljs-class .hljs-title),
.xh-code--dark :deep(.hljs-built_in) {
  color: #ffa657;
}

.xh-code--dark :deep(.hljs-tag),
.xh-code--dark :deep(.hljs-name),
.xh-code--dark :deep(.hljs-attribute) {
  color: #7ee787;
}

.xh-code--dark :deep(.hljs-meta),
.xh-code--dark :deep(.hljs-doctag) {
  color: #8b949e;
}
</style>
