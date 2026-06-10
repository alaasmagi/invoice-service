using Contracts.Application;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class InvoiceImportsController(IPdfInvoiceImportService importService) : UserScopedControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile? pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            return View("Result", ErrorResult("Choose a PDF invoice file to import."));
        }

        if (pdfFile.Length > MaxUploadBytes)
        {
            return View("Result", ErrorResult("The PDF is too large. Maximum allowed size is 10 MB."));
        }

        var hasPdfExtension = Path.GetExtension(pdfFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        var hasPdfContentType = pdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                                || pdfFile.ContentType.Equals("application/x-pdf", StringComparison.OrdinalIgnoreCase);
        if (!hasPdfExtension || !hasPdfContentType)
        {
            return View("Result", ErrorResult("Only PDF files can be imported."));
        }

        await using var stream = pdfFile.OpenReadStream();
        var result = await importService.ImportAsync(new PdfInvoiceImportRequest
        {
            PdfStream = stream,
            FileName = pdfFile.FileName,
            Length = pdfFile.Length
        }, CurrentUserId());

        return View("Result", result);
    }

    private static PdfInvoiceImportResult ErrorResult(string message)
    {
        var result = new PdfInvoiceImportResult();
        result.FatalErrors.Add(message);
        return result;
    }
}
