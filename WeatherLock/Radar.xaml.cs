using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Phone.Maps;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Threading;

namespace WeatherLock
{
    public partial class Radar : PhoneApplicationPage
    {
        #region variables
        String latitude = null;
        String longitude = null;
        int locationSearchTimes;
        int age;
        bool isCurrent;

        dynamic store = IsolatedStorageSettings.ApplicationSettings;
        List<RadarCache> radarHistory = new List<RadarCache>();

        #endregion
        public Radar()
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
            age = 0;
            setupRadar();
        }

        private void setupRadar()
        {
            if (latitude != null && longitude != null)
            {

                double lat = Convert.ToDouble(latitude);
                double lon = Convert.ToDouble(longitude);
                map.Center = new GeoCoordinate(lat, lon);
                map.CartographicMode = MapCartographicMode.Road;
                map.ZoomLevel = 7;

                //map.Tap += map_Tap;

                showRadarLocation();
                map.Loaded += addRadar;
            }
            else
            {
                findLocation();
                setupRadar();
            }
        }

        void map_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //animate();
        }
        void addRadar(object sender, RoutedEventArgs e)
        {
            MapsSettings.ApplicationContext.ApplicationId = "<applicationid>";
            MapsSettings.ApplicationContext.AuthenticationToken = "<authenticationtoken>";


            RadarCache radarCache = new RadarCache();
            for (int i = 0; i < 51; i = i + 5)
            {
                TileSource radar = new AnimatedRadar();
                string radarAge;
                if (i == 0)
                {
                    radarAge = "nexrad-n0q-900913";
                }
                else if (i == 5)
                {
                    radarAge = "nexrad-n0q-900913-m05m";
                }
                else
                {
                    radarAge = "nexrad-n0q-900913-m" + i + "m";
                }
                radar.UriFormat = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/" + radarAge + "/{0}/{1}/{2}.png";

                switch (i)
                {
                    case 0:
                        radarCache.current = radar;
                        break;
                    case 5:
                        radarCache.five = radar;
                        break;
                    case 10:
                        radarCache.ten = radar;
                        break;
                    case 15:
                        radarCache.fifteen = radar;
                        break;
                    case 20:
                        radarCache.twenty = radar;
                        break;
                    case 25:
                        radarCache.twentyfive = radar;
                        break;
                    case 30:
                        radarCache.thirty = radar;
                        break;
                    case 35:
                        radarCache.thirtyfive = radar;
                        break;
                    case 40:
                        radarCache.forty = radar;
                        break;
                    case 45:
                        radarCache.fortyfive = radar;
                        break;
                    case 50:
                        radarCache.fifty = radar;
                        break;
                }
            }
            radarHistory.Add(radarCache);
            map.TileSources.Add(radarCache.current);
        }

        private void animate()
        {
            foreach (RadarCache radarCache in radarHistory)
            {
                
               // map.TileSources.Remove(map.TileSources.Last());

                TileSource radar = new AnimatedRadar();
                switch (age)
                {
                    case 0:
                        radar = radarCache.current;
                        break;
                    case 5:
                        radar = radarCache.five;
                        break;
                    case 10:
                        radar = radarCache.ten;
                        break;
                    case 15:
                        radar = radarCache.fifteen;
                        break;
                    case 20:
                        radar = radarCache.twenty;
                        break;
                    case 25:
                        radar = radarCache.twentyfive;
                        break;
                    case 30:
                        radar = radarCache.thirty;
                        break;
                    case 35:
                        radar = radarCache.thirtyfive;
                        break;
                    case 40:
                        radar = radarCache.forty;
                        break;
                    case 45:
                        radar = radarCache.fortyfive;
                        break;
                    case 50:
                        radar = radarCache.fifty;
                        age = 0;
                        break;
                }
                map.TileSources.Add(radar);
                age = +5;
                break;
            }
            //System.Threading.Thread.Sleep(500);
            animate();
        }




        public class RadarCache
        {
            public TileSource current { get; set; }
            public TileSource five { get; set; }
            public TileSource ten { get; set; }
            public TileSource fifteen { get; set; }
            public TileSource twenty { get; set; }
            public TileSource twentyfive { get; set; }
            public TileSource thirty { get; set; }
            public TileSource thirtyfive { get; set; }
            public TileSource forty { get; set; }
            public TileSource fortyfive { get; set; }
            public TileSource fifty { get; set; }

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