using System;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc; 

namespace ELearning.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        { 
            return View();
        }
         
    }
}