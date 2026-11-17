using System.Windows;

namespace KidsColoringBook.Views;

public partial class ConfirmDialogWindow : Window
{
    public ConfirmDialogWindow(string title, string message)
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtMessage.Text = message;
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnNoClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
