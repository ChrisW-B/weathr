using Microsoft.Phone.Controls;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Phone.Maps;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Microsoft.Phone.Tasks;
using Helpers;
using WeatherData;
using System.Globalization;

namespace WeatherLock
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region variables
        //Collection of weather data
        private WeatherInfo weather;
        private string weatherConditions;
        private String cityName;

        //Set Units
        bool tempUnitIsC;
        bool windUnitIsM;
        bool forecastUnitIsM;

        //location data
        String latitude = null;
        String longitude = null;

        //flags 
        bool mapsSet;
        bool alertSet;
        bool getBackground;
        bool weatherSet = false;
        bool errorSet = false;
        int radTries;
        int satTries;
        int locationSearchTimes;
        int numFlickrAttempts;
        int timesWeatherBroke;

        //Wunderground Api
        String apiKey = "102b8ec7fbd47a05";
        String urlKey = null;
        bool isCurrent;
        String cityNameLoad;
        String url = null;

        //Flickr Api
        private String flickrTags;
        String fApiKey = "2781c025a4064160fc77a52739b552ff";
        bool useWeatherGroup;
        String weatherGroup = "1463451@N25";
        String fUrl = null;

        //collections of alerts
        ObservableCollection<HazardResults> results = new ObservableCollection<HazardResults>();

        //List of photo data
        List<FlickrImage> photoList = new List<FlickrImage>();

        //create a clock
        private Clock clock;

        //Progress Indicators and flags
        private bool progIndicatorsCreated = false;
        ProgressIndicator progWeather;
        ProgressIndicator progFlickr;
        ProgressIndicator progAlerts;

        //Save typing every time
        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        //Check to see if app is running as trial
        LicenseInformation licInfo;
        public bool isTrial;

        //background stuff
        PeriodicTask periodicTask;

        string periodicTaskName = "PeriodicAgent";
        public bool agentsAreEnabled = true;
        #endregion

        //Initialize the main page
        public MainPage()
        {
            InitializeComponent();

            //Testing Key
            apiKey = "fb1dd3f4321d048d";
            ApplicationBar.StateChanged += ApplicationBar_StateChanged;
            initializeProgIndicators();
            setUnits();
            restoreWeather();
            this.clock = new Clock(this);
        }

        //show system tray on app bar swipe
        private void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            SystemTray.Opacity = .5;

            if (!e.IsMenuVisible)
            {
                HideTray();
            }
            else
            {
                SystemTray.IsVisible = e.IsMenuVisible;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            licInfo = new LicenseInformation();
            isTrial = licInfo.IsTrial();

            satTries = 0;
            radTries = 0;
            locationSearchTimes = 0;
            mapsSet = false;
            getBackground = false;
            weatherSet = false;

            checkUseWeatherGroup();

            if (!progIndicatorsCreated)
            {
                initializeProgIndicators();
            }

            base.OnNavigatedTo(e);
            if (this.NavigationContext.QueryString.ContainsKey("cityName") && this.NavigationContext.QueryString.ContainsKey("url") && this.NavigationContext.QueryString.ContainsKey("isCurrent") && this.NavigationContext.QueryString.ContainsKey("lat") && this.NavigationContext.QueryString.ContainsKey("lon"))
            {

                cityNameLoad = this.NavigationContext.QueryString["cityName"];
                urlKey = this.NavigationContext.QueryString["url"];
                isCurrent = Convert.ToBoolean(this.NavigationContext.QueryString["isCurrent"]);
                latitude = this.NavigationContext.QueryString["lat"];
                longitude = this.NavigationContext.QueryString["lon"];
                String[] loc = { latitude, longitude };
                store["loc"] = loc;

                if (store.Contains("cityName") && !isCurrent)
                {
                    string stateLoadSplit;
                    string cityLoadSplit;
                    string citySplit;
                    string stateSplit;

                    string cityStore = (string)store["cityName"];

                    if (cityStore.Contains(','))
                    {
                    citySplit = cityStore.Split(',')[0];
                    stateSplit = cityStore.Split(',')[1];
                    }
                    else
                    {
                        citySplit = cityStore;
                        stateSplit = "";
                    }

                    if (cityNameLoad.Contains(',')) {
                        cityLoadSplit = cityNameLoad.Split(',')[0];
                        stateLoadSplit = cityNameLoad.Split(',')[1];
                    }
                    else
                    {
                        cityLoadSplit = cityNameLoad;
                        stateLoadSplit = "";
                    }

                    if (!cityLoadSplit.Contains(citySplit))
                    {
                        if (!stateLoadSplit.Contains(stateSplit))
                        {
                            store["locChanged"] = true;
                        }
                    }
                }
                else if (isCurrent)
                {
                    store["locChanged"] = true;
                    cityNameLoad = "Current Location";
                }
                store.Save();
            }
            else
            {
                noParams();
            }
            title.Title = cityNameLoad;

            checkPinned();
            checkUpdated();
            showAd();
        }

        private void checkPinned()
        {
            ApplicationBarIconButton pinButton = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.ToString().Contains(cityNameLoad) && !tile.NavigationUri.ToString().Contains("isCurrent=True"))
                {
                    pinButton.IsEnabled = false;
                    break;
                }
                else if (tile.NavigationUri.ToString().Contains("isCurrent=True") && isCurrent)
                {
                    pinButton.IsEnabled = false;
                    break;
                }
                else
                {
                    pinButton.IsEnabled = true;
                }

            }
        }

        private void checkUseWeatherGroup()
        {
            if (store.Contains("useWeatherGroup"))
            {
                if ((bool)store["useWeatherGroup"])
                {
                    useWeatherGroup = true;
                }
                else
                {
                    useWeatherGroup = false;
                }
            }
            else
            {
                store["useWeatherGroup"] = true;
                useWeatherGroup = true;
            }
        }


        //Set up location info
        private void noParams()
        {
            if (store.Contains("defaultLocation") && store.Contains("defaultUrl") && store.Contains("defaultCurrent"))
            {
                if (store.Contains("cityName"))
                {
                    if (store["cityName"] != cityNameLoad)
                    {
                        if (store["cityName"] != store["defaultLocation"])
                        {
                            store["locChanged"] = true;
                        }
                    }
                }
                else
                {
                    noDefaults();
                }
                cityNameLoad = (string)store["defaultLocation"];
                urlKey = (string)store["defaultUrl"];
                isCurrent = Convert.ToBoolean(store["defaultCurrent"]);
            }
            else
            {
                noDefaults();
            }
        }
        private void noDefaults()
        {
            store["defaultLocation"] = "Current Location";
            store["defaultUrl"] = "null";
            store["defaultCurrent"] = true;
            cityNameLoad = "Current Location";
            urlKey = "null";
            isCurrent = true;
        }

        //Check the units to use
        private void setUnits()
        {
            if (store.Contains("tempIsC"))
            {
                if ((bool)store["tempIsC"])
                {
                    this.tempUnitIsC = true;
                }
                else
                {
                    this.tempUnitIsC = false;
                }
            }
            else
            {
                this.tempUnitIsC = true;
            }

            if (store.Contains("windUnitIsM"))
            {
                if ((bool)store["windUnitIsM"])
                {
                    this.windUnitIsM = true;
                }
                else
                {
                    this.windUnitIsM = false;
                }
            }
            else
            {
                this.windUnitIsM = false;
            }

            if (store.Contains("forecastUnitIsM"))
            {
                if ((bool)store["forecastUnitIsM"])
                {
                    this.forecastUnitIsM = true;
                }
                else
                {
                    this.forecastUnitIsM = false;
                }
            }
            else
            {
                forecastUnitIsM = true;
            }
        }

        //Set the URLs
        private void setURL()
        {
            if (isCurrent)
            {
                if (store.Contains("locChanged"))
                {
                    if ((bool)store["locChanged"])
                    {
                        findLocation();
                    }
                }
                if (latitude != null && longitude != null)
                {
                    url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast/q/" + latitude + "," + longitude + ".xml";
                    if (useWeatherGroup)
                    {
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&group_id=" + weatherGroup + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weatherConditions + "&per_page=500&tag_mode=any&content_type=1&media=photos&radius=32&format=rest";

                    }
                    else
                    {
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weatherConditions + "&per_page=500&tag_mode=any&content_type=1&media=photos&radius=32&format=rest";
                    }
                }
                else
                {
                    findLocation();
                    setURL();
                }
            }
            else
            {
                if ((bool)store["locChanged"])
                {
                    findLocation();
                }
                if (latitude != null && longitude != null)
                {
                    if (useWeatherGroup)
                    {
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&group_id=" + weatherGroup + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather.currentConditions + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&has_geo=&format=rest";
                    }
                    else
                    {
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather.currentConditions + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&has_geo=&format=rest";
                    }
                }
                else
                {
                    findLocation();
                    setURL();
                }


                url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + urlKey + ".xml";



            }
        }

        //Find the location
        private void findLocation()
        {

            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {

                    if (isCurrent && locationSearchTimes <= 5)
                    {
                        //get location
                        var getLocation = new getLocation();
                        if (getLocation.getLat() != null && getLocation.getLat() != "NA")
                        {
                            errorSet = false;
                            //Set long and lat
                            latitude = getLocation.getLat();
                            longitude = getLocation.getLong();

                            if (latitude.Contains(","))
                            {
                                latitude = latitude.Replace(",", ".");
                            }
                            if (longitude.Contains(","))
                            {
                                longitude = longitude.Replace(",", ".");
                            }

                            //Save
                            String[] loc = { latitude, longitude };
                            store["loc"] = loc;
                            store.Save();
                        }
                        else
                        {
                            locationSearchTimes++;
                            findLocation();
                        }
                    }
                    if (locationSearchTimes > 5)
                    {
                        if (store.Contains("loc"))
                        {
                            String[] loc = (string[])store["loc"];
                            latitude = loc[0];
                            longitude = loc[1];

                            if (latitude.Contains(","))
                            {
                                latitude = latitude.Replace(",", ".");
                            }
                            if (longitude.Contains(","))
                            {
                                longitude = longitude.Replace(",", ".");
                            }

                            //prevent reuse of same location
                            store.Remove("loc");
                        }
                        else if (!errorSet)
                        {
                            errorSet = true;
                            errorText.Text = "Cannot get current location. Check to make sure location services are enabled";
                            clearWeather();
                        }
                        else
                        {
                            latitude = "0";
                            longitude = "0";
                        }
                    }
                }
                else
                {
                    errorSet = true;
                    errorText.Text = "Cannot get current location. Check to make sure location services are enabled";
                    clearWeather();
                }
            }
            else
            {
                store["enableLocation"] = true;
                findLocation();
            }
        }

        //Check whether everything should be updated
        private void checkUpdated()
        {
            setURL();
            setUnits();
            initializeStores();

            if (store.Contains("lastUpdated"))
            {
                var appLastRun = Convert.ToDateTime(store["lastUpdated"]);
                var now = DateTime.Now;
                TimeSpan timeDiff = now.Subtract(appLastRun);
                if ((int)timeDiff.TotalMinutes > 15)
                {
                    getBackground = true;
                    clearWeather();
                    updateWeather();
                    store["lastUpdated"] = DateTime.Now;
                }
                else if ((bool)store["locChanged"])
                {
                    clearWeather();
                    store["locChanged"] = false;
                    getBackground = true;
                    updateWeather();
                    store["lastUpdated"] = DateTime.Now;
                }
                else if ((bool)store["unitChanged"])
                {
                    store["unitChanged"] = false;
                    restoreWeather();
                }
                else if ((bool)store["groupChanged"])
                {
                    store["groupChanged"] = false;
                    getFlickrPic();
                }
                else
                {
                    getFlickrPic();
                }
            }
            else
            {
                getBackground = true;
                updateWeather();
                store["lastUpdated"] = DateTime.Now;
            }
        }
        private void initializeStores()
        {
            if (!store.Contains("locChanged"))
            {
                store["locChanged"] = false;
            }
            if (!store.Contains("unitChanged"))
            {
                store["unitChanged"] = false;
            }
            if (!store.Contains("groupChanged"))
            {
                store["groupChanged"] = false;
            }
        }

        //Show ad if nessecary
        private void showAd()
        {

            if (isTrial)
            {
                adControl.Visibility = System.Windows.Visibility.Visible;
                forecastListBox.Margin = new Thickness(0, 0, 0, 110);
            }
            else
            {
                adControl.Visibility = System.Windows.Visibility.Collapsed;
            }

        }

        //enable periodic task
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

            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background tasks have been disabled for Weathr. Tile and Lockscreen will not update");
                    agentsAreEnabled = false;
                }

                if (exception.Message.Contains("You have too many background tasks. Remove some and try again"))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
            }
            catch (SchedulerServiceException)
            {
            }
        }
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

        //Progress Bars
        private void initializeProgIndicators()
        {
            progIndicatorsCreated = true;
            progWeather = new ProgressIndicator();
            progWeather.Text = "Updating Weather";
            progWeather.IsIndeterminate = true;
            progWeather.IsVisible = false;

            progAlerts = new ProgressIndicator();
            progAlerts.Text = "Updating Alerts";
            progAlerts.IsIndeterminate = true;
            progAlerts.IsVisible = false;

            progFlickr = new ProgressIndicator();
            progFlickr.Text = "Updating Image";
            progFlickr.IsIndeterminate = true;
            progFlickr.IsVisible = false;
        }
        private void startWeatherProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0.5);
            progWeather.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progWeather);
        }
        private void startFlickrProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0.5);
            progFlickr.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progFlickr);
        }
        private void startAlertProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0.5);
            progAlerts.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progAlerts);
        }
        private void HideTray()
        {
            if (progAlerts.IsVisible == false && progFlickr.IsVisible == false && progWeather.IsVisible == false)
            {
                SystemTray.SetIsVisible(this, false);
            }
        }



        //Getting and Setting Weather data
        private void setWeather()
        {
            #region variables
            string windCon;
            #endregion

            //Convert weather text to caps
            string weatherUpper = weather.currentConditions.ToUpper();
            weatherConditions = weather.currentConditions;

            //set name
            if (weatherSet)
            {
                title.Title = cityName;
                weatherSet = false;
            }

            //set temp
            if (tempUnitIsC)
            {
                temp.Text = weather.tempC + "°";
                feelsLike.Text = "Feels like: " + weather.feelsLikeC + "°";
            }
            else
            {
                temp.Text = weather.tempF + "°";
                feelsLike.Text = "Feels like: " + weather.feelsLikeF + "°";
            }

            //set wind
            if (windUnitIsM)
            {
                windCon = "Wind: " + weather.windSpeedM;
            }
            else
            {
                windCon = "Wind: " + weather.windSpeedK;
            }
            windCon += " " + weather.windDir;
            wind.Text = windCon;

            //Set current conditons
            conditions.Text = weatherUpper;

            //Set humidity
            humidity.Text = "Humidity: " + weather.humidity;

            //Set compare text
            if (tempUnitIsC)
            {
                tempCompare.Text = "TOMORROW WILL BE " + weather.tempCompareC + " TODAY";
            }
            else
            {
                tempCompare.Text = "TOMORROW WILL BE " + weather.tempCompareF + " TODAY";
            }

            //set forecasts
            forecastListBox.ItemsSource = null;
            if (forecastUnitIsM)
            {
                forecastListBox.ItemsSource = weather.forecastC;
            }
            else
            {
                forecastListBox.ItemsSource = weather.forecastF;
            }

            //set alerts box
            alertListBox.ItemsSource = results;

            store["cityName"] = cityName;
            //Set errors (if there are any)
            if (!errorSet)
            {
                errorText.Text = null;
            }

            //backup weather
            backupWeather();
            progWeather.IsVisible = false;
            HideTray();

            //set background if necessary
            if (getBackground)
            {
                getBackground = false;
                getFlickrPic();
            }
        }
        private void clearWeather()
        {
            //Clear old location data
            temp.Text = null;
            feelsLike.Text = null;
            wind.Text = null;
            conditions.Text = null;
            humidity.Text = null;
            tempCompare.Text = null;
            forecastListBox.ItemsSource = null;
            alertListBox.ItemsSource = null;
        }
        private void restoreWeather()
        {
            if (store.Contains("backupAlerts"))
            {
                results = (ObservableCollection<HazardResults>)store["backupAlerts"];
            }

            if (store.Contains("loc"))
            {
                String[] latlng = new String[2];
                latlng = (String[])store["loc"];
                this.latitude = latlng[0];
                this.longitude = latlng[1];
            }

            if ((bool)store.Contains("backupApp"))
            {
                weather = store["backupApp"];
                cityName = weather.city + ", " + weather.state;

                if (this.weather == null)
                {
                    updateWeather();
                }
                else if (!errorSet)
                {
                    setWeather();
                }
            }
        }
        private void updateWeather()
        {

            startWeatherProg();
            setURL();

            if (!errorSet)
            {
                clearWeather();
                WebClient client = new WebClient();
                client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
                client.DownloadStringAsync(new Uri(url));
            }
            else if (errorSet)
            {
                progWeather.IsVisible = false;
                HideTray();
            }
        }
        private void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && e.Result != null)
            {
                weather = new WeatherInfo();
                XDocument doc = XDocument.Parse(e.Result);
                XElement error = doc.Element("response").Element("error");
                if (error == null)
                {

                    #region current conditions
                    //Current Conditions
                    var currentObservation = doc.Element("response").Element("current_observation");

                    //location name
                    weather.city = (string)currentObservation.Element("display_location").Element("city");
                    weather.state = (string)currentObservation.Element("display_location").Element("state_name");
                    weather.shortCityName = (string)currentObservation.Element("display_location").Element("city");

                    cityName = weather.city + ", " + weather.state;

                    if (store["defaultLocation"] == "Current Location")
                    {
                        store["saveDefaultLocName"] = cityName;
                        store.Save();
                    }

                    //Current Weather
                    weather.currentConditions = (string)currentObservation.Element("weather");

                    //Current wind
                    weather.windSpeedM = (string)currentObservation.Element("wind_mph") + " mph";
                    weather.windSpeedK = (string)currentObservation.Element("wind_kph") + " kph";
                    weather.windDir = (string)currentObservation.Element("wind_dir");
                    //Current Temp and feels like
                    weather.tempC = (string)currentObservation.Element("temp_c");
                    weather.feelsLikeC = (string)currentObservation.Element("feelslike_c");
                    weather.tempF = (string)currentObservation.Element("temp_f");
                    weather.feelsLikeF = (string)currentObservation.Element("feelslike_f");

                    //current humidity
                    weather.humidity = (string)currentObservation.Element("relative_humidity");
                    #endregion

                    #region forecast conditions
                    //Forecast Conditions
                    XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                    //Today's conditions
                    XElement today = forecastDays.Element("forecastday");

                    //Today's High/Low
                    weather.todayLowC = (string)today.Element("low").Element("celsius");
                    weather.todayHighC = (string)today.Element("high").Element("celsius");
                    weather.todayLowF = (string)today.Element("low").Element("fahrenheit");
                    weather.todayHighF = (string)today.Element("high").Element("fahrenheit");

                    //Tomorrow's conditions
                    XElement tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                    //Tomorrow's High/Low
                    weather.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");
                    weather.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                    weather.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                    weather.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");

                    //convert to ints
                    weather.todayHighIntC = Convert.ToInt32(weather.todayHighC);
                    weather.tomorrowHighIntC = Convert.ToInt32(weather.tomorrowHighC);
                    weather.todayHighIntF = Convert.ToInt32(weather.todayHighF);
                    weather.tomorrowHighIntF = Convert.ToInt32(weather.tomorrowHighF);

                    if (weather.todayHighIntC + 10 < weather.tomorrowHighIntC)
                    {
                        weather.tempCompareC = "MUCH WARMER THAN";
                    }
                    else if (weather.todayHighIntC + 3 < weather.tomorrowHighIntC)
                    {
                        weather.tempCompareC = "WARMER THAN";
                    }                    
                    else if (weather.todayHighIntC - 10 > weather.tomorrowHighIntC)
                    {
                        weather.tempCompareC = "MUCH COOLER THAN";
                    }
                    else if (weather.todayHighIntC - 3 > weather.tomorrowHighIntC)
                    {
                        weather.tempCompareC = "COOLER THAN";
                    }
                    else
                    {
                        weather.tempCompareC = "ABOUT THE SAME AS";
                    }

                    if (weather.todayHighIntF + 20 < weather.tomorrowHighIntF)
                    {
                        weather.tempCompareF = "MUCH WARMER THAN";
                    }
                    else if (weather.todayHighIntF + 5 < weather.tomorrowHighIntF)
                    {
                        weather.tempCompareF = "WARMER THAN";
                    }
                    else if (weather.todayHighIntF - 20 > weather.tomorrowHighIntF)
                    {
                        weather.tempCompareF = "MUCH COOLER THAN";
                    }
                    else if (weather.todayHighIntF - 5 > weather.tomorrowHighIntF)
                    {
                        weather.tempCompareF = "COOLER THAN";
                    }
                    else
                    {
                        weather.tempCompareF = "ABOUT THE SAME AS";
                    }

                    var forecastDaysTxt = doc.Element("response").Element("forecast").Element("txt_forecast").Element("forecastdays");

                    //clear out forecast list first

                    weather.forecastC = new ObservableCollection<ForecastC>();
                    weather.forecastF = new ObservableCollection<ForecastF>();

                    weather.forecastC.Clear();
                    weather.forecastF.Clear();

                    foreach (XElement elm in forecastDaysTxt.Elements("forecastday"))
                    {
                        ForecastC forecastC = new WeatherData.ForecastC();
                        ForecastF forecastF = new WeatherData.ForecastF();



                        forecastC.title = forecastF.title = (string)elm.Element("title");
                        forecastC.text = (string)elm.Element("fcttext_metric");
                        forecastF.text = (string)elm.Element("fcttext");
                        forecastC.pop = forecastF.pop = (string)elm.Element("pop");


                        weather.forecastF.Add(forecastF);
                        weather.forecastC.Add(forecastC);
                        if (forecastUnitIsM)
                        {
                            forecastListBox.ItemsSource = weather.forecastC;
                        }
                        else
                        {
                            forecastListBox.ItemsSource = weather.forecastF;
                        }
                    }
                    #endregion

                    #region tile stuff
                    weather.todayShort = (string)today.Element("conditions");
                    weather.tomorrowShort = (string)tomorrow.Element("conditions");

                    weather.todayLowC = (string)today.Element("low").Element("celsius");
                    weather.todayHighC = (string)today.Element("high").Element("celsius");
                    weather.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                    weather.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");

                    weather.todayLowF = (string)today.Element("low").Element("fahrenheit");
                    weather.todayHighF = (string)today.Element("high").Element("fahrenheit");
                    weather.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                    weather.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");
                    #endregion


                    //set ui elements
                    weatherSet = true;
                    setWeather();

                    //Back it all up
                    backupWeather();
                    timesWeatherBroke = 0;
                }
                else
                {
                    weather.error = doc.Element("response").Element("error").Element("description").Value;
                    clearWeather();

                    errorSet = true;
                    string errorDescrip = weather.error;
                    if(errorDescrip.Contains("you must supply a location query"))
                    {
                        errorDescrip = "No info for this location, try selecting another";
                    }
                    else if (errorDescrip.Contains("location"))
                    {
                        errorDescrip += Environment.NewLine + "Try checking your location settings.";
                    }
                    errorText.Text = errorDescrip;

                    progWeather.IsVisible = false;
                    HideTray();
                }
            }
            else if (timesWeatherBroke < 5)
            {
                timesWeatherBroke++;
                updateWeather();
            }

        }
        private void backupWeather()
        {
            //Backup location, weather data, and alerts
            store["locationName"] = cityName;
            store["backupApp"] = weather;
            store["backupAlerts"] = results;
        }

        //Radar Map
        private void setupRadar()
        {
            MapsSettings.ApplicationContext.ApplicationId = "6613ed8e-4185-4b0d-b0ba-a530ac174ad5";
            MapsSettings.ApplicationContext.AuthenticationToken = "YWpfouhJ8iOBbeDGnmKULA";
            if (radTries == 0)
            {
                radarMap.Loaded += addRadar;
                radarMap.IsEnabled = false;
            }
            if (latitude != null && longitude != null && latitude != "" && longitude != "" && radTries < 5)
            {
                double lat = Convert.ToDouble(latitude, new CultureInfo("en-US"));
                double lon = Convert.ToDouble(longitude, new CultureInfo("en-US"));
               
                radarMap.Center = new GeoCoordinate(lat, lon);

                radarMap.CartographicMode = MapCartographicMode.Road;
                radarMap.ZoomLevel = 5;

                showRadarLocation(lat, lon);
            }
            else if (radTries >= 5)
            {
                return;
            }
            else
            {
                radTries++;
                findLocation();
                setupRadar();

            }
        }
        void addRadar(object sender, RoutedEventArgs e)
        {
            TileSource radar = new CurrentRadar();
            radarMap.TileSources.Add(radar);
        }
        private void showRadarLocation(double lat, double lon)
        {

            //create a marker

            Polygon triangle = new Polygon();
            triangle.Fill = new SolidColorBrush(Colors.Black);
            triangle.Points.Add((new Point(0, 0)));
            triangle.Points.Add((new Point(0, 80)));
            triangle.Points.Add((new Point(40, 80)));
            triangle.Points.Add((new Point(40, 40)));



            ScaleTransform flip = new ScaleTransform();
            flip.ScaleY = -1;
            triangle.RenderTransform = flip;

            // Create a MapOverlay to contain the marker
            MapOverlay myLocationOverlay = new MapOverlay();

            myLocationOverlay.Content = triangle;
            myLocationOverlay.PositionOrigin = new Point(0, 0);

            myLocationOverlay.GeoCoordinate = new GeoCoordinate(lat, lon);

            // Create a MapLayer to contain the MapOverlay.
            MapLayer myLocationLayer = new MapLayer();
            myLocationLayer.Add(myLocationOverlay);

            // Add the MapLayer to the Map.
            radarMap.Layers.Add(myLocationLayer);
        }
        void radarMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Radar.xaml?isCurrent=" + isCurrent + "&lat=" + latitude + "&lon=" + longitude, UriKind.Relative));
        }
        //Sat Map
        private void setupSat()
        {
            MapsSettings.ApplicationContext.ApplicationId = "6613ed8e-4185-4b0d-b0ba-a530ac174ad5";
            MapsSettings.ApplicationContext.AuthenticationToken = "YWpfouhJ8iOBbeDGnmKULA";
            if (satTries == 0)
            {
                satMap.Loaded += addSat;
                satMap.IsEnabled = false;
            }
            if (latitude != null && longitude != null && latitude != "" && longitude != "" && satTries < 5)
            {
                double lat = Convert.ToDouble(latitude, new CultureInfo("en-US"));
                double lon = Convert.ToDouble(longitude, new CultureInfo("en-US"));
                satMap.Center = new GeoCoordinate(lat, lon);
                satMap.CartographicMode = MapCartographicMode.Road;
                satMap.ZoomLevel = 5;


                showSatLocation(lat, lon);

            }
            else if (satTries >= 5)
            {
                return;
            }
            else
            {
                satTries++;
                findLocation();
                setupSat();
            }
        }
        private void addSat(object sender, RoutedEventArgs e)
        {
            TileSource sat = new CurrentSat();
            satMap.TileSources.Add(sat);
        }
        private void showSatLocation(double lat, double lon)
        {

            //create a marker

            Polygon triangle = new Polygon();
            triangle.Fill = new SolidColorBrush(Colors.Black);
            triangle.Points.Add((new Point(0, 0)));
            triangle.Points.Add((new Point(0, 80)));
            triangle.Points.Add((new Point(40, 80)));
            triangle.Points.Add((new Point(40, 40)));



            ScaleTransform flip = new ScaleTransform();
            flip.ScaleY = -1;
            triangle.RenderTransform = flip;

            // Create a MapOverlay to contain the marker
            MapOverlay myLocationOverlay = new MapOverlay();

            myLocationOverlay.Content = triangle;
            myLocationOverlay.PositionOrigin = new Point(0, 0);

            myLocationOverlay.GeoCoordinate = new GeoCoordinate(lat, lon);

            // Create a MapLayer to contain the MapOverlay.
            MapLayer myLocationLayer = new MapLayer();
            myLocationLayer.Add(myLocationOverlay);

            // Add the MapLayer to the Map
            satMap.Layers.Add(myLocationLayer);
        }
        void satMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Sat.xaml?isCurrent=" + isCurrent + "&lat=" + latitude + "&lon=" + longitude, UriKind.Relative));
        }

        //Getting and Setting the background photo
        private void getFlickrPic()
        {
            setURL();
            numFlickrAttempts = 0;
            if (store.Contains("useFlickr"))
            {
                if ((bool)store["useFlickr"])
                {
                    if (weather == null)
                    {
                        flickrTags = "sky";
                    }
                    else if (weather.currentConditions == null)
                    {
                        flickrTags = "sky";
                    }
                    else
                    {
                        editFlickrTags();
                        fUrl = fUrl.Replace(weather.currentConditions, flickrTags);
                    }

                    startFlickrProg();

                    WebClient client = new WebClient();
                    client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(getFlickrXml);
                    client.DownloadStringAsync(new Uri(fUrl));
                }
                else
                {
                    return;
                }
            }
            else
            {
                store["useFlickr"] = true;
                getFlickrPic();
            }

        }
        private void editFlickrTags()
        {
            if (weather.currentConditions == null)
            {
                return;
            }
            else
            {
                string weatherUpper = weather.currentConditions.ToUpper();

                if (weatherUpper.Contains("THUNDER"))
                {
                    flickrTags = "thunder, thunderstorm, lightning, storm";
                }
                else if (weather.currentConditions.Contains("RAIN"))
                {
                    flickrTags = "rain, drizzle";
                }
                else if (weatherUpper.Contains("SNOW") || weatherUpper.Contains("FLURRY"))
                {
                    flickrTags = "snow, flurry, snowing";
                }
                else if (weatherUpper.Contains("FOG") || weatherUpper.Contains("MIST"))
                {
                    flickrTags = "fog, foggy, mist";
                }
                else if (weatherUpper.Contains("CLEAR"))
                {
                    flickrTags = "clear, sun, sunny, blue sky";
                }
                else if (weatherUpper.Contains("OVERCAST"))
                {
                    flickrTags = "overcast, cloudy";
                }
                else if (weatherUpper.Contains("CLOUDS") || weatherUpper.Contains("CLOUDY"))
                {
                    flickrTags = "cloudy, clouds, fluffy cloud";
                }
                else
                {
                    flickrTags = weatherUpper;
                }
            }
        }
        private void getFlickrXml(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                photoList.Clear();
                XDocument doc = XDocument.Parse(e.Result);
                var stat = doc.Element("rsp").Attribute("stat");
                if ((string)stat == "fail")
                {
                    progFlickr.Text = "Could not find any relevant pictures";
                    System.Threading.Thread.Sleep(500);
                    progFlickr.IsVisible = false;
                    HideTray();
                    return;
                }
                XElement photos = doc.Element("rsp").Element("photos");

                var rand = new Random();

                int numPhotos;
                if (photos.Attribute("total") != null && (string)photos.Attribute("total") != "" )
                {
                    numPhotos = Convert.ToInt32(photos.Attribute("total").Value);
                }
                else
                {
                    numPhotos = 0;
                }

                if (numPhotos == 0 && numFlickrAttempts <= 5)
                {
                    numFlickrAttempts++;
                    if (useWeatherGroup)
                    {
                        this.fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&group_id=" + weatherGroup + "&tags=" + flickrTags + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&format=rest";
                    }
                    else
                    {
                        this.fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&tags=" + flickrTags + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&format=rest";
                    }
                    WebClient client = new WebClient();
                    client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(getFlickrXml);
                    client.DownloadStringAsync(new Uri(fUrl));
                }
                else
                {
                    if (numFlickrAttempts > 5)
                    {
                        progFlickr.IsVisible = false;
                        HideTray();
                        return;
                    }



                    foreach (XElement photo in photos.Elements("photo"))
                    {
                        photoList.Add(new FlickrImage { farm = photo.Attribute("farm").Value, server = photo.Attribute("server").Value, secret = photo.Attribute("secret").Value, id = photo.Attribute("id").Value });
                    }

                    int randValue = rand.Next(photoList.Count);

                    string farm = photoList[randValue].farm;
                    string server = photoList[randValue].server;
                    string id = photoList[randValue].id;
                    string secret = photoList[randValue].secret;

                    string photoUrl = "http://farm" + farm + ".staticflickr.com/" + server + "/" + id + "_" + secret + "_b.jpg";
                    Uri photoUri = new Uri(photoUrl);
                    var downloadedPhoto = new BitmapImage(photoUri);

                    ImageBrush imageBrush = new ImageBrush();
                    imageBrush.ImageSource = downloadedPhoto;
                    imageBrush.Opacity = 0.7;
                    title.Background = imageBrush;
                    progFlickr.IsVisible = false;
                    HideTray();
                }
            }
        }
        public class FlickrImage
        {
            public string farm { get; set; }
            public string server { get; set; }
            public string secret { get; set; }
            public string id { get; set; }
        }

        //Getting Alerts
        private void updateAlerts()
        {
            if (store.Contains("loc"))
            {
                startAlertProg();

                alertListBox.ItemsSource = null;
                results.Clear();

                //get lat/long data from store
                String[] latlng = new String[2];
                latlng = (String[])store["loc"];
                this.latitude = latlng[0];
                this.longitude = latlng[1];

                string weatherGovUrl = "http://forecast.weather.gov/MapClick.php?lat=" + latitude + "&lon=" + longitude + "&FcstType=dwml";

                //start calling weather.gov
                WebClient client = new WebClient();
                client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_getAlerts);
                client.DownloadStringAsync(new Uri(weatherGovUrl));
            }
        }
        private void client_getAlerts(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && !e.Result.Contains("javascript") && !e.Result.Contains("!DOCTYPE"))
            {
                XDocument doc = XDocument.Parse(e.Result);
                var hazards = doc.Element("dwml").Element("data").Element("parameters").Element("hazards");
                var paramDesc = doc.Element("dwml").Element("data").Element("parameters").Elements("hazards");
                if (hazards == null)
                {
                    results.Add(new HazardResults() { Headline = "All clear right now!", TextUrl = null });
                }
                else
                {
                    foreach (XElement elm in doc.Element("dwml").Element("data").Element("parameters").Elements("hazards"))
                    {
                        var hazard = elm.Element("hazard-conditions").Element("hazard");
                        var hazardHeadline = (string)hazard.Attribute("headline").Value;
                        var hazardUrl = (string)hazard.Element("hazardTextURL").Value;
                        results.Add(new HazardResults() { Headline = hazardHeadline, TextUrl = hazardUrl });
                    }
                }
            }
            else
            {
                results.Add(new HazardResults() { Headline = "Can't get alerts for your area", TextUrl = null });
            }
            alertListBox.ItemsSource = results;
            progAlerts.IsVisible = false;
            HideTray();
        }
        private void alertListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WebBrowserTask webBrowser = new WebBrowserTask();
            var x = alertListBox.SelectedIndex;
            if (x != -1)
            {
                var alertArray = results.ToArray()[x];
                var url = alertArray.TextUrl;
                if (url != null)
                {
                    webBrowser.Uri = new Uri(url);
                    webBrowser.Show();
                }
            }

        }
        public class HazardResults
        {
            public string Headline { get; set; }
            public string TextUrl { get; set; }
        }

        //app bar click stuff
        private void settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPivot.xaml", UriKind.Relative));
        }
        private void refresh_Click(object sender, EventArgs e)
        {
            if (isTrial)
            {
                if (store.Contains("lastUpdated"))
                {
                    var appLastRun = Convert.ToDateTime(store["lastUpdated"]);
                    var now = DateTime.Now;
                    TimeSpan timeDiff = now.Subtract(appLastRun);
                    if ((int)timeDiff.TotalMinutes > 45)
                    {
                        getBackground = true;
                        updateWeather();
                        updateAlerts();
                    }
                    else
                    {
                        MessageBoxResult m = MessageBox.Show("Trial Mode can only be updated every 45 minutes, to save on cost. Buy now?", "Trial Mode", MessageBoxButton.OKCancel);
                        if (m == MessageBoxResult.OK)
                        {
                            MarketplaceDetailTask task = new MarketplaceDetailTask();
                            task.Show();
                        }
                    }
                }
            }
            else
            {
                getBackground = true;
                updateWeather();
                updateAlerts();
            }

        }
        private void pin_Click(object sender, EventArgs e)
        {
            if (!isTrial)
            {
                if (checkPeriodic(sender, e))
                {
                    if (store.Contains("lockUnitIsC"))
                    {
                        if (weather != null)
                        {
                            #region variables
                            convertTemp getTemp;
                            string todayHigh;
                            string todayLow;
                            string tomorrowHigh;
                            string tomorrowLow;
                            Uri normalIcon;
                            Uri smallIcon;
                            #endregion

                            IconicTileData locTile = new IconicTileData();
                            Uri[] weatherIcons = getWeatherIcons(weather.currentConditions);
                            normalIcon = weatherIcons[0];
                            smallIcon = weatherIcons[1];

                            locTile.IconImage = normalIcon;
                            locTile.SmallIconImage = smallIcon;
                            locTile.Title = cityNameLoad;

                            if ((bool)store["lockUnitIsC"])
                            {
                                getTemp = new convertTemp(weather.tempC);
                                todayHigh = weather.todayHighC;
                                todayLow = weather.todayLowC;
                                tomorrowHigh = weather.tomorrowHighC;
                                tomorrowLow = weather.tomorrowLowC;
                            }
                            else
                            {
                                getTemp = new convertTemp(weather.tempF);
                                todayHigh = weather.todayHighF;
                                todayLow = weather.todayLowF;
                                tomorrowHigh = weather.tomorrowHighF;
                                tomorrowLow = weather.tomorrowLowF;
                            }
                            locTile.Count = getTemp.temp;
                            locTile.WideContent1 = string.Format("Currently: " + weather.currentConditions + ", " + getTemp.temp + " degrees");
                            locTile.WideContent2 = string.Format("Today: " + weather.todayShort + " " + todayHigh + "/" + todayLow);
                            locTile.WideContent3 = string.Format("Tomorrow: " + weather.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow);

                            ShellTile.Create(new Uri("/MainPage.xaml?cityName=" + cityName + "&url=" + urlKey + "&isCurrent=" + isCurrent + "&lat=" + latitude + "&lon=" + longitude, UriKind.Relative), locTile, true);
                        }
                        else
                        {
                            IconicTileData locTile = new IconicTileData
                            {
                                IconImage = new Uri("SunCloud202.png", UriKind.Relative),
                                SmallIconImage = new Uri("SunCloud110.png", UriKind.Relative),
                                Title = cityNameLoad
                            };

                            ShellTile.Create(new Uri("/MainPage.xaml?cityName=" + cityName + "&url=" + urlKey + "&isCurrent=" + isCurrent + "&lat=" + latitude + "&lon=" + longitude, UriKind.Relative), locTile, true);
                        }
                    }
                    else
                    {
                        store["lockUnitIsC"] = true;
                        pin_Click(sender, e);
                    }
                }
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

        //get icons
        private Uri[] getWeatherIcons(string weatherConditions)
        {
            Uri normalIcon;
            Uri smallIcon;
            string weatherLower = weatherConditions.ToLower();

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

        private bool checkPeriodic(object sender, EventArgs e)
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
        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            getFlickrPic();
        }
        private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }
        private void ApplicationBarMenuItem_Click_2(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SelectLocation.xaml", UriKind.Relative));
        }

        //Change stuff in panorama as you move through
        private void title_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((Panorama)sender).SelectedIndex)
            {
                case 0:
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    break;
                case 1:
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    if (!mapsSet)
                    {
                        mapsSet = true;
                        setupRadar();
                        setupSat();
                    }
                    break;
                case 2:
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    break;
                case 3:
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    if (!alertSet)
                    {
                        alertSet = true;
                        updateAlerts();
                    }
                    break;
            }
        }
    }
}