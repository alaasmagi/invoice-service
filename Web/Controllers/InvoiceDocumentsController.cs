using Base.Contracts.DTO;
using Contracts.Application;
using Contracts.DataAccess;
using Domain;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;

namespace Web.Controllers;

public class InvoiceDocumentsController(
    IInvoiceDocumentService documentService,
    IInvoiceDocumentReviewService reviewService,
    IServiceRepository serviceRepository,
    IAddressRepository addressRepository,
    IMapper<InvoiceDocumentDetailsDto, InvoiceDocument> mapper) : UserScopedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var documents = await documentService.GetAllAsync(CurrentUserId());
        return View(documents.Select(document => mapper.Map(document)!).ToList());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var userId = CurrentUserId();
        var document = await documentService.GetWithRowsAsync(id, userId);
        if (document == null) return NotFound();

        var services = await serviceRepository.GetAllForUserAsync(userId);
        var addresses = await addressRepository.GetAllForUserAsync(userId);
        return View(new InvoiceDocumentDetailsViewModel
        {
            Document = mapper.Map(document)!,
            ServiceOptions = services.OrderBy(s => s.Name).Select(s => new SelectListItem(s.Name, s.Id.ToString())),
            AddressOptions = addresses.OrderBy(a => a.Name).Select(a => new SelectListItem(a.Name, a.Id.ToString())),
            SuggestedServiceId = services.FirstOrDefault(s =>
                string.Equals(s.Name, document.ServiceName, StringComparison.OrdinalIgnoreCase))?.Id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(Guid id)
    {
        var result = await documentService.RetryAsync(id, CurrentUserId(), HttpContext.RequestAborted);
        if (result.Error?.Code == Base.DTO.ErrorDefaults.Codes.NotFound) return NotFound();
        if (!result.Successful) Flash(T["This document cannot be sent again right now."], "warning");
        else if (result.Value!.Status == EInvoiceDocumentStatus.Processing) Flash(T["Sent for reading again."]);
        else Flash(T["The document could not be sent for reading. Try again later."], "danger");
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var result = await documentService.GetDownloadUrlAsync(id, CurrentUserId(), HttpContext.RequestAborted);
        if (!result.Successful) return NotFound();
        return Redirect(result.Value!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, Guid rowId, Guid? serviceId, Guid? addressId)
    {
        if (serviceId == null || addressId == null)
        {
            Flash(T["Choose a service and an address."], "warning");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await reviewService.ResolveRowAsync(id, rowId, serviceId.Value, addressId.Value, CurrentUserId());
        return AfterReview(id, result, T["Row resolved."]);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Discard(Guid id, Guid rowId)
    {
        var result = await reviewService.DiscardRowAsync(id, rowId, CurrentUserId());
        return AfterReview(id, result, T["Row discarded."]);
    }

    private IActionResult AfterReview(Guid id, IMethodResponse<InvoiceDocument> result, string success)
    {
        if (result.Error?.Code == Base.DTO.ErrorDefaults.Codes.NotFound) return NotFound();
        if (!result.Successful) Flash(T["This row has already been handled."], "warning");
        else if (result.Value!.Status == EInvoiceDocumentStatus.Completed) Flash(T["All rows are handled. The document is done."]);
        else Flash(success);
        return RedirectToAction(nameof(Details), new { id });
    }
}
