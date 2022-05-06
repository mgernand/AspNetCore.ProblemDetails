namespace AspNetCore.ProblemDetails
{
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.Formatters;

	internal static class Util
	{
		// A status code between (including) 400 and 599 is a problem.
		public static bool IsProblemStatusCode(int? statusCode)
		{
			if(statusCode is < 400 or > 599 or null)
			{
				return false;
			}

			return true;
		}

		public static ObjectResult CreateResult(ProblemDetails problemDetails)
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
	}
}
