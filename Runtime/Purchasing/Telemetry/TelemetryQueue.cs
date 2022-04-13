using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryQueue<TTelemetryEventParams>
    {
        Action<TTelemetryEventParams> m_SendTelemetryEvent;
        Queue<TTelemetryEventParams> m_Queue;
        internal const int k_maxQueueSize = 10;

        public TelemetryQueue(Action<TTelemetryEventParams> sendTelemetryEvent)
        {
            m_SendTelemetryEvent = sendTelemetryEvent;
        }

        internal void QueueEvent(TTelemetryEventParams telemetryEvent)
        {
            m_Queue ??= new Queue<TTelemetryEventParams>();
            m_Queue.Enqueue(telemetryEvent);

            if (m_Queue.Count > k_maxQueueSize)
            {
                m_Queue.Dequeue();
            }
        }

        internal void SendQueuedEvents()
        {
            if (m_SendTelemetryEvent == null || m_Queue == null)
            {
                return;
            }

            foreach (var telemetryEvent in m_Queue)
            {
                m_SendTelemetryEvent(telemetryEvent);
            }
            m_Queue.Clear();
        }
    }
}
