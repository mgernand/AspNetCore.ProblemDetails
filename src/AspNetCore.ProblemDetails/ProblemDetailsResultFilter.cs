namespace AspNetCore.ProblemDetails
{
	using System;
	using System.Net;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.Filters;
	using Microsoft.AspNetCore.Mvc.Infrastructure;
	using Microsoft.Extensions.Options;

	internal sealed class ProblemDetailsResultFilter : IAlwaysRunResultFilter, IOrderedFilter
	{
		private readonly ProblemDetailsOptions options;
		private readonly CustomProblemDetailsFactory problemDetailsFactory;

		public ProblemDetailsResultFilter(
			ProblemDetailsFactory problemDetailsFactory,
			IOptions<ProblemDetailsOptions> options)
		{
			this.problemDetailsFactory = (CustomProblemDetailsFactory)problemDetailsFactory;
			this.options = options.Value;
		}

		/// <inheritdoc />
		public void OnResultExecuting(ResultExecutingContext context)
		{
			// Only handle ObjectResult.
			if(context.Result is not ObjectResult result)
			{
				return;
			}

			if(result.Value is ProblemDetails)
			{
				return;
			}

			// This occurs most likely as a result of calling (some subclass of)
			// ObjectResult(ModelState) which indicates a validation error.
			if(result.Value is SerializableError error)
			{
				ProblemDetails problemDetails = this.problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, error, result.StatusCode);
				context.Result = Util.CreateResult(problemDetails);
				return;
			}

			// Make sure the result should be treated as a problem.
			if(!Util.IsProblemStatusCode(result.StatusCode))
			{
				return;
			}

			// If the result is a string, we treat it as the "detail" property.
			if(result.Value is string detail)
			{
				ProblemDetails problemDetails = this.problemDetailsFactory.CreateProblemDetails(context.HttpContext, result.StatusCode, detail: detail);
				context.Result = Util.CreateResult(problemDetails);
				return;
			}

			// The result is an exception: treat it as if it has been thrown.
			if(result.Value is Exception exception)
			{
				// Set the response status code because it might be used for mapping inside the factory.
				context.HttpContext.Response.StatusCode = result.StatusCode ?? (int)HttpStatusCode.InternalServerError;

				ProblemDetails problemDetails = this.problemDetailsFactory.CreateProblemDetails(context.HttpContext, exception);

				// Developers may choose to ignore errors by returning null.
				if(problemDetails is null)
				{
					return;
				}

				context.Result = Util.CreateResult(problemDetails);
			}
		}

		/// <inheritdoc />
		void IResultFilter.OnResultExecuted(ResultExecutedContext context)
		{
			// Intentionally left blank.
		}

		/// <summary>
		///     Order is set to 1 so that execution is after <see cref="ProducesAttribute" />,
		///     which clears and sets ObjectResult.ContentTypes.
		/// </summary>
		public int Order => 1;
	}
}
