namespace AspNetCore.ProblemDetails.Demo
{
	using System;
	using System.Net;
	using Fluxera.Extensions.Validation;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;

	public static class Program
	{
		public static void Main(string[] args)
		{
			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services
				.AddEndpointsApiExplorer()
				.AddSwaggerGen()
				.AddControllers()
				.AddControllersAsServices()
				.AddProblemDetails(options =>
				{
					// Only include exception details in a development environment.
					// This is the default behavior and is included just for demo purposes.
					options.IncludeExceptionDetails = (_, _) => !builder.Environment.IsDevelopment();

					// Use the status code 501 for this type of exception.
					options.MapStatusCode<NotImplementedException>(HttpStatusCode.NotImplemented);

					// Use the status code 405 for this type of exception.
					options.MapStatusCode<InvalidOperationException>(HttpStatusCode.MethodNotAllowed);

					// Use the status code 400 withe the details factory for this type of exception.
					options.MapStatusCode<ValidationException>(HttpStatusCode.BadRequest, (context, exception, httpStatusCode, problemDetailsFactory) =>
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

					// Add a fallback for all other exceptions.
					options.MapStatusCode<Exception>(HttpStatusCode.InternalServerError);
				});

			WebApplication app = builder.Build();

			// Configure the HTTP request pipeline.			
			if(app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseCors();
			app.UseProblemDetails();
			app.UseHttpsRedirection();
			app.UseAuthorization();
			app.MapControllers();
			app.Run();
		}
	}
}
