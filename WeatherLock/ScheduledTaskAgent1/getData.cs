using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace ScheduledTaskAgent1
{

    class getData
    {
        #region variables
        //Current Conditions
        private String cityName;
        private String tempC;
        private String tempF;
        private String weather;

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

        //Flags
        private bool currentComplete;
        private bool forecastComplete;
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

        //Flags
        public bool getCurrentComplete()
        {
            return currentComplete;
        }
        public bool getForecastComplete()
        {
            return forecastComplete;
        }
        #endregion


        public getData(string current, string forecast)
        {
            this.currentComplete = false;
            this.forecastComplete = false;
            getCurrentData(current);
            getForecastData(forecast);
        }

        private void getCurrentData(string url)
        {
            var client = new WebClient();
            Uri uri = new Uri(url);

            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(CurrentStringCallback);
            client.DownloadStringAsync(uri);
        }
        private void getForecastData(string url)
        {
            var client = new WebClient();
            Uri uri = new Uri(url);

            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(ForecastStringCallback);
            client.DownloadStringAsync(uri);
        }

        private void CurrentStringCallback(object sender, DownloadStringCompletedEventArgs e)
        { 
            if (!e.Cancelled && e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);
                var currentObservation = doc.Element("response").Element("current_observation");
                this.cityName = (string)currentObservation.Element("display_location").Element("full");
                this.tempC = (string)currentObservation.Element("temp_c");
                this.tempF = (string)currentObservation.Element("temp_f");
                this.weather = (string)currentObservation.Element("weather");

                this.currentComplete = true;
            }
        }
        private void ForecastStringCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);
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

                this.forecastComplete = true;
            }
        }
    }
}
