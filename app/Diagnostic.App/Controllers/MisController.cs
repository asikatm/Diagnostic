using Microsoft.AspNetCore.Mvc;

namespace Diagnostic.App.Controllers;

/// <summary>MIS module screens (management reports).</summary>
public class MisController : Controller
{
    public IActionResult MediaCommission() => View();
}
