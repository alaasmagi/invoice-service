using System.Security.Claims;
using Base.Contracts.DTO;
using Contracts.Application;
using Domain;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Web.Authentication;
using Web.Configuration;

namespace Web.Controllers.Api;

[ApiController]
[Route("api/v1/invoice-documents")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class InvoiceDocumentsApiController(
    IInvoiceDocumentService documentService,
    IMapper<InvoiceDocumentDto, InvoiceDocument> mapper) : ControllerBase
{
    // Slightly above the 10 MB file limit, so a file just over it gets the 400 from validation, not a 413.
    private const long MaxRequestBytes = 11 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<InvoiceDocumentDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<InvoiceDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file == null)
        {
            return Error("INVOICE_PDF_MISSING", "Send the PDF as the multipart field 'file'.");
        }

        await using var content = file.OpenReadStream();
        var result = await documentService.UploadAsync(new InvoiceDocumentUploadRequest
        {
            Content = content,
            FileName = file.FileName,
            Length = file.Length
        }, CurrentUserId(), ct);

        if (!result.Successful || result.Value == null)
        {
            return Error(result.Error?.Code, result.Error?.Message);
        }

        var dto = mapper.Map(result.Value.Document)!;
        return result.Value.IsDuplicate ? Ok(dto) : Accepted(dto);
    }

    private Guid CurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private BadRequestObjectResult Error(string? code, string? message)
    {
        var problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = message };
        problem.Extensions["code"] = code;
        return BadRequest(problem);
    }
}
