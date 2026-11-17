using System.Windows;

namespace KidsColoringBook.Views;

public partial class TextInputDialogWindow : Window
{
    public string InputText { get; private set; } = string.Empty;

    public TextInputDialogWindow(string title, string defaultText)
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtInput.Text = defaultText;
        TxtInput.SelectAll();
        Loaded += (s, e) => TxtInput.Focus();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        InputText = TxtInput.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
