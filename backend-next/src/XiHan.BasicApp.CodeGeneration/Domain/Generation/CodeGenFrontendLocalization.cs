// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成的前端文案：每个区域一份 <c>locales/&lt;区域&gt;.ts</c>。
/// </summary>
/// <remarks>
/// 键在 <c>&lt;包&gt;.&lt;资源&gt;</c> 下（连字符换成下划线），与前端模板里的 <c>t('…')</c> 一一对应：页面标题（也是菜单标题键）、字段名、按钮、弹窗标题、必填提示与枚举选项。
/// 用户已定：固定用语按区域内置，表与列的注释在 10 种区域里都原样用；值按 vue-i18n 的消息语法转义（<c>@ | { }</c> 折成字面量插值）。
/// 「新建包」拼成 10 个文件，「加入已有包」写进登记片段。
/// </remarks>
public static class CodeGenFrontendLocalization
{
    private static readonly Dictionary<string, string[]> _phrases = new(StringComparer.Ordinal)
    {
        ["created_at"] = ["Created At", "创建时间", "建立時間", "作成日時", "생성 시간", "Erstellt am", "Creado el", "Créé le", "Criado em", "Создано"],
        ["keyword"] = ["Keyword", "关键词", "關鍵字", "キーワード", "키워드", "Stichwort", "Palabra clave", "Mot-clé", "Palavra-chave", "Ключевое слово"],
        ["keyword_placeholder"] = ["Search %name%", "搜索%name%", "搜尋%name%", "%name%を検索", "%name% 검색", "%name% suchen", "Buscar %name%", "Rechercher %name%", "Pesquisar %name%", "Поиск: %name%"],
        ["create"] = ["New %name%", "新增%name%", "新增%name%", "%name%を追加", "%name% 추가", "%name% hinzufügen", "Añadir %name%", "Ajouter %name%", "Adicionar %name%", "Добавить: %name%"],
        ["edit"] = ["Edit %name%", "编辑%name%", "編輯%name%", "%name%を編集", "%name% 편집", "%name% bearbeiten", "Editar %name%", "Modifier %name%", "Editar %name%", "Изменить: %name%"],
        ["toggle"] = ["Enable / Disable", "启用/停用", "啟用/停用", "有効化 / 無効化", "사용 / 중지", "Aktivieren / Deaktivieren", "Habilitar / deshabilitar", "Activer / désactiver", "Ativar / desativar", "Включить / отключить"],
        ["print"] = ["Print", "打印", "列印", "印刷", "인쇄", "Drucken", "Imprimir", "Imprimer", "Imprimir", "Печать"],
        ["required_input"] = ["Enter {label}", "请输入{label}", "請輸入{label}", "{label}を入力してください", "{label}을(를) 입력하세요", "Bitte {label} eingeben", "Introduce {label}", "Saisissez {label}", "Informe {label}", "Введите: {label}"],
        ["required_select"] = ["Select {label}", "请选择{label}", "請選擇{label}", "{label}を選択してください", "{label}을(를) 선택하세요", "Bitte {label} auswählen", "Selecciona {label}", "Sélectionnez {label}", "Selecione {label}", "Выберите: {label}"],
        ["required_upload"] = ["Upload {label}", "请上传{label}", "請上傳{label}", "{label}をアップロードしてください", "{label}을(를) 업로드하세요", "Bitte {label} hochladen", "Sube {label}", "Téléversez {label}", "Envie {label}", "Загрузите: {label}"],
        ["time_invalid"] = ["{label} must be HH:mm or HH:mm:ss", "{label}格式应为 HH:mm 或 HH:mm:ss", "{label}格式應為 HH:mm 或 HH:mm:ss", "{label}は HH:mm または HH:mm:ss の形式で入力してください", "{label}은(는) HH:mm 또는 HH:mm:ss 형식이어야 합니다", "{label} muss HH:mm oder HH:mm:ss sein", "{label} debe tener el formato HH:mm o HH:mm:ss", "{label} doit être au format HH:mm ou HH:mm:ss", "{label} deve estar no formato HH:mm ou HH:mm:ss", "{label}: нужен формат HH:mm или HH:mm:ss"],
        ["load_details_failed"] = ["Failed to load the details", "加载明细失败", "載入明細失敗", "明細の読み込みに失敗しました", "상세 항목을 불러오지 못했습니다", "Positionen konnten nicht geladen werden", "No se pudieron cargar los detalles", "Impossible de charger le détail", "Falha ao carregar os detalhes", "Не удалось загрузить детали"],
        ["print_failed"] = ["Print failed", "打印失败", "列印失敗", "印刷に失敗しました", "인쇄하지 못했습니다", "Drucken fehlgeschlagen", "Error al imprimir", "Échec de l’impression", "Falha ao imprimir", "Не удалось напечатать"],
        ["active"] = ["Active", "启用", "啟用", "有効", "사용", "Aktiv", "Activo", "Actif", "Ativo", "Активно"],
        ["disabled"] = ["Disabled", "停用", "停用", "無効", "중지", "Deaktiviert", "Desactivado", "Désactivé", "Desativado", "Отключено"],
    };

