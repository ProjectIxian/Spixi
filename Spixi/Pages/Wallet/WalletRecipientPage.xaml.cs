﻿using SPIXI.Interfaces;
using System;
using System.Web;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WalletRecipientPage : SpixiContentPage
	{
        public event EventHandler<SPIXI.EventArgs<string>> pickSucceeded;

        public WalletRecipientPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "wallet_recipient.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            loadContacts();
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (onNavigatingGlobal(current_url))
            {
                e.Cancel = true;
                return;
            }

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopModalAsync();
            }
            else if (current_url.Contains("ixian:select:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:select:|" }, StringSplitOptions.None);
                string id = split[1];
                onPickSucceeded(id);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void onPickSucceeded(string id)
        {
            if (pickSucceeded != null)
            {
                pickSucceeded(this, new SPIXI.EventArgs<string>(id));
            }
        }

        public void loadContacts()
        {
            Utils.sendUiCommand(webView, "clearContacts");

            foreach (Friend friend in FriendList.friends)
            {
                string str_online = "false";
                if (friend.online)
                    str_online = "true";

                Utils.sendUiCommand(webView, "addContact", friend.walletAddress.ToString(), friend.nickname, "img/spixiavatar.png", str_online);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopModalAsync();

            return true;
        }
    }
}