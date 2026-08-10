# Quy trình Phát triển Tính năng (Step-by-Step Development Workflow)

Tài liệu này hướng dẫn quy trình chuẩn để thêm một tính năng mới vào dự án Rhymo, từ việc định nghĩa dữ liệu đến khi xuất bản API.

---

## Bước 1: Định nghĩa Thực thể tại Tầng Domain
Tạo file Entity mới trong `Rhymo.Domain/Entity/`.

- **Quy tắc:** Chỉ chứa thuộc tính và các phương thức logic nội tại (Domain Methods).
- **Ví dụ:** Tạo thực thể `Category.cs`

```csharp
namespace Rhymo.Domain.Entity
{
    public class Category : BaseEntity // Kế thừa từ BaseEntity để có Id, CreatedAt...
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Domain Method: Logic liên quan trực tiếp đến thực thể
        public void UpdateName(string newName) 
        {
            if(string.IsNullOrWhiteSpace(newName)) throw new Exception("Tên không được để trống");
            Name = newName;
        }
    }
}
```

---

## Bước 2: Cấu hình Database (Infrastructure)
Định nghĩa cách Entity ánh xạ xuống Database trong `Rhymo.Infrastructure/Persistence/`.

1.  **Configuration:** Tạo file config (nếu cần logic phức tạp) hoặc viết trực tiếp trong `OnModelCreating`.
2.  **DbContext:** Khai báo `DbSet<Category> Categories { get; set; }` trong `DatabaseContext.cs`.

```csharp
// Trong DatabaseContext.cs
public DbSet<Category> Categories { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Category>(entity => {
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
    });
}
```

---

## Bước 3: Quản lý Repositories & UnitOfWork
Dự án ưu tiên sử dụng **Generic Repository** thông qua `UnitOfWork` để tránh tạo quá nhiều file không cần thiết.

- **Trường hợp thông thường:** KHÔNG tạo Repository mới. Sử dụng trực tiếp:
  `_unitOfWork.GetRepository<Category>().AddAsync(entity);`
- **Trường hợp đặc biệt:** Chỉ tạo Repository riêng (ví dụ `ICategoryRepository`) khi có các câu truy vấn phức tạp (Join nhiều bảng, Raw SQL, tối ưu hóa đặc biệt) mà Generic Repository không đáp ứng được.

---

## Bước 4: Viết Feature (Application - Vertical Slice)
Tạo folder tính năng trong `Rhymo.Application/UseCase/` (hoặc `Features/`). Một tính năng thường gồm: **Command/Query**, **Handler**, **Response DTO**.

**Ví dụ: CreateCategory tính năng**

1.  **Command:** `CreateCategoryCommand.cs` (Chứa input)
2.  **Handler:** `CreateCategoryCommandHandler.cs` (Chứa logic xử lý)

```csharp
// Handler sử dụng UnitOfWork
public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category { Name = request.Name, Description = request.Description };
        
        // Sử dụng Generic Repository từ UnitOfWork
        await _unitOfWork.GetRepository<Category>().AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return category.Id;
    }
}
```

---

## Bước 5: Controller (WebAPI)
Tiếp nhận request và điều hướng vào MediatR.

- Nếu đã có Controller cho thực thể đó (vd: `CategoriesController`), chỉ cần viết thêm Method.
- Nếu chưa có, tạo mới.

```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

---

## Tóm tắt luồng dữ liệu (Data Flow)

1.  **Client** gửi JSON đến **Controller**.
2.  **Controller** đóng gói dữ liệu vào **Command/Query** và gửi cho **MediatR**.
3.  **MediatR** tìm đúng **Handler** để xử lý.
4.  **Handler** thực hiện logic:
    - Kiểm tra nghiệp vụ.
    - Gọi **UnitOfWork** để lấy Repository (Generic hoặc Specific).
    - Thao tác với **Domain Entity**.
    - Lưu thay đổi qua `SaveChangesAsync()`.
5.  **Handler** trả về **DTO** cho Controller.
6.  **Controller** trả về **HTTP Response** cho Client.

---
**Lưu ý quan trọng:** Luôn giữ tầng Application "mỏng" về logic hạ tầng và "dày" về logic nghiệp vụ. Không bao giờ để logic Database rò rỉ vào Controller.
