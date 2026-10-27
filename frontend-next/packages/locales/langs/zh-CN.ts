// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: '曦寒基础应用',
    version: 'v{version}',
    locale: '语言',
    actions: {
      refresh: '刷新',
      previous: '上一页',
      next: '下一页',
      home: '首页',
    },
    session: {
      label: '会话',
      signed_in: '{name} · {tenant}',
      platform: '平台',
      change_password: '修改密码',
      sign_out: '退出登录',
      must_change_password: '密码已被管理员重置，请先修改密码。',
    },
    nav: {
      label: '导航',
    },
    status: {
      forbidden: '无权访问',
      forbidden_hint: '你没有这个页面的权限。',
      not_found: '页面不存在',
      not_found_hint: '这个地址没有对应的页面。',
      home: '没有可访问的页面',
      home_hint: '请管理员给你指派角色。',
    },
  },
}
