using Microsoft.Phone.Shell;

namespace ScheduledTaskAgent1
{
    internal class Toast
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