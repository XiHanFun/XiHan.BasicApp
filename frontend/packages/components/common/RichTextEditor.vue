<script setup lang="ts">
import { Color } from '@tiptap/extension-color'
import Highlight from '@tiptap/extension-highlight'
import Image from '@tiptap/extension-image'
import Link from '@tiptap/extension-link'
import Placeholder from '@tiptap/extension-placeholder'
import TextAlign from '@tiptap/extension-text-align'
import { TextStyle } from '@tiptap/extension-text-style'
import Underline from '@tiptap/extension-underline'
import StarterKit from '@tiptap/starter-kit'
import { EditorContent, useEditor } from '@tiptap/vue-3'
import { NButton, NButtonGroup, NDivider, NInput, NPopover } from 'naive-ui'
import { Icon } from '~/iconify'

defineOptions({ name: 'XRichTextEditor' })

const props = withDefaults(defineProps<{
  placeholder?: string
  disabled?: boolean
  minHeight?: string
}>(), {
  placeholder: '请输入内容...',
  disabled: false,
  minHeight: '200px',
})

const modelValue = defineModel<string>({ default: '' })

const linkUrl = ref('')
const imageUrl = ref('')
const textColor = ref('#000000')
const highlightColor = ref('#fef08a')

const TEXT_COLORS = ['#000000', '#434343', '#e03131', '#2f9e44', '#1971c2', '#f08c00', '#7048e8', '#0c8599', '#ffffff']
const HIGHLIGHT_COLORS = ['#fef08a', '#bbf7d0', '#bfdbfe', '#fecaca', '#e9d5ff', '#fed7aa', '#fecdd3', '#d1d5db', 'transparent']

const editor = useEditor({
  content: modelValue.value,
  editable: !props.disabled,
  extensions: [
    StarterKit,
    Placeholder.configure({ placeholder: props.placeholder }),
    Underline,
    TextAlign.configure({ types: ['heading', 'paragraph'] }),
    Link.configure({ openOnClick: false }),
    Image.configure({ inline: true }),
    TextStyle,
    Color,
    Highlight.configure({ multicolor: true }),
  ],
  onUpdate: ({ editor: e }) => {
    modelValue.value = e.getHTML()
  },
})

watch(modelValue, (val) => {
  if (editor.value && val !== editor.value.getHTML()) {
    editor.value.commands.setContent(val, { emitUpdate: false })
  }
})

watch(() => props.disabled, (val) => {
  editor.value?.setEditable(!val)
})

onBeforeUnmount(() => {
  editor.value?.destroy()
})

function applyTextColor(color: string) {
  textColor.value = color
  editor.value?.chain().focus().setColor(color).run()
}

function applyHighlight(color: string) {
  highlightColor.value = color
  if (color === 'transparent') {
    editor.value?.chain().focus().unsetHighlight().run()
  }
  else {
    editor.value?.chain().focus().toggleHighlight({ color }).run()
  }
}

function setLink() {
  if (!linkUrl.value) {
    editor.value?.chain().focus().extendMarkRange('link').unsetLink().run()
    return
  }
  editor.value?.chain().focus().extendMarkRange('link').setLink({ href: linkUrl.value }).run()
  linkUrl.value = ''
}

function addImage() {
  if (!imageUrl.value) {
    return
  }
  editor.value?.chain().focus().setImage({ src: imageUrl.value }).run()
  imageUrl.value = ''
}

function isActive(nameOrAttrs: string | Record<string, unknown>, attrs?: Record<string, unknown>) {
  return editor.value?.isActive(nameOrAttrs as string, attrs) ?? false
}

const headingLevels = [1, 2, 3, 4] as const
</script>

