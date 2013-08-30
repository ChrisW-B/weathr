//#define DEBUG_AGENT
using Microsoft.Phone.Controls;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Helpers;

namespace WeatherLock
{
    public partial class SettingsPivot : PhoneApplicationPage
    {
        #region variables
        PeriodicTask periodicTask;

        string periodicTaskName = "PeriodicAgent";
        public bool agentsAreEnabled = true;

        ProgressIndicator progTile;

        //Check to see if app is running as trial
        LicenseInformation licInfo;
        public bool isTrial;


        //save some typing
        dynamic store = IsolatedStorageSettings.ApplicationSettings;
        #endregion

        public SettingsPivot()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            licInfo = new LicenseInformation();
            isTrial = licInfo.IsTrial();

            ignoreCheckBoxEvents = true;

            periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            if (periodicTask != null)
            {
                PeriodicStackPanel.DataContext = periodicTask;
            }

            setValues();
            ignoreCheckBoxEvents = false;

            createDefaultLoc();
            addLocation();
        }
        private void setValues()
        {
            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {
                    enableLocation.IsChecked = true;
                }
                else
                {
                    enableLocation.IsChecked = false;
                }
            }
            else
            {
                store["enableLocation"] = true;
                enableLocation.IsChecked = true;
            }
            if (store.Contains("lockUnitIsC"))
            {
                if ((bool)store["lockUnitIsC"])
                {
                    lockCelsius.IsChecked = true;
                }
                else
                {
                    lockFahrenheit.IsChecked = true;
                }
            }
            else
            {
                store["lockUnitIsC"] = true;
                lockCelsius.IsChecked = true;
            }

            if (store.Contains("forecastUnitIsM"))
            {
                if ((bool)store["forecastUnitIsM"])
                {
                    metric.IsChecked = true;
                }
                else
                {
                    imperial.IsChecked = true;
                }
            }
            else
            {
                store["forecastUnitIsM"] = true;
                metric.IsChecked = true;
            }

            if (store.Contains("useFlickr"))
            {
                if ((bool)store["useFlickr"])
                {
                    flickrPics.IsChecked = true;
                    weatherGroup.IsEnabled = true;
                }
                else
                {
                    flickrPics.IsChecked = false;
                    weatherGroup.IsEnabled = false;
                }
            }
            else
            {
                store["useFlickr"] = true;
                flickrPics.IsChecked = true;
                weatherGroup.IsEnabled = true;
            }

            if (store.Contains("useWeatherGroup"))
            {
                if ((bool)store["useWeatherGroup"])
                {
                    weatherGroup.IsChecked = true;
                }
                else
                {
                    weatherGroup.IsChecked = false;
                }
            }
            else
            {
                store["useWeatherGroup"] = true;
                weatherGroup.IsChecked = true;
            }

            if (store.Contains("tempIsC"))
            {
                if ((bool)store["tempIsC"])
                {
                    celsius.IsChecked = true;
                }
                else
                {
                    fahrenheit.IsChecked = true;
                }
            }
            else
            {
                store["tempIsC"] = true;
                celsius.IsChecked = true;
            }

            if (store.Contains("windUnitIsM"))
            {
                if ((bool)store["windUnitIsM"])
                {
                    unitMiles.IsChecked = true;
                }
                else
                {
                    unitKms.IsChecked = true;
                }
            }
            else
            {
                store["windUnitIsM"] = false;
                unitKms.IsChecked = true;
            }
            if (store.Contains("tempAlert"))
            {
                if ((bool)store["tempAlert"])
                {
                    HiLo.IsChecked = true;
                }
                else
                {
                    HiLo.IsChecked = false;
                }
            }
            else
            {
                store["tempAlert"] = true;
                HiLo.IsChecked = true;
            }


            if (store.Contains("updateRate"))
            {
                string x = (string)store["updateRate"];
                switch (x)
                {
                    case "720":
                        UpdateBox.SelectedIndex = 0;

                        store["updateRate"] = "720";
                        break;
                    case "360":
                        UpdateBox.SelectedIndex = 1;

                        store["updateRate"] = "360";
                        break;
                    case "180":
                        UpdateBox.SelectedIndex = 2;

                        store["updateRate"] = "180";
                        break;
                    case "60":
                        UpdateBox.SelectedIndex = 3;

                        store["updateRate"] = "60";
                        break;
                    case "0":
                        UpdateBox.SelectedIndex = 4;

                        store["updateRate"] = "0";
                        break;
                }
            }
            else
            {
                UpdateBox.SelectedIndex = 0;

                store["updateRate"] = "720";
            }

