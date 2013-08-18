﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Maps.Controls;
using System.Text;

namespace WeatherLock
{
    public class AnimatedRadar : TileSource
    {
        public AnimatedRadar()
            : base(@"http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{0}/{1}/{2}.png") { }

        public override Uri GetUri(int tilePositionX, int tilePositionY, int tileLevel)
        {
            int zoom = tileLevel;
            string wmsUrl = string.Format(this.UriFormat, zoom, tilePositionX, tilePositionY);
            return new Uri(wmsUrl);
        }
    }
}