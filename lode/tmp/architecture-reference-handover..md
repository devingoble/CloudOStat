\# Vertical Slice Minimal API Architecture — Agent Handover



Self-contained reference for replicating this architecture in any .NET project. Every code example below is taken from a production codebase. Domain names are left in place for concreteness; replace them with your own.



---



\## 1. Core Philosophy



\- \*\*No Controllers.\*\* Every HTTP route is a Minimal API lambda that delegates to an endpoint handler class.

\- \*\*One class per operation.\*\* `Create.cs`, `List.cs`, `Update.cs`, `Delete.cs`, `Restore.cs`, etc. Each is a plain class with constructor injection — no interfaces, no base classes.

\- \*\*Nested records for I/O.\*\* Commands and DTOs are `record` types nested inside the endpoint class. This co-locates the contract with its handler and avoids a separate "Models" folder.

\- \*\*Result pattern everywhere.\*\* Endpoint methods return `Result<T>` (or `Result` for void operations). A single `ToHttpResult()` extension converts to HTTP status codes at the Minimal API boundary.

\- \*\*Static mappers.\*\* Explicit `public static` mapper classes — no source generators.

\- \*\*Specifications for queries.\*\* Ardalis.Specification encapsulates filtering, sorting and pagination outside repositories.



---



\## 2. Folder Structure (per module)



```

\[YourApi].csproj

└── Modules/

&nbsp;   └── \[FeatureName]/

&nbsp;       ├── Endpoints/

&nbsp;       │   ├── Create.cs

&nbsp;       │   ├── List.cs

&nbsp;       │   ├── Update.cs

&nbsp;       │   ├── Delete.cs

&nbsp;       │   ├── Restore.cs

&nbsp;       │   └── GetById.cs

&nbsp;       ├── Mappers/

&nbsp;       │   └── \[Entity]Mapper.cs

&nbsp;       ├── Validators/

&nbsp;       │   ├── Create\[Entity]Validator.cs

&nbsp;       │   ├── Update\[Entity]Validator.cs

&nbsp;       │   ├── List\[Entity]Validator.cs

&nbsp;       │   └── PaginationOptionsValidator.cs

&nbsp;       ├── \[Feature]Module.cs          ← endpoint route mapping

&nbsp;       └── \[Feature]Services.cs        ← DI registration

```



Everything for a feature lives in one directory. Adding or removing a feature is a folder-level operation.



---



\## 3. Foundation Types



These go in a shared library referenced by all API and Core projects.



\### BaseEntity



```csharp

public abstract class BaseEntity

{

&nbsp;   public Guid Id { get; set; }

&nbsp;   public DateTime CreatedTimestamp { get; set; }

&nbsp;   public DateTime ModifiedTimestamp { get; set; }

&nbsp;   public bool IsDeleted { get; set; }

&nbsp;   public DateTime? DeletedTimestamp { get; set; }

}

```



\### IAggregateRoot



```csharp

public interface IAggregateRoot { }

```



\### Repository Interfaces



Thin wrappers over Ardalis.Specification to constrain to aggregate roots:



```csharp

public interface IRepository<T> : IRepositoryBase<T> where T : class, IAggregateRoot { }

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAggregateRoot { }

```



\### AuditableEntityInterceptor



EF Core `SaveChangesInterceptor` that auto-sets timestamps:



```csharp

public class AuditableEntityInterceptor : SaveChangesInterceptor

{

&nbsp;   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(

&nbsp;       DbContextEventData eventData, InterceptionResult<int> result,

&nbsp;       CancellationToken cancellationToken = default)

&nbsp;   {

&nbsp;       UpdateTimestamps(eventData.Context);

&nbsp;       return base.SavingChangesAsync(eventData, result, cancellationToken);

&nbsp;   }



&nbsp;   private static void UpdateTimestamps(DbContext? context)

&nbsp;   {

&nbsp;       if (context is null) return;

&nbsp;       foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())

&nbsp;       {

&nbsp;           if (entry.State == EntityState.Added)

&nbsp;           {

&nbsp;               entry.Entity.CreatedTimestamp = DateTime.UtcNow;

&nbsp;               entry.Entity.ModifiedTimestamp = DateTime.UtcNow;

&nbsp;           }

&nbsp;           else if (entry.State == EntityState.Modified)

&nbsp;           {

&nbsp;               entry.Entity.ModifiedTimestamp = DateTime.UtcNow;

&nbsp;           }

&nbsp;       }

&nbsp;   }

}

```



