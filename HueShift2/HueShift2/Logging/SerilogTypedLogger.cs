using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using System;

namespace HueShift2.Logging
{
	public class SerilogTypedLogger<T> : ILogger<T>
	{
		private readonly ILogger logger;

		public SerilogTypedLogger(Serilog.ILogger logger)
		{
			using (var logfactory = new SerilogLoggerFactory(logger))
            {
				this.logger = logfactory.CreateLogger(typeof(T).FullName);
			}
		}

		IDisposable ILogger.BeginScope<TState>(TState state) =>
			logger.BeginScope<TState>(state);

		bool ILogger.IsEnabled(LogLevel logLevel) =>
			logger.IsEnabled(logLevel);

		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
			logger.Log<TState>(logLevel, eventId, state, exception, formatter);
	}
}