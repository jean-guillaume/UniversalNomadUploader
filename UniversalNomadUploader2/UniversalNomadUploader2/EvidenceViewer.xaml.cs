﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.DefaultClasses;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace UniversalNomadUploader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EvidenceViewer : Page
    {
        DBManager db = null;
        ObservableDictionary defaultViewModel = new ObservableDictionary();
        IEnumerable<IGrouping<String, Evidence>> evidenceGrouped;

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public IEnumerable<IGrouping<String, Evidence>> EvidenceGrouped
        {
            get
            {
                return evidenceGrouped;
            }
        }

        public EvidenceViewer()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            if (db == null)
            {
                db = new DBManager("UniversalNomadUploader.db");
            }
            else
            {
                db = (DBManager)e.Parameter;
            }
            //db.add();
            evidenceGrouped = db.readAllEvidence();
            //var res = evidenceGrouped.GroupBy(x => x.Name == "" ? " " : x.Name.Substring(0, 1)).OrderBy(x => x.Key);
            this.DefaultViewModel["EvidenceItems"] = evidenceGrouped;
            //itemListView.ItemsSource = evidenceGrouped;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EnterPreviewMode_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CaptureView), db);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StartStopRecord_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TakePicture_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LeavePreviewMode_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}