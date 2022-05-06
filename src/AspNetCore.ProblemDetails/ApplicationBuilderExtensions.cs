namespace AspNetCore.ProblemDetails
{
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Builder;

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
			return app.UseMiddleware<ProblemDetailsMiddleware>();
		}
	}
}
