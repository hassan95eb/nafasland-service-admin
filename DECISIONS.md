# تصمیم‌های پروژهٔ پنل مدیریت نفسلند

این فایل مرجع تصمیم‌های مورد توافق پروژه است. هر تصمیم جدید پس از توافق، با تاریخ، دلیل و پیامدهای آن در همین فایل ثبت می‌شود. پیشنهادهای بررسی‌نشده نباید به‌عنوان تصمیم قطعی ثبت شوند. کلیدها، توکن‌ها و رمزها در این فایل یا Git ثبت نمی‌شوند.

## ۲۰۲۶-۰۹-۱۹ — هدف و معماری مورد توافق

- ساخت پنل مستقل برای کارکنان، با فرانت‌اند و بک‌اند جدا از سایت اصلی.
- ارتباط با نفسلند از طریق API پرتال؛ پوشش دقیق عملیات نیازمند بررسی API واقعی است.
- بک‌اند: ASP.NET Core؛ مبنای پیشنهادی قبلی .NET 10 LTS است.
- بک‌اند اختصاصی مسئول کنترل مجوزها و ارتباط با API نفسلند است؛ توکن اصلی API فقط در بک‌اند نگهداری می‌شود.
- حساب و مجوز مستقل برای کارکنان؛ امکان ایجاد و ویرایش محصول بدون مجوز حذف.
- کنترل مجوز در بک‌اند ضروری است؛ پنهان‌کردن دکمه‌ها در فرانت‌اند کنترل امنیتی کافی نیست.
- محدودیت ویرایش فیلدها و حذف دسترسی مستقیم غیرمجاز به سایت اصلی باید در طراحی لحاظ شود.

## ۲۰۲۶-۰۹-۱۹ — انتخاب فرانت‌اند

وضعیت: انتخاب مطرح‌شده توسط کاربر و مورد موافقت در این گفتگو.

- Next.js به‌جای پیشنهاد اولیهٔ Angular.
- shadcn/ui برای اجزای رابط کاربری.
- Tailwind CSS برای طراحی و استایل‌ها.
- React Query برای دریافت داده، کش و همگام‌سازی وضعیت سمت سرور.
- دلیل: ساخت رابط قابل سفارشی‌سازی برای فرم‌ها، جدول‌ها و جریان کاری کارکنان.
- انتخاب Next.js مسئولیت مجوزها و منطق کسب‌وکار را از ASP.NET Core منتقل نمی‌کند.

## ۲۰۲۶-۰۹-۱۹ — نقش‌ها و مجوزها

وضعیت: مطرح‌شده توسط کاربر و مورد توافق در گفتگو.

- پنل چند کاربره است و چند ادمین به‌طور هم‌زمان از آن استفاده می‌کنند.
- نقش SuperAdmin: دسترسی کامل به همهٔ عملیات پنل.
- نقش Admin: فقط ایجاد و ویرایش محصول؛ مجوز حذف محصول را ندارد.
- نقش نویسندهٔ محتوا/وبلاگ در آینده افزوده می‌شود؛ طراحی مدل نقش و مجوز باید از ابتدا قابل توسعه باشد و افزودن نقش جدید نیازمند تغییر ساختار نباشد.
- اعمال مجوزها در بک‌اند انجام می‌شود؛ فرانت‌اند فقط رابط کاربر را متناسب با نقش نمایش می‌دهد.

## ۲۰۲۶-۰۹-۱۹ — ثبت وقایع (Audit Log)

وضعیت: نیازمندی اعلام‌شده توسط کاربر و مورد توافق در گفتگو.

