using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Syncfusion;
namespace Fetcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NTk5MzNAMzEzNjJlMzQyZTMwU0JRcXdRVThnbm5KT0MzaTRVaUVmMEVvTmxvYk1YQmwyNDJVdkp3SkZLYz0=");
        }
    }
}
