using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeatherLock
{
    class convertTempMain
    {
        public int temp;
        public convertTempMain(string tempStr)
        {
            tempCon(tempStr);
        }
        private void tempCon(string tempStr)
        {
            //convert temp into integer
            decimal tempDec;
           

            try
            {
                tempDec = Convert.ToDecimal(tempStr);
                this.temp = (int)tempDec;
               
            }
            catch (FormatException e)
            {
                
            }
            catch (OverflowException e)
            {
                
            }
        }
    }
}
