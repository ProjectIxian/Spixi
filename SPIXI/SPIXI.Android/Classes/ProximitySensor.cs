using Android.Content;
using Android.Hardware;
using Android.Runtime;

namespace SPIXI.Droid.Classes
{
    class ProximitySensor : Java.Lang.Object, ISensorEventListener
    {
        private SensorManager sensorManager;
        private Sensor proximity;

        public ProximitySensor()
        {
            // Get an instance of the sensor service, and use that to get an instance of
            // a particular sensor.
            sensorManager = (SensorManager)MainActivity.Instance.GetSystemService(Context.SensorService);
            proximity = sensorManager.GetDefaultSensor(SensorType.Proximity);
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e.Values[0] < proximity.MaximumRange)
            {
                App.proximityNear = true;
            }
            else
            {
                App.proximityNear = false;
            }
        }

        public void OnPause()
        {
            // Be sure to unregister the sensor when the activity pauses.
            sensorManager.UnregisterListener(this);
        }

        public void OnResume()
        {
            // Register a listener for the sensor.
            sensorManager.RegisterListener(this, proximity, SensorDelay.Normal);
        }
    }
}