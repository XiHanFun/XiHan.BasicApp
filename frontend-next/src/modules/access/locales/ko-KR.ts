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
        save_data_scope: '데이터 범위 저장',
      },
      sections: {
        permissions: '권한',
        members: '구성원',
        data_scope: '데이터 범위',
      },
      empty: '아직 역할이 없습니다.',
      total: '역할 {total}개',
      select_user: '사용자 선택',
      no_members: '아직 구성원이 없습니다.',
      saved: '권한을 저장했습니다.',
      built_in_badge: '내장',
      data_scope: {
        self_only: '본인만',
        department_only: '소속 부서',
        department_and_children: '소속 부서와 하위 부서',
        all: '전체',
        custom: '사용자 지정 부서',
      },
      custom_departments: '사용자 지정 부서',
      include_children: '하위 부서 포함',
      data_scope_saved: '데이터 범위를 저장했습니다.',
    },
  },
}
