# پرامپت گام ۰ — اسکلت راه‌رونده

> این متن را به‌عنوان پرامپت به AI کدنویس بده. پیش از اجرا، مطمئن شو `AGENTS.md` و `DECISIONS.md` در دسترس آن هستند.

---

تو روی مخزن `nafasland-service-admin` کار می‌کنی. پیش از هر کاری `AGENTS.md` را بخوان و همهٔ قواعدش را رعایت کن. جزئیات هر تصمیم در `DECISIONS.md` با شمارهٔ ADR آمده است.

## هدف این مرحله

ساختن **اسکلت راه‌رونده**: یک برنامهٔ حداقلی که تمام تصمیم‌های زیرساختی در آن واقعاً کار می‌کنند. هیچ منطق محصولی و هیچ تماسی با API پرتال در این مرحله نوشته نمی‌شود.

## استک

- ASP.NET Core روی .NET 10 LTS، C#
- EF Core با SQL Server
- Serilog برای لاگ ساختاریافته
- FluentValidation
- Hangfire یا Quartz به‌عنوان job runner (فقط وایر شود، بدون job واقعی)

## ساختار پوشه‌ها (ADR-004)

```
src/
├─ Api/                  host و Program.cs
├─ Shared/
│  ├─ Kernel/            Result<T>, IModule, IDomainEvent, PermissionDefinition, base types
│  └─ Infrastructure/    pipeline behaviors، permission handler، Options، لاگ
└─ Modules/
   └─ Sample/            یک ماژول نمونه، فقط برای اثبات کارکرد اسکلت
tests/
└─ <پروژه‌های تست متناظر>
```

## آنچه باید ساخته شود

### ۱. `IModule` و ثبت خودکار (ADR-005)

```csharp
public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration config);
    void MapEndpoints(IEndpointRouteBuilder app);
    IReadOnlyList<PermissionDefinition> Permissions { get; }
}
```

`Program.cs` اسمبلی‌ها را اسکن و همهٔ `IModule`ها را رجیستر می‌کند. permissionها از همین‌جا جمع و در استارتاپ seed می‌شوند. کلید permission هر ماژول به‌صورت const داخل خودش تعریف می‌شود (`SamplePermissions.Ping`)، نه در enum مرکزی.

### ۲. Pipeline اجباری (ADR-006)

ترتیب دقیق: `Logging → Validation → Authorization → Transaction → Audit → Handler`.

- `AuthorizationBehavior` اگر روی command هیچ permission تعریف‌شده‌ای نبود، درخواست را **رد** می‌کند. پیش‌فرض بسته.
- `AuditBehavior` در این مرحله فقط یک اسکلت است که در گام ۲ پر می‌شود؛ ولی جایگاهش در pipeline همین حالا تثبیت شود.
- چک دسترسی با Authorization Policy و یک `IAuthorizationHandler` که permission را از claims می‌خواند (ADR-001). هرگز `[Authorize(Roles = ...)]`.

### ۳. قرارداد خطا و CorrelationId (ADR-036)

- همهٔ خطاها با ProblemDetails (RFC 7807) به‌علاوهٔ فیلد `correlationId` برگردانده شوند.
- خطای اعتبارسنجی، فهرست خطا به تفکیک فیلد داشته باشد.
- `CorrelationId` در ابتدای هر درخواست ساخته شود یا از هدر `X-Correlation-Id` خوانده شود، در همهٔ لاگ‌ها منتشر شود و در هدر پاسخ برگردد.

### ۴. کانفیگ و اسرار (ADR-039)

- همهٔ کانفیگ‌ها با Options pattern و اعتبارسنجی در استارتاپ خوانده شوند. کانفیگ نامعتبر یعنی برنامه بالا نمی‌آید.
- در این مرحله دست‌کم این‌ها تعریف شوند: رشتهٔ اتصال، تنظیمات پرتال (آدرس پایه، نرخ، شناسهٔ محصول تستی)، تنظیمات لاگ.
- هیچ مقدار حساسی در فایل کانفیگ مخزن نباشد. `appsettings.Development.json` فقط مقادیر بی‌خطر داشته باشد و User Secrets وایر شود.

