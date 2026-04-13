using System.Windows;

namespace Collectibles.SyncTool;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string header, string detail, string footer)
    {
        InitializeComponent();
        HeaderText.Text = header;
        DetailText.Text = detail;
        FooterText.Text = footer;
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
