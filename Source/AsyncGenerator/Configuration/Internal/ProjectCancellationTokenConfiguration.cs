﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Core;
using AsyncGenerator.Core.Analyzation;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectCancellationTokenConfiguration : IFluentProjectCancellationTokenConfiguration, IProjectCancellationTokenConfiguration
	{
		public bool Enabled { get; internal set; }

		public bool Guards { get; private set; }

		public Func<IMethodSymbolInfo, MethodCancellationToken> MethodGeneration { get; private set; } =
			symbolInfo => symbolInfo.Symbol.ExplicitInterfaceImplementations.Any()
				? MethodCancellationToken.Required
				: MethodCancellationToken.Optional;

		public Func<IMethodSymbol, bool?> RequiresCancellationToken { get; private set; } = symbol => null;

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.Guards(bool value)
		{
			Guards = value;
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.ParameterGeneration(Func<IMethodSymbolInfo, MethodCancellationToken> func)
		{
			MethodGeneration = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		IFluentProjectCancellationTokenConfiguration IFluentProjectCancellationTokenConfiguration.RequiresCancellationToken(Func<IMethodSymbol, bool?> func)
		{
			RequiresCancellationToken = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}
	}
}
