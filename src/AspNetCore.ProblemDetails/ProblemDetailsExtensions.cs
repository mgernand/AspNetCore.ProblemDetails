using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MadEyeMatt.AspNetCore.ProblemDetails
{
    internal static class ProblemDetailsExtensions
	{
		public static void AddExceptionDetails(this Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails, Exception exception)
		{
			problemDetails.Detail ??= exception.Message;
			problemDetails.Instance ??= GetHelpLink(exception);
			problemDetails.Extensions.TryAdd("exception", GetExceptionDetails(exception));
		}

		public static ObjectResult CreateResult(this Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)
		{
			ObjectResult result = new ObjectResult(problemDetails)
			{
				StatusCode = problemDetails.Status,
				ContentTypes = new MediaTypeCollection
				{
					"application/problem+json",
					"application/problem+xml"
				}
			};

			return result;
		}

		private static ExceptionDetails GetExceptionDetails(Exception exception)
		{
			return new ExceptionDetails(exception);
		}

		private static string GetHelpLink(Exception exception)
		{
			string link = exception.HelpLink;

			if(string.IsNullOrWhiteSpace(link))
			{
				return null;
			}

			if(Uri.TryCreate(link, UriKind.Absolute, out Uri result))
			{
				return result.ToString();
			}

			return null;
		}

		private sealed class ExceptionDetails
		{
			public ExceptionDetails(Exception exception)
			{
				this.Message = exception?.Message;
				this.Type = exception?.GetType().Name;
				this.StackTrace = exception?.StackTrace;

				if(exception?.InnerException is not null)
				{
					this.InnerException = new ExceptionDetails(exception.InnerException);
				}
			}

			public string Message { get; }

			public string Type { get; }

			public string StackTrace { get; }

			public ExceptionDetails InnerException { get; }
		}
	}
}
