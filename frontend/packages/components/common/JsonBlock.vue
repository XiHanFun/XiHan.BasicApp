<script setup lang="ts">
import { XhClipboardIndicator, XhClipboardRoot, XhClipboardTrigger, XhJsonViewerRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

/**
 * JSON 展示块：原文能解析成对象或数组就铺成可折叠的树，否则原样出纯文本。
 * 右上角的复制钮取的一律是后端下发的原文，不是树里那份重新格式化过的值。
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

const { t } = useI18n()

const text = computed(() => props.raw ?? '')

/**
 * 树视图的数据源；解析不出对象或数组时为空，此时退回纯文本。
 * XhJsonViewerRoot 的 value 只在类型上被推成 undefined，运行期收任意形状。
 */
const tree = computed(() => {
  if (!text.value) {
    return undefined
  }
  try {
    const parsed: unknown = JSON.parse(text.value)
    return (typeof parsed === 'object' && parsed !== null ? parsed : undefined) as undefined
  }
  catch {
    return undefined
  }
})
</script>

<template>
  <div class="x-json-block" :style="{ '--x-json-block-max-h': maxHeight }">
    <XhClipboardRoot v-if="text" class="x-json-block__copy" :value="text">
      <XhClipboardTrigger :aria-label="t('common.actions.copy')">
        <XhClipboardIndicator />
        <XhClipboardIndicator copied />
      </XhClipboardTrigger>
    </XhClipboardRoot>

    <XhJsonViewerRoot
      v-if="tree"
      class="x-json-block__tree"
      :value="tree"
      :default-expanded-depth="defaultExpandedDepth"
      :max-string-length="maxStringLength"
      :max-items="maxItems"
      size="sm"
    />
    <pre v-else class="x-json-block__text">{{ text || '-' }}</pre>
  </div>
</template>

<style scoped>
.x-json-block {
  position: relative;
}

/* 复制钮压在右上角，避开树的首行缩进 */
.x-json-block__copy {
  position: absolute;
  inset-block-start: 4px;
  inset-inline-end: 4px;
  z-index: 1;
}

.x-json-block__tree {
  --xh-json-viewer-max-h: var(--x-json-block-max-h);
}

.x-json-block__text {
  margin: 0;
  max-block-size: var(--x-json-block-max-h);
  overflow: auto;
  padding: 12px;
  padding-inline-end: 36px;
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-subtle);
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
