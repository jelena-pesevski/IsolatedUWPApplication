using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace OposZadaci2._2020
{
    class NotificationManager
    {
        public static readonly SemaphoreSlim notificationSemaphore = new SemaphoreSlim(1);

        public static async Task NotifyUser(string text, Job job)
        {
            const int maxLengthFilename = 100;

            await notificationSemaphore.WaitAsync();
            try
            {
                ToastContentBuilder toastContentBuilder = new ToastContentBuilder();
                toastContentBuilder.AddText(text, AdaptiveTextStyle.Title);
                toastContentBuilder.AddText(job.Filename.Shorten(maxLengthFilename, fromEnd: true));
        
                ToastContent content = toastContentBuilder.GetToastContent();
                ToastNotification notification = new ToastNotification(content.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
            catch
            { }
            finally
            {
                notificationSemaphore.Release();
            }
        }

        public static async Task NotifyUserFromBackground(string text)
        {
            await notificationSemaphore.WaitAsync();
            try
            {
                ToastContentBuilder toastContentBuilder = new ToastContentBuilder();
                toastContentBuilder.AddText(text, AdaptiveTextStyle.Title);
              
                ToastContent content = toastContentBuilder.GetToastContent();
                ToastNotification notification = new ToastNotification(content.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
            catch
            { }
            finally
            {
                notificationSemaphore.Release();
            }
        }
    }
}