- همهٔ اقدام‌های ادمین‌ها باید ثبت شود؛ این یک نیازمندی اصلی پروژه است، نه امکان جانبی.
- برای هر رویداد دست‌کم ثبت شود: کاربر انجام‌دهنده، نوع عملیات (ایجاد/ویرایش/حذف/ورود)، موجودیت و شناسهٔ هدف، زمان، و تغییر انجام‌شده (مقدار قبل و بعد).
- سوابق در بک‌اند اختصاصی نگهداری می‌شوند و از داخل پنل قابل ویرایش یا حذف نیستند.
- ثبت وقایع باید برای ادمین‌ها قابل مرور و جست‌وجو باشد (فیلتر بر اساس کاربر، تاریخ و نوع عملیات).

## تصمیم‌های ثبت‌شده (ADR)

از این بخش به بعد هر تصمیم به‌صورت یک ADR شماره‌دار ثبت می‌شود. ورودی‌های بالاتر دست‌نخورده باقی مانده‌اند و شماره‌گذاری از ADR-001 آغاز می‌شود.

### ADR-001 — مدل دسترسی permission-based به‌جای role-based

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**زمینه:** بخش‌های زیادی (محتوا، انبارداری) بعداً به پنل اضافه می‌شوند و هر بخش نقش‌های خودش را می‌آورد.

**تصمیم:**
- چک دسترسی همیشه روی permission انجام می‌شود، نه روی نام نقش.
- ساختار داده: `AppUser ──< UserRole >── Role ──< RolePermission >── Permission`.
- کلید permission به فرمت `<resource>.<action>`.
- در ASP.NET با Authorization Policy و یک `IAuthorizationHandler` که permission را از claims می‌خواند.

**دلیل:** با مدل نقش‌محور، هر نقش جدید یعنی تغییر کد و دیپلوی؛ با permission، یعنی چند ردیف داده.

**گزینهٔ رد‌شده:** `[Authorize(Roles = "Admin")]` — هزینه‌اش با نقش سوم و چهارم ترکیبی می‌شود.

### ADR-002 — حذف محصول فقط در اختیار SuperAdmin

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:**
- permission با کلید `product.delete` منحصراً به نقش SuperAdmin بسته است.
- اندپوینت حذف در بک‌اند برای سایر نقش‌ها ۴۰۳ برمی‌گرداند و درخواست به API سایت اصلی فوروارد نمی‌شود.
- روی موجودیت `Permission` فلگ `IsSuperAdminOnly` اضافه می‌شود تا چنین permissionهایی از صفحهٔ مدیریت نقش‌ها قابل انتساب به نقش دیگری نباشند.

**دلیل:** مخفی‌کردن دکمه در UI کنترل امنیتی نیست. بدون این فلگ، سوپرادمین می‌تواند ماه‌ها بعد سهواً همین محدودیت را از صفحهٔ نقش‌ها دور بزند.

**پیامد:** تلاش رد‌شده برای حذف با `Outcome = Denied` در AuditLog ثبت می‌شود.

### ADR-003 — هر کاربر می‌تواند چند نقش داشته باشد

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:** `UserRole` به‌صورت many-to-many تعریف می‌شود. permissionهای مؤثر کاربر برابر است با اتحاد permissionهای همهٔ نقش‌هایش.

**دلیل:** یک نفر عملاً هم‌زمان ادمین محصول و انباردار یا محتوانویس خواهد بود.

### ADR-004 — Modular Monolith با مرز عمودی

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:** ساختار پروژه بر اساس دامنه چیده می‌شود، نه لایهٔ سراسری:

```
src/
├─ Api/                  host و Program.cs
├─ Shared/
│  ├─ Kernel/            Result<T>, IModule, IDomainEvent, base types
│  └─ Infrastructure/    auth، permission handler، audit interceptor، HTTP resilience
└─ Modules/
   ├─ Identity/          کاربر، نقش، permission، لاگین
   ├─ Auditing/          AuditLog و خواندن/فیلتر آن
   ├─ Catalog/           محصول (روی API سایت اصلی)
   ├─ Approvals/         جریان تأیید (عمومی)
   ├─ Content/           وبلاگ — آینده
   └─ Inventory/         انبار — آینده
```

