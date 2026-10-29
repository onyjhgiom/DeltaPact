using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class SourceValidator
{
    public static int Main(string[] arguments)
    {
        if (arguments.Length != 1) return 2;
        string root = Path.GetFullPath(arguments[0]);
        string assets = Path.Combine(root, "Assets");
        string[] files = Directory.GetFiles(assets, "*.cs", SearchOption.AllDirectories);
        int errors = 0;
        var variants = new[] { new[] { "UNITY_EDITOR", "ENABLE_INPUT_SYSTEM" }, new[] { "ENABLE_INPUT_SYSTEM" }, new string[0] };
        foreach (string[] symbols in variants)
        {
            var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: symbols);
            foreach (string file in files)
            {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, file);
                foreach (Diagnostic diagnostic in tree.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error))
                {
                    errors++;
                    Console.Error.WriteLine(diagnostic.ToString());
                }
            }
        }
        Console.WriteLine("C# 9 syntax check: " + files.Length + " Unity source files x " + variants.Length + " define variants; " + errors + " syntax errors.");
        Console.WriteLine("This is NOT Unity assembly compilation, API/type checking, Play Mode testing or a Windows game build.");
        return errors == 0 ? 0 : 1;
    }
}
