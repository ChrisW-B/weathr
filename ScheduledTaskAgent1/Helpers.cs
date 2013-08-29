using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;
using System.IO.IsolatedStorage;

namespace ScheduledTaskAgent1
{
    public class getLocation
    {
        #region variables
        private string latitude;
        private string longitude;
        #endregion

        #region getters
        public string getLat()
        {
            return latitude;
        }
        public string getLong()
        {
            return longitude;
        }
        #endregion

        public getLocation()
        {
            getInfo();
        }

        private void getInfo()
        {

            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
            watcher.MovementThreshold = 1000;

            watcher.Start();

            GeoCoordinate coord = watcher.Position.Location;

            if (coord.IsUnknown != true)
            {
                latitude = coord.Latitude.ToString();
                longitude = coord.Longitude.ToString();
            }
            else
            {
                Console.WriteLine("Unknown latitude and longitude.");
            }
        }
    }

    public class convertTemp
    {
        public int temp;
        dynamic store = IsolatedStorageSettings.ApplicationSettings;
        public convertTemp(string tempStr)
        {
            tempCon(tempStr);
        }

        private void tempCon(string tempStr)
        {
            //convert temp into integer
            decimal tempDec;

            tempDec = Convert.ToDecimal(tempStr);
            this.temp = (int)tempDec;
        }
    }

    public class updateTile
    {
        public updateTile(
            string cityName,
            int temp,
            string conditions,
            string todayHigh,
            string todayLow,
            string forecastToday,
            string forecastTomorrow,
            string tomorrowHigh,
            string tomorrowLow
            )
        {
            update(cityName, temp, conditions, todayHigh, todayLow, forecastToday, forecastTomorrow, tomorrowHigh, tomorrowLow);
        }
        private void update(
            string cityName,
            int temp,
            string conditions,
            string high,
            string low,
            string forecastToday,
            string forecastTomorrow,
            string forecastHigh,
            string forecastLow
            )
        {

            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault();

            IconicTileData TileData = new IconicTileData
            {
                Title = cityName,
                Count = temp,
                WideContent1 = string.Format("Currently: " + conditions + ", " + temp + " degrees"),
                WideContent2 = string.Format("Today: " + forecastToday + " " + high + "/" + low),
                WideContent3 = string.Format("Tomorrow: " + forecastTomorrow + " " + forecastHigh + "/" + forecastLow)

            };
            tile.Update(TileData);
        }
    }
}
