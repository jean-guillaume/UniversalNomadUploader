using UniversalNomadUploader.Common;
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
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.Exceptions;
using Windows.Storage;
using HockeyApp;
using UniversalNomadUploader.SQLUtils;

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
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values["Username"] != null)
            {
                Username.Text = roamingSettings.Values["Username"].ToString();
                Password.Focus(FocusState.Pointer);
            }
            Live.IsChecked = true;
            await HockeyClient.Current.SendCrashesAsync();
            logon.IsEnabled = true;
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
            ////TODO : suppress
            //Username.Text = "BASSESSOR1";
            //Password.Password = "Test1234";
            //GlobalVariables.SelectedServer = ServerEnum.UAT1;
            //UAT2.IsChecked = false;
            //DEV.IsChecked = false;
            //MessageDialog a = new MessageDialog("HACK HACK HACK");
            //await a.ShowAsync();

            logon.IsEnabled = false;
            ShowProgress();
            if (String.IsNullOrWhiteSpace(Username.Text))
            {
                MessageDialog msg = new MessageDialog("Please enter a Username", "Required");
                await msg.ShowAsync();
                HideProgress();
                logon.IsEnabled = true;
                return;
            }
            if (String.IsNullOrWhiteSpace(Password.Password))
            {
                MessageDialog msg = new MessageDialog("Please enter a Password", "Required");
                await msg.ShowAsync();
                HideProgress();
                logon.IsEnabled = true;
                return;
            }

            m_dataManager = new DataManager(Username.Text, Password.Password);

            switch((await m_dataManager.ConnectToServer()))
            {
                case connectionStatus.Success:
                    this.Frame.Navigate(typeof(EvidenceViewer), m_dataManager);
                    return;
                case connectionStatus.BadPassword:
                    MessageDialog msg = new MessageDialog("Incorrect Password", "Required");
                    await msg.ShowAsync();
                    break;
                case connectionStatus.BadUsername:
                    MessageDialog msg0 = new MessageDialog("Incorrect Password", "Required");
                    await msg0.ShowAsync();
                    break;
                case connectionStatus.SqlError:
                    MessageDialog msg1 = new MessageDialog("Failed to register into the database", "Database error");
                    await msg1.ShowAsync();
                    break;
                case connectionStatus.AuthenticationFailed:
                    MessageDialog msg2 = new MessageDialog("Incorrect user or password", "Authentication failure");
                    await msg2.ShowAsync();
                    break;
                default:
                    MessageDialog msg3 = new MessageDialog("Unattented result", "Unknown error");
                    await msg3.ShowAsync();
                    break;
            }

            HideProgress();
            logon.IsEnabled = true;
        }

        private void ShowProgress()
        {
            SyncProgress.IsIndeterminate = true;
        }

        private void HideProgress()
        {
            SyncProgress.IsIndeterminate = false;
        }       

        private void pageTitle_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (LiveOptions.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                GlobalVariables.SelectedServer = ServerEnum.Live;
                LiveOptions.Visibility = Windows.UI.Xaml.Visibility.Visible;
                DEVoptions.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                DEVoptions2.Visibility = Windows.UI.Xaml.Visibility.Visible;
                LogonGrid.RowDefinitions[2].Height = new GridLength(70.0);
                LogonGrid.RowDefinitions[3].Height = new GridLength(0.0);
                LogonGrid.RowDefinitions[4].Height = new GridLength(0.0);
                Live.IsChecked = false;
                Live.IsEnabled = true;
                Practice.IsChecked = false;
                Practice.IsEnabled = true;
            }
            else
            {
                GlobalVariables.SelectedServer = ServerEnum.UAT1;
                LiveOptions.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                DEVoptions.Visibility = Windows.UI.Xaml.Visibility.Visible;
                DEVoptions2.Visibility = Windows.UI.Xaml.Visibility.Visible;
                LogonGrid.RowDefinitions[2].Height = new GridLength(0.0);
                LogonGrid.RowDefinitions[3].Height = new GridLength(70.0);
                LogonGrid.RowDefinitions[4].Height = new GridLength(70.0);
                Live.IsChecked = false;
                Live.IsEnabled = false;
                Practice.IsChecked = false;
                Practice.IsEnabled = false;
                Beta.IsChecked = false;
                Beta.IsEnabled = true;
            }
        }

        private void Live_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Live;
            Practice.IsChecked = false;
            Beta.IsChecked = false;
            UAT1.IsChecked = false;
            UAT2.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void Practice_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Practice;
            Beta.IsChecked = false;
            Live.IsChecked = false;
            UAT1.IsChecked = false;
            UAT2.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void Beta_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Beta;
            Practice.IsChecked = false;
            Live.IsChecked = false;
            UAT1.IsChecked = false;
            UAT2.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void UAT1_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.UAT1;
            Practice.IsChecked = false;
            Beta.IsChecked = false;
            Live.IsChecked = false;
            UAT2.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void UAT2_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.UAT2;
            Practice.IsChecked = false;
            Beta.IsChecked = false;
            Live.IsChecked = false;
            UAT1.IsChecked = false;
            DEV.IsChecked = false;
        }

        private void DEV_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.DEV;
            Practice.IsChecked = false;
            Beta.IsChecked = false;
            Live.IsChecked = false;
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
