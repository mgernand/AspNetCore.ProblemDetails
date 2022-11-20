using System.Net;

namespace MadEyeMatt.AspNetCore.ProblemDetails
{
    internal static class HttpStatusCodeExtensions
	{
		/// <summary>
		///     A status code between (including) 400 and 599 is considered a problem.
		/// </summary>
		/// <param name="httpStatusCode"></param>
		/// <returns></returns>
		public static bool IsProblemStatusCode(this HttpStatusCode? httpStatusCode)
		{
			if(!httpStatusCode.HasValue)
			{
				return false;
			}

			return httpStatusCode.Value.IsProblemStatusCode();
		}

		/// <summary>
		///     A status code between (including) 400 and 599 is considered a problem.
		/// </summary>
		/// <param name="httpStatusCode"></param>
		/// <returns></returns>
		public static bool IsProblemStatusCode(this HttpStatusCode httpStatusCode)
		{
			int statusCode = (int)httpStatusCode;
			return statusCode.IsProblemStatusCode();
		}

		/// <summary>
		///     A status code between (including) 400 and 599 is considered a problem.
		/// </summary>
		/// <param name="statusCode"></param>
		/// <returns></returns>
		public static bool IsProblemStatusCode(this int? statusCode)
		{
			if(!statusCode.HasValue)
			{
				return false;
			}

			return statusCode.Value.IsProblemStatusCode();
		}

		/// <summary>
		///     A status code between (including) 400 and 599 is considered a problem.
		/// </summary>
		/// <param name="statusCode"></param>
		/// <returns></returns>
		public static bool IsProblemStatusCode(this int statusCode)
		{
			if(statusCode is < 400 or > 599)
			{
				return false;
			}

			return true;
		}
	}
}
