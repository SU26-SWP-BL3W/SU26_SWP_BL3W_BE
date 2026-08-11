# Hướng dẫn Kiến trúc Sạch (Clean Architecture Guidelines) - Rhymo Project

Tài liệu này quy định các tiêu chuẩn kiến trúc, cấu trúc thư mục và quy tắc phát triển phần mềm cho dự án Rhymo (.NET 8). Mục tiêu là xây dựng một hệ thống dễ bảo trì, dễ mở rộng và có khả năng kiểm thử cao.

---

## 1. Tổng quan Kiến trúc (Architectural Overview)

Dự án áp dụng **Clean Architecture** kết hợp với **Vertical Slice Architecture** (trong tầng Application). Hệ thống được chia thành 4 lớp chính theo nguyên tắc Dependency Rule: **Sự phụ thuộc chỉ hướng vào bên trong.**

| Layer | Project | Trách nhiệm | Phụ thuộc vào |
| :--- | :--- | :--- | :--- |
| **Domain** | `Rhymo.Domain` | Chứa Entities, Enums, Interfaces cơ bản, Exceptions cốt lõi. Không chứa logic bên thứ ba. | Không có |
| **Application** | `Rhymo.Application` | Chứa logic nghiệp vụ (Use Cases), Commands/Queries (MediatR), DTOs, Mapping, Validators. | Domain |
| **Infrastructure** | `Rhymo.Infrastructure` | Triển khai Repositories, DBContext (EF Core), Mail, Cloud Storage, Logging. | Application, Domain |
| **WebAPI** | `Rhymo.UserService` | Controllers, Middlewares, Configuration, DI Registration. | Infrastructure, Application |

---

## 2. Cấu trúc Thư mục (Folder Structure)

Áp dụng **Vertical Slice Architecture**: Gom nhóm các file theo tính năng (Feature) thay vì gom nhóm theo loại file.

### 2.1. Tầng Application (Trái tim của hệ thống)
Thư mục chính: `Rhymo.Application/Features/{DomainEntity}/{Action}`

```text
Rhymo.Application/
├── Common/                 # Các logic dùng chung (Paging, Result, Exceptions)
├── DTOs/                   # Data Transfer Objects (Request/Response)
├── Interfaces/             # Interfaces cho Repositories, Services, UnitOfWork
├── Features/               # [MỚI] Vertical Slices
│   └── Courses/
│       ├── Commands/
│       │   └── CreateCourse/
│       │       ├── CreateCourseCommand.cs
│       │       ├── CreateCourseCommandHandler.cs
│       │       └── CreateCourseValidator.cs
│       └── Queries/
│           └── GetCourseById/
│               ├── GetCourseByIdQuery.cs
│               └── GetCourseByIdHandler.cs
└── UseCase/                # [HIỆN TẠI] Đang chứa các Features (Sẽ dần chuyển sang Features/)
```

### 2.2. Tầng Domain
```text
Rhymo.Domain/
├── Entity/                 # Các thực thể Database (POCO)
├── Base/                   # BaseEntity, BaseException
└── Enums/                  # Các hằng số, định nghĩa loại
```

### 2.3. Tầng Infrastructure
```text
Rhymo.Infrastructure/
├── Persistence/            # DatabaseContext, Configurations (Fluent API)
├── Repositories/           # Triển khai thực tế của các Interfaces
├── Services/               # Triển khai các dịch vụ bên thứ ba (Cloud, Mail)
└── UnitOfWork/             # Triển khai Unit of Work
```

---

## 3. Quy tắc Đặt tên (Naming Conventions)

| Thành phần | Quy tắc đặt tên | Ví dụ |
| :--- | :--- | :--- |
| **Entity** | Danh từ số nhiều (hoặc đơn) | `User`, `Course`, `Lesson` |
| **Command** | `[Verb][Noun]Command` | `CreateCourseCommand` |
| **Query** | `[Get][Noun][Criteria]Query` | `GetCourseByIdQuery`, `GetUsersPagingQuery` |
| **Handler** | `[Command/QueryName]Handler` | `CreateCourseCommandHandler` |
| **Validator** | `[Command/QueryName]Validator` | `CreateCourseValidator` |
| **DTO (Request)** | `[Action][Noun]Request` | `CreateCourseRequest` |
| **DTO (Response)** | `[Noun]Response` hoặc `[Action][Noun]Response` | `CourseResponse`, `CreateCourseResponse` |
| **Interface** | `I[Name]` | `IUserRepository`, `ICloudStorageService` |

---

## 4. Trách nhiệm của từng Layer (Responsibilities)

