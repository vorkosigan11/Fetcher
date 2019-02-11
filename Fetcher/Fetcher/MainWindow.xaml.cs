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
            dataGrid.ItemsSource = MyViewModel.Pieces;
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
                RunButton.IsEnabled = false;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MyViewModel)
            {
                MyViewModel.WorkOrder.WorkOrderNumber = numerZlecenia.Text + "/SW/" + rokZlecenia.Text + "/ZLP";

                messageBox.Visibility = Visibility.Hidden;
                dataGrid.Visibility = Visibility.Hidden;

                BusyIndicator.IsBusy = true;
                BusyIndicator.Header = "Sprawdzanie czy istnieje zlecenie w Sigmie...";

                bool sigma = await Task.Run(() => MyViewModel.isWoInSigma(Properties.Settings.Default.sigmaConnectionString));

                BusyIndicator.IsBusy = true;
                BusyIndicator.Header = "Sprawdzanie czy istnieje zlecenie w Vendo...";

                bool vendo = await Task.Run(() => MyViewModel.isWoInVendo(Properties.Settings.Default.vendoConnectionString));

                if ((!sigma && vendo) == true)
                {
                    BusyIndicator.Header = "Pobieranie danych z bazy Vendo...";
                    await Task.Run(() =>
                         MyViewModel.AddDataToResults(Fetcher.Properties.Settings.Default.vendoConnectionString, Fetcher.Properties.Settings.Default.sigmaConnectionString));

                    BusyIndicator.IsBusy = false;
                    dataGrid.Visibility = Visibility.Visible;

                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = MyViewModel.Pieces;

                    this.dataGrid.SortColumnDescriptions.Clear();
                    SortColumnDescription sortColumnDescription = new SortColumnDescription();
                    sortColumnDescription.ColumnName = "Exist";
                    sortColumnDescription.SortDirection = ListSortDirection.Ascending;
                    this.dataGrid.SortColumnDescriptions.Add(sortColumnDescription);

                    BusyIndicator.Header = "Sprawdzanie czy wszytkie pliki istnieją...";
                    BusyIndicator.IsBusy = true;
                    bool fileExist = await Task.Run(() => MyViewModel.AllFilesExist());

                    if (fileExist)
                    {
                        BusyIndicator.IsBusy = false;
                        BusyIndicator.Header = "Ładowanie WOL do Sigmy...";
                        BusyIndicator.IsBusy = true;

                        await Task.Run(() =>
                        {
                            MyViewModel.RunSigma();
                        });

                        BusyIndicator.IsBusy = false;
                        BusyIndicator.Header = "Aktualizacja zlecenia w Sigmie...";
                        BusyIndicator.IsBusy = true;

                        if (MyViewModel.isWoInSigma(Fetcher.Properties.Settings.Default.sigmaConnectionString))
                        {
                            int i = MyViewModel.ChangeInSigma(Fetcher.Properties.Settings.Default.sigmaConnectionString, MyViewModel.makeQueryUpdateWoInSigma());
                        }
                        else
                        {
                            MessageBox.Show("Posypało się - brak zlecenia w Sigmie", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    BusyIndicator.IsBusy = false;
                    numerZlecenia.Text = string.Empty;
                }
                else
                {
                    BusyIndicator.IsBusy = false;
                    messageBox.Visibility = Visibility.Visible;

                    if (sigma)
                        messageBox.Text = ("Zlecenie " + MyViewModel.WorkOrder.WorkOrderNumber + " już istnieje w Sigmie");

                    if (!vendo)
                        messageBox.Text = ("Brak zlecenia " + MyViewModel.WorkOrder.WorkOrderNumber + " w Vendo");
                }
            }
        }
    }
}