### ۵. پایگاه داده و مهاجرت (ADR-019، ADR-041)

- `DbContext` با schema جداگانه برای ماژول نمونه.
- اولین مهاجرت ساخته شود.
- **مهاجرت در استارتاپ اجرا نشود.** برنامه فقط بررسی کند مهاجرت معوقی نمانده باشد و در صورت وجود، با پیام روشن بالا نیاید.

### ۶. رصدپذیری (ADR-042)

- Serilog با خروجی JSON و چرخش روزانهٔ فایل، شامل `CorrelationId`، شناسهٔ کاربر، نام ماژول و مدت زمان.
- اندپوینت `/health` با بررسی دسترسی به پایگاه داده و وضعیت job runner.
- پاک‌سازی مقادیر حساس در تنظیمات لاگر.

### ۷. تصمیم‌های ارزان امروز (ADR-012)

- همهٔ اندپوینت‌ها زیر `/api/v1/`.
- feature flag به ازای هر ماژول، طوری که ماژول خاموش اصلاً رجیستر نشود.
- job runner وایر شود.

### ۸. ماژول نمونه

یک ماژول `Sample` با یک command ساده (مثلاً `PingCommand`) که:

- یک permission به نام `sample.ping` تعریف می‌کند،
- یک Validator دارد،
- یک رکورد در جدول خودش می‌نویسد تا تراکنش و مهاجرت واقعاً امتحان شوند،
- و از طریق `POST /api/v1/sample/ping` در دسترس است.

به‌علاوه یک command دوم **بدون** permission تعریف‌شده، صرفاً برای اثبات اینکه `AuthorizationBehavior` آن را رد می‌کند.

## معیار پذیرش

این‌ها باید قابل نمایش باشند:

1. `dotnet build` و `dotnet test` سبز.
2. `POST /api/v1/sample/ping` با permission مناسب کار می‌کند و رکورد در دیتابیس ثبت می‌شود.
3. همان درخواست بدون permission، `403` با ProblemDetails و `correlationId` می‌دهد.
4. command بدون permission تعریف‌شده، **رد** می‌شود (نه اینکه اجرا شود).
5. خطای اعتبارسنجی، `400` با فهرست خطا به تفکیک فیلد می‌دهد.
6. `GET /health` سبز است و وضعیت دیتابیس را نشان می‌دهد.
7. با یک مهاجرت اعمال‌نشده، برنامه بالا نمی‌آید و پیام روشن می‌دهد.
8. با پاک کردن یک مقدار کانفیگ اجباری، برنامه در استارتاپ خطای روشن می‌دهد.
9. در لاگ‌ها، `CorrelationId` یک درخواست در همهٔ خطوط مربوط به آن دیده می‌شود.
10. خاموش کردن feature flag ماژول نمونه، اندپوینت‌هایش را حذف می‌کند.

## آنچه در این مرحله **نباید** انجام شود

- هیچ تماسی با API پرتال، هیچ `IPortalProductClient`.
- هیچ موجودیت محصول، واریانت، سفارش یا دسته‌بندی.
- هیچ فرانت‌اندی.
- هیچ مدل کامل کاربر و نقش — آن گام ۱ است. در این مرحله یک کاربر ساختگی با claims ثابت برای تست کافی است.
- هیچ کتابخانهٔ اضافه‌ای بدون بررسی `DECISIONS.md`.

## خروجی

- کد در همین مخزن.
- یک کامیت با فرمت Conventional Commits که بدنه‌اش **دلیل** ساختار انتخاب‌شده را توضیح دهد، نه فهرست فایل‌ها.
- یک `README.md` کوتاه با نحوهٔ اجرای پروژه، اعمال مهاجرت، و متغیرهای محیطی لازم.
- اگر جایی از `DECISIONS.md` مبهم بود یا با واقعیت پیاده‌سازی نخواند، **حدس نزن** — همان مورد را فهرست کن و بپرس.
