using System.Diagnostics;
using Diagnostic.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostic.App.Controllers;

public class HomeController : Controller
{
    /// <summary>Module tiles (Diagnostic, Administrative ...).</summary>
    public IActionResult Index() => View();

    public IActionResult Login() => View();

    /// <summary>Remembers the chosen module in the browser and opens its first menu.</summary>
    public IActionResult Module(string id)
    {
        ViewBag.ProjectCode = id;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

/// <summary>Generic setup screen; the grid / form definition for <c>id</c> lives in wwwroot/js/masters.js.</summary>
public class MasterController : Controller
{
    public IActionResult Index(string id)
    {
        ViewBag.MasterKey = id;
        return View();
    }
}

public class DiagnosticController : Controller
{
    public IActionResult Dashboard() => View();
    public IActionResult Investigation() => View();
    public IActionResult Invoices() => View();
    public IActionResult SampleCollection() => View();
    public IActionResult SampleAcknowledgement() => View();
    public IActionResult Sticker() => View();
    public IActionResult ResultEntry() => View();
    public IActionResult LabReport() => View();
    public IActionResult ReportFinalization() => View();
    public IActionResult ReportDelivery() => View();
    public IActionResult CancelService() => View();
    public IActionResult ApproveCancel() => View();
}

public class AdminController : Controller
{
    public IActionResult MenuPermission() => View();
    public IActionResult ReportPermission() => View();
}

public class ReportsController : Controller
{
    public IActionResult Index() => View();
}
