import type { App, Directive, DirectiveBinding } from 'vue'
import { usePermission } from '~/hooks/usePermission'

/** v-access:code / v-access:role，无权限时从 DOM 移除节点 */
function checkAccess(el: HTMLElement, binding: DirectiveBinding) {
  const { hasPermission, hasRole } = usePermission()
  const modifier = binding.arg as 'code' | 'role' | undefined
  const values = Array.isArray(binding.value) ? binding.value : [binding.value]

  if (!values.length)
    return

  let allowed = false
  if (modifier === 'role') {
    allowed = hasRole(values as string[])
  }
  else {
    allowed = hasPermission(values as string[])
  }

  if (!allowed) {
    el.parentNode?.removeChild(el)
  }
}

export const accessDirective: Directive = {
  mounted(el: HTMLElement, binding: DirectiveBinding) {
    checkAccess(el, binding)
  },
  updated(el: HTMLElement, binding: DirectiveBinding) {
    checkAccess(el, binding)
  },
}

export function registerAccessDirective(app: App) {
  app.directive('access', accessDirective)
}
