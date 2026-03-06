namespace BIA.ToolKit.Application.ViewModel.Interfaces
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using System;

    /// <summary>
    /// Defines a typed publish/subscribe messaging contract between ViewModels.
    /// Replaces the non-typed UIEventBroker event pattern.
    /// </summary>
    public interface IMessenger
    {
        /// <summary>
        /// Subscribe to messages of type <typeparamref name="T"/>.
        /// </summary>
        void Subscribe<T>(Action<T> handler) where T : IMessage;

        /// <summary>
        /// Unsubscribe a previously registered handler for messages of type <typeparamref name="T"/>.
        /// </summary>
        void Unsubscribe<T>(Action<T> handler) where T : IMessage;

        /// <summary>
        /// Publish a message to all subscribers of type <typeparamref name="T"/>.
        /// </summary>
        void Send<T>(T message) where T : IMessage;
    }
}
