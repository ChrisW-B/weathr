using Microsoft.Phone.Maps.Controls;
using System;

namespace WeatherLock
{
    public class CurrentRadar : TileSource
    {
        public const int TILE_SIZE = 256;

        public CurrentRadar()
            : base(@"http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{0}/{1}/{2}.png"){}

        public override Uri GetUri(int tilePositionX, int tilePositionY, int tileLevel)
        {
            int zoom = tileLevel; //SSU tileLevel would be same as zoom in Bing control
            string wmsUrl = string.Format(this.UriFormat, zoom, tilePositionX, tilePositionY);
            return new Uri(wmsUrl);
        }
    }
}