<template>
  <div class="rounded border border-gray-200 x-rte dark:border-gray-600">
    <!-- 工具栏 -->
    <div v-if="editor && !props.disabled" class="flex flex-wrap gap-1 items-center px-2 py-1.5 bg-gray-50 border-b border-gray-200 dark:border-gray-600 dark:bg-gray-800">
      <!-- 标题 -->
      <NButtonGroup size="small">
        <NButton
          v-for="level in headingLevels"
          :key="level"
          :type="isActive('heading', { level }) ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleHeading({ level }).run()"
        >
          H{{ level }}
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 基本格式 -->
      <NButtonGroup size="small">
        <NButton
          :type="isActive('bold') ? 'primary' : 'default'"
          quaternary
          title="粗体"
          @click="editor!.chain().focus().toggleBold().run()"
        >
          <Icon icon="lucide:bold" :width="16" />
        </NButton>
        <NButton
          :type="isActive('italic') ? 'primary' : 'default'"
          quaternary
          title="斜体"
          @click="editor!.chain().focus().toggleItalic().run()"
        >
          <Icon icon="lucide:italic" :width="16" />
        </NButton>
        <NButton
          :type="isActive('underline') ? 'primary' : 'default'"
          quaternary
          title="下划线"
          @click="editor!.chain().focus().toggleUnderline().run()"
        >
          <Icon icon="lucide:underline" :width="16" />
        </NButton>
        <NButton
          :type="isActive('strike') ? 'primary' : 'default'"
          quaternary
          title="删除线"
          @click="editor!.chain().focus().toggleStrike().run()"
        >
          <Icon icon="lucide:strikethrough" :width="16" />
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 文本颜色 -->
      <NPopover trigger="click" placement="bottom" :show-arrow="false">
        <template #trigger>
          <NButton size="small" quaternary title="文字颜色">
            <div class="flex flex-col items-center">
              <Icon icon="lucide:baseline" :width="16" />
              <div class="-mt-0.5 w-3.5 h-0.5 rounded-sm" :style="{ background: textColor }" />
            </div>
          </NButton>
        </template>
        <div class="grid grid-cols-5 gap-1 p-1">
          <button
            v-for="c in TEXT_COLORS"
            :key="c"
            class="w-6 h-6 rounded border border-gray-300 transition-transform cursor-pointer hover:scale-110"
            :class="{ 'ring-2 ring-blue-500 ring-offset-1': textColor === c }"
            :style="{ background: c }"
            @click="applyTextColor(c)"
          />
        </div>
      </NPopover>

      <!-- 高亮色 -->
      <NPopover trigger="click" placement="bottom" :show-arrow="false">
        <template #trigger>
          <NButton size="small" quaternary title="背景高亮">
            <div class="flex flex-col items-center">
              <Icon icon="lucide:highlighter" :width="16" />
              <div class="-mt-0.5 w-3.5 h-0.5 rounded-sm" :style="{ background: highlightColor === 'transparent' ? '#e5e7eb' : highlightColor }" />
            </div>
          </NButton>
        </template>
        <div class="grid grid-cols-5 gap-1 p-1">
          <button
            v-for="c in HIGHLIGHT_COLORS"
            :key="c"
            class="w-6 h-6 rounded border border-gray-300 transition-transform cursor-pointer hover:scale-110"
            :class="{ 'ring-2 ring-blue-500 ring-offset-1': highlightColor === c }"
            :style="{ background: c === 'transparent' ? 'repeating-conic-gradient(#d1d5db 0% 25%, transparent 0% 50%) 50%/8px 8px' : c }"
            :title="c === 'transparent' ? '清除高亮' : c"
            @click="applyHighlight(c)"
          />
        </div>
      </NPopover>

      <NDivider vertical />

      <!-- 对齐 -->
      <NButtonGroup size="small">
        <NButton
          :type="isActive({ textAlign: 'left' }) ? 'primary' : 'default'"
          quaternary
          title="左对齐"
          @click="editor!.chain().focus().setTextAlign('left').run()"
        >
          <Icon icon="lucide:align-left" :width="16" />
        </NButton>
        <NButton
          :type="isActive({ textAlign: 'center' }) ? 'primary' : 'default'"
          quaternary
          title="居中"
          @click="editor!.chain().focus().setTextAlign('center').run()"
        >
          <Icon icon="lucide:align-center" :width="16" />
        </NButton>
        <NButton
          :type="isActive({ textAlign: 'right' }) ? 'primary' : 'default'"
          quaternary
          title="右对齐"
          @click="editor!.chain().focus().setTextAlign('right').run()"
        >
          <Icon icon="lucide:align-right" :width="16" />
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 列表 -->
      <NButtonGroup size="small">
        <NButton
          :type="isActive('bulletList') ? 'primary' : 'default'"
          quaternary
          title="无序列表"
          @click="editor!.chain().focus().toggleBulletList().run()"
        >
          <Icon icon="lucide:list" :width="16" />
        </NButton>
        <NButton
          :type="isActive('orderedList') ? 'primary' : 'default'"
          quaternary
          title="有序列表"
          @click="editor!.chain().focus().toggleOrderedList().run()"
        >
          <Icon icon="lucide:list-ordered" :width="16" />
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 引用 / 代码 / 分隔线 -->
      <NButtonGroup size="small">
        <NButton
          :type="isActive('blockquote') ? 'primary' : 'default'"
          quaternary
          title="引用"
          @click="editor!.chain().focus().toggleBlockquote().run()"
        >
          <Icon icon="lucide:quote" :width="16" />
        </NButton>
        <NButton
          :type="isActive('codeBlock') ? 'primary' : 'default'"
          quaternary
          title="代码块"
          @click="editor!.chain().focus().toggleCodeBlock().run()"
        >
          <Icon icon="lucide:code" :width="16" />
        </NButton>
        <NButton
          quaternary
          title="分隔线"
          @click="editor!.chain().focus().setHorizontalRule().run()"
        >
          <Icon icon="lucide:minus" :width="16" />
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 链接 -->
      <NPopover trigger="click" placement="bottom">
        <template #trigger>
          <NButton
            size="small"
            :type="isActive('link') ? 'primary' : 'default'"
            quaternary
            title="链接"
          >
            <Icon icon="lucide:link" :width="16" />
          </NButton>
        </template>
        <div class="flex gap-2 items-center">
          <NInput v-model:value="linkUrl" placeholder="https://" size="small" style="width: 200px" />
          <NButton size="small" type="primary" @click="setLink">
            确定
          </NButton>
        </div>
      </NPopover>

      <!-- 图片 -->
      <NPopover trigger="click" placement="bottom">
        <template #trigger>
          <NButton size="small" quaternary title="图片">
            <Icon icon="lucide:image" :width="16" />
          </NButton>
        </template>
        <div class="flex gap-2 items-center">
          <NInput v-model:value="imageUrl" placeholder="图片地址" size="small" style="width: 200px" />
          <NButton size="small" type="primary" @click="addImage">
            确定
          </NButton>
        </div>
      </NPopover>

      <NDivider vertical />

      <!-- 撤销 / 重做 -->
      <NButtonGroup size="small">
        <NButton
          quaternary
          :disabled="!editor!.can().undo()"
          title="撤销"
          @click="editor!.chain().focus().undo().run()"
        >
          <Icon icon="lucide:undo-2" :width="16" />
        </NButton>
        <NButton
          quaternary
          :disabled="!editor!.can().redo()"
          title="重做"
          @click="editor!.chain().focus().redo().run()"
        >
          <Icon icon="lucide:redo-2" :width="16" />
        </NButton>
      </NButtonGroup>
    </div>

    <!-- 编辑区 -->
    <EditorContent :editor="editor" class="x-rte-content" :style="{ minHeight: props.minHeight }" />
  </div>
