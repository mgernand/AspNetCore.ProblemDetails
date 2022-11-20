using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fluxera.Extensions.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Mock;
using Microsoft.Net.Http.Headers;
using Moq;
using NUnit.Framework;
using ProblemDetailsOptions = MadEyeMatt.AspNetCore.ProblemDetails.ProblemDetailsOptions;

namespace MadEyeMatt.AspNetCore.ProblemDetails.UnitTests
{
	using NameValueHeaderValue = System.Net.Http.Headers.NameValueHeaderValue;

	[TestFixture]
	public class ProblemDetailsMiddlewareTests
	{
		public static IEnumerable<object[]> StatusCodeTestCases()
		{
			HttpStatusCode[] statusCodes = Enum.GetValues<HttpStatusCode>();
			foreach(HttpStatusCode httpStatusCode in statusCodes)
			{
				int code = (int)httpStatusCode;
				if(code is >= 400 and < 599)
				{
					yield return new object[] { httpStatusCode, true };
				}
				else
				{
					yield return new object[] { httpStatusCode, false };
				}
			}
		}

		public Mock<ILogger> MockLogger { get; set; }

		public HttpClient CreateHttpClient(RequestDelegate handler = null, Action<ProblemDetailsOptions> configureAction = null, string environment = null)
		{
			this.MockLogger = new Mock<ILogger>();

			IWebHostBuilder builder = new WebHostBuilder()
				.UseEnvironment(environment ?? Environments.Development)
				.ConfigureLogging(logging =>
				{
					logging.AddConsole();
					logging.AddMock(this.MockLogger);
				})
				.ConfigureServices(services =>
				{
					services.AddLogging();
					services.AddCors();
					services
						.AddControllers()
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

		private static Exception GetInnermostException(Exception exception)
		{
			while(exception.InnerException != null)
			{
				exception = exception.InnerException;
			}

			return exception;
		}

		[Test]
		public async Task ShouldControllerHandleCustomProblemModel()
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync("/api/problem-model");

				response.Should().HaveStatusCode(HttpStatusCode.TooManyRequests);
				response.Should().BeProblemDetails(false);
			}
		}

		[Test]
		public async Task ShouldControllerHandleErrorModel()
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync("/api/error-model");

				response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
				response.Should().BeProblemDetails(true);
			}
		}

		[Test]
		[TestCase(HttpStatusCode.BadRequest)]
		[TestCase(HttpStatusCode.Unauthorized)]
		[TestCase(HttpStatusCode.NotImplemented)]
		[TestCase(HttpStatusCode.ServiceUnavailable)]
		[TestCase(HttpStatusCode.InternalServerError)]
		public async Task ShouldControllerHandleErrorStatusCode(HttpStatusCode httpStatusCode)
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync($"/api/statusCode/{httpStatusCode:D}");

