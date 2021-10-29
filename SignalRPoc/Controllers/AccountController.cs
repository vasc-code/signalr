using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRPoc.Helper;
using SignalRPoc.Models;
using System;
using System.Collections.Generic;

namespace SignalRPoc.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public AccountController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Logoof()
        {
            var user = HttpContext.GetUserSession();
            if (user != null)
            {
                HttpContext.SetUserSession(default);
                HttpContext.SetUser(default);
            }
            return RedirectToAction("Index", "Account");
        }

        public IActionResult LoginExternal()
        {
            var user = HttpContext.GetUserSession();
            if (user == null)
            {
                var googleRoute = GoogleLoginHelper.GetGoogleLoginRoute("Account/BackLogin", HttpContext.Request);
                return Redirect(googleRoute);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult LoginOff(string email, string name)
        {
            try
            {
                ViewBag.Email = email;
                ViewBag.Name = name;
                if (string.IsNullOrWhiteSpace(email)
                    || !email.Contains("@")
                    || !email.Contains("."))
                {
                    ViewBag.EmailInvalid = "Informe um e-mail válido.";
                    return View("Index");
                }
                if (string.IsNullOrWhiteSpace(name)
                    || !name.Contains(" "))
                {
                    ViewBag.NameInvalid = "Informe um nome com sobrenome.";
                    return View("Index");
                }
                HttpContext.SetUserSession(new UserSession(email, name, "/images/user.png"));
                return RedirectToAction("Index", "Home");
            }
            catch (Exception x)
            {
                return RedirectToAction("Index");
            }
        }

        public IActionResult BackLogin()
        {
            try
            {
                var user = GoogleLoginHelper.GetGoogleLoginUserInfo("Account/BackLogin", HttpContext.Request);
                if (user != null)
                {
                    if (!user.Email.Contains("@domvsit.com.br"))
                    {
                        ViewBag.Errors = new List<string>()
                        {
                            "Faça login com seu e-mail @domvsit.com.br"
                        };
                        return View("Index");
                    }
                    HttpContext.SetUser(user);
                    HttpContext.SetUserSession(new UserSession(user.Email, user.FirstName + " " + user.LastName, user.Image64));
                }
                ViewBag.Errors = new List<string>()
                    {
                        "Desculpe! ",
                        "não foi possivel fazer login com google. ",
                        "tente novamente."
                    };
                return RedirectToAction("Index", "Home");
            }
            catch (Exception x)
            {
                ViewBag.Errors = new List<string>()
                        {
                            x.Message
                        };
                return View("Index");
            }
        }

        public IActionResult SignalR()
        {
            return View();
        }
    }
}
