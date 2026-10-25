using Contracts.Application;
using DataAccess.Context;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Helpers;
using Web.Mapping;
using Web.Models;

namespace Web.Controllers;

/// <summary>Split rules: how invoices of an address (or one of its services) are divided between its residents.</summary>
public class AllocationRulesController(
    AppDbContext context,
    WebDtoMapper mapper,
    IAllocationRuleValidator validator,
    IInvoiceSplitCalculator splitCalculator) : UserScopedControllerBase
{
    private const string Prefix = "Rule";
    private const decimal SampleAmount = 100m;

    public async Task<IActionResult> Index(Guid? addressId, bool showPast = false)
    {
        var userId = CurrentUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = RulesWithDetails().Where(e => e.UserId == userId);
        if (addressId != null) query = query.Where(e => e.AddressId == addressId);
        var entities = await query
            .OrderBy(e => e.Address.Name)
            .ThenBy(e => e.ServiceId != null)
            .ThenBy(e => e.Service!.Name)
            .ThenByDescending(e => e.ValidFrom)
            .ToListAsync();
        var pastCount = entities.Count(e => e.ValidTo != null && e.ValidTo < today);
        if (!showPast) entities = entities.Where(e => e.ValidTo == null || e.ValidTo >= today).ToList();

        return View(new AllocationRuleIndexViewModel
        {
            Rules = entities.Select(mapper.ToDto).ToList(),
            AddressOptions = await AddressOptions(userId),
            AddressId = addressId,
            ShowPast = showPast,
            PastCount = pastCount
        });
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await RulesWithDetails().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entity == null ? NotFound() : View(await BuildDetailsModel(entity, userId));
    }

    public async Task<IActionResult> Create(Guid? addressId, Guid? serviceId, string? returnUrl)
    {
        var userId = CurrentUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rule = new AllocationRuleDto
        {
            AddressId = addressId ?? Guid.Empty,
            ServiceId = serviceId,
            Method = EAllocationMethod.Percentage,
            ValidFrom = new DateOnly(today.Year, today.Month, 1)
        };
        if (addressId != null)
        {
            rule.Shares = (await ActiveResidents(userId, addressId.Value, rule.ValidFrom))
                .Select(resident => new AllocationRuleShareDto { ContactId = resident.Id })
                .ToList();
        }
        return View(await BuildFormModel(rule, userId, returnUrl));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = Prefix)] AllocationRuleDto dto, string? returnUrl)
    {
        var userId = CurrentUserId();
        Normalize(dto);
        await Validate(dto, userId);
        if (!ModelState.IsValid) return View(await BuildFormModel(dto, userId, returnUrl));

        var entity = mapper.ToEntity(dto);
        StampNew(entity, userId);
        foreach (var share in entity.Shares)
        {
            StampNew(share, userId);
            share.AllocationRuleId = entity.Id;
        }
        context.Add(entity);
        await context.SaveChangesAsync();
        Flash(T["Split rule saved. Regenerate statements for affected months to apply it."]);
        return RedirectToLocalOr(returnUrl, nameof(Details), new { id = entity.Id });
    }

    public async Task<IActionResult> Edit(Guid? id, string? returnUrl)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await RulesWithDetails().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        var dto = mapper.ToDto(entity);
        dto.Shares = dto.Shares.OrderBy(share => share.Contact?.FullName).ToList();
        return View(await BuildFormModel(dto, userId, returnUrl));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = Prefix)] AllocationRuleDto dto, string? returnUrl)
    {
        if (id != dto.Id) return NotFound();
        var userId = CurrentUserId();
        var existing = await context.AllocationRules.Include(e => e.Shares).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existing == null) return NotFound();
        Normalize(dto);
        await Validate(dto, userId);
        if (!ModelState.IsValid) return View(await BuildFormModel(dto, userId, returnUrl));

        existing.AddressId = dto.AddressId;
        existing.ServiceId = dto.ServiceId;
        existing.Method = dto.Method;
        existing.ValidFrom = dto.ValidFrom;
        existing.ValidTo = dto.ValidTo;
        StampExisting(existing, userId, existing);
        SyncShares(existing, dto.Shares, userId);
        await context.SaveChangesAsync();
        Flash(T["Split rule updated. Regenerate statements for affected months to apply the change."]);
        return RedirectToLocalOr(returnUrl, nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();
        var userId = CurrentUserId();
        var entity = await RulesWithDetails().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity == null) return NotFound();
        return View(new DeleteViewModel<AllocationRuleDto>
        {
            Item = mapper.ToDto(entity),
            Consequences = [await DescribeFallback(entity, userId), T["Statements that were already generated keep their amounts until you regenerate them."]]
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = CurrentUserId();
        var entity = await context.AllocationRules.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entity != null)
        {
            context.AllocationRules.Remove(entity);
            await context.SaveChangesAsync();
            Flash(T["Split rule deleted. Regenerate statements for affected months to apply the change."]);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Residents of an address on a date, used to prefill share rows when the address changes.</summary>
    [HttpGet]
    public async Task<IActionResult> Residents(Guid addressId, DateOnly? date)
    {
        var userId = CurrentUserId();
        var residents = await ActiveResidents(userId, addressId, date ?? DateOnly.FromDateTime(DateTime.UtcNow));
        return Json(residents.Select(resident => new { id = resident.Id, fullName = resident.FullName }));
    }

    private IQueryable<AllocationRuleEntity> RulesWithDetails() =>
        context.AllocationRules
            .Include(e => e.Address)
            .Include(e => e.Service)
            .Include(e => e.Shares).ThenInclude(e => e.Contact);

    /// <summary>Drops empty share rows and, for Equal, all shares (the form hides them for that method).</summary>
    private static void Normalize(AllocationRuleDto dto)
    {
        dto.Shares = dto.Method == EAllocationMethod.Equal
            ? []
            : dto.Shares.Where(share => share.ContactId != Guid.Empty || share.Value != null).ToList();
    }

    private async Task Validate(AllocationRuleDto dto, Guid userId)
    {
        ClearServerManagedModelState(dto);
        foreach (var key in ModelState.Keys.Where(key => key.StartsWith(Prefix + ".")).ToList())
        {
            var property = key[(Prefix.Length + 1)..];
            if (property is "Id" or "UserId" or "ConcurrencyToken" or "Address" or "Service"
                || property.StartsWith("Address.") || property.StartsWith("Service."))
            {
                ModelState.Remove(key);
            }
        }
        // Rows may have been dropped by Normalize, so their indexes no longer match the posted ones.
        foreach (var key in ModelState.Keys.Where(key => key.StartsWith(Prefix + ".Shares[")).ToList()) ModelState.Remove(key);
        for (var i = 0; i < dto.Shares.Count; i++)
        {
            if (dto.Shares[i].Value == null) ModelState.AddModelError($"{Prefix}.Shares[{i}].Value", T["Enter a value."]);
        }
        if (!ModelState.IsValid) return;

        foreach (var error in await validator.ValidateAsync(mapper.ToDomain(dto), userId))
        {
            var key = error.Field == null ? string.Empty : $"{Prefix}.{error.Field}";
            ModelState.AddModelError(key, await DescribeError(error, userId));
        }
    }

    private async Task<string> DescribeError(AllocationRuleError error, Guid userId) => error.Kind switch
    {
        EAllocationRuleErrorKind.AddressNotFound => T["Select an address."],
        EAllocationRuleErrorKind.ServiceNotFound => T["Selected service was not found."],
        EAllocationRuleErrorKind.ContactNotFound => T["Select a person."],
        EAllocationRuleErrorKind.ValidToBeforeValidFrom => T["The end date can't be before the start date."],
        EAllocationRuleErrorKind.ManualNotAllowed => T["Choose Equal, Percentage or Fixed amount."],
        EAllocationRuleErrorKind.EqualHasShares => T["An equal split doesn't use shares."],
        EAllocationRuleErrorKind.SharesRequired => T["Add at least one person."],
        EAllocationRuleErrorKind.PercentageOutOfRange => T["Enter a percentage between 0 and 100."],
        EAllocationRuleErrorKind.PercentageTotalNot100 => T["The shares add up to {0} instead of 100%.", Fmt.SharePercent(error.Amount ?? 0)],
        EAllocationRuleErrorKind.FixedAmountNotPositive => T["Enter an amount greater than zero."],
        EAllocationRuleErrorKind.DuplicateContact => T["This person is already in the rule."],
        EAllocationRuleErrorKind.ContactNotResident => T["This person doesn't live at the address while the rule is valid."],
        EAllocationRuleErrorKind.OverlappingRule => await DescribeConflict(error.ConflictingRuleId, userId),
        _ => error.Kind.ToString()
    };

    private async Task<string> DescribeConflict(Guid? ruleId, Guid userId)
    {
        var conflict = await context.AllocationRules.FirstOrDefaultAsync(e => e.Id == ruleId && e.UserId == userId);
        return conflict == null
            ? T["Another split rule already covers these dates."]
            : T["Another split rule for the same address and service already covers these dates ({0}). Change its dates or edit it instead.", Fmt.Range(conflict.ValidFrom, conflict.ValidTo)];
    }

    /// <summary>Updates shares in place so the (rule, contact) unique index is never violated mid-save.</summary>
    private void SyncShares(AllocationRuleEntity rule, IReadOnlyList<AllocationRuleShareDto> shares, Guid userId)
    {
        var wanted = shares.ToDictionary(share => share.ContactId, share => share.Value ?? 0m);
        foreach (var share in rule.Shares.Where(share => !wanted.ContainsKey(share.ContactId)).ToList())
        {
            context.AllocationRuleShares.Remove(share);
        }
        foreach (var (contactId, value) in wanted)
        {
            var share = rule.Shares.FirstOrDefault(existing => existing.ContactId == contactId);
            if (share == null)
            {
                share = new AllocationRuleShareEntity { AllocationRuleId = rule.Id, ContactId = contactId, Value = value };
                StampNew(share, userId);
                context.AllocationRuleShares.Add(share);
            }
            else if (share.Value != value)
            {
                share.Value = value;
                StampExisting(share, userId, share);
            }
        }
    }

    private async Task<AllocationRuleFormViewModel> BuildFormModel(AllocationRuleDto rule, Guid userId, string? returnUrl)
    {
        // Always offer one empty row so a person can be added without JavaScript.
        if (rule.Method != EAllocationMethod.Equal || rule.Shares.Count == 0) rule.Shares.Add(new AllocationRuleShareDto());
        return new AllocationRuleFormViewModel
        {
            Rule = rule,
            AddressOptions = await AddressOptions(userId),
            ServiceOptions = (await context.Services.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync())
                .Select(e => new SelectListItem(e.Name, e.Id.ToString())).ToList(),
            ContactOptions = (await context.Contacts.Where(e => e.UserId == userId).OrderBy(e => e.FullName).ToListAsync())
                .Select(e => new SelectListItem(e.FullName, e.Id.ToString())).ToList(),
            ReturnUrl = returnUrl
        };
    }

    private async Task<List<SelectListItem>> AddressOptions(Guid userId) =>
        (await context.Addresses.Where(e => e.UserId == userId).OrderBy(e => e.Name).ToListAsync())
        .Select(e => new SelectListItem(e.Name, e.Id.ToString())).ToList();

    private async Task<List<ContactEntity>> ActiveResidents(Guid userId, Guid addressId, DateOnly date) =>
        await context.AddressContacts
            .Where(e => e.UserId == userId && e.AddressId == addressId
                        && e.StartDate <= date && (e.EndDate == null || e.EndDate >= date))
            .Select(e => e.Contact)
            .Distinct()
            .OrderBy(e => e.FullName)
            .ToListAsync();

    private async Task<AllocationRuleDetailsViewModel> BuildDetailsModel(AllocationRuleEntity entity, Guid userId)
    {
        var rule = mapper.ToDomain(entity);
        var invoices = await context.Invoices.Include(e => e.Address).Include(e => e.Service)
            .Where(e => e.UserId == userId && e.AddressId == entity.AddressId
                        && (entity.ServiceId == null || e.ServiceId == entity.ServiceId)
                        && e.InvoiceDate >= entity.ValidFrom && (entity.ValidTo == null || e.InvoiceDate <= entity.ValidTo))
            .OrderByDescending(e => e.InvoiceDate)
            .Take(20)
            .ToListAsync();
        // Most recent invoice this rule actually decides (a service rule may shadow an address default).
        var otherRules = (await context.AllocationRules.Include(e => e.Shares)
                .Where(e => e.UserId == userId && e.AddressId == entity.AddressId).ToListAsync())
            .Select(mapper.ToDomain)
            .ToList();
        var previewInvoice = invoices.FirstOrDefault(invoice =>
            splitCalculator.ResolveRule(mapper.ToDomain(invoice), otherRules)?.Id == entity.Id);

        var invoice = previewInvoice != null
            ? mapper.ToDomain(previewInvoice)
            : new Invoice { Id = Guid.Empty, AddressId = entity.AddressId, ServiceId = entity.ServiceId ?? Guid.Empty, InvoiceDate = PreviewDate(entity), TotalSum = SampleAmount };
        var residents = await ActiveResidents(userId, entity.AddressId, invoice.InvoiceDate);
        var preview = splitCalculator.Calculate(new InvoiceSplitInput
        {
            Invoice = invoice,
            Rules = [rule],
            ActiveResidentIds = residents.Select(resident => resident.Id).ToList()
        });

        var names = residents.ToDictionary(e => e.Id, e => e.FullName);
        foreach (var share in entity.Shares.Where(share => share.Contact != null)) names.TryAdd(share.ContactId, share.Contact.FullName);
        foreach (var warning in preview.Warnings)
        {
            if (warning.ContactId is { } contactId && names.TryGetValue(contactId, out var name)) warning.ContactName = name;
        }
        return new AllocationRuleDetailsViewModel
        {
            Rule = mapper.ToDto(entity),
            PreviewInvoice = previewInvoice == null ? null : mapper.ToDto(previewInvoice),
            PreviewTotal = invoice.TotalSum,
            Preview = preview,
            ContactNames = names,
            PreviewWarnings = preview.Warnings
        };
    }

    /// <summary>A date the rule is valid on, as close to today as possible.</summary>
    private static DateOnly PreviewDate(AllocationRuleEntity entity)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today < entity.ValidFrom) return entity.ValidFrom;
        return entity.ValidTo != null && today > entity.ValidTo ? entity.ValidTo.Value : today;
    }

    private async Task<string> DescribeFallback(AllocationRuleEntity entity, Guid userId)
    {
        var service = entity.Service?.Name;
        if (entity.ServiceId != null)
        {
            var addressDefault = await context.AllocationRules.AnyAsync(e => e.UserId == userId && e.AddressId == entity.AddressId
                                                                             && e.ServiceId == null && e.Id != entity.Id
                                                                             && e.ValidFrom <= (entity.ValidTo ?? DateOnly.MaxValue)
                                                                             && (e.ValidTo == null || e.ValidTo >= entity.ValidFrom));
            if (addressDefault)
            {
                return T["{0} invoices at {1} will be split by the address's default rule when statements are regenerated.", service!, entity.Address.Name];
            }
            return T["{0} invoices at {1} will be split equally between the residents when statements are regenerated.", service!, entity.Address.Name];
        }
        return T["Invoices at {0} without their own service rule will be split equally between the residents when statements are regenerated.", entity.Address.Name];
    }
}
