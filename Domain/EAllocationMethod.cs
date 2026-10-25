namespace Domain;

/// <summary>How an invoice is split between people. Stored as int: only ever append new members.</summary>
public enum EAllocationMethod
{
    Equal = 0,
    Percentage = 1,
    Fixed = 2,
    /// <summary>Amounts come from per-invoice <see cref="InvoiceAllocation"/> overrides. Not selectable on a rule.</summary>
    Manual = 3
}
