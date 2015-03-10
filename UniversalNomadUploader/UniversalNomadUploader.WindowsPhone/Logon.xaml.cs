using UniversalNomadUploader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.APIUtils;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.Exceptions;
using Windows.Storage;
using Windows.UI.ViewManagement;
using UniversalNomadUploader.Common;
using HockeyApp;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace UniversalNomadUploader
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Logon : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private DataManager m_dataManager = null;  

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public Logon()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            this.Loaded += Logon_Loaded;
        }

        async void Logon_Loaded(object sender, RoutedEventArgs e)
        {
            StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            // Hide the status bar
            await statusBar.HideAsync();
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values["Username"] != null)
            {
                Username.Text = roamingSettings.Values["Username"].ToString();
                Password.Focus(FocusState.Pointer);
            }
            Live.IsChecked = true;
            await HockeyClient.Current.SendCrashesAsync();
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void logon_Click(object sender, RoutedEventArgs e)
        {
            //TODO : suppress
            Username.Text = "BASSESSOR1";
            Password.Password = "Test1234";
            GlobalVariables.SelectedServer = ServerEnum.UAT1;
            UAT2.IsChecked = false;
            DEV.IsChecked = false;
            MessageDialog a = new MessageDialog("HACK HACK HACK");
            await a.ShowAsync();

            ShowProgress();
            if (String.IsNullOrWhiteSpace(Username.Text))
            {
                MessageDialog msg = new MessageDialog("Please enter a Username", "Required");
                await msg.ShowAsync();
                HideProgress();
                return;
            }
            if (String.IsNullOrWhiteSpace(Password.Password))
            {
                MessageDialog msg = new MessageDialog("Please enter a Password", "Required");
                await msg.ShowAsync();
                HideProgress();
                return;
            }

            m_dataManager = new DataManager(Username.Text, Password.Password);

            switch ((await m_dataManager.ConnectToServer()))
            {
                case connectionStatus.success:
                    this.Frame.Navigate(typeof(EvidenceViewer), m_dataManager);
                    return;
                case connectionStatus.badPassword:
                    MessageDialog msg = new MessageDialog("Incorrect Password", "Required");
                    await msg.ShowAsync();
                    break;
                case connectionStatus.badUsername:
                    MessageDialog msg0 = new MessageDialog("Incorrect Password", "Required");
                    await msg0.ShowAsync();
                    break;
                case connectionStatus.sqlError:
                    MessageDialog msg1 = new MessageDialog("Failed to register into the database", "Database error");
                    await msg1.ShowAsync();
                    break;
                case connectionStatus.authenticationFailed:
                    MessageDialog msg2 = new MessageDialog("Incorrect user or password", "Authentication failure");
                    await msg2.ShowAsync();
                    break;
                default:
                    MessageDialog msg3 = new MessageDialog("Unattented result", "Unknown error");
                    await msg3.ShowAsync();
                    break;
            }
        }

        private void ShowProgress()
        {
            SyncProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;
            SyncProgress.IsIndeterminate = true;
        }

        private void HideProgress()
        {
            SyncProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            SyncProgress.IsIndeterminate = false;
        }

        private void Live_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Live;
            Demo.IsChecked = false;
            Beta.IsChecked = false;
        }

        private void Demo_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Practice;
            Beta.IsChecked = false;
            Live.IsChecked = false;
        }

        private void Beta_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Beta;
            Demo.IsChecked = false;
            Live.IsChecked = false;
        }

        private void pageTitle_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (LiveOptions.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                GlobalVariables.SelectedServer = ServerEnum.Live;
                LiveOptions.Visibility = Windows.UI.Xaml.Visibility.Visible;
                DEVoptions.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                LogonGrid.RowDefinitions[3].Height = new GridLength(0.0);
                LogonGrid.RowDefinitions[2].Height = new GridLength(50.0);
                Live.IsChecked = false;
                Live.IsEnabled = true;
                Demo.IsChecked = false;
                Demo.IsEnabled = true;
                Beta.IsChecked = false;
                Beta.IsEnabled = true;
            }
            else
            {
                GlobalVariables.SelectedServer = ServerEnum.UAT1;
                LiveOptions.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                DEVoptions.Visibility = Windows.UI.Xaml.Visibility.Visible;
                LogonGrid.RowDefinitions[2].Height = new GridLength(0.0);
                LogonGrid.RowDefinitions[3].Height = new GridLength(50.0);
                Live.IsChecked = false;
                Live.IsEnabled = false;
                Demo.IsChecked = false;
                Demo.IsEnabled = false;
                Beta.IsChecked = false;
                Beta.IsEnabled = false;
            }
        }

        private void UAT1_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.UAT1;
            UAT2.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void UAT2_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.UAT2;
            UAT1.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void DEV_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.DEV;
            UAT1.IsChecked = false;
            UAT2.IsChecked = false;
        }

        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            Username.Text = Username.Text.ToUpper();
            Username.SelectionStart = Username.Text.Length;
        }
    }
}
