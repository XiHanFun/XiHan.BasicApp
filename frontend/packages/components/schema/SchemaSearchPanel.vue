<script setup lang="ts" generic="TRow extends object">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { ListFieldSchema } from './types'
import { NButton, NDatePicker, NIcon, NInput, NSelect } from 'naive-ui'
import { computed, ref } from 'vue'
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

const expanded = ref(false)

const hasAdvanced = computed(() => props.advancedFields.length > 0)

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
    <!-- 常用条件 + 操作按钮（按钮永远固定第一行右上） -->
    <div class="xh-search__bar">
      <div class="xh-search__fields">
        <div
          v-for="field in commonFields"
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

      <!-- 操作按钮：align-self 顶对齐 + margin-left auto，常用条件换行时仍固定第一行右上 -->
      <div class="xh-search__actions">
        <NButton size="small" type="primary" @click="emit('search')">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="emit('reset')">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
        <!-- 搜索设置（排序/固定）插槽 -->
        <slot name="settings" />
        <NButton v-if="hasAdvanced" size="small" quaternary @click="toggleExpand">
          <template #icon>
            <NIcon><Icon :icon="expanded ? 'lucide:chevron-up' : 'lucide:sliders-horizontal'" /></NIcon>
          </template>
          {{ expanded ? '隐藏条件' : '高级搜索' }}
        </NButton>
      </div>
    </div>

    <!-- 高级条件：上层浮层滑入，不占文档流（不推动下方按钮/列表） -->
    <Transition name="xh-search-expand">
      <div v-if="expanded && hasAdvanced" class="xh-search__advanced">
        <div
          v-for="field in advancedFields"
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
.xh-search {
  position: relative;
  padding: 10px 12px;
  border-radius: var(--n-border-radius, 6px);
  background: var(--n-card-color, transparent);
}

/* 顶层：常用条件块 + 按钮块。按钮块顶对齐，常用条件换行时按钮固定第一行右上 */
.xh-search__bar {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

/* 常用条件区：内部 flex-wrap，挤占左侧 */
.xh-search__fields {
  display: flex;
  flex-wrap: wrap;
  flex: 1 1 auto;
  gap: 10px 12px;
  min-width: 0;
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
  color: var(--n-text-color-3, inherit);
}

/* 操作按钮：顶对齐 + 推到最右，永远在第一行 */
.xh-search__actions {
  display: flex;
  flex-shrink: 0;
  gap: 8px;
  margin-left: auto;
  /* 与控件底边对齐：标题约 17px 高 + 2px gap */
  padding-top: 19px;
}

/* 高级条件区：绝对定位浮层，叠在内容上层，不占文档流（不推动按钮/列表） */
.xh-search__advanced {
  position: absolute;
  top: 100%;
  right: 0;
  left: 0;
  z-index: 20;
  display: flex;
  flex-wrap: wrap;
  gap: 10px 12px;
  margin-top: 4px;
  padding: 14px 12px;
  border-radius: var(--n-border-radius, 6px);
  background: var(--n-card-color, #fff);
  box-shadow: 0 6px 16px rgb(0 0 0 / 0.12);
}

/* 展开/收起动画 */
.xh-search-expand-enter-active,
.xh-search-expand-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.xh-search-expand-enter-from,
.xh-search-expand-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* 移动端：常用条件项占满整行，按钮区独占一行铺满 */
@media (max-width: 767px) {
  .xh-search__bar {
    flex-direction: column;
  }

  .xh-search__item {
    width: 100%;
  }

  .xh-search__actions {
    width: 100%;
    margin-left: 0;
    padding-top: 0;
  }

  .xh-search__actions :deep(.n-button) {
    flex: 1 1 auto;
  }

  .xh-search__advanced {
    grid-template-columns: 1fr;
  }
}
</style>
