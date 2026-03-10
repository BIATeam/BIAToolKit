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
            SolutionParsedMessage received = null;
            _messenger.Subscribe<SolutionParsedMessage>(m => received = m);

            var sent = new SolutionParsedMessage();
            _messenger.Send(sent);

            Assert.Same(sent, received);
        }

        [Fact]
        public void Send_WithNoSubscribers_DoesNotThrow()
        {
            var ex = Record.Exception(() => _messenger.Send(new SolutionParsedMessage()));
            Assert.Null(ex);
        }

        [Fact]
        public void Unsubscribe_StopsDelivery()
        {
            int callCount = 0;
            Action<SolutionParsedMessage> handler = _ => callCount++;
            _messenger.Subscribe(handler);
            _messenger.Send(new SolutionParsedMessage());

            _messenger.Unsubscribe(handler);
            _messenger.Send(new SolutionParsedMessage());

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Subscribe_MultipleHandlers_AllReceiveMessage()
        {
            int callCount = 0;
            _messenger.Subscribe<SolutionParsedMessage>(_ => callCount++);
            _messenger.Subscribe<SolutionParsedMessage>(_ => callCount++);

            _messenger.Send(new SolutionParsedMessage());

            Assert.Equal(2, callCount);
        }

        [Fact]
        public void Send_DifferentMessageTypes_OnlyMatchingHandlerCalled()
        {
            bool solutionParsed = false;
            bool newVersion = false;

            _messenger.Subscribe<SolutionParsedMessage>(_ => solutionParsed = true);
            _messenger.Subscribe<NewVersionAvailableMessage>(_ => newVersion = true);

            _messenger.Send(new SolutionParsedMessage());

            Assert.True(solutionParsed);
            Assert.False(newVersion);
        }

        [Fact]
        public void Subscribe_NullHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _messenger.Subscribe<SolutionParsedMessage>(null));
        }

        [Fact]
        public void Unsubscribe_NullHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _messenger.Unsubscribe<SolutionParsedMessage>(null));
        }

        [Fact]
        public void Send_NullMessage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _messenger.Send<SolutionParsedMessage>(null));
        }
    }
}