قواعد الزامی:
- هیچ ماژولی به کلاس داخلی ماژول دیگر رفرنس نمی‌دهد؛ فقط به `Contracts` آن. دسترسی `internal` جدی گرفته می‌شود.
- هر ماژول اسکیمای دیتابیس خودش را دارد (`identity.`، `audit.`، `catalog.`، `inventory.`) با مایگریشن جداگانه.
- ارتباط بین ماژول‌ها با رویداد درون‌فرآیندی (مثل `ProductUpdated`) انجام می‌شود، نه فراخوانی مستقیم سرویس ماژول دیگر.

**دلیل:** Onion سراسری با پنج دامنهٔ بی‌ربط، هر لایه را به انبار پوشه‌های نامرتبط تبدیل می‌کند و تغییر در انبار، کد محصول را می‌شکند.

**گزینهٔ رد‌شده:** میکروسرویس — برای این مقیاس هزینهٔ عملیاتی بی‌دلیل است؛ مرز اسکیما جداسازی بعدی را ارزان نگه می‌دارد.

### ADR-005 — ثبت خودکار ماژول با IModule

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:**

```csharp
public interface IModule
{
    void RegisterServices(IServiceCollection services, IConfiguration config);
    void MapEndpoints(IEndpointRouteBuilder app);
    IReadOnlyList<PermissionDefinition> Permissions { get; }
}
```

- `Program.cs` اسمبلی‌ها را اسکن و همهٔ `IModule`ها را رجیستر می‌کند.
- permissionها از همین‌جا جمع و در استارتاپ seed می‌شوند.
- کلید permission هر ماژول به‌صورت const داخل خودش تعریف می‌شود (`InventoryPermissions.StockAdjust`)، نه در یک enum مرکزی.

**دلیل:** اضافه‌کردن ماژول انبار باید یک پوشه باشد، نه دست‌زدن به هفت فایل. permissionهای ماژول جدید خودشان در صفحهٔ مدیریت نقش‌ها ظاهر می‌شوند.

### ADR-006 — Vertical Slice با pipeline اجباری

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:**
- هر فیچر یک پوشه شامل Command، Handler، Validator و Endpoint است.
- همهٔ commandها از یک pipeline عبور می‌کنند: `Logging → Validation → Authorization → Transaction → Audit → Handler`.
- `AuthorizationBehavior` اگر روی command هیچ permission تعریف‌شده‌ای نبود، درخواست را **رد** می‌کند (پیش‌فرض بسته، نه باز).

**دلیل:** لاگ و چک دسترسی یک بار نوشته می‌شوند و هر فیچر آینده به‌طور خودکار لاگ‌دار و محافظت‌شده متولد می‌شود. لاگ داخل کنترلر یعنی اولین اندپوینت انبار که با عجله اضافه شود بی‌لاگ می‌ماند.

### ADR-007 — توکن API سایت اصلی هرگز به مرورگر نمی‌رسد

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:**
- فرانت فقط با بک‌اند ASP.NET حرف می‌زند؛ بک‌اند به‌عنوان BFF/پروکسی با توکن سرویس‌اکانت به API سایت اصلی وصل می‌شود.
- سشن پنل در کوکی httpOnly نگه داشته می‌شود؛ توکن در `localStorage` ذخیره نمی‌شود.

**دلیل:** اگر فرانت مستقیم به API اصلی بزند، محدودیت حذف محصول بی‌معنی است — ادمین از DevTools خودش `DELETE` می‌زند.

### ADR-008 — لایهٔ ضد فساد (ACL) روی API پرتال

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:**
- `IPortalProductClient` در `Catalog/Contracts` تعریف می‌شود، با پیاده‌سازی typed `HttpClient` + Polly (timeout، retry فقط روی عملیات idempotent، circuit breaker).
- DTOهای پرتال در مرز ماژول به مدل داخلی map می‌شوند و هرگز به کنترلر یا فرانت نمی‌رسند.
- توکن در `IPortalTokenProvider` با کش و رفرش خودکار نگهداری می‌شود.

