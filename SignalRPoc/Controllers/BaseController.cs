using Microsoft.AspNetCore.Mvc;
using SignalRPoc.Models;

namespace SignalRPoc.Controllers
{
    public abstract class BaseController : Controller
    {
        public UserSession UserSession { get; set; }
    }
}
