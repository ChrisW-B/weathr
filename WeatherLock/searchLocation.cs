using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Xml.Linq;

namespace WeatherLock
{
    internal class searchLocation : INotifyPropertyChanged
    {
        #region variables

        //Current Conditions
        private String cityName;

        private String wuUrl;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion variables

        #region getters/setters

        //Current Conditions
        public String CityName
        {
            get
            {
                return cityName;
            }
            set
            {
                if (value != cityName)
                {
                    cityName = value;
                    NotifyPropertyChanged("CityName");
                }
            }
        }

        public string WUUrl
        {
            get
            {
                return wuUrl;
            }
            set
            {
                if (value != wuUrl)
                {
                    wuUrl = value;
                    NotifyPropertyChanged("WUUrl");
                }
            }
        }

        #endregion getters/setters

        #region private helpers

        //Rais the property changed event and pass along the property that changed

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion private helpers

        #region constructors

        public searchLocation()
        {
        }

        #endregion constructors

        public void getLocations(string location)
        {
            //form the uri
            string url = "http://autocomplete.wunderground.com/aq?query=" + location + "&format=xml";
            Uri uri = new Uri(url);

            //initialize a webrequest
            HttpWebRequest locationRequest = (HttpWebRequest)WebRequest.Create(uri);

            //set up the state object for the async request
            LocationUpdateState locationState = new LocationUpdateState();
            locationState.AsyncRequest = locationRequest;

            //start the async request
            locationRequest.BeginGetResponse(new AsyncCallback(HandleLocationResponse), locationState);
        }

        //handle the info returned from the async request
        private void HandleLocationResponse(IAsyncResult asyncResult)
        {
            //get the state information
            LocationUpdateState locationState = (LocationUpdateState)asyncResult.AsyncState;
            HttpWebRequest locationRequest = (HttpWebRequest)locationState.AsyncRequest;

            //end the async request
            locationState.AsyncResponse = (HttpWebResponse)locationRequest.EndGetResponse(asyncResult);

            Stream streamResult;

            string newCityName = "";
            string newWUUrl = "";

            try
            {
                //get the stream containing the response from the async call
                streamResult = locationState.AsyncResponse.GetResponseStream();

                //load the xml
                XDocument xmlLocation = XDocument.Load(streamResult);

                //Start parsing the XML
                XElement xmlResults = xmlLocation.Element("RESULTS");

                newCityName = (string)xmlResults.Element("name");
                newWUUrl = (string)xmlResults.Element("l");

                //copy the data over
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //copy forecast object over
                    cityName = newCityName;
                    wuUrl = newWUUrl;
                });
            }
            catch (FormatException)
            {
                //handle errors in formatting
            }
        }

        public class LocationUpdateState
        {
            public HttpWebRequest AsyncRequest { get; set; }

            public HttpWebResponse AsyncResponse { get; set; }
        }
    }
}