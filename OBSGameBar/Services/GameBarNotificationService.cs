using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace OBSGameBar.Services
{
    public class GameBarNotificationService
    {
        public static void ShowNotification(string title, string message)
        {
            try
            {
                var template = ToastTemplateType.ToastText02;
                var xml = ToastNotificationManager.GetTemplateContent(template);

                var textNodes = xml.GetElementsByTagName("text");
                if (textNodes.Length >= 2)
                {
                    textNodes[0].AppendChild(xml.CreateTextNode(title));
                    textNodes[1].AppendChild(xml.CreateTextNode(message));
                }
                else if (textNodes.Length >= 1)
                {
                    textNodes[0].AppendChild(xml.CreateTextNode($"{title}: {message}"));
                }

                var toast = new ToastNotification(xml)
                {
                    ExpirationTime = DateTimeOffset.Now.AddSeconds(4)
                };

                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GameBarNotificationService] Toast notification failed: {ex.Message}");
            }
        }
    }
}
