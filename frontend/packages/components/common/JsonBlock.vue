<script setup lang="ts">
import { XhJsonViewerRoot } from '@xihan-ui/vue'
import { computed } from 'vue'

/**
 * JSON 展示块：原文能解析成对象或数组就铺成可折叠的树，否则直接显示原文。
 * 工具条（展开 / 收起全部、原文与自动换行、复制）由查看器内建；原文档与复制取的一律是后端下发的原文，
 * 64 位 ID 这类超出安全整数的数字不会因解析而失真。
 */
defineOptions({ name: 'XJsonBlock' })

const props = withDefaults(defineProps<{
  /** 后端下发的 JSON 原文 */
  raw?: null | string
  /** 展开到第几层 */
  defaultExpandedDepth?: number
  /** 单个字符串值最多显示几个字符，超出由组件截断 */
  maxStringLength?: number
  /** 数组最多铺几项 */
  maxItems?: number
  /** 块高上限，超出内部滚动 */
  maxHeight?: string
}>(), {
  raw: '',
  defaultExpandedDepth: 1,
  maxStringLength: 200,
  maxItems: 100,
  maxHeight: '24rem',
})

/** 解析不成树的原文只能看原文：工具条只留换行与复制 */
const TEXT_TOOLS: Array<'wrap' | 'copy'> = ['wrap', 'copy']

const text = computed(() => props.raw ?? '')

/** 树视图的数据源；解析不出对象或数组时为空，此时只显示原文 */
const tree = computed<unknown>(() => {
  if (!text.value) {
    return undefined
  }
  try {
    const parsed: unknown = JSON.parse(text.value)
    return typeof parsed === 'object' && parsed !== null ? parsed : undefined
  }
  catch {
    return undefined
  }
})
</script>

<template>
  <XhJsonViewerRoot
    v-if="tree"
    class="x-json-block"
    :style="{ '--xh-json-viewer-max-h': maxHeight }"
    :value="tree"
    :source="text"
    tools
    :default-expanded-depth="defaultExpandedDepth"
    :max-string-length="maxStringLength"
    :max-items="maxItems"
    size="sm"
  />
  <XhJsonViewerRoot
    v-else-if="text"
    class="x-json-block"
    :style="{ '--xh-json-viewer-max-h': maxHeight }"
    :value="text"
    :source="text"
    :tools="TEXT_TOOLS"
    default-view="text"
    default-wrap
    size="sm"
  />
  <span v-else>-</span>
</template>