### ✅ Tầng Domain:
- Định nghĩa các thuộc tính của thực thể.
- **Nghiêm cấm:** Không phụ thuộc vào bất kỳ thư viện ngoài nào (trừ các thư viện thiết yếu của .NET hoặc định danh như `Guid`).
- **Khuyến khích:** Sử dụng Domain Methods để thực hiện logic nội tại của Entity (Encapsulation).

### ✅ Tầng Application:
- Xử lý luồng nghiệp vụ thông qua MediatR Handlers.
- Thực hiện Mapping giữa Entity và DTO.
- Gọi Repositories để truy vấn/lưu dữ liệu.
- Validate dữ liệu đầu vào (FluentValidation).
- **Nghiêm cấm:** Không viết code truy vấn SQL trực tiếp hoặc gọi DBContext.

### ✅ Tầng Infrastructure:
- Cấu hình quan hệ Database (Fluent API).
- Thực hiện logic lưu trữ (EF Core Repositories).
- Gọi API bên thứ ba.
- **Nghiêm cấm:** Không chứa logic nghiệp vụ (Business Logic).

### ✅ Tầng WebAPI (Presentation):
- Tiếp nhận Request từ Client qua Controller.
- Gửi Command/Query vào MediatR `IMediator.Send()`.
- Cấu hình Dependency Injection.
- Xử lý Global Error thông qua Middleware.

---

## 5. Ví dụ Code Thực tế (Template)

### A. Command (Request)
```csharp
// Rhymo.Application/Features/Courses/Commands/CreateCourse/CreateCourseCommand.cs
public class CreateCourseCommand : IRequest<CreateCourseResponse>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile? ThumbnailUrl { get; set; }
}
```

### B. Handler (Logic)
```csharp
// Rhymo.Application/Features/Courses/Commands/CreateCourse/CreateCourseCommandHandler.cs
public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CreateCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public CreateCourseCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<CreateCourseResponse> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // 1. Logic nghiệp vụ & Kiểm tra
        if (string.IsNullOrEmpty(_currentUser.Id)) 
            throw new UnauthorizedAccessException();

        // 2. Mapping DTO -> Entity
        var course = _mapper.Map<Courses>(request);
        course.AuthorId = _currentUser.Id;

        // 3. Sử dụng Repository qua UnitOfWork
        await _unitOfWork.GetRepository<Courses>().AddAsync(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Trả về Response DTO
        return _mapper.Map<CreateCourseResponse>(course);
    }
}
```

---

## 6. Công nghệ & Pattern sử dụng

- **MediatR:** Tách biệt Controller và Business Logic. Mỗi Action trong Controller chỉ cần gọi `_mediator.Send(command)`.
- **EF Core + Unit of Work:** Sử dụng `IUnitOfWork.GetRepository<T>()` để đảm bảo tính nhất quán dữ liệu (Atomicity).
- **AutoMapper:** Chuyển đổi qua lại giữa Entities và DTOs.
- **FluentValidation:** Validate dữ liệu đầu vào một cách minh bạch.
- **Current User Service:** Truy xuất thông tin người dùng hiện tại từ JWT Token thông qua Interface `ICurrentUser`.

---

## 7. Validation Checklist (Dành cho AI & Dev)

Trước khi xác nhận hoàn thành một tính năng, hãy kiểm tra danh sách sau:

1. [ ] **Layer Dependency:** Code trong `Application` có gọi trực tiếp `DBContext` không? (Nếu có là SAI, phải dùng Repository/UnitOfWork).
2. [ ] **Vertical Slice:** Các file liên quan đến 1 Command (Command, Handler, Validator) có nằm chung trong 1 folder không?
3. [ ] **Naming:** Tên class có đúng hậu tố `Command`, `Query`, `Handler` không?
4. [ ] **DTOs:** Controller có trả về trực tiếp `Entity` từ Database không? (Nếu có là SAI, phải trả về `DTO`).
5. [ ] **Encapsulation:** Logic tính toán thuộc về thực thể đã được đưa vào Domain Method trong Entity chưa?
6. [ ] **Validation:** Đã có `Validator` cho dữ liệu đầu vào chưa?
7. [ ] **Error Handling:** Sử dụng `BaseException` hoặc custom Exceptions thay vì trả về `StatusCode` thủ công trong Handler.
8. [ ] **Async:** Tất cả các tác vụ I/O (Database, API, File) đã sử dụng `await` và `CancellationToken` chưa?

---
*Tài liệu này là tài liệu sống (Living Document) và cần được cập nhật khi có thay đổi về kiến trúc hệ thống.*
