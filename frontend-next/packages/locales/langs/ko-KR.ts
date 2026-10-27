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
      token: '토큰',
      token_placeholder: 'pnpm dev:token 으로 발급한 토큰을 붙여넣기',
      apply: '적용',
      signed_in: '로그인: {subject} · {tenant}',
    },
  },
}
