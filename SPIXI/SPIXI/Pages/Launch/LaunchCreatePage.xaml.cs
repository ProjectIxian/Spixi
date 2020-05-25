using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

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
                displaySpixiAlert("SPIXI Account", "Please type your nickname.", "OK");
            }
            else if (current_url.Equals("ixian:avatar", StringComparison.Ordinal))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                onChangeAvatarAsync(sender, e);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
            var picker_service = DependencyService.Get<IPicturePicker>();

            SpixiImageData spixi_img_data = await picker_service.PickImageAsync();
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
                    image_bytes = picker_service.ResizeImage(ms.ToArray(), 960, 960);
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
                await displaySpixiAlert("Error", ex.ToString(), "ok");
                return;
            }

            Utils.sendUiCommand(webView, "loadAvatar", file_path);
        }

        public void onCreateAccount(string nick, string pass)
        {
            // Generate the account on a different thread
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // Aquire the wake lock
                bool wake_lock_sd = DependencyService.Get<IPowerManager>().AquireLock("screenDim");
                bool wake_lock_p = DependencyService.Get<IPowerManager>().AquireLock("partial");

                if (Node.generateWallet(pass))
                {
                    Node.start();

                    Node.localStorage.nickname = nick;
                    Node.localStorage.writeAccountFile();

                    // TODO: encrypt the password
                    Application.Current.Properties["walletpass"] = pass;
                    Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF

                    // Release the wake lock
                    if (wake_lock_sd)
                        DependencyService.Get<IPowerManager>().ReleaseLock("screenDim");
                    if (wake_lock_p)
                        DependencyService.Get<IPowerManager>().ReleaseLock("partial");

                    Device.BeginInvokeOnMainThread(() => {
                        Navigation.PushAsync(HomePage.Instance(), Config.defaultXamarinAnimations);
                        Navigation.RemovePage(this);
                    });

                    Friend friend = FriendList.addFriend(Base58Check.Base58CheckEncoding.DecodePlain("419jmKRKVFcsjmwpDF1XSZ7j1fez6KWaekpiawHvrpyZ8TPVmH1v6bhT2wFc1uddV"), null, "Spixi Group Chat", null, null, 0);

                    FriendList.saveToStorage();

                    StreamProcessor.sendContactRequest(friend);
                }
                else
                {
                    // Release the wake lock
                    if (wake_lock_sd)
                        DependencyService.Get<IPowerManager>().ReleaseLock("screenDim");
                    if (wake_lock_p)
                        DependencyService.Get<IPowerManager>().ReleaseLock("partial");

                    Device.BeginInvokeOnMainThread(() => {
                        displaySpixiAlert("Error", "Cannot generate new wallet. Please try again.", "Ok");
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