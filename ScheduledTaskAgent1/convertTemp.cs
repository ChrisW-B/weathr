using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace ScheduledTaskAgent1
{
    class convertTemp
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
            var newToast = new Toast();

            try
            {
                tempDec = Convert.ToDecimal(tempStr);
                this.temp = (int)tempDec;
                if ((temp > 99))
                {
                    if (store.Contains("tempAlert"))
                    {
                        if ((bool)store["tempAlert"])
                        {
                            var toastMessage = new Toast();
                            toastMessage.sendToast("Temp over 99");
                        }
                    }
                    temp = 99;
                }
                else if (temp < 1)
                {
                    if (store.Contains("tempAlert"))
                    {
                        if ((bool)store["tempAlert"])
                        {
                            var toastMessage = new Toast();
                            toastMessage.sendToast("Temp below 1");
                        }
                    }
                    temp = 1;
                }
            }
            catch (FormatException e)
            {
                newToast.sendToast("Not a sequence of Digits");
            }
            catch (OverflowException e)
            {
                newToast.sendToast("Cannot fit into Int32");
            }
        }
    }
}
