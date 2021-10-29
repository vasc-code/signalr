using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SignalRPoc.Controllers;
using SignalRPoc.Helper;

namespace SignalRPoc.Filters
{
    public class AccountFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var user = context.HttpContext.GetUserSession();
            if (user != null)
            {
                var controller = (BaseController)context.Controller;
                controller.ViewBag.User = user;
            }
        }

        public async void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = (BaseController)context.Controller;
            if (controller is AccountController == false)
            {
                var user = context.HttpContext.GetUserSession();
                if (user == null)
                {
                    context.Result = new RedirectToActionResult("Index", "Account", null);
                    await context.Result.ExecuteResultAsync(context);
                    return;
                }
            }
        }
    }
}
