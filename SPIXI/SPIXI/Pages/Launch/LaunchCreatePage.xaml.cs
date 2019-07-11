using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/intro_new.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

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
                DisplayAlert("SPIXI Account", "Please type your nickname.", "OK");
            }
            else if (current_url.Equals("ixian:avatar", StringComparison.Ordinal))
            {
                onChangeAvatarAsync(sender, e);
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
            Stream stream = await DependencyService.Get<IPicturePicker>().GetImageStreamAsync();

            if (stream != null)
            {
                Image image = new Image
                {
                    Source = ImageSource.FromStream(() => stream),
                    BackgroundColor = Color.Gray
                };

                var filePath = Node.localStorage.getOwnAvatarPath();

                //DisplayAlert("Alert", filePath, "OK");

                FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                stream.CopyTo(fs);
                /*using (FileStream file = new FileStream(filePath, FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    file.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }*/
                Utils.sendUiCommand(webView, "loadAvatar", filePath);
                stream.Close();
                fs.Close();

                /*
                var filename = System.IO.Path.Combine(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).ToString(), "NewFolder");
                filename = System.IO.Path.Combine(filename, "filename.jpg");
                using (var fileOutputStream = new FileOutputStream(filename))
                {
                    await fileOutputStream.WriteAsync(reducedImage);
                }*/

            }

        }

        public void onCreateAccount(string nick, string pass)
        {
            // Generate the account on a different thread
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // Aquire the wake lock
                bool wake_lock = DependencyService.Get<IPowerManager>().AquireLock();

                if (Node.generateWallet(pass))
                {
                    Node.start();

                    Node.localStorage.nickname = nick;
                    Node.localStorage.writeAccountFile();

                    // TODO: encrypt the password
                    Application.Current.Properties["walletpass"] = pass;
                    Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF

                    // Release the wake lock
                    if (wake_lock)
                        DependencyService.Get<IPowerManager>().ReleaseLock();

                    Device.BeginInvokeOnMainThread(() => {
                        Navigation.PushAsync(new HomePage(), Config.defaultXamarinAnimations);
                        Navigation.RemovePage(this);
                    });
                }
                else
                {
                    // Release the wake lock
                    if (wake_lock)
                        DependencyService.Get<IPowerManager>().ReleaseLock();

                    Device.BeginInvokeOnMainThread(() => {
                        DisplayAlert("Error", "Cannot generate new wallet. Please try again.", "Ok");
                    });
                    return;
                }
            }).Start();
        }

    }
}