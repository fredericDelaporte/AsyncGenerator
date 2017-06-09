using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;

namespace AsyncGenerator.Tests.ExternalProjects.NHibernate
{
	/// <summary>
	/// Transformation for the NHibernate project. 
	/// Before running the test the following steps needs to be done:
	///		- Fetch the NHibernate submodule
	///		- Run "Source/ShowBuildMenu.bat" script with the option "A" to generate the SharedAssembly.cs
	///		- Restore nuget packages for the NHibernate solution
	///		- Run the test
	/// </summary>
	[TestFixture]
	public class Fixture : BaseFixture
	{
		[Explicit]
		[Test]
		public async Task PerformanceTest()
		{
			var slnFilePath = Path.GetFullPath(GetBaseDirectory() + @"..\..\ExternalProjects\NHibernate\Source\src\NHibernate.sln");
			var props = new Dictionary<string, string>
			{
				["CheckForSystemRuntimeDependency"] = "true"
			};
			var workspace = MSBuildWorkspace.Create(props);
			var solution = await workspace.OpenSolutionAsync(slnFilePath).ConfigureAwait(false);
			var project = solution.Projects.First(o => o.Name == "NHibernate");
			var methodsList = await Task.WhenAll(project.Documents.Select(GetDocumentMethods));
			var methods = methodsList.SelectMany(o => o).Select(o => o.OriginalDefinition).ToList();
			var searchableProjects = new[] {project}.ToImmutableHashSet();

			var interfaceMethods = methods.Where(o => o.ContainingType.TypeKind == TypeKind.Interface).ToList();
			var abstractMethods = methods.Where(o => o.ContainingType.TypeKind != TypeKind.Interface && (o.IsAbstract || o.IsVirtual)).ToList();
			var sw = new Stopwatch();

			// Test FindImplementationsAsync performance
			Console.WriteLine($"Start calling sequentially FindImplementationsAsync for {interfaceMethods.Count} interface methods");
			sw.Start();
			foreach (var interfaceMethod in interfaceMethods)
			{
				await SymbolFinder.FindImplementationsAsync(interfaceMethod, solution, searchableProjects);
			}
			Console.WriteLine($"Total elapsed time {sw.ElapsedMilliseconds}ms");

			Console.WriteLine($"Start calling concurrently FindImplementationsAsync for {interfaceMethods.Count} interface methods");
			sw.Restart();
			foreach (var interfaceMethod in interfaceMethods)
			{
				await Task.WhenAll(SymbolFinder.FindImplementationsAsync(interfaceMethod, solution, searchableProjects));
			}
			Console.WriteLine($"Total elapsed time {sw.ElapsedMilliseconds}ms");


			// Test FindImplementationsAsync performance
			Console.WriteLine($"Start calling sequentially FindOverridesAsync for {abstractMethods.Count} abstract/virtual methods");
			sw.Restart();
			foreach (var abstractMethod in abstractMethods)
			{
				await SymbolFinder.FindOverridesAsync(abstractMethod, solution, searchableProjects);
			}
			Console.WriteLine($"Total elapsed time {sw.ElapsedMilliseconds}ms");

			Console.WriteLine($"Start calling concurrently FindOverridesAsync for {abstractMethods.Count} abstract/virtual methods");
			sw.Restart();
			foreach (var abstractMethod in abstractMethods)
			{
				await Task.WhenAll(SymbolFinder.FindOverridesAsync(abstractMethod, solution, searchableProjects));
			}
			Console.WriteLine($"Total elapsed time {sw.ElapsedMilliseconds}ms");

		}

		private async Task<List<IMethodSymbol>> GetDocumentMethods(Document document)
		{
			var rootNode = (CompilationUnitSyntax)await document.GetSyntaxRootAsync();
			var semanticModel = await document.GetSemanticModelAsync();
			return rootNode.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Select(o => semanticModel.GetDeclaredSymbol(o))
				.ToList();
		}

	}
}
