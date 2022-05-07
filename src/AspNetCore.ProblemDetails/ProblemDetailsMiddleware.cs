namespace AspNetCore.ProblemDetails
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Net;
	using System.Runtime.ExceptionServices;
	using System.Threading.Tasks;
	using Microsoft.AspNetCore.Diagnostics;
	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.Abstractions;
	using Microsoft.AspNetCore.Mvc.Infrastructure;
	using Microsoft.AspNetCore.Routing;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;
	using Microsoft.Extensions.Primitives;
	using Microsoft.Net.Http.Headers;

	internal sealed class ProblemDetailsMiddleware
	{
		private const string DiagnosticListenerKey = "Microsoft.AspNetCore.Diagnostics.HandledException";

		private static readonly HashSet<string> AllowedHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			HeaderNames.WWWAuthenticate,
			HeaderNames.StrictTransportSecurity,
			HeaderNames.AccessControlAllowCredentials,
			HeaderNames.AccessControlAllowHeaders,
			HeaderNames.AccessControlAllowMethods,
			HeaderNames.AccessControlAllowOrigin,
			HeaderNames.AccessControlExposeHeaders,
			HeaderNames.AccessControlMaxAge
		};

		private readonly IActionResultExecutor<ObjectResult> actionResultExecutor;
		private readonly DiagnosticListener diagnosticListener;
		private readonly ILogger<ProblemDetailsMiddleware> logger;

		private readonly RequestDelegate next;
		private readonly ProblemDetailsOptions options;
		private readonly CustomProblemDetailsFactory problemDetailsFactory;

		public ProblemDetailsMiddleware(
			RequestDelegate next,
			ILogger<ProblemDetailsMiddleware> logger,
			IOptions<ProblemDetailsOptions> options,
			ProblemDetailsFactory problemDetailsFactory,
			IActionResultExecutor<ObjectResult> actionResultExecutor,
			DiagnosticListener diagnosticListener)
		{
			this.next = next;
			this.logger = logger;
			this.options = options.Value;
			this.problemDetailsFactory = (CustomProblemDetailsFactory)problemDetailsFactory;
			this.actionResultExecutor = actionResultExecutor;
			this.diagnosticListener = diagnosticListener;
		}

		public async Task Invoke(HttpContext httpContext)
		{
			ExceptionDispatchInfo exceptionDispatchInfo = null;

			try
			{
				await this.next(httpContext);

				if(httpContext.Response.HasProblem())
				{
					// Handle Response
					await this.HandleResponseProblemAsync(httpContext);
				}
			}
			catch(Exception ex)
			{
				exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
			}

			// Handle Exception
			if(exceptionDispatchInfo is not null)
			{
				await this.HandleExceptionProblemAsync(httpContext, exceptionDispatchInfo);
			}
		}

		private async Task HandleResponseProblemAsync(HttpContext httpContext)
		{
			// Don't handle the problem if the response was already started.
			if(httpContext.Response.HasStarted)
			{
				this.logger.LogWarning("The response was already started. The problem details middleware will not be executed.");
				return;
			}

			// First we need to prepare the response. Remove headers and status code, consider caching.
			PrepareResponse(httpContext, httpContext.Response.StatusCode);

			// Create the problem details response object.
			ProblemDetails problemDetails = this.problemDetailsFactory.CreateProblemDetails(httpContext, httpContext.Response.StatusCode);

			// Write the result and complete the response.
			await this.WriteProblemDetailsAsync(httpContext, problemDetails);
		}

		private async Task HandleExceptionProblemAsync(HttpContext httpContext, ExceptionDispatchInfo exceptionDispatchInfo)
		{
			// Don't handle the exception if the response was already started.
			if(httpContext.Response.HasStarted)
			{
				this.logger.LogWarning("The response was already started. The problem details middleware will not be executed.");

				// We can't handle the exception, so we re-throw the source exception.
				exceptionDispatchInfo.Throw();
			}

			try
			{
				Exception exception = exceptionDispatchInfo.SourceException;

				// First we need to prepare the response. Remove headers and status code, consider caching.
				PrepareResponse(httpContext, (int)HttpStatusCode.InternalServerError);

				// Add the exception handler feature for the exception.
				ExceptionHandlerFeature feature = new ExceptionHandlerFeature
				{
					Path = httpContext.Request.Path,
					Error = exception
				};
				httpContext.Features.Set<IExceptionHandlerPathFeature>(feature);
				httpContext.Features.Set<IExceptionHandlerFeature>(feature);

				// Create the problem details response object.
				ProblemDetails problemDetails = this.problemDetailsFactory.CreateProblemDetails(httpContext, exception);

				if(problemDetails is not null)
				{
					// Optionally log the unhandled exception.
					if(this.options.LogUnhandledException.Invoke(httpContext, exception, problemDetails))
					{
						this.logger.LogError(exception, "An unhandled exception has occurred while executing the request.");
					}

					// Add the exception details.
					await this.WriteProblemDetailsAsync(httpContext, problemDetails);

					// Write the exception to diagnostic if enabled.
					if(this.diagnosticListener.IsEnabled() && this.diagnosticListener.IsEnabled(DiagnosticListenerKey))
					{
						this.diagnosticListener.Write(DiagnosticListenerKey, new
						{
							httpContext,
							exception
						});
					}

					// The exception was not configured for re-throw, just return from this method.
					if(!this.options.ShouldRethrow(httpContext, exception))
					{
						return;
					}
				}
				else
				{
					// The problem exception was ignored, just log that.
					this.logger.LogInformation("An exception has occurred while executing the request, but it was ignored by exception problem mappings.");
				}
			}
			catch(Exception ex)
			{
				// We failed to write the problem details to the response, so just log the source exception.
				this.logger.LogError(ex, "An exception was thrown attempting to execute the problem details middleware.");
			}

			// We couldn't handle the exception or it is intended, so we re-throw the source exception.
			exceptionDispatchInfo.Throw();
		}

		private async Task WriteProblemDetailsAsync(HttpContext httpContext, ProblemDetails problemDetails)
		{
			RouteData routeData = httpContext.GetRouteData();
			ActionContext actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());

			ObjectResult result = problemDetails.CreateResult();

			await this.actionResultExecutor.ExecuteAsync(actionContext, result);

			await httpContext.Response.CompleteAsync();
		}

		private static void PrepareResponse(HttpContext httpContext, int statusCode)
		{
			HeaderDictionary headers = new HeaderDictionary
			{
				// We make sure that problem responses are never cached.
				[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate",
				[HeaderNames.Pragma] = "no-cache",
				[HeaderNames.Expires] = "0",
				[HeaderNames.ETag] = default
			};

			// Copy any allowed headers, f.e. the CORS headers.
			foreach((string headerName, StringValues headerValue) in httpContext.Response.Headers)
			{
				if(AllowedHeaderNames.Contains(headerName))
				{
					headers.Add(headerName, headerValue);
				}
			}

			// Clear the response and populate with our values.
			httpContext.Response.Clear();
			httpContext.Response.StatusCode = statusCode;

			foreach(KeyValuePair<string, StringValues> header in headers)
			{
				httpContext.Response.Headers.Add(header);
			}
		}
	}
}
