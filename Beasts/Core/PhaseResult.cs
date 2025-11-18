using System;
using DreamPoeBot.Loki.Bot;

namespace Beasts.Core
{
    /// <summary>
    /// Result returned by phases to indicate their status
    /// This provides more granular control than LogicResult
    /// </summary>
    public class PhaseResult
    {
        public PhaseStatus Status { get; set; }
        public string Message { get; set; }
        public TimeSpan? WaitDuration { get; set; }

        public static PhaseResult Success(string message = null) => 
            new PhaseResult { Status = PhaseStatus.Success, Message = message };

        public static PhaseResult InProgress(string message = null) => 
            new PhaseResult { Status = PhaseStatus.InProgress, Message = message };

        public static PhaseResult Wait(string message, TimeSpan? duration = null) => 
            new PhaseResult { Status = PhaseStatus.Wait, Message = message, WaitDuration = duration };

        public static PhaseResult Failed(string message) => 
            new PhaseResult { Status = PhaseStatus.Failed, Message = message };

        /// <summary>
        /// Convert PhaseResult to LogicResult for DreamPoeBot compatibility
        /// </summary>
        public LogicResult ToLogicResult()
        {
            switch (Status)
            {
                case PhaseStatus.Success:
                case PhaseStatus.InProgress:
                case PhaseStatus.Wait:
                    return LogicResult.Provided;
                case PhaseStatus.Failed:
                    return LogicResult.Unprovided;
                default:
                    return LogicResult.Unprovided;
            }
        }
    }

    public enum PhaseStatus
    {
        InProgress,  // Phase is still running
        Success,     // Phase completed successfully
        Failed,      // Phase failed
        Wait         // Phase is waiting for something (loading, animation, etc.)
    }
}
