using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

namespace WeatherLock
{
    public partial class AddLocation : PhoneApplicationPage
    {
        #region variables
        ObservableCollection<LocResults> locResults = new ObservableCollection<LocResults>();
        ProgressIndicator progSearch;
        dynamic store = IsolatedStorageSettings.ApplicationSettings;
        bool searchComplete;
        #endregion

        public AddLocation()
        {
            InitializeComponent();
        }

        //Location Pivot
        void seachResults(object sender, DownloadStringCompletedEventArgs e)
        {
            locResults.Add(new LocResults() { LocName = "Current Location", LocUrl = "http://bing.com" });
            //HAP needs a HTML-Document as it is based on Linq/Xpath
            XDocument doc = new XDocument();
            doc = XDocument.Parse(e.Result);

            //search the html document for the search result, based on Xpath:
            var locNames = doc.Descendants().Elements("name");
            foreach (XElement elm in doc.Descendants().Elements("name"))
            {
                var locationName = (string)elm.Value;
                var wuUrlNode = elm.NextNode.NextNode.NextNode.NextNode.NextNode.NextNode;
                var wuUrl = wuUrlNode.ToString();
                wuUrl = wuUrl.Replace("<l>", "");
                wuUrl = wuUrl.Replace("</l>", "");

                locResults.Add(new LocResults() { LocName = locationName, LocUrl = wuUrl });
                ResultListBox.ItemsSource = locResults;

            }
            progSearch.IsVisible = false;
        }
        private void SearchBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            
            if (SearchBox.Text == "enter location" || SearchBox.Text == (string)store["locName"])
            {
                SearchBox.Text = "";
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchComplete == false)
            {
                //Start Progress Bar
                startSearchProg();

                //clear previous results, if there are any since this is a non-MVVM sample:
                ResultListBox.ItemsSource = null;
                locResults.Clear();
                //create searchUri
                //search is based on the user's CurrentCulture
                string searchUri = string.Format("http://autocomplete.wunderground.com/aq?query={0}&format=XML", SearchBox.Text);

                //start WebClient (this way it will work on WP7 & WP8)
                WebClient client = new WebClient();
                //Add this header to asure that new results will be downloaded, also if the search term has not changed
                // otherwise it would not load again the result string (because of WP cashing)
                client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
                //Download the String and add new EventHandler once the Download has completed
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(seachResults);
                client.DownloadStringAsync(new Uri(searchUri));
            }
        }
        private void ResultListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultListBox.SelectedIndex > -1)
            {
                var resArray = locResults.ToArray()[ResultListBox.SelectedIndex];
                store["locAdded"] = true;
                store["newLocation"] = resArray.LocName;
                store["newUrl"] = resArray.LocUrl;
                store.Save();
                if (!"Current Location".Equals((String)(store["newLocation"])))
                {
                    string googleUrl = "http://maps.googleapis.com/maps/api/geocode/xml?address=" + store["newLocation"] + "&sensor=true";

                    WebClient client = new WebClient();
                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(getCoordinates);
                    client.DownloadStringAsync(new Uri(googleUrl));

                }
                else
                {
                    SearchBox.Text = store["newLocation"];
                    NavigationService.GoBack();

                }
            }
        }
        private void getCoordinates(object sender, DownloadStringCompletedEventArgs e)
        {
            XDocument doc = XDocument.Parse(e.Result);
            var location = doc.Element("GeocodeResponse").Element("result").Element("geometry").Element("location");
            string lat = (string)location.Element("lat").Value;
            string lng = (string)location.Element("lng").Value;
            String[] loc = { lat, lng };
            store["newLoc"] = loc;
            searchComplete = true;
            store.Save();
            SearchBox.Text = store["newLocation"];
            NavigationService.GoBack();
        }
        private void startSearchProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progSearch = new ProgressIndicator();
            progSearch.Text = "Seaching";
            progSearch.IsIndeterminate = true;
            progSearch.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progSearch);
        }
        public class LocResults
        {
            public string LocUrl { get; set; }
            public string LocName { get; set; }
        }

    }
}