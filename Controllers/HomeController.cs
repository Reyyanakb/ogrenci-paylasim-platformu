using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using mvcFinal2.Models;

namespace mvcFinal2.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly mvcFinal2.Data.AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, mvcFinal2.Data.AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        var recentListings = _context.Listings
            .Where(l => l.IsApproved)
            .OrderByDescending(l => l.CreatedAt)
            .Take(3)
            .ToList();
        return View(recentListings);
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
