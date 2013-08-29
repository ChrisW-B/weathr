using System;
using System.Windows.Threading;

namespace WeatherLock
{
    class Clock
    {
        private DispatcherTimer dispatcherTimer;
        private MainPage mainPage;

        public Clock(MainPage mainPage)
        {
            this.mainPage = mainPage;

            this.dispatcherTimer = new DispatcherTimer();
            this.dispatcherTimer.Tick += dispatcherTimer_Tick;
            this.dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            this.dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            TimeSpan currentTime = now.TimeOfDay;

            // Update clock.
            this.mainPage.time.Text = GetFormattedDateTimeString(now);          
        }

        private string GetFormattedDateTimeString(DateTime dateTime)
        {
            return dateTime.ToString("h:mm");            
        }
    }
}
