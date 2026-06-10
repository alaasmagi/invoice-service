## ADDED Requirements

### Requirement: DataAccess mappers convert Domain to DataAccess DTO
Each DataAccess mapper class in `DTO/DataAccess/Mapper` SHALL implement all four `IMapper<TDomain, TEntity>` methods with explicit property-by-property mapping between Domain entities and their corresponding DataAccess DTO entities. Navigation collections SHALL NOT be deep-mapped; only scalar and FK properties SHALL be copied.

#### Scenario: Map Address domain to AddressEntity
- **WHEN** `AddressEntityMapper.Map(Address)` is called with a valid `Address`
- **THEN** it SHALL return an `AddressEntity` with `Id`, `Name`, `FullAddress`, and `AppUserId` copied from the source

#### Scenario: Map AddressEntity to Address domain
- **WHEN** `AddressEntityMapper.Map(AddressEntity)` is called with a valid `AddressEntity`
- **THEN** it SHALL return an `Address` with `Id`, `Name`, and `FullAddress` copied from the source

#### Scenario: Map null returns null
- **WHEN** any mapper's `Map` method is called with `null`
- **THEN** it SHALL return `null`

#### Scenario: Map collection delegates to single-item Map
- **WHEN** any mapper's `Map(IEnumerable<T>)` method is called
- **THEN** it SHALL map each item using the single-item `Map` method and return the results

### Requirement: Web mappers convert Domain to Web DTO
Each Web mapper class in `DTO/Web/Mapper` SHALL implement all four `IMapper<TDto, TDomain>` methods with explicit property-by-property mapping between Web DTOs and Domain entities. Navigation collections SHALL NOT be deep-mapped.

#### Scenario: Map Address domain to AddressDto
- **WHEN** `AddressDtoMapper.Map(Address)` is called with a valid `Address`
- **THEN** it SHALL return an `AddressDto` with `Id`, `Name`, `FullAddress`, and `AppUserId` copied

#### Scenario: Map AddressDto to Address domain
- **WHEN** `AddressDtoMapper.Map(AddressDto)` is called with a valid `AddressDto`
- **THEN** it SHALL return an `Address` with `Id`, `Name`, and `FullAddress` copied

### Requirement: All 8 DataAccess mappers are implemented
The following mapper classes SHALL have complete implementations: `AddressEntityMapper`, `AddressContactEntityMapper`, `ContactEntityMapper`, `ContactMonthlyStatementEntityMapper`, `InvoiceEntityMapper`, `InvoiceAllocationEntityMapper`, `MonthlyStatementEntityMapper`, `ServiceEntityMapper`.

#### Scenario: No mapper throws NotImplementedException
- **WHEN** any DataAccess mapper method is invoked
- **THEN** it SHALL NOT throw `NotImplementedException`

### Requirement: All 8 Web mappers are implemented
The following mapper classes SHALL have complete implementations: `AddressDtoMapper`, `AddressContactDtoMapper`, `ContactDtoMapper`, `ContactMonthlyStatementDtoMapper`, `InvoiceDtoMapper`, `InvoiceAllocationDtoMapper`, `MonthlyStatementDtoMapper`, `ServiceDtoMapper` (or `ServiceDto` mapper if named differently).

#### Scenario: No Web mapper throws NotImplementedException
- **WHEN** any Web mapper method is invoked
- **THEN** it SHALL NOT throw `NotImplementedException`

