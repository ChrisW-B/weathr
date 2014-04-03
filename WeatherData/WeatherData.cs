using System.Collections.ObjectModel;

namespace WeatherData
{
    public class WeatherInfo
    {
        public string error { get; set; }

        public string rand { get; set; }

        public string city { get; set; }

        public string state { get; set; }

        public string shortCityName { get; set; }

        public string currentConditions { get; set; }

        public string windSpeedM { get; set; }

        public string windSpeedK { get; set; }

        public string windDir { get; set; }

        public string tempC { get; set; }

        public string tempF { get; set; }

        public string feelsLikeC { get; set; }

        public string feelsLikeF { get; set; }

        public string humidity { get; set; }

        public string todayHighC { get; set; }

        public string todayHighF { get; set; }

        public string todayLowC { get; set; }

        public string todayLowF { get; set; }

        public string tomorrowHighC { get; set; }

        public string tomorrowHighF { get; set; }

        public string tomorrowLowC { get; set; }

        public string tomorrowLowF { get; set; }

        public ObservableCollection<ForecastC> forecastC { get; set; }

        public ObservableCollection<ForecastF> forecastF { get; set; }

        public int todayHighIntC { get; set; }

        public int tomorrowHighIntC { get; set; }

        public int todayHighIntF { get; set; }

        public int tomorrowHighIntF { get; set; }

        public string tempCompareF { get; set; }

        public string tempCompareC { get; set; }

        public string todayShort { get; set; }

        public string tomorrowShort { get; set; }
    }

    public class ForecastC
    {
        public string title { get; set; }

        public string text { get; set; }

        public string pop { get; set; }
    }

    public class ForecastF
    {
        public string text { get; set; }

        public string title { get; set; }

        public string pop { get; set; }
    }
}