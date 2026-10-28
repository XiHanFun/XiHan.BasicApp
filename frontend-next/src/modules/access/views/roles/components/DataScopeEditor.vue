<script setup lang="ts" generic="TDraft extends DataScopeDraft | MemberDataScopeDraft">
import type { DataScopeDraft, DataScopeValue, DepartmentNode, MemberDataScopeDraft } from '../data-scope'
import { XhButton, XhRadioGroupItem, XhRadioGroupItemDescription, XhRadioGroupItemText, XhRadioGroupLabel, XhRadioGroupRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XTree } from '~/components'
import { buildNameMap, buildParentMap, DATA_SCOPE_LEVELS, findCoveredDepartments, setIncludeChildren, syncPickedDepartments } from '../data-scope'

/**
 * 数据范围编辑（4.x 的 DataScopeEditor 搬入，按 6.0 的部门树与档位改型）：先选档位；自定义时在部门树里勾部门，已选部门逐项设「含下级」。
 * 角色与成员共用——成员多一个「跟随角色」档（`allowInherit`，档位为空即不覆盖，与 4.x 相同）。
 */
defineOptions({ name: 'AccessRoleDataScopeEditor' })

const props = withDefaults(defineProps<{
  /** 自定义档位的候选部门（本租户的部门树） */
  departmentTree?: DepartmentNode[]
  /** 提供「跟随角色」档位（成员覆盖用） */
  allowInherit?: boolean
  readonly?: boolean
}>(), {
  departmentTree: () => [],
  allowInherit: false,
  readonly: false,
})

const draft = defineModel<TDraft>({ required: true })

/** 单选组只认字符串：「跟随角色」用哨兵值，提交时还原为 null */
const INHERIT = '__inherit__'

const { t } = useI18n()

// 档位是契约字面量，文案键静态写出以便 i18n 门禁核对
function levelLabel(scope: DataScopeValue): string {
  switch (scope) {
    case 'All': return t('access.roles.data_scope.all')
    case 'DepartmentAndChildren': return t('access.roles.data_scope.department_and_children')
    case 'DepartmentOnly': return t('access.roles.data_scope.department_only')
    case 'Custom': return t('access.roles.data_scope.custom')
    default: return t('access.roles.data_scope.self_only')
  }
}

function levelHint(scope: DataScopeValue): string {
  switch (scope) {
    case 'All': return t('access.roles.scope_editor.hint_all')
    case 'DepartmentAndChildren': return t('access.roles.scope_editor.hint_department_and_children')
    case 'DepartmentOnly': return t('access.roles.scope_editor.hint_department_only')
    case 'Custom': return t('access.roles.scope_editor.hint_custom')
    default: return t('access.roles.scope_editor.hint_self_only')
  }
}

const levels = computed(() => {
  const list = DATA_SCOPE_LEVELS.map(scope => ({ value: scope as string, label: levelLabel(scope), hint: levelHint(scope) }))
  return props.allowInherit
    ? [{ value: INHERIT, label: t('access.roles.scope_editor.inherit'), hint: t('access.roles.scope_editor.hint_inherit') }, ...list]
    : list
})

const selectedLevel = computed(() => draft.value.dataScope ?? INHERIT)

function onLevelChange(value: null | string) {
  if (value === null || props.readonly)
    return
  draft.value = { ...draft.value, dataScope: value === INHERIT ? null : value as DataScopeValue } as TDraft
}

const isCustom = computed(() => draft.value.dataScope === 'Custom')

interface DepartmentOption {
  value: string
  label: string
  children?: DepartmentOption[]
}

function toOptions(nodes: readonly DepartmentNode[]): DepartmentOption[] {
  return nodes.map(node => ({
    value: node.department.id,
    label: node.department.name,
    ...(node.children.length > 0 ? { children: toOptions(node.children) } : {}),
  }))
}

const treeOptions = computed(() => toOptions(props.departmentTree))
const parentMap = computed(() => buildParentMap(props.departmentTree))
const nameById = computed(() => buildNameMap(props.departmentTree))

