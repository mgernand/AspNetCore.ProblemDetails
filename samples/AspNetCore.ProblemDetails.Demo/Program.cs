namespace AspNetCore.ProblemDetails.Demo
{
	using System;
	using System.Net;
	using Microsoft.AspNetCore.Builder;
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
					// Only include exception details in a development environment. This is the default
					// behavior and is included to demo purposes.
					options.IncludeExceptionDetails = (context, exception) => builder.Environment.IsDevelopment();

					// Use the status code 501 for this type of exception.
					options.StatusCode<NotImplementedException>(HttpStatusCode.NotImplemented);

					// Use the status code 501 for this type of exception.
					options.StatusCode<InvalidOperationException>(HttpStatusCode.InternalServerError);

					// Add a fallback for all other exceptions with the status code 500.
					options.StatusCode<Exception>(HttpStatusCode.InternalServerError);
				});

			WebApplication app = builder.Build();

			// Configure the HTTP request pipeline.			
			if(app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseProblemDetails();
			app.UseHttpsRedirection();
			app.UseAuthorization();
			app.MapControllers();
			app.Run();
		}
	}
}