\### Pagination \& Filtering



```csharp

public record PaginationOptions(bool IsPaginationEnabled = true, int Page = 1, int PageSize = 20);



public record SortField(string FieldName, SortOrderType SortType);



public enum SortOrderType { OrderBy, OrderByDescending, ThenBy, ThenByDescending }



public record BaseFilter(PaginationOptions PaginationOptions, IReadOnlyList<SortField> SortFields);

```



Domain filters extend `BaseFilter`:



```csharp

public record SiteFilter(string SearchString, PaginationOptions PaginationOptions,

&nbsp;   IReadOnlyList<SortField> SortFields) : BaseFilter(PaginationOptions, SortFields);

```



\### PaginationHelper



```csharp

public static class PaginationHelper

{

&nbsp;   public static int DefaultPage => 1;

&nbsp;   public static int DefaultPageSize => 10;



&nbsp;   public static int CalculateTake(BaseFilter f) =>

&nbsp;       f.PaginationOptions.PageSize <= 0 ? DefaultPageSize : f.PaginationOptions.PageSize;



&nbsp;   public static int CalculateSkip(BaseFilter f)

&nbsp;   {

&nbsp;       var page = f.PaginationOptions.Page <= 0 ? DefaultPage : f.PaginationOptions.Page;

&nbsp;       return CalculateTake(f) \* page;

&nbsp;   }



&nbsp;   public static int CalculatePageCount(int totalCount, int pageSize)

&nbsp;   {

&nbsp;       if (pageSize <= 0) pageSize = DefaultPageSize;

&nbsp;       return (int)Math.Ceiling((decimal)totalCount / pageSize);

&nbsp;   }

}

```



\### QueryExtensions — Dynamic Sort



Applies `SortField` list to a specification builder via reflection:



```csharp

public static class QueryExtensions

{

&nbsp;   public static IOrderedSpecificationBuilder<T> OrderBy<T>(

&nbsp;       this ISpecificationBuilder<T> builder, IReadOnlyList<SortField> sortFields)

&nbsp;   {

&nbsp;       IOrderedSpecificationBuilder<T>? ordered = null;

&nbsp;       for (int i = 0; i < sortFields.Count; i++)

&nbsp;       {

&nbsp;           var field = sortFields\[i];

&nbsp;           var prop = typeof(T).GetProperties()

&nbsp;               .FirstOrDefault(p => p.Name.Equals(field.FieldName, StringComparison.InvariantCultureIgnoreCase))

&nbsp;               ?? throw new ArgumentException($"Property '{field.FieldName}' not found on '{typeof(T).Name}'");



&nbsp;           var param = Expression.Parameter(typeof(T));

&nbsp;           var access = Expression.PropertyOrField(param, prop.Name);

&nbsp;           var expr = Expression.Lambda<Func<T, object?>>(Expression.Convert(access, typeof(object)), param);



&nbsp;           if (i == 0)

&nbsp;               ordered = field.SortType == SortOrderType.OrderBy

&nbsp;                   ? builder.OrderBy(expr) : builder.OrderByDescending(expr);

&nbsp;           else

&nbsp;               ordered = field.SortType == SortOrderType.ThenBy

&nbsp;                   ? ordered!.ThenBy(expr) : ordered!.ThenByDescending(expr);

&nbsp;       }

&nbsp;       return ordered!;

&nbsp;   }

}

```



---



\## 4. Result-to-HTTP Bridge



A single extension class eliminates per-endpoint status mapping:



```csharp

public static class ResultsExtensions

{

&nbsp;   /// <summary>

&nbsp;   /// For operations that return a body (Create, Update, List, GetById).

&nbsp;   /// </summary>

&nbsp;   public static IResult ToHttpResult<T>(this Result<T> result) =>

&nbsp;       result.Status switch

&nbsp;       {

&nbsp;           ResultStatus.Ok          => TypedResults.Ok(result.Value),

&nbsp;           ResultStatus.NotFound    => TypedResults.NotFound(),

&nbsp;           ResultStatus.Forbidden   => TypedResults.Forbid(),

&nbsp;           ResultStatus.Unauthorized=> TypedResults.Unauthorized(),

&nbsp;           ResultStatus.Invalid     => TypedResults.BadRequest(result.Errors),

&nbsp;           ResultStatus.Error       => TypedResults.BadRequest(result.Errors),

&nbsp;           \_                        => TypedResults.BadRequest()

&nbsp;       };



&nbsp;   /// <summary>

&nbsp;   /// For void operations (Delete, Restore). Defaults to 204 No Content on success.

&nbsp;   /// </summary>

&nbsp;   public static IResult ToHttpResult(this Result result) =>

&nbsp;       result.Status switch

&nbsp;       {

&nbsp;           ResultStatus.Ok          => TypedResults.NoContent(),

&nbsp;           ResultStatus.NotFound    => TypedResults.NotFound(),

&nbsp;           ResultStatus.Forbidden   => TypedResults.Forbid(),

&nbsp;           ResultStatus.Unauthorized=> TypedResults.Unauthorized(),

&nbsp;           ResultStatus.Invalid     => TypedResults.BadRequest(result.Errors),

&nbsp;           ResultStatus.Error       => TypedResults.BadRequest(result.Errors),

&nbsp;           \_                        => TypedResults.BadRequest()

&nbsp;       };

}

```



