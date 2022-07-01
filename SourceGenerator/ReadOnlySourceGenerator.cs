using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator;

[Generator]
public class ReadOnlySourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        //Debugger.Launch();

        // Register a factory that can create our custom syntax receiver
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            throw new Exception("Expected a SyntaxReceiver");

        var entitySymbols = receiver.AllClasses
            .Select(cds => GetSymbol(context, cds))
            .Where(ShouldBeMadeReadOnly)
            .ToArray();

        GenerateExtensions(context, entitySymbols);

        foreach (var entitySymbol in entitySymbols)
            GenerateReadOnly(context, entitySymbol);
    }

    private static ITypeSymbol GetSymbol(GeneratorExecutionContext context, ClassDeclarationSyntax cds)
    {
        var semanticModel = context.Compilation.GetSemanticModel(cds.SyntaxTree);

        return (ITypeSymbol?)semanticModel.GetDeclaredSymbol(cds)
               ?? throw new Exception("Could not get symbol");
    }


    public bool ShouldBeMadeReadOnly(ITypeSymbol symbol)
    {
        // var iDomainInterface = symbol.AllInterfaces.FirstOrDefault(i => i.Name == "IDomain");
        var ns = symbol.ContainingNamespace.FullNamespace();
        return ns.StartsWith("Example.Domain") &&
               symbol.IsReferenceType &&
               !symbol.IsRecord;
    }
    
    private void GenerateExtensions(GeneratorExecutionContext context, ITypeSymbol[] entities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace Example.Domain;");
        sb.AppendLine();
        sb.AppendLine("public static class ReadOnlyExtensions");
        sb.AppendLine("{");

        foreach (var entity in entities)
        {
            var ns = entity.ContainingNamespace.FullNamespace();
            sb.AppendLine($"    public static {ns}.{entity.Name}ReadOnly ToReadOnly(this {ns}.{entity.Name} entity)");
            sb.AppendLine($"        => new(");

            var publicProperties = entity.PublicProperties().ToArray();
            for (var i = 0; i < publicProperties.Length; i++)
            {
                var prop = publicProperties[i];
                sb.Append($"            entity.{prop.Name}");

                if (ShouldBeMadeReadOnly(prop.Type))
                {
                    if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                        sb.Append("?");

                    sb.Append(".ToReadOnly()");
                }

                var iRol = prop.Type.AllInterfaces.FirstOrDefault(i => i.Name == "IReadOnlyList");
                if (iRol != null && ShouldBeMadeReadOnly(iRol.TypeArguments[0]))
                    sb.Append(".Select(e => e.ToReadOnly()).ToArray()");

                if (i < publicProperties.Length - 1)
                    sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine($"        );");
        }

        sb.AppendLine("}");

        context.AddSource("ReadOnlyExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }


    private void GenerateReadOnly(GeneratorExecutionContext context, ITypeSymbol entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace Example.Domain;");
        sb.AppendLine();
        sb.AppendLine($"public record {entity.Name}ReadOnly(");

        var publicProperties = entity.PublicProperties().ToArray();
        for (var i = 0; i < publicProperties.Length; i++)
        {
            var prop = publicProperties[i];

            sb.Append($"    ")
                .Append(prop.Type.ContainingNamespace.FullNamespace())
                .Append(".")
                .Append(GetDesiredType(prop.Type));

            if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                sb.Append("?");

            sb.Append(" ").Append(prop.Name);

            if (i < publicProperties.Length - 1)
                sb.Append(",");
            sb.AppendLine();
        }

        sb.AppendLine(");");

        context.AddSource($"{entity.Name}ReadOnly.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private string GetDesiredType(ITypeSymbol propType)
    {
        if (ShouldBeMadeReadOnly(propType))
            return propType.Name + "ReadOnly";

        var iRol = propType.AllInterfaces.FirstOrDefault(i => i.Name == "IReadOnlyList");
        if (iRol != null)
            return $"IReadOnlyList<{GetDesiredType(iRol.TypeArguments[0])}>";

        return propType.Name;
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        public IList<ClassDeclarationSyntax> AllClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax cds)
                AllClasses.Add(cds);
        }
    }
}