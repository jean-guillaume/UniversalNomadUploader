using UniversalNomadUploader.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.DataModels.Enums;
using UniversalNomadUploader.APIUtils;
using UniversalNomadUploader.Exceptions;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace UniversalNomadUploader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Logon : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public Logon()
        {
            this.InitializeComponent();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            // Hide the status bar
            await statusBar.HideAsync();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        private async void logon_Click(object sender, RoutedEventArgs e)
        {
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
            Guid Session = await AuthenticationUtil.Authenticate(Username.Text, Password.Password, GlobalVariables.SelectedServer);
            if (Session != Guid.Empty)
            {
                String ErrorMessage = String.Empty;
                try
                {
                    await SQLUtils.UserUtil.InsertUser(new User() { Username = Username.Text, SessionID = Session });
                    await SQLUtils.UserUtil.UpdateUser(await APIUtils.UserUtil.GetProfile());
                }
                catch (ApiException exception)
                {
                    ErrorMessage = exception.Message;
                }
                HideProgress();
                if (ErrorMessage != String.Empty)
                {
                    MessageDialog msg = new MessageDialog(ErrorMessage, "Access denied");
                    await msg.ShowAsync();
                    HideProgress();
                    return;
                }
                this.Frame.Navigate(typeof(EvidenceView));
            }
            else
            {
                MessageDialog msg = new MessageDialog("Incorrect Username or Password", "Access denied");
                await msg.ShowAsync();
                HideProgress();
                return;
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
            GlobalVariables.SelectedServer = ServerEnum.Demo;
            Live.IsChecked = false;
            Beta.IsChecked = false;
        }

        private void Beta_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariables.SelectedServer = ServerEnum.Beta;
            Live.IsChecked = false;
            Demo.IsChecked = false;
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

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

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
    }
}
