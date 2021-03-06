﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Messaging;
using System.Runtime.Serialization;
using System.IO;
using System.Windows.Resources;
using System.Text;
using System.Threading;
using System.Device.Location;
using Rider.Persistent;
using Rider.Utils;

namespace Rider.Location
{
    public class LocationService
    {
        public static readonly string TABLE_NAME = "locations";
        public static readonly string SESSION_ID_COLUMN_NAME = "session_id";
        public static readonly string LAT_COLUMN_NAME = "lat";
        public static readonly string LNG_COLUMN_NAME = "lng";

        public enum State
        {
            Init = 0,
            Ready,
            Disabled,
            Error
        };

        public enum Mode
        {
            JsonFile = 0,
            Web
        };

        private static string DesiredMovementThreshold = "DesiredMovementThreshold";
        private const string TAG = "LocationService";
        public static string locationChanged = "locationChanged";
        public static string locationStateChanged = "locationStateChanged";

        private EventHandler<GeoPositionStatusChangedEventArgs> statusEvent;
        private EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>> positionEvent;
        private IGeoPositionWatcher<GeoCoordinate> watcher;
        private Mode selectedMode = Mode.JsonFile;
        private bool isRunning;
        private GeoCoordinate lastLocation;
        private double accuracy;
        private Random random;
        private static LocationService instance;

        #region Constructors

        private LocationService()
        {
            lastLocation = null;
            if (Configuration.FAKE_LOCATION_ENABLED)
            {
                watcher = new FakeLocationMessenger(selectedMode);
                random = new Random();
            }
            else
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                (watcher as GeoCoordinateWatcher).MovementThreshold = UserData.Get<double>(DesiredMovementThreshold);
            }
        }

        public static LocationService Instance
        {
            get
            {
                if (instance == null)
                    instance = new LocationService();
                return instance;
            }
        }

        #endregion

        #region manager

        public void StartLocationWatcher()
        {
            statusEvent = new EventHandler<GeoPositionStatusChangedEventArgs>(StatusChanged);
            positionEvent = new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(PositionChanged);

            // Listen to the GPS events
            watcher.StatusChanged += statusEvent;
            watcher.PositionChanged += positionEvent;

            watcher.Start();
            this.isRunning = true;
            DebugUtils.Log(TAG, "LocationService Started");
        }

        public void StopLocationWatcher()
        {
            // Remove event Handlers
            watcher.StatusChanged -= statusEvent;
            watcher.PositionChanged -= positionEvent;

            statusEvent = null;
            positionEvent = null;

            if (Configuration.FAKE_LOCATION_ENABLED)
            {
                (watcher as FakeLocationMessenger).Close();
            }
            else
            {
                watcher.Stop();
            }
            this.isRunning = false;
            DebugUtils.Log(TAG, "LocationService Stoppped");
        }

        #endregion

        #region properties

        public bool IsRunnig
        {
            get
            {
                return this.isRunning;
            }
        }

        public GeoPositionStatus CurrentState
        {
            get { return watcher.Status; }
        }

        #endregion

        #region events

        void StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Initializing:
                    Messenger.Default.Send(State.Init, locationStateChanged);
                    break;
                case GeoPositionStatus.Ready:
                    Messenger.Default.Send(State.Ready, locationStateChanged);
                    break;
                case GeoPositionStatus.Disabled:
                    Messenger.Default.Send(State.Disabled, locationStateChanged);
                    break;
                case GeoPositionStatus.NoData:
                    Messenger.Default.Send(State.Error, locationStateChanged);
                    break;
            }
        }

        void PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            GeoCoordinate location = e.Position.Location;
            double speed = e.Position.Location.Speed;

            if (Configuration.FAKE_LOCATION_ENABLED)
            {
                accuracy = 40;
                speed = random.Next(Configuration.FAKE_MIN_SPEED, Configuration.FAKE_MAX_SPEED);

                if (double.IsNaN(location.Course))
                {
                    if (lastLocation != null)
                        location.Course = MathUtils.CourseFromPoint(lastLocation, location);
                    else
                        location.Course = 180.0;
                }
            }
            else accuracy = e.Position.Location.HorizontalAccuracy;

            if (double.IsNaN(location.Course)) location.Course = 180;

            if (accuracy <= 100)
            {
                location.Speed = speed;
                lastLocation = location;
                Messenger.Default.Send(location, locationChanged);
            }
            else Messenger.Default.Send(State.Error, locationStateChanged);
        }

        #endregion
    }
}
