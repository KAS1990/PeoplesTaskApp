using System.Diagnostics;

namespace PeoplesTaskApp.Utils.Extensions
{
    public static class ExceptionExtensions
    {
        public static string ToDebugString(this Exception ex) => ex.ToString();

        public static void WriteToDebugConsole(this Exception ex)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
            Debug.WriteLine(ex.ToDebugString());
        }

        public static string GetAllMessagesAsList(this Exception extension, bool isNumberedList = true)
        {
            var ex = extension;

            int i = 1;
            var message = isNumberedList && ex.InnerException is not null ? $"{i}. {ex.Message}" : ex.Message;

            ex = ex.InnerException;
            while (ex is not null)
            {
                i++;
                message += isNumberedList ? $"{Environment.NewLine}{i}. {ex.Message}" : $"{Environment.NewLine}{ex.Message}";
                ex = ex.InnerException;
            }

            return message;
        }

        public static string GetAllStackTraceAsList(this Exception extension, bool isNumberedList = true)
        {
            var ex = extension;

            int i = 1;
            var message = isNumberedList && ex.InnerException is not null ? $"{i}. {ex.StackTrace}" : ex.StackTrace ?? "";

            ex = ex.InnerException;
            while (ex is not null)
            {
                i++;
                message += isNumberedList ? $"{Environment.NewLine}{i}. {ex.StackTrace}" : $"{Environment.NewLine}{ex.StackTrace}";
                ex = ex.InnerException;
            }

            return message;
        }

        public static Exception ExceptionOrUnknownError(this Exception? ex) => ex ?? new Exception("Unknown error");
    }
}
