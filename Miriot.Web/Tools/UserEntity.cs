using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Miriot.Web.Tools
{
    public class UserEntity : TableEntity
    {
        public UserEntity(Guid personId, string name, string widgets)
        {
            PartitionKey = "User";
            RowKey = personId.ToString();
            Name = name;
            Widgets = widgets;
        }

        public UserEntity() { }

        public string Name { get; set; }
        public string Widgets { get; set; }
    }

    internal class LogEntity : TableEntity
    {
        public LogEntity(string message, Level level, Exception exception)
        {
            PartitionKey = level.ToString();
            RowKey = DateTime.UtcNow.ToString("yyyyMMddhhmmssfff");
            Message = message;
            Exception = exception?.ToString();
        }

        public string Exception { get; set; }

        public string Message { get; set; }

        public LogEntity() { }

    }

    public enum Level
    {
        Info,
        Warning,
        Error
    }
}
