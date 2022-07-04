using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator;

[Generator]
public class LookupSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        //Debugger.Launch();

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            throw new Exception("Expected a SyntaxReceiver");


        var cases = receiver.AllClasses
            .Select(c => $"case \"{c.Identifier.ValueText}\": return typeof({c.Identifier.ValueText});");

        var source = $@"
using Example.Domain;

class Lookup2
{{
    public static Type Get(string value)
    {{
        switch (value)
        {{
            {string.Join(Environment.NewLine, cases)}
            default:
                throw new Exception();
        }}
    }}
}}
";

        context.AddSource("Lookup.g.cs", source);
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
