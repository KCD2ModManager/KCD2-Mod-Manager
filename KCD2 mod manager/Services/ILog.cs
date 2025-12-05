namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Logging-Funktionalität
    /// </summary>
    public interface ILog
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
    }
}

