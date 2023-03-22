namespace MadEyeMatt.AspNetCore.ProblemDetails
{
	using System;
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.ApplicationModels;
	using Microsoft.AspNetCore.Mvc.Infrastructure;
	using Microsoft.AspNetCore.WebUtilities;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;

	/// <summary>
	///     Extensions methods for the <see cref="IMvcBuilder" /> type.
	/// </summary>
	[PublicAPI]
	public static class MvcBuilderExtensions
	{
		/// <summary>
		///     Adds the required services for the problem details middleware.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="configureAction"></param>
		/// <returns></returns>
		public static IMvcBuilder AddProblemDetails(this IMvcBuilder builder, Action<ProblemDetailsOptions> configureAction = null)
		{
			if(configureAction != null)
			{
				builder.Services.Configure(configureAction);
			}

			ProblemDetailsOptions problemDetailsOptions = new ProblemDetailsOptions();
			configureAction?.Invoke(problemDetailsOptions);

			// Create the complete list of status code mappings.
			builder.ConfigureApiBehaviorOptions(options =>
			{
				// Turn off the built-in client error mapping and use the middleware instead.
				options.SuppressMapClientErrors = true;

				// Create new client error mappings.
				options.ClientErrorMapping.Clear();
				for(int statusCode = 400; statusCode < 600; statusCode++)
				{
					string reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);
					if(!string.IsNullOrWhiteSpace(reasonPhrase))
					{
						options.ClientErrorMapping[statusCode] = new ClientErrorData
						{
							Title = reasonPhrase,
							Link = problemDetailsOptions.CreateProblemLinkUri(statusCode).AbsoluteUri
						};
					}
				}
			});

			// Decorate the default problem detail factory.
			ServiceDescriptor serviceDescriptor = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(ProblemDetailsFactory));
			Type defaultProblemDetailsFactoryType = serviceDescriptor.ImplementationType;

			builder.Services.AddTransient(defaultProblemDetailsFactoryType);
			builder.Services.AddTransient<CustomProblemDetailsFactory>();
			builder.Services.AddTransient<ProblemDetailsFactory>(serviceProvider =>
			{
				IOptions<ProblemDetailsOptions> options = serviceProvider.GetRequiredService<IOptions<ProblemDetailsOptions>>();
				ILogger<ProblemDetailsFactory> logger = serviceProvider.GetRequiredService<ILogger<ProblemDetailsFactory>>();
				ProblemDetailsFactory defaultProblemDetailsFactory = (ProblemDetailsFactory)serviceProvider.GetRequiredService(defaultProblemDetailsFactoryType);

				return new CustomProblemDetailsFactory(options, logger, defaultProblemDetailsFactory);
			});

			// Add a ProducesErrorResponseTypeAttribute to all actions with in controllers with an ApiControllerAttribute
			// Add a result filter that transforms ObjectResult containing a string to ProblemDetails responses.
			builder.Services.AddTransient<IApplicationModelProvider, ProblemDetailsApplicationModelProvider>();

			return builder;
		}
	}
}
