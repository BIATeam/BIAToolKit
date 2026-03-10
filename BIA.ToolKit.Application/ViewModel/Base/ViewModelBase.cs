namespace BIA.ToolKit.Application.ViewModel.Base
{
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// Abstract base class for all ViewModels.
    /// Combines <see cref="ObservableObject"/> (INotifyPropertyChanged) with
    /// <see cref="IViewModel"/> lifecycle hooks and access to <see cref="IMessenger"/>.
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject, IViewModel
    {
        /// <summary>
        /// Gets the typed messenger used to publish and subscribe to messages.
        /// </summary>
        protected IMessenger Messenger { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ViewModelBase"/>.
        /// </summary>
        /// <param name="messenger">The application-wide messenger instance.</param>
        protected ViewModelBase(IMessenger messenger)
        {
            Messenger = messenger;
        }

        /// <summary>
        /// Called when the associated view is loaded.
        /// Override to subscribe to messages and initialize state.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when the associated view is unloaded.
        /// Override to unsubscribe from messages and release resources.
        /// </summary>
        public virtual void Cleanup() { }
    }
}
