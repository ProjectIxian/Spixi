
namespace SPIXI.Interfaces
{
    public interface IPushService
    {
        void initialize();
        void setTag(string tag);
        void clearNotifications();
        void showLocalNotification(string title, string message, string data);
    }
}
