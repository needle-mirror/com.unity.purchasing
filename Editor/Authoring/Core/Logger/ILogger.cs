namespace UnityEditor.Purchasing.Editor.Authoring.Core.Logger
{
    public interface ILogger
    {
        void LogError(object message);
        void LogWarning(object message);
        void LogInfo(object message);
        void LogVerbose(object message);
    }
}
