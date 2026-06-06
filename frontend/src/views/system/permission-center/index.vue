<script setup lang="ts">
import type { ApiId, PermissionListItemDto, RolePermissionListItemDto, RoleSelectItemDto } from '@/api'
import { NCheckbox, NEmpty, NIcon, NInput, NSelect, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, onMounted, ref, watch } from 'vue'
import { createPageRequest, PermissionAction, permissionApi, roleApi, rolePermissionApi } from '@/api'
import { Icon } from '~/iconify'

defineOptions({ name: 'PermissionCenterPage' })

const message = useMessage()

interface ModuleNav {
  key: string
  label: string
  icon: string
  desc: string
  ready: boolean
}

const modules: ModuleNav[] = [
  { key: 'role', label: '角色授权', icon: 'lucide:shield-check', desc: '权限分配 / 菜单授权 / 数据范围', ready: true },
  { key: 'user', label: '用户直授', icon: 'lucide:user-cog', desc: '直接授角色与权限', ready: false },
  { key: 'fls', label: '字段权限', icon: 'lucide:eye-off', desc: '字段级脱敏与可编辑', ready: false },
  { key: 'request', label: '授权申请', icon: 'lucide:file-check-2', desc: '申请审批与权限委托', ready: false },
]
const activeModule = ref('role')
const activeModuleMeta = computed(() => modules.find(m => m.key === activeModule.value) ?? modules[0]!)

// ==================== 角色授权 ====================

const roles = ref<RoleSelectItemDto[]>([])
const selectedRoleId = ref<ApiId | null>(null)
const roleOptions = computed(() =>
  roles.value.map(role => ({ label: `${role.roleName}（${role.roleCode}）`, value: role.basicId })),
)

const catalog = ref<PermissionListItemDto[]>([])
const grants = ref<RolePermissionListItemDto[]>([])
const loading = ref(false)
const keyword = ref('')
const togglingId = ref<ApiId | null>(null)

/** permissionId → 授权记录（用于收权时取记录主键） */
const grantByPermissionId = computed(() => {
  const map = new Map<ApiId, RolePermissionListItemDto>()
  for (const grant of grants.value) {
    map.set(grant.permissionId, grant)
  }
  return map
})

const grantedCount = computed(() => grants.value.length)

const filteredCatalog = computed(() => {
  const kw = keyword.value.trim().toLowerCase()
  if (!kw) {
    return catalog.value
  }
  return catalog.value.filter(p =>
    p.permissionName.toLowerCase().includes(kw) || p.permissionCode.toLowerCase().includes(kw),
  )
})

/** 按资源（其次模块）分组的权限目录 */
const groups = computed(() => {
  const map = new Map<string, PermissionListItemDto[]>()
  for (const permission of filteredCatalog.value) {
    const group = permission.resourceName || permission.moduleCode || '其他'
    const list = map.get(group)
    if (list) {
      list.push(permission)
    }
    else {
      map.set(group, [permission])
    }
  }
  return [...map.entries()].map(([name, items]) => ({ name, items }))
})

async function loadRoles() {
  try {
    roles.value = await roleApi.enabledList({ limit: 200 })
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载角色失败')
  }
}

/** 权限目录翻页拉全集（后端 pageSize 受夹紧，按空页停止） */
async function loadCatalog() {
  if (catalog.value.length > 0) {
    return
  }
  const all: PermissionListItemDto[] = []
  for (let page = 1; page <= 50; page++) {
    const result = await permissionApi.page(createPageRequest({ page: { pageIndex: page, pageSize: 100 } }))
    const items = result.items ?? []
    if (items.length === 0) {
      break
    }
    all.push(...items)
    if (all.length >= (result.page?.totalCount ?? all.length)) {
      break
    }
  }
  catalog.value = all
}

async function loadGrants() {
  if (selectedRoleId.value == null) {
    grants.value = []
    return
  }
  grants.value = await rolePermissionApi.list(selectedRoleId.value)
}

