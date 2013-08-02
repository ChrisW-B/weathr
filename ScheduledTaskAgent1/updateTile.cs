using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScheduledTaskAgent1
{
    
    class updateTile
    {
        private int x;

        public updateTile()
        {
            x = 0;

        }
        public void update(
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



            ShellTile tile = ShellTile.ActiveTiles.ElementAt(x);
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
