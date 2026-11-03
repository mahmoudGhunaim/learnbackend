using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LearnBackend.Api.CodeRunner;

public sealed record SafetyViolation(int Line, int Column, string Api, string ReasonAr, string ReasonEn);

/// <summary>
/// Walks YOUR syntax tree with Roslyn's semantic model and rejects code that touches
/// dangerous APIs before it ever runs: files, processes, network, reflection, native interop,
/// Environment.Exit, swapping the real console stream...
///
/// It works on symbols, not on text, so "using F = System.IO.File; F.Delete(...)" is caught too.
/// This is the first layer of defence; the separate sandbox process is the second.
/// </summary>
public static class SafetyAnalyzer
{
    private static readonly string[] BlockedNamespaces =
    [
        "System.IO", "System.Net", "System.Reflection", "System.Runtime.InteropServices",
        "System.Runtime.Loader", "System.Diagnostics", "System.Security", "System.Resources",
        "Microsoft.Win32",
    ];

    private static readonly string[] AllowedNamespaces = ["System.Diagnostics.CodeAnalysis"];

    private static readonly HashSet<string> AllowedTypes =
    [
        "System.IO.TextWriter", "System.IO.TextReader", "System.IO.StringWriter", "System.IO.StringReader",
        "System.IO.MemoryStream", "System.IO.Stream", "System.IO.SeekOrigin", "System.IO.IOException",
        "System.IO.InvalidDataException", "System.IO.EndOfStreamException", "System.IO.Path",
        "System.Diagnostics.Stopwatch", "System.Diagnostics.Debug", "System.Diagnostics.ConditionalAttribute",
        "System.Diagnostics.DebuggerDisplayAttribute", "System.Diagnostics.UnreachableException",
    ];

    /// <summary>Harmless members on otherwise-blocked types (typeof(int).Name is fine to read).</summary>
    private static readonly Dictionary<string, HashSet<string>> AllowedMembers = new()
    {
        ["System.Reflection.MemberInfo"] = ["Name", "MemberType"],
    };

    /// <summary>Types where every member is blocked, except the listed safe ones (null = block all).</summary>
    private static readonly Dictionary<string, HashSet<string>?> TypesWithAllowedMembersOnly = new()
    {
        ["System.Environment"] = ["NewLine", "ProcessorCount", "TickCount", "TickCount64", "CurrentManagedThreadId", "StackTrace", "Is64BitProcess"],
        ["System.AppDomain"] = null,
        ["System.Activator"] = null,
        ["System.Runtime.CompilerServices.Unsafe"] = null,
        ["System.Runtime.CompilerServices.RuntimeHelpers"] = null,
        ["System.Threading.ExecutionContext"] = null,
    };

    /// <summary>Specific dangerous members on otherwise-safe types.</summary>
    private static readonly Dictionary<string, HashSet<string>> BlockedMembers = new()
    {
        ["System.Console"] = ["OpenStandardOutput", "OpenStandardInput", "OpenStandardError", "SetOut", "SetError", "SetIn"],
        ["System.Type"] =
        [
            "GetType", "InvokeMember", "GetMethod", "GetMethods", "GetField", "GetFields", "GetProperty",
            "GetProperties", "GetMember", "GetMembers", "GetConstructor", "GetConstructors", "GetEvent",
            "GetEvents", "GetNestedType", "GetNestedTypes", "GetTypeFromProgID", "GetTypeFromCLSID",
            "MakeGenericType", "TypeHandle", "Module", "Assembly",
        ],
        ["System.Delegate"] = ["Method", "DynamicInvoke", "CreateDelegate"],
        // Path is pure string manipulation, except these members that touch the real file system.
        ["System.IO.Path"] = ["GetTempFileName", "GetTempPath", "GetFullPath", "Exists"],
        ["System.GC"] = ["AllocateUninitializedArray", "AllocateArray"],
    };

    public static IReadOnlyList<SafetyViolation> Analyze(SemanticModel model, SyntaxTree tree)
    {
        var violations = new Dictionary<(int, string), SafetyViolation>();
        var root = tree.GetRoot();

        foreach (var node in root.DescendantNodes())
        {
            // 1) Names: type names, method names, property names, attribute names, using directives
            if (node is SimpleNameSyntax)
            {
                var info = model.GetSymbolInfo(node);
                var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
                if (symbol is not null) Check(symbol, node, violations);
            }
            // 2) Expressions whose TYPE is dangerous, even if the name looked innocent (var x = Helper();)
            else if (node is ExpressionSyntax expr and not (LiteralExpressionSyntax or InterpolatedStringExpressionSyntax))
            {
                var type = model.GetTypeInfo(expr).Type;
                if (type is not null && FindBlockedType(type) is { } blocked)
                    Add(violations, node, blocked, "استخدام نوع غير مسموح به داخل بيئة التشغيل الآمنة.", "Uses a type that is not allowed inside the sandbox.");
            }

            // 3) Syntax-level bans
            if (node is MethodDeclarationSyntax m && m.Modifiers.Any(SyntaxKind.ExternKeyword))
                Add(violations, node, "extern", "الدوال الخارجية (extern) غير مسموحة.", "extern methods are not allowed.");
        }

        return violations.Values.OrderBy(v => v.Line).ThenBy(v => v.Column).ToList();
    }