            if (store.Contains("notifyMe"))
            {
                if ((bool)store["notifyMe"])
                {
                    notifyMe.IsChecked = true;
                }
                else
                {
                    notifyMe.IsChecked = false;
                }
            }
            else
            {
                notifyMe.IsChecked = false;
                store["notifyMe"] = false;
            }
        }

        //show/hide add button
        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((Pivot)sender).SelectedIndex)
            {
                case 0:
                    ApplicationBar.IsVisible = false;
                    break;
                case 1:
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    ApplicationBar.IsVisible = true;
                    break;
                case 2:
                    ApplicationBar.IsVisible = false;
                    break;
                case 3:
                    ApplicationBar.IsVisible = false;
                    break;
            }

        }

        //Now Pivot
        private void celsius_Checked(object sender, RoutedEventArgs e)
        {
            if (!ignoreCheckBoxEvents)
            {
                store["tempIsC"] = true;
                store["unitChanged"] = true;
            }
            else
            {
                return;
            }
        }
        private void fahrenheit_Checked(object sender, RoutedEventArgs e)
        {
            if (!ignoreCheckBoxEvents)
            {
                store["tempIsC"] = false;
                store["unitChanged"] = true;
            }
            else
            {
                return;
            }
        }
        private void unitKms_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["windUnitIsM"] = false;
                store["unitChanged"] = true;
            }
        }
        private void unitMiles_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["windUnitIsM"] = true;
                store["unitChanged"] = true;
            }
        }
        private void flickrPics_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["useFlickr"] = true;
                weatherGroup.IsEnabled = true;
            }
        }
        private void flickrPics_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["useFlickr"] = false;
                weatherGroup.IsEnabled = false;
            }
        }
        private void weatherGroup_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["useWeatherGroup"] = true;
                store["groupChanged"] = true;
            }
        }
        private void weatherGroup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["useWeatherGroup"] = false;
                store["groupChanged"] = true;
            }
        }

        //Location Pivot
        #region variables
        ObservableCollection<Locations> locations = new ObservableCollection<Locations>();

        #endregion
        private void enableLocation_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["enableLocation"] = true;
            }
        }
        private void enableLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["enableLocation"] = false;
            }
        }
        private void initializeLocations()
        {
            locations.Add(new Locations() { LocName = "Current Location", CurrentLoc = true, ImageSource = "/Images/favs.png" });
            LocationListBox.ItemsSource = locations;
            createDefaultLoc();
        }
        private void restoreLocations()
        {
            if (store.Contains("locations"))
            {
                locations = (ObservableCollection<Locations>)store["locations"];
                LocationListBox.ItemsSource = locations;

                backupLocations();
            }
        }
        private void backupLocations()
        {
            store["locations"] = locations;
            store.Save();
        }
        private void addLocation()
        {
            if (locations.Count == 0)
            {
                initializeLocations();
            }
            if (store.Contains("locAdded") && store.Contains("newLocation"))
            {
                if ((bool)store["locAdded"])
                {
                    foreach (Locations loc in locations)
                    {
                        if (store["newLocation"].Contains(loc.LocName))
                        {
                            return;
                        }
                    }
                    String lat;
                    String lon;
                    store["locAdded"] = false;
                    String locationName = store["newLocation"];
                    String locationUrl = store["newUrl"];
                    if (!locationName.Contains("Current Location"))
                    {
                        String[] location = store["newLoc"];
                        lat = location[0];
                        lon = location[1];
                    }
                    else
                    {
                        lat = null;
                        lon = null;
                    }
                    if (!locationName.Contains("Current Location"))
                    {
                        locations.Add(new Locations() { LocName = locationName, CurrentLoc = false, LocUrl = locationUrl, Lat = lat, Lon = lon, ImageSource = "/Images/Clear.png" });
                    }
                    else
                    {
                        locations.Add(new Locations() { LocName = locationName, CurrentLoc = true, LocUrl = locationUrl, Lat = lat, Lon = lon, ImageSource = "/Images/Clear.png" });
                    }
                    LocationListBox.ItemsSource = locations;
                    backupLocations();
                }
                else
                {
                    restoreLocations();
                }
            }
        }
        public class Locations
        {
            public string LocName { get; set; }
            public string LocUrl { get; set; }
            public bool CurrentLoc { get; set; }
            public string Lat { get; set; }
            public string Lon { get; set; }
            public string ImageSource { get; set; }
        }
        private void createDefaultLoc()
        {
            if (!store.Contains("defaultLocation") || !store.Contains("defaultUrl") || !store.Contains("defaultCurrent"))
            {
                store["defaultLocation"] = "Current Location";
                store["defaultUrl"] = "null";
                store["defaultCurrent"] = true;

                store.Save();
            }
        }
        private void delete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            removeLocation(menuItem.Tag.ToString());
        }
        private void pin_Click(object sender, RoutedEventArgs e)
        {
            if (!isTrial)
            {

                MenuItem menuItem = (MenuItem)sender;

                String[] resArray = getArray(menuItem.Tag.ToString());

                foreach (ShellTile tile in ShellTile.ActiveTiles)
                {
                    if (tile.NavigationUri.ToString().Contains(resArray[0]) && !tile.NavigationUri.ToString().Contains("isCurrent=True"))
                    {
                        MessageBoxResult m = MessageBox.Show("Already Pinned!", "", MessageBoxButton.OK);
                        return;
                    }
                    else if (tile.NavigationUri.ToString().Contains("isCurrent=True") && resArray[2]=="True")
                    {
                        MessageBoxResult m = MessageBox.Show("Already Pinned!", "", MessageBoxButton.OK);
                        return;
                    }
                }

                if (checkPeriodic(sender, e))
                {
                    IconicTileData locTile = new IconicTileData
                    {
                        IconImage = new Uri("SunCloud202.png", UriKind.Relative),
                        SmallIconImage = new Uri("SunCloud110.png", UriKind.Relative),
                        Title = resArray[0]
                    };

                   


                    ShellTile.Create(new Uri("/MainPage.xaml?cityName=" + resArray[0] + "&url=" + resArray[1] + "&isCurrent=" + resArray[2] + "&lat=" + resArray[3] + "&lon=" + resArray[4], UriKind.Relative), locTile, true);

                }
                else
                {
                    MessageBoxResult m = MessageBox.Show("Multiple location Pinning is only supported in the full version. Buy now?", "Trial Mode", MessageBoxButton.OKCancel);
                    if (m == MessageBoxResult.OK)
                    {
                        MarketplaceDetailTask task = new MarketplaceDetailTask();
                        task.Show();
                    }
                }
            }
        }
        private bool checkPeriodic(object sender, RoutedEventArgs e)
        {
            if (store.Contains("periodicStarted"))
            {
                if (!(bool)store["periodicStarted"])
                {
                    if (!store.Contains("manPerOff"))
                    {
                        StartPeriodicAgent();
                        return true;
                    }
                    else if (!(bool)(store["manPerOff"]))
                    {
                        StartPeriodicAgent();
                        return true;
                    }
                }
            }
            else
            {
                store["periodicStarted"] = false;
                pin_Click(sender, e);
                return false;
            }
            return true;
        }
        private void default_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            String[] resArray = getArray(menuItem.Tag.ToString());
            changeDefault(menuItem.Tag.ToString());
            store["defaultLocation"] = resArray[0];
            store["defaultUrl"] = resArray[1];
            store["defaultCurrent"] = resArray[2];
        }
        private void changeDefault(string loc)
        {
            //set new default
            foreach (Locations location in locations)
            {
                if (location.LocName == loc)
                {
                    location.ImageSource = "/Images/favs.png";
                }
                else
                {
                    location.ImageSource = "/Images/Clear.png";
                }
            }
            LocationListBox.ItemsSource = null;
            LocationListBox.ItemsSource = locations;
            backupLocations();

        }
        private String[] getArray(string loc)
        {
            foreach (Locations location in locations)
            {
                if (location.LocName == loc)
                {
                    String[] thisLocation =  {
                                               location.LocName,
                                               location.LocUrl,
                                               Convert.ToString(location.CurrentLoc),
                                               location.Lat,
                                               location.Lon,
                                               location.ImageSource
                                           };
                    return thisLocation;
                }
            }
            return null;
        }
        private void removeLocation(string loc)
        {
            foreach (Locations location in locations)
            {
                if (location.LocName == loc)
                {
                    locations.Remove(location);
                    restoreLocations();
                    break;
                }
            }
        }
        private void LocationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                if (LocationListBox.SelectedIndex != -1)
                {
                    var resArray = locations.ToArray()[LocationListBox.SelectedIndex];
                    string current = (string)resArray.LocName;
                    store["isCurrent"] = resArray.CurrentLoc;
                    store["locUrl"] = resArray.LocUrl;
                    store["locName"] = resArray.LocName;
                    store["locChanged"] = true;
                    String[] location = { resArray.Lat, resArray.Lon };
                    store["loc"] = location;
                    store.Save();
                }
            }

        }
        private void locationName_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBoxItem lbItem = (ListBoxItem)sender;
            String loc = lbItem.Content.ToString();

            String[] resArray = getArray(loc);

            NavigationService.Navigate(new Uri("/MainPage.xaml?cityName=" + resArray[0] + "&url=" + resArray[1] + "&isCurrent=" + resArray[2] + "&lat=" + resArray[3] + "&lon=" + resArray[4], UriKind.Relative));
        }

        //Forecast Pivot
        private void metric_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["forecastUnitIsM"] = true;
                store["unitChanged"] = true;
                metric.IsChecked = true;
            }
        }
        private void imperial_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["forecastUnitIsM"] = false;
                store["unitChanged"] = true;
                imperial.IsChecked = true;
            }
        }

        //Tile and Lock pivot
        private void RemoveAgent(string name)
        {
            try
            {
                store["periodicStarted"] = false;
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }
        private void notifyMe_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["notifyMe"] = true;
                notifyMe.IsChecked = true;
            }
        }
        private void notifyMe_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["notifyMe"] = false;
                notifyMe.IsChecked = false;
            }
        }
        private async void btnGoToLockSettings_Click(object sender, RoutedEventArgs e)
        {
            // Launch URI for the lock screen settings screen.
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }
        private void changeLoc_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/ChangeLocation.xaml", UriKind.Relative));
        }
        private void UpdateBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                if (!isTrial)
                {
                    int y = UpdateBox.SelectedIndex;
                    switch (y)
                    {
                        case 0:
                            store["updateRate"] = "720";
                            break;
                        case 1:
                            store["updateRate"] = "360";
                            break;
                        case 2:
                            store["updateRate"] = "180";
                            break;
                        case 3:
                            store["updateRate"] = "60";
                            break;
                        case 4:
                            store["updateRate"] = "0";
                            break;
                    }
                }
                else
                {
                    store["updateRate"] = "720";
                    UpdateBox.SelectedIndex = 0;
                }
                store.Save();
            }
        }
        private void HiLo_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["tempAlert"] = true;
            }
        }
        private void HiLo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["tempAlert"] = false;
            }
        }
        private void StartPeriodicAgent()
        {
            store["periodicStarted"] = true;

            // Variable for tracking enabled status of background agents for this app.
            agentsAreEnabled = true;

            // Obtain a reference to the period task, if one exists
            periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule
            if (periodicTask != null)
            {
                RemoveAgent(periodicTaskName);
            }

            periodicTask = new PeriodicTask(periodicTaskName);

            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the device.
            periodicTask.Description = "Updates tile and Lockscreen with weather conditions and forecast";

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(periodicTask);
                PeriodicStackPanel.DataContext = periodicTask;
               // #if(DEBUG_AGENT)
                //ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10));
               // #endif

            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background tasks have been disabled for Weathr. Tile and Lockscreen will not update");
                    agentsAreEnabled = false;
                    PeriodicCheckBox.IsChecked = false;
                    UpdateBox.IsEnabled = false;
                }

                if (exception.Message.Contains("You have too many background tasks. Remove some and try again"))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
                PeriodicCheckBox.IsChecked = false;
                UpdateBox.IsEnabled = false;
            }
            catch (SchedulerServiceException)
            {
                // No user action required.
                PeriodicCheckBox.IsChecked = false;
                UpdateBox.IsEnabled = false;
            }
        }
        bool ignoreCheckBoxEvents = false;
        private void PeriodicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
                return;
            if (!isTrial)
            {
                UpdateBox.IsEnabled = true;
            }
            store["manPerOff"] = false;
            StartPeriodicAgent();

        }
        private void PeriodicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
                return;

            UpdateBox.IsEnabled = false;
            store["manPerOff"] = true;
            RemoveAgent(periodicTaskName);

        }
        private void lockCelsius_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["lockUnitIsC"] = true;
                UpdateTileFromApp update = new UpdateTileFromApp();
                lockCelsius.IsChecked = true;
            }
        }
        private void lockFahrenheit_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
            {
                return;
            }
            else
            {
                store["lockUnitIsC"] = false;
                UpdateTileFromApp update = new UpdateTileFromApp();
                lockFahrenheit.IsChecked = true;
            }
        }
        private void startTileProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progTile = new ProgressIndicator();
            progTile.Text = "Updating Tile...";
            progTile.IsIndeterminate = true;
            progTile.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progTile);
        }

       

        private void UpdateBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isTrial)
            {
                MessageBoxResult m = MessageBox.Show("Trial Mode can only be updated every 12 hours, to save on cost. Buy now?", "Trial Mode", MessageBoxButton.OKCancel);
                if (m == MessageBoxResult.OK)
                {
                    MarketplaceDetailTask task = new MarketplaceDetailTask();
                    task.Show();
                }
            }
        }

        //AppMenu Bar
        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddLocation.xaml", UriKind.Relative));
        }
    }
}