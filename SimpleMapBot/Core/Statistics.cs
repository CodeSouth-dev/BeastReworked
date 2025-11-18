using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using log4net;

namespace SimpleMapBot.Core
{
    /// <summary>
    /// Tracks bot performance statistics.
    /// Implements INotifyPropertyChanged for GUI data binding.
    /// </summary>
    public class Statistics : INotifyPropertyChanged
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private static Statistics _instance;
        public static Statistics Instance => _instance ?? (_instance = new Statistics());

        private Statistics()
        {
            _uptimeTimer.Start();
        }

        #region Private Fields

        private readonly Stopwatch _uptimeTimer = new Stopwatch();
        private readonly Stopwatch _currentMapTimer = new Stopwatch();
        private readonly List<int> _mapCompletionTimes = new List<int>(); // in seconds

        private int _mapsStarted = 0;
        private int _mapsCompleted = 0;
        private int _deaths = 0;
        private int _currencyLooted = 0;
        private int _itemsLooted = 0;
        private int _mapsLooted = 0;

        #endregion

        #region Public Properties

        public int MapsStarted
        {
            get => _mapsStarted;
            private set
            {
                if (value == _mapsStarted) return;
                _mapsStarted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MapsPerHour));
            }
        }

        public int MapsCompleted
        {
            get => _mapsCompleted;
            private set
            {
                if (value == _mapsCompleted) return;
                _mapsCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MapsPerHour));
                OnPropertyChanged(nameof(CompletionRate));
            }
        }

        public int Deaths
        {
            get => _deaths;
            private set
            {
                if (value == _deaths) return;
                _deaths = value;
                OnPropertyChanged();
            }
        }

        public int CurrencyLooted
        {
            get => _currencyLooted;
            private set
            {
                if (value == _currencyLooted) return;
                _currencyLooted = value;
                OnPropertyChanged();
            }
        }

        public int ItemsLooted
        {
            get => _itemsLooted;
            private set
            {
                if (value == _itemsLooted) return;
                _itemsLooted = value;
                OnPropertyChanged();
            }
        }

        public int MapsLooted
        {
            get => _mapsLooted;
            private set
            {
                if (value == _mapsLooted) return;
                _mapsLooted = value;
                OnPropertyChanged();
            }
        }

        // Calculated properties
        public double MapsPerHour
        {
            get
            {
                var hours = _uptimeTimer.Elapsed.TotalHours;
                if (hours < 0.01) return 0;
                return Math.Round(_mapsCompleted / hours, 2);
            }
        }

        public double CompletionRate
        {
            get
            {
                if (_mapsStarted == 0) return 0;
                return Math.Round((_mapsCompleted / (double)_mapsStarted) * 100, 1);
            }
        }

        public string AverageMapTime
        {
            get
            {
                if (_mapCompletionTimes.Count == 0) return "0:00";
                var avgSeconds = _mapCompletionTimes.Average();
                return TimeSpan.FromSeconds(avgSeconds).ToString(@"m\:ss");
            }
        }

        public string TotalRuntime => _uptimeTimer.Elapsed.ToString(@"hh\:mm\:ss");
        public string CurrentMapTime => _currentMapTimer.Elapsed.ToString(@"m\:ss");

        #endregion

        #region Public Methods

        /// <summary>
        /// Call when a new map is entered
        /// </summary>
        public void OnMapStarted()
        {
            MapsStarted++;
            _currentMapTimer.Restart();
            Log.InfoFormat("[Statistics] Map started. Total: {0}", MapsStarted);
        }

        /// <summary>
        /// Call when a map is completed
        /// </summary>
        public void OnMapCompleted()
        {
            MapsCompleted++;
            _currentMapTimer.Stop();

            var completionTime = (int)_currentMapTimer.Elapsed.TotalSeconds;
            _mapCompletionTimes.Add(completionTime);

            Log.InfoFormat("[Statistics] Map completed in {0}s. Total: {1} ({2:F1}%)",
                completionTime, MapsCompleted, CompletionRate);

            OnPropertyChanged(nameof(AverageMapTime));
            OnPropertyChanged(nameof(CurrentMapTime));
        }

        /// <summary>
        /// Call when player dies
        /// </summary>
        public void OnDeath()
        {
            Deaths++;
            Log.WarnFormat("[Statistics] Death recorded. Total deaths: {0}", Deaths);
        }

        /// <summary>
        /// Call when currency is looted
        /// </summary>
        public void OnCurrencyLooted(int count = 1)
        {
            CurrencyLooted += count;
        }

        /// <summary>
        /// Call when any item is looted
        /// </summary>
        public void OnItemLooted(int count = 1)
        {
            ItemsLooted += count;
        }

        /// <summary>
        /// Call when a map is looted
        /// </summary>
        public void OnMapLooted(int count = 1)
        {
            MapsLooted += count;
        }

        /// <summary>
        /// Resets all statistics
        /// </summary>
        public void Reset()
        {
            MapsStarted = 0;
            MapsCompleted = 0;
            Deaths = 0;
            CurrencyLooted = 0;
            ItemsLooted = 0;
            MapsLooted = 0;
            _mapCompletionTimes.Clear();
            _uptimeTimer.Restart();
            _currentMapTimer.Reset();

            Log.Info("[Statistics] All statistics reset");

            OnPropertyChanged(nameof(MapsPerHour));
            OnPropertyChanged(nameof(CompletionRate));
            OnPropertyChanged(nameof(AverageMapTime));
            OnPropertyChanged(nameof(TotalRuntime));
            OnPropertyChanged(nameof(CurrentMapTime));
        }

        /// <summary>
        /// Call periodically to update time-based properties
        /// </summary>
        public void Tick()
        {
            // Update time-based properties for GUI binding
            OnPropertyChanged(nameof(TotalRuntime));
            OnPropertyChanged(nameof(CurrentMapTime));
            OnPropertyChanged(nameof(MapsPerHour));
        }

        /// <summary>
        /// Returns a formatted summary of current statistics
        /// </summary>
        public string GetSummary()
        {
            return $"Maps: {MapsCompleted}/{MapsStarted} ({CompletionRate:F1}%) | " +
                   $"Maps/hr: {MapsPerHour:F2} | " +
                   $"Avg time: {AverageMapTime} | " +
                   $"Deaths: {Deaths} | " +
                   $"Items: {ItemsLooted} | " +
                   $"Currency: {CurrencyLooted} | " +
                   $"Runtime: {TotalRuntime}";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
