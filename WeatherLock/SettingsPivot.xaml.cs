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
using System.Collections.Generic;
using WeatherData;

namespace WeatherLock
{
    public partial class SettingsPivot : PhoneApplicationPage
    {
        #region variables

        //Flags for getting data/comparing city
        private Boolean tempUnitIsC;
        private Boolean isCurrent;
        private int locationSearchTimes;
        public bool agentsAreEnabled = true;

        //info
        private String url;
        private String locUrl;
        private String defaultCityName;
        private String apiKey = "102b8ec7fbd47a05";
       

        //coordinates
        private String latitude;
        private String longitude;

        //handling errors with getting data
        private Boolean error;
        private string errorText;

        //data lists for updating tiles
        List<Pins> pinnedList = new List<Pins>();
        List<Pins> pinnedListCopy = new List<Pins>();

        //tile info
        public class Pins
        {
            public string LocName;
            public string LocUrl;
            public bool currentLoc;
            public bool updated;
        }

        //periodic task starting
        PeriodicTask periodicTask;
        string periodicTaskName = "PeriodicAgent";

        //Progress bar
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
            //Testing Key
            apiKey = "fb1dd3f4321d048d";
            SystemTray.Opacity = .5;
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

        ///checkboxes and buttons
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

                Locations location = getLocation(menuItem.Tag.ToString());

                foreach (ShellTile tile in ShellTile.ActiveTiles)
                {
                    if (tile.NavigationUri.ToString().Contains(location.LocName) && !tile.NavigationUri.ToString().Contains("isCurrent=True"))
                    {
                        MessageBoxResult m = MessageBox.Show("Already Pinned!", "", MessageBoxButton.OK);
                        return;
                    }
                    else if (tile.NavigationUri.ToString().Contains("isCurrent=True") && location.IsCurrent)
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
                        Title = location.LocName
                    };




                    ShellTile.Create(new Uri(
                "/MainPage.xaml?cityName=" + location.LocName
                + "&url=" + location.LocUrl
                + "&isCurrent=" + location.IsCurrent
                + "&lat=" + location.Lat
                + "&lon=" + location.Lon,
                UriKind.Relative),
                locTile,
                true);

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
        private void default_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Locations location = getLocation(menuItem.Tag.ToString());
            changeDefault(menuItem.Tag.ToString());
            store["defaultLocation"] = location.LocName;
            store["defaultUrl"] = location.LocUrl;
            store["defaultCurrent"] = location.IsCurrent;
        }

