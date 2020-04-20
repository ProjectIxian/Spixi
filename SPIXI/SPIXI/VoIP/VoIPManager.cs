using IXICore.Meta;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace SPIXI.VoIP
{

    public class VoIPManager
    {
        public static byte[] currentCallSessionId { get; private set; }
        public static Friend currentCallContact { get; private set; }
        public static bool currentCallAccepted { get; private set; }
        public static bool currentCallCalleeAccepted { get; private set; }
        public static string currentCallCodec { get; private set; }

        static IAudioPlayer audioPlayer = null;
        static IAudioRecorder audioRecorder = null;


        public static bool isInitiated()
        {
            if(currentCallSessionId != null)
            {
                return true;
            }
            return false;
        }

        public static void initiateCall(Friend friend)
        {
            if (currentCallSessionId != null)
            {
                return;
            }

            currentCallSessionId = Guid.NewGuid().ToByteArray();
            currentCallContact = friend;
            currentCallCalleeAccepted = false;
            currentCallAccepted = true;
            currentCallCodec = null;

            string codecs = String.Join("|", DependencyService.Get<ISpixiCodecs>().getSupportedAudioCodecs());

            StreamProcessor.sendAppRequest(friend, "spixi.voip", currentCallSessionId, Encoding.UTF8.GetBytes(codecs));
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).displayCallBar(currentCallSessionId, "Calling " + friend.nickname + "...");
        }

        public static void onReceivedCall(Friend friend, byte[] session_id, byte[] data)
        {
            if (currentCallSessionId != null)
            {
                StreamProcessor.sendAppRequestReject(friend, session_id);
                return;
            }

            currentCallSessionId = session_id;
            currentCallContact = friend;
            currentCallCalleeAccepted = true;
            currentCallAccepted = false;
            currentCallCodec = null;

            var codec_service = DependencyService.Get<ISpixiCodecs>();

            string codecs_str = Encoding.UTF8.GetString(data);

            string[] codecs = codecs_str.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var codec in codecs)
            {
                if (codec_service.isCodecSupported(codec))
                {
                    currentCallCodec = codec;
                    break;
                }
            }
            if (currentCallCodec == null)
            {
                Logging.error("Unsupported audio codecs: " + codecs_str);
                endVoIPSession();
                return;
            }
        }

        private static void startVoIPSession()
        {
            DependencyService.Get<IPowerManager>().AquireLock("partial");
            DependencyService.Get<IPowerManager>().AquireLock("wifi");

            audioPlayer = DependencyService.Get<IAudioPlayer>(DependencyFetchTarget.NewInstance);
            audioPlayer.start();

            audioRecorder = DependencyService.Get<IAudioRecorder>(DependencyFetchTarget.NewInstance);
            audioRecorder.start();
            audioRecorder.setOnSoundDataReceived((data) => {
                StreamProcessor.sendAppData(currentCallContact, currentCallSessionId, data);
            });
        }

        private static void endVoIPSession()
        {
            DependencyService.Get<IPowerManager>().ReleaseLock("partial");
            DependencyService.Get<IPowerManager>().ReleaseLock("wifi");

            if (audioPlayer != null)
            {
                audioPlayer.Dispose();
                audioPlayer = null;
            }
            if (audioRecorder != null)
            {
                audioRecorder.Dispose();
                audioRecorder = null;
            }
            currentCallSessionId = null;
            currentCallContact = null;
            currentCallCalleeAccepted = false;
            currentCallAccepted = false;
            currentCallCodec = null;
        }

        public static void acceptCall(byte[] session_id)
        {
            if (!hasSession(session_id))
            {
                return;
            }

            if (currentCallAccepted)
            {
                return;
            }

            currentCallAccepted = true;
            StreamProcessor.sendAppRequestAccept(currentCallContact, session_id, Encoding.UTF8.GetBytes(currentCallCodec));
            startVoIPSession();
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).displayCallBar(currentCallSessionId, "In-call with " + currentCallContact.nickname + ".");
        }

        public static void onAcceptedCall(byte[] session_id, byte[] data)
        {
            if (!hasSession(session_id))
            {
                return;
            }

            if(currentCallCalleeAccepted)
            {
                return;
            }

            currentCallCodec = Encoding.UTF8.GetString(data);
            currentCallCalleeAccepted = true;
            startVoIPSession();
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).displayCallBar(currentCallSessionId, "In-call with " + currentCallContact.nickname + ".");
        }

        public static void rejectCall(byte[] session_id)
        {
            if (!hasSession(session_id))
            {
                return;
            }
            StreamProcessor.sendAppRequestReject(currentCallContact, session_id);
            endVoIPSession();
        }

        public static void onRejectedCall(byte[] session_id)
        {
            if (!hasSession(session_id))
            {
                return;
            }
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).hideCallBar();
            endVoIPSession();
        }

        public static void hangupCall(byte[] session_id)
        {
            if (session_id == null)
            {
                session_id = currentCallSessionId;
            }
            StreamProcessor.sendAppEndSession(currentCallContact, session_id);
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).hideCallBar();
            endVoIPSession();
        }

        public static void onHangupCall(byte[] session_id)
        {
            if (!hasSession(session_id))
            {
                return;
            }
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).hideCallBar();
            endVoIPSession();
        }

        public static void onData(byte[] session_id, byte[] data)
        {
            if (!hasSession(session_id))
            {
                return;
            }
            if (audioPlayer != null)
            {
                audioPlayer.write(data, 0, data.Length);
            }
        }

        public static bool hasSession(byte[] session_id)
        {
            if(currentCallSessionId != null && session_id != null && currentCallSessionId.SequenceEqual(session_id))
            {
                return true;
            }
            return false;
        }
    }
}
