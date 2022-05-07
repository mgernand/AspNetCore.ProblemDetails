namespace AspNetCore.ProblemDetails
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using Fluxera.Guards;
	using JetBrains.Annotations;
	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Mvc;

	/// <summary>
	///     The options for the problem details middleware.
	/// </summary>
	[PublicAPI]
	public sealed class ProblemDetailsOptions
	{
		/// <summary>
		///     Creates a new instance of the <see cref="ProblemDetailsOptions" /> type.
		/// </summary>
		public ProblemDetailsOptions()
		{
			this.StatusCodeMappings = new List<StatusCodeMapper>();
			this.RethrowMappings = new List<Func<HttpContext, Exception, bool>>();
		}

		/// <summary>
		///     Gets or sets a predicate used to determine if the exception details should be
		///     included in the problem details response. The default returns <c>true</c> for
		///     the <c>Development</c> environment.
		/// </summary>
		public Func<HttpContext, Exception, bool> IncludeExceptionDetails { get; set; }

		/// <summary>
		///     Gets or sets a predicate used to determine if an exception should be logged
		///     as unhandled. The default returns <c>true</c> if the status code has no value,
		///     or the  value is <c>500</c> or higher.
		/// </summary>
		public Func<HttpContext, Exception, ProblemDetails, bool> LogUnhandledException { get; set; }

		internal IList<StatusCodeMapper> StatusCodeMappings { get; }

		internal IList<Func<HttpContext, Exception, bool>> RethrowMappings { get; }

		/// <summary>
		///     Configures the middleware to use the given HTTP status code value to be used
		///     for the problem details and the response for any occurring instance of the
		///     given exception.
		/// </summary>
		/// <typeparam name="TException"></typeparam>
		/// <param name="httpStatusCode"></param>
		public void MapStatusCode<TException>(HttpStatusCode httpStatusCode) where TException : Exception
		{
			this.Map<TException>((_, _) => httpStatusCode);
		}

		/// <summary>
		///     Configures the middleware to ignore any occurring instance of the given exception.
		///     This causes the middleware to re-throw the exception.
		/// </summary>
		/// <typeparam name="TException"></typeparam>
		public void Ignore<TException>() where TException : Exception
		{
			this.Rethrow<TException>();
		}

		/// <summary>
		///     Configures the middleware to re-throw occurring instances of the given exception.
		/// </summary>
		/// <typeparam name="TException"></typeparam>
		public void Rethrow<TException>() where TException : Exception
		{
			this.Rethrow<TException>((_, _) => true);
		}

		/// <summary>
		///     Configures the middleware to re-throw all occurring exceptions.
		/// </summary>
		public void RethrowAll()
		{
			// Just add one mapping that always returns true.
			this.RethrowMappings.Clear();
			this.Rethrow((_, _) => true);
		}

		public void Rethrow<TException>(Func<HttpContext, TException, bool> predicate) where TException : Exception
		{
			this.Rethrow((context, exception) => exception is TException ex && predicate(context, ex));
		}

		public void Rethrow(Func<HttpContext, Exception, bool> predicate)
		{
			this.RethrowMappings.Add(predicate);
		}

		public void Map<TException>(Func<TException, HttpStatusCode?> mapping)
			where TException : Exception
		{
			this.Map<TException>((_, exception) => mapping.Invoke(exception));
		}

		public void Map<TException>(Func<HttpContext, TException, HttpStatusCode?> mapping)
			where TException : Exception
		{
			this.Map((_, _) => true, mapping);
		}

		public void Map<TException>(
			Func<HttpContext, TException, bool> predicate,
			Func<HttpContext, TException, HttpStatusCode?> mapping)
			where TException : Exception
		{
			StatusCodeMapper mapper = new StatusCodeMapper(
				typeof(TException),
				(context, exception) => predicate.Invoke(context, (TException)exception),
				(context, exception) => mapping.Invoke(context, (TException)exception));

			this.StatusCodeMappings.Add(mapper);
		}

		internal bool TryMapStatusCode(HttpContext httpContext, Exception exception, out HttpStatusCode? httpStatusCode)
		{
			if(exception is null)
			{
				httpStatusCode = default;
				return false;
			}

			foreach(StatusCodeMapper statusCodeMapper in this.StatusCodeMappings)
			{
				if(statusCodeMapper.TryMapStatusCode(httpContext, exception, out httpStatusCode))
				{
					return httpStatusCode.HasValue;
				}
			}

			httpStatusCode = default;
			return false;
		}

		internal bool ShouldRethrow(HttpContext httpContext, Exception exception)
		{
			foreach(Func<HttpContext, Exception, bool> mapping in this.RethrowMappings)
			{
				if(mapping.Invoke(httpContext, exception))
				{
					return true;
				}
			}

			return false;
		}

		internal sealed class StatusCodeMapper
		{
			private readonly Type exceptionType;
			private readonly Func<HttpContext, Exception, HttpStatusCode?> mapping;
			private readonly Func<HttpContext, Exception, bool> predicate;

			public StatusCodeMapper(
				Type exceptionType,
				Func<HttpContext, Exception, bool> predicate,
				Func<HttpContext, Exception, HttpStatusCode?> mapping)
			{
				this.exceptionType = Guard.Against.Null(exceptionType);
				this.predicate = Guard.Against.Null(predicate);
				this.mapping = Guard.Against.Null(mapping);
			}

			private bool ShouldMap(HttpContext context, Exception exception)
			{
				return this.exceptionType.IsInstanceOfType(exception) && this.predicate.Invoke(context, exception);
			}

			public bool TryMapStatusCode(HttpContext httpContext, Exception exception, out HttpStatusCode? httpStatusCode)
			{
				if(this.ShouldMap(httpContext, exception))
				{
					try
					{
						httpStatusCode = this.mapping(httpContext, exception);
						return true;
					}
					catch
					{
						httpStatusCode = default;
						return false;
					}
				}

				httpStatusCode = default;
				return false;
			}
		}
	}
}
