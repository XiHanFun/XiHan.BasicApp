<script setup lang="ts">
import type { SessionRoleOption } from '~/types'
import { XhButton, XhCheckbox, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'

/**
 * 切换角色：本会话此刻持有的角色里选要激活的（令牌只带激活的角色）。同一动态职责分离组的角色不能同时激活，由服务端判定；
 * 保存后换签令牌、整页重建（与切换租户同一条路）。
 */
defineOptions({ name: 'SwitchRolesDialog' })

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const authStore = useAuthStore()

const loading = ref(false)
const saving = ref(false)
const roles = ref<SessionRoleOption[]>([])
const selected = ref<Set<string>>(new Set())
const errorMessage = ref('')
const unchanged = computed(() => roles.value.every(role => role.isActive === selected.value.has(role.roleId)))

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    roles.value = await authStore.sessionRoles()
    selected.value = new Set(roles.value.filter(role => role.isActive).map(role => role.roleId))
  }
  catch (error) {
    roles.value = []
    errorMessage.value = (error as Error)?.message || t('header.switch_roles.load_failed')
  }
  finally {
    loading.value = false
  }
}

function toggle(roleId: string, checked: boolean) {
  const next = new Set(selected.value)
  if (checked)
    next.add(roleId)
  else
    next.delete(roleId)
  selected.value = next
}

async function save() {
  if (selected.value.size === 0) {
    errorMessage.value = t('header.switch_roles.pick_one')
    return
  }
  saving.value = true
  errorMessage.value = ''
  try {
    await authStore.activateRoles([...selected.value])
  }
  catch (error) {
    errorMessage.value = (error as Error)?.message || t('header.switch_roles.save_failed')
    saving.value = false
  }
}

watch(show, (open) => {
  if (open)
    void load()
})
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 420px">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('header.switch_roles.title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <div class="flex flex-col gap-3" data-testid="switch-roles-dialog">
          <p class="switch-roles-hint">
            {{ t('header.switch_roles.hint') }}
          </p>
          <p v-if="errorMessage" class="switch-roles-error" role="alert">
            {{ errorMessage }}
          </p>
          <div v-if="loading" class="switch-roles-loading">
            <XhSpinner />
          </div>
          <p v-else-if="roles.length === 0" class="switch-roles-hint">
            {{ t('header.switch_roles.empty') }}
          </p>
          <ul v-else class="switch-roles-list">
            <li v-for="role in roles" :key="role.roleId" class="switch-roles-item">
              <XhCheckbox
                :checked="selected.has(role.roleId)"
                :aria-label="role.name"
                @update:checked="(value: boolean) => toggle(role.roleId, value)"
              />
              <span class="switch-roles-name">{{ role.name }}</span>
              <code class="switch-roles-code">{{ role.code }}</code>
            </li>
          </ul>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton size="sm" variant="ghost" @click="show = false">
          {{ t('header.switch_roles.cancel') }}
        </XhButton>
        <XhButton size="sm" tone="brand" :loading="saving" :disabled="loading || unchanged || roles.length === 0" @click="save">
          {{ t('header.switch_roles.save') }}
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.switch-roles-hint {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.switch-roles-error {
  margin: 0;
  color: var(--xh-fg-danger);
  font-size: var(--xh-text-caption-size);
}

.switch-roles-loading {
  display: flex;
  justify-content: center;
  padding-block: var(--xh-space-4);
}

.switch-roles-list {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.switch-roles-item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}

.switch-roles-name {
  flex: 1;
  min-inline-size: 0;
}

.switch-roles-code {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}
</style>
