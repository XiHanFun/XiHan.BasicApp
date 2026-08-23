<script setup lang="ts">
import {
  XhPaginationEllipsis,
  XhPaginationItem,
  XhPaginationNextTrigger,
  XhPaginationPrevTrigger,
  XhPaginationRoot,
  XhSelectRoot,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaPagination' })

const props = withDefaults(defineProps<{
  /** 总条数 */
  total: number
  /** 当前页 */
  page: number
  /** 每页数量 */
  pageSize: number
  /** 每页可选数量；只有一个选项时不出条数选择器 */
  pageSizes?: number[]
  /** 紧凑档：窄屏用，少显几个页码且藏掉条数选择器 */
  compact?: boolean
}>(), {
  pageSizes: () => [10, 20, 50, 100],
  compact: false,
})

const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
}>()

const { t } = useI18n()

// 首尾各恒显 1 页，siblingCount 决定当前页两侧各显几页
const siblingCount = computed(() => (props.compact ? 1 : 2))

const sizeOptions = computed(() =>
  props.pageSizes.map(size => ({ value: String(size), label: t('component.pagination.per_page', { size }) })),
)

const sizeValue = computed(() => [String(props.pageSize)])

function onSizeChange(value: string[]): void {
  const next = Number(value[0])
  if (Number.isFinite(next) && next > 0 && next !== props.pageSize) {
    emit('update:pageSize', next)
  }
}
</script>

<template>
  <div class="schema-pagination">
    <XhPaginationRoot
      v-slot="{ pages }"
      :count="total"
      :page="page"
      :page-size="pageSize"
      :sibling-count="siblingCount"
      :size="compact ? 'sm' : 'md'"
      @update:page="(value: number) => emit('update:page', value)"
    >
      <XhPaginationPrevTrigger>
        <Icon icon="lucide:chevron-left" width="14" height="14" />
      </XhPaginationPrevTrigger>
      <template v-for="(item, index) in pages" :key="`${item}-${index}`">
        <XhPaginationItem v-if="item !== 'ellipsis'" :value="item">
          {{ item }}
        </XhPaginationItem>
        <XhPaginationEllipsis v-else>
          …
        </XhPaginationEllipsis>
      </template>
      <XhPaginationNextTrigger>
        <Icon icon="lucide:chevron-right" width="14" height="14" />
      </XhPaginationNextTrigger>
    </XhPaginationRoot>

    <XhSelectRoot
      v-if="!compact && pageSizes.length > 1"
      class="schema-pagination__size"
      :collection="sizeOptions"
      :value="sizeValue"
      size="sm"
      @update:value="onSizeChange"
    />
  </div>
</template>

<style scoped>
.schema-pagination {
  display: flex;
  gap: 8px;
  align-items: center;
}

/* 每页数量：外框定宽，触发器跟着收进来。
   触发器有自己的固有宽度，不收就会顶出外框、给分页条挤出一条横向滚动条 */
.schema-pagination__size {
  inline-size: 110px;
}

.schema-pagination__size :deep([data-scope='select'][data-part='control']) {
  inline-size: 100%;
  min-inline-size: 0;
}
</style>
