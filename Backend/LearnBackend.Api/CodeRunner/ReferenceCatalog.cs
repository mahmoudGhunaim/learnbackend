using Microsoft.CodeAnalysis;

namespace LearnBackend.Api.CodeRunner;

/// <summary>
/// The set of .NET assemblies your code is allowed to compile against.
///
/// Roslyn needs "metadata references" (the DLLs that define Console, List&lt;T&gt;, LINQ, ...).
/// We take them from the running .NET runtime (TRUSTED_PLATFORM_ASSEMBLIES) but only an
/// allow-list of them: no System.Net.Http, no System.Diagnostics.Process, no System.IO.FileSystem.Watcher...
/// If an assembly is not referenced, code that uses it simply does not compile.
/// Registered as a Singleton: building the list is expensive and it never changes.
/// </summary>
public sealed class ReferenceCatalog
{
    private static readonly HashSet<string> AllowedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.Private.CoreLib", "System.Runtime", "System.Console", "System.Collections",
        "System.Collections.Concurrent", "System.Linq", "System.Memory", "System.Runtime.Numerics",
        "System.Text.RegularExpressions", "System.Text.Json", "System.Text.Encodings.Web",
        "System.Threading", "System.Threading.Tasks", "System.ObjectModel",
        "System.ComponentModel", "System.ComponentModel.Primitives", "System.ComponentModel.Annotations",
        "System.ComponentModel.DataAnnotations",
        "System.Xml.ReaderWriter", "System.Xml.XDocument", "System.Private.Xml", "System.Private.Xml.Linq",
        "System.Runtime.Extensions", "netstandard",
    };

    public IReadOnlyList<MetadataReference> References { get; }

    public ReferenceCatalog()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)
                  ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available.");

        References = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => AllowedAssemblies.Contains(Path.GetFileNameWithoutExtension(path)))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }
}
