﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace AsyncGenerator
{
	public abstract class FunctionData : IFunctionAnalyzationResult
	{
		protected FunctionData(IMethodSymbol methodSymbol)
		{
			Symbol = methodSymbol;
		}

		//TODO: remove if not needed
		public bool IsAsync { get; set; }

		public IMethodSymbol Symbol { get; }

		public abstract TypeData TypeData { get; }

		public MethodConversion Conversion { get; set; }

		/// <summary>
		/// References of types that are used inside this method
		/// </summary>
		public ConcurrentSet<ReferenceLocation> TypeReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		/// <summary>
		/// References to other methods that are invoked inside this method and are candidates to be async
		/// </summary>
		public ConcurrentSet<ReferenceLocation> MethodReferences { get; } = new ConcurrentSet<ReferenceLocation>();

		public List<StatementSyntax> Preconditions { get; } = new List<StatementSyntax>();

		public abstract SyntaxNode GetNode();

		public abstract SyntaxNode GetBodyNode();

		public abstract IEnumerable<AnonymousFunctionData> GetAnonymousFunctionData();

		public abstract MethodData GetMethodData();

		#region Analyze step

		public ConcurrentSet<FunctionReferenceData> MethodReferenceData { get; } = new ConcurrentSet<FunctionReferenceData>();

		public bool HasYields { get; set; }

		#endregion

		#region Post analyze step

		public bool SplitTail { get; set; }

		public bool OmitAsync { get; set; }

		public bool WrapInTryCatch { get; set; }

		#endregion

		#region IFunctionAnalyzationResult

		private IReadOnlyList<IFunctionReferenceAnalyzationResult> _cachedMethodReferences;
		IReadOnlyList<IFunctionReferenceAnalyzationResult> IFunctionAnalyzationResult.MethodReferences => _cachedMethodReferences ?? (_cachedMethodReferences = MethodReferenceData.ToImmutableArray());

		private IReadOnlyList<ReferenceLocation> _cachedTypeReferences;
		IReadOnlyList<ReferenceLocation> IFunctionAnalyzationResult.TypeReferences => _cachedTypeReferences ?? (_cachedTypeReferences = TypeReferences.ToImmutableArray());

		private IReadOnlyList<StatementSyntax> _cachedPreconditions;
		IReadOnlyList<StatementSyntax> IFunctionAnalyzationResult.Preconditions => _cachedPreconditions ?? (_cachedPreconditions = Preconditions.ToImmutableArray());

		#endregion
	}
}
