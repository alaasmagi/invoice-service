## ADDED Requirements

### Requirement: All contexts registered in DI
`Program.cs` SHALL register both `AppDbContext` and `AppIdentityDbContext` with their respective PostgreSQL connection strings.

#### Scenario: Both contexts are resolvable
- **WHEN** the application starts
- **THEN** both `AppDbContext` and `AppIdentityDbContext` SHALL be resolvable from the DI container

### Requirement: All repositories registered in DI
`Program.cs` SHALL register all repository implementations from `DataAccess` against their corresponding `Contracts/DataAccess` interfaces as scoped services.

#### Scenario: Repository is resolvable
- **WHEN** `IAddressRepository` is requested from DI
- **THEN** it SHALL resolve to `AddressRepository`

### Requirement: All mappers registered in DI
`Program.cs` SHALL register all DataAccess and Web mapper implementations as scoped services against their `IMapper<,>` interfaces.

#### Scenario: DataAccess mapper is resolvable
- **WHEN** `IMapper<Address, AddressEntity>` is requested from DI
- **THEN** it SHALL resolve to `AddressEntityMapper`

#### Scenario: Web mapper is resolvable
- **WHEN** `IMapper<AddressDto, Address>` is requested from DI
- **THEN** it SHALL resolve to `AddressDtoMapper`

### Requirement: All application services registered in DI
`Program.cs` SHALL register all application service implementations against their corresponding `Contracts/Application` interfaces as scoped services.

#### Scenario: Application service is resolvable
- **WHEN** `ICreateAddressService` is requested from DI
- **THEN** it SHALL resolve to `CreateAddressService`

### Requirement: UnitOfWork registered in DI
`Program.cs` SHALL register `DataAccessUow` against its base UoW interface as a scoped service.

#### Scenario: UoW is resolvable
- **WHEN** the UoW interface is requested from DI
- **THEN** it SHALL resolve to `DataAccessUow`

