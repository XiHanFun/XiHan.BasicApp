<script setup lang="ts" generic="T extends PermissionGrantItem">
import type { PermissionGrantItem } from './permission-grant-panel'
import { NEmpty, NInput, NSpin, NTag } from 'naive-ui'
import { computed, ref } from 'vue'

defineOptions({ name: 'XPermissionGrantPanel' })

const props = defineProps<{
  /** 全量权限目录（或已授予集合，取决于调用方语义） */
  items: T[]
  loading?: boolean
  /** 已授予数量，显示在搜索框右侧 */
  grantedCount?: number
  searchPlaceholder?: string
  grantedCountLabel?: string
  emptyDescription?: string
  otherGroupLabel?: string
}>()

const keyword = ref('')

function reset() {
  keyword.value = ''
}

defineExpose({ reset })

const filtered = computed(() => {
  const kw = keyword.value.trim().toLowerCase()
  if (!kw) {
    return props.items
  }
  return props.items.filter(item =>
    (item.permissionName ?? '').toLowerCase().includes(kw)
    || (item.permissionCode ?? '').toLowerCase().includes(kw),
  )
})

/** 取权限码的资源段作为分组键：saas:{resource}:{action} → resource */
function resourceKey(code: string): string {
  const parts = (code ?? '').split(':')
  return parts.length >= 3 ? parts[1]! : (parts[0] ?? '')
}

/** 一组权限名的公共前缀，作为功能块显示名 */
function commonPrefix(names: string[]): string {
  if (names.length === 0) {
    return ''
  }
  let prefix = names[0]!
  for (const name of names) {
    let i = 0
    while (i < prefix.length && i < name.length && prefix[i] === name[i]) {
      i++
    }
    prefix = prefix.slice(0, i)
    if (!prefix) {
      break
    }
  }
  return prefix
}

/** 按资源分组：组码优先用后端 groupCode，缺省回退资源段推导 */
const groups = computed(() => {
  const map = new Map<string, T[]>()
  for (const item of filtered.value) {
    const key = item.groupCode
      || item.resourceName
      || resourceKey(item.permissionCode)
      || item.moduleCode
      || (props.otherGroupLabel ?? 'other')
    const list = map.get(key)
    if (list) {
      list.push(item)
    }
    else {
      map.set(key, [item])
    }
  }
  return [...map.entries()].map(([key, groupItems]) => ({
    key,
    name: groupItems[0]?.groupName
      || commonPrefix(groupItems.map(groupItem => groupItem.permissionName))
      || key,
    items: groupItems,
  }))
})
</script>

<template>
  <div class="xh-perm-panel">
    <div class="xh-perm-panel__toolbar">
      <NInput v-model:value="keyword" clearable :placeholder="searchPlaceholder" style="width: 240px" />
      <NTag v-if="grantedCountLabel" :bordered="false" round type="success">
        {{ grantedCountLabel }}
      </NTag>
      <!-- 额外工具位：如版本权限的「授予」入口 -->
      <slot name="toolbar" />
    </div>

    <NSpin :show="Boolean(loading)">
      <NEmpty v-if="groups.length === 0 && !loading" class="xh-perm-panel__empty" :description="emptyDescription" />
      <div v-else class="xh-perm-panel__groups">
        <section v-for="group in groups" :key="group.key" class="xh-perm-panel__group">
          <div class="xh-perm-panel__group-head">
            <span>{{ group.name }}</span>
            <span class="xh-perm-panel__group-count">{{ group.items.length }}</span>
          </div>
          <div class="xh-perm-panel__list">
            <div
              v-for="item in group.items"
              :key="String(item.basicId)"
              class="xh-perm-panel__item"
            >
              <span class="xh-perm-panel__text">
                <span class="xh-perm-panel__name">{{ item.permissionName }}</span>
                <span class="xh-perm-panel__code">{{ item.permissionCode }}</span>
              </span>
              <!-- 每项右侧控件由调用方决定：复选框 / 授予拒绝 / 启停撤销 -->
              <span class="xh-perm-panel__action">
                <slot name="action" :item="item" />
              </span>
            </div>
          </div>
        </section>
      </div>
    </NSpin>
  </div>
</template>

<style scoped>
.xh-perm-panel__toolbar {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.xh-perm-panel__empty {
  padding: 48px 0;
}

.xh-perm-panel__groups {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.xh-perm-panel__group {
  border: 1px solid var(--n-border-color, rgb(239 239 245));
  border-radius: 8px;
  overflow: hidden;
}

.xh-perm-panel__group-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background-color: var(--n-color-modal, rgb(250 250 252));
  font-size: 13px;
  font-weight: 500;
}

.xh-perm-panel__group-count {
  font-size: 11px;
  opacity: 0.55;
}

.xh-perm-panel__list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
}

.xh-perm-panel__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 12px;
  border-top: 1px solid var(--n-border-color, rgb(239 239 245));
  min-width: 0;
}

.xh-perm-panel__text {
  display: flex;
  flex-direction: column;
  min-width: 0;
  line-height: 1.4;
}

.xh-perm-panel__name {
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.xh-perm-panel__code {
  font-size: 11px;
  opacity: 0.55;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.xh-perm-panel__action {
  flex: none;
  display: flex;
  align-items: center;
  gap: 6px;
}
</style>
