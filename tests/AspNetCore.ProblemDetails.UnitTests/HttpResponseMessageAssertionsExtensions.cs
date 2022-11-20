using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Primitives;
using Fluxera.Utilities;

namespace MadEyeMatt.AspNetCore.ProblemDetails.UnitTests
{
    internal static class HttpResponseMessageAssertionsExtensions
	{
		public static Microsoft.AspNetCore.Mvc.ProblemDetails BeProblemDetails(this HttpResponseMessageAssertions should, bool expectExceptionDetails)
		{
			HttpResponseMessage httpResponseMessage = should.Subject;

			httpResponseMessage.Content.Headers.ContentType.Should().NotBeNull();
			httpResponseMessage.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

			Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = AsyncHelper.RunSync(() => httpResponseMessage.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>());

			string json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			Console.WriteLine(json);

			problemDetails.Should().NotBeNull();
			problemDetails?.Type.Should().NotBeNullOrWhiteSpace();
			problemDetails?.Title.Should().NotBeNullOrWhiteSpace();
			problemDetails?.Status.Should().NotBeNull();

			if(expectExceptionDetails)
			{
				problemDetails?.Extensions.Should().ContainKey("exception");
			}
			else
			{
				problemDetails?.Extensions.Should().NotContainKey("exception");
			}

			return problemDetails;
		}
	}
}
