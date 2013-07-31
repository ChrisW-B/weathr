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



namespace ScheduledTaskAgent1
{
    public class ScheduledAgent : ScheduledTaskAgent
    {

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
        private String apiKey = "fb1dd3f4321d048d";

        private String latitude;
        private String longitude;

        //save myself a bit of typing
        dynamic store = IsolatedStorageSettings.ApplicationSettings;
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
                    updateData();
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
                updateData();
            }
        }

        private void updateData()
        {
            checkLocation();
            checkUnits();
            getWeatherData(); 
        }

        private void getWeatherData()
        {
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
                var getTemp = new convertTemp(tempStr);
                int temp = getTemp.temp;

                //update the tile and lockscreen
                var updateTile = new updateTile(cityName, temp, weather, todayHigh, todayLow, forecastToday, forecastTomorrow, tomorrowHigh, tomorrowLow);

                //send toast if enabled
                if (store.Contains("notifyMe"))
                {
                    if ((bool)store["notifyMe"] == true)
                    {
                        var newToast = new Toast();
                        newToast.sendToast("I updated!");
                    }
                }

                backupWeather();

                //save the time of the last time the app was run
                store["lastRun"] = DateTime.Now.ToString();
                store["locName"] = cityName;
                store.Save();

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
            if (store.Contains("LocationConsent"))
            {
                if ((bool)store["LocationConsent"])
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
    }
}