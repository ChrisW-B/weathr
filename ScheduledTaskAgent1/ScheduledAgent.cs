﻿#define DEBUG_AGENT
using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.Xml.Linq;
using System.Xml;
using System.Collections;
using System.IO;
using Windows.Devices.Geolocation;
using System.Collections.ObjectModel;
using Pinned;



namespace ScheduledTaskAgent1
{
    public class ScheduledAgent : ScheduledTaskAgent
    {

        #region variables
        //Current Conditions


        private int timesRun;
        private int numPins;

        private String defaultCityName;
        private String tempUnit;
        private String url;
        private String locUrl;
        private Boolean isCurrent;
        private String apiKey = "fb1dd3f4321d048d";

        private String latitude;
        private String longitude;

        //save myself a bit of typing
        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        List<Pins> pinnedList = new List<Pins>();
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

        protected override void OnInvoke(ScheduledTask task)
        {
            timesRun = 1;
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.OriginalString == "/")
                {
                    timesRun = 0;
                }
                else
                {
                    String[] uriSplit = tile.NavigationUri.ToString().Split('&');

                    string locationName = uriSplit[0].Split('=')[1];
                    string locationUrl = uriSplit[1].Split('=')[1];
                    bool isCurrentLoc = Convert.ToBoolean(uriSplit[2].Split('=')[1]);
                    pinnedList.Add(new Pins { LocName = locationName, LocUrl = locationUrl, currentLoc = isCurrentLoc });
                }

            }



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
                                defaultCityName = store["defaultLocation"];
                                locUrl = store["defaultUrl"];
                                isCurrent = Convert.ToBoolean(store["defaultCurrent"]);
                            }
                            else
                            {
                                store["defaultLocation"] = "Current Location";
                                store["defaultUrl"] = "null";
                                store["defaultCurrent"] = true;
                                defaultCityName = "Current Location";
                                locUrl = "null";
                                isCurrent = true;
                            }
                            updateDefault();
                        }
                        else
                        {
                            updateOthers();
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
                            defaultCityName = store["defaultLocation"];
                            locUrl = store["defaultUrl"];
                            isCurrent = Convert.ToBoolean(store["defaultCurrent"]);
                        }
                        else
                        {
                            store["defaultLocation"] = "Current Location";
                            store["defaultUrl"] = "null";
                            store["defaultCurrent"] = true;
                            defaultCityName = "Current Location";
                            locUrl = "null";
                            isCurrent = true;
                        }
                        updateDefault();
                    }
                    else
                    {
                        updateOthers();
                    }
                }
            }
        }

        private void updateOthers()
        {
            foreach (Pins pinnedTile in pinnedList)
            {
                String pinnedUrl = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + pinnedTile.LocUrl + ".xml";
                checkUnits();
                var client = new WebClient();

                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(WeatherStringCallback);
                client.DownloadStringAsync(new Uri(pinnedUrl));
            }
        }

        private void updateDefault()
        {
            checkLocation();
            checkUnits();
            var client = new WebClient();

            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(updateMainTile);
            client.DownloadStringAsync(new Uri(url));
        }
        private void WeatherStringCallback(object sender, DownloadStringCompletedEventArgs e)
        {

            if (!e.Cancelled && e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);

                string tempStr = "0";
                string todayHigh = "0";
                string todayLow = "0";
                string tomorrowHigh = "0";
                string tomorrowLow = "0";

                //Current Conditions
                var currentObservation = doc.Element("response").Element("current_observation");
                string city = (string)currentObservation.Element("display_location").Element("city");
                string state = (string)currentObservation.Element("display_location").Element("state_name");
                string cityName = city + ", " + state;
                string weather = (string)currentObservation.Element("weather");

                XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                var today = forecastDays.Element("forecastday");
                var tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                string forecastToday = (string)today.Element("conditions");
                string forecastTomorrow = (string)tomorrow.Element("conditions");
                if (tempUnit == "c")
                {
                    tempStr = (string)currentObservation.Element("temp_c");
                    todayLow = (string)today.Element("low").Element("celsius");
                    todayHigh = (string)today.Element("high").Element("celsius");
                    tomorrowLow = (string)tomorrow.Element("low").Element("celsius");
                    tomorrowHigh = (string)tomorrow.Element("high").Element("celsius");
                }
                else
                {
                    tempStr = (string)currentObservation.Element("temp_f");
                    todayLow = (string)today.Element("low").Element("fahrenheit");
                    todayHigh = (string)today.Element("high").Element("fahrenheit");
                    tomorrowHigh = (string)tomorrow.Element("high").Element("fahrenheit");
                    tomorrowLow = (string)tomorrow.Element("low").Element("fahrenheit");
                }

                //convert temps to ints
                var getTemp = new convertTemp(tempStr);
                int temp = getTemp.temp;
                foreach (ShellTile tile in ShellTile.ActiveTiles)
                {
                    if (tile.NavigationUri.ToString() != "/")
                    {
                        foreach (Pins tileData in pinnedList)
                        {
                            if (tile.NavigationUri.ToString().Split('&')[0].Split('=')[1] == cityName && tileData.LocName == cityName)
                            {
                                IconicTileData TileData = new IconicTileData
                                {
                                    Title = cityName,
                                    Count = temp,
                                    WideContent1 = string.Format("Currently: " + weather + ", " + temp + " degrees"),
                                    WideContent2 = string.Format("Today: " + forecastToday + " " + todayHigh + "/" + todayLow),
                                    WideContent3 = string.Format("Tomorrow: " + forecastTomorrow + " " + tomorrowHigh + "/" + tomorrowLow)

                                };
                                tile.Update(TileData);
                                timesRun++;
                                break;
                            }
                        }
                    }
                }


                //send toast if enabled
                if (store.Contains("notifyMe"))
                {
                    if ((bool)store["notifyMe"] == true)
                    {
                        var newToast = new Toast();
                        newToast.sendToast("I updated!");
                    }
                }

                //backupWeather();

                //save the time of the last time the app was run
                store["lastRun"] = DateTime.Now.ToString();
                store["locName"] = cityName;
                store.Save();

                finish();

            }
            else
            {
                //restoreWeather();
            }
        }

        private void updateMainTile(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);
                string tempStr = "0";
                string todayHigh = "0";
                string todayLow = "0";
                string tomorrowHigh = "0";
                string tomorrowLow = "0";

                //Current Conditions
                var currentObservation = doc.Element("response").Element("current_observation");
                string city = (string)currentObservation.Element("display_location").Element("city");
                string state = (string)currentObservation.Element("display_location").Element("state_name");
                string cityName = city + ", " + state;
                string weather = (string)currentObservation.Element("weather");

                XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");

                var today = forecastDays.Element("forecastday");
                var tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                string forecastToday = (string)today.Element("conditions");
                string forecastTomorrow = (string)tomorrow.Element("conditions");
                if (tempUnit == "c")
                {
                    tempStr = (string)currentObservation.Element("temp_c");
                    todayLow = (string)today.Element("low").Element("celsius");
                    todayHigh = (string)today.Element("high").Element("celsius");
                    tomorrowLow = (string)tomorrow.Element("low").Element("celsius");
                    tomorrowHigh = (string)tomorrow.Element("high").Element("celsius");
                }
                else
                {
                    tempStr = (string)currentObservation.Element("temp_f");
                    todayLow = (string)today.Element("low").Element("fahrenheit");
                    todayHigh = (string)today.Element("high").Element("fahrenheit");
                    tomorrowHigh = (string)tomorrow.Element("high").Element("fahrenheit");
                    tomorrowLow = (string)tomorrow.Element("low").Element("fahrenheit");
                }

                //convert temps to ints
                var getTemp = new convertTemp(tempStr);
                int temp = getTemp.temp;
                foreach (ShellTile tile in ShellTile.ActiveTiles)
                {
                    if (tile.NavigationUri.OriginalString == "/")
                    {
                        if (isCurrent)
                        {
                            IconicTileData TileData = new IconicTileData
                            {
                                Title = cityName,
                                Count = temp,
                                WideContent1 = string.Format("Currently: " + weather + ", " + temp + " degrees"),
                                WideContent2 = string.Format("Today: " + forecastToday + " " + todayHigh + "/" + todayLow),
                                WideContent3 = string.Format("Tomorrow: " + forecastTomorrow + " " + tomorrowHigh + "/" + tomorrowLow)

                            };
                            tile.Update(TileData);
                            timesRun++;

                        }
                        else if (store.Contains("defaultLocation"))
                        {
                            if (store["defaultLocation"] == cityName && store["defaultLocation"] != "Current Location")
                            {
                                IconicTileData TileData = new IconicTileData
                                {
                                    Title = cityName,
                                    Count = temp,
                                    WideContent1 = string.Format("Currently: " + weather + ", " + temp + " degrees"),
                                    WideContent2 = string.Format("Today: " + forecastToday + " " + todayHigh + "/" + todayLow),
                                    WideContent3 = string.Format("Tomorrow: " + forecastTomorrow + " " + tomorrowHigh + "/" + tomorrowLow)

                                };
                                tile.Update(TileData);
                                timesRun++;

                            }
                        }
                    }
                }
                //send toast if enabled
                if (store.Contains("notifyMe"))
                {
                    if ((bool)store["notifyMe"] == true)
                    {
                        var newToast = new Toast();
                        newToast.sendToast("I updated!");
                    }
                }
                //save the time of the last time the app was run
                store["lastRun"] = DateTime.Now.ToString();
                store["locName"] = cityName;
                store.Save();
                finish();
            }
        }
        private void finish()
        {
            if (timesRun > numPins)
            {
                NotifyComplete();
            }
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
            if (isCurrent)
            {
                //get location
                var getLocation = new getLocation();
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
                url = "http://api.wunderground.com/api/" + apiKey + "/conditions/forecast" + locUrl + ".xml";
            }
        }
    }
}