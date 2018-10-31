using DLT;
using DLT.Meta;
using DLT.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ContactsPage : SpixiContentPage
	{
		public ContactsPage ()
		{
			InitializeComponent ();
            Title = "Contacts";
            loadContacts();

        }

        public void loadContacts()
        {
            FriendList.refreshList();
            lst.ItemsSource = FriendList.friends;
        }

        private void lstRefreshing(object sender, EventArgs e)
        {
            loadContacts();
            lst.IsRefreshing = false;
        }

        private void lstItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null)
            {
                Friend contact = (Friend)e.Item;
                Navigation.PushAsync(new SingleChatPage(contact));

                lst.SelectedItem = null;
            }
        }
    }


}