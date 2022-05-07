namespace AspNetCore.ProblemDetails
{
	using Microsoft.AspNetCore.Mvc.ApplicationModels;

	internal sealed class ProblemDetailsResultFilterConvention : IActionModelConvention
	{
		private readonly ProblemDetailsResultFilterFactory factory = new ProblemDetailsResultFilterFactory();

		public void Apply(ActionModel action)
		{
			action.Filters.Add(this.factory);
		}
	}
}