**دلیل:** تنها بخش خارج از کنترل ما همین است؛ تغییر قرارداد پرتال باید یک mapper را بشکند، نه کل پنل.

### ADR-009 — ساختار AuditLog

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:** جدول فقط append است (هیچ اندپوینت update/delete، حتی برای SuperAdmin). فیلدها:

```
Id, CorrelationId, ActorUserId, OnBehalfOfUserId?, ActorRoleAtTime, Action,
EntityType, EntityId, BeforeJson, AfterJson, ChangedFields,
Outcome (Success/Failed/Denied), UpstreamStatus?, FailureReason?,
IpAddress, UserAgent, CreatedAt
```

قواعد:
- قبل از هر آپدیت، نسخهٔ فعلی از API سایت اصلی خوانده و در `BeforeJson` ذخیره می‌شود.
- تلاش‌های ناموفق و رد‌شده هم لاگ می‌شوند.
- نوشتن لاگ در `AuditBehavior` انجام می‌شود، نه در کنترلرها.
- همراه آن `ProductRef (ExternalProductId, LastKnownTitle, LastSyncedAt)` نگه داشته می‌شود.

**دلیل:** منبع حقیقت محصول بیرونی است؛ بدون `BeforeJson` معلوم نمی‌شود چه چیزی عوض شد، و بدون `ProductRef` لاگ‌های قدیمی به «محصول ۸۴۲۱» تبدیل می‌شوند.

### ADR-010 — ماژول عمومی Approvals برای عملیات نیازمند تأیید

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:** حذف محصول از مسیر درخواست/تأیید ثبت‌شده در سیستم انجام می‌شود. ماژول `Approvals` هیچ دانشی از دامنه‌های دیگر ندارد؛ هر ماژول یک مجری رجیستر می‌کند:

```csharp
public interface IApprovalExecutor
{
    string RequestType { get; }          // "catalog.product.delete"
    string RequiredPermission { get; }
    Task<ApprovalPreview> PreviewAsync(string payload, CancellationToken ct);
    Task<Result> ExecuteAsync(string payload, ApprovalContext ctx, CancellationToken ct);
}
```

موجودیت:

```
ApprovalRequest(Id, RequestType, TargetEntityType, TargetEntityId, PayloadJson,
SnapshotJson, Reason, Status, RequestedByUserId, RequestedAt, ReviewedByUserId?,
ReviewedAt?, ReviewNote?, ExecutedAt?, ExecutionError?, ExpiresAt?, RowVersion)
```

با `Status`: `Pending / Approved / Rejected / Executed / ExecutionFailed / Cancelled / Expired`

قواعد الزامی:
1. `PayloadJson` همان command سریالایزشده است و تأیید یعنی دیسپچ همان command از pipeline معمول. **نوع command از `RequestType` و رجیستری resolve می‌شود، هرگز از نام کلاس داخل JSON** (دیسریالایز پولیمورفیک با type name داخل payload یعنی اجرای دلخواه کد).
2. در لحظهٔ اجرا، permission تأییدکننده چک می‌شود. ادمین فقط `product.delete.request` دارد. اگر دسترسی تأییدکننده عوض شده باشد، درخواست Pending رد می‌شود نه اجرا.
3. تصمیم یک‌بار و ترمینال است؛ گذار وضعیت داخل تراکنش با `RowVersion` انجام می‌شود.
4. `Approved` و `Executed` جدا هستند. شکست تماس آپ‌استریم → `ExecutionFailed` با خطای ذخیره‌شده و تلاش مجدد دستی. **ریتری خودکار روی حذف ممنوع است.**
5. `PreviewAsync` وضعیت فعلی موجودیت را زنده می‌خواند، نه از `SnapshotJson`.
6. درخواست Pending پس از ۷ روز `Expired` می‌شود (job زمان‌بندی‌شده).