watch(selectedRoleId, async () => {
  if (selectedRoleId.value == null) {
    return
  }
  loading.value = true
  try {
    await Promise.all([loadCatalog(), loadGrants()])
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载权限失败')
  }
  finally {
    loading.value = false
  }
})

async function toggle(permission: PermissionListItemDto, checked: boolean) {
  if (selectedRoleId.value == null || togglingId.value != null) {
    return
  }
  togglingId.value = permission.basicId
  try {
    if (checked) {
      await rolePermissionApi.grant({
        roleId: selectedRoleId.value,
        permissionId: permission.basicId,
        permissionAction: PermissionAction.Grant,
      })
      message.success(`已授权：${permission.permissionName}`)
    }
    else {
      const grant = grantByPermissionId.value.get(permission.basicId)
      if (grant) {
        await rolePermissionApi.revoke(grant.basicId)
        message.success(`已收回：${permission.permissionName}`)
      }
    }
    await loadGrants()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
  finally {
    togglingId.value = null
  }
}

onMounted(loadRoles)
</script>

<template>
  <div class="pmc">
    <div class="pmc__shell">
      <!-- 左侧模块导航 -->
      <aside class="pmc__rail">
        <div class="pmc__rail-label">
          权限中心
        </div>
        <nav class="pmc__nav">
          <button
            v-for="item in modules"
            :key="item.key"
            type="button"
            class="pmc__nav-item"
            :class="{ 'is-active': activeModule === item.key }"
            @click="activeModule = item.key"
          >
            <span class="pmc__nav-icon"><Icon :icon="item.icon" width="17" /></span>
            <span class="pmc__nav-text">
              <span class="pmc__nav-name">{{ item.label }}</span>
              <span class="pmc__nav-desc">{{ item.desc }}</span>
            </span>
          </button>
        </nav>
      </aside>

      <!-- 右侧内容 -->
      <main class="pmc__main">
        <header class="pmc__bar">
          <div class="pmc__bar-title">
            <Icon :icon="activeModuleMeta.icon" width="18" />
            <span>{{ activeModuleMeta.label }}</span>
          </div>
          <div class="pmc__bar-desc">
            {{ activeModuleMeta.desc }}
          </div>
        </header>

        <div class="pmc__body">
          <!-- 角色授权·权限分配 -->
          <template v-if="activeModule === 'role'">
            <div class="pmc__toolbar">
              <NSelect
                v-model:value="selectedRoleId"
                :options="roleOptions"
                placeholder="选择角色进行授权"
                filterable
                clearable
                style="width: 280px"
              />
              <NInput
                v-if="selectedRoleId != null"
                v-model:value="keyword"
                placeholder="搜索权限名称 / 编码"
                clearable
                style="width: 220px"
              />
              <NTag v-if="selectedRoleId != null" round type="success" :bordered="false">
                已授权 {{ grantedCount }} 项
              </NTag>
            </div>

            <NEmpty v-if="selectedRoleId == null" description="请选择一个角色" class="pmc__empty" />
            <NSpin v-else :show="loading">
              <div v-if="groups.length === 0 && !loading" class="pmc__hint">
                无匹配权限
              </div>
              <div v-else class="pmc__groups">
                <section v-for="group in groups" :key="group.name" class="pmc__group">
                  <div class="pmc__group-head">
                    <span class="pmc__group-name">{{ group.name }}</span>
                    <span class="pmc__group-count">{{ group.items.length }}</span>
                  </div>
                  <div class="pmc__perms">
                    <label
                      v-for="permission in group.items"
                      :key="String(permission.basicId)"
                      class="pmc__perm"
                    >
                      <NCheckbox
                        :checked="grantByPermissionId.has(permission.basicId)"
                        :disabled="togglingId === permission.basicId"
                        @update:checked="(checked: boolean) => toggle(permission, checked)"
                      />
                      <span class="pmc__perm-text">
                        <span class="pmc__perm-name">{{ permission.permissionName }}</span>
                        <span class="pmc__perm-code">{{ permission.permissionCode }}</span>
                      </span>
                    </label>
                  </div>
                </section>
              </div>
            </NSpin>
          </template>

          <!-- 其余模块占位（后续阶段实现） -->
          <NEmpty v-else class="pmc__empty">
            <template #default>
              {{ activeModuleMeta.label }}建设中
            </template>
            <template #icon>
              <NIcon><Icon icon="lucide:hammer" /></NIcon>
            </template>
          </NEmpty>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
.pmc {
  height: 100%;
  padding: 16px;
}

.pmc__shell {
  display: grid;
  grid-template-columns: 240px 1fr;
  height: 100%;
  overflow: hidden;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
}

.pmc__rail {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 16px 12px;
  background: hsl(var(--muted) / 0.5);
  border-right: 1px solid var(--border-color);
  overflow-y: auto;
}

.pmc__rail-label {
  padding: 4px 10px 10px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.14em;
  color: var(--text-secondary);
  opacity: 0.6;
}

.pmc__nav {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.pmc__nav-item {
  display: flex;
  align-items: center;
  gap: 11px;
  width: 100%;
  padding: 9px 10px;
  border: none;
  border-radius: var(--radius);
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
  text-align: left;
  transition: background 0.16s, color 0.16s;
}

.pmc__nav-item:hover {
  background: hsl(var(--accent));
  color: var(--text-primary);
}

.pmc__nav-item.is-active {
  background: hsl(var(--primary) / 11%);
  color: hsl(var(--primary));
}

.pmc__nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  border-radius: 8px;
  background: hsl(var(--muted));
  color: var(--text-secondary);
}

.pmc__nav-item.is-active .pmc__nav-icon {
  background: hsl(var(--primary) / 16%);
  color: hsl(var(--primary));
}

.pmc__nav-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.pmc__nav-name {
  font-size: 14px;
  font-weight: 500;
}

.pmc__nav-desc {
  font-size: 11.5px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pmc__main {
  display: flex;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
}

.pmc__bar {
  flex-shrink: 0;
  padding: 16px 24px;
  border-bottom: 1px solid var(--border-color);
}

.pmc__bar-title {
  display: flex;
  align-items: center;
  gap: 9px;
  font-size: 16px;
  font-weight: 700;
  color: var(--text-primary);
}

.pmc__bar-desc {
  margin-top: 3px;
  font-size: 13px;
  color: var(--text-secondary);
}

.pmc__body {
  flex: 1;
  overflow-y: auto;
  padding: 18px 24px 28px;
  scrollbar-gutter: stable;
}

.pmc__toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
  flex-wrap: wrap;
}

.pmc__empty {
  padding: 64px 0;
}

.pmc__hint {
  padding: 32px 0;
  text-align: center;
  color: var(--text-secondary);
  font-size: 13px;
}

.pmc__groups {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.pmc__group {
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  overflow: hidden;
}

.pmc__group-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  background: hsl(var(--muted) / 0.5);
  border-bottom: 1px solid var(--border-color);
}

.pmc__group-name {
  font-size: 13.5px;
  font-weight: 600;
  color: var(--text-primary);
}

.pmc__group-count {
  font-size: 12px;
  color: var(--text-secondary);
}

.pmc__perms {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 4px 16px;
  padding: 12px 14px;
}

.pmc__perm {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 6px 8px;
  border-radius: var(--radius-sm, 6px);
  cursor: pointer;
}

.pmc__perm:hover {
  background: hsl(var(--accent));
}

.pmc__perm-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.pmc__perm-name {
  font-size: 13.5px;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pmc__perm-code {
  font-size: 11.5px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 768px) {
  .pmc {
    height: auto;
    padding: 12px;
  }

  .pmc__shell {
    grid-template-columns: 1fr;
    height: auto;
  }

  .pmc__rail {
    border-right: none;
    border-bottom: 1px solid var(--border-color);
  }

  .pmc__main {
    overflow: visible;
  }

  .pmc__body {
    overflow: visible;
  }
}
</style>
