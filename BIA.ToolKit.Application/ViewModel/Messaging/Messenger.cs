namespace BIA.ToolKit.Application.ViewModel.Messaging
{
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Thread-safe, typed publish/subscribe message broker.
    /// Replaces the non-typed UIEventBroker event pattern.
    /// </summary>
    public class Messenger : IMessenger
    {
        private readonly Dictionary<Type, List<Delegate>> subscriptions = [];
        private readonly object syncLock = new();

        /// <inheritdoc/>
        public void Subscribe<T>(Action<T> handler) where T : IMessage
        {
            ArgumentNullException.ThrowIfNull(handler);
            var key = typeof(T);
            lock (syncLock)
            {
                if (!subscriptions.TryGetValue(key, out var handlers))
                {
                    handlers = [];
                    subscriptions[key] = handlers;
                }
                handlers.Add(handler);
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe<T>(Action<T> handler) where T : IMessage
        {
            ArgumentNullException.ThrowIfNull(handler);
            var key = typeof(T);
            lock (syncLock)
            {
                if (subscriptions.TryGetValue(key, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }

        /// <inheritdoc/>
        public void Send<T>(T message) where T : IMessage
        {
            ArgumentNullException.ThrowIfNull(message);
            var key = typeof(T);
            List<Delegate> snapshot;
            lock (syncLock)
            {
                if (!subscriptions.TryGetValue(key, out var handlers) || handlers.Count == 0)
                    return;
                snapshot = new List<Delegate>(handlers);
            }

            foreach (var handler in snapshot)
            {
                ((Action<T>)handler)(message);
            }
        }
    }
}
