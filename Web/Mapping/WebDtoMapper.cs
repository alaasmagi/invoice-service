using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;
using DTO.DataAccess.Web.DTO;

namespace Web.Mapping;

/// <summary>
/// Maps DataAccess entities to Web DTOs (and back) through the Domain layer, reusing the registered
/// entity and Web mappers. Loaded navigation properties are carried over so views can show related names.
/// </summary>
public class WebDtoMapper(
    IMapper<Address, AddressEntity> addressEntityMapper,
    IMapper<AddressDto, Address> addressDtoMapper,
    IMapper<AddressContact, AddressContactEntity> addressContactEntityMapper,
    IMapper<AddressContactDto, AddressContact> addressContactDtoMapper,
    IMapper<Contact, ContactEntity> contactEntityMapper,
    IMapper<ContactDto, Contact> contactDtoMapper,
    IMapper<Invoice, InvoiceEntity> invoiceEntityMapper,
    IMapper<InvoiceDto, Invoice> invoiceDtoMapper,
    IMapper<InvoiceAllocation, InvoiceAllocationEntity> allocationEntityMapper,
    IMapper<InvoiceAllocationDto, InvoiceAllocation> allocationDtoMapper,
    IMapper<MonthlyStatement, MonthlyStatementEntity> statementEntityMapper,
    IMapper<MonthlyStatementDto, MonthlyStatement> statementDtoMapper,
    IMapper<MonthlyStatementLine, MonthlyStatementLineEntity> lineEntityMapper,
    IMapper<MonthlyStatementLineDto, MonthlyStatementLine> lineDtoMapper,
    IMapper<Service, ServiceEntity> serviceEntityMapper,
    IMapper<ServiceDto, Service> serviceDtoMapper)
{
    public AddressDto ToDto(AddressEntity entity) => addressDtoMapper.Map(addressEntityMapper.Map(entity))!;

    public ContactDto ToDto(ContactEntity entity) => contactDtoMapper.Map(contactEntityMapper.Map(entity))!;

    public ServiceDto ToDto(ServiceEntity entity) => serviceDtoMapper.Map(serviceEntityMapper.Map(entity))!;

    public AddressContactDto ToDto(AddressContactEntity entity)
    {
        var dto = addressContactDtoMapper.Map(addressContactEntityMapper.Map(entity))!;
        if (entity.Address != null) dto.Address = ToDto(entity.Address);
        if (entity.Contact != null) dto.Contact = ToDto(entity.Contact);
        return dto;
    }

    public InvoiceDto ToDto(InvoiceEntity entity)
    {
        var dto = invoiceDtoMapper.Map(invoiceEntityMapper.Map(entity))!;
        if (entity.Address != null) dto.Address = ToDto(entity.Address);
        if (entity.Service != null) dto.Service = ToDto(entity.Service);
        return dto;
    }

    public InvoiceAllocationDto ToDto(InvoiceAllocationEntity entity)
    {
        var dto = allocationDtoMapper.Map(allocationEntityMapper.Map(entity))!;
        if (entity.Invoice != null) dto.Invoice = ToDto(entity.Invoice);
        if (entity.Contact != null) dto.Contact = ToDto(entity.Contact);
        return dto;
    }

    public MonthlyStatementDto ToDto(MonthlyStatementEntity entity)
    {
        var dto = statementDtoMapper.Map(statementEntityMapper.Map(entity))!;
        if (entity.Contact != null) dto.Contact = ToDto(entity.Contact);
        return dto;
    }

    public MonthlyStatementLineDto ToDto(MonthlyStatementLineEntity entity)
    {
        var dto = lineDtoMapper.Map(lineEntityMapper.Map(entity))!;
        if (entity.MonthlyStatement != null) dto.MonthlyStatement = ToDto(entity.MonthlyStatement);
        return dto;
    }

    public Invoice ToDomain(InvoiceDto dto) => invoiceDtoMapper.Map(dto)!;

    public AddressEntity ToEntity(AddressDto dto) => addressEntityMapper.Map(addressDtoMapper.Map(dto))!;

    public ContactEntity ToEntity(ContactDto dto) => contactEntityMapper.Map(contactDtoMapper.Map(dto))!;

    public ServiceEntity ToEntity(ServiceDto dto) => serviceEntityMapper.Map(serviceDtoMapper.Map(dto))!;

    public AddressContactEntity ToEntity(AddressContactDto dto) => addressContactEntityMapper.Map(addressContactDtoMapper.Map(dto))!;

    public InvoiceEntity ToEntity(InvoiceDto dto) => invoiceEntityMapper.Map(invoiceDtoMapper.Map(dto))!;

    public InvoiceAllocationEntity ToEntity(InvoiceAllocationDto dto) => allocationEntityMapper.Map(allocationDtoMapper.Map(dto))!;

    public MonthlyStatementEntity ToEntity(MonthlyStatementDto dto) => statementEntityMapper.Map(statementDtoMapper.Map(dto))!;
}
