﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using AsyncGenerator.Tests.Preconditions.Input;

namespace AsyncGenerator.Tests.Preconditions
{
	[TestFixture]
	public class Fixture : BaseFixture<TestCase>
	{
		[Test]
		public Task TestAfterAnalyzation()
		{
			var divide = GetMethodName(o => o.Divide(0, 0));
			var divideShort = GetMethodName(o => o.DivideShort(0, 0));
			var readFile = GetMethodName(o => o.ReadFile(null));

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(2, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);

				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(3, types[nameof(TestCase)].Methods.Count);
				var methods = types[nameof(TestCase)].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].Preconditions.Count);
				Assert.IsFalse(methods[readFile].SplitTail);

				Assert.AreEqual(0, methods[divideShort].Preconditions.Count);
				Assert.IsFalse(methods[divideShort].SplitTail);

				Assert.AreEqual(1, methods[divide].Preconditions.Count);
				Assert.IsTrue(methods[divide].SplitTail);

				foreach (var method in methods.Values)
				{
					Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				}
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile(nameof(TestCase)), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestLocalFunctionAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseLocalFunctions"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCustomPreconditionCheckerAfterAnalyzation()
		{
			var divide = GetMethodName(o => o.Divide(0, 0));
			var divideShort = GetMethodName(o => o.DivideShort(0, 0));
			var readFile = GetMethodName(o => o.ReadFile(null));

			void AfterAnalyzation(IProjectAnalyzationResult result)
			{
				Assert.AreEqual(1, result.Documents.Count);
				Assert.AreEqual(1, result.Documents[0].GlobalNamespace.NestedNamespaces.Count);
				Assert.AreEqual(2, result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.Count);

				var types = result.Documents[0].GlobalNamespace.NestedNamespaces[0].Types.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(3, types[nameof(Input.TestCase)].Methods.Count);
				var methods = types[nameof(Input.TestCase)].Methods.ToDictionary(o => o.Symbol.Name);

				Assert.AreEqual(1, methods[readFile].Preconditions.Count);
				Assert.IsFalse(methods[readFile].SplitTail);

				Assert.AreEqual(1, methods[divideShort].Preconditions.Count);
				Assert.IsTrue(methods[divideShort].SplitTail);

				Assert.AreEqual(1, methods[divide].Preconditions.Count);
				Assert.IsTrue(methods[divide].SplitTail);

				foreach (var method in methods.Values)
				{
					Assert.AreEqual(MethodConversion.ToAsync, method.Conversion);
				}
			}

			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.IsPrecondition((statement, semanticModel) =>
					{
						var expressionStatement = statement as ExpressionStatementSyntax;
						var invocationExpression = expressionStatement?.Expression as InvocationExpressionSyntax;
						if (invocationExpression == null)
						{
							return false;
						}
						var symbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol;
						return symbol?.ContainingType?.Name == "Requires";
					})
					.AfterAnalyzation(AfterAnalyzation)
				)
			);
		}

		[Test]
		public Task TestCustomPreconditionCheckerAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.IsPrecondition((statement, semanticModel) =>
					{
						var expressionStatement = statement as ExpressionStatementSyntax;
						var invocationExpression = expressionStatement?.Expression as InvocationExpressionSyntax;
						if (invocationExpression == null)
						{
							return false;
						}
						var symbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol;
						return symbol?.ContainingType?.Name == "Requires";
					})
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseCustomChecker"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}

		[Test]
		public Task TestCancellationTokensLocalFunctionsAfterTransformation()
		{
			return ReadonlyTest(p => p
				.ConfigureAnalyzation(a => a
					.MethodConversion(symbol => MethodConversion.Smart)
					.CancellationTokens(true)
				)
				.ConfigureTransformation(t => t
					.LocalFunctions(true)
					.AfterTransformation(result =>
					{
						Assert.AreEqual(1, result.Documents.Count);
						var document = result.Documents[0];
						Assert.NotNull(document.OriginalModified);
						Assert.AreEqual(GetOutputFile("TestCaseLocalFunctionsWithTokens"), document.Transformed.ToFullString());
					})
				)
			);
		}
	}
}
