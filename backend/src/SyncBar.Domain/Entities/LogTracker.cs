using SyncBar.Domain.Primitives;
using System;

namespace SyncBar.Domain.Entities
{
    public sealed class LogTracker : AggregateRoot
    {
        public LogTracker(long id) : base(id)
        {
        }

        private LogTracker() : base(0)
        {
        }

        public long? AppUserId { get; set; }
        public string DirectoryName { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public bool IsSuccess { get; set; }
        public long? ExecutionTimeMs { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}