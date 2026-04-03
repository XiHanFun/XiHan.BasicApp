<script setup lang="ts">
import { NButton, NButtonGroup, NDivider, NPopover, NInput, NColorPicker } from 'naive-ui'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Placeholder from '@tiptap/extension-placeholder'
import Underline from '@tiptap/extension-underline'
import TextAlign from '@tiptap/extension-text-align'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import TextStyle from '@tiptap/extension-text-style'
import { Color } from '@tiptap/extension-color'
import Highlight from '@tiptap/extension-highlight'

defineOptions({ name: 'XRichTextEditor' })

const props = withDefaults(defineProps<{
  /** 占位文本 */
  placeholder?: string
  /** 是否只读 */
  disabled?: boolean
  /** 最小高度 */
  minHeight?: string
}>(), {
  placeholder: '请输入内容...',
  disabled: false,
  minHeight: '200px',
})

const modelValue = defineModel<string>({ default: '' })

const linkUrl = ref('')
const imageUrl = ref('')

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
    editor.value.commands.setContent(val, false)
  }
})

watch(() => props.disabled, (val) => {
  editor.value?.setEditable(!val)
})

onBeforeUnmount(() => {
  editor.value?.destroy()
})

/** 插入链接 */
function setLink() {
  if (!linkUrl.value) {
    editor.value?.chain().focus().extendMarkRange('link').unsetLink().run()
    return
  }
  editor.value?.chain().focus().extendMarkRange('link').setLink({ href: linkUrl.value }).run()
  linkUrl.value = ''
}

/** 插入图片 */
function addImage() {
  if (!imageUrl.value) return
  editor.value?.chain().focus().setImage({ src: imageUrl.value }).run()
  imageUrl.value = ''
}

/** 工具栏按钮激活状态，支持 isActive('bold') 和 isActive({ textAlign: 'left' }) 两种调用 */
function isActive(nameOrAttrs: string | Record<string, unknown>, attrs?: Record<string, unknown>) {
  return editor.value?.isActive(nameOrAttrs as string, attrs) ?? false
}

const headingLevels = [1, 2, 3, 4] as const
</script>

<template>
  <div class="x-rich-text-editor rounded border border-gray-200 dark:border-gray-700">
    <!-- 工具栏 -->
    <div v-if="editor && !props.disabled" class="flex flex-wrap items-center gap-1 border-b border-gray-200 px-2 py-1 dark:border-gray-700">
      <!-- 标题 -->
      <NButtonGroup size="tiny">
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
      <NButtonGroup size="tiny">
        <NButton
          :type="isActive('bold') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleBold().run()"
          title="粗体"
        >
          <span class="font-bold">B</span>
        </NButton>
        <NButton
          :type="isActive('italic') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleItalic().run()"
          title="斜体"
        >
          <span class="italic">I</span>
        </NButton>
        <NButton
          :type="isActive('underline') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleUnderline().run()"
          title="下划线"
        >
          <span class="underline">U</span>
        </NButton>
        <NButton
          :type="isActive('strike') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleStrike().run()"
          title="删除线"
        >
          <span class="line-through">S</span>
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 文本颜色 -->
      <NPopover trigger="click" placement="bottom" :show-arrow="false">
        <template #trigger>
          <NButton size="tiny" quaternary title="文字颜色">
            <span class="font-bold" :style="{ color: editor!.getAttributes('textStyle').color || '#000' }">A</span>
          </NButton>
        </template>
        <NColorPicker
          style="width: 240px"
          :default-value="editor!.getAttributes('textStyle').color || '#000000'"
          @update:value="(c: string) => editor!.chain().focus().setColor(c).run()"
        />
      </NPopover>

      <!-- 高亮色 -->
      <NPopover trigger="click" placement="bottom" :show-arrow="false">
        <template #trigger>
          <NButton size="tiny" quaternary title="背景高亮">
            <span class="rounded bg-yellow-200 px-1 text-xs font-bold">H</span>
          </NButton>
        </template>
        <NColorPicker
          style="width: 240px"
          default-value="#fef08a"
          @update:value="(c: string) => editor!.chain().focus().toggleHighlight({ color: c }).run()"
        />
      </NPopover>

      <NDivider vertical />

      <!-- 对齐 -->
      <NButtonGroup size="tiny">
        <NButton
          :type="isActive({ textAlign: 'left' }) ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().setTextAlign('left').run()"
          title="左对齐"
        >
          ≡
        </NButton>
        <NButton
          :type="isActive({ textAlign: 'center' }) ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().setTextAlign('center').run()"
          title="居中"
        >
          ≡
        </NButton>
        <NButton
          :type="isActive({ textAlign: 'right' }) ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().setTextAlign('right').run()"
          title="右对齐"
        >
          ≡
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 列表 -->
      <NButtonGroup size="tiny">
        <NButton
          :type="isActive('bulletList') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleBulletList().run()"
          title="无序列表"
        >
          •
        </NButton>
        <NButton
          :type="isActive('orderedList') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleOrderedList().run()"
          title="有序列表"
        >
          1.
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 引用 / 代码 / 分隔线 -->
      <NButtonGroup size="tiny">
        <NButton
          :type="isActive('blockquote') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleBlockquote().run()"
          title="引用"
        >
          「」
        </NButton>
        <NButton
          :type="isActive('codeBlock') ? 'primary' : 'default'"
          quaternary
          @click="editor!.chain().focus().toggleCodeBlock().run()"
          title="代码块"
        >
          &lt;/&gt;
        </NButton>
        <NButton
          quaternary
          @click="editor!.chain().focus().setHorizontalRule().run()"
          title="分隔线"
        >
          ─
        </NButton>
      </NButtonGroup>

      <NDivider vertical />

      <!-- 链接 -->
      <NPopover trigger="click" placement="bottom">
        <template #trigger>
          <NButton
            size="tiny"
            :type="isActive('link') ? 'primary' : 'default'"
            quaternary
            title="链接"
          >
            🔗
          </NButton>
        </template>
        <div class="flex items-center gap-2">
          <NInput v-model:value="linkUrl" placeholder="https://" size="small" style="width: 200px" />
          <NButton size="small" type="primary" @click="setLink">确定</NButton>
        </div>
      </NPopover>

      <!-- 图片 -->
      <NPopover trigger="click" placement="bottom">
        <template #trigger>
          <NButton size="tiny" quaternary title="图片">
            🖼
          </NButton>
        </template>
        <div class="flex items-center gap-2">
          <NInput v-model:value="imageUrl" placeholder="图片地址" size="small" style="width: 200px" />
          <NButton size="small" type="primary" @click="addImage">确定</NButton>
        </div>
      </NPopover>

      <NDivider vertical />

      <!-- 撤销 / 重做 -->
      <NButtonGroup size="tiny">
        <NButton
          quaternary
          :disabled="!editor!.can().undo()"
          @click="editor!.chain().focus().undo().run()"
          title="撤销"
        >
          ↩
        </NButton>
        <NButton
          quaternary
          :disabled="!editor!.can().redo()"
          @click="editor!.chain().focus().redo().run()"
          title="重做"
        >
          ↪
        </NButton>
      </NButtonGroup>
    </div>

    <!-- 编辑区 -->
    <EditorContent :editor="editor" class="x-rich-text-content" :style="{ minHeight: props.minHeight }" />
  </div>
