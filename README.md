# AspNetCore.ProblemDetails

A middleware to create RFC 7807 problem details for APIs.

## This repository was moved to https://codeberg.org/mgernand/AspNetCore.ProblemDetails

## Usage

To configute the mandatory services for the problem details middleware just execute
```AddProblemDetails``` on the ```IMvcBuilder``` instance in your startup code.

```C#
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddControllers()
	.AddProblemDetails(options =>
	{
		// Only include exception details in a development environment. This is the default
		// behavior and is included to demo purposes.
		options.IncludeExceptionDetails = (context, exception) => builder.Environment.IsDevelopment();

		// Use the status code 501 for this type of exception.
		options.MapStatusCode<NotImplementedException>(HttpStatusCode.NotImplemented);

		// Use the status code 501 for this type of exception.
		options.MapStatusCode<InvalidOperationException>(HttpStatusCode.InternalServerError);

		// Add a fallback for all other exceptions with the status code 500.
		options.MapStatusCode<Exception>(HttpStatusCode.InternalServerError);
	});
```

After you've built your web application instance add the middlware to the pipeline by
calling ```UseProblemDetails```.

```C#
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.			
app.UseCors();
app.UseProblemDetails();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

