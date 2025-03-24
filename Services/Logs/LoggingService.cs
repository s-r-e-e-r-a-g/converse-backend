namespace Converse.Services.Logs
{
    public class LoggingService
    {
        public static void Log(string logFile, string message)
        {
            File.AppendAllText(logFile, $"{DateTime.UtcNow}: {message}\n");
        }
    }
}