\### OpenAPI Metadata Helpers



Fluent extensions for common `Produces` combinations:



```csharp

public static class RouteHandlerBuilderExtensions

{

&nbsp;   // Create / write with auth

&nbsp;   public static RouteHandlerBuilder ProducesOkBadRequestAndForbidden<T>(this RouteHandlerBuilder b) =>

&nbsp;       b.Produces<T>(200).Produces<IEnumerable<string>>(400).Produces(403);



&nbsp;   // List / read (always succeeds if valid)

&nbsp;   public static RouteHandlerBuilder ProducesOkAndBadRequest<T>(this RouteHandlerBuilder b) =>

&nbsp;       b.Produces<T>(200).Produces<IEnumerable<string>>(400);



&nbsp;   // GetById / read that might miss

&nbsp;   public static RouteHandlerBuilder ProducesOkNotFoundAndBadRequest<T>(this RouteHandlerBuilder b) =>

&nbsp;       b.Produces<T>(200).Produces(404).Produces(400);



&nbsp;   // Update with auth

&nbsp;   public static RouteHandlerBuilder ProducesOkNotFoundBadRequestAndForbidden<T>(this RouteHandlerBuilder b) =>

&nbsp;       b.Produces<T>(200).Produces(404).Produces(400).Produces(403);



&nbsp;   // Delete / restore with auth

&nbsp;   public static RouteHandlerBuilder ProducesNoContentNotFoundBadRequestAndForbidden(this RouteHandlerBuilder b) =>

&nbsp;       b.Produces(204).Produces(404).Produces(400).Produces(403);

}

```



---



\## 5. Complete Module Walkthrough — "Sites"



\### 5a. Domain Entity (Core layer)



```csharp

public class Site : BaseEntity, IAggregateRoot

{

&nbsp;   public string TenantId { get; private set; } = null!;

&nbsp;   public string SiteName { get; private set; } = null!;

&nbsp;   public bool IsLive { get; private set; }



&nbsp;   private Site() { } // EF Core



&nbsp;   public Site(string tenantId, string siteName)

&nbsp;   {

&nbsp;       Id = Guid.NewGuid();

&nbsp;       TenantId = Guard.Against.NullOrWhiteSpace(tenantId);

&nbsp;       SiteName = Guard.Against.NullOrWhiteSpace(siteName);

&nbsp;   }



&nbsp;   public void UpdateName(string siteName) =>

&nbsp;       SiteName = Guard.Against.NullOrWhiteSpace(siteName);



&nbsp;   public void Publish() => IsLive = true;

&nbsp;   public void Unpublish() => IsLive = false;



&nbsp;   public void SoftDelete()

&nbsp;   {

&nbsp;       IsDeleted = true;

&nbsp;       DeletedTimestamp = DateTime.UtcNow;

&nbsp;   }



&nbsp;   public void Restore()

&nbsp;   {

&nbsp;       IsDeleted = false;

&nbsp;       DeletedTimestamp = null;

&nbsp;   }

}

```



\### 5b. Specifications (Core layer)



\*\*Filter record:\*\*

```csharp

public record SiteFilter(string SearchString, PaginationOptions PaginationOptions,

&nbsp;   IReadOnlyList<SortField> SortFields) : BaseFilter(PaginationOptions, SortFields);

```



\*\*List specification (with pagination):\*\*

