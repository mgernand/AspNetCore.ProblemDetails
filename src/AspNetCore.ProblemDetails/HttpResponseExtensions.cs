namespace AspNetCore.ProblemDetails
{
	using Fluxera.Guards;
	using Microsoft.AspNetCore.Http;

	internal static class HttpResponseExtensions
	{
		public static bool HasProblem(this HttpResponse response)
		{
			Guard.Against.Null(response);

			if(!Util.IsProblemStatusCode(response.StatusCode))
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
