// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: '역할',
      description: '현재 측의 역할, 권한, 구성원.',
      fields: {
        code: '코드',
        name: '이름',
        description: '설명',
        status: '상태',
        built_in: '내장',
        created_at: '생성 시각',
      },
      actions: {
        create: '역할 생성',
        save_permissions: '권한 저장',
        delete: '역할 삭제',
        add_member: '구성원 추가',
        remove_member: '제거',
      },
      sections: {
        permissions: '권한',
        members: '구성원',
      },
      empty: '아직 역할이 없습니다.',
      total: '역할 {total}개',
      select_user: '사용자 선택',
      no_members: '아직 구성원이 없습니다.',
      saved: '권한을 저장했습니다.',
      built_in_badge: '내장',
    },
  },
}