				response.Should().HaveStatusCode(httpStatusCode);
				response.Should().BeProblemDetails(false);
			}
		}

		[Test]
		public async Task ShouldControllerHandleException()
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync("/api/error");

				response.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
				response.Should().BeProblemDetails(true);
			}
		}

		[Test]
		public async Task ShouldControllerHandleInvalidModelState()
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync("/api/statusCode");

				response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
				response.Should().BeProblemDetails(false);
			}
		}

		[Test]
		public async Task ShouldControllerHandleModelState()
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync("/api/validation");

				response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
				response.Should().BeProblemDetails(false);
			}
		}

		[Test]
		public async Task ShouldControllerStringDetail()
		{
			using(HttpClient httpClient = this.CreateHttpClient())
			{
				HttpResponseMessage response = await httpClient.GetAsync("/api/string-detail");

				response.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
				response.Should().BeProblemDetails(false);
			}
		}

		[Test]
		[TestCase(HttpStatusCode.BadRequest)]
		[TestCase(HttpStatusCode.NotFound)]
		[TestCase(HttpStatusCode.Unauthorized)]
		[TestCase(HttpStatusCode.UnprocessableEntity)]
		public async Task ShouldHandleClientErrorStatusCodes(HttpStatusCode httpStatusCode)
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseWithStatusCode(httpStatusCode)))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(httpStatusCode);
				response.Should().BeProblemDetails(false);
			}
		}


		[Test]
		public async Task ShouldHandleException()
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseThrowsException()))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
				response.Should().BeProblemDetails(true);
			}
		}

		[Test]
		public async Task ShouldHandleExceptionWhenRethrowPredicatePrevents()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new ArgumentException("property")),
					  options =>
					  {
						  options.Rethrow<ArithmeticException>();
						  options.Rethrow<ArgumentException>((context, ex) => ex.Message != "property");
					  }))
			{
				await httpClient.GetAsync(string.Empty);
			}
		}

		[Test]
		[TestCase("includeException")]
		[TestCase("excludeException")]
		public async Task ShouldHandleMappingPredicate(string predicateValue)
		{
			const string paramName = "includeException";

			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new ArgumentException(string.Empty, paramName)),
					  options =>
					  {
						  options.Map<ArgumentException>(
							  (context, exception) => exception.ParamName == predicateValue,
							  (context, exception) => HttpStatusCode.UnprocessableEntity);

						  options.MapStatusCode<Exception>(HttpStatusCode.InternalServerError);
					  }))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				if(predicateValue == paramName)
				{
					response.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
					response.Should().BeProblemDetails(true);
				}
				else
				{
					response.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
					response.Should().BeProblemDetails(true);
				}
			}
		}

		[Test]
		[TestCaseSource(nameof(StatusCodeTestCases))]
		public async Task ShouldHandleProblemStatusCode(HttpStatusCode statusCode, bool isProblem)
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseWithStatusCode(statusCode)))
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
		[TestCase(HttpStatusCode.InternalServerError)]
		[TestCase(HttpStatusCode.ServiceUnavailable)]
		[TestCase(HttpStatusCode.NotImplemented)]
		public async Task ShouldHandleServerErrorStatusCodes(HttpStatusCode httpStatusCode)
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseWithStatusCode(httpStatusCode)))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(httpStatusCode);
				response.Should().BeProblemDetails(false);
			}
		}

		[Test]
		[TestCaseSource(nameof(ExceptionTestCases))]
		public async Task ShouldHandleStatusCodes(Exception exception, HttpStatusCode expectedStatusCode)
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(exception),
					  options =>
					  {
						  options.MapStatusCode<NotImplementedException>(HttpStatusCode.NotImplemented);
						  options.MapStatusCode<InvalidOperationException>(HttpStatusCode.UnprocessableEntity);
						  options.MapStatusCode<Exception>(HttpStatusCode.InternalServerError);
					  }))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(expectedStatusCode);
				response.Should().BeProblemDetails(true);
			}
		}

		[Test]
		public Task ShouldIgnoreException()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new NotImplementedException()),
					  options =>
					  {
						  options.Ignore<NotImplementedException>();
					  }))
			{
				Exception exception = Assert.ThrowsAsync<HttpRequestException>(async () => await httpClient.GetAsync(string.Empty));
				Exception innermostException = GetInnermostException(exception);
				innermostException.Should().BeOfType<NotImplementedException>();
			}

			return Task.CompletedTask;
		}

		[Test]
		[TestCase("Development", true)]
		[TestCase("Staging", false)]
		[TestCase("Production", false)]
		public async Task ShouldIncludeExceptionDetailsForEnvironment(string environment, bool expectExceptionDetails)
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseThrowsException(), environment: environment))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = response.Should().BeProblemDetails(expectExceptionDetails);
				problemDetails.Extensions.ContainsKey("exception").Should().Be(expectExceptionDetails);
			}
		}

		[Test]
		public async Task ShouldLogMappedServerException()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new NotImplementedException()),
					  options =>
					  {
						  options.MapStatusCode<NotImplementedException>(HttpStatusCode.NotImplemented);
					  }))
			{
				await httpClient.GetAsync(string.Empty);
				this.MockLogger.VerifyLog().ErrorWasCalled();
			}
		}

		[Test]
		public async Task ShouldLogUnhandledException()
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseThrowsException()))
			{
				await httpClient.GetAsync(string.Empty);
				this.MockLogger.VerifyLog().ErrorWasCalled();
			}
		}

		[Test]
		public async Task ShouldMapCustomProblemDetails()
		{
			ICollection<ValidationError> errors = new List<ValidationError>
			{
				new ValidationError("property")
				{
					ErrorMessages =
					{
						"This property's validation failed. - 1",
						"This property's validation failed. - 2",
						"This property's validation failed. - 3",
						"This property's validation failed. - 4"
					}
				}
			};

			ValidationException validationException = new ValidationException(errors);

			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(validationException),
					  options =>
					  {
						  options.MapStatusCode<ValidationException>(HttpStatusCode.BadRequest,
							  (context, exception, httpStatusCode, problemDetailsFactory) =>
							  {
								  ModelStateDictionary modelState = new ModelStateDictionary();

								  foreach(ValidationError validationError in exception.Errors)
								  {
									  foreach(string errorMessage in validationError.ErrorMessages)
									  {
										  modelState.AddModelError(validationError.PropertyName, errorMessage);
									  }
								  }

								  return problemDetailsFactory.CreateValidationProblemDetails(context, modelState, (int)httpStatusCode);
							  });
					  }))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
				Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = response.Should().BeProblemDetails(true);
				problemDetails?.Extensions.Should().ContainKey("errors");
			}
		}

		[Test]
		public async Task ShouldNotCacheProblemDetailsResponse()
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseThrowsException()))
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, "/");
				request.Headers.Add(HeaderNames.Origin, "localhost");

				HttpResponseMessage response = await httpClient.SendAsync(request);

				response.Headers
					.Any(x => x.Key.StartsWith("Access-Control-Allow-"))
					.Should().BeTrue();
			}
		}

		[Test]
		public Task ShouldNotHandleExceptionAfterStartedResponse()
		{
			static Task WriteResponse(HttpContext context)
			{
				context.Response.WriteAsync("problems");
				throw new InvalidOperationException("Request Failed");
			}

			using(HttpClient httpClient = this.CreateHttpClient(WriteResponse))
			{
				Exception exception = Assert.ThrowsAsync<HttpRequestException>(async () => await httpClient.GetAsync(string.Empty));
				Exception innermostException = GetInnermostException(exception);
				innermostException.Should().BeOfType<InvalidOperationException>();
			}

			return Task.CompletedTask;
		}

		[Test]
		public async Task ShouldNotHandleStartedResponse()
		{
			static Task WriteResponse(HttpContext context)
			{
				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				return context.Response.WriteAsync("problems");
			}

			using(HttpClient httpClient = this.CreateHttpClient(WriteResponse))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Content.Headers.ContentLength.Should().Be(8);
				response.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
			}
		}

		[Test]
		[TestCase(HttpStatusCode.OK)]
		[TestCase(HttpStatusCode.Created)]
		[TestCase(HttpStatusCode.NoContent)]
		[TestCase((HttpStatusCode)100)]
		[TestCase((HttpStatusCode)300)]
		[TestCase((HttpStatusCode)600)]
		[TestCase((HttpStatusCode)900)]
		public async Task ShouldNotHandleSuccessStatusCodes(HttpStatusCode httpStatusCode)
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseWithStatusCode(httpStatusCode)))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(httpStatusCode);
				response.Content.Headers.ContentLength.Should().Be(0);
			}
		}

		[Test]
		public async Task ShouldNotLogMappedClientException()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new NotImplementedException()),
					  options =>
					  {
						  options.MapStatusCode<NotImplementedException>(HttpStatusCode.Forbidden);
					  }))
			{
				await httpClient.GetAsync(string.Empty);
				this.MockLogger.VerifyLog().ErrorWasNotCalled();
			}
		}

		[Test]
		public async Task ShouldPreserveCorsHeader()
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseThrowsException()))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Headers.CacheControl.NoCache.Should().BeTrue();
				response.Headers.CacheControl.NoStore.Should().BeTrue();
				response.Headers.CacheControl.MustRevalidate.Should().BeTrue();
				response.Headers.Pragma.Should().Contain(new NameValueHeaderValue("no-cache"));
			}
		}

		[Test]
		public async Task ShouldPreserveStatusCodeWhenExcludingExceptionDetails()
		{
			using(HttpClient httpClient = this.CreateHttpClient(ResponseThrowsException(), environment: "Production"))
			{
				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
			}
		}

		[Test]
		public Task ShouldRethrowForDerivedExceptionWhenRethrowIsConfigured()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new DivideByZeroException()),
					  options =>
					  {
						  options.Rethrow<ArithmeticException>();
					  }))
			{
				Exception exception = Assert.ThrowsAsync<HttpRequestException>(async () => await httpClient.GetAsync(string.Empty));
				Exception innermostException = GetInnermostException(exception);
				innermostException.Should().BeOfType<DivideByZeroException>();
			}

			return Task.CompletedTask;
		}

		[Test]
		public Task ShouldRethrowWhenRethrowIsConfigured()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new ArithmeticException()),
					  options =>
					  {
						  options.Rethrow<ArithmeticException>();
					  }))
			{
				Exception exception = Assert.ThrowsAsync<HttpRequestException>(async () => await httpClient.GetAsync(string.Empty));
				Exception innermostException = GetInnermostException(exception);
				innermostException.Should().BeOfType<ArithmeticException>();
			}

			return Task.CompletedTask;
		}

		[Test]
		public Task ShouldRethrowWhenRethrowPredicateReturnsTrue()
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(new ArithmeticException()),
					  options =>
					  {
						  options.Rethrow<ArithmeticException>((_, _) => true);
					  }))
			{
				Exception exception = Assert.ThrowsAsync<HttpRequestException>(async () => await httpClient.GetAsync(string.Empty));
				Exception innermostException = GetInnermostException(exception);
				innermostException.Should().BeOfType<ArithmeticException>();
			}

			return Task.CompletedTask;
		}


		[Test]
		[TestCase("application/csv", "application/problem+json")]
		[TestCase("application/json", "application/problem+json")]
		public async Task ShouldSendProblemDetailsContentTypes(string acceptContentType, string responseContentType)
		{
			using(HttpClient httpClient = this.CreateHttpClient(
					  ResponseThrowsException(),
					  options =>
					  {
						  options.MapStatusCode<Exception>(HttpStatusCode.InternalServerError);
					  }))
			{
				httpClient.DefaultRequestHeaders.Accept.Clear();
				httpClient.DefaultRequestHeaders.Accept.ParseAdd(acceptContentType);

				HttpResponseMessage response = await httpClient.GetAsync(string.Empty);

				response.Content.Headers.ContentType.MediaType.Should().Be(responseContentType);
			}
		}
	}
}
