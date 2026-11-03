// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成的文案里固定的用语，10 种区域
/// </summary>
/// <remarks>
/// 用户已定：固定用语按区域内置，表与列的注释在 10 种区域里都原样用（不机器翻译），生成之后由人改
/// 用语里 <c>%name%</c> 一类是生成时代入的（业务对象名、列名、包名），花括号的是运行时的消息参数、原样留着
/// </remarks>
public static class CodeGenPhrases
{
    /// <summary>
    /// 10 种区域，与各包 <c>Localization/&lt;区域&gt;.json</c> 一致
    /// </summary>
    public static IReadOnlyList<string> Locales { get; } = ["en-US", "zh-CN", "zh-TW", "ja-JP", "ko-KR", "de-DE", "es-ES", "fr-FR", "pt-BR", "ru-RU"];

    private static readonly Dictionary<string, string[]> _phrases = new(StringComparer.Ordinal)
    {
        ["op.list.title"] = ["List %name%", "%name%列表", "%name%列表", "%name%の一覧", "%name% 목록", "%name% auflisten", "Listar %name%", "Lister %name%", "Listar %name%", "Список: %name%"],
        ["op.list.description"] = ["List %name% with paging, keyword, filters and sorting.", "分页列出%name%，可按关键字搜索、筛选与排序。", "分頁列出%name%，可按關鍵字搜尋、篩選與排序。", "%name%をページ単位で一覧表示します（キーワード検索、絞り込み、並べ替え）。", "%name%을(를) 페이지 단위로 나열합니다(키워드 검색, 필터, 정렬).", "%name% seitenweise auflisten, mit Stichwortsuche, Filtern und Sortierung.", "Lista paginada de %name% con búsqueda, filtros y orden.", "Liste paginée de %name% avec recherche, filtres et tri.", "Lista paginada de %name% com pesquisa, filtros e ordenação.", "Постраничный список: %name% — с поиском, фильтрами и сортировкой."],
        ["op.get.title"] = ["Get %name% details", "%name%详情", "%name%詳情", "%name%の詳細", "%name% 상세", "%name% abrufen", "Obtener %name%", "Obtenir %name%", "Obter %name%", "Получить: %name%"],
        ["op.get.description"] = ["Get one %name% record.", "取一条%name%。", "取一筆%name%。", "%name%を 1 件取得します。", "%name% 한 건을 가져옵니다.", "Einen Datensatz von %name% abrufen.", "Obtiene un registro de %name%.", "Renvoie un enregistrement de %name%.", "Obtém um registro de %name%.", "Возвращает одну запись: %name%."],
        ["op.create.title"] = ["Create %name%", "新建%name%", "新增%name%", "%name%を作成", "%name% 만들기", "%name% anlegen", "Crear %name%", "Créer %name%", "Criar %name%", "Создать: %name%"],
        ["op.create.description"] = ["Create a %name% record.", "新建一条%name%。", "新增一筆%name%。", "%name%を 1 件作成します。", "%name%을(를) 한 건 만듭니다.", "Einen Datensatz von %name% anlegen.", "Crea un registro de %name%.", "Crée un enregistrement de %name%.", "Cria um registro de %name%.", "Создаёт запись: %name%."],
        ["op.update.title"] = ["Update %name%", "修改%name%", "修改%name%", "%name%を更新", "%name% 수정", "%name% bearbeiten", "Actualizar %name%", "Modifier %name%", "Atualizar %name%", "Изменить: %name%"],
        ["op.update.description"] = ["Update a %name% record.", "修改一条%name%。", "修改一筆%name%。", "%name%を 1 件更新します。", "%name% 한 건을 수정합니다.", "Einen Datensatz von %name% bearbeiten.", "Actualiza un registro de %name%.", "Modifie un enregistrement de %name%.", "Atualiza um registro de %name%.", "Изменяет запись: %name%."],
        ["op.delete.title"] = ["Delete %name%", "删除%name%", "刪除%name%", "%name%を削除", "%name% 삭제", "%name% löschen", "Eliminar %name%", "Supprimer %name%", "Excluir %name%", "Удалить: %name%"],
        ["op.delete.description"] = ["Delete %name% records in batch; the ones that cannot be deleted are reported item by item.", "批量删除%name%，删不了的逐项报告。", "批次刪除%name%，刪不了的逐項回報。", "%name%をまとめて削除します。削除できないものは個別に報告します。", "%name%을(를) 일괄 삭제합니다. 삭제할 수 없는 항목은 개별로 알립니다.", "%name% gesammelt löschen; nicht löschbare Einträge werden einzeln gemeldet.", "Elimina %name% en lote; los que no se pueden eliminar se informan uno a uno.", "Supprime %name% par lot ; ceux qui ne peuvent pas l’être sont signalés un par un.", "Exclui %name% em lote; os que não podem ser excluídos são informados um a um.", "Пакетное удаление: %name%; то, что удалить нельзя, перечисляется по отдельности."],
        ["op.set_status.title"] = ["Enable or disable %name%", "启停%name%", "啟停%name%", "%name%の有効化・無効化", "%name% 사용·중지", "%name% aktivieren oder deaktivieren", "Habilitar o deshabilitar %name%", "Activer ou désactiver %name%", "Ativar ou desativar %name%", "Включить или отключить: %name%"],
        ["op.set_status.description"] = ["Enable or disable a %name% record.", "启用或停用一条%name%。", "啟用或停用一筆%name%。", "%name%を有効化または無効化します。", "%name%을(를) 사용하거나 중지합니다.", "Einen Datensatz von %name% aktivieren oder deaktivieren.", "Habilita o deshabilita un registro de %name%.", "Active ou désactive un enregistrement de %name%.", "Ativa ou desativa um registro de %name%.", "Включает или отключает запись: %name%."],
        ["op.tree.title"] = ["Get %name% tree", "%name%树", "%name%樹", "%name%のツリー", "%name% 트리", "Baum von %name% abrufen", "Árbol de %name%", "Arbre de %name%", "Árvore de %name%", "Дерево: %name%"],
        ["op.tree.description"] = ["The whole %name% tree of the current tenant.", "当前租户的整棵%name%树。", "目前租戶的整棵%name%樹。", "現在のテナントの%name%のツリー全体です。", "현재 테넌트의 %name% 트리 전체입니다.", "Der vollständige Baum von %name% im aktuellen Mandanten.", "El árbol completo de %name% del inquilino actual.", "L’arbre complet de %name% du locataire courant.", "A árvore completa de %name% do locatário atual.", "Всё дерево «%name%» текущего арендатора."],
        ["op.options.title"] = ["List %label% options", "%label%的选项", "%label%的選項", "%label%の選択肢", "%label% 선택 항목", "Optionen für %label%", "Opciones de %label%", "Options de %label%", "Opções de %label%", "Варианты: %label%"],
        ["op.options.description"] = ["Options for %label%: the IDs and display names of %target%.", "%label%的选项：%target%的 ID 与显示名。", "%label%的選項：%target%的 ID 與顯示名。", "%label%の選択肢：%target%の ID と表示名です。", "%label% 선택 항목: %target%의 ID와 표시 이름입니다.", "Optionen für %label%: IDs und Anzeigenamen von %target%.", "Opciones de %label%: ID y nombres de %target%.", "Options de %label% : identifiants et noms de %target%.", "Opções de %label%: IDs e nomes de %target%.", "Варианты для «%label%»: ID и названия из «%target%»."],
        ["msg.not_found"] = ["%name% '{id}' does not exist.", "%name%「{id}」不存在。", "%name%「{id}」不存在。", "%name%「{id}」は存在しません。", "%name% '{id}'이(가) 없습니다.", "%name% „{id}“ existiert nicht.", "%name% '{id}' no existe.", "%name% « {id} » n’existe pas.", "%name% '{id}' não existe.", "%name% «{id}» не существует."],
        ["msg.taken"] = ["%label% '{value}' is already taken.", "%label%「{value}」已被占用。", "%label%「{value}」已被使用。", "%label%「{value}」は既に使われています。", "%label% '{value}'은(는) 이미 사용 중입니다.", "%label% „{value}“ ist bereits vergeben.", "%label% '{value}' ya está en uso.", "%label% « {value} » est déjà utilisé.", "%label% '{value}' já está em uso.", "%label% «{value}» уже занято."],
        ["msg.has_children"] = ["%name% '{id}' still has children.", "%name%「{id}」还有下级。", "%name%「{id}」還有下級。", "%name%「{id}」にはまだ子があります。", "%name% '{id}'에 아직 하위 항목이 있습니다.", "%name% „{id}“ hat noch untergeordnete Einträge.", "%name% '{id}' todavía tiene elementos hijos.", "%name% « {id} » a encore des enfants.", "%name% '{id}' ainda tem filhos.", "У записи «%name%» «{id}» ещё есть дочерние."],
        ["msg.invalid_parent"] = ["The parent '{id}' does not exist or is the record itself or one of its descendants.", "上级「{id}」不存在，或是自己、自己的下级。", "上級「{id}」不存在，或是自己、自己的下級。", "親「{id}」が存在しないか、自分自身またはその子孫です。", "상위 '{id}'이(가) 없거나 자기 자신 또는 하위 항목입니다.", "Der übergeordnete Eintrag „{id}“ existiert nicht oder ist der Eintrag selbst bzw. ein Nachfahre.", "El padre '{id}' no existe o es el propio registro o un descendiente.", "Le parent « {id} » n’existe pas ou est l’enregistrement lui-même ou un descendant.", "O pai '{id}' não existe ou é o próprio registro ou um descendente.", "Родитель «{id}» не существует или это сама запись либо её потомок."],
        ["msg.filter_unsupported"] = ["Filter '{field} {operator}' is not supported.", "不支持按「{field} {operator}」筛选。", "不支援按「{field} {operator}」篩選。", "「{field} {operator}」での絞り込みはできません。", "'{field} {operator}' 필터는 지원하지 않습니다.", "Der Filter „{field} {operator}“ wird nicht unterstützt.", "El filtro '{field} {operator}' no es compatible.", "Le filtre « {field} {operator} » n’est pas pris en charge.", "O filtro '{field} {operator}' não é suportado.", "Фильтр «{field} {operator}» не поддерживается."],
        ["msg.sort_unsupported"] = ["Sorting by '{field}' is not supported.", "不支持按「{field}」排序。", "不支援按「{field}」排序。", "「{field}」での並べ替えはできません。", "'{field}' 기준 정렬은 지원하지 않습니다.", "Sortieren nach „{field}“ wird nicht unterstützt.", "No se puede ordenar por '{field}'.", "Le tri par « {field} » n’est pas pris en charge.", "Não é possível ordenar por '{field}'.", "Сортировка по «{field}» не поддерживается."],
        ["tag.description"] = ["Create, read, update and delete %name%.", "%name%的增删改查。", "%name%的增刪改查。", "%name%の作成・参照・更新・削除。", "%name%의 생성·조회·수정·삭제.", "%name% anlegen, lesen, bearbeiten und löschen.", "Crear, leer, actualizar y eliminar %name%.", "Créer, lire, modifier et supprimer %name%.", "Criar, ler, atualizar e excluir %name%.", "Создание, чтение, изменение и удаление: %name%."],
        ["group.description"] = ["The %module% package created by code generation.", "代码生成建出的 %module% 包。", "程式碼產生建出的 %module% 套件。", "コード生成で作成した %module% パッケージです。", "코드 생성으로 만든 %module% 패키지입니다.", "Das per Codegenerierung angelegte Paket %module%.", "El paquete %module% creado por la generación de código.", "Le paquet %module% créé par la génération de code.", "O pacote %module% criado pela geração de código.", "Пакет %module%, созданный генерацией кода."],
        ["perm.read.name"] = ["Read %name%", "查看%name%", "檢視%name%", "%name%を閲覧", "%name% 보기", "%name% lesen", "Ver %name%", "Consulter %name%", "Ver %name%", "Просмотр: %name%"],
        ["perm.read.description"] = ["View %name%.", "查看%name%。", "檢視%name%。", "%name%を閲覧します。", "%name%을(를) 봅니다.", "%name% ansehen.", "Ver %name%.", "Consulter %name%.", "Ver %name%.", "Просмотр: %name%."],
        ["perm.create.name"] = ["Create %name%", "新建%name%", "新增%name%", "%name%を作成", "%name% 만들기", "%name% anlegen", "Crear %name%", "Créer %name%", "Criar %name%", "Создание: %name%"],
        ["perm.create.description"] = ["Create %name%.", "新建%name%。", "新增%name%。", "%name%を作成します。", "%name%을(를) 만듭니다.", "%name% anlegen.", "Crear %name%.", "Créer %name%.", "Criar %name%.", "Создание: %name%."],
        ["perm.update.name"] = ["Update %name%", "修改%name%", "修改%name%", "%name%を更新", "%name% 수정", "%name% bearbeiten", "Actualizar %name%", "Modifier %name%", "Atualizar %name%", "Изменение: %name%"],
        ["perm.update.description"] = ["Edit %name%.", "修改%name%。", "修改%name%。", "%name%を更新します。", "%name%을(를) 수정합니다.", "%name% bearbeiten.", "Editar %name%.", "Modifier %name%.", "Editar %name%.", "Изменение: %name%."],
        ["perm.delete.name"] = ["Delete %name%", "删除%name%", "刪除%name%", "%name%を削除", "%name% 삭제", "%name% löschen", "Eliminar %name%", "Supprimer %name%", "Excluir %name%", "Удаление: %name%"],
        ["perm.delete.description"] = ["Delete %name%.", "删除%name%。", "刪除%name%。", "%name%を削除します。", "%name%을(를) 삭제합니다.", "%name% löschen.", "Eliminar %name%.", "Supprimer %name%.", "Excluir %name%.", "Удаление: %name%."],
    };

    /// <summary>
    /// 一句用语在某个区域的写法，代入生成时的参数
    /// </summary>
    /// <param name="key">用语键</param>
    /// <param name="locale">区域</param>
    /// <param name="values">生成时的参数（如 <c>name</c>、<c>label</c>）</param>
    public static string Text(string key, string locale, params (string Name, string Value)[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var index = Locales.ToList().IndexOf(locale);
        var text = _phrases[key][index < 0 ? 0 : index];
        foreach (var (name, value) in values)
        {
            text = text.Replace($"%{name}%", value, StringComparison.Ordinal);
        }

        return text;
    }

    /// <summary>
    /// 全部用语键（用例核对每句都有 10 种区域）
    /// </summary>
    public static IReadOnlyCollection<string> Keys => _phrases.Keys;

    /// <summary>
    /// 一句用语的全部区域写法（用例用）
    /// </summary>
    /// <param name="key">用语键</param>
    public static IReadOnlyList<string> All(string key) => _phrases[key];
}
