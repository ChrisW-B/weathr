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
using Microsoft.Phone.Maps;
using Microsoft.Phone.Maps.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Device.Location;

namespace WeatherLock
{
    public partial class Sat : PhoneApplicationPage
    {
        #region variables
        String latitude = null;
        String longitude = null;
        int locationSearchTimes;
        bool isCurrent;

        dynamic store = IsolatedStorageSettings.ApplicationSettings;
        #endregion

        public Sat()
        {
            InitializeComponent();
            
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            locationSearchTimes = 0;

            base.OnNavigatedTo(e);
            if (this.NavigationContext.QueryString.ContainsKey("isCurrent") && this.NavigationContext.QueryString.ContainsKey("lat") && this.NavigationContext.QueryString.ContainsKey("lon"))
            {
                isCurrent = Convert.ToBoolean(this.NavigationContext.QueryString["isCurrent"]);
                latitude = this.NavigationContext.QueryString["lat"];
                longitude = this.NavigationContext.QueryString["lon"];
                String[] loc = { latitude, longitude };
                store["loc"] = loc;
            }

            setupSat();
        }

        private void setupSat()
        {
            if (latitude != null && longitude != null)
            {
                double lat = Convert.ToDouble(latitude);
                double lon = Convert.ToDouble(longitude);
                map.Center = new GeoCoordinate(lat, lon);
                map.CartographicMode = MapCartographicMode.Road;
                map.ZoomLevel = 5;

                showSatLocation();
                map.Loaded += addSat;
            }
            else
            {
                findLocation();
                setupSat();
            }
        }
        private void addSat(object sender, RoutedEventArgs e)
        {
            MapsSettings.ApplicationContext.ApplicationId = "<applicationid>";
            MapsSettings.ApplicationContext.AuthenticationToken = "<authenticationtoken>";
            TileSource sat = new AnimatedSat();
            map.TileSources.Add(sat);
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
            map.Layers.Add(myLocationLayer);
        }

        //Find the location
        private void findLocation()
        {
            if (isCurrent && locationSearchTimes <= 5)
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
                    String[] loc = store["loc"];
                    latitude = loc[0];
                    longitude = loc[1];
                }
                else
                {
                    latitude = "0";
                    longitude = "0";
                }

            }
        }
    }
}