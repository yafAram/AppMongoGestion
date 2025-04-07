using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppMongo.Controllers
{
    public class DatabaseController : Controller
    {
        // GET: DatabaseController
        public ActionResult Index()
        {
            return View();
        }

        // GET: DatabaseController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: DatabaseController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DatabaseController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DatabaseController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: DatabaseController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DatabaseController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DatabaseController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
