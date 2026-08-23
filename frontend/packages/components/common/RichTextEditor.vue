<!-- 富文本编辑器封装模块：管理 Tiptap 生命周期、内容同步及常用格式工具。 -->
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
import { XhButton, XhButtonGroup, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTrigger, XhSeparator } from '@xihan-ui/vue'
import { onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import XInput from './XInput.vue'

defineOptions({ name: 'XRichTextEditor' })

const props = withDefaults(defineProps<{
  placeholder?: string
  disabled?: boolean
  minHeight?: string
}>(), {
  placeholder: undefined,
  disabled: false,
  minHeight: '200px',
})

const modelValue = defineModel<string>({ default: '' })

const { t } = useI18n()

/** 占位文本：外部未传时回落到 i18n 默认值 */
const resolvedPlaceholder = props.placeholder ?? t('component.rich_text_editor.placeholder')

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
    Placeholder.configure({ placeholder: resolvedPlaceholder }),
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
      <XhButtonGroup size="sm">
        <XhButton
          v-for="level in headingLevels"
          :key="level"
          :tone="isActive('heading', { level }) ? 'brand' : 'neutral'"
          variant="ghost"
          @click="editor!.chain().focus().toggleHeading({ level }).run()"
        >
          H{{ level }}
        </XhButton>
      </XhButtonGroup>

      <XhSeparator orientation="vertical" decorative />

      <!-- 基本格式 -->
      <XhButtonGroup size="sm">
        <XhButton
          :tone="isActive('bold') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.bold')"
          @click="editor!.chain().focus().toggleBold().run()"
        >
          <Icon icon="lucide:bold" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive('italic') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.italic')"
          @click="editor!.chain().focus().toggleItalic().run()"
        >
          <Icon icon="lucide:italic" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive('underline') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.underline')"
          @click="editor!.chain().focus().toggleUnderline().run()"
        >
          <Icon icon="lucide:underline" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive('strike') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.strikethrough')"
          @click="editor!.chain().focus().toggleStrike().run()"
        >
          <Icon icon="lucide:strikethrough" :width="16" />
        </XhButton>
      </XhButtonGroup>

      <XhSeparator orientation="vertical" decorative />

      <!-- 文本颜色 -->
      <XhPopoverRoot placement="bottom">
        <XhPopoverTrigger class="x-rte-pop-trigger" :title="t('component.rich_text_editor.text_color')">
          <div class="flex flex-col items-center">
            <Icon icon="lucide:baseline" :width="16" />
            <div class="-mt-0.5 w-3.5 h-0.5 rounded-sm" :style="{ background: textColor }" />
          </div>
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent>
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
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>

      <!-- 高亮色 -->
      <XhPopoverRoot placement="bottom">
        <XhPopoverTrigger class="x-rte-pop-trigger" :title="t('component.rich_text_editor.highlight')">
          <div class="flex flex-col items-center">
            <Icon icon="lucide:highlighter" :width="16" />
            <div class="-mt-0.5 w-3.5 h-0.5 rounded-sm" :style="{ background: highlightColor === 'transparent' ? '#e5e7eb' : highlightColor }" />
          </div>
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent>
            <div class="grid grid-cols-5 gap-1 p-1">
              <button
                v-for="c in HIGHLIGHT_COLORS"
                :key="c"
                class="w-6 h-6 rounded border border-gray-300 transition-transform cursor-pointer hover:scale-110"
                :class="{ 'ring-2 ring-blue-500 ring-offset-1': highlightColor === c }"
                :style="{ background: c === 'transparent' ? 'repeating-conic-gradient(#d1d5db 0% 25%, transparent 0% 50%) 50%/8px 8px' : c }"
                :title="c === 'transparent' ? t('component.rich_text_editor.clear_highlight') : c"
                @click="applyHighlight(c)"
              />
            </div>
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>

      <XhSeparator orientation="vertical" decorative />

      <!-- 对齐 -->
      <XhButtonGroup size="sm">
        <XhButton
          :tone="isActive({ textAlign: 'left' }) ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.align_left')"
          @click="editor!.chain().focus().setTextAlign('left').run()"
        >
          <Icon icon="lucide:align-left" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive({ textAlign: 'center' }) ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.align_center')"
          @click="editor!.chain().focus().setTextAlign('center').run()"
        >
          <Icon icon="lucide:align-center" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive({ textAlign: 'right' }) ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.align_right')"
          @click="editor!.chain().focus().setTextAlign('right').run()"
        >
          <Icon icon="lucide:align-right" :width="16" />
        </XhButton>
      </XhButtonGroup>

      <XhSeparator orientation="vertical" decorative />

      <!-- 列表 -->
      <XhButtonGroup size="sm">
        <XhButton
          :tone="isActive('bulletList') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.bullet_list')"
          @click="editor!.chain().focus().toggleBulletList().run()"
        >
          <Icon icon="lucide:list" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive('orderedList') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.ordered_list')"
          @click="editor!.chain().focus().toggleOrderedList().run()"
        >
          <Icon icon="lucide:list-ordered" :width="16" />
        </XhButton>
      </XhButtonGroup>

      <XhSeparator orientation="vertical" decorative />

      <!-- 引用 / 代码 / 分隔线 -->
      <XhButtonGroup size="sm">
        <XhButton
          :tone="isActive('blockquote') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.blockquote')"
          @click="editor!.chain().focus().toggleBlockquote().run()"
        >
          <Icon icon="lucide:quote" :width="16" />
        </XhButton>
        <XhButton
          :tone="isActive('codeBlock') ? 'brand' : 'neutral'"
          variant="ghost"
          :title="t('component.rich_text_editor.code_block')"
          @click="editor!.chain().focus().toggleCodeBlock().run()"
        >
          <Icon icon="lucide:code" :width="16" />
        </XhButton>
        <XhButton
          variant="ghost"
          :title="t('component.rich_text_editor.horizontal_rule')"
          @click="editor!.chain().focus().setHorizontalRule().run()"
        >
          <Icon icon="lucide:minus" :width="16" />
        </XhButton>
      </XhButtonGroup>

      <XhSeparator orientation="vertical" decorative />

      <!-- 链接 -->
      <XhPopoverRoot placement="bottom">
        <XhPopoverTrigger class="x-rte-pop-trigger" :class="{ 'x-rte-pop-trigger--on': isActive('link') }" :title="t('component.rich_text_editor.link')">
          <Icon icon="lucide:link" :width="16" />
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent>
            <div class="flex gap-2 items-center">
              <XInput v-model:value="linkUrl" placeholder="https://" size="sm" style="inline-size: 200px" />
              <XhButton size="sm" variant="solid" @click="setLink">
                {{ t('common.actions.confirm') }}
              </XhButton>
            </div>
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>

      <!-- 图片 -->
      <XhPopoverRoot placement="bottom">
        <XhPopoverTrigger class="x-rte-pop-trigger" :title="t('component.rich_text_editor.image')">
          <Icon icon="lucide:image" :width="16" />
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent>
            <div class="flex gap-2 items-center">
              <XInput v-model:value="imageUrl" :placeholder="t('component.rich_text_editor.image_url_placeholder')" size="sm" style="inline-size: 200px" />
              <XhButton size="sm" variant="solid" @click="addImage">
                {{ t('common.actions.confirm') }}
              </XhButton>
            </div>
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>

      <XhSeparator orientation="vertical" decorative />

      <!-- 撤销 / 重做 -->
      <XhButtonGroup size="sm">
        <XhButton
          variant="ghost"
          :disabled="!editor!.can().undo()"
          :title="t('component.rich_text_editor.undo')"
          @click="editor!.chain().focus().undo().run()"
        >
          <Icon icon="lucide:undo-2" :width="16" />
        </XhButton>
        <XhButton
          variant="ghost"
          :disabled="!editor!.can().redo()"
          :title="t('component.rich_text_editor.redo')"
          @click="editor!.chain().focus().redo().run()"
        >
          <Icon icon="lucide:redo-2" :width="16" />
        </XhButton>
      </XhButtonGroup>
    </div>

    <!-- 编辑区 -->
    <EditorContent :editor="editor" class="x-rte-content" :style="{ minHeight: props.minHeight }" />
  </div>
</template>

<style scoped>
/* 浮层触发器：与工具栏其它图标钮同款；激活态套品牌淡底 */
.x-rte-pop-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  block-size: var(--xh-control-h-sm);
  padding-inline: var(--xh-control-px-sm);
  border: 0;
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: inherit;
  cursor: pointer;
}

.x-rte-pop-trigger:hover {
  background: var(--xh-bg-subtle-hover);
}

.x-rte-pop-trigger--on {
  background: var(--xh-bg-brand-subtle);
  color: var(--xh-fg-brand);
}

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
