namespace BIA.ToolKit.UserControls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Markup;

    /// <summary>
    /// Interaction logic for LabeledField.xaml
    /// </summary>
    [ContentProperty(nameof(FieldContent))]          // rend l’écriture XAML naturelle
    public partial class LabeledField : UserControl
    {
        public LabeledField() => InitializeComponent();

        public static readonly DependencyProperty FieldContentProperty =
            DependencyProperty.Register(nameof(FieldContent),
                typeof(object), typeof(LabeledField));

        public object FieldContent
        {
            get => GetValue(FieldContentProperty);
            set => SetValue(FieldContentProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label),
                typeof(string), typeof(LabeledField));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
