using Crowd_knowledge_contribution.Data;
using Crowd_knowledge_contribution.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Crowd_knowledge_contribution.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly ApplicationDbContext db;
        public HomeController(ApplicationDbContext context)
        {
            db = context;
        }
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Articles");
            }
            var articles = from article in db.Articles
                           select article;
            ViewBag.FirstArticle = articles.First();
            ViewBag.Articles = articles.OrderBy(o =>
           o.Date).Skip(1).Take(2);
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}