```csharp

public class SiteNameSpecification : Specification<Site>

{

&nbsp;   public SiteNameSpecification(SiteFilter filter)

&nbsp;   {

&nbsp;       if (string.IsNullOrWhiteSpace(filter.SearchString))

&nbsp;           Query.Where(s => s.SiteName != null);

&nbsp;       else

&nbsp;           Query.Where(s => s.SiteName != null \&\& s.SiteName.Contains(filter.SearchString));



&nbsp;       if (filter.SortFields is { Count: > 0 })

&nbsp;           Query.OrderBy(filter.SortFields);



&nbsp;       Query.Skip(PaginationHelper.CalculateSkip(filter))

&nbsp;            .Take(PaginationHelper.CalculateTake(filter));

&nbsp;   }

}

```



\*\*Count specification (no pagination, for total count):\*\*

```csharp

public class SiteNameCountSpecification : Specification<Site>

{

&nbsp;   public SiteNameCountSpecification(SiteFilter filter)

&nbsp;   {

&nbsp;       if (string.IsNullOrWhiteSpace(filter.SearchString))

&nbsp;           Query.Where(s => s.SiteName != null);

&nbsp;       else

&nbsp;           Query.Where(s => s.SiteName != null \&\& s.SiteName.Contains(filter.SearchString));

&nbsp;   }

}

```



\*\*By-ID specification:\*\*

```csharp

public class SiteByIdSpecification : Specification<Site>

{

&nbsp;   public SiteByIdSpecification(Guid siteId, string tenantId)

&nbsp;   {

&nbsp;       Query.Where(s => s.Id == siteId \&\& s.TenantId == tenantId);

&nbsp;   }

}

```



\### 5c. Endpoint: Create



```csharp

public class Create

{

&nbsp;   private readonly IRepository<Site> repository;

&nbsp;   private readonly IValidator<CreateSiteCommand> validator;

&nbsp;   private readonly IMultiTenantContextAccessor multiTenantContextAccessor;



&nbsp;   public Create(IRepository<Site> repository, IValidator<CreateSiteCommand> validator,

&nbsp;       IMultiTenantContextAccessor multiTenantContextAccessor)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(repository);

&nbsp;       ArgumentNullException.ThrowIfNull(validator);

&nbsp;       ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);

&nbsp;       this.repository = repository;

&nbsp;       this.validator = validator;

&nbsp;       this.multiTenantContextAccessor = multiTenantContextAccessor;

&nbsp;   }



&nbsp;   public async Task<Result<SiteDto>> CreateAsync(CreateSiteCommand command, CancellationToken cancellationToken)

&nbsp;   {

&nbsp;       var validation = await validator.ValidateAsync(command, cancellationToken);

&nbsp;       if (!validation.IsValid)

&nbsp;           return Result<SiteDto>.Invalid(validation.AsErrors());



&nbsp;       var tenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

&nbsp;       if (string.IsNullOrWhiteSpace(tenantId))

&nbsp;           return Result<SiteDto>.Forbidden(new\[] { "Tenant context is required." });



&nbsp;       var site = new Site(tenantId, command.SiteName);

&nbsp;       site = await repository.AddAsync(site, cancellationToken);

&nbsp;       return new Result<SiteDto>(SiteMapper.MapToCreateDto(site));

&nbsp;   }



&nbsp;   public record CreateSiteCommand(string SiteName);

&nbsp;   public record SiteDto(Guid Id, string SiteName, bool IsLive, int PageCount);

}

```



\### 5d. Endpoint: List



```csharp

public class List

{

&nbsp;   private readonly IReadRepository<Site> repository;

&nbsp;   private readonly IValidator<ListSitesCommand> validator;



&nbsp;   public List(IReadRepository<Site> repository, IValidator<ListSitesCommand> validator)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(repository);

&nbsp;       ArgumentNullException.ThrowIfNull(validator);

&nbsp;       this.repository = repository;

&nbsp;       this.validator = validator;

&nbsp;   }



&nbsp;   public async Task<Result<ListSitesResponse>> ListAsync(ListSitesCommand command, CancellationToken ct)

&nbsp;   {

&nbsp;       var validation = await validator.ValidateAsync(command, ct);

&nbsp;       if (!validation.IsValid)

&nbsp;           return Result<ListSitesResponse>.Invalid(validation.AsErrors());



&nbsp;       var filter = SiteMapper.MapToFilter(command);

&nbsp;       var totalCount = await repository.CountAsync(new SiteNameCountSpecification(filter), ct);

&nbsp;       var pageCount = PaginationHelper.CalculatePageCount(totalCount, filter.PaginationOptions.PageSize);

&nbsp;       var sites = await repository.ListAsync(new SiteNameSpecification(filter), ct) ?? \[];



&nbsp;       return new Result<ListSitesResponse>(new ListSitesResponse(

&nbsp;           SiteMapper.MapToListResponses(sites), totalCount,

&nbsp;           filter.PaginationOptions.Page, pageCount));

&nbsp;   }



&nbsp;   // Command used internally (from mapper)

&nbsp;   public record ListSitesCommand(string SearchString,

&nbsp;       PaginationOptions? PaginationOptions = null,

&nbsp;       IReadOnlyList<SortField>? SortFields = null)

&nbsp;   {

&nbsp;       public PaginationOptions PaginationOptions { get; init; } =

&nbsp;           PaginationOptions ?? new PaginationOptions(true, 1, 20);

&nbsp;   }



&nbsp;   // Query bound from URL query string via \[AsParameters]

&nbsp;   public record ListSitesQuery(string SearchString,

&nbsp;       bool IsPaginationEnabled = true, int Page = 1, int PageSize = 20);



&nbsp;   public record ListSitesResponse(IReadOnlyList<SiteResponse> Sites,

&nbsp;       int SiteCount, int CurrentPage, int PageCount);

&nbsp;   public record SiteResponse(Guid Id, string SiteName, int PageCount, bool IsLive);

}

```