const pickedKeys = computed(() => draft.value.departments.map(item => item.departmentId))
const covered = computed(() => findCoveredDepartments(draft.value.departments, parentMap.value))

/** 默认展开第一层，便于直接勾到常用部门 */
const expandedKeys = ref<string[]>([])
watch(() => props.departmentTree, (tree) => {
  expandedKeys.value = tree.map(node => node.department.id)
}, { immediate: true })

function departmentName(departmentId: string): string {
  return nameById.value.get(departmentId) ?? departmentId
}

function onPick(keys: string[]) {
  if (props.readonly)
    return
  draft.value = { ...draft.value, departments: syncPickedDepartments(draft.value.departments, keys) } as TDraft
}

function onIncludeChildren(departmentId: string, includeChildren: boolean) {
  if (props.readonly)
    return
  draft.value = { ...draft.value, departments: setIncludeChildren(draft.value.departments, departmentId, includeChildren) } as TDraft
}

function remove(departmentId: string) {
  onPick(pickedKeys.value.filter(id => id !== departmentId))
}
</script>

<template>
  <div class="data-scope-editor">
    <!-- 逐项手写条目时 label 属性不渲染，组名要写成 label 部件 -->
    <XhRadioGroupRoot
      :value="selectedLevel"
      :disabled="readonly"
      orientation="vertical"
      class="data-scope-levels"
      @update:value="onLevelChange"
    >
      <XhRadioGroupLabel>{{ t('access.roles.scope_editor.level_label') }}</XhRadioGroupLabel>
      <XhRadioGroupItem v-for="level in levels" :key="level.value" :value="level.value">
        <XhRadioGroupItemText>{{ level.label }}</XhRadioGroupItemText>
        <XhRadioGroupItemDescription>{{ level.hint }}</XhRadioGroupItemDescription>
      </XhRadioGroupItem>
    </XhRadioGroupRoot>

    <section v-if="isCustom" class="data-scope-custom" :aria-label="t('access.roles.scope_editor.custom_section')">
      <div class="data-scope-custom__tree">
        <p v-if="treeOptions.length === 0" class="data-scope-custom__empty">
          {{ t('access.roles.scope_editor.tree_empty') }}
        </p>
        <XTree
          v-else
          v-model:expanded-keys="expandedKeys"
          :data="treeOptions"
          :aria-label="t('access.roles.scope_editor.custom_section')"
          multiple
          :selected-keys="pickedKeys"
          @update:selected-keys="onPick"
        />
      </div>
      <div class="data-scope-custom__picked">
        <p class="data-scope-custom__title">
          {{ t('access.roles.scope_editor.picked', { count: draft.departments.length }) }}
        </p>
        <p v-if="draft.departments.length === 0" class="data-scope-custom__empty">
          {{ t('access.roles.scope_editor.picked_empty') }}
        </p>
        <ul v-else class="data-scope-picked">
          <li v-for="item in draft.departments" :key="item.departmentId" class="data-scope-picked__row">
            <div class="data-scope-picked__name">
              <span class="data-scope-picked__text">{{ departmentName(item.departmentId) }}</span>
              <span v-if="covered.has(item.departmentId)" class="data-scope-picked__note">
                {{ t('access.roles.scope_editor.covered_by', { name: departmentName(covered.get(item.departmentId)!) }) }}
              </span>
            </div>
            <XhSwitch
              :checked="item.includeChildren"
              :disabled="readonly"
              @update:checked="(value: boolean) => onIncludeChildren(item.departmentId, value)"
            >
              {{ t('access.roles.scope_editor.include_children') }}
            </XhSwitch>
            <XhButton
              variant="ghost"
              size="sm"
              tone="danger"
              :disabled="readonly"
              :aria-label="t('access.roles.scope_editor.remove_named', { name: departmentName(item.departmentId) })"
              @click="remove(item.departmentId)"
            >
              {{ t('access.roles.scope_editor.remove') }}
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
