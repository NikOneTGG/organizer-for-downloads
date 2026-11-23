using System.IO;
using System.Windows;

namespace DownloadManager;

public partial class InputDialog : Window
{
    public string Value { get; private set; } = "";

    public InputDialog(string prompt, string initialValue)
    {
        InitializeComponent();

        PromptText.Text = prompt;
        InputBox.Text = initialValue;

        Loaded += (_, _) =>
        {
            Activate();
            InputBox.Focus();
            InputBox.SelectAll();
        };
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        string text = InputBox.Text.Trim();

        if (text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            System.Windows.MessageBox.Show(
                this,
                "The name cannot contain any of these characters: \\ / : * ? \" < > |",
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.None);

            InputBox.Focus();
            return;
        }

        Value = text;
        DialogResult = true;
    }
}
