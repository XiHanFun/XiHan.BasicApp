<script lang="ts" setup>
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useUsers } from './use-users'

defineOptions({ name: 'IdentityUsersView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useUsers(10)

onMounted(() => {
  void page.load()
})

const columns = [
  { id: 'userName', label: 'identity.users.fields.user_name' },
  { id: 'displayName', label: 'identity.users.fields.display_name' },
  { id: 'email', label: 'identity.users.fields.email' },
  { id: 'status', label: 'identity.users.fields.status' },
  { id: 'createdAt', label: 'identity.users.fields.created_at' },
] as const

function formatTime(value: string): string {
  // 后端一律 UTC；按浏览器区域显示（用户偏好时区随 N1 的个人设置接入）
  return new Date(value).toLocaleString()
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('identity.users.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('identity.users.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 创建：按钮即操作，门控用操作的权限码（安全边界在后端）；每个字段有可见标签；校验以后端 Schema 与自校验为准（novalidate 关掉浏览器原生校验，否则 type=email 不合法时 submit 根本不触发），字段级 errors 由 ApiError 交到对应字段 -->
    <form v-if="auth.can('identity.users.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-4" data-testid="create-user" novalidate @submit.prevent="page.submit()">
      <!-- 字段部件不写默认插槽：标题、错误文案由 label / error-text 属性铺出，控件放 control 插槽，标签的 for 与控件的 id、aria-* 由它生成 -->
      <XhFieldRoot :label="t('identity.users.fields.user_name')" :error-text="page.fieldError('userName')" :invalid="!!page.fieldError('userName')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.userName" name="userName" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.users.fields.display_name')" :error-text="page.fieldError('displayName')" :invalid="!!page.fieldError('displayName')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.displayName" name="displayName" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.users.fields.email')" :error-text="page.fieldError('email')" :invalid="!!page.fieldError('email')">
        <template #control>
          <XhTextFieldRoot :value="page.form.email ?? ''" name="email" type="email" @update:value="page.form.email = $event" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('identity.users.actions.create') }}
        </XhButton>
        <XhButton type="button" :disabled="page.loading.value" @click="page.load()">
          {{ t('common.actions.refresh') }}
        </XhButton>
      </div>
      <p v-if="page.submitError.value" class="text-caption text-fg-danger md:col-span-4" role="alert">
        {{ page.submitError.value }}
      </p>
    </form>

    <p v-if="page.listError.value" class="text-caption text-fg-danger" role="alert">
      {{ page.listError.value }}
    </p>

    <XhTableRoot
      :columns="columns.map(c => ({ id: c.id, label: t(c.label) }))"
      :rows="page.items.value.map(row => ({ id: row.id }))"
      :loading="page.loading.value"
      size="sm"
      ruled
    >
      <XhTableHeader>
        <XhTableRow>
          <XhTableColumnHeader v-for="column in columns" :key="column.id" :value="column.id">
            <XhTableColumnLabel>{{ t(column.label) }}</XhTableColumnLabel>
          </XhTableColumnHeader>
        </XhTableRow>
      </XhTableHeader>
      <XhTableBody>
        <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id">
          <XhTableCell value="userName">
            {{ row.userName }}
          </XhTableCell>
          <XhTableCell value="displayName">
            {{ row.displayName }}
          </XhTableCell>
          <XhTableCell value="email">
            {{ row.email ?? '-' }}
          </XhTableCell>
          <XhTableCell value="status">
            {{ row.status }}
          </XhTableCell>
          <XhTableCell value="createdAt">
            {{ formatTime(row.createdAt) }}
          </XhTableCell>
        </XhTableRow>
        <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
          {{ t('identity.users.empty') }}
        </XhTableEmpty>
      </XhTableBody>
    </XhTableRoot>

    <div class="flex items-center gap-3 text-caption text-fg-muted">
      <span>{{ t('identity.users.total', { total: page.total.value }) }}</span>
      <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
        {{ t('common.actions.previous') }}
      </XhButton>
      <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
      <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
        {{ t('common.actions.next') }}
      </XhButton>
    </div>
  </section>
</template>
