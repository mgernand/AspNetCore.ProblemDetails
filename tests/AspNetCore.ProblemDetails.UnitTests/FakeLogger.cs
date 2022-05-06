namespace AspNetCore.ProblemDetails.UnitTests
{
	using System;
	using Microsoft.Extensions.Logging;

	internal class FakeLogger<T> : ILogger<T>
	{
		/// <inheritdoc />
		public IDisposable BeginScope<TState>(TState state)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		/// <inheritdoc />
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
		}
	}
}