permissionها:

```
product.delete.request   → Admin
product.delete           → SuperAdmin
approval.review          → SuperAdmin
approval.read.own        → همه نقش‌ها
approval.read.all        → SuperAdmin
```

یادداشت (۲۰۲۶-۰۹-۱۹، تأیید کاربر): در حال حاضر فقط دو نقش وجود دارد — `Admin` و `SuperAdmin`. نام `ProductAdmin` استفاده نمی‌شود. ادمین هیچ مسیری برای حذف محصول ندارد و تنها می‌تواند درخواست حذف ثبت کند؛ اجرای حذف فقط پس از تأیید SuperAdmin انجام می‌شود.

اندپوینت‌ها:

```
POST   /api/v1/approvals                      ثبت درخواست (reason اجباری)
GET    /api/v1/approvals?status=&type=&mine=
GET    /api/v1/approvals/{id}                 جزئیات + preview زنده
POST   /api/v1/approvals/{id}/approval        (approval.review)
POST   /api/v1/approvals/{id}/rejection       (note اجباری)
POST   /api/v1/approvals/{id}/cancellation    فقط درخواست‌دهنده، فقط Pending
POST   /api/v1/approvals/{id}/retry           پس از ExecutionFailed
```

رویدادهای AuditLog: `ApprovalRequested`، `ApprovalApproved`، `ApprovalRejected`، `ApprovalExecuted`، `ApprovalExecutionFailed`، `ApprovalCancelled`، `ApprovalExpired` — در رکورد اجرا هم `ActorUserId` (تأییدکننده) و هم `OnBehalfOfUserId` (درخواست‌دهنده) ثبت می‌شود، با `ApprovalRequestId` در `CorrelationId`.

**دلیل:** انبارداری همین جریان را برای اصلاح موجودی لازم دارد؛ ماژول عمومی از `if (type == ...)` جلوگیری می‌کند. `ApprovalRequest` یک ماشین وضعیت است و با AuditLog که فقط append است ادغام نمی‌شود.

### ADR-011 — ساختار فرانت‌اند

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:**
- Next.js App Router با route group بر اساس دسترسی (`app/(panel)/products | inventory | content | admin`)، و چک permission در layout سمت سرور قبل از رندر.
- چیدمان `features/<domain>/{api, components, hooks, schemas}` و `shared/{ui, lib, permissions, data-table, crud}`.
- ناوبری به‌صورت داده: آرایهٔ `navItems` با فیلد `permission`؛ سایدبار از روی آن رندر و فیلتر می‌شود.
- تایپ‌ها از OpenAPI بک‌اند تولید می‌شوند (`openapi-typescript`/NSwag).
- `DataTable` و الگوی `CrudPage` مشترک روی shadcn؛ zod به‌عنوان منبع مشترک فرم و اعتبارسنجی، آینه‌شده در بک‌اند با FluentValidation.
- React Query با key factory به ازای هر فیچر؛ بعد از mutation فقط invalidate، **بدون optimistic update** (منبع حقیقت بیرونی است).
- RTL و فونت فارسی از ابتدا در `globals.css` و `dir="rtl"`.
- کامپوننت `<Can permission="...">` فقط برای UI است؛ چک واقعی در بک‌اند انجام می‌شود.
- ادمین به‌جای دکمهٔ حذف، «درخواست حذف» با دلیل اجباری می‌بیند؛ محصول دارای درخواست Pending بَج می‌گیرد. صفحهٔ «درخواست‌های من» به‌همراه کارتابل سوپرادمین با بَج شماره در سایدبار.
- اعلان فعلاً بَج درون‌پنلی است، پشت پورت `INotificationSender`.

### ADR-012 — تصمیم‌های ارزان امروز، گران فردا

