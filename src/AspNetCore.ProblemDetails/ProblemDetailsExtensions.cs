namespace AspNetCore.ProblemDetails
{
	using System;
	using System.Collections.Generic;
	using Microsoft.AspNetCore.Mvc;

	internal static class ProblemDetailsExtensions
	{
		public static void AddExceptionDetails(this ProblemDetails problemDetails, Exception exception)
		{
			problemDetails.Detail ??= exception.Message;
			problemDetails.Instance ??= GetHelpLink(exception);
			problemDetails.Extensions.TryAdd("exception", GetExceptionDetails(exception));
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
