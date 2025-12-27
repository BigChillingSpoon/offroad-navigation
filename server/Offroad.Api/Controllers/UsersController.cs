using Microsoft.AspNetCore.Mvc;

namespace Offroad.Api.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
