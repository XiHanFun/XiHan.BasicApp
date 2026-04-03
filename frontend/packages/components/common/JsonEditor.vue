<script setup lang="ts">
import JsonEditorVue from 'vue3-ts-jsoneditor'

defineOptions({ name: 'XJsonEditor' })

const props = withDefaults(defineProps<{
  /** 编辑器高度，默认 400px */
  height?: string | number
  /** 显示模式 */
  mode?: 'tree' | 'text' | 'table'
  /** 只读 */
  readOnly?: boolean
  /** 主颜色 */
  mainMenuBar?: boolean
  /** 显示导航栏 */
  navigationBar?: boolean
  /** 显示状态栏 */
  statusBar?: boolean
}>(), {
  height: 400,
  mode: 'tree',
  readOnly: false,
  mainMenuBar: true,
  navigationBar: true,
  statusBar: true,
})

/** 绑定 JSON 对象类型数据 */
const jsonModel = defineModel<Record<string, unknown> | unknown[] | null>('json', { default: undefined })
/** 绑定 JSON 字符串类型数据 */
const textModel = defineModel<string>('text', { default: undefined })

const emit = defineEmits<{
  error: [error: Error]
  focus: []
  blur: []
}>()

const heightStyle = computed(() => {
  return typeof props.height === 'number' ? `${props.height}px` : props.height
})
</script>

<template>
  <div :style="{ height: heightStyle }" class="x-json-editor">
    <JsonEditorVue
      v-model:json="jsonModel"
      v-model:text="textModel"
      :mode="props.mode"
      :read-only="props.readOnly"
      :main-menu-bar="props.mainMenuBar"
      :navigation-bar="props.navigationBar"
      :status-bar="props.statusBar"
      @error="(e: Error) => emit('error', e)"
      @focus="() => emit('focus')"
      @blur="() => emit('blur')"
    />
  </div>
</template>

<style scoped>
.x-json-editor {
  width: 100%;
}
.x-json-editor :deep(.jse-main) {
  height: 100%;
}
</style>
