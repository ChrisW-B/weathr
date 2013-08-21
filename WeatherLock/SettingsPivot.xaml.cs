#define DEBUG_AGENT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Scheduler;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Tasks;
using System.Windows.Media;

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


            setValues();

            ignoreCheckBoxEvents = true;

            periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            if (periodicTask != null)
            {
                PeriodicStackPanel.DataContext = periodicTask;
            }
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
            if (store.Contains("lockUnit"))
            {
                if (store["lockUnit"] == "c")
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
                store["lockUnit"] = "c";
                lockCelsius.IsChecked = true;
            }

            if (store.Contains("forecastUnit"))
            {
                if (store["forecastUnit"] == "i")
                {
                    imperial.IsChecked = true;
                }
                else
                {
                    metric.IsChecked = true;
                }
            }
            else
            {
                store["forecastUnit"] = "m";
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
                store["tempIsC"] = "true";
                celsius.IsChecked = true;
            }

            if (store.Contains("windUnit"))
            {
                if ((string)store["windUnit"] == "m")
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
                store["windUnit"] = "k";
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
            store["tempIsC"] = true;
            store["unitChanged"] = true;
        }
        private void fahrenheit_Checked(object sender, RoutedEventArgs e)
        {
            store["tempIsC"] = false;
            store["unitChanged"] = true;
        }
        private void unitKms_Checked(object sender, RoutedEventArgs e)
        {
            store["windUnit"] = "k";
            store["unitChanged"] = true;
        }
        private void unitMiles_Checked(object sender, RoutedEventArgs e)
        {
            store["windUnit"] = "m";
            store["unitChanged"] = true;
        }
        private void flickrPics_Checked(object sender, RoutedEventArgs e)
        {
            store["useFlickr"] = true;
            weatherGroup.IsEnabled = true;
        }
        private void flickrPics_Unchecked(object sender, RoutedEventArgs e)
        {
            store["useFlickr"] = false;
            weatherGroup.IsEnabled = false;
        }
        private void weatherGroup_Checked(object sender, RoutedEventArgs e)
        {
            store["useWeatherGroup"] = true;
            store["groupChanged"] = true;
        }
        private void weatherGroup_Unchecked(object sender, RoutedEventArgs e)
        {
            store["useWeatherGroup"] = false;
            store["groupChanged"] = true;
        }

        //Location Pivot
        #region variables
        ObservableCollection<Locations> locations = new ObservableCollection<Locations>();
        
        #endregion
        private void enableLocation_Checked(object sender, RoutedEventArgs e)
        {
            store["enableLocation"] = true;
        }
        private void enableLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            store["enableLocation"] = false;
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
                    if(!locationName.Contains("Current Location")){
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
            store["forecastUnit"] = "m";
            store["unitChanged"] = true;
            metric.IsChecked = true;
        }
        private void imperial_Checked(object sender, RoutedEventArgs e)
        {
            store["forecastUnit"] = "i";
            store["unitChanged"] = true;
            imperial.IsChecked = true;
        }

        //Tile and Lock pivot
        private void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            startTileProg();
            updateData();
        }
        private void notifyMe_Checked(object sender, RoutedEventArgs e)
        {
            store["notifyMe"] = true;
            notifyMe.IsChecked = true;
        }
        private void notifyMe_Unchecked(object sender, RoutedEventArgs e)
        {
            store["notifyMe"] = false;
            notifyMe.IsChecked = false;

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
        private void HiLo_Checked(object sender, RoutedEventArgs e)
        {
            store["tempAlert"] = true;
        }
        private void HiLo_Unchecked(object sender, RoutedEventArgs e)
        {
            store["tempAlert"] = false;
        }
        private void StartPeriodicAgent()
        {
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
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                    agentsAreEnabled = false;
                    PeriodicCheckBox.IsChecked = false;
                    UpdateBox.IsEnabled = false;
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
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
            StartPeriodicAgent();
            if (!isTrial)
            {
                UpdateBox.IsEnabled = true;
            }
        }
        private void PeriodicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckBoxEvents)
                return;
            RemoveAgent(periodicTaskName);
            UpdateBox.IsEnabled = false;
        }
        private void lockCelsius_Checked(object sender, RoutedEventArgs e)
        {
            store["lockUnit"] = "c";
            lockCelsius.IsChecked = true;
        }
        private void lockFahrenheit_Checked(object sender, RoutedEventArgs e)
        {
            store["lockUnit"] = "f";
            lockFahrenheit.IsChecked = true;
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

        //Update the tile
        #region variables
        //Current Conditions
        private String cityName;
        private String tempStr;
        private String weather;

        //Forecast Conditions
        private String todayHigh;
        private String todayLow;
        private String forecastToday;
        private String forecastTomorrow;
        private String tomorrowHigh;
        private String tomorrowLow;

        private String tempUnit;
        private String url;

        //Release Key
        private String apiKey = "102b8ec7fbd47a05";



        private String latitude;
        private String longitude;
        #endregion
        private void updateData()
        {
            //Testing Key
            apiKey = "fb1dd3f4321d048d";

            checkLocation();
            checkUnits();
            getWeatherData();
        }
        private void getWeatherData()
        {
            checkLocation();
            var client = new WebClient();
            Uri uri = new Uri(url);

            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(WeatherStringCallback);
            client.DownloadStringAsync(uri);
        }
        private void WeatherStringCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            restoreWeather();
            if (!e.Cancelled && e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);

                //Current Conditions
                var currentObservation = doc.Element("response").Element("current_observation");
                this.cityName = (string)currentObservation.Element("display_location").Element("full");
                this.weather = (string)currentObservation.Element("weather");

                XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                var today = forecastDays.Element("forecastday");
                var tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                this.forecastToday = (string)today.Element("conditions");
                this.forecastTomorrow = (string)tomorrow.Element("conditions");
                if (tempUnit == "c")
                {
                    tempStr = (string)currentObservation.Element("temp_c");
                    this.todayLow = (string)today.Element("low").Element("celsius");
                    this.todayHigh = (string)today.Element("high").Element("celsius");
                    this.tomorrowLow = (string)tomorrow.Element("low").Element("celsius");
                    this.tomorrowHigh = (string)tomorrow.Element("high").Element("celsius");
                }
                else
                {
                    tempStr = (string)currentObservation.Element("temp_f");
                    this.todayLow = (string)today.Element("low").Element("fahrenheit");
                    this.todayHigh = (string)today.Element("high").Element("fahrenheit");
                    this.tomorrowHigh = (string)tomorrow.Element("high").Element("fahrenheit");
                    this.tomorrowLow = (string)tomorrow.Element("low").Element("fahrenheit");
                }

                //convert temps to ints
                var getTemp = new convertTempMain(tempStr);
                int temp = getTemp.temp;

                //update the tile and lockscreen
                var updateTile = new updateTileMain(cityName, temp, weather, todayHigh, todayLow, forecastToday, forecastTomorrow, tomorrowHigh, tomorrowLow);

                backupWeather();
                //save the time of the last time the app was run
                store["lastRun"] = DateTime.Now.ToString();
                store["locName"] = cityName;
                store.Save();
            }
            progTile.IsVisible = false;
        }
        private void checkUnits()
        {
            //check what the temp units should be
            if (store.Contains("lockUnit"))
            {
                if (store["lockUnit"] == "c")
                {
                    tempUnit = "c";
                }
                else
                {
                    tempUnit = "f";
                }
            }
            else
            {
                store["lockUnit"] = "c";
                tempUnit = "c";
            }
        }
        private void checkLocation()
        {
            //Check to see if allowed to get location
            if (store.Contains("defaultCurrent"))
            {
                if (Convert.ToBoolean(store["defaultCurrent"]))
                {
                    //get location
                    var getLocation = new getLocationMain();
                    if (getLocation.getLat() != null)
                    {
                        latitude = getLocation.getLat();
                        longitude = getLocation.getLong();
                        String[] loc = { latitude, longitude };
                        store["loc"] = loc;
                    }
                    else
                    {
                        if (store.Contains("loc"))
                        {
                            String[] latlng = new String[2];
                            latlng = (String[])store["loc"];
                            latitude = latlng[0];
                            longitude = latlng[1];
                        }
                        else
                        {
                            latitude = "0";
                            longitude = "0";
                        }
                    }
                    url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast/q/" + latitude + "," + longitude + ".xml";
                }
                else
                {
                    if (store.Contains("locUrl"))
                    {
                        url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + store["locUrl"] + ".xml";
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        public void backupWeather()
        {
            string[] backup = {
                 cityName,
                 tempStr,
                 weather,
                 todayHigh,
                 todayLow,
                 forecastToday,
                 forecastTomorrow,
                 tomorrowHigh,
                 tomorrowLow
                 };
            store["backupTile"] = backup;
            store.Save();
        }
        private void restoreWeather()
        {
            if (store.Contains("backupTile"))
            {
                String[] backupTile = new String[9];
                backupTile = store["backupTile"];

                this.cityName = backupTile[0];
                this.tempStr = backupTile[1];
                this.weather = backupTile[2];
                this.todayHigh = backupTile[3];
                this.todayLow = backupTile[4];
                this.forecastToday = backupTile[5];
                this.forecastTomorrow = backupTile[6];
                this.tomorrowHigh = backupTile[7];
                this.tomorrowLow = backupTile[8];
            }
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