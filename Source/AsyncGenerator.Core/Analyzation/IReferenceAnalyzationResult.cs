﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator.Core.Analyzation
{
	public interface IReferenceAnalyzationResult
	{
		SimpleNameSyntax ReferenceNameNode { get; }

		ReferenceLocation ReferenceLocation { get; }

		bool IsCref { get; }

		bool IsTypeOf { get; }

		bool IsNameOf { get; }
	}

	public interface IReferenceAnalyzationResult<out TSymbol> : IReferenceAnalyzationResult where TSymbol : ISymbol
	{
		TSymbol ReferenceSymbol { get; }
	}
}
