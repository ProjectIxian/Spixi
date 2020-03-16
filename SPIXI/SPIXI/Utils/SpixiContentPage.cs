using SPIXI.Interfaces;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SPIXI
{
	public class SpixiContentPage : ContentPage
	{
        public bool CancelsTouchesInView = true;

        public virtual void recalculateLayout()
        {

        }

        public Task displaySpixiAlert(string title, string message, string cancel)
        {
            ISystemAlert alert = DependencyService.Get<ISystemAlert>();
            if (alert != null)
            {
                alert.displayAlert(title, message, cancel);
                return null;
            }

            return DisplayAlert(title, message, cancel);
        }


        public virtual void updateScreen()
        {

        }

        public virtual void onResume()
        {

        }

    }
}
 