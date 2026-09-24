<script setup lang="ts">
import type { DataScopeDraft } from './data-scope'
import type { ApiId, DepartmentTreeNodeDto } from '@/api'
import { XhButton, XhRadioGroupItem, XhRadioGroupItemText, XhRadioGroupRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { DataPermissionScope } from '@/api'
import { DATA_SCOPE_OPTIONS } from '@/constants'
import { XTree } from '~/components'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import { buildParentMap, findCoveredDepartments, setIncludeChildren, syncPickedDepartments } from './data-scope'

/**
 * 数据范围编辑：先选档位；自定义时在部门树里勾部门，已选部门逐项设「含下级」。
 * 角色与成员共用——成员多一个「跟随角色」，全局角色不能自定义（部门是租户自己的数据）
 */
defineOptions({ name: 'DataScopeEditor' })

const props = withDefaults(defineProps<{
  /** 自定义档位的候选部门；不允许自定义时不必传 */
  departmentTree?: DepartmentTreeNodeDto[]
  /** 提供「跟随角色」档位（成员覆盖用） */
  allowInherit?: boolean
  /** 是否允许自定义部门 */
  allowCustom?: boolean
  readonly?: boolean
}>(), {
  departmentTree: () => [],
  allowInherit: false,
  allowCustom: true,
  readonly: false,
})

const draft = defineModel<DataScopeDraft>({ required: true })

const { t } = useI18n()
const scopeOptions = useEnumOptions('DataPermissionScope', DATA_SCOPE_OPTIONS)

/** 单选组只认字符串：「跟随角色」用哨兵值，提交时还原为 null */
const INHERIT = '__inherit__'

const LEVEL_ORDER = [
  DataPermissionScope.All,
  DataPermissionScope.DepartmentAndChildren,
  DataPermissionScope.DepartmentOnly,
  DataPermissionScope.SelfOnly,
  DataPermissionScope.Custom,
] as const

const levels = computed(() => {
  const hints: Record<DataPermissionScope, string> = {
    [DataPermissionScope.All]: t('identity.data_scope.hint_all'),
    [DataPermissionScope.DepartmentAndChildren]: t('identity.data_scope.hint_department_and_children'),
    [DataPermissionScope.DepartmentOnly]: t('identity.data_scope.hint_department_only'),
    [DataPermissionScope.SelfOnly]: t('identity.data_scope.hint_self_only'),
    [DataPermissionScope.Custom]: props.allowCustom ? t('identity.data_scope.hint_custom') : t('identity.data_scope.hint_custom_global'),
  }
  const list = LEVEL_ORDER.map(scope => ({
    value: scope as string,
    label: getOptionLabel(scopeOptions.value, scope),
    hint: hints[scope],
    disabled: scope === DataPermissionScope.Custom && !props.allowCustom,
  }))
  return props.allowInherit
    ? [{ value: INHERIT, label: t('identity.data_scope.inherit'), hint: t('identity.data_scope.hint_inherit'), disabled: false }, ...list]
    : list
})

const selectedLevel = computed(() => draft.value.dataScope ?? INHERIT)

function onLevelChange(value: string | null) {
  if (value == null || props.readonly)
    return
  draft.value = { ...draft.value, dataScope: value === INHERIT ? null : value as DataPermissionScope }
}

const isCustom = computed(() => draft.value.dataScope === DataPermissionScope.Custom)

interface DepartmentOption {
  value: string
  label: string
  children?: DepartmentOption[]
}

function toOptions(nodes: readonly DepartmentTreeNodeDto[]): DepartmentOption[] {
  return nodes.map(node => ({
    value: node.basicId,
    label: node.departmentName,
    ...(node.children?.length ? { children: toOptions(node.children) } : {}),
  }))
}

const treeOptions = computed(() => toOptions(props.departmentTree))
const parentMap = computed(() => buildParentMap(props.departmentTree))
const nameById = computed(() => {
  const names = new Map<ApiId, string>()
  const walk = (nodes: readonly DepartmentTreeNodeDto[]) => {
    for (const node of nodes) {
      names.set(node.basicId, node.departmentName)
      if (node.children?.length)
        walk(node.children)
    }
  }
  walk(props.departmentTree)
  return names
})

const pickedKeys = computed(() => draft.value.departments.map(item => item.departmentId))
const covered = computed(() => findCoveredDepartments(draft.value.departments, parentMap.value))

/** 默认展开第一层，便于直接勾到常用部门 */
const expandedKeys = ref<string[]>([])
watch(() => props.departmentTree, (tree) => {
  expandedKeys.value = tree.map(node => node.basicId)
}, { immediate: true })

function departmentName(departmentId: ApiId) {
  return nameById.value.get(departmentId) ?? departmentId
}

function onPick(keys: string[]) {
  if (props.readonly)
    return
  draft.value = { ...draft.value, departments: syncPickedDepartments(draft.value.departments, keys) }
}

function onIncludeChildren(departmentId: ApiId, includeChildren: boolean) {
  if (props.readonly)
    return
  draft.value = { ...draft.value, departments: setIncludeChildren(draft.value.departments, departmentId, includeChildren) }
}

function remove(departmentId: ApiId) {
  onPick(pickedKeys.value.filter(id => id !== departmentId))
}
</script>

<template>
  <div class="data-scope-editor">
    <XhRadioGroupRoot
      :value="selectedLevel"
      :disabled="readonly"
      :label="t('identity.data_scope.level_label')"
      orientation="vertical"
      class="data-scope-levels"
      @update:value="onLevelChange"
    >
      <XhRadioGroupItem
        v-for="level in levels"
        :key="level.value"
        :value="level.value"
        :disabled="level.disabled"
        class="data-scope-level"
      >
        <XhRadioGroupItemText>
          <span class="data-scope-level__label">{{ level.label }}</span>
          <span class="data-scope-level__hint">{{ level.hint }}</span>
        </XhRadioGroupItemText>
      </XhRadioGroupItem>
    </XhRadioGroupRoot>

    <section v-if="isCustom" class="data-scope-custom" :aria-label="t('identity.data_scope.custom_section')">
      <div class="data-scope-custom__tree">
        <p v-if="treeOptions.length === 0" class="data-scope-custom__empty">
          {{ t('identity.data_scope.tree_empty') }}
        </p>
        <XTree
          v-else
          v-model:expanded-keys="expandedKeys"
          :data="treeOptions"
          multiple
          :selected-keys="pickedKeys"
          @update:selected-keys="onPick"
        />
      </div>
      <div class="data-scope-custom__picked">
        <p class="data-scope-custom__title">
          {{ t('identity.data_scope.picked', { count: draft.departments.length }) }}
        </p>
        <p v-if="draft.departments.length === 0" class="data-scope-custom__empty">
          {{ t('identity.data_scope.picked_empty') }}
        </p>
        <ul v-else class="data-scope-picked">
          <li v-for="item in draft.departments" :key="item.departmentId" class="data-scope-picked__row">
            <div class="data-scope-picked__name">
              <span class="data-scope-picked__text">{{ departmentName(item.departmentId) }}</span>
              <span v-if="covered.has(item.departmentId)" class="data-scope-picked__note">
                {{ t('identity.data_scope.covered_by', { name: departmentName(covered.get(item.departmentId)!) }) }}
              </span>
            </div>
            <XhSwitch
              :checked="item.includeChildren"
              :disabled="readonly"
              @update:checked="(value: boolean) => onIncludeChildren(item.departmentId, value)"
            >
              {{ t('identity.data_scope.include_children') }}
            </XhSwitch>
            <XhButton
              variant="ghost"
              size="sm"
              tone="danger"
              :disabled="readonly"
              :aria-label="t('identity.data_scope.remove_named', { name: departmentName(item.departmentId) })"
              @click="remove(item.departmentId)"
            >
              {{ t('identity.data_scope.remove') }}
            </XhButton>
          </li>
        </ul>
      </div>
    </section>
  </div>
</template>

<style scoped>
.data-scope-editor {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-4);
  min-block-size: 0;
}

.data-scope-levels {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.data-scope-level :deep([data-part='item-text']) {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
}

.data-scope-level__label {
  color: var(--xh-fg-default);
  font-size: var(--xh-text-body-size);
}

.data-scope-level__hint {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

/* 树与已选并排，窄屏自动叠成上下两块 */
.data-scope-custom {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
  gap: var(--xh-space-3);
  min-block-size: 0;
}

.data-scope-custom__tree,
.data-scope-custom__picked {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  min-block-size: 0;
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.data-scope-custom__title {
  margin: 0;
  color: var(--xh-fg-default);
  font-size: var(--xh-text-secondary-size);
}

.data-scope-custom__empty {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-secondary-size);
}

.data-scope-picked {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.data-scope-picked__row {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}

.data-scope-picked__name {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-inline-size: 0;
}

.data-scope-picked__text {
  overflow: hidden;
  color: var(--xh-fg-default);
  font-size: var(--xh-text-secondary-size);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.data-scope-picked__note {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}
</style>
