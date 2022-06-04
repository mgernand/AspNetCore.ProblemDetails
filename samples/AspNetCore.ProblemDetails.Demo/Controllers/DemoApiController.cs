namespace AspNetCore.ProblemDetails.Demo.Controllers
{
	using System;
	using System.Collections.Generic;
	using Fluxera.Extensions.Validation;
	using Microsoft.AspNetCore.Mvc;

	[ApiController]
	[Route("api")]
	public class DemoApiController : ControllerBase
	{
		[HttpGet("status/{statusCode}")]
		public IActionResult Status([FromRoute] int statusCode)
		{
			return this.StatusCode(statusCode);
		}

		[HttpGet("exception")]
		public IActionResult Exception()
		{
			throw new Exception("This is a simple exception.");
		}

		[HttpGet("exception/result")]
		public IActionResult ExceptionResult()
		{
			return this.BadRequest(new NotSupportedException());
		}

		[HttpGet("exception/not-implemented")]
		public IActionResult NotImplemented()
		{
			throw new NotImplementedException("This is a not implemented exception.");
		}

		[HttpGet("exception/invalid-operation")]
		public IActionResult InvalidOperation()
		{
			throw new InvalidOperationException("This is an invalid operation exception.");
		}

		[HttpGet("validation")]
		public IActionResult Validation()
		{
			this.ModelState.AddModelError("property", "This property's validation failed.");
			return this.ValidationProblem(this.ModelState);
		}

		[HttpGet("validation/bad")]
		public IActionResult ValidationBad()
		{
			this.ModelState.AddModelError("property", "This property's validation failed. - 1");
			this.ModelState.AddModelError("property", "This property's validation failed. - 2");
			this.ModelState.AddModelError("property", "This property's validation failed. - 3");
			this.ModelState.AddModelError("property", "This property's validation failed. - 4");
			return this.BadRequest(this.ModelState);
		}

		[HttpGet("validation/plain")]
		public IActionResult ValidationPlain()
		{
			return this.BadRequest("There was an error.");
		}

		[HttpGet("validation/exception")]
		public IActionResult ValidationException()
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

			throw new ValidationException(errors);
		}
	}
}
