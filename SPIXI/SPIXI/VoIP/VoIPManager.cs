using System;
using System.Linq;
using Xamarin.Forms;

namespace SPIXI.VoIP
{

    public class VoIPManager
    {
        public static byte[] currentCallSessionId { get; private set; }
        public static Friend currentCallContact { get; private set; }
        public static bool currentCallAccepted { get; private set; }
        public static bool currentCallCalleeAccepted { get; private set; }

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
            StreamProcessor.sendAppRequest(friend, "spixi.voip", currentCallSessionId);
            ((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).displayCallBar(currentCallSessionId, "Calling " + friend.nickname + "...");
        }

        public static void onReceivedCall(Friend friend, byte[] session_id)
        {
            if (currentCallSessionId == null)
            {
                currentCallSessionId = session_id;
                currentCallContact = friend;
                currentCallCalleeAccepted = true;
                currentCallAccepted = false;
            }
            else
            {
                StreamProcessor.sendAppRequestReject(friend, session_id);
            }
        }

        private static void startVoIPSession()
        {
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
            StreamProcessor.sendAppRequestAccept(currentCallContact, session_id);
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
