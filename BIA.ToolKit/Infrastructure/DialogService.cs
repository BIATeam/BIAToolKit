namespace BIA.ToolKit.Infrastructure
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.ViewModels;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Helper;
    using MaterialDesignThemes.Wpf;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// WPF implementation of IDialogService using MaterialDesign DialogHost.
    /// </summary>
    public class DialogService(GitService gitService, IConsoleWriter consoleWriter) : IDialogService
    {
        private readonly GitService gitService = gitService;
        private readonly IConsoleWriter consoleWriter = consoleWriter;

        private const string DialogIdentifier = "RootDialog";

        public bool? ShowLogDetail(List<LogMessage> messages)
        {
            var wpfMessages = messages
                .Select(m => new ConsoleWriter.Message { message = m.Text, color = m.Color })
                .ToList();

            var dialog = new LogDetailUC();
            dialog.LoadMessages(wpfMessages);

            ShowDialogSync(dialog);
            return true;
        }

        public RepositoryViewModel ShowRepositoryForm(RepositoryViewModel repository)
        {
            var form = new RepositoryFormUC(repository, gitService, consoleWriter);
            var result = ShowDialogSync(form);
            return result is true or "True" ? form.ViewModel.Repository : null;
        }

        public string BrowseFolder(string defaultFolder, string description = null)
        {
            return FileDialog.BrowseFolder(defaultFolder, description);
        }

        public bool Confirm(string message, string title = "Warning")
        {
            var content = BuildMessageDialogContent(title, message, okText: "OK", cancelText: "CANCEL");
            var result = ShowDialogSync(content);
            return result is true or "True";
        }

        public void ShowMessage(string message, string title = null)
        {
            var content = BuildMessageDialogContent(title ?? string.Empty, message, okText: "OK", cancelText: null);
            ShowDialogSync(content);
        }

        public string BrowseFile(string defaultFolder, string fileFilter = null)
        {
            return FileDialog.BrowseFile(defaultFolder, fileFilter);
        }

        public void CopyToClipboard(string text)
        {
            Clipboard.SetText(text);
        }

        public (string TeamName, string TeamNamePlural, string DomainName)? ShowDefaultTeamSettings(
            string currentName, string currentNamePlural, string currentDomainName)
        {
            var dialog = new Dialogs.DefaultTeamSettingsWindow(currentName, currentNamePlural, currentDomainName);
            var result = ShowDialogSync(dialog);
            if (result is true or "True")
            {
                return (dialog.ViewModel.DefaultTeamName, dialog.ViewModel.DefaultTeamNamePlural, dialog.ViewModel.DefaultTeamDomainName);
            }
            return null;
        }

        private static UserControl BuildMessageDialogContent(string title, string message, string okText, string cancelText)
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            if (!string.IsNullOrEmpty(title))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }

            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                Margin = new Thickness(0, 0, 0, 16)
            });

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (cancelText != null)
            {
                var cancelButton = new Button
                {
                    Content = cancelText,
                    Margin = new Thickness(5),
                    Command = DialogHost.CloseDialogCommand,
                    CommandParameter = "False"
                };
                cancelButton.SetResourceReference(FrameworkElement.StyleProperty, "MaterialDesignFlatButton");
                buttonsPanel.Children.Add(cancelButton);
            }

            var okButton = new Button
            {
                Content = okText,
                Margin = new Thickness(5),
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = "True"
            };
            okButton.SetResourceReference(FrameworkElement.StyleProperty, "MaterialDesignFlatButton");
            buttonsPanel.Children.Add(okButton);

            panel.Children.Add(buttonsPanel);

            return new UserControl { Content = panel, MinWidth = 300 };
        }

        /// <summary>
        /// Shows a DialogHost dialog synchronously by dispatching to the UI thread.
        /// Returns the dialog result.
        /// </summary>
        private static object ShowDialogSync(object content)
        {
            object result = null;

            // DialogHost.Show is async but our interface is sync.
            // Use Dispatcher to run the async show and pump messages until done.
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                // We're on the UI thread — use a nested frame to wait
                var frame = new System.Windows.Threading.DispatcherFrame();
                _ = Task.Run(async () =>
                {
                    result = await dispatcher.InvokeAsync(async () =>
                        await DialogHost.Show(content, DialogIdentifier)
                    ).Task.Unwrap();
                    frame.Continue = false;
                });
                System.Windows.Threading.Dispatcher.PushFrame(frame);
            }
            else
            {
                result = dispatcher.Invoke(() =>
                    DialogHost.Show(content, DialogIdentifier).GetAwaiter().GetResult());
            }

            return result;
        }
    }
}