</template>

<style scoped>
.x-rte-content :deep(.tiptap) {
  padding: 12px 16px;
  outline: none;
  min-height: v-bind('props.minHeight');
}

.x-rte-content :deep(.tiptap p.is-editor-empty:first-child::before) {
  content: attr(data-placeholder);
  float: left;
  color: #adb5bd;
  pointer-events: none;
  height: 0;
}

.x-rte-content :deep(.tiptap h1) {
  font-size: 1.75em;
  font-weight: 700;
  margin: 0.5em 0;
}

.x-rte-content :deep(.tiptap h2) {
  font-size: 1.5em;
  font-weight: 700;
  margin: 0.4em 0;
}

.x-rte-content :deep(.tiptap h3) {
  font-size: 1.25em;
  font-weight: 600;
  margin: 0.3em 0;
}

.x-rte-content :deep(.tiptap h4) {
  font-size: 1.1em;
  font-weight: 600;
  margin: 0.2em 0;
}

.x-rte-content :deep(.tiptap ul) {
  list-style: disc;
  padding-left: 1.5em;
}

.x-rte-content :deep(.tiptap ol) {
  list-style: decimal;
  padding-left: 1.5em;
}

.x-rte-content :deep(.tiptap blockquote) {
  border-left: 3px solid #d1d5db;
  padding-left: 1em;
  color: #6b7280;
  margin: 0.5em 0;
}

.x-rte-content :deep(.tiptap pre) {
  background: #f3f4f6;
  border-radius: 6px;
  padding: 12px 16px;
  font-family: ui-monospace, monospace;
  font-size: 0.875em;
  overflow-x: auto;
}

.x-rte-content :deep(.tiptap code) {
  background: #f3f4f6;
  border-radius: 3px;
  padding: 2px 4px;
  font-size: 0.9em;
}

.x-rte-content :deep(.tiptap hr) {
  border: none;
  border-top: 1px solid #e5e7eb;
  margin: 1em 0;
}

.x-rte-content :deep(.tiptap a) {
  color: #2563eb;
  text-decoration: underline;
  cursor: pointer;
}

.x-rte-content :deep(.tiptap img) {
  max-width: 100%;
  height: auto;
  border-radius: 4px;
}
</style>
