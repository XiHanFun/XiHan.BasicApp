<script lang="ts" setup>
import { NCard, NTabPane, NTabs } from 'naive-ui'
import { XJsonEditor, XMdEditor, XRichTextEditor } from '~/components'

defineOptions({ name: 'EditorDemoPage' })

const activeTab = ref('markdown')

// Markdown 编辑器数据
const mdContent = ref(`# Markdown 编辑器演示

## 功能特性

- 支持**粗体**、*斜体*等基本格式
- 支持代码高亮
- 支持数学公式
- 支持 Mermaid 图表

\`\`\`typescript
const greeting = (name: string) => {
  console.log(\`Hello, \${name}!\`)
}
\`\`\`

> 这是一段引用文本
`)

// JSON 编辑器数据
const jsonData = ref<Record<string, unknown>>({
  name: 'XiHan BasicApp',
  version: '1.2.0',
  features: ['Vue 3', 'Naive UI', 'TypeScript', 'Tailwind CSS'],
  config: {
    theme: 'light',
    language: 'zh-CN',
    layout: { sidebar: true, header: true, footer: false },
  },
})

// 富文本编辑器数据
const richContent = ref('<h2>富文本编辑器演示</h2><p>这是一段示例文本，你可以使用工具栏进行<strong>格式化</strong>操作。</p><ul><li>支持标题、粗体、斜体、下划线、删除线</li><li>支持文字颜色与背景高亮</li><li>支持有序/无序列表与引用</li><li>支持链接与图片插入</li></ul>')
</script>

<template>
  <div class="flex flex-col gap-3 p-4">
    <NCard :bordered="false" size="small">
      <template #header>
        <span class="text-base font-semibold">编辑器组件演示</span>
      </template>
      <NTabs v-model:value="activeTab" type="segment" animated>
        <!-- Markdown 编辑器 -->
        <NTabPane name="markdown" tab="Markdown 编辑器">
          <div class="flex flex-col gap-3 pt-3">
            <XMdEditor v-model="mdContent" editor-id="demo-md" />
            <NCard title="Markdown 原始内容" :bordered="true" size="small">
              <pre class="max-h-48 overflow-auto rounded bg-gray-50 p-3 text-xs leading-5">{{ mdContent }}</pre>
            </NCard>
          </div>
        </NTabPane>

        <!-- JSON 编辑器 -->
        <NTabPane name="json" tab="JSON 编辑器">
          <div class="flex flex-col gap-3 pt-3">
            <XJsonEditor v-model:json="jsonData" mode="tree" :height="420" />
            <NCard title="JSON 数据输出" :bordered="true" size="small">
              <pre class="max-h-48 overflow-auto rounded bg-gray-50 p-3 text-xs leading-5">{{ JSON.stringify(jsonData, null, 2) }}</pre>
            </NCard>
          </div>
        </NTabPane>

        <!-- 富文本编辑器 -->
        <NTabPane name="richtext" tab="富文本编辑器">
          <div class="flex flex-col gap-3 pt-3">
            <XRichTextEditor v-model="richContent" min-height="280px" />
            <NCard title="HTML 输出" :bordered="true" size="small">
              <pre class="max-h-48 overflow-auto rounded bg-gray-50 p-3 text-xs leading-5">{{ richContent }}</pre>
            </NCard>
          </div>
        </NTabPane>
      </NTabs>
    </NCard>
  </div>
</template>