\*\*Key pattern:\*\* `ListSitesQuery` binds from the URL. The mapper converts it to `ListSitesCommand` which the endpoint handler accepts. This separates HTTP binding concerns from business logic.



\### 5e. Endpoint: Update



```csharp

public class Update

{

&nbsp;   private readonly IRepository<Site> repository;

&nbsp;   private readonly IValidator<UpdateSiteCommand> validator;

&nbsp;   private readonly IMultiTenantContextAccessor multiTenantContextAccessor;



&nbsp;   public Update(IRepository<Site> repository, IValidator<UpdateSiteCommand> validator,

&nbsp;       IMultiTenantContextAccessor multiTenantContextAccessor) { /\* guards + assign \*/ }



&nbsp;   public async Task<Result<SiteDto>> UpdateAsync(UpdateSiteCommand command, CancellationToken ct)

&nbsp;   {

&nbsp;       var validation = await validator.ValidateAsync(command, ct);

&nbsp;       if (!validation.IsValid)

&nbsp;           return Result<SiteDto>.Invalid(validation.AsErrors());



&nbsp;       var tenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

&nbsp;       if (string.IsNullOrWhiteSpace(tenantId))

&nbsp;           return Result<SiteDto>.Forbidden(new\[] { "Tenant context is required." });



&nbsp;       var site = await repository.FirstOrDefaultAsync(new SiteByIdSpecification(command.Id, tenantId), ct);

&nbsp;       if (site is null) return Result<SiteDto>.NotFound();



&nbsp;       site.UpdateName(command.SiteName);

&nbsp;       if (command.IsLive) site.Publish(); else site.Unpublish();

&nbsp;       await repository.UpdateAsync(site, ct);



&nbsp;       return new Result<SiteDto>(SiteMapper.MapToUpdateDto(site));

&nbsp;   }



&nbsp;   public record UpdateSiteCommand(Guid Id, string SiteName, bool IsLive);

&nbsp;   public record SiteDto(Guid Id, string SiteName, bool IsLive, int PageCount);

}

```



\### 5f. Endpoint: Delete (soft)



```csharp

public class Delete

{

&nbsp;   private readonly IRepository<Site> repository;

&nbsp;   private readonly IMultiTenantContextAccessor multiTenantContextAccessor;



&nbsp;   public Delete(IRepository<Site> repository,

&nbsp;       IMultiTenantContextAccessor multiTenantContextAccessor) { /\* guards + assign \*/ }



&nbsp;   public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)

&nbsp;   {

&nbsp;       var tenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

&nbsp;       if (string.IsNullOrWhiteSpace(tenantId))

&nbsp;           return Result.Forbidden(new\[] { "Tenant context is required." });



&nbsp;       var site = await repository.FirstOrDefaultAsync(new SiteByIdSpecification(id, tenantId), ct);

&nbsp;       if (site is null) return Result.NotFound();



&nbsp;       site.SoftDelete();

&nbsp;       await repository.UpdateAsync(site, ct);

&nbsp;       return Result.Success();

&nbsp;   }

}

```



\### 5g. Endpoint: Restore



