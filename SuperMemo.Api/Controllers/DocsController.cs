using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;

namespace SuperMemo.Api.Controllers;

/// <summary>
/// Serves project documentation (e.g. planning docs) from the API.
/// </summary>
[Route("api/docs")]
public class DocsController(IWebHostEnvironment env) : BaseController
{
    private const string VirtualBankingPlanningFileName = "VirtualBankingAPI-Planning.md";
    private const string Phase2RequirementsFileName = "VirtualBankingAPI-Phase2-Requirements.md";
    private const string Phase3SystemDesignFileName = "VirtualBankingAPI-Phase3-SystemDesign.md";
    private const string Phase4ImplementationFileName = "VirtualBankingAPI-Phase4-Implementation.md";
    private const string Phase5TestingFileName = "VirtualBankingAPI-Phase5-TestingAndQA.md";
    private const string Phase6MaintenanceFileName = "VirtualBankingAPI-Phase6-MaintenanceAndOperations.md";
    private const string BackendChecklistFileName = "Backend-Todo-Checklist.md";

    /// <summary>
    /// Returns the Virtual Banking API – Customer Wallet planning document (Phase 1) as raw Markdown.
    /// </summary>
    [HttpGet("virtual-banking-planning")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetVirtualBankingPlanning(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", VirtualBankingPlanningFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Planning document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Virtual Banking API – Customer Wallet with Linked Cards (Phase 1 Planning)", markdown });

        return Content(markdown, "text/markdown");
    }

    /// <summary>
    /// Returns the Phase 2 Requirements &amp; Analysis document as raw Markdown.
    /// </summary>
    [HttpGet("phase2-requirements")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetPhase2Requirements(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", Phase2RequirementsFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Phase 2 requirements document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Virtual Banking API – Phase 2 Requirements & Analysis", markdown });

        return Content(markdown, "text/markdown");
    }

    /// <summary>
    /// Returns the Phase 3 System Design document as raw Markdown.
    /// </summary>
    [HttpGet("phase3-system-design")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetPhase3SystemDesign(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", Phase3SystemDesignFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Phase 3 system design document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Virtual Banking API – Phase 3 System Design", markdown });

        return Content(markdown, "text/markdown");
    }

    /// <summary>
    /// Returns the Phase 4 Implementation guide as raw Markdown.
    /// </summary>
    [HttpGet("phase4-implementation")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetPhase4Implementation(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", Phase4ImplementationFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Phase 4 implementation document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Virtual Banking API – Phase 4 Implementation Guide", markdown });

        return Content(markdown, "text/markdown");
    }

    /// <summary>
    /// Returns the Phase 5 Testing &amp; QA strategy as raw Markdown.
    /// </summary>
    [HttpGet("phase5-testing-qa")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetPhase5TestingAndQa(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", Phase5TestingFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Phase 5 testing document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Virtual Banking API – Phase 5 Testing & QA Strategy", markdown });

        return Content(markdown, "text/markdown");
    }

    /// <summary>
    /// Returns the Phase 6 Maintenance &amp; Operations plan as raw Markdown.
    /// </summary>
    [HttpGet("phase6-maintenance-operations")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetPhase6MaintenanceAndOperations(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", Phase6MaintenanceFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Phase 6 maintenance document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Virtual Banking API – Phase 6 Maintenance & Operations", markdown });

        return Content(markdown, "text/markdown");
    }

    /// <summary>
    /// Returns the Backend Feature Checklist (status vs original todo list) as raw Markdown.
    /// </summary>
    [HttpGet("backend-todo-checklist")]
    [Produces("text/markdown", "application/json")]
    public async Task<IActionResult> GetBackendTodoChecklist(CancellationToken cancellationToken)
    {
        var path = Path.Combine(env.ContentRootPath, "Docs", BackendChecklistFileName);
        if (!System.IO.File.Exists(path))
            return NotFound("Backend checklist document not found.");

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);

        if (Request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return Ok(new { title = "Backend Feature Checklist – Virtual Banking API", markdown });

        return Content(markdown, "text/markdown");
    }
}