        ///handling data
        private void initializeLocations()
        {
            locations.Add(new Locations() { LocName = "Current Location", IsCurrent = true, ImageSource = "/Images/favs.png" });
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
                        locations.Add(new Locations() { LocName = locationName, IsCurrent = false, LocUrl = locationUrl, Lat = lat, Lon = lon, ImageSource = "/Images/Clear.png" });
                    }
                    else
                    {
                        locations.Add(new Locations() { LocName = locationName, IsCurrent = true, LocUrl = locationUrl, Lat = lat, Lon = lon, ImageSource = "/Images/Clear.png" });
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
        private Locations getLocation(string loc)
        {
            foreach (Locations location in locations)
            {
                if (location.LocName == loc)
                {
                    return location;
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
                    store["isCurrent"] = resArray.IsCurrent;
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

            Locations location = getLocation(loc);

            NavigationService.Navigate(new Uri(
                "/MainPage.xaml?cityName=" + location.LocName
                + "&url=" + location.LocUrl
                + "&isCurrent=" + location.IsCurrent
                + "&lat=" + location.Lat
                + "&lon=" + location.Lon,
                UriKind.Relative));
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
                //#if(DEBUG_AGENT)
                //ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10));
                //#endif

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
                startTileProg();
                store["lockUnitIsC"] = true;
                updateTileFromApp();
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
                startTileProg();
                store["lockUnitIsC"] = false;
                updateTileFromApp();
                lockFahrenheit.IsChecked = true;
            }
        }
        private void startTileProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progTile = new ProgressIndicator();
            progTile.Text = "Updating Tiles...";
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

        //Background stuff to update the tiles units
        private void updateTileFromApp()
        {
            error = false;
            checkUnits();
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.OriginalString != "/")
                {
                    String[] uriSplit = tile.NavigationUri.ToString().Split('&');

                    bool isCurrentLoc = Convert.ToBoolean(uriSplit[2].Split('=')[1]);
                    string locationUrl = uriSplit[1].Split('=')[1];
                    string locationName = uriSplit[0].Split('=')[1];

                    pinnedList.Add(new Pins { LocName = locationName, LocUrl = locationUrl, currentLoc = isCurrentLoc, updated = false });
                }
                else if (tile.NavigationUri.OriginalString == "/")
                {
                    string locationName = "default location";
                    pinnedList.Add(new Pins { LocName = locationName, updated = false });
                }
            }

            //prevent enumeration errors
            pinnedListCopy = pinnedList;



            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.OriginalString == "/")
                {
                    if (store.Contains("defaultLocation") && store.Contains("defaultUrl") && store.Contains("defaultCurrent"))
                    {
                        defaultCityName = (string)store["defaultLocation"];
                        locUrl = (string)store["defaultUrl"];
                        isCurrent = Convert.ToBoolean(store["defaultCurrent"]);
                    }
                    else
                    {
                        progTile.IsVisible = false;
                    }
                }
            }



            updateDefault();
            updateOthers();
            //save the time of the last time the app was run
            store["lastRun"] = DateTime.Now.ToString();
            store.Save();

        }

        //Update each type of tile
        private void updateDefault()
        {
            //set location
            locationSearchTimes = 0;
            checkLocation();

            //check get url
            bool mainCurrent = isCurrent;
            url = getUrl(mainCurrent);

            //check units
            checkUnits();

            //get weather data
            var client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(updateMainTile);
            client.DownloadStringAsync(new Uri(url));
        }

        private void updateOthers()
        {
            foreach (Pins pinnedTile in pinnedList)
            {
                if (!pinnedTile.LocName.Contains("default location") && !pinnedTile.currentLoc)
                {
                    String pinnedUrl = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + pinnedTile.LocUrl + ".xml";
                    checkUnits();
                    var client = new WebClient();

                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(updateOtherTiles);
                    client.DownloadStringAsync(new Uri(pinnedUrl));
                }
                if (pinnedTile.currentLoc && !pinnedTile.LocName.Contains("default location"))
                {
                    locationSearchTimes = 0;
                    checkLocation();
                    string pinnedUrl = getUrl(pinnedTile.currentLoc);
                    checkUnits();
                    var client = new WebClient();

                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(updateLocAwareTile);
                    client.DownloadStringAsync(new Uri(pinnedUrl));
                }
            }
        }

        //Async Calls to get weather data for each tile type
        private void updateMainTile(object sender, DownloadStringCompletedEventArgs e)
        {
            WeatherInfo mainTileWeather = new WeatherInfo();
            if (!e.Cancelled && e.Error == null)
            {
                Uri normalIcon = new Uri("/SunCloud202.png", UriKind.Relative);
                Uri smallIcon = new Uri("/SunCloud110.png", UriKind.Relative);

                XDocument doc = XDocument.Parse(e.Result);
                XElement weatherError = doc.Element("response").Element("error");
                if (weatherError == null && !error)
                {
                    WeatherToClass creator = new WeatherToClass();
                    mainTileWeather = creator.weatherToClass(doc);

                    string cityName = mainTileWeather.city + ", " + mainTileWeather.state;

                    //get weather icons
                    Uri[] weatherIcons = getWeatherIcons(mainTileWeather.currentConditions);
                    normalIcon = weatherIcons[0];
                    smallIcon = weatherIcons[1];

                    //convert temps to ints
                    convertTemp getTemp;
                    string todayHigh;
                    string todayLow;
                    string tomorrowHigh;
                    string tomorrowLow;
                    string origTemp;

                    if (tempUnitIsC)
                    {
                        getTemp = new convertTemp(mainTileWeather.tempC);
                        origTemp = mainTileWeather.tempC;
                        todayHigh = mainTileWeather.todayHighC;
                        todayLow = mainTileWeather.todayLowC;
                        tomorrowHigh = mainTileWeather.tomorrowHighC;
                        tomorrowLow = mainTileWeather.tomorrowLowC;
                    }
                    else
                    {
                        getTemp = new convertTemp(mainTileWeather.tempF);
                        origTemp = mainTileWeather.tempF;
                        todayHigh = mainTileWeather.todayHighF;
                        todayLow = mainTileWeather.todayLowF;
                        tomorrowHigh = mainTileWeather.tomorrowHighF;
                        tomorrowLow = mainTileWeather.tomorrowLowF;
                    }
                    int temp = getTemp.temp;

                    if (temp > 99)
                    {
                        temp = 99;
                    }
                    else if (temp < 1)
                    {
                        temp = 1;
                    }

                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.OriginalString == "/")
                        {
                            if (isCurrent)
                            {
                                IconicTileData TileData = new IconicTileData
                                {
                                    IconImage = normalIcon,
                                    SmallIconImage = smallIcon,
                                    Title = cityName,
                                    Count = temp,
                                    WideContent1 = string.Format("Currently: " + mainTileWeather.currentConditions + ", " + origTemp + " degrees"),
                                    WideContent2 = string.Format("Today: " + mainTileWeather.todayShort + " " + todayHigh + "/" + todayLow),
                                    WideContent3 = string.Format("Tomorrow: " + mainTileWeather.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                };
                                tile.Update(TileData);

                                //mark tile as updated
                                markUpdated("default location", mainTileWeather.city, mainTileWeather.state, false);

                                //stop looping
                                break;
                            }
                            else if (store.Contains("defaultLocation"))
                            {
                                if (store["defaultLocation"] == cityName && store["defaultLocation"] != "Current Location")
                                {
                                    IconicTileData TileData = new IconicTileData
                                    {
                                        IconImage = normalIcon,
                                        SmallIconImage = smallIcon,
                                        Title = cityName,
                                        Count = temp,
                                        WideContent1 = string.Format("Currently: " + mainTileWeather.currentConditions + ", " + temp + " degrees"),
                                        WideContent2 = string.Format("Today: " + mainTileWeather.todayShort + " " + todayHigh + "/" + todayLow),
                                        WideContent3 = string.Format("Tomorrow: " + mainTileWeather.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                    };
                                    tile.Update(TileData);

                                    //mark tile as updated
                                    markUpdated("default location", mainTileWeather.city, mainTileWeather.state, false);

                                    //stop looping
                                    break;
                                }

                            }
                        }
                    }
                }
                else if (!error)
                {
                    mainTileWeather.error = doc.Element("response").Element("error").Element("description").Value;
                    errorText = mainTileWeather.error;
                    error = true;

                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.OriginalString == "/")
                        {
                            IconicTileData TileData = new IconicTileData
                            {
                                IconImage = normalIcon,
                                SmallIconImage = smallIcon,
                                Title = "Error",
                                Count = 0,
                                WideContent1 = errorText,
                                WideContent2 = "Try checking location services",
                                WideContent3 = ""
                            };
                            tile.Update(TileData);

                            //mark tile as updated
                            markUpdated("default location", "null", "null", false);

                            //stop looping
                            break;
                        }
                    }
                }
                else if (error)
                {
                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.OriginalString == "/")
                        {
                            IconicTileData TileData = new IconicTileData
                            {
                                IconImage = normalIcon,
                                SmallIconImage = smallIcon,
                                Title = "Error",
                                Count = 0,
                                WideContent1 = errorText,
                                WideContent2 = "Try checking location services",
                                WideContent3 = ""
                            };
                            tile.Update(TileData);

                            //mark tile as updated
                            markUpdated("default location", "null", "null", false);

                            //stop looping
                            break;
                        }
                    }
                }
                if (finished())
                {
                    progTile.IsVisible = false;
                    return;
                }
            }
            else
            {
                progTile.IsVisible = false;
                return;
            }
        }
        private void updateOtherTiles(object sender, DownloadStringCompletedEventArgs e)
        {
            WeatherInfo otherTileWeather = new WeatherInfo();

            if (!e.Cancelled && e.Error == null)
            {
                Uri normalIcon = new Uri("/SunCloud202.png", UriKind.Relative);
                Uri smallIcon = new Uri("/SunCloud110.png", UriKind.Relative);

                XDocument doc = XDocument.Parse(e.Result);
                XElement weatherError = doc.Element("response").Element("error");
                if (weatherError == null && !error)
                {

                    WeatherToClass creator = new WeatherToClass();
                    otherTileWeather = creator.weatherToClass(doc);

                    string cityName = otherTileWeather.city + ", " + otherTileWeather.state;


                    //get weather icons
                    Uri[] weatherIcons = getWeatherIcons(otherTileWeather.currentConditions);
                    normalIcon = weatherIcons[0];
                    smallIcon = weatherIcons[1];

                    //convert temps to ints
                    convertTemp getTemp;
                    string todayHigh;
                    string todayLow;
                    string tomorrowHigh;
                    string tomorrowLow;
                    string origTemp;

                    if (tempUnitIsC)
                    {
                        getTemp = new convertTemp(otherTileWeather.tempC);
                        origTemp = otherTileWeather.tempC;
                        todayHigh = otherTileWeather.todayHighC;
                        todayLow = otherTileWeather.todayLowC;
                        tomorrowHigh = otherTileWeather.tomorrowHighC;
                        tomorrowLow = otherTileWeather.tomorrowLowC;
                    }
                    else
                    {
                        getTemp = new convertTemp(otherTileWeather.tempF);
                        origTemp = otherTileWeather.tempF;
                        todayHigh = otherTileWeather.todayHighF;
                        todayLow = otherTileWeather.todayLowF;
                        tomorrowHigh = otherTileWeather.tomorrowHighF;
                        tomorrowLow = otherTileWeather.tomorrowLowF;
                    }
                    int temp = getTemp.temp;

                    if (temp > 99)
                    {
                        temp = 99;
                    }
                    else if (temp < 1)
                    {
                        temp = 1;
                    }


                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.ToString() != "/")
                        {
                            foreach (Pins pin in pinnedList)
                            {
                                //get name and location from tile url
                                string tileLoc = tile.NavigationUri.ToString().Split('&')[0].Split('=')[1];
                                bool tileIsCurrent = Convert.ToBoolean(tile.NavigationUri.ToString().Split('&')[2].Split('=')[1]);

                                if (tileIsCurrent)
                                {
                                    break;
                                }
                                else if (((tileLoc == cityName && pin.LocName == cityName) || (pin.LocName.Split(',')[0].Contains(otherTileWeather.city) && tileLoc.Split(',')[0].Contains(otherTileWeather.city) && pin.LocName.Split(',')[1].Contains(otherTileWeather.state) && tileLoc.Split(',')[1].Contains(otherTileWeather.state))) && !pin.updated)
                                {
                                    //Update Tile
                                    IconicTileData TileData = new IconicTileData
                                    {
                                        IconImage = normalIcon,
                                        SmallIconImage = smallIcon,
                                        Title = cityName,
                                        Count = temp,
                                        WideContent1 = string.Format("Currently: " + otherTileWeather.currentConditions + ", " + origTemp + " degrees"),
                                        WideContent2 = string.Format("Today: " + otherTileWeather.todayShort + " " + todayHigh + "/" + todayLow),
                                        WideContent3 = string.Format("Tomorrow: " + otherTileWeather.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                    };
                                    tile.Update(TileData);

                                    //mark the tile as finished updating
                                    markUpdated(cityName, otherTileWeather.city, otherTileWeather.state, false);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                if (finished())
                {
                    progTile.IsVisible = false;
                    return;
                }
            }
            else
            {
                progTile.IsVisible = false;
                return;
            }
        }
        private void updateLocAwareTile(object sender, DownloadStringCompletedEventArgs e)
        {
            WeatherInfo weatherCurrent = new WeatherInfo();

            if (!e.Cancelled && e.Error == null)
            {
                Uri normalIcon = new Uri("/SunCloud202.png", UriKind.Relative);
                Uri smallIcon = new Uri("/SunCloud110.png", UriKind.Relative);

                XDocument doc = XDocument.Parse(e.Result);
                XElement weatherError = doc.Element("response").Element("error");
                if (weatherError == null && !error)
                {
                    WeatherToClass creator = new WeatherToClass();
                    weatherCurrent = creator.weatherToClass(doc);
                    
                    string cityName = weatherCurrent.city + ", " + weatherCurrent.state;
                    

                    //get weather icons
                    Uri[] weatherIcons = getWeatherIcons(weatherCurrent.currentConditions);
                    normalIcon = weatherIcons[0];
                    smallIcon = weatherIcons[1];

                    //convert temps to ints
                    convertTemp getTemp;
                    string todayHigh;
                    string todayLow;
                    string tomorrowHigh;
                    string tomorrowLow;
                    string origTemp;

                    if (tempUnitIsC)
                    {
                        getTemp = new convertTemp(weatherCurrent.tempC);
                        origTemp = weatherCurrent.tempC;
                        todayHigh = weatherCurrent.todayHighC;
                        todayLow = weatherCurrent.todayLowC;
                        tomorrowHigh = weatherCurrent.tomorrowHighC;
                        tomorrowLow = weatherCurrent.tomorrowLowC;
                    }
                    else
                    {
                        getTemp = new convertTemp(weatherCurrent.tempF);
                        origTemp = weatherCurrent.tempF;
                        todayHigh = weatherCurrent.todayHighF;
                        todayLow = weatherCurrent.todayLowF;
                        tomorrowHigh = weatherCurrent.tomorrowHighF;
                        tomorrowLow = weatherCurrent.tomorrowLowF;
                    }
                    int temp = getTemp.temp;

                    if (temp > 99)
                    {
                        temp = 99;
                    }
                    else if (temp < 1)
                    {
                        temp = 1;
                    }

                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.ToString() != "/")
                        {
                            //get name and location from tile url
                            string tileLoc = tile.NavigationUri.ToString().Split('&')[0].Split('=')[1];
                            bool tileIsCurrent = Convert.ToBoolean(tile.NavigationUri.ToString().Split('&')[2].Split('=')[1]);

                            foreach (Pins pin in pinnedList)
                            {
                                if (tileIsCurrent && pin.currentLoc && !pin.LocName.Contains("default location"))
                                {
                                    //Update Tile
                                    IconicTileData TileData = new IconicTileData
                                    {
                                        IconImage = normalIcon,
                                        SmallIconImage = smallIcon,
                                        Title = cityName,
                                        Count = temp,
                                        WideContent1 = string.Format("Currently: " + weatherCurrent.currentConditions + ", " + origTemp + " degrees"),
                                        WideContent2 = string.Format("Today: " + weatherCurrent.todayShort + " " + todayHigh + "/" + todayLow),
                                        WideContent3 = string.Format("Tomorrow: " + weatherCurrent.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                    };
                                    tile.Update(TileData);

                                    //mark the tile as finished updating
                                    markUpdated("current location", weatherCurrent.city, weatherCurrent.state, true);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (!error)
                {
                    errorText = weatherCurrent.error;
                    error = true;

                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.ToString() != "/")
                        {
                            //get name and location from tile url
                            string tileLoc = tile.NavigationUri.ToString().Split('&')[0].Split('=')[1];
                            bool tileIsCurrent = Convert.ToBoolean(tile.NavigationUri.ToString().Split('&')[2].Split('=')[1]);

                            foreach (Pins pin in pinnedList)
                            {
                                if (tileIsCurrent && pin.currentLoc && !pin.LocName.Contains("default location"))
                                {
                                    IconicTileData TileData = new IconicTileData
                                    {
                                        IconImage = normalIcon,
                                        SmallIconImage = smallIcon,
                                        Title = "Error",
                                        Count = 0,
                                        WideContent1 = errorText,
                                        WideContent2 = "Try checking location services",
                                        WideContent3 = ""
                                    };
                                    tile.Update(TileData);

                                    //mark tile as updated
                                    markUpdated("default location", "null", "null", false);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (error)
                {
                    foreach (ShellTile tile in ShellTile.ActiveTiles)
                    {
                        if (tile.NavigationUri.ToString() != "/")
                        {
                            //get name and location from tile url
                            string tileLoc = tile.NavigationUri.ToString().Split('&')[0].Split('=')[1];
                            bool tileIsCurrent = Convert.ToBoolean(tile.NavigationUri.ToString().Split('&')[2].Split('=')[1]);

                            foreach (Pins pin in pinnedList)
                            {
                                if (tileIsCurrent && pin.currentLoc && !pin.LocName.Contains("default location"))
                                {
                                    IconicTileData TileData = new IconicTileData
                                    {
                                        IconImage = normalIcon,
                                        SmallIconImage = smallIcon,
                                        Title = "Error",
                                        Count = 0,
                                        WideContent1 = errorText,
                                        WideContent2 = "Try checking location services",
                                        WideContent3 = ""
                                    };
                                    tile.Update(TileData);

                                    //mark tile as updated
                                    markUpdated("default location", "null", "null", false);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                if (finished())
                {
                    progTile.IsVisible = false;
                    return;
                }
            }
            else
            {
                progTile.IsVisible = false;
                return;
            }
        }

        //get weather icon Uri for tiles
        private Uri[] getWeatherIcons(string weather)
        {
            Uri normalIcon;
            Uri smallIcon;
            string weatherLower = weather.ToLower();

            if (weatherLower.Contains("thunder") || weatherLower.Contains("storm"))
            {
                normalIcon = new Uri("/TileImages/Medium/Thunder202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Thunder110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("overcast"))
            {
                normalIcon = new Uri("/TileImages/Medium/Cloudy202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Cloudy110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("shower") || weatherLower.Contains("drizzle") || weatherLower.Contains("light rain"))
            {
                normalIcon = new Uri("/TileImages/Medium/Drizzle202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Drizzle110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("flurry") || weatherLower.Contains("snow shower"))
            {
                normalIcon = new Uri("/TileImages/Medium/Flurry202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Flurry110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("fog") || weatherLower.Contains("mist") || weatherLower.Contains("haz"))
            {
                normalIcon = new Uri("/TileImages/Medium/Fog202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Fog110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("freezing"))
            {
                normalIcon = new Uri("/TileImages/Medium/FreezingRain202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/FreezingRain110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("cloudy") || weatherLower.Contains("partly") || weatherLower.Contains("mostly") || weatherLower.Contains("clouds"))
            {
                normalIcon = new Uri("/TileImages/Medium/PartlyCloudy202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/PartlyCloudy110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("rain"))
            {
                normalIcon = new Uri("/TileImages/Medium/Rain202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Rain110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("sleet") || weatherLower.Contains("pellet"))
            {
                normalIcon = new Uri("/TileImages/Medium/Sleet202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Sleet110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("snow") || weatherLower.Contains("blizzard"))
            {
                normalIcon = new Uri("/TileImages/Medium/Snow202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Snow110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("sun") || weatherLower.Contains("sunny") || weatherLower.Contains("clear"))
            {
                normalIcon = new Uri("/TileImages/Medium/Sun202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Sun110.png", UriKind.Relative);
            }
            else if (weatherLower.Contains("wind"))
            {
                normalIcon = new Uri("/TileImages/Medium/Wind202.png", UriKind.Relative);
                smallIcon = new Uri("/TileImages/Small/Wind110.png", UriKind.Relative);
            }
            else
            {
                normalIcon = new Uri("SunCloud202.png", UriKind.Relative);
                smallIcon = new Uri("SunCloud110.png", UriKind.Relative);
            }

            Uri[] icons = { normalIcon, smallIcon };
            return icons;
        }

        //check unit settings
        private void checkUnits()
        {
            //check what the temp units should be
            if (store.Contains("lockUnitIsC"))
            {
                if ((bool)store["lockUnitIsC"])
                {
                    tempUnitIsC = true;
                }
                else
                {
                    tempUnitIsC = false;
                }
            }
            else
            {
                store["lockUnitIsC"] = true;
                tempUnitIsC = true;
            }
        }

        //check location settings, return location
        private void checkLocation()
        {
            //Check to see if allowed to get location
            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {
                    if (locationSearchTimes <= 5)
                    {
                        //get location
                        var getLocation = new getLocation();
                        if (getLocation.getLat() != null && getLocation.getLat() != "NA")
                        {
                            latitude = getLocation.getLat();
                            longitude = getLocation.getLong();
                            String[] loc = { latitude, longitude };
                            store["loc"] = loc;
                        }
                        else
                        {
                            locationSearchTimes++;
                            checkLocation();
                        }
                    }
                    if (locationSearchTimes > 5)
                    {
                        if (store.Contains("loc"))
                        {
                            String[] latlng = new String[2];
                            latlng = (String[])store["loc"];
                            latitude = latlng[0];
                            longitude = latlng[1];

                            //stop reuse of the location too many times
                            store.Remove("loc");
                        }
                        else
                        {
                            error = true;
                            errorText = "Cannot get location";
                        }
                    }
                }
                else
                {
                    error = true;
                    errorText = "Cannot get location";
                }
            }
        }

        //return a url for weather data depending on whether the tile is location aware or not
        private string getUrl(bool isCurrent)
        {
            if (isCurrent)
            {
                return "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast/q/" + latitude + "," + longitude + ".xml";
            }
            else
            {
                return "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + locUrl + ".xml";
            }
        }

        //mark a tile as updated
        private void markUpdated(string cityName, string city, string state, bool isCurrent)
        {
            foreach (Pins pin in pinnedListCopy)
            {
                if ((pin.LocName == cityName) || (pin.LocName.Split(',')[0].Contains(city) && pin.LocName.Split(',')[1].Contains(state)) || (pin.currentLoc == isCurrent && isCurrent))
                {
                    pin.updated = true;
                    break;
                }
            }
        }

        //check if all tiles have been updated
        private bool finished()
        {
            foreach (Pins pin in pinnedListCopy)
            {
                if (pin.updated == false)
                {
                    return false;
                }
            }
            progTile.IsVisible = false;
            return true;
        }
    }
}