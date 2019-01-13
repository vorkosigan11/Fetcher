using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Controls.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Fetcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RunButton.IsEnabled = false;
            BusyIndicator.IsBusy = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string value = (sender as TextBox).Text;

            if (Int32.TryParse(value, out int x))
            {
                if (DateTime.Now.Month < 3 && x > 600)
                {
                    rokZlecenia.Text = (DateTime.Now.Year - 1).ToString().Remove(0, 2);
                }
                else
                { rokZlecenia.Text = DateTime.Now.ToString("yy"); }

                RunButton.IsEnabled = true;
            }
            else
            {
                rokZlecenia.Text = "";
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MyViewModel)
            {
                dataGrid.Dispatcher.Invoke(() => dataGrid.Visibility = Visibility.Hidden);
                BusyIndicator.Dispatcher.Invoke(() => BusyIndicator.IsBusy = true);

                //await Task.Run(() => (this.DataContext as MyViewModel).AddDataToResults(numerZlecenia.Text, rokZlecenia.Text, Fetcher.Properties.Settings.Default.connectionString));

                await Task.Run(() => MyViewModel.AddDataToResults(
                                                            numerZlecenia.Dispatcher.Invoke(() => numerZlecenia.Text),
                                                            rokZlecenia.Dispatcher.Invoke(() => rokZlecenia.Text),
                                                            Fetcher.Properties.Settings.Default.connectionString));

                this.dataGrid.SortColumnDescriptions.Clear();
                SortColumnDescription sortColumnDescription = new SortColumnDescription();
                sortColumnDescription.ColumnName = "Exist";
                sortColumnDescription.SortDirection = ListSortDirection.Ascending;
                this.dataGrid.SortColumnDescriptions.Add(sortColumnDescription);

                BusyIndicator.IsBusy = false;
                dataGrid.Visibility = Visibility.Visible;
            }
        }
    }
}