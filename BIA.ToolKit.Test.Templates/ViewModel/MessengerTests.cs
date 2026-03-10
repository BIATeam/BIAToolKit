namespace BIA.ToolKit.Test.Templates.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.Messaging;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using System;
    using Xunit;

    /// <summary>
    /// Unit tests for the typed <see cref="Messenger"/> publish/subscribe broker.
    /// </summary>
    public sealed class MessengerTests
    {
        private readonly Messenger _messenger = new();

        [Fact]
        public void Subscribe_And_Send_DeliverMessageToHandler()
        {
            ProjectChangedMessage received = null;
            _messenger.Subscribe<ProjectChangedMessage>(m => received = m);

            var sent = new ProjectChangedMessage { Project = null };
            _messenger.Send(sent);

            Assert.Same(sent, received);
        }

        [Fact]
        public void Send_WithNoSubscribers_DoesNotThrow()
        {
            var ex = Record.Exception(() => _messenger.Send(new ProjectChangedMessage { Project = null }));
            Assert.Null(ex);
        }

        [Fact]
        public void Unsubscribe_StopsDelivery()
        {
            int callCount = 0;
            Action<ProjectChangedMessage> handler = _ => callCount++;
            _messenger.Subscribe(handler);
            _messenger.Send(new ProjectChangedMessage { Project = null });

            _messenger.Unsubscribe(handler);
            _messenger.Send(new ProjectChangedMessage { Project = null });

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Subscribe_MultipleHandlers_AllReceiveMessage()
        {
            int callCount = 0;
            _messenger.Subscribe<ProjectChangedMessage>(_ => callCount++);
            _messenger.Subscribe<ProjectChangedMessage>(_ => callCount++);

            _messenger.Send(new ProjectChangedMessage { Project = null });

            Assert.Equal(2, callCount);
        }

        [Fact]
        public void Send_DifferentMessageTypes_OnlyMatchingHandlerCalled()
        {
            bool projectChanged = false;
            bool newVersion = false;

            _messenger.Subscribe<ProjectChangedMessage>(_ => projectChanged = true);
            _messenger.Subscribe<NewVersionAvailableMessage>(_ => newVersion = true);

            _messenger.Send(new ProjectChangedMessage { Project = null });

            Assert.True(projectChanged);
            Assert.False(newVersion);
        }

        [Fact]
        public void Subscribe_NullHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _messenger.Subscribe<ProjectChangedMessage>(null));
        }

        [Fact]
        public void Unsubscribe_NullHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _messenger.Unsubscribe<ProjectChangedMessage>(null));
        }

        [Fact]
        public void Send_NullMessage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _messenger.Send<ProjectChangedMessage>(null));
        }
    }
}
