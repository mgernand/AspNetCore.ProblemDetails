using Fluxera.Guards;
using Microsoft.AspNetCore.Http;

namespace MadEyeMatt.AspNetCore.ProblemDetails
{
    internal static class HttpResponseExtensions
	{
		public static bool HasProblem(this HttpResponse response)
		{
			Guard.Against.Null(response);

			if(!response.StatusCode.IsProblemStatusCode())
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
