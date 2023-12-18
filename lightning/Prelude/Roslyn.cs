#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
    using Operand = System.UInt16;
using System.Collections.Generic;
using System.IO;


#else
    using Float = System.Single;
    using Integer = System.Int32;
    using Operand = System.UInt16;
#endif

#if ROSLYN
using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
#endif

using System;

namespace lightning
{
    public class Roslyn
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit roslyn = new TableUnit(null);

            Unit CSharpScriptCompile(VM p_vm)
            {
                string name = p_vm.GetString(0);
                Float arity = p_vm.GetInteger(1);
                string body = p_vm.GetString(2);

                var options = ScriptOptions.Default.AddReferences(
                    typeof(Unit).Assembly,
                    typeof(VM).Assembly).WithImports(); // "lightning", "System");

                Func<VM, Unit> new_func = CSharpScript.EvaluateAsync<Func<VM, Unit>>(body, options)
                .GetAwaiter().GetResult();

                return new Unit(new IntrinsicUnit(name, new_func, (Operand)arity));
            }
            roslyn.Set("csharpscript_compile", new IntrinsicUnit("csharpscript_compile", CSharpScriptCompile, 3));

            Unit CSharpScriptEval(VM p_vm)
            {
                string body = p_vm.GetString(0);

                var result = CSharpScript.EvaluateAsync(body)
                    .GetAwaiter().GetResult();

                return Unit.FromObject(result);
            }
            roslyn.Set("csharp_eval", new IntrinsicUnit("csharp_eval", CSharpScriptEval, 1));

            Unit GetAllDotNetAssemblies(VM p_vm)
            {
                List<Unit> assemblies_list = new List<Unit>();
                foreach (string dll in Directory.GetFiles(Defaults.Config.AssembliesPath, "*.dll"))
                {
                    assemblies_list.Add(new Unit(dll.ToString()));
                }

                return new Unit(new ListUnit(assemblies_list));
            }
            roslyn.Set("get_available_assemblies", new IntrinsicUnit("get_available_assemblies", GetAllDotNetAssemblies, 0));

            Unit GetReferencesFromList(VM p_vm)
            {
                List<Unit> assemblies_list = (p_vm.GetList(0)).Elements;

                List<Unit> references_list = new List<Unit>();
                foreach (Unit assembly_name in assemblies_list )
                {
                    references_list.Add(new Unit(new WrapperUnit<PortableExecutableReference>(MetadataReference.CreateFromFile(assembly_name.ToString()))));
                }

                return new Unit(new ListUnit(references_list));
            }
            roslyn.Set("get_references_from_assemblies_list", new IntrinsicUnit("get_references_from_assemblies_list", GetReferencesFromList, 1));

            Unit CSharpCompile(VM p_vm)
            {
                string name = p_vm.GetString(0);
                Float arity = p_vm.GetInteger(1);
                string body = p_vm.GetString(2);

                var tree = SyntaxFactory.ParseSyntaxTree(body);
                string file_name = name + ".dll";

                var refs = new List<PortableExecutableReference> {
                    MetadataReference.CreateFromFile(typeof(Unit).GetTypeInfo().Assembly.Location)
                    , MetadataReference.CreateFromFile(AppDomain.CurrentDomain.BaseDirectory + "refs/" + "System.Runtime.dll")
                    , MetadataReference.CreateFromFile(AppDomain.CurrentDomain.BaseDirectory + "refs/" + "System.Console.dll")
                    , MetadataReference.CreateFromFile(AppDomain.CurrentDomain.BaseDirectory + "refs/" + "System.Core.dll")
                };

                var compilation = CSharpCompilation.Create(file_name)
                    .WithOptions(
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    )
                    .AddReferences(refs)
                    .AddSyntaxTrees(tree);

                string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), file_name);

                // Emit to disk
                // EmitResult compilationResult = compilation.Emit(path);
                var ms = new MemoryStream();
                var compilationResult = compilation.Emit(ms);
                if(compilationResult.Success)
                {
                    // Load the assembly from file
                    // Assembly ourAssembly =
                    // AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

                    // Load from memory
                    // Load into currently running assembly. Normally we'd probably want to do this in an AppDomain
                    var ourAssembly = Assembly.Load(ms.ToArray());

                    // // Create func
                    // MethodInfo func_1 = ourAssembly.GetType("RoslynCore.Helper").GetMethod("CalculateCircleArea");

                    // // Invoke the RoslynCore.Helper.CalculateCircleArea method passing an argument
                    // double radius = 10;
                    // object result = func_1.Invoke(null, new object[] { radius });
                    // Console.WriteLine($"Circle area with radius = {radius} is {result}");

                    MethodInfo func_2 = ourAssembly.GetType("RoslynCore.Helper").GetMethod(name);
                    Unit ex_func_2 = new Unit(new ExternalFunctionUnit(name, func_2, (Operand)arity));
                    return ex_func_2;
                }
                else
                {
                    foreach (Diagnostic codeIssue in compilationResult.Diagnostics)
                    {
                        string issue = $"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()}, Location: {codeIssue.Location.GetLineSpan()}, Severity: {codeIssue.Severity}";
                    // Console.WriteLine(issue);
                        Logger.LogLine(issue, Defaults.Config.VMLogFile);
                    }
                }

                return new Unit(UnitType.Null);
                // return new Unit(new IntrinsicUnit(name, new_func, (Operand)arity));
            }
            roslyn.Set("compile", new IntrinsicUnit("compile", CSharpCompile, 3));

            return roslyn;
        }
    }
}