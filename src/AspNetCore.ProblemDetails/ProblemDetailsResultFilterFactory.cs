namespace MadEyeMatt.AspNetCore.ProblemDetails
{
	using System;
	using Microsoft.AspNetCore.Mvc.Filters;
	using Microsoft.Extensions.DependencyInjection;

	internal sealed class ProblemDetailsResultFilterFactory : IFilterFactory, IOrderedFilter
	{
		public bool IsReusable => true;

		public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
		{
			return ActivatorUtilities.CreateInstance<ProblemDetailsResultFilter>(serviceProvider);
		}

		/// <summary>
		///     The same order as the built-in ClientErrorResultFilterFactory.
		/// </summary>
		public int Order => -2000;
	}
}
