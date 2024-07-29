using BepInEx.Logging;

namespace SpeenChroma2
{
    internal static class Log
    {
        private static ManualLogSource _logger;

        public static void Init(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void Info(object msg) => _logger.LogInfo(msg);
        public static void Debug(object msg) => _logger.LogDebug(msg);
        public static void Message(object msg) => _logger.LogMessage(msg);
        public static void Warning(object msg) => _logger.LogWarning(msg);
        public static void Error(object msg) => _logger.LogError(msg);
        public static void Fatal(object msg) => _logger.LogFatal(msg);
    }
}
