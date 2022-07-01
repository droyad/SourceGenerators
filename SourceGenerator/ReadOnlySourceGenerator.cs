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
        var ns = symbol.ContainingNamespace.FullNamespace();
        return ns.StartsWith("Example.Domain") &&
               symbol.IsReferenceType &&
               !symbol.IsRecord;
    }


    private void GenerateReadOnly(GeneratorExecutionContext context, ITypeSymbol entity)
    {
        var publicProperties = entity.PublicProperties().ToArray();
        var pureMethods = entity.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Where(m => m.DeclaredAccessibility.HasFlag(Accessibility.Public))
            .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.Name == "PureAttribute"))
            .ToArray();


        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace Example.Domain;");
        sb.AppendLine();

        sb.AppendLine($"public interface IReadOnly{entity.Name} {{");

        foreach (var prop in publicProperties)
        {
            sb.Append($"    ")
                .Append(prop.Type.ContainingNamespace.FullNamespace())
                .Append(".")
                .Append(GetDesiredType(prop.Type));

            if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                sb.Append("?");

            sb.Append(" ")
                .Append(prop.Name)
                .AppendLine(" { get; }");
        }

        foreach (var method in pureMethods)
        {
            sb.Append($"    ")
                .Append(method.ReturnType.ContainingNamespace.FullNamespace())
                .Append(".")
                .Append(GetDesiredType(method.ReturnType));

            if (method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated)
                sb.Append("?");

            sb.Append(" ")
                .Append(method.Name)
                .AppendLine("(");

            for (var index = 0; index < method.Parameters.Length; index++)
            {
                var param = method.Parameters[index];
                sb.Append($"        ")
                    .Append(param.Type.ContainingNamespace.FullNamespace())
                    .Append(".")
                    .Append(param.Type.Name);

                if (param.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    sb.Append("?");

                sb.Append(" ")
                    .Append(param.Name);

                if (index != method.Parameters.Length - 1)
                    sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("    );");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"public partial class {entity.Name} : IReadOnly{entity.Name} {{");
        sb.AppendLine();

        foreach (var prop in publicProperties)
        {
            sb.Append($"    ")
                .Append(prop.Type.ContainingNamespace.FullNamespace())
                .Append(".")
                .Append(GetDesiredType(prop.Type));

            if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                sb.Append("?");

            sb.Append(" IReadOnly")
                .Append(entity.Name)
                .Append(".")
                .Append(prop.Name)
                .Append(" => this.")
                .Append(prop.Name)
                .AppendLine(";");
        }

        foreach (var method in pureMethods)
        {
            sb.Append($"    ")
                .Append(method.ReturnType.ContainingNamespace.FullNamespace())
                .Append(".")
                .Append(GetDesiredType(method.ReturnType));

            if (method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated)
                sb.Append("?");

            sb.Append(" IReadOnly")
                .Append(entity.Name)
                .Append(".")
                .Append(method.Name)
                .AppendLine("(");

            for (var index = 0; index < method.Parameters.Length; index++)
            {
                var param = method.Parameters[index];
                sb.Append($"        ")
                    .Append(param.Type.ContainingNamespace.FullNamespace())
                    .Append(".")
                    .Append(param.Type.Name);

                if (param.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    sb.Append("?");

                sb.Append(" ")
                    .Append(param.Name);

                if (index != method.Parameters.Length - 1)
                    sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("    )");

            sb.Append("    => this.")
                .Append(method.Name)
                .AppendLine("(");
                
            for (var index = 0; index < method.Parameters.Length; index++)
            {
                var param = method.Parameters[index];
                sb.Append($"        ")
                    .Append(param.Name);

                if (index != method.Parameters.Length - 1)
                    sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("    );");
        }

        sb.AppendLine("}");

        context.AddSource($"IReadOnly{entity.Name}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private string GetDesiredType(ITypeSymbol propType)
    {
        if (ShouldBeMadeReadOnly(propType))
            return "IReadOnly" + propType.Name;

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