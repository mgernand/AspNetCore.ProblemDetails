namespace AspNetCore.ProblemDetails
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.Infrastructure;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.Extensions.Options;

	[UsedImplicitly]
	internal sealed class CustomProblemDetailsFactory : ProblemDetailsFactory
	{
		private readonly ProblemDetailsFactory defaultProblemDetailsFactory;
		private readonly ProblemDetailsOptions options;

		public CustomProblemDetailsFactory(
			IOptions<ProblemDetailsOptions> options,
			ProblemDetailsFactory defaultProblemDetailsFactory)
		{
			this.options = options.Value;
			this.defaultProblemDetailsFactory = defaultProblemDetailsFactory;
		}

		/// <inheritdoc />
		public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
		{
			return this.defaultProblemDetailsFactory.CreateProblemDetails(httpContext, statusCode, title, type, detail, instance);
		}

		/// <inheritdoc />
		public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
		{
			return this.defaultProblemDetailsFactory.CreateValidationProblemDetails(httpContext, modelStateDictionary, statusCode, title, type, detail, instance);
		}

		public ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
		{
			ProblemDetails problemDetails = this.MapStatusCode(httpContext, exception);

			// Add the exception details if requested.
			if(this.options.IncludeExceptionDetails.Invoke(httpContext, exception))
			{
				problemDetails?.AddExceptionDetails(exception);
			}

			return problemDetails;
		}

		private ProblemDetails MapStatusCode(HttpContext httpContext, Exception exception)
		{
			// Try to map the exception to a configured status code.
			if(this.options.TryMapStatusCode(httpContext, exception, out HttpStatusCode? httpStatusCode))
			{
				// Create the problem details.
				int statusCode = (int)httpStatusCode.GetValueOrDefault();
				ProblemDetails problemDetails = this.CreateProblemDetails(httpContext, statusCode);
				httpContext.Response.StatusCode = statusCode;

				return problemDetails;
			}

			return this.CreateProblemDetails(httpContext, httpContext.Response.StatusCode);
		}

		public ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, SerializableError error, int? statusCode)
		{
			IDictionary<string, string[]> errors = error
				.Where(x => x.Value is string[])
				.ToDictionary(x => x.Key, x => (string[])x.Value);

			ModelStateDictionary modelStateDictionary = new ModelStateDictionary();
			foreach((string key, string[] values) in errors)
			{
				foreach(string value in values)
				{
					modelStateDictionary.AddModelError(key, value);
				}
			}

			return this.CreateValidationProblemDetails(httpContext, modelStateDictionary, statusCode);
		}
	}
}
