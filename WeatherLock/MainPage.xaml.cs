using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Marketplace;
using Microsoft.Advertising.Mobile.UI;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using System.Windows.Shapes;
using Microsoft.Phone.Maps;


namespace WeatherLock
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region variables

        //Set Units
        bool tempUnitIsC;
        bool windUnitIsM;

        //Current Conditions
        private String cityName;
        private String weather;
        private String shortCityName;
        private String realFeel;
        private String windSpeedM;
        private String windSpeedK;
        private String windDir;
        private String humidityValue;
        private String updateTime;
        private String tempCompareText;
        private String tempC;
        private String tempF;
        private String realFeelC;
        private String realFeelF;

        //Forecast Conditions
        private String todayHigh;
        private String todayLow;
        private String forecastToday;
        private String forecastTomorrow;
        private String tomorrowHigh;
        private String tomorrowLow;

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

        //collections of alerts and forecast
        ObservableCollection<HazardResults> results = new ObservableCollection<HazardResults>();
        ObservableCollection<ForecastResults> foreRes = new ObservableCollection<ForecastResults>();

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
        #endregion

        //Initialize the main page
        public MainPage()
        {
            InitializeComponent();

            //Testing Key
            apiKey = "fb1dd3f4321d048d";

            initializeProgIndicators();
            //noParams();
            setUnits();
            restoreWeather();
            this.clock = new Clock(this);
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

                if (store.Contains("cityName") && !cityNameLoad.Contains("Current Location"))
                {
                    string citySplit = (string)store["cityName"].Split(',')[0];
                    string stateSplit = (string)store["cityName"].Split(',')[1];
                    string cityLoadSplit = cityNameLoad.Split(',')[0];
                    string stateLoadSplit = cityNameLoad.Split(',')[1];

                    if (!cityLoadSplit.Contains(citySplit))
                    {
                        if (!stateLoadSplit.Contains(stateSplit))
                        {
                            store["locChanged"] = true;
                        }
                    }
                }
                else if (cityNameLoad.Contains("Current Location"))
                {
                    store["locChanged"] = true;
                }
                store.Save();
            }
            else
            {
                noParams();
            }
            title.Title = cityNameLoad;
            checkUpdated();
            showAd();
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
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&group_id=" + weatherGroup + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather + "&per_page=500&tag_mode=any&content_type=1&media=photos&radius=32&format=rest";

                    }
                    else
                    {
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather + "&per_page=500&tag_mode=any&content_type=1&media=photos&radius=32&format=rest";
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
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&group_id=" + weatherGroup + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&has_geo=&format=rest";
                    }
                    else
                    {
                        fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&has_geo=&format=rest";
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
                        var getLocation = new getLocationMain();
                        if (getLocation.getLat() != null && getLocation.getLat() != "NA")
                        {
                            errorSet = false;
                            //Set long and lat
                            this.latitude = getLocation.getLat();
                            this.longitude = getLocation.getLong();

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

            if (store.Contains("lastUpdated"))
            {
                var appLastRun = Convert.ToDateTime(store["lastUpdated"]);
                var now = DateTime.Now;
                TimeSpan timeDiff = now.Subtract(appLastRun);
                if ((int)timeDiff.TotalMinutes > 15)
                {
                    getBackground = true;
                    //clearWeather();
                    updateWeather();
                    store["lastUpdated"] = DateTime.Now;
                }
                if (store.Contains("locChanged"))
                {
                    if ((bool)store["locChanged"])
                    {
                        //clearWeather();
                        store["locChanged"] = false;
                        getBackground = true;
                        updateWeather();
                        store["lastUpdated"] = DateTime.Now;
                    }
                }
                if (store.Contains("unitChanged"))
                {
                    if ((bool)store["unitChanged"])
                    {
                        store["unitChanged"] = false;
                        restoreWeather();
                    }
                }
                if (store.Contains("groupChanged"))
                {
                    if ((bool)store["groupChanged"])
                    {
                        store["groupChanged"] = false;
                        getFlickrPic();
                    }
                }
            }
            else
            {
                updateWeather();
                store["lastUpdated"] = DateTime.Now;
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
            SystemTray.SetOpacity(this, 0);
            progWeather.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progWeather);
        }
        private void startFlickrProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progFlickr.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progFlickr);
        }
        private void startAlertProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
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
            //Convert weather text to caps
            weather = weather.ToUpper();

            //Restore all the data
            if (weatherSet)
            {
                title.Title = cityName;
                weatherSet = false;
            }
            if (tempUnitIsC)
            {
                temp.Text = tempC + "°";
                feelsLike.Text = "Feels like: " + realFeelC + "°";
            }
            else
            {
                temp.Text = tempF + "°";
                feelsLike.Text = "Feels like: " + realFeelF + "°";
            }
            if (windUnitIsM)
            {
                wind.Text = "Wind: " + windSpeedM + " " + windDir;
            }
            else
            {
                wind.Text = "Wind: " + windSpeedK + " " + windDir;
            }

            conditions.Text = weather;


            humidity.Text = "Humidity: " + humidityValue;
            tempCompare.Text = "TOMORROW WILL BE " + tempCompareText + " TODAY";
            forecastListBox.ItemsSource = foreRes;
            alertListBox.ItemsSource = results;

            store["cityName"] = cityName;

            if (!errorSet)
            {
                errorText.Text = null;
            }

            backupWeather();
            progWeather.IsVisible = false;
            HideTray();
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
            if ((bool)store.Contains("backupForecast"))
            {
                forecastListBox.ItemsSource = null;
                foreRes.Clear();
                foreRes = (ObservableCollection<ForecastResults>)store["backupForecast"];
            }


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

            String[] savedData = new String[15];
            if ((bool)store.Contains("backupApp"))
            {
                savedData = (string[])store["backupApp"];

                this.cityName = savedData[0];
                this.shortCityName = savedData[1];
                this.realFeelF = savedData[2];
                this.weather = savedData[3];
                this.todayHigh = savedData[4];
                this.todayLow = savedData[5];
                this.forecastToday = savedData[6];
                this.forecastTomorrow = savedData[7];
                this.tomorrowHigh = savedData[8];
                this.tomorrowLow = savedData[9];
                this.windSpeedM = savedData[10];
                this.realFeel = savedData[11];
                this.humidityValue = savedData[12];
                this.windDir = savedData[13];
                this.updateTime = savedData[14];
                this.tempCompareText = savedData[15];
                this.tempC = savedData[16];
                this.tempF = savedData[17];
                this.realFeelC = savedData[18];
                this.windSpeedK = savedData[19];

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

            forecastListBox.ItemsSource = null;
            foreRes.Clear();
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
                XDocument doc = XDocument.Parse(e.Result);
                XElement error = doc.Element("response").Element("error");
                if (error == null)
                {

                    #region current conditions
                    //Current Conditions
                    var currentObservation = doc.Element("response").Element("current_observation");
                    //location name
                    string city = (string)currentObservation.Element("display_location").Element("city");
                    string state = (string)currentObservation.Element("display_location").Element("state_name");
                    this.cityName = city + ", " + state;
                    this.shortCityName = (string)currentObservation.Element("display_location").Element("city");

                    if (store["defaultLocation"] == "Current Location")
                    {
                        store["saveDefaultLocName"] = cityName;
                        store.Save();
                    }
                    //Current Weather
                    this.weather = (string)currentObservation.Element("weather");
                    //Current wind

                    this.windSpeedM = (string)currentObservation.Element("wind_mph") + " mph";

                    this.windSpeedK = (string)currentObservation.Element("wind_kph") + " kph";

                    this.windDir = (string)currentObservation.Element("wind_dir");
                    //Current Temp and feels like

                    this.tempC = (string)currentObservation.Element("temp_c");
                    this.realFeelC = (string)currentObservation.Element("feelslike_c");

                    this.tempF = (string)currentObservation.Element("temp_f");
                    this.realFeelF = (string)currentObservation.Element("feelslike_f");

                    //current humidity
                    this.humidityValue = (string)currentObservation.Element("relative_humidity");
                    #endregion
                    #region forecast conditions
                    //Forecast Conditions
                    XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");
                    //Today's conditions
                    var today = forecastDays.Element("forecastday");
                    //Today's Forecast
                    this.forecastToday = (string)today.Element("conditions");
                    //Today's High/Low
                    if (tempUnitIsC)
                    {
                        this.todayLow = (string)today.Element("low").Element("celsius");
                        this.todayHigh = (string)today.Element("high").Element("celsius");
                    }
                    else
                    {
                        this.todayLow = (string)today.Element("low").Element("fahrenheit");
                        this.todayHigh = (string)today.Element("high").Element("fahrenheit");
                    }
                    //Tomorrow's conditions
                    var tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();
                    //Tomorrow's Forecast
                    this.forecastTomorrow = (string)tomorrow.Element("conditions");
                    //Tomorrow's High/Low
                    if (tempUnitIsC)
                    {
                        this.tomorrowHigh = (string)tomorrow.Element("low").Element("celsius");
                        this.tomorrowLow = (string)tomorrow.Element("high").Element("celsius");
                    }
                    else
                    {
                        this.tomorrowHigh = (string)tomorrow.Element("high").Element("fahrenheit");
                        this.tomorrowLow = (string)tomorrow.Element("low").Element("fahrenheit");
                    }

                    int todayHighInt = Convert.ToInt32(todayHigh);
                    int tomorrowHighInt = Convert.ToInt32(tomorrowLow);

                    if (todayHighInt > tomorrowHighInt)
                    {
                        tempCompareText = "COOLER THAN";
                    }
                    else if (todayHighInt < tomorrowHighInt)
                    {
                        tempCompareText = "WARMER THAN";
                    }
                    else
                    {
                        tempCompareText = "ABOUT THE SAME AS";
                    }

                    var forecastDaysTxt = doc.Element("response").Element("forecast").Element("txt_forecast").Element("forecastdays");
                    foreach (XElement elm in forecastDaysTxt.Elements("forecastday"))
                    {
                        string title = (string)elm.Element("title");
                        string fcttext = (string)elm.Element("fcttext");
                        string fcttextMet = (string)elm.Element("fcttext_metric");
                        string pop = (string)elm.Element("pop");

                        if (store.Contains("forecastUnitisI"))
                            if (!(bool)store["forecastUnitisI"])
                            {
                                fcttext = fcttextMet;
                            }

                        this.foreRes.Add(new ForecastResults() { title = title, fcttext = fcttext, pop = pop });
                        this.forecastListBox.ItemsSource = foreRes;
                    }
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
                    clearWeather();

                    errorSet = true;
                    string errorDescrip = (string)error.Element("description");
                    if (errorDescrip.Contains("location"))
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
            String[] backup = { cityName,
                                  shortCityName,
                                  realFeelF,
                                  weather,
                                  todayHigh,
                                  todayLow,
                                  forecastToday,
                                  forecastTomorrow,
                                  tomorrowHigh,
                                  tomorrowLow,
                                  windSpeedM,
                                  realFeel,
                                  humidityValue,
                                  windDir,
                                  updateTime,
                                  tempCompareText,
                                  tempC,
                                  tempF,
                                  realFeelC,
                                  windSpeedK
                              };
            store["locationName"] = cityName;
            store["backupApp"] = backup;

            store["backupForecast"] = foreRes;
            store["backupAlerts"] = results;
        }
        public class ForecastResults
        {
            public string title { get; set; }
            public string fcttext { get; set; }
            public string pop { get; set; }
        }

        //Maps Pane
        //Radar
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
                double lat = Convert.ToDouble(latitude);
                double lon = Convert.ToDouble(longitude);
                radarMap.Center = new GeoCoordinate(lat, lon);
                radarMap.CartographicMode = MapCartographicMode.Road;
                radarMap.ZoomLevel = 5;

                showRadarLocation();
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
        private void showRadarLocation()
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

            double lat = Convert.ToDouble(latitude);
            double lon = Convert.ToDouble(longitude);

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
        //Sat
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
                double lat = Convert.ToDouble(latitude);
                double lon = Convert.ToDouble(longitude);
                satMap.Center = new GeoCoordinate(lat, lon);
                satMap.CartographicMode = MapCartographicMode.Road;
                satMap.ZoomLevel = 5;


                showSatLocation();

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
        private void showSatLocation()
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

            double lat = Convert.ToDouble(latitude);
            double lon = Convert.ToDouble(longitude);

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
                    else
                    {
                        editFlickrTags();
                        fUrl = fUrl.Replace(weather, flickrTags);
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
            if (weather.Contains("THUNDER"))
            {
                flickrTags = "thunder, thunderstorm, lightning, storm";
            }
            else if (weather.Contains("RAIN"))
            {
                flickrTags = "rain, drizzle";
            }
            else if (weather.Contains("SNOW") || weather.Contains("FLURRY"))
            {
                flickrTags = "snow, flurry, snowing";
            }
            else if (weather.Contains("FOG") || weather.Contains("MIST"))
            {
                flickrTags = "fog, foggy, mist";
            }
            else if (weather.Contains("CLEAR"))
            {
                flickrTags = "clear, sun, sunny, blue sky";
            }
            else if (weather.Contains("OVERCAST"))
            {
                flickrTags = "overcast, cloudy";
            }
            else if (weather.Contains("CLOUDS") || weather.Contains("CLOUDY"))
            {
                flickrTags = "cloudy, clouds, fluffy cloud";
            }
            else
            {
                flickrTags = weather;
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

                int numPhotos = Convert.ToInt32(photos.Attribute("total").Value);
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


                IconicTileData locTile = new IconicTileData
                {
                    IconImage = new Uri("SunCloud202.png", UriKind.Relative),
                    SmallIconImage = new Uri("SunCloud110.png", UriKind.Relative),
                    Title = cityName
                };

                ShellTile.Create(new Uri("/MainPage.xaml?cityName=" + cityName + "&url=" + urlKey + "&isCurrent=" + isCurrent + "&lat=" + latitude + "&lon=" + longitude, UriKind.Relative), locTile, true);
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