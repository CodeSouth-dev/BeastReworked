using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using SimpleMapBot.Configuration;
using SimpleMapBot.Tasks;
using log4net;

namespace SimpleMapBot.Core
{
    /// <summary>
    /// Simple map clearing bot - takes maps from stash, clears them, loots, returns
    /// </summary>
    public class SimpleMapBot : IBot
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private TaskManager _taskManager;
        private readonly Stopwatch _runStopwatch = new Stopwatch();

        #region IAuthored Implementation
        public string Name => "SimpleMapBot";
        public string Description => "Simple map clearing bot - runs maps, kills monsters, loots items";
        public string Author => "YourName";
        public string Version => "1.0.0";
        #endregion

        #region IBase Implementation
        public void Initialize()
        {
            Log.InfoFormat("[{0}] Initializing version {1}", Name, Version);

            // Create task manager
            _taskManager = new TaskManager();

            // Register tasks in execution order
            RegisterTasks();

            Log.InfoFormat("[{0}] Initialized with {1} tasks", Name, _taskManager.Tasks.Count);
        }

        public void Deinitialize()
        {
            Log.InfoFormat("[{0}] Deinitializing", Name);
        }

        public void Enable()
        {
            Log.InfoFormat("[{0}] Enabled", Name);
        }

        public void Disable()
        {
            Log.InfoFormat("[{0}] Disabled", Name);
        }
        #endregion

        #region IConfigurable Implementation
        public JsonSettings Settings => SimpleMapBotSettings.Instance;
        public UserControl Control => null; // Using JSON settings only
        #endregion

        #region ILogicProvider Implementation
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }
        #endregion

        #region IMessageHandler Implementation
        public MessageResult Message(Message message)
        {
            // Handle GetTaskManager message
            if (message.Id == "GetTaskManager")
            {
                message.AddOutput(this, _taskManager);
                return MessageResult.Processed;
            }

            return MessageResult.Unprocessed;
        }
        #endregion

        #region IStartStopEvents Implementation
        public void Start()
        {
            Log.InfoFormat("[{0}] Starting bot", Name);
            _runStopwatch.Restart();
        }

        public void Stop()
        {
            Log.InfoFormat("[{0}] Stopping bot after {1}", Name, _runStopwatch.Elapsed);
            _runStopwatch.Stop();
        }
        #endregion

        #region ITickEvents Implementation
        public void Tick()
        {
            // Tick logic if needed
        }
        #endregion

        #region IBot Implementation
        public void Execute()
        {
            // Main bot execution loop
            if (!LokiPoe.IsInGame)
            {
                Log.Warn("[SimpleMapBot] Not in game, waiting...");
                return;
            }

            // Run task manager
            _taskManager.Execute();
        }
        #endregion

        /// <summary>
        /// Register all bot tasks in execution order
        /// </summary>
        private void RegisterTasks()
        {
            // Task execution order:
            // 1. Return to hideout if needed
            // 2. Take map from stash
            // 3. Open and activate map device
            // 4. Enter map portal
            // 5. Explore and clear zone
            // 6. Loot items
            // 7. Return when done

            _taskManager.AddTask(new ReturnToHideoutTask());
            _taskManager.AddTask(new TakeMapFromStashTask());
            _taskManager.AddTask(new OpenMapDeviceTask());
            _taskManager.AddTask(new EnterMapTask());
            _taskManager.AddTask(new ExploreAndClearTask());
            _taskManager.AddTask(new ReturnToHideoutTask());

            Log.InfoFormat("[SimpleMapBot] Registered {0} tasks", _taskManager.Tasks.Count);
        }
    }
}
