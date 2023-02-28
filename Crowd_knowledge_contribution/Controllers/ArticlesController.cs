using Crowd_knowledge_contribution.Data;
using Crowd_knowledge_contribution.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;


namespace Crowd_knowledge_contribution.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public ArticlesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        // Se afiseaza lista tuturor articolelor impreuna cu categoria 
        // din care fac parte dar
        // Pentru fiecare articol se afiseaza si userul care a postat articolul respectiv
        // HttpGet implicit
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            var articles = db.Articles.Include("Category").Include("User").OrderBy(a => a.Date);
            var search = "";
            // MOTOR DE CAUTARE
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                // eliminam spatiile libere
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();
                List<int> articleIds = db.Articles.Where(at => at.Title.Contains(search) || at.Content.Contains(search)).Select(a => a.Id).ToList();
                // Cautare in comentarii (Content)
                List<int> articleIdsOfCommentsWithSearchString = db.Comments.Where(c => c.Content.Contains(search)).Select(c => (int)c.ArticleId).ToList();
                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = articleIds.Union(articleIdsOfCommentsWithSearchString).ToList();
                // Lista articolelor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                articles = db.Articles.Where(article => mergedIds.Contains(article.Id)).Include("Category").Include("User").OrderBy(a => a.Date);
            }
            ViewBag.SearchString = search;
            // Alegem sa afisam 3 articole pe pagina
            int _perPage = 3;
            //var articles = db.Articles.Include("Category").OrderBy(a => a.Date);
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message =
                TempData["message"].ToString();
            }
            // Fiind un numar variabil de articole, verificam de fiecare data utilizand
            // metoda Count()
            int totalItems = articles.Count();
            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Articles/Index?page=valoare
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            var offset = 0;
            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }
            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam
            // in functie de offset
            var paginatedArticles =
            articles.Skip(offset).Take(_perPage);
            // Preluam numarul ultimei pagini

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Articles = paginatedArticles;
            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Articles/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Articles/Index/?page";
            }

            return View();
        }

        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // Se afiseaza si userul care a postat articolul respectiv
        // HttpGet implicit

        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            Article article = db.Articles.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(art => art.Id == id)
                                         .First();


            SetAccessRights();

            return View(article);
        }


        // Adaugarea unui comentariu asociat unui articol in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Articles/Show/" + comment.ArticleId);
            }

            else
            {
                Article art = db.Articles.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(art => art.Id == comment.ArticleId)
                                         .First();

                //return Redirect("/Articles/Show/" + comm.ArticleId);

                SetAccessRights();

                return View(art);
            }
        }



        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // Doar utilizatorii cu rolul de Editor sau Admin pot adauga articole in platforma
        // HttpGet implicit

        [Authorize(Roles = "Editor,Admin")]
        public IActionResult New()
        {
            Article article = new Article();

            // Se preia lista de categorii din metoda GetAllCategories()
            article.Categ = GetAllCategories();


            return View(article);
        }

        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul de Editor sau Admin pot adauga articole in platforma

        [Authorize(Roles = "Editor,Admin")]
        [HttpPost]
        public IActionResult New(Article article)
        {
            var sanitizer = new HtmlSanitizer();

            article.Date = DateTime.Now;
            article.Restriction = false;
            article.UserId = _userManager.GetUserId(User);

    
            if (ModelState.IsValid)
            {
                article.Content = sanitizer.Sanitize(article.Content);
                article.Content = (article.Content);

                db.Articles.Add(article);
                /////////////////////////////////////////////////////CREEZ UN ISTORIC NOU
                ///
                ArticleHistory articleHistory = new ArticleHistory();
                //articleHistory.IdHistory = article.Id;
                articleHistory.Title = article.Title;
                articleHistory.Content= article.Content;    
                articleHistory.Date = article.Date;
                articleHistory.CategoryId = article.CategoryId;
                db.ArticlesHistory.Add(articleHistory);
                ////////////////////////////////////////////////////////////////////////////
                db.SaveChanges();
                TempData["message"] = "Articolul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                article.Categ = GetAllCategories();
                return View(article);
            }
        }

        // Se editeaza un articol existent in baza de date impreuna cu categoria
        // din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // HttpGet implicit
        // Se afiseaza formularul impreuna cu datele aferente articolului
        // din baza de date
        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id)
        {

            Article article = db.Articles.Include("Category")
                                        .Where(art => art.Id == id)
                                        .First();

            article.Categ = GetAllCategories();

            if (article.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin") )
            {
                return View(article);
            }
            else 
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                return RedirectToAction("Index");
            }

        }

        // Se adauga articolul modificat in baza de date
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id, Article requestArticle)
        {
            var sanitizer = new HtmlSanitizer();

            Article article = db.Articles.Find(id);
 ////////////////////////////////////////////////Editare 
            ArticleHistory articleHistory = db.ArticlesHistory.Find(id);

            if (ModelState.IsValid)
            {
                if (article.UserId == _userManager.GetUserId(User) && article.Restriction != true)
                {
                    requestArticle.Content = sanitizer.Sanitize(requestArticle.Content);

                    ///////////////////////////////////////////////////////////////EDITARE
                    // articleHistory.IdHistory = article.Id;
                    articleHistory.Title = article.Title;
                    articleHistory.Content = article.Content;
                    articleHistory.CategoryId = article.CategoryId;
                    ////////////////////////////////////////////////////////////////////
                    article.Title = requestArticle.Title;
                    article.Content = requestArticle.Content;
                    article.CategoryId = requestArticle.CategoryId;
 
                    TempData["message"] = "Articolul a fost modificat";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else if (User.IsInRole("Admin"))
                {
                    ///////////////////////////////////////////////////////////////EDITARE
                    // articleHistory.IdHistory = article.Id;
                    articleHistory.Title = article.Title;
                    articleHistory.Content = article.Content;
                    articleHistory.CategoryId = article.CategoryId;
                    ////////////////////////////////////////////////////////////////////
                    article.Title = requestArticle.Title;
                    article.Content = requestArticle.Content;
                    article.CategoryId = requestArticle.CategoryId;

                    TempData["message"] = "Articolul a fost modificat";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra articolului ";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestArticle.Categ = GetAllCategories();
                return View(requestArticle);
            }
        }
        /////////////////////////////EDITARE ADMIN 
        [Authorize(Roles = "Admin")]
        public IActionResult EditAdmin(int id)
        {

            Article article = db.Articles.Find(id);
            ArticleHistory articleHistory = db.ArticlesHistory.Find(id);

            if ( User.IsInRole("Admin"))
            {
                article.Title = articleHistory.Title;
                article.Content = articleHistory.Content;
                article.CategoryId = articleHistory.CategoryId;

                TempData["message"] = "Articolul a fost modificat";
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu s-a modificat! Nu sunteti Admin.";
                return RedirectToAction("Index");
            }

        }
        //Restrictionare user de catre admin
        [Authorize(Roles = "Admin")]
        public IActionResult Restrictionare(int id)
        {

            Article article = db.Articles.Find(id);

            if (User.IsInRole("Admin"))
            {
                // article.UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0";
                article.Restriction = true;
                TempData["message"] = "Userul a fost restrictionat.";
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Usetul nu a fost restrictionat. ";
                return RedirectToAction("Index");
            }

        }
        // Se sterge un articol din baza de date 
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Delete(int id)
        {
            Article article = db.Articles.Include("Comments")
                                         .Where(art => art.Id == id)
                                         .First();
            ArticleHistory articleHistory= db.ArticlesHistory.Find(id);
            if (article.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Articles.Remove(article);
                //stergere
                db.ArticlesHistory.Remove(articleHistory);
                db.SaveChanges();
                TempData["message"] = "Articolul a fost sters";
                return RedirectToAction("Index");
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti un articol care nu va apartine";
                return RedirectToAction("Index");
            }
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Categories
                             select cat;

            // iteram prin categorii
            foreach (var category in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName.ToString()
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var category in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = category.Id.ToString();
                listItem.Text = category.CategoryName.ToString();

                selectList.Add(listItem);
             }*/


            // returnam lista de categorii
            return selectList;
        }

        // Metoda utilizata pentru exemplificarea Layout-ului
        // Am adaugat un nou Layout in Views -> Shared -> numit _LayoutNou.cshtml
        // Aceasta metoda are un View asociat care utilizeaza noul layout creat
        // in locul celui default generat de framework numit _Layout.cshtml
        public IActionResult IndexNou()
        {
            return View();
        }

        // Conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Editor"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}

