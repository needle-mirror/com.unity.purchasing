#nullable enable

namespace UnityEngine.Purchasing
{
    public interface IRetryPolicyInformation
    {
        public int NumberOfAttempts { get; }
        public float TimeSinceFirstAttempt { get; }
    }
}
