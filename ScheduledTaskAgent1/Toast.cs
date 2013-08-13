using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace ScheduledTaskAgent1
{
    class Toast
    {
        public void sendToast(string toastTitle, string toastMessage)
        {
            ShellToast toast = new ShellToast();
            toast.Title = toastTitle;
            toast.Content = toastMessage;
            toast.Show();
        }
    }
}
