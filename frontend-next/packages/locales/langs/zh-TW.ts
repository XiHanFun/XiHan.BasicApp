// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: '曦寒基礎應用',
    version: 'v{version}',
    locale: '語言',
    actions: {
      refresh: '重新整理',
      previous: '上一頁',
      next: '下一頁',
      home: '首頁',
    },
    session: {
      label: '工作階段',
      signed_in: '{name} · {tenant}',
      platform: '平台',
      change_password: '變更密碼',
      sign_out: '登出',
      must_change_password: '密碼已由管理員重設，請先變更密碼。',
    },
    nav: {
      label: '導覽',
    },
    status: {
      forbidden: '無權存取',
      forbidden_hint: '你沒有這個頁面的權限。',
      not_found: '頁面不存在',
      not_found_hint: '這個網址沒有對應的頁面。',
      home: '沒有可存取的頁面',
      home_hint: '請管理員為你指派角色。',
    },
  },
}
