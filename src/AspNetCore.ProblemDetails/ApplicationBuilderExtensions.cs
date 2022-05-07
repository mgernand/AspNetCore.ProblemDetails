namespace AspNetCore.ProblemDetails
{
	using System;
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Mvc.Infrastructure;
	using Microsoft.Extensions.DependencyInjection;

	/// <summary>
	///     Extension methods for the <see cref="IApplicationBuilder" /> type.
	/// </summary>
	[PublicAPI]
	public static class ApplicationBuilderExtensions
	{
		/// <summary>
		///     Adds the <see cref="ProblemDetailsMiddleware" /> to the application's pipeline.
		/// </summary>
		/// <param name="app">The application builder.</param>
		/// <returns></returns>
		public static IApplicationBuilder UseProblemDetails(this IApplicationBuilder app)
		{
			ProblemDetailsFactory problemDetailsFactory = app.ApplicationServices.GetService<ProblemDetailsFactory>();
			if(problemDetailsFactory is not CustomProblemDetailsFactory)
			{
				throw new InvalidOperationException("The problem details middleware services have not been added.");
			}

			return app.UseMiddleware<ProblemDetailsMiddleware>();
		}
	}
}
