using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace WeatherLock
{

    class getDataMain
    {
        #region variables

        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        //Current Conditions
        private String cityName;
        private String tempC;
        private String tempF;
        private String weather;
        private int tempInt;

        //Forecast Conditions
        private String minC;
        private String maxC;
        private String minF;
        private String maxF;
        private String forecastToday;
        private String forecastTomorrow;
        private String minCTomorrow;
        private String maxCTomorrow;
        private String minFTomorrow;
        private String maxFTomorrow;
        private String todayHigh;
        private String todayLow;
        private String tomorrowHigh;
        private String tomorrowLow;

        private String tempUnit;

        #endregion

        #region getters/setters
        //Current Conditions
        public string getCityName()
        {
            return cityName;
        }
        public string getTempC()
        {
            return tempC;
        }
        public string getTempF()
        {
            return tempF;
        }
        public string getWeather()
        {
            return weather;
        }

        //Forecast Conditions
        public string getMinC()
        {
            return minC;
        }
        public string getMaxC()
        {
            return maxC;
        }
        public string getMinF()
        {
            return minF;
        }
        public string getMaxF()
        {
            return maxF;
        }
        public string getForecastToday()
        {
            return forecastToday;
        }
        public string getForecastTomorrow()
        {
            return forecastTomorrow;
        }
        public string getMaxFTomorrow()
        {
            return maxFTomorrow;
        }
        public string getMinFTomorrow()
        {
            return minFTomorrow;
        }
        public string getMinCTomorrow()
        {
            return minCTomorrow;
        }
        public string getMaxCTomorrow()
        {
            return maxCTomorrow;
        }


        #endregion


        public getDataMain(string url, string tempUnit)
        {
            this.tempUnit = tempUnit;
            getData(url);
        }

        private void getData(string url)
        {
            var client = new WebClient();

            Uri uri = new Uri(url);

            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(StringCallback);
            client.DownloadStringAsync(uri);
        }


        private void StringCallback(object sender, DownloadStringCompletedEventArgs e)
        {

            if (!e.Cancelled && e.Error == null)
            {
                String tempStr = null;

                XDocument doc = XDocument.Parse(e.Result);
                var currentObservation = doc.Element("response").Element("current_observation");
                this.cityName = (string)currentObservation.Element("display_location").Element("full");
                this.tempC = (string)currentObservation.Element("temp_c");
                this.tempF = (string)currentObservation.Element("temp_f");
                this.weather = (string)currentObservation.Element("weather");


                XElement forecastDays = doc.Element("response").Element("forecast").Element("simpleforecast").Element("forecastdays");
                var today = forecastDays.Element("forecastday");
                this.forecastToday = (string)today.Element("conditions");
                this.minC = (string)today.Element("low").Element("celsius");
                this.maxC = (string)today.Element("high").Element("celsius");
                this.minF = (string)today.Element("low").Element("fahrenheit");
                this.maxF = (string)today.Element("high").Element("fahrenheit");

                var tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();
                this.forecastTomorrow = (string)tomorrow.Element("conditions");
                this.minCTomorrow = (string)tomorrow.Element("low").Element("celsius");
                this.maxCTomorrow = (string)tomorrow.Element("high").Element("celsius");
                this.maxFTomorrow = (string)tomorrow.Element("high").Element("fahrenheit");
                this.minFTomorrow = (string)tomorrow.Element("low").Element("fahrenheit");

                if (tempUnit == "c")
                {
                    todayHigh = maxC;
                    todayLow = minC;
                    tomorrowHigh = maxCTomorrow;
                    tomorrowLow = minCTomorrow;

                }
                else
                {
                    todayHigh = maxF;
                    todayLow = minF;
                    tomorrowHigh = maxFTomorrow;
                    tomorrowLow = minFTomorrow;
                }


                String[] backupResavedSettings = new String[9];

                if ((bool)store.Contains("backup"))
                {
                    backupResavedSettings = (String[])store["backup"];

                    if (cityName == null)
                    {
                        cityName = backupResavedSettings[0];
                    }
                    if (tempStr == null)
                    {
                        tempStr = backupResavedSettings[1];
                    }
                    if (weather == null)
                    {
                        weather = backupResavedSettings[2];
                    }
                    if (todayHigh == null)
                    {
                        todayHigh = backupResavedSettings[3];
                    }
                    if (todayLow == null)
                    {
                        todayLow = backupResavedSettings[4];
                    }
                    if (forecastToday == null)
                    {
                        forecastToday = backupResavedSettings[5];
                    }
                    if (forecastTomorrow == null)
                    {
                        forecastTomorrow = backupResavedSettings[6];
                    }
                    if (tomorrowHigh == null)
                    {
                        tomorrowHigh = backupResavedSettings[7];
                    }
                    if (tomorrowLow == null)
                    {
                        tomorrowLow = backupResavedSettings[8];
                    }
                }
                else
                {
                    String[] backup = { cityName, tempStr, weather, todayHigh, todayLow, forecastToday, forecastTomorrow, tomorrowHigh, tomorrowLow };
                    store["backup"] = backup;
                }

                if (tempUnit == "c")
                {
                    tempStr = tempC;
                }
                else
                {
                    tempStr = tempF;
                }

                var getTemp = new convertTempMain(tempStr);
                this.tempInt = getTemp.temp;

                var updateTile = new updateTileMain(cityName, tempInt, weather, todayHigh, todayLow, forecastToday, forecastTomorrow, tomorrowHigh, tomorrowLow);
                store["locName"] = cityName;
            }
        }

    }
}