```csharp

public class Restore

{

&nbsp;   private readonly IRepository<Site> repository;

&nbsp;   private readonly IMultiTenantContextAccessor multiTenantContextAccessor;



&nbsp;   public Restore(IRepository<Site> repository,

&nbsp;       IMultiTenantContextAccessor multiTenantContextAccessor) { /\* guards + assign \*/ }



&nbsp;   public async Task<Result> RestoreAsync(Guid id, CancellationToken ct)

&nbsp;   {

&nbsp;       var tenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

&nbsp;       if (string.IsNullOrWhiteSpace(tenantId))

&nbsp;           return Result.Forbidden(new\[] { "Tenant context is required." });



&nbsp;       // Uses a spec that includes soft-deleted records

&nbsp;       var site = await repository.FirstOrDefaultAsync(

&nbsp;           new SiteByIdWithDeletedSpecification(id, tenantId), ct);

&nbsp;       if (site is null) return Result.NotFound();



&nbsp;       site.Restore();

&nbsp;       await repository.UpdateAsync(site, ct);

&nbsp;       return Result.Success();

&nbsp;   }

}

```



\### 5h. Mapper



```csharp

public static class SiteMapper

{

&nbsp;   public static Create.SiteDto MapToCreateDto(Site site)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(site);

&nbsp;       return new Create.SiteDto(site.Id, site.SiteName, site.IsLive, site.Pages.Count);

&nbsp;   }



&nbsp;   public static Update.SiteDto MapToUpdateDto(Site site)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(site);

&nbsp;       return new Update.SiteDto(site.Id, site.SiteName, site.IsLive, site.Pages.Count);

&nbsp;   }



&nbsp;   public static List.SiteResponse MapToListResponse(Site site)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(site);

&nbsp;       return new List.SiteResponse(site.Id, site.SiteName, site.Pages.Count, site.IsLive);

&nbsp;   }



&nbsp;   public static IReadOnlyList<List.SiteResponse> MapToListResponses(IEnumerable<Site> sites)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(sites);

&nbsp;       return sites.Select(MapToListResponse).ToList();

&nbsp;   }



&nbsp;   public static SiteFilter MapToFilter(List.ListSitesCommand command)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(command);

&nbsp;       return new SiteFilter(command.SearchString,

&nbsp;           new PaginationOptions(command.PaginationOptions.IsPaginationEnabled,

&nbsp;               command.PaginationOptions.Page, command.PaginationOptions.PageSize),

&nbsp;           command.SortFields ?? \[]);

&nbsp;   }



&nbsp;   public static List.ListSitesCommand MapToListCommand(List.ListSitesQuery query)

&nbsp;   {

&nbsp;       ArgumentNullException.ThrowIfNull(query);

&nbsp;       return new List.ListSitesCommand(query.SearchString,

&nbsp;           new PaginationOptions(query.IsPaginationEnabled, query.Page, query.PageSize));

&nbsp;   }

}

```



\### 5i. Validators



```csharp

public class CreateSiteValidator : AbstractValidator<Create.CreateSiteCommand>

{

&nbsp;   public CreateSiteValidator()

&nbsp;   {

&nbsp;       RuleFor(s => s.SiteName).NotEmpty().MaximumLength(250);

&nbsp;   }

}



public class UpdateSiteValidator : AbstractValidator<Update.UpdateSiteCommand>

{

&nbsp;   public UpdateSiteValidator()

&nbsp;   {

&nbsp;       RuleFor(x => x.Id).NotEmpty();

&nbsp;       RuleFor(x => x.SiteName).NotEmpty().MaximumLength(250);

&nbsp;   }

}



public class ListSitesValidator : AbstractValidator<List.ListSitesCommand>

{

&nbsp;   public ListSitesValidator()

&nbsp;   {

&nbsp;       RuleFor(x => x.SearchString).MaximumLength(200);

&nbsp;       RuleFor(x => x.PaginationOptions).NotNull();

&nbsp;       RuleForEach(x => x.SortFields)

&nbsp;           .ChildRules(sort => sort.RuleFor(sf => sf.FieldName).NotEmpty())

&nbsp;           .When(x => x.SortFields != null);

&nbsp;   }

}



public class PaginationOptionsValidator : AbstractValidator<PaginationOptions>

{

&nbsp;   public PaginationOptionsValidator()

&nbsp;   {

&nbsp;       RuleFor(opt => opt.Page).GreaterThan(0);

&nbsp;       RuleFor(opt => opt.PageSize).GreaterThan(0);

&nbsp;   }

}

```



\### 5j. Module (endpoint route mapping)



