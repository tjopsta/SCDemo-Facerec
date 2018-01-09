using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SCDemoFaceRec.Web;
using SCDemoFaceRecWeb.Models;

namespace SCDemoFaceRecWeb.Controllers
{
    public class HomeController : Controller
    {
        IDocumentDBRepository _repo;

        public HomeController(IDocumentDBRepository r)
        {
            _repo = r;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ActionName("List")]
        public async Task<ActionResult> ListAsync()
        {
            var persons = await _repo.GetItemsFromCollectionAsync();
            return View(persons);
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            var person = await _repo.GetItemFromCollectionAsync(id);
            return View(person);
        }
    }
}