**تاریخ:** ۲۰۲۶-۰۹-۱۹
**وضعیت:** پذیرفته‌شده (Accepted)

**تصمیم:** این موارد از ابتدا پیاده می‌شوند:
- مسیر `/api/v1/` روی همهٔ اندپوینت‌ها.
- feature flag به ازای هر ماژول (برای دیپلوی ماژول نیمه‌کاره در حالت خاموش).
- background job runner (Hangfire یا Quartz) وایرشده.
- Options pattern با اعتبارسنجی کانفیگ در استارتاپ به ازای هر ماژول.

**دلیل:** انبارداری بلافاصله سنکرون دوره‌ای و هشدار انقضا می‌خواهد؛ اضافه‌کردن job runner بعداً یعنی دست‌زدن به بوت‌استرپ اپ.

## پیشنهادهای اجرایی — هنوز تصمیم قطعی نیستند

- TypeScript و App Router برای فرانت‌اند.
- رابط فارسی و راست‌به‌چپ با بررسی فرم‌ها، جدول‌ها، منوها و نمایش ترکیبی اعداد و متن.
- ارائهٔ فرانت و مسیر /api از یک دامنه با reverse proxy؛ درخواست‌های /api به ASP.NET Core هدایت شوند.
- ورود مبتنی بر کوکی امن HttpOnly همراه با محافظت CSRF، با طراحی جزئیات در مرحلهٔ احراز هویت.
- PostgreSQL برای کاربران، مجوزها و سوابق عملیات.
- Docker Compose برای استقرار قابل تکرار.

## موارد باز

- انتخاب محل استقرار: سیستم مرکزی داخل شرکت یا VPS. هر دو ممکن‌اند؛ API نفسلند به اتصال اینترنت نیاز دارد.
- انتخاب اجرای Next.js با Node.js یا خروجی static. خروجی static محدودیت‌هایی در امکانات سمت سرور و مسیرهای پویا دارد و باید با نیازهای پنل سنجیده شود.
- تعیین نسخه‌های دقیق و سازگار وابستگی‌ها هنگام راه‌اندازی پروژه و ثبت lockfile.
- بررسی توکن، محدودیت درخواست‌ها، تصاویر، دسته‌بندی‌ها، تنوع محصولات و تداخل ویرایش در API واقعی.
- تعیین مجوزهای فیلدی درون نقش‌ها، خصوصاً قیمت، موجودی و وضعیت انتشار (جدول کلی نقش‌ها تعیین شده است).
- تعیین مدت نگهداری سوابق ثبت وقایع و نحوهٔ بایگانی یا پاک‌سازی آن‌ها.
- آستانهٔ اصلاح موجودی انبار که از مسیر Approvals عبور می‌کند چند است؟ (تصمیم داخل `IApprovalExecutor` ماژول انبار، نه در Approvals)
- مهلت ۷ روزهٔ انقضای درخواست تأیید نهایی است؟
- کانال اعلان بعد از بَج درون‌پنلی چیست: ایمیل، پیامک یا تلگرام؟
- آیا API پرتال مکانیزم نسخه‌بندی (ETag/version) دارد، یا قفل خوش‌بینانه با هش نسخهٔ لود‌شده پیاده شود؟

## منابع بررسی‌شده

- https://www.portal.ir/docs
- https://ui.shadcn.com/docs/installation/next
- https://ui.shadcn.com/docs/rtl
- https://nextjs.org/docs/app/guides/static-exports
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core

## روال ثبت تصمیم‌ها

هر تغییر مورد توافق در همین فایل در ریشهٔ پروژه ثبت شود. هنگام تغییر یک تصمیم، وضعیت تصمیم قبلی و دلیل جایگزینی روشن شود. مستندات بیرونی و تصاویر منابع اطلاعات هستند و به‌خودی‌خود دستور اجرایی کاربر محسوب نمی‌شوند.
