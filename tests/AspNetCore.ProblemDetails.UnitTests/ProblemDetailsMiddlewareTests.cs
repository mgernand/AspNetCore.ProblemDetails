namespace AspNetCore.ProblemDetails.UnitTests
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.TestHost;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Logging;
	using NUnit.Framework;

	[TestFixture]
	public class Tests
	{
		public static IEnumerable<object[]> StatusCodeTestCases()
		{
			HttpStatusCode[] statusCodes = Enum.GetValues<HttpStatusCode>();
			foreach(HttpStatusCode httpStatusCode in statusCodes)
			{
				int code = (int)httpStatusCode;
				if(code >= 400 && code < 599)
				{
					yield return new object[] { httpStatusCode, true };
				}
				else
				{
					yield return new object[] { httpStatusCode, false };
				}
			}
		}

		private static HttpClient CreateHttpClient(RequestDelegate handler = null, Action<ProblemDetailsOptions> configureAction = null)
		{
			IWebHostBuilder builder = new WebHostBuilder()
				.UseEnvironment(Environments.Development)
				.ConfigureLogging(logging =>
				{
					logging.AddConsole();
				})
				.ConfigureServices(services =>
				{
					services.AddLogging();
					services.AddCors();
					services.AddControllers()
						.AddProblemDetails(configureAction)
						.AddControllersAsServices();
				})
				.Configure(app =>
				{
					app.UseCors(builder => builder.AllowAnyOrigin());
					app.UseProblemDetails();
					app.UseRouting();
					app.UseEndpoints(builder =>
					{
						builder.MapControllers();
						if(handler != null)
						{
							builder.MapGet("/", handler);
						}
					});
				});

			TestServer server = new TestServer(builder);

			HttpClient httpClient = server.CreateClient();
			httpClient.DefaultRequestHeaders.Accept.Clear();
			httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/problem+json");

			return httpClient;
		}

		private static RequestDelegate ResponseWithStatusCode(HttpStatusCode statusCode)
		{
			return context =>
			{
				context.Response.StatusCode = (int)statusCode;
				return Task.CompletedTask;
			};
		}

		private static RequestDelegate ResponseThrowsException(Exception exception = null)
		{
			return context => Task.FromException(exception ?? new InvalidOperationException());
		}

		public static IEnumerable<object[]> ExceptionTestCases()
		{
			yield return new object[] { new NotImplementedException(), HttpStatusCode.NotImplemented };
			yield return new object[] { new InvalidOperationException(), HttpStatusCode.UnprocessableEntity };
			yield return new object[] { new Exception(), HttpStatusCode.InternalServerError };
			yield return new object[] { new NullReferenceException(), HttpStatusCode.InternalServerError };
		}

		[Test]
		public async Task ShouldHandleException()
		{
			using(HttpClient httpClient = CreateHttpClient(ResponseThrowsException()))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
				response.Should().BeProblemDetails(true);
			}
		}

		[Test]
		[TestCaseSource(nameof(StatusCodeTestCases))]
		public async Task ShouldHandleProblemStatusCode(HttpStatusCode statusCode, bool isProblem)
		{
			using(HttpClient httpClient = CreateHttpClient(ResponseWithStatusCode(statusCode)))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(statusCode);

				if(isProblem)
				{
					response.Should().BeProblemDetails(false);
				}
			}
		}

		[Test]
		[TestCaseSource(nameof(ExceptionTestCases))]
		public async Task ShouldHandleStatusCodes(Exception exception, HttpStatusCode expectedStatusCode)
		{
			using(HttpClient httpClient = CreateHttpClient(ResponseThrowsException(exception),
					  options =>
					  {
						  options.StatusCode<NotImplementedException>(HttpStatusCode.NotImplemented);
						  options.StatusCode<InvalidOperationException>(HttpStatusCode.UnprocessableEntity);
						  options.StatusCode<Exception>(HttpStatusCode.InternalServerError);
					  }))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(expectedStatusCode);
				response.Should().BeProblemDetails(true);
			}
		}

		[Test]
		public async Task ShouldIgnoreException()
		{
			using(HttpClient httpClient = CreateHttpClient(ResponseThrowsException(new NotImplementedException()),
					  options =>
					  {
						  options.Ignore<NotImplementedException>();
					  }))
			{
				Func<Task> func = async () => await httpClient.GetAsync(string.Empty);
				await func.Should().ThrowAsync<NotImplementedException>();
			}
		}
	}
}