```csharp

public static class SiteManagerModule

{

&nbsp;   public static WebApplication MapSiteEndpoints(this WebApplication app)

&nbsp;   {

&nbsp;       app.MapPost("/sites", async (Create.CreateSiteCommand command,

&nbsp;           Create createEndpoint, CancellationToken ct) =>

&nbsp;       {

&nbsp;           var result = await createEndpoint.CreateAsync(command, ct);

&nbsp;           return result.ToHttpResult();

&nbsp;       })

&nbsp;       .Accepts<Create.CreateSiteCommand>("application/json")

&nbsp;       .ProducesOkBadRequestAndForbidden<Create.SiteDto>()

&nbsp;       .WithName("CreateSite")

&nbsp;       .WithSummary("Creates a new site");



&nbsp;       app.MapGet("/sites", async (\[AsParameters] List.ListSitesQuery query,

&nbsp;           List listEndpoint, CancellationToken ct) =>

&nbsp;       {

&nbsp;           var command = SiteMapper.MapToListCommand(query);

&nbsp;           var result = await listEndpoint.ListAsync(command, ct);

&nbsp;           return result.ToHttpResult();

&nbsp;       })

&nbsp;       .ProducesOkAndBadRequest<List.ListSitesResponse>()

&nbsp;       .WithName("ListSites");



&nbsp;       app.MapPut("/sites/{id}", async (Guid id, Update.UpdateSiteCommand command,

&nbsp;           Update updateEndpoint, CancellationToken ct) =>

&nbsp;       {

&nbsp;           if (id != command.Id) return TypedResults.BadRequest();

&nbsp;           var result = await updateEndpoint.UpdateAsync(command, ct);

&nbsp;           return result.ToHttpResult();

&nbsp;       })

&nbsp;       .Accepts<Update.UpdateSiteCommand>("application/json")

&nbsp;       .ProducesOkNotFoundBadRequestAndForbidden<Update.SiteDto>()

&nbsp;       .WithName("UpdateSite");



&nbsp;       app.MapDelete("/sites/{id}", async (Guid id,

&nbsp;           Delete deleteEndpoint, CancellationToken ct) =>

&nbsp;       {

&nbsp;           var result = await deleteEndpoint.DeleteAsync(id, ct);

&nbsp;           return result.ToHttpResult();

&nbsp;       })

&nbsp;       .ProducesNoContentNotFoundBadRequestAndForbidden()

&nbsp;       .WithName("DeleteSite");



&nbsp;       app.MapPost("/sites/{id}/restore", async (Guid id,

&nbsp;           Restore restoreEndpoint, CancellationToken ct) =>

&nbsp;       {

&nbsp;           var result = await restoreEndpoint.RestoreAsync(id, ct);

&nbsp;           return result.ToHttpResult();

&nbsp;       })

&nbsp;       .ProducesNoContentNotFoundBadRequestAndForbidden()

&nbsp;       .WithName("RestoreSite");



&nbsp;       return app;

&nbsp;   }

}

```



\### 5k. Services (DI registration)



```csharp

public static class SiteManagerServices

{

&nbsp;   public static IServiceCollection RegisterSiteServices(this IServiceCollection services)

&nbsp;   {

&nbsp;       services.AddSingleton<AuditableEntityInterceptor>();



&nbsp;       // Endpoint handlers — one per operation

&nbsp;       services.AddScoped<Create>();

&nbsp;       services.AddScoped<List>();

&nbsp;       services.AddScoped<Update>();

&nbsp;       services.AddScoped<Delete>();

&nbsp;       services.AddScoped<Restore>();



&nbsp;       // Repositories

&nbsp;       services.AddScoped<IReadRepository<Site>, SitesRepository<Site>>();

&nbsp;       services.AddScoped<IRepository<Site>, SitesRepository<Site>>();



&nbsp;       // Validators (auto-discovers all in assembly)

&nbsp;       services.AddValidatorsFromAssemblyContaining<CreateSiteValidator>();



&nbsp;       return services;

&nbsp;   }

}

```



---



\## 6. Program.cs Wiring



```csharp

var builder = WebApplication.CreateSlimBuilder(args);



builder.AddServiceDefaults();        // OpenTelemetry, health checks, resilience

builder.MapAspireResources();        // DbContext registration (see §7)



builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi("v1");



// Module registration — one call per feature

builder.Services.RegisterSiteServices();

builder.Services.RegisterPageServices();



var app = builder.Build();



// Endpoint mapping — one call per feature

app.MapDefaultEndpoints();           // Health checks

app.MapSiteEndpoints();

app.MapPageEndpoints();



if (app.Environment.IsDevelopment())

{

&nbsp;   app.UseDeveloperExceptionPage();

&nbsp;   app.MapOpenApi("/openapi/{documentName}/openapi.json");

}



app.Run();

```