    /// <summary>
    /// 一个区域在资源下的键值。
    /// </summary>
    /// <remarks>
    /// 键相对 <c>&lt;包&gt;.&lt;资源&gt;</c>、点分；值已是 TypeScript 单引号字面量里的写法。
    /// </remarks>
    /// <param name="context">生成上下文。</param>
    /// <param name="locale">区域。</param>
    public static IReadOnlyList<KeyValuePair<string, string>> Entries(CodeGenContext context, string locale)
    {
        ArgumentNullException.ThrowIfNull(context);
        var name = CodeGenTextEscaper.I18nMessage(context.DisplayName);
        string Phrase(string key) => CodeGenTextEscaper.TsString(Of(key, locale)).Replace("%name%", name, StringComparison.Ordinal);

        var business = context.Columns.Where(static c => c.IsBusiness).ToList();
        var inForm = business.Where(static c => c.IsInsert || c.IsEdit).ToList();
        var canCreate = context.Can(CodeGenActions.Create) && inForm.Count > 0;
        var canUpdate = context.Can(CodeGenActions.Update) && inForm.Count > 0;
        var entries = new List<KeyValuePair<string, string>> { new("title", name) };
        entries.AddRange(business.Select(static c => new KeyValuePair<string, string>($"fields.{Snake(c.CSharpProperty)}", CodeGenTextEscaper.I18nMessage(CodeGenItems.LabelOf(c)))));
        entries.Add(new("fields.created_at", Phrase("created_at")));
        if (context.TreeParentColumn is null && business.Any(static c => c.IsQuery && c.QueryType is CodeGenQueryType.Like or CodeGenQueryType.LikeLeft or CodeGenQueryType.LikeRight && c.CSharpType.TrimEnd('?') == "string"))
        {
            entries.Add(new("keyword", Phrase("keyword")));
            entries.Add(new("keyword_placeholder", Phrase("keyword_placeholder")));
        }

        if (canCreate)
        {
            entries.Add(new("actions.create", Phrase("create")));
        }

        if (context.StatusColumn is not null)
        {
            entries.Add(new("actions.toggle", Phrase("toggle")));
        }

        if (context.Can(CodeGenActions.Print))
        {
            entries.Add(new("actions.print", Phrase("print")));
            entries.Add(new("print_failed", Phrase("print_failed")));
        }

        if (canCreate || canUpdate)
        {
            entries.Add(new("create_title", Phrase("create")));
            entries.Add(new("edit_title", Phrase("edit")));
            entries.Add(new("required_input", Phrase("required_input")));
            entries.Add(new("required_select", Phrase("required_select")));
            entries.Add(new("required_upload", Phrase("required_upload")));
            if (inForm.Any(static c => c.HtmlType == CodeGenHtmlType.TimePicker && c.CSharpType.TrimEnd('?') != "bool"))
            {
                entries.Add(new("time_invalid", Phrase("time_invalid")));
            }
        }

        if (context.TreeParentColumn is null && context.DetailTables.Count > 0)
        {
            entries.Add(new("load_details_failed", Phrase("load_details_failed")));
        }

        // 枚举选项：启停两个成员用固定用语，其余成员名原样（枚举没有可读的显示名，生成之后由人改）
        foreach (var column in business.Where(static c => c.Enum is not null))
        {
            foreach (var member in column.Enum!.Members)
            {
                var text = member switch
                {
                    "Active" => Phrase("active"),
                    "Disabled" => Phrase("disabled"),
                    _ => CodeGenTextEscaper.I18nMessage(member),
                };
                entries.Add(new($"options.{Snake(column.CSharpProperty)}.{Snake(member)}", text));
            }
        }

        return entries;
    }

    /// <summary>
    /// 一个区域的文案文件（新建包）。
    /// </summary>
    /// <param name="context">生成上下文。</param>
    /// <param name="locale">区域。</param>
    public static string File(CodeGenContext context, string locale)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ts = new StringBuilder();
        ts.Append("// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。").Append('\n');
        ts.Append("export default {").Append('\n');
        ts.Append("  ").Append(context.ModuleCode).Append(": {").Append('\n');
        Block(ts, context, locale, indent: 2);
        ts.Append("  },").Append('\n');
        ts.Append('}').Append('\n');
        return ts.ToString();
    }

    /// <summary>
    /// 资源这一块（加入已有包时合进 <c>&lt;包&gt;</c> 对象里）。
    /// </summary>
    /// <param name="context">生成上下文。</param>
    /// <param name="locale">区域。</param>
    public static string Block(CodeGenContext context, string locale)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ts = new StringBuilder();
        Block(ts, context, locale, indent: 0);
        return ts.ToString().TrimEnd('\n');
    }

    private static void Block(StringBuilder ts, CodeGenContext context, string locale, int indent)
    {
        // 点分键还原成嵌套对象，保持登记顺序
        var root = new Node();
        foreach (var (key, value) in Entries(context, locale))
        {
            var node = root;
            var segments = key.Split('.');
            foreach (var segment in segments[..^1])
            {
                node = node.Children.FirstOrDefault(c => c.Key == segment) is { } child ? child : node.Add(segment);
            }

            node.Add(segments[^1]).Value = value;
        }

        var resource = new Node { Key = context.Resource.Replace('-', '_') };
        resource.Children.AddRange(root.Children);
        Write(ts, resource, indent + 2);
    }

    private static void Write(StringBuilder ts, Node node, int indent)
    {
        var pad = new string(' ', indent);
        if (node.Value is not null)
        {
            ts.Append(pad).Append(node.Key).Append(": '").Append(node.Value).Append("',").Append('\n');
            return;
        }

        ts.Append(pad).Append(node.Key).Append(": {").Append('\n');
        foreach (var child in node.Children)
        {
            Write(ts, child, indent + 2);
        }

        ts.Append(pad).Append("},").Append('\n');
    }

    private static string Of(string key, string locale)
    {
        var index = CodeGenPhrases.Locales.ToList().IndexOf(locale);
        return _phrases[key][index < 0 ? 0 : index];
    }

    private static string Snake(string value) => CodeGenNaming.Snakeize(value);

    private sealed class Node
    {
        public string Key { get; init; } = string.Empty;

        public string? Value { get; set; }

        public List<Node> Children { get; } = [];

        public Node Add(string key)
        {
            var child = new Node { Key = key };
            Children.Add(child);
            return child;
        }
    }
}
