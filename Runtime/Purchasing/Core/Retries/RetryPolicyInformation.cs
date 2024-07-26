#nullable enable

namespace UnityEngine.Purchasing
{
    public struct RetryPolicyInformation : IRetryPolicyInformation
    {
        public int NumberOfAttempts { get; }
        public float TimeSinceFirstAttempt { get; }

        internal RetryPolicyInformation(int numberOfAttempts, float timeSinceFirstAttempt)
        {
            NumberOfAttempts = numberOfAttempts;
            TimeSinceFirstAttempt = timeSinceFirstAttempt;
        }
    }
}
