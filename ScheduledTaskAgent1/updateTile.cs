using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScheduledTaskAgent1
{
    class updateTile
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
            ) {
           
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
