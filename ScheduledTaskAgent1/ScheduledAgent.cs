using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using System.Linq;
using Helpers;
using WeatherData;

namespace ScheduledTaskAgent1
{
    public class ScheduledAgent : ScheduledTaskAgent
    {

        #region variables
        private String defaultCityName;
        private Boolean tempUnitIsC;
        private String url;
        private String locUrl;
        private Boolean isCurrent;
        private String apiKey = "102b8ec7fbd47a05";

        private String latitude;
        private String longitude;

        private int locationSearchTimesDefault;
        private int locationSearchTimesCurrent;
        private Boolean error;
        private string errorText;

        //save myself a bit of typing
        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        List<Pins> pinnedList = new List<Pins>();
        List<Pins> pinnedListCopy = new List<Pins>();
        List<WeatherInfo> weatherList = new List<WeatherInfo>();

        public class Pins
        {
            public string LocName;
            public string LocUrl;
            public bool currentLoc;
            public bool updated;
        }
        #endregion


        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        //Do this stuff first
        protected override void OnInvoke(ScheduledTask task)
        {
            //Testing Key
            apiKey = "fb1dd3f4321d048d";

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

            //updates the app after a selected time
            if (store.Contains("lastRun") && store.Contains("updateRate"))
            {
                //get savedSettingsd time variables
                var updateRate = Convert.ToInt32(store["updateRate"]);
                var currentTime = DateTime.Now;
                var lastRunTime = Convert.ToDateTime(store["lastRun"]);

                //calculate time (in min) since last run
                TimeSpan timeDiff = currentTime.Subtract(lastRunTime);

                //if more than run period, update
                if ((int)timeDiff.TotalMinutes > updateRate)
                {

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
                                var toast = new Toast();
                                toast.sendToast("Weathr", "Please run the app first");
                                NotifyComplete();
                            }
                        }
                    }
                }
                else
                {
                    //if time period is too short, don't update
                    NotifyComplete();
                }
            }
            //runs the app for the first time (when the background agent has never been launched)
            else if (!store.Contains("lastRun"))
            {
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
                            var toast = new Toast();
                            toast.sendToast("Weathr", "Please run the app first");
                            NotifyComplete();
                        }
                    }
                }
            }
            else
            {
                NotifyComplete();
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
            locationSearchTimesDefault = 0;
            checkLocationDefault();

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
                    locationSearchTimesCurrent = 0;
                    checkLocationCurrent();
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
            WeatherInfo weatherMain = new WeatherInfo();

            if (!e.Cancelled && e.Error == null)
            {
                Uri normalIcon = new Uri("/SunCloud202.png", UriKind.Relative);
                Uri smallIcon = new Uri("/SunCloud110.png", UriKind.Relative);

                XDocument doc = XDocument.Parse(e.Result);
                XElement weatherError = doc.Element("response").Element("error");
                if (weatherError == null && !error)
                {
                    //Current Conditions
                    XElement currentObservation = doc.Element("response").Element("current_observation");
                    weatherMain.city = (string)currentObservation.Element("display_location").Element("city");
                    weatherMain.state = (string)currentObservation.Element("display_location").Element("state_name");
                    string cityName = weatherMain.city + ", " + weatherMain.state;


                    weatherMain.currentConditions = (string)currentObservation.Element("weather");
                    weatherMain.tempC = (string)currentObservation.Element("temp_c");
                    weatherMain.tempF = (string)currentObservation.Element("temp_f");

                    XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                    XElement today = forecastDays.Element("forecastday");
                    weatherMain.todayShort = (string)today.Element("conditions");
                    weatherMain.todayLowC = (string)today.Element("low").Element("celsius");
                    weatherMain.todayHighC = (string)today.Element("high").Element("celsius");
                    weatherMain.todayLowF = (string)today.Element("low").Element("fahrenheit");
                    weatherMain.todayHighF = (string)today.Element("high").Element("fahrenheit");

                    XElement tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();
                    weatherMain.tomorrowShort = (string)tomorrow.Element("conditions");
                    weatherMain.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                    weatherMain.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");
                    weatherMain.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                    weatherMain.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");

                    //get weather icons
                    Uri[] weatherIcons = getWeatherIcons(weatherMain.currentConditions);
                    normalIcon = weatherIcons[0];
                    smallIcon = weatherIcons[1];

                    //convert temps to ints
                    convertTemp getTemp;
                    string todayHigh;
                    string todayLow;
                    string tomorrowHigh;
                    string tomorrowLow;

                    if (tempUnitIsC)
                    {
                        getTemp = new convertTemp(weatherMain.tempC);
                        todayHigh = weatherMain.todayHighC;
                        todayLow = weatherMain.todayLowC;
                        tomorrowHigh = weatherMain.tomorrowHighC;
                        tomorrowLow = weatherMain.tomorrowLowC;
                    }
                    else
                    {
                        getTemp = new convertTemp(weatherMain.tempF);
                        todayHigh = weatherMain.todayHighF;
                        todayLow = weatherMain.todayLowF;
                        tomorrowHigh = weatherMain.tomorrowHighF;
                        tomorrowLow = weatherMain.tomorrowLowF;
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
                                    WideContent1 = string.Format("Currently: " + weatherMain.currentConditions + ", " + temp + " degrees"),
                                    WideContent2 = string.Format("Today: " + weatherMain.todayShort + " " + todayHigh + "/" + todayLow),
                                    WideContent3 = string.Format("Tomorrow: " + weatherMain.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                };
                                tile.Update(TileData);

                                //mark tile as updated
                                markUpdated("default location", weatherMain.city, weatherMain.state, false);

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
                                        WideContent1 = string.Format("Currently: " + weatherMain + ", " + temp + " degrees"),
                                        WideContent2 = string.Format("Today: " + weatherMain.todayShort + " " + todayHigh + "/" + todayLow),
                                        WideContent3 = string.Format("Tomorrow: " + weatherMain.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                    };
                                    tile.Update(TileData);

                                    //mark tile as updated
                                    markUpdated("default location", weatherMain.city, weatherMain.state, false);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (!error)
                {
                    weatherMain.error = doc.Element("response").Element("error").Element("description").Value;
                    errorText = weatherMain.error;
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
                    NotifyComplete();
                }
            }
            else
            {
                NotifyComplete();
            }
        }
        private void updateOtherTiles(object sender, DownloadStringCompletedEventArgs e)
        {
            WeatherInfo weather = new WeatherInfo();

            if (!e.Cancelled && e.Error == null)
            {
                Uri normalIcon = new Uri("/SunCloud202.png", UriKind.Relative);
                Uri smallIcon = new Uri("/SunCloud110.png", UriKind.Relative);

                XDocument doc = XDocument.Parse(e.Result);
                XElement weatherError = doc.Element("response").Element("error");
                if (weatherError == null && !error)
                {
                    //Current Conditions
                    XElement currentObservation = doc.Element("response").Element("current_observation");
                    weather.city = (string)currentObservation.Element("display_location").Element("city");
                    weather.state = (string)currentObservation.Element("display_location").Element("state_name");
                    string cityName = weather.city + ", " + weather.state;
                    weather.currentConditions = (string)currentObservation.Element("weather");

                    XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                    XElement today = forecastDays.Element("forecastday");
                    XElement tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                    weather.todayShort = (string)today.Element("conditions");
                    weather.tomorrowShort = (string)tomorrow.Element("conditions");

                    weather.tempC = (string)currentObservation.Element("temp_c");
                    weather.todayLowC = (string)today.Element("low").Element("celsius");
                    weather.todayHighC = (string)today.Element("high").Element("celsius");
                    weather.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                    weather.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");

                    weather.tempF = (string)currentObservation.Element("temp_f");
                    weather.todayLowF = (string)today.Element("low").Element("fahrenheit");
                    weather.todayHighF = (string)today.Element("high").Element("fahrenheit");
                    weather.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                    weather.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");

                    //get weather icons
                    Uri[] weatherIcons = getWeatherIcons(weather.currentConditions);
                    normalIcon = weatherIcons[0];
                    smallIcon = weatherIcons[1];

                    //convert temps to ints
                    convertTemp getTemp;
                    string todayHigh;
                    string todayLow;
                    string tomorrowHigh;
                    string tomorrowLow;

                    if (tempUnitIsC)
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
                                else if (((tileLoc == cityName && pin.LocName == cityName) || (pin.LocName.Split(',')[0].Contains(weather.city) && tileLoc.Split(',')[0].Contains(weather.city) && pin.LocName.Split(',')[1].Contains(weather.state) && tileLoc.Split(',')[1].Contains(weather.state))) && !pin.updated)
                                {
                                    //Update Tile
                                    IconicTileData TileData = new IconicTileData
                                    {
                                        IconImage = normalIcon,
                                        SmallIconImage = smallIcon,
                                        Title = cityName,
                                        Count = temp,
                                        WideContent1 = string.Format("Currently: " + weather.currentConditions + ", " + temp + " degrees"),
                                        WideContent2 = string.Format("Today: " + weather.todayShort + " " + todayHigh + "/" + todayLow),
                                        WideContent3 = string.Format("Tomorrow: " + weather.tomorrowShort + " " + tomorrowHigh + "/" + tomorrowLow)

                                    };
                                    tile.Update(TileData);

                                    //mark the tile as finished updating
                                    markUpdated(cityName, weather.city, weather.state, false);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                if (finished())
                {
                    NotifyComplete();
                }
            }
            else
            {
                NotifyComplete();
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
                    //Current Conditions
                    XElement currentObservation = doc.Element("response").Element("current_observation");
                    weatherCurrent.city = (string)currentObservation.Element("display_location").Element("city");
                    weatherCurrent.state = (string)currentObservation.Element("display_location").Element("state_name");
                    string cityName = weatherCurrent.city + ", " + weatherCurrent.state;
                    weatherCurrent.currentConditions = (string)currentObservation.Element("weather");

                    XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                    XElement today = forecastDays.Element("forecastday");
                    XElement tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                    weatherCurrent.todayShort = (string)today.Element("conditions");
                    weatherCurrent.tomorrowShort = (string)tomorrow.Element("conditions");

                    weatherCurrent.tempC = (string)currentObservation.Element("temp_c");
                    weatherCurrent.todayLowC = (string)today.Element("low").Element("celsius");
                    weatherCurrent.todayHighC = (string)today.Element("high").Element("celsius");
                    weatherCurrent.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                    weatherCurrent.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");

                    weatherCurrent.tempF = (string)currentObservation.Element("temp_f");
                    weatherCurrent.todayLowF = (string)today.Element("low").Element("fahrenheit");
                    weatherCurrent.todayHighF = (string)today.Element("high").Element("fahrenheit");
                    weatherCurrent.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                    weatherCurrent.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");

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

                    if (tempUnitIsC)
                    {
                        getTemp = new convertTemp(weatherCurrent.tempC);
                        todayHigh = weatherCurrent.todayHighC;
                        todayLow = weatherCurrent.todayLowC;
                        tomorrowHigh = weatherCurrent.tomorrowHighC;
                        tomorrowLow = weatherCurrent.tomorrowLowC;
                    }
                    else
                    {
                        getTemp = new convertTemp(weatherCurrent.tempF);
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
                                        WideContent1 = string.Format("Currently: " + weatherCurrent.currentConditions + ", " + temp + " degrees"),
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
                                    markUpdated("current location", "null", "null", true);

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
                                    markUpdated("current location", "null", "null", true);

                                    //stop looping
                                    break;
                                }
                            }
                        }
                    }
                }
                if (finished())
                {
                    NotifyComplete();
                }
            }
            else
            {
                NotifyComplete();
            }
        }

        private void sendToast(string cityName)
        {
            //send toast if enabled
            if (store.Contains("notifyMe"))
            {
                if ((bool)store["notifyMe"] == true)
                {
                    var newToast = new Toast();
                    newToast.sendToast(cityName, " has been updated!");
                }
            }
        }

        //get weather icon Uri for tiles
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
        private void checkLocationDefault()
        {
            //Check to see if allowed to get location
            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {
                    if (locationSearchTimesDefault <= 5)
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
                            locationSearchTimesDefault++;
                            checkLocationDefault();
                        }
                    }
                    if (locationSearchTimesDefault > 5)
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
        private void checkLocationCurrent()
        {
            //Check to see if allowed to get location
            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {
                    if (locationSearchTimesCurrent <= 5)
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
                            locationSearchTimesCurrent++;
                            checkLocationCurrent();
                        }
                    }
                    if (locationSearchTimesCurrent > 5)
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
            return true;
        }

        private bool checkRange(string loc, int temp)
        {
            if ((temp > 99))
            {
                if (store.Contains("tempAlert"))
                {
                    if ((bool)store["tempAlert"])
                    {
                        var toastMessage = new Toast();
                        toastMessage.sendToast(loc, "Temp over 99");
                    }
                }
                temp = 99;
                return true;
            }
            else if (temp < 1)
            {
                if (store.Contains("tempAlert"))
                {
                    if ((bool)store["tempAlert"])
                    {
                        var toastMessage = new Toast();
                        toastMessage.sendToast(loc, "Temp below 1");
                    }
                }
                temp = 1;
                return true;
            }
            return false;
        }
    }
}