<script setup lang="ts" generic="TRow extends object">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { ListFieldSchema } from './types'
import { NButton, NDatePicker, NIcon, NInput, NSelect, NTooltip, useThemeVars } from 'naive-ui'
import { computed, ref } from 'vue'
import { useIsMobile } from '~/composables'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaSearchPanel' })

const props = defineProps<{
  /** 常用搜索字段（始终展示） */
  commonFields: ListFieldSchema<TRow>[]
  /** 高级搜索字段（展开后展示，内部滑入） */
  advancedFields: ListFieldSchema<TRow>[]
  /** 过滤条件模型（来自 useSchemaTable.filters） */
  model: Record<string, unknown>
}>()

const emit = defineEmits<{
  search: []
  reset: []
}>()

/** 主题变量：高级浮层背景/边框/文字随明暗主题切换（避免硬编码白底导致暗色模式露白） */
const themeVars = useThemeVars()

/** 高级条件展开状态 */
const expanded = ref(false)

/** 小屏断点（max-width:767px，与组件内媒体查询一致） */
const { isMobile } = useIsMobile()

// 小屏：常用区清空，所有搜索条件收入高级区（仅保留操作按钮行，点「高级搜索」展开）
const effectiveCommonFields = computed(() => isMobile.value ? [] : props.commonFields)
const effectiveAdvancedFields = computed(() =>
  isMobile.value ? [...props.commonFields, ...props.advancedFields] : props.advancedFields,
)

const hasAdvanced = computed(() => effectiveAdvancedFields.value.length > 0)

function toggleExpand() {
  expanded.value = !expanded.value
}

/** 选项断言：业务 SelectOption 与 Naive 选项结构兼容 */
function asOptions(field: ListFieldSchema<TRow>): SelectMixedOption[] {
  return (field.options as unknown as SelectMixedOption[] | undefined) ?? []
}

function isSelect(field: ListFieldSchema<TRow>): boolean {
  return (field.dataType === 'enum' || field.dataType === 'tag' || field.dataType === 'boolean') && !!field.options
}

function isDate(field: ListFieldSchema<TRow>): boolean {
  return field.dataType === 'date' || field.dataType === 'datetime'
}
</script>

<template>
  <div class="xh-search">
    <!-- 常用条件 + 操作按钮：同一 flex-wrap 流，按钮组随条件自适应流动并在所在行靠右 -->
    <div class="xh-search__bar">
      <div
        v-for="field in effectiveCommonFields"
        :key="field.key"
        class="xh-search__item"
      >
        <span class="xh-search__label">{{ field.title }}</span>
        <NSelect
          v-if="isSelect(field)"
          v-model:value="(model[field.key] as string)"
          clearable
          size="small"
          :options="asOptions(field)"
          :placeholder="field.searchPlaceholder ?? field.title"
        />
        <NDatePicker
          v-else-if="isDate(field)"
          v-model:value="(model[field.key] as number)"
          clearable
          size="small"
          class="w-full"
          :type="field.dataType === 'datetime' ? 'datetime' : 'date'"
          :placeholder="field.searchPlaceholder ?? field.title"
        />
        <NInput
          v-else
          v-model:value="(model[field.key] as string)"
          clearable
          size="small"
          :placeholder="field.searchPlaceholder ?? field.title"
          @keyup.enter="emit('search')"
        />
      </div>

      <!-- 操作按钮：纯图标 + tooltip，作为流的最后一项，margin-left:auto 推到所在行右侧 -->
      <div class="xh-search__actions">
        <NTooltip>
          <template #trigger>
            <NButton circle size="small" type="primary" aria-label="查询" @click="emit('search')">
              <template #icon>
                <NIcon><Icon icon="lucide:search" /></NIcon>
              </template>
            </NButton>
          </template>
          查询
        </NTooltip>
        <NTooltip>
          <template #trigger>
            <NButton circle size="small" aria-label="重置" @click="emit('reset')">
              <template #icon>
                <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
              </template>
            </NButton>
          </template>
          重置
        </NTooltip>
        <!-- 搜索设置（排序/固定）插槽 -->
        <slot name="settings" />
        <NTooltip v-if="hasAdvanced">
          <template #trigger>
            <NButton circle size="small" quaternary :aria-label="expanded ? '隐藏条件' : '高级搜索'" @click="toggleExpand">
              <template #icon>
                <NIcon><Icon :icon="expanded ? 'lucide:chevron-up' : 'lucide:sliders-horizontal'" /></NIcon>
              </template>
            </NButton>
          </template>
          {{ expanded ? '隐藏条件' : '高级搜索' }}
        </NTooltip>
      </div>
    </div>

    <!-- 高级条件：上层浮层滑入，不占文档流（不推动下方按钮/列表） -->
    <Transition name="xh-search-expand">
      <div v-if="expanded && hasAdvanced" class="xh-search__advanced">
        <div
          v-for="field in effectiveAdvancedFields"
          :key="field.key"
          class="xh-search__item"
        >
          <span class="xh-search__label">{{ field.title }}</span>
          <NSelect
            v-if="isSelect(field)"
            v-model:value="(model[field.key] as string)"
            clearable
            size="small"
            :options="asOptions(field)"
            :placeholder="field.searchPlaceholder ?? field.title"
          />
          <NDatePicker
            v-else-if="isDate(field)"
            v-model:value="(model[field.key] as number)"
            clearable
            size="small"
            class="w-full"
            :type="field.dataType === 'datetime' ? 'datetime' : 'date'"
            :placeholder="field.searchPlaceholder ?? field.title"
          />
          <NInput
            v-else
            v-model:value="(model[field.key] as string)"
            clearable
            size="small"
            :placeholder="field.searchPlaceholder ?? field.title"
            @keyup.enter="emit('search')"
          />
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* 搜索区作为高级浮层的定位上下文（外层由 NCard 提供卡片容器与内边距） */
.xh-search {
  position: relative;
}

