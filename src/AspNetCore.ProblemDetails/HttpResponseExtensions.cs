namespace AspNetCore.ProblemDetails
{
	using Fluxera.Guards;
	using Microsoft.AspNetCore.Http;

	internal static class HttpResponseExtensions
	{
		public static bool HasProblem(this HttpResponse response)
		{
			Guard.Against.Null(response);

			// A status code between (including) 400 and 599 is a problem.
			if(response.StatusCode < 400 && response.StatusCode > 599)
			{
				return false;
			}

			if(response.ContentLength.HasValue)
			{
				return false;
			}

			if(!string.IsNullOrEmpty(response.ContentType))
			{
				return false;
			}

			return true;
		}
	}
}
