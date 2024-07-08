using AdmxParser.Models;
using AdmxParser.Serialization;
using Scriban;
using Scriban.Runtime;
using System.Text.RegularExpressions;
using System.Web;

namespace AdmxCodeGen;

internal static class Helpers
{
    private static readonly Lazy<Regex> _refRegexFactory = new Lazy<Regex>(
        () => new Regex(@"\$\((?<ResourceType>[^.]+)\.(?<ResourceKey>[^\)]+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        LazyThreadSafetyMode.None);

    public static TemplateContext ToTemplateContext<T>(this T model,
        bool isLiquidTemplate = false,
        object? additionalModel = default,
        MemberRenamerDelegate? memberRenamer = default,
        MemberFilterDelegate? memberFilter = default)
    {
        var templateContext = isLiquidTemplate ?
            new LiquidTemplateContext() : new TemplateContext();
        templateContext.MemberRenamer = memberRenamer;
        templateContext.MemberFilter = memberFilter;

        var scriptObject = new ScriptObject();
        scriptObject.Import("escape_type", (Delegate)EscapeTypeName);
        scriptObject.Import("escape_identifier", (Delegate)EscapeIdentifier);
        scriptObject.Import("escape_namespace", (Delegate)EscapeNamespace);
        scriptObject.Import("escape_xmldoc", (Delegate)EscapeXmlDocumentation);
        scriptObject.Import("literal", (Delegate)ToCSharpLiteral);
        scriptObject.Import("is_delval", (Delegate)IsDeleteValue);
        scriptObject.Import("ref_id", (Delegate)GetRefId);

        scriptObject.Import("is_bei", (Delegate)IsBooleanElementItem);
        scriptObject.Import("is_dei", (Delegate)IsDecimalElementItem);
        scriptObject.Import("is_eei", (Delegate)IsEnumElementItem);
        scriptObject.Import("is_lei", (Delegate)IsListElementItem);
        scriptObject.Import("is_ldei", (Delegate)IsLongDecimalElementItem);
        scriptObject.Import("is_mtei", (Delegate)IsMultiTextElementItem);
        scriptObject.Import("is_tei", (Delegate)IsTextElementItem);

        scriptObject.Import("to_bei", (Delegate)ToBooleanElementItem);
        scriptObject.Import("to_dei", (Delegate)ToDecimalElementItem);
        scriptObject.Import("to_eei", (Delegate)ToEnumElementItem);
        scriptObject.Import("to_lei", (Delegate)ToListElementItem);
        scriptObject.Import("to_ldei", (Delegate)ToLongDecimalElementItem);
        scriptObject.Import("to_mtei", (Delegate)ToMultiTextElementItem);
        scriptObject.Import("to_tei", (Delegate)ToTextElementItem);

        if (model != null)
            scriptObject.Import(model, memberFilter, memberRenamer);
        if (additionalModel != null)
            scriptObject.Import(additionalModel, memberFilter, memberRenamer);

        templateContext.PushGlobal(scriptObject);
        return templateContext;
    }

	private static bool IsAsciiLetter(char c)
        => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

	private static string EscapeTypeName(string? s)
    {
        if (s == null)
            return string.Empty;
        s = s.Trim();
        s = s.Replace("-", string.Empty);
        s = s.Replace("_", string.Empty);
        if (!IsAsciiLetter(s[0]))
            s = "_" + s;
        return s;
    }

    private static string EscapeIdentifier(string? s)
    {
        if (s == null)
            return string.Empty;
        s = s.Trim();
        s = s.Replace("-", string.Empty);
        if (!IsAsciiLetter(s[0]))
            s = "_" + s;
        return s;
    }

    private static string EscapeNamespace(string? s)
    {
        if (s == null)
            return string.Empty;
        var list = new List<string>();
        foreach (var eachPart in s.Split(new char[] { '.', }, StringSplitOptions.RemoveEmptyEntries))
            list.Add(EscapeTypeName(eachPart));
        return string.Join('.', list);
    }

    private static string EscapeXmlDocumentation(string? s)
    {
        if (s == null)
            return string.Empty;
        var lines = s.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.None);
        var modifiedLines = new List<string>(lines.Length);
        foreach (var eachLine in lines)
            modifiedLines.Add($"/// {HttpUtility.HtmlEncode(eachLine)}");
        return string.Join(Environment.NewLine, modifiedLines);
    }

    private static string ToCSharpLiteral(object? o)
    {
        if (o is bool @bool) return @bool ? "true" : "false";
        if (o is char @char)
        {
            if (char.IsLetterOrDigit(@char))
                return "'" + @char.ToString().Replace("'", "\'") + "'";
            else
                return "'\\u" + ((short)@char).ToString("x4") + "'";
        }
        if (o is uint @uint) return @uint.ToString() + "u";
        if (o is ulong @ulong) return @ulong.ToString() + "uL";
        if (o is float @float) return @float.ToString() + "f";
        if (o is double @double) return @double.ToString() + "d";
        if (o is decimal @decimal) return @decimal.ToString() + "m";
        if (o is string @string) return @"@""" + @string.Replace("\"", "\"\"") + @"""";
        if (o is Enum e) return $"{e.GetType().Name}.{e.ToString()}";
        if (o is ValueDelete) return "null";
        return o?.ToString() ?? "null";
    }

    private static bool IsDeleteValue(object? o) => o is ValueDelete;

    private static string GetRefId(string? s)
    {
        var regex = _refRegexFactory.Value;
        var match = regex.Match(s ?? string.Empty);
        if (!match.Success)
            return string.Empty;
        return match.Groups["ResourceKey"].Value;
    }

    private static bool IsBooleanElementItem(object? o) => o is ParsedBooleanElementItem;
    private static bool IsDecimalElementItem(object? o) => o is ParsedDecimalElementItem;
    private static bool IsEnumElementItem(object? o) => o is ParsedEnumerationElementItem;
    private static bool IsListElementItem(object? o) => o is ParsedListElementItem;
    private static bool IsLongDecimalElementItem(object? o) => o is ParsedLongDecimalElementItem;
    private static bool IsMultiTextElementItem(object? o) => o is ParsedMultiTextElementItem;
    private static bool IsTextElementItem(object? o) => o is ParsedTextElementItem;

    private static ParsedBooleanElementItem ToBooleanElementItem(object o) => (ParsedBooleanElementItem)o;
    private static ParsedDecimalElementItem ToDecimalElementItem(object o) => (ParsedDecimalElementItem)o;
    private static ParsedEnumerationElementItem ToEnumElementItem(object o) => (ParsedEnumerationElementItem)o;
    private static ParsedListElementItem ToListElementItem(object o) => (ParsedListElementItem)o;
    private static ParsedLongDecimalElementItem ToLongDecimalElementItem(object o) => (ParsedLongDecimalElementItem)o;
    private static ParsedMultiTextElementItem ToMultiTextElementItem(object o) => (ParsedMultiTextElementItem)o;
    private static ParsedTextElementItem ToTextElementItem(object o) => (ParsedTextElementItem)o;
}
