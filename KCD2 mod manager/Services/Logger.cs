using Serilog;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Logger-Implementierung mit Serilog
    /// </summary>
    public class Logger : ILog
    {
        private readonly Serilog.ILogger _logger;

        public Logger()
        {
            _logger = new LoggerConfiguration()
                .WriteTo.File("logs/kcd2-mod-manager-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Information(message);
        }

        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        public void Error(string message, Exception? exception = null)
        {
            if (exception != null)
                _logger.Error(exception, message);
            else
                _logger.Error(message);
        }
    }
}