---



\## 7. Aspire Resource Mapping



Each API project has an `AspireMapping.cs` (or similar) that configures DbContexts:



```csharp

public static class AspireMapping

{

&nbsp;   public static WebApplicationBuilder MapAspireResources(this WebApplicationBuilder builder)

&nbsp;   {

&nbsp;       builder.Services.AddSingleton<AuditableEntityInterceptor>();



&nbsp;       builder.AddCosmosDbContext<SitesContext>("rogue-element-cosmos",

&nbsp;           databaseName: "rogue-element-cosmos-cms",

&nbsp;           configureDbContextOptions: options =>

&nbsp;           {

&nbsp;               options.EnableDetailedErrors();

&nbsp;               if (builder.Environment.IsDevelopment())

&nbsp;                   options.EnableSensitiveDataLogging();

&nbsp;               // Add AuditableEntityInterceptor via service provider

&nbsp;           });



&nbsp;       return builder;

&nbsp;   }

}

```



---



\## 8. Invariants \& Rules



| Rule | Why |

|---|---|

| Endpoint methods return `Result<T>` or `Result` | Uniform error handling; never throw for expected failures |

| `ToHttpResult()` at the Minimal API boundary only | Endpoint handlers stay testable without HTTP concerns |

| Nested records for Commands/DTOs | Co-location; no folder of disconnected model classes |

| Static mapper classes with `ArgumentNullException.ThrowIfNull` | Explicit, debuggable, zero magic |

| One validator per command type | Validators reference nested command types directly |

| `IReadRepository<T>` for queries, `IRepository<T>` for writes | Intention-revealing DI registrations |

| `PaginationOptionsValidator` per module | Reusable; validates Page > 0, PageSize > 0 |

| Specifications never paginate in count specs | Count spec mirrors filter logic minus Skip/Take |

| `\[AsParameters]` on `ListQuery` record | Binds query-string params; mapper converts to command |

| Guard clauses in entity constructors and methods | Domain invariants enforced at the entity, not the API layer |

| Soft delete via `SoftDelete()`/`Restore()` | Entity controls its own lifecycle; no raw property sets |



---



\## 9. NuGet Packages Required



| Package | Purpose |

|---|---|

| `Ardalis.Result` | Result pattern (`Result<T>`, `Result`) |

| `Ardalis.Result.FluentValidation` | `.AsErrors()` bridge |

| `Ardalis.Specification` | Specification pattern base types |

| `Ardalis.Specification.EntityFrameworkCore` | `RepositoryBase<T>` implementation |

| `Ardalis.GuardClauses` | `Guard.Against.\*` in domain entities |

| `FluentValidation` | `AbstractValidator<T>` |

| `FluentValidation.DependencyInjectionExtensions` | `AddValidatorsFromAssemblyContaining<T>()` |



---



\## 10. Checklist for Adding a New Module



1\. \*\*Domain entity\*\* in `\[Domain].Core/Entities/` — inherit `BaseEntity`, implement `IAggregateRoot`, private setters, Guard clauses, `SoftDelete()`/`Restore()`.

2\. \*\*Filter record\*\* in `\[Domain].Core/Specifications/Filters/` — extend `BaseFilter` with domain-specific search fields.

3\. \*\*Specifications\*\* in `\[Domain].Core/Specifications/` — list spec (with pagination), count spec (without), by-id spec, by-id-with-deleted spec.

4\. \*\*Endpoint classes\*\* in `\[Domain].API/Modules/\[Feature]/Endpoints/` — `Create`, `List`, `Update`, `Delete`, `Restore` (+ any domain-specific like `Publish`).

5\. \*\*Mapper\*\* in `Modules/\[Feature]/Mappers/\[Entity]Mapper.cs` — static methods, null checks, maps between all command/DTO/filter types.

6\. \*\*Validators\*\* in `Modules/\[Feature]/Validators/` — one per command + `PaginationOptionsValidator`.

7\. \*\*Module\*\* in `\[Feature]Module.cs` — `Map\[Feature]Endpoints(this WebApplication app)`.

8\. \*\*Services\*\* in `\[Feature]Services.cs` — `Register\[Feature]Services(this IServiceCollection services)`.

9\. \*\*Program.cs\*\* — call `builder.Services.Register\[Feature]Services()` and `app.Map\[Feature]Endpoints()`.



