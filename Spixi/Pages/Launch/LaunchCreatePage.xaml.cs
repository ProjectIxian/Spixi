﻿using Spixi;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LaunchCreatePage : SpixiContentPage
	{
		public LaunchCreatePage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "intro_new.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Contains("ixian:create:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:create:" }, StringSplitOptions.None);
                // Extract the nickname
                string[] split2 = split[1].Split(new string[] { ":" }, StringSplitOptions.None);
                string nick = split2[0];

                // All the remaining text, including seperator chars are part of the password
                string pass = split[1].Replace(nick+":","");

                // Create the account
                onCreateAccount(nick, pass);              
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                displaySpixiAlert(SpixiLocalization._SL("intro-new-emptynick-title"), SpixiLocalization._SL("intro-new-emptynick-text"), SpixiLocalization._SL("global -dialog-ok"));
            }
            else if (current_url.Equals("ixian:avatar", StringComparison.Ordinal))
            {
                _ = onChangeAvatarAsync(sender, e);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }


        public async Task onChangeAvatarAsync(object sender, EventArgs e)
        {
            SpixiImageData spixi_img_data = await SFilePicker.PickImageAsync();
            Stream stream = spixi_img_data.stream;

            if (stream == null)
            {
                return;
            }

            var file_path = Node.localStorage.getOwnAvatarPath(false);
            try
            {
                byte[] image_bytes = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    stream.Close();
                    image_bytes = SFilePicker.ResizeImage(ms.ToArray(), 960, 960, 80);
                    if(image_bytes == null)
                    {
                        return;
                    }
                }

                FileStream fs = new FileStream(file_path, FileMode.OpenOrCreate, FileAccess.Write);
                fs.Write(image_bytes, 0, image_bytes.Length);
                fs.Close();
            }
            catch (Exception ex)
            {
                await displaySpixiAlert(SpixiLocalization._SL("intro-new-avatarerror-title"), ex.ToString(), SpixiLocalization._SL("global-dialog-ok"));
                return;
            }

            Utils.sendUiCommand(this, "loadAvatar", file_path);
        }

        public void onCreateAccount(string nick, string pass)
        {
            // Generate the account on a different thread
            new Thread(() =>
            {
                // Aquire the wake lock
                bool wake_lock_sd = SPowerManager.AquireLock("screenDim");
                bool wake_lock_p = SPowerManager.AquireLock("partial");

                if (Node.generateWallet(pass))
                {
                    Node.generatedNewWallet = true;

                    Node.start();

                    Node.localStorage.nickname = nick;
                    Node.localStorage.writeAccountFile();

                    // TODO: encrypt the password
                    Preferences.Default.Set("walletpass", pass);

                    // Release the wake lock
                    if (wake_lock_sd)
                        SPowerManager.ReleaseLock("screenDim");
                    if (wake_lock_p)
                        SPowerManager.ReleaseLock("partial");

                    MainThread.BeginInvokeOnMainThread(() => {
                        Navigation.PushAsync(HomePage.Instance(true), Config.defaultXamarinAnimations);
                        Navigation.RemovePage(this);
                    });
                }
                else
                {
                    // Release the wake lock
                    if (wake_lock_sd)
                        SPowerManager.ReleaseLock("screenDim");
                    if (wake_lock_p)
                        SPowerManager.ReleaseLock("partial");

                    MainThread.BeginInvokeOnMainThread(() => {
                        displaySpixiAlert(SpixiLocalization._SL("intro-new-walleterror-title"), SpixiLocalization._SL("intro-new-walleterror-text"), SpixiLocalization._SL("global-dialog-ok"));
                    });
                    return;
                }
            }).Start();
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}