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

namespace WeatherLock
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region variables

        //Set Units
        String tempUnit = null;
        String windUnit = null;

        //Current Conditions
        private String cityName;
        private String tempStr;
        private String weather;
        private String shortCityName;
        private String realFeel;
        private String windSpeed;
        private String windDir;
        private String humidityValue;
        private String updateTime;
        private String tempCompareText;

        private String flickrTags;

        //Forecast Conditions
        private String todayHigh;
        private String todayLow;
        private String forecastToday;
        private String forecastTomorrow;
        private String tomorrowHigh;
        private String tomorrowLow;

        String latitude = null;
        String longitude = null;

        //Wunderground Api
        //Release Key
        String apiKey = "102b8ec7fbd47a05";

        String url = null;

        //Flickr Api
        String fApiKey = "2781c025a4064160fc77a52739b552ff";
        String fUrl = null;

        ObservableCollection<HazardResults> results = new ObservableCollection<HazardResults>();
        ObservableCollection<ForecastResults> foreRes = new ObservableCollection<ForecastResults>();

        private Clock clock;
        ProgressIndicator progWeather;
        ProgressIndicator progFlickr;
        ProgressIndicator progAlerts;

        private int hideTray;

        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        //Check to see if app is running as trial
        LicenseInformation licInfo;
        public bool isTrial;

        #endregion

        public MainPage()
        {
            InitializeComponent();

            //Testing Key
            apiKey = "fb1dd3f4321d048d";

            restoreWeather();
            getFlickrPic();
            this.clock = new Clock(this);
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            licInfo = new LicenseInformation();
            isTrial = licInfo.IsTrial();

            setUnits();
            checkUpdated();
            updateAlerts();
            showAd();
        }

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
        private void startWeatherProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progWeather = new ProgressIndicator();
            progWeather.Text = "Updating Weather";
            progWeather.IsIndeterminate = true;
            progWeather.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progWeather);
        }
        private void startFlickrProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progFlickr = new ProgressIndicator();
            progFlickr.Text = "Updating Background";
            progFlickr.IsIndeterminate = true;
            progFlickr.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progFlickr);
        }
        private void startAlertProg()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progAlerts = new ProgressIndicator();
            progAlerts.Text = "Updating Alerts";
            progAlerts.IsIndeterminate = true;
            progAlerts.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progAlerts);

        }
        private void HideTray()
        {
            //if(progAlerts.IsVisible==false && progFlickr.IsVisible==false && progWeather.IsVisible == false){
            //  SystemTray.SetIsVisible(this, false);
            //}
            if (hideTray > 2)
            {
                hideTray = 0;
                SystemTray.SetIsVisible(this, false);

            }
            else
            {
                hideTray++;
            }

        }

        //Check the units to use
        private void setUnits()
        {
            if (store.Contains("tempIsC"))
            {
                if ((bool)store["tempIsC"])
                {
                    this.tempUnit = "c";
                }
                else
                {
                    this.tempUnit = "f";
                }
            }
            else
            {
                this.tempUnit = "c";
            }

            if (store.Contains("windUnit"))
            {
                if ((string)store["windUnit"] == "m")
                {
                    this.windUnit = "m";
                }
                else
                {
                    this.windUnit = "k";
                }
            }
            else
            {
                this.windUnit = "m";
            }
        }

        //Set the URLs
        private void setURL()
        {
            if (latitude == null || longitude == null)
            {
                findLocation();
            }
            else
            {
                if (store.Contains("LocationConsent"))
                {
                    if ((bool)store["LocationConsent"])
                    {
                        if (latitude != null && longitude != null)
                        {
                            url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast/q/" + latitude + "," + longitude + ".xml";
                            fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather + "&per_page=500&tag_mode=any&content_type=1&media=photos&radius=32&format=rest";
                        }
                        else
                        {
                            findLocation();
                            setURL();
                        }
                    }
                    else
                    {
                        if (latitude != null && longitude != null)
                        {
                            fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&lat=" + latitude + "&lon=" + longitude + "&tags=" + weather + "&per_page=500&tag_mode=any&content_type=1&media=photos&sort=relevance&has_geo=&format=rest";
                        }
                        else
                        {
                            findLocation();
                            setURL();
                        }

                        if (store.Contains("locUrl"))
                        {
                            url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + store["locUrl"] + ".xml";
                        }
                    }
                }
            }
        }

        //Find the location
        private void findLocation()
        {
            if (!store.Contains("LocationConsent"))
            {
                store["LocationConsent"] = true;
                findLocation();
            }

            if ((bool)store["LocationConsent"])
            {
                //get location
                var getLocation = new getLocationMain();
                if (getLocation.getLat() != null && getLocation.getLat() != "NA")
                {
                    //Set long and lat
                    this.latitude = getLocation.getLat();
                    this.longitude = getLocation.getLong();

                    //Save
                    String[] loc = { latitude, longitude };
                    store["loc"] = loc;
                }
                else
                {
                    findLocation();
                }
            }
            else
            {
                if (store.Contains("loc"))
                {
                    String[] latlng = new String[2];
                    latlng = (String[])store["loc"];
                    this.latitude = latlng[0];
                    this.longitude = latlng[1];
                }
            }
            setURL();
        }

        //Check whether weather should be updated
        private void checkUpdated()
        {
            setURL();
            if (!isTrial)
            {
                if (store.Contains("lastUpdated"))
                {
                    var appLastRun = Convert.ToDateTime(store["lastUpdated"]);
                    var now = DateTime.Now;
                    TimeSpan timeDiff = now.Subtract(appLastRun);
                    if ((int)timeDiff.TotalMinutes > 15)
                    {

                        updateWeather();
                        store["lastUpdated"] = DateTime.Now;
                    }
                    if (store.Contains("locChanged"))
                    {
                        if ((bool)store["locChanged"] == true)
                        {
                            store["locChanged"] = false;

                            updateWeather();
                            getFlickrPic();
                            store["lastUpdated"] = DateTime.Now;
                        }
                    }
                    if (store.Contains("unitChanged") == true)
                    {
                        if ((bool)store["unitChanged"] == true)
                        {
                            store["unitChanged"] = false;

                            updateWeather();
                            store["lastUpdated"] = DateTime.Now;
                        }
                    }
                }
                else
                {

                    updateWeather();
                    store["lastUpdated"] = DateTime.Now;
                }
            }
            //Only allow updating every 45 min if in trial
            else
            {
                if (store.Contains("lastUpdated"))
                {
                    var appLastRun = Convert.ToDateTime(store["lastUpdated"]);
                    var now = DateTime.Now;
                    TimeSpan timeDiff = now.Subtract(appLastRun);
                    if ((int)timeDiff.TotalMinutes > 45)
                    {

                        updateWeather();
                        store["lastUpdated"] = DateTime.Now;
                    }
                    if (store.Contains("locChanged") && (int)timeDiff.TotalMinutes > 45)
                    {
                        if ((bool)store["locChanged"] == true)
                        {
                            store["locChanged"] = false;

                            updateWeather();
                            getFlickrPic();
                            store["lastUpdated"] = DateTime.Now;
                        }
                    }
                    if (store.Contains("unitChanged") && (int)timeDiff.TotalMinutes > 45)
                    {
                        if ((bool)store["unitChanged"] == true)
                        {
                            store["unitChanged"] = false;

                            updateWeather();
                            store["lastUpdated"] = DateTime.Now;
                        }
                    }
                }
                else
                {

                    updateWeather();
                    store["lastUpdated"] = DateTime.Now;
                }
            }
        }

        //Getting and Setting Weather data
        private void restoreWeather()
        {
            startWeatherProg();
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
                savedData = store["backupApp"];

                this.cityName = savedData[0];
                this.shortCityName = savedData[1];
                this.tempStr = savedData[2];
                this.weather = savedData[3];
                this.todayHigh = savedData[4];
                this.todayLow = savedData[5];
                this.forecastToday = savedData[6];
                this.forecastTomorrow = savedData[7];
                this.tomorrowHigh = savedData[8];
                this.tomorrowLow = savedData[9];
                this.windSpeed = savedData[10];
                this.realFeel = savedData[11];
                this.humidityValue = savedData[12];
                this.windDir = savedData[13];
                this.updateTime = savedData[14];
                this.tempCompareText = savedData[15];

                if (this.weather == null)
                {
                    updateWeather();
                }
                else
                {
                    setWeather();
                }
            }
        }
        private void setWeather()
        {
            //Convert weather text to caps
            weather = weather.ToUpper();

            //Restore all the data
            title.Title = cityName;
            temp.Text = tempStr + "°";
            conditions.Text = weather;
            feelsLike.Text = "Feels like: " + realFeel + "°";
            wind.Text = "Wind: " + windSpeed + " " + windDir;
            humidity.Text = "Humidity: " + humidityValue;
            tempCompare.Text = "TOMORROW WILL BE " + tempCompareText + " TODAY";
            forecastListBox.ItemsSource = foreRes;
            alertListBox.ItemsSource = results;

            progWeather.IsVisible = false;
            backupWeather();
            HideTray();
        }
        private void updateWeather()
        {
            setURL();
            startWeatherProg();

            forecastListBox.ItemsSource = null;
            foreRes.Clear();

            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            client.DownloadStringAsync(new Uri(url));
        }
        private void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);

                #region current conditions
                //Current Conditions
                var currentObservation = doc.Element("response").Element("current_observation");
                //location name
                this.cityName = (string)currentObservation.Element("display_location").Element("full");
                this.shortCityName = (string)currentObservation.Element("display_location").Element("city");
                //Current Weather
                this.weather = (string)currentObservation.Element("weather");
                //Current wind
                if (windUnit == "m")
                {
                    this.windSpeed = (string)currentObservation.Element("wind_mph") + " mph";
                }
                else
                {
                    this.windSpeed = (string)currentObservation.Element("wind_kph") + " kph";
                }
                this.windDir = (string)currentObservation.Element("wind_dir");
                //Current Temp and feels like
                if (tempUnit == "c")
                {
                    this.tempStr = (string)currentObservation.Element("temp_c");
                    this.realFeel = (string)currentObservation.Element("feelslike_c");
                }
                else
                {
                    this.tempStr = (string)currentObservation.Element("temp_f");
                    this.realFeel = (string)currentObservation.Element("feelslike_f");
                }
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
                if (tempUnit == "c")
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
                if (tempUnit == "c")
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

                    if (store.Contains("forecastUnit"))
                        if (store["forecastUnit"] == "m")
                        {
                            fcttext = fcttextMet;
                        }

                    this.foreRes.Add(new ForecastResults() { title = title, fcttext = fcttext, pop = pop });
                    this.forecastListBox.ItemsSource = foreRes;
                }
                #endregion

                //set ui elements
                setWeather();

                //Back it all up
                backupWeather();

            }

        }
        private void backupWeather()
        {
            String[] backup = { cityName,
                                  shortCityName,
                                  tempStr,
                                  weather,
                                  todayHigh,
                                  todayLow,
                                  forecastToday,
                                  forecastTomorrow,
                                  tomorrowHigh,
                                  tomorrowLow,
                                  windSpeed,
                                  realFeel,
                                  humidityValue,
                                  windDir,
                                  updateTime,
                                  tempCompareText
                              };
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

        //Getting and Setting the background photo
        private void getFlickrPic()
        {
            setURL();
            
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
            else if (weather.Contains("FOG"))
            {
                flickrTags = "fog, foggy";
            }
            else if (weather.Contains("CLEAR"))
            {
                flickrTags = "sky, clear, sun, sunny, blue sky";
            }
            else if (weather.Contains("CLOUDY"))
            {
                flickrTags = "cloudy, clouds, sky, fluffy cloud";
            }
            else if (weather.Contains("OVERCAST"))
            {
                flickrTags = "overcast, clouds, cloudy";
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
                XDocument doc = XDocument.Parse(e.Result);
                var stat = doc.Element("rsp").Attribute("stat");
                if ((string)stat == "fail")
                {
                    return;
                }
                var photos = doc.Element("rsp").Element("photos");

                var rand = new Random();

                int numPhotos = Convert.ToInt32(photos.Attribute("total").Value);
                if (numPhotos == 0)
                {
                    this.fUrl = "http://ycpi.api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + fApiKey + "&tags=" + flickrTags + "&per_page=500&tag_mode=any&content_type=1&media=photos&accuracy=11&sort=relevance&has_geo=&format=rest";
                    WebClient client = new WebClient();
                    client.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.Now.ToString();
                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(getFlickrXml);
                    client.DownloadStringAsync(new Uri(fUrl));
                }
                else
                {
                    int randValue;
                    if (numPhotos < 251)
                    {
                        randValue = rand.Next(numPhotos + 1);
                    }
                    else
                    {
                        randValue = rand.Next(251);
                    }


                    var photo = photos.Element("photo");
                    for (int x = 1; x < randValue - 1; x++)
                    {
                        photo = photo.ElementsAfterSelf("photo").First();
                    }

                    string farm = photo.Attribute("farm").Value;
                    string server = photo.Attribute("server").Value;
                    string secret = photo.Attribute("secret").Value;
                    string id = photo.Attribute("id").Value;

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
            if (!e.Cancelled && e.Error == null && !e.Result.Contains("javascript"))
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
                        alertListBox.ItemsSource = results;
                    }
                }
            }
            else
            {
                results.Add(new HazardResults() { Headline = "Can't get alerts for your area", TextUrl = null });
            }
            progAlerts.IsVisible = false;
            HideTray();
        }
        private void alertListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WebBrowserTask webBrowser = new WebBrowserTask();
            var x = alertListBox.SelectedIndex;
            var alertArray = results.ToArray()[x];
            var url = alertArray.TextUrl;
            if (url != null)
            {
                webBrowser.Uri = new Uri(url);
                webBrowser.Show();
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
                        hideTray = 4;
                        updateWeather();
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
                hideTray = 4;
                updateWeather();
            }

        }
        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            hideTray = 4;
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
    }
}