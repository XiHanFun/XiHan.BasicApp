// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: '언어',
    actions: {
      refresh: '새로 고침',
      previous: '이전',
      next: '다음',
    },
    session: {
      label: '세션',
      signed_in: '{name} · {tenant}',
      platform: '플랫폼',
      change_password: '비밀번호 변경',
      sign_out: '로그아웃',
      must_change_password: '관리자가 비밀번호를 재설정했습니다. 계속하기 전에 변경하세요.',
    },
  },
}
