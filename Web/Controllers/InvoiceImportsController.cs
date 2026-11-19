using Contracts.Application;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class InvoiceImportsController(IInvoiceDocumentService documentService) : UserScopedControllerBase
{
    // Slightly above the 10 MB file limit, so a file just over it gets the validation message instead of a 413.
    private const long MaxRequestBytes = 11 * 1024 * 1024;

    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Upload(IFormFile? pdfFile)
    {
        if (pdfFile == null)
        {
            ModelState.AddModelError(nameof(pdfFile), T["Choose a PDF invoice file to import."]);
            return View();
        }

        await using var stream = pdfFile.OpenReadStream();
        var result = await documentService.UploadAsync(new InvoiceDocumentUploadRequest
        {
            Content = stream,
            FileName = pdfFile.FileName,
            Length = pdfFile.Length
        }, CurrentUserId(), HttpContext.RequestAborted);

        if (!result.Successful)
        {
            ModelState.AddModelError(nameof(pdfFile), result.Error?.Code switch
            {
                InvoiceDocumentErrors.FileTooLarge => T["The PDF is too large. Maximum allowed size is 10 MB."],
                InvoiceDocumentErrors.EmptyFile => T["The file is empty."],
                _ => T["Only PDF files can be imported."]
            });
            return View();
        }

        var document = result.Value!.Document;
        if (result.Value.IsDuplicate) Flash(T["You have already uploaded this PDF. Showing the existing document."], "info");
        else if (document.Status == Domain.EInvoiceDocumentStatus.Processing) Flash(T["PDF uploaded. Its invoices appear here once it has been read."]);
        else Flash(T["PDF saved, but it could not be sent for reading. Use Send again."], "warning");
        return RedirectToAction("Details", "InvoiceDocuments", new { id = document.Id });
    }
}
