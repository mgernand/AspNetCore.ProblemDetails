namespace AspNetCore.ProblemDetails
{
	using System;
	using System.Linq;
	using System.Net;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Options;

	internal sealed class ConfigureProblemDetailsOptions : IConfigureOptions<ProblemDetailsOptions>
	{
		/// <inheritdoc />
		public void Configure(ProblemDetailsOptions options)
		{
			options.IncludeExceptionDetails ??= (context, exception) => context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();

			options.LogUnhandledException ??= (context, exception, problemDetails) => problemDetails.Status is not < 500;

			// Define the catch-all mapping if no mapping was configured.
			if(!options.StatusCodeMappings.Any())
			{
				options.StatusCode<Exception>(HttpStatusCode.InternalServerError);
			}
		}
	}
}
