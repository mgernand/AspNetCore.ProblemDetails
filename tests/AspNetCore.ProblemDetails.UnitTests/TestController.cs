namespace AspNetCore.ProblemDetails.UnitTests
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Net;
	using Microsoft.AspNetCore.Mvc;

	[ApiController]
	[Route("api")]
	public class TestController : ControllerBase
	{
		[HttpGet("statusCode/{statusCode?}")]
		public ActionResult Status([Required] int? statusCode)
		{
			return this.StatusCode(statusCode.GetValueOrDefault());
		}

		[HttpGet("validation")]
		public ActionResult Validation()
		{
			this.ModelState.AddModelError("property", "An error message.");
			return this.BadRequest(this.ModelState);
		}

		[HttpGet("error")]
		public ActionResult Error()
		{
			throw new Exception("Error");
		}

		[HttpGet("string-detail")]
		public ActionResult StringDetail()
		{
			return this.UnprocessableEntity("A detailed message.");
		}

		[HttpGet("problem-model")]
		public ActionResult ProblemModel()
		{
			ProblemDetails problemDetails = this.ProblemDetailsFactory.CreateProblemDetails(this.HttpContext,
				(int)HttpStatusCode.TooManyRequests,
				"A title");

			return new ObjectResult(problemDetails);
		}

		[HttpGet("error-model")]
		public ActionResult ErrorModel()
		{
			return this.BadRequest(new Exception("Error"));
		}
	}
}
