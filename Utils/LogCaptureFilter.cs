using Microsoft.AspNetCore.Mvc.Filters;

namespace Floaty_Music.Utils
{
    public class LogCaptureFilter : IActionFilter
    {
        public static List<string> Logs = new();

        public void OnActionExecuting(ActionExecutingContext context)
        {
            Logs.Add($"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}");
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

}