</template>

<style scoped>
.x-rich-text-content :deep(.tiptap) {
  padding: 12px 16px;
  outline: none;
  min-height: v-bind('props.minHeight');
}

.x-rich-text-content :deep(.tiptap p.is-editor-empty:first-child::before) {
  content: attr(data-placeholder);
  float: left;
  color: #adb5bd;
  pointer-events: none;
  height: 0;
}

.x-rich-text-content :deep(.tiptap h1) { font-size: 1.75em; font-weight: 700; margin: 0.5em 0; }
.x-rich-text-content :deep(.tiptap h2) { font-size: 1.5em; font-weight: 700; margin: 0.4em 0; }
.x-rich-text-content :deep(.tiptap h3) { font-size: 1.25em; font-weight: 600; margin: 0.3em 0; }
.x-rich-text-content :deep(.tiptap h4) { font-size: 1.1em; font-weight: 600; margin: 0.2em 0; }

.x-rich-text-content :deep(.tiptap ul) { list-style: disc; padding-left: 1.5em; }
.x-rich-text-content :deep(.tiptap ol) { list-style: decimal; padding-left: 1.5em; }

.x-rich-text-content :deep(.tiptap blockquote) {
  border-left: 3px solid #d1d5db;
  padding-left: 1em;
  color: #6b7280;
  margin: 0.5em 0;
}

.x-rich-text-content :deep(.tiptap pre) {
  background: #f3f4f6;
  border-radius: 6px;
  padding: 12px 16px;
  font-family: ui-monospace, monospace;
  font-size: 0.875em;
  overflow-x: auto;
}

.x-rich-text-content :deep(.tiptap code) {
  background: #f3f4f6;
  border-radius: 3px;
  padding: 2px 4px;
  font-size: 0.9em;
}

.x-rich-text-content :deep(.tiptap hr) {
  border: none;
  border-top: 1px solid #e5e7eb;
  margin: 1em 0;
}

.x-rich-text-content :deep(.tiptap a) {
  color: #2563eb;
  text-decoration: underline;
  cursor: pointer;
}

.x-rich-text-content :deep(.tiptap img) {
  max-width: 100%;
  height: auto;
  border-radius: 4px;
}
</style>
