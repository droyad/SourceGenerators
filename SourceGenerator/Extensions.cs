using Microsoft.CodeAnalysis;

namespace SourceGenerator;

static class Extensions
{
    public static string FullNamespace(this INamespaceSymbol ns)
        => string.Join(".", GetNamespaceParts(ns));

    public static bool Is(this INamespaceSymbol ns, params string[] nsParts)
        => GetNamespaceParts(ns).SequenceEqual(nsParts);

    static IReadOnlyList<string> GetNamespaceParts(INamespaceSymbol ns)
    {
        var parts = new List<string>();
        while (ns != null && ns.Name != "")
        {
            parts.Add(ns.Name);
            ns = ns.ContainingNamespace;
        }

        parts.Reverse();
        return parts;
    }

    public static IEnumerable<IPropertySymbol> PublicProperties(this ITypeSymbol symbol)
        => symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public);
}