using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

namespace WeatherLock
{
    public partial class SelectLocation : PhoneApplicationPage
    {
        #region variables
        private String[] locationArray;
        ObservableCollection<Locations> locations = new ObservableCollection<Locations>();
        dynamic store = IsolatedStorageSettings.ApplicationSettings;
        #endregion
        public SelectLocation()
        {
            InitializeComponent();
            restoreLocations();
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            addLocation();
        }

        public void restoreLocations()
        {
            if (store.Contains("locations"))
            {
                locations = (ObservableCollection<Locations>)store["locations"];
                LocationListBox.ItemsSource = locations;
            }
        }

        public void backupLocations()
        {
            store["locations"] = locations;
        }

        public void addLocation()
        {
            if (store.Contains("locAdded") && store.Contains("newLocation"))
            {
                if ((bool)store["locAdded"])
                {
                    store["locAdded"] = false;
                    String locationName = store["newLocation"];
                    locations.Add(new Locations() { LocName = locationName });
                    LocationListBox.ItemsSource = locations;
                }
            }
        }

        public class Locations
        {
            public string LocName { get; set; }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddLocation.xaml", UriKind.Relative));
        }
    }
}