/* 常用条件 + 按钮组：同一 flex-wrap 流，底对齐（按钮与控件底边平齐） */
.xh-search__bar {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 12px;
  align-items: flex-end;
}

/* 单个搜索项：上下布局（标题在上、控件在下），常用/高级统一宽度 */
.xh-search__item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  width: 180px;
}

/* 搜索标题：小字号、紧靠控件 */
.xh-search__label {
  font-size: 12px;
  line-height: 1.4;
  color: v-bind('themeVars.textColor3');
}

/* 操作按钮：作为流最后一项，margin-left:auto 推到所在行右侧，随条件自适应流动 */
.xh-search__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-left: auto;
}

/* 高级条件区：绝对定位浮层，叠在内容上层，不占文档流（不推动按钮/列表）。
   left/right 用负值抵消 NCard 内容内边距（16px），使浮层铺满卡片宽度；
   自身 padding 再以同样 16px 内缩，让高级字段左边缘与常用字段严格对齐。 */
.xh-search__advanced {
  position: absolute;
  top: 100%;
  right: -16px;
  left: -16px;
  z-index: 20;
  display: flex;
  flex-wrap: wrap;
  gap: 10px 12px;
  margin-top: 12px;
  padding: 14px 16px;
  border: 1px solid v-bind('themeVars.borderColor');
  border-radius: v-bind('themeVars.borderRadius');
  background: v-bind('themeVars.cardColor');
  box-shadow: v-bind('themeVars.boxShadow2');
  /* 限制浮层高度并内部滚动：字段过多时也不会撑出视口、盖住下方列表 */
  max-height: calc(100vh - 160px);
  overflow-y: auto;
  overscroll-behavior: contain;
  -webkit-overflow-scrolling: touch;
}

/* 展开/收起动画 */
.xh-search-expand-enter-active,
.xh-search-expand-leave-active {
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.xh-search-expand-enter-from,
.xh-search-expand-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* 移动端：常用条件项占满整行；按钮组仍为右对齐的圆形图标按钮（不拉伸成整行） */
@media (max-width: 767px) {
  .xh-search__item {
    width: 100%;
  }

  /* 按钮组保持紧凑右对齐：margin-left:auto 推到右侧，按钮维持圆形不被拉伸 */
  .xh-search__actions {
    margin-left: auto;
  }

  /* 移动端字段纵向堆叠极易过高：浮层最多占 60% 屏高并内部滚动，下方列表仍可见 */
  .xh-search__advanced {
    max-height: 60vh;
  }
}
</style>
