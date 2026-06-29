// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Purchasing.Shared.EditorUtils
{
    static class Sync
    {
        internal class Throttler
        {
            readonly Action m_Action;
            TimeSpan m_Timeout;
            string m_LastTrace;
            readonly object m_Lock = new object();
            bool m_ActionPending;
            float m_Deadline;
            readonly ThrottleOption m_Mode;

            public Throttler(Action action, TimeSpan timeout, ThrottleOption mode = ThrottleOption.Trailing)
            {
                m_Action = action;
                m_Timeout = timeout;
                m_Mode = mode;
            }

            void EditorCallback()
            {
                if (Time.realtimeSinceStartup < m_Deadline)
                {
                    // keep going the timeout is not reached
                    return;
                }

                // timeout was reached, let's execute action and unregister callback
                lock (m_Lock)
                {
                    m_ActionPending = false;
                    EditorApplication.update -= EditorCallback;
                }

                if (m_Mode == ThrottleOption.Leading)
                {
                    return;
                }
                // if trailing mode, execute action now
                try
                {
                    m_Action();
                }
                catch (Exception e)
                {
                    throw new ThrottlerActionFailedException(m_LastTrace, e);
                }
            }

            void SetupDeadline()
            {
                m_Deadline = Time.realtimeSinceStartup + (float)m_Timeout.TotalSeconds;
            }

            // Flush will trigger the throttled action immediately without waiting for the deadline or the editor callback
            // CAUTION: to be run on main thread
            internal void Flush()
            {
                bool shouldTrigger = false;
                lock (m_Lock)
                {
                    if (m_ActionPending)
                    {
                        m_Deadline = 0;
                        shouldTrigger = true;
                    }
                }

                if (shouldTrigger)
                {
                    EditorCallback();
                }
            }

            public void Trigger()
            {
                lock(m_Lock)
                {
                    if (m_ActionPending)
                    {
                        if (m_Mode == ThrottleOption.Debounce)
                        {
                            SetupDeadline();
                        }
                        return;
                    }

                    m_LastTrace = Environment.StackTrace;
                    m_ActionPending = true;
                    SetupDeadline();
                    EditorApplication.update += EditorCallback;
                    if (m_Mode is ThrottleOption.Leading or ThrottleOption.Both)
                    {
                        RunNextUpdateOnMain(m_Action);
                    }
                }
            }

            class ThrottlerActionFailedException : Exception
            {
                public ThrottlerActionFailedException(string trace, Exception e)
                    : base($"A throttled execution threw an exception. Original stack trace: {trace}", e) { }
            }
        }

        internal enum ThrottleOption
        {
            Leading,
            Trailing,
            Both,
            Debounce,
        }

        public static void RunNextUpdateOnMain(
            Action action,
            [CallerFilePath] string file = null,
            [CallerMemberName] string caller = null,
            [CallerLineNumber] int line = 0)
        {
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                EditorApplication.update -= callback;
                try
                {
                    action();
                }
                catch (Exception e) when (caller != null && file != null && line != 0)
                {
                    throw new Exception($"Exception thrown from invocation made by '{file}'({line}) by {caller}", e);
                }
            };
            EditorApplication.update += callback;
        }

        public static Task SafeAsync(Func<Task> action, Action<Task> success = null)
        {
            return action().ContinueWith(t =>
            {
                RunNextUpdateOnMain(() =>
                {
                    if (t.Exception != null)
                    {
                        Debug.LogException(t.Exception);
                    }
                    else
                    {
                        success?.Invoke(t);
                    }
                });
            });
        }

        public static Task SafeAsync<T>(Func<Task<T>> action, Action<Task<T>> success = null)
        {
            return SafeAsync(
                (Func<Task>)action,
                task =>
                {
                    success?.Invoke((Task<T>)task);
                });
        }

        public static Task WaitForEventAsync(Action<Action> sub, Action<Action> unsub)
        {
            var tcs = new TaskCompletionSource<bool>();

            Action handler = null;
            handler = () =>
            {
                unsub(handler);
                tcs.SetResult(true);
            };

            sub(handler);
            return tcs.Task;
        }
    }
}