    private static void Check(ISymbol symbol, SyntaxNode node, Dictionary<(int, string), SafetyViolation> violations)
    {
        if (symbol is INamespaceSymbol) return;

        if (symbol is ITypeSymbol type)
        {
            if (FindBlockedType(type) is { } blocked)
                Add(violations, node, blocked, ReasonAr(blocked), ReasonEn(blocked));
            return;
        }

        var containing = symbol.ContainingType;
        if (containing is null) return;

        var owner = FullName(containing.OriginalDefinition);
        if (AllowedMembers.TryGetValue(owner, out var safe) && safe.Contains(symbol.Name)) return;

        if (FindBlockedType(containing) is { } blockedOwner)
        {
            Add(violations, node, $"{blockedOwner}.{symbol.Name}", ReasonAr(blockedOwner), ReasonEn(blockedOwner));
            return;
        }

        if (TypesWithAllowedMembersOnly.TryGetValue(owner, out var allowed) && (allowed is null || !allowed.Contains(symbol.Name)))
            Add(violations, node, $"{owner}.{symbol.Name}", ReasonAr(owner), ReasonEn(owner));
        else if (BlockedMembers.TryGetValue(owner, out var members) && members.Contains(symbol.Name))
            Add(violations, node, $"{owner}.{symbol.Name}", ReasonAr(owner), ReasonEn(owner));
    }

    /// <summary>Returns the full name of the first blocked type found inside <paramref name="type"/> (arrays, generics...).</summary>
    private static string? FindBlockedType(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return FindBlockedType(array.ElementType);
            case IPointerTypeSymbol or IFunctionPointerTypeSymbol:
                return "pointer";
            case INamedTypeSymbol named:
                foreach (var arg in named.TypeArguments)
                    if (FindBlockedType(arg) is { } inner) return inner;
                if (named.ContainingType is not null && FindBlockedType(named.ContainingType) is { } outer) return outer;

                var full = FullName(named.OriginalDefinition);
                if (AllowedTypes.Contains(full)) return null;
                if (TypesWithAllowedMembersOnly.TryGetValue(full, out var allowed) && allowed is null) return full;

                var ns = named.ContainingNamespace?.ToDisplayString() ?? "";
                if (AllowedNamespaces.Any(a => ns == a || ns.StartsWith(a + ".", StringComparison.Ordinal))) return null;
                return BlockedNamespaces.Any(b => ns == b || ns.StartsWith(b + ".", StringComparison.Ordinal)) ? full : null;
            default:
                return null;
        }
    }

    private static string FullName(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace is { IsGlobalNamespace: false } n ? n.ToDisplayString() + "." : "";
        return ns + type.Name;
    }

    private static string ReasonAr(string api) => api switch
    {
        _ when api.StartsWith("System.IO", StringComparison.Ordinal) => "الوصول إلى نظام الملفات (File System) ممنوع في بيئة التشغيل الآمنة.",
        _ when api.StartsWith("System.Net", StringComparison.Ordinal) => "الوصول إلى الشبكة (Network) ممنوع في بيئة التشغيل الآمنة.",
        _ when api.StartsWith("System.Reflection", StringComparison.Ordinal) || api.StartsWith("System.Type", StringComparison.Ordinal) || api.StartsWith("System.Activator", StringComparison.Ordinal)
            => "الانعكاس (Reflection) وتوليد الكود ديناميكياً ممنوعان هنا.",
        _ when api.StartsWith("System.Diagnostics", StringComparison.Ordinal) => "تشغيل العمليات (Process) وأدوات التشخيص ممنوع.",
        _ when api.StartsWith("System.Environment", StringComparison.Ordinal) => "التحكم في بيئة التشغيل (Environment) مثل الخروج أو قراءة المتغيرات ممنوع.",
        _ when api.StartsWith("System.Console", StringComparison.Ordinal) => "تغيير مجرى الإخراج الحقيقي للـ Console ممنوع.",
        _ => "هذه الواجهة البرمجية (API) غير مسموحة في بيئة التشغيل الآمنة.",
    };

    private static string ReasonEn(string api) => api switch
    {
        _ when api.StartsWith("System.IO", StringComparison.Ordinal) => "File system access is not allowed in the sandbox.",
        _ when api.StartsWith("System.Net", StringComparison.Ordinal) => "Network access is not allowed in the sandbox.",
        _ when api.StartsWith("System.Reflection", StringComparison.Ordinal) || api.StartsWith("System.Type", StringComparison.Ordinal) || api.StartsWith("System.Activator", StringComparison.Ordinal)
            => "Reflection and dynamic code generation are not allowed.",
        _ when api.StartsWith("System.Diagnostics", StringComparison.Ordinal) => "Processes and diagnostics APIs are not allowed.",
        _ when api.StartsWith("System.Environment", StringComparison.Ordinal) => "Controlling the runtime environment (exit, env vars) is not allowed.",
        _ when api.StartsWith("System.Console", StringComparison.Ordinal) => "Replacing the real console streams is not allowed.",
        _ => "This API is not allowed in the sandbox.",
    };

    private static void Add(Dictionary<(int, string), SafetyViolation> violations, SyntaxNode node, string api, string ar, string en)
    {
        var span = node.GetLocation().GetLineSpan();
        var line = span.StartLinePosition.Line + 1;
        violations.TryAdd((line, api), new SafetyViolation(line, span.StartLinePosition.Character + 1, api, ar, en));
    }
}
