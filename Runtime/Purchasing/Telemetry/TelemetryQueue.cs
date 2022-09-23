using System;
using System.Collections.Concurrent;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryQueue<TTelemetryEventParams>
    {
        readonly Action<TTelemetryEventParams> m_SendTelemetryEvent;
        ConcurrentQueue<TTelemetryEventParams> m_Queue;
        internal const int k_maxQueueSize = 10;

        public TelemetryQueue(Action<TTelemetryEventParams> sendTelemetryEvent)
        {
            m_SendTelemetryEvent = sendTelemetryEvent;
        }

        internal void QueueEvent(TTelemetryEventParams telemetryEvent)
        {
            m_Queue ??= new ConcurrentQueue<TTelemetryEventParams>();
            m_Queue.Enqueue(telemetryEvent);

            if (m_Queue.Count > k_maxQueueSize)
            {
                m_Queue.TryDequeue(out _);
            }
        }

        internal void SendQueuedEvents()
        {
            if (m_SendTelemetryEvent == null || m_Queue == null)
            {
                return;
            }

            while (m_Queue.TryDequeue(out var telemetryEvent))
            {
                m_SendTelemetryEvent(telemetryEvent);
            }
        }
    }
}
