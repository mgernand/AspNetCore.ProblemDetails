using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MadEyeMatt.AspNetCore.ProblemDetails
{
    internal sealed class ProblemDetailsApplicationModelProvider : IApplicationModelProvider
	{
		public ProblemDetailsApplicationModelProvider()
		{
			ProducesErrorResponseTypeAttribute defaultErrorResponseType = new ProducesErrorResponseTypeAttribute(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails));

			this.ActionModelConventions = new List<IActionModelConvention>
			{
				new ApiConventionApplicationModelConvention(defaultErrorResponseType),
				new ProblemDetailsResultFilterConvention()
			};
		}

		private List<IActionModelConvention> ActionModelConventions { get; }

		/// <inheritdoc />
		public int Order => -1000 + 200;

		/// <inheritdoc />
		public void OnProvidersExecuting(ApplicationModelProviderContext context)
		{
			foreach(ControllerModel controller in context.Result.Controllers)
			{
				if(!IsApiController(controller))
				{
					continue;
				}

				foreach(ActionModel action in controller.Actions)
				{
					foreach(IActionModelConvention convention in this.ActionModelConventions)
					{
						convention.Apply(action);
					}
				}
			}
		}

		/// <inheritdoc />
		void IApplicationModelProvider.OnProvidersExecuted(ApplicationModelProviderContext context)
		{
			// Intentionally left blank.
		}

		private static bool IsApiController(ControllerModel controller)
		{
			if(controller.Attributes.OfType<IApiBehaviorMetadata>().Any())
			{
				return true;
			}

			Assembly assembly = controller.ControllerType.Assembly;
			IEnumerable<Attribute> attributes = assembly.GetCustomAttributes();

			return attributes.OfType<IApiBehaviorMetadata>().Any();
		}
	}
}
