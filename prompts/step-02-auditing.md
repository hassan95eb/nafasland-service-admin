# پرامپت گام ۲ — رصدپذیری فعالیت ادمین‌ها (Auditing)

> این متن را به‌عنوان پرامپت به AI کدنویس بده. پیش از اجرا، مطمئن شو `AGENTS.md` و `DECISIONS.md` در دسترس آن هستند.

---

تو روی مخزن `nafasland-service-admin` کار می‌کنی. پیش از هر کاری `AGENTS.md` را بخوان و همهٔ قواعدش را رعایت کن. جزئیات هر تصمیم در `DECISIONS.md` با شمارهٔ ADR آمده است.

## قواعد همکاری (الزامی)
## گیت

1. **کامیت و پوش نکن.** تغییرات را فقط در فایل‌ها اعمال کن و همان‌جا رها کن. کاری با `git commit`، `git push`، `git rebase`، `git reset` یا تغییر تاریخچه نداشته باش.
2. **متن کامیت را پیشنهاد بده، نه اجرا.** در پایان کار، متن پیشنهادی کامیت را به‌صورت یک بلوک جدا در پاسخت بنویس تا بررسی و تأیید شود. فرمت Conventional Commits با بدنه‌ای که **دلیل** تغییر را توضیح می‌دهد، نه فهرست فایل‌ها.
3. **هیچ اشاره‌ای به هوش مصنوعی در متن کامیت نباشد** — نه در عنوان، نه در بدنه، نه به‌صورت `Co-Authored-By` یا هر امضای مشابه. کامیت باید طوری نوشته شود که انگار توسعه‌دهنده نوشته است.
4. **هر مرحله روی برنچ جداگانه.** پیش از شروع، برنچ جدید از `master` بساز با نام `step-<شماره>-<عنوان-کوتاه>` (مثلاً `step-02-auditing`). روی `master` مستقیم کار نکن.

## مرور و ادغام

5. **برنچ را خودت merge نکن.** کار روی برنچ مرحله تمام می‌شود و همان‌جا می‌ماند تا مرور و ادغام شود.
6. در پایان، خلاصه‌ای در قالب توضیحات Pull Request بنویس: چه چیزی ساخته شد، چرا این‌طور، کدام معیارهای پذیرش بررسی و تأیید شدند، و چه چیزی باز مانده. حتی اگر مخزن ریموت ندارد، این خلاصه مبنای مرور است (`git diff master...step-XX`).
7. **force push و بازنویسی تاریخچه ممنوع است**، در هیچ شرایطی.

## دامنهٔ تغییرات

8. **فقط کاری را انجام بده که این پرامپت خواسته.** هیچ refactor، تغییر نام، مرتب‌سازی import یا فرمت مجدد فایل‌های بی‌ربط.
9. **`DECISIONS.md`، `AGENTS.md` و `ROADMAP.md` را تغییر نده.** اگر جایی از آن‌ها اشتباه یا ناقص بود، در پاسخت گزارش کن تا تصمیم گرفته شود.
10. **مهاجرت‌های موجود را ویرایش یا حذف نکن.** مهاجرت جدید اضافه کن.
11. **وابستگی جدید بدون تأیید اضافه نکن.** اگر لازم شد، اسم بسته و دلیلش را بگو و منتظر بمان. (این مرحله یک استثنای صریح دارد: افزودن `ClosedXML` طبق ADR-047 از پیش تأیید شده — نیازی به پرسیدن دوباره نیست.)
12. نسخهٔ .NET، پکیج‌منیجر یا ابزارهای پایه را عوض نکن.

## ایمنی

13. **هیچ دستور مخربی اجرا نکن**: `rm -rf`، drop کردن دیتابیس، پاک کردن فایل‌های کاربر.
14. **هیچ مقدار حساسی را ننویس و چاپ نکن** — توکن، رمز، رشتهٔ اتصال. حتی در لاگ نمونه یا کامنت.
15. **به API واقعی پرتال درخواست نزن**، مگر جایی که پرامپت صراحتاً گفته باشد.

## کیفیت

16. **اگر جایی مبهم بود، حدس نزن.** فهرست ابهام‌ها را بنویس و بپرس. یک سؤال به‌موقع ارزان‌تر از یک پیاده‌سازی اشتباه است.
17. **کد زائد تولید نکن:** تست‌هایی که چیزی را نمی‌سنجند، کامنت‌های توضیح‌واضحات، فایل‌های scaffold بلااستفاده.
18. **پیش از تحویل، `dotnet build` و `dotnet test` را اجرا کن** و نتیجه را گزارش بده. کار ناتمام را «تمام» اعلام نکن.
19. **در پایان سه چیز گزارش کن:** خلاصهٔ آنچه ساخته شد، هر جایی که از پرامپت منحرف شدی و چرا، و فهرست سؤال‌ها یا موارد باز.

---

## هدف این مرحله

ساختن ماژول **`Auditing`**: جدول فقط‌append برای ثبت خودکار هر command (ADR-009)، پر کردن اسکلت `AuditBehavior` که در گام ۰ جایش رزرو شده بود، چهار نمای خواندنی و خروجی CSV/Excel (ADR-014)، و job پاک‌سازی ۶ماهه (ADR-015). این مرحله کاملاً بک‌اند است — فرانت‌اند در گام ۴ ساخته می‌شود؛ همان‌طور که در گام ۱، خروجی این مرحله فقط از طریق endpoint و تست قابل بررسی است.

## استک (افزودهٔ گام ۲)

- **ClosedXML** برای تولید خروجی `.xlsx` واقعی — از پیش با ADR-047 تأیید شده، بدون نیاز به تأیید مجدد.
- **Hangfire** (از پیش وایرشده در گام ۰، نهایی‌شده در ADR-045) برای دو job پس‌زمینه: خروجی حجیم (>۲۵٬۰۰۰ ردیف) و پاک‌سازی دوره‌ای.
- سریال‌سازی `BeforeJson`/`AfterJson` با **`System.Text.Json`** (کتابخانهٔ استاندارد runtime، نه Newtonsoft.Json — override نسخهٔ Newtonsoft در ADR-045 فقط دربارهٔ وابستگی ترانزیتیو Hangfire.Core است، نه انتخاب سریال‌سازی پروژه؛ اضافه‌کردن وابستگی مستقیم به Newtonsoft.Json نیاز به تأیید جداگانه دارد و در این مرحله لازم نیست).

## ماژول جدید: `Modules/Auditing`

ساختار دقیقاً مطابق الگوی `Modules/Identity` (ADR-004، ADR-006):

```
src/Modules/Auditing/
├─ Contracts/           تنها بخش public ماژول (فعلاً چیزی از این ماژول را ماژول دیگری صدا نمی‌زند؛ Contracts می‌تواند خالی/حداقلی باشد)
├─ Features/
│  ├─ Queries/          چهار نمای خواندنی (پایین‌تر توضیح داده شده) — Endpoint مستقیم روی DbContext، بدون Command (مثل Identity/Features/Queries)
│  └─ ExportAuditLog/   Command واقعی (چون خودش باید در AuditLog لاگ شود؛ ADR-014)
├─ Persistence/
│  ├─ AuditingDbContext.cs      schema: audit (ADR-019، ADR-045)
│  ├─ Configurations/
│  ├─ Migrations/
│  ├─ AuditLog.cs
│  └─ ProductRef.cs
├─ AuditingPermissions.cs
└─ AuditingModule.cs (پیاده‌سازی IModule)
```

علاوه‌براین، در `Shared/Kernel` و `Shared/Infrastructure` این فایل‌ها اضافه می‌شوند (چون رفتار cross-cutting‌اند، نه مخصوص یک ماژول — دقیقاً مثل `IUnitOfWork`):

- `Shared/Kernel/Auditing/IAuditableCommand.cs`
- `Shared/Kernel/Auditing/IAuditContext.cs`
- `Shared/Kernel/Auditing/IAuditLogWriter.cs`
- `Shared/Infrastructure/Auditing/AuditContext.cs` (پیاده‌سازی، Scoped)

پیادهٔ واقعی `IAuditLogWriter` (چون به `AuditingDbContext` نیاز دارد) داخل ماژول `Auditing` قرار می‌گیرد و در `AuditingModule.RegisterServices` به‌صورت `services.AddScoped<IAuditLogWriter, AuditLogWriter>()` ثبت می‌شود (بدون کلید — بر خلاف `IUnitOfWork` که per-module کلیددار است، چون فقط یک جدول AuditLog برای کل برنامه وجود دارد).

## طراحی از پیش تصمیم‌گیری‌شده: اتصال Auditing به pipeline (ADR-048)

این بخش را دقیقاً همین‌طور پیاده کن — حدس نزن، این یک تصمیم معماری از پیش قطعی‌شده است، نه چیزی که باید خودت طراحی کنی:

1. **`IAuditableCommand`** (در `Shared.Kernel`) یک مارکر اینترفیس است که command پیاده می‌کند تا اعلام کند باید لاگ شود:
   ```csharp
   public interface IAuditableCommand
   {
       string AuditAction { get; }     // مثلاً "UserCreated"، "RolePermissionsChanged"
       string AuditEntityType { get; } // مثلاً "User"، "Role"، "Product"
   }
   ```
   commandهایی که این را پیاده نمی‌کنند (مثل `PingCommand`، `LoginCommand`) کاملاً بی‌تغییر می‌مانند — نه لاگ Success/Failed، نه Denied.

2. **`IAuditContext`** (در `Shared.Kernel`, پیاده‌سازی Scoped در `Shared.Infrastructure`) سرویسی است که Handler در طول اجرا پر می‌کند تا اطلاعات مخصوص آن عملیات را به بیرون بدهد — Handler هرگز مستقیم به جدول AuditLog نمی‌نویسد:
   ```csharp
   public interface IAuditContext
   {
       void SetEntityId(string entityId);
       void SetBefore(object? snapshot);
       void SetAfter(object? snapshot);
   }
   ```
   - `SetBefore`/`SetAfter` مقدار را با `System.Text.Json` سریالایز و نگه می‌دارند.
   - `ChangedFields` را خودِ context، نه Handler، حساب می‌کند: مقایسهٔ سطح‌بالای کلیدهای دو JSON (نام فیلدهایی که مقدارشان فرق دارد)، وقتی هر دو snapshot ست شده باشند. اگر Handler فقط یکی از `Before`/`After` را ست کند (مثلاً عملیات Create که Before ندارد)، `ChangedFields` خالی می‌ماند و فقط `AfterJson` ثبت می‌شود.
   - وقتی Handler اصلاً این context را صدا نزند (چون داده‌ای برای قبل/بعد ندارد)، رکورد AuditLog با `EntityId = null`, `BeforeJson = null`, `AfterJson = null` نوشته می‌شود؛ این حالت خطا نیست.

3. **`IAuditLogWriter`** (در `Shared.Kernel`, پیاده‌سازی در ماژول `Auditing`) تنها نقطه‌ای است که واقعاً یک ردیف در جدول `AuditLog` درج می‌کند:
   ```csharp
   public interface IAuditLogWriter
   {
       Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken);
   }
   ```
   `AuditLogEntry` یک DTO ساده در `Shared.Kernel` با همان فیلدهای جدول AuditLog (پایین‌تر) به‌جز `Id`/`CreatedAt` (که خود writer پر می‌کند).

4. **`AuditBehavior`** (بدون تغییر جایگاه — همچنان پنجمین حلقه، بعد از Transaction) اسکلت گام ۰ را این‌طور پر کن:
   - اگر `command is not IAuditableCommand auditable`، بدون هیچ کاری `return await next();`.
   - در غیر این صورت، `next()` را در `try/catch` اجرا کن:
     - موفق: یک `AuditLogEntry` با `Outcome = Success` و مقادیر گرفته‌شده از `IAuditContext` (بعد از اجرای handler، چون context در طول اجرای همان handler پر می‌شود) بساز و از طریق `IAuditLogWriter` بنویس، سپس نتیجه را برگردان.
     - استثنا: یک `AuditLogEntry` با `Outcome = Failed` و `FailureReason = exception.Message` بنویس (بدون چاپ stack trace کامل در این فیلد — قاعدهٔ عدم افشای اطلاعات داخلی)، سپس دوباره پرتاب کن (`throw;`).
   - `ActorUserId`، `ActorRoleAtTime`، `CorrelationId`، `IpAddress`، `UserAgent` از `IHttpContextAccessor` خوانده می‌شوند (دقیقاً همان الگوی `AuthorizationBehavior`/`LoggingBehavior` که از قبل `IHttpContextAccessor` تزریق می‌کنند).

5. **`AuthorizationBehavior`** (بدون تغییر جایگاه — همچنان سومین حلقه) یک تغییر کوچک می‌گیرد: در شاخهٔ `default` که الان `AuthorizationDeniedException` پرتاب می‌کند (چون command هیچ‌کدام از `IRequiresPermission`/`IRequiresAuthenticatedUser` را ندارد) **و** در شاخهٔ `IRequiresPermission` وقتی `AuthorizeAsync` شکست بخورد، پیش از `throw`، اگر `command is IAuditableCommand`، یک `AuditLogEntry` با `Outcome = Denied` از طریق همان `IAuditLogWriter` بنویس. این تنها جایی‌ست که `AuthorizationBehavior` به `IAuditLogWriter` وابسته می‌شود (تزریق سازنده‌ای اضافه).
   - توجه: چون در این نقطه Handler هنوز اجرا نشده، `IAuditContext` خالی است؛ رکورد Denied فقط `AuditAction`/`AuditEntityType` (از خود command) و اطلاعات actor/correlation دارد — `EntityId`/`BeforeJson`/`AfterJson` خالی می‌مانند. این طبیعی است.

6. **Job پس‌زمینه** (خروجی حجیم، پاک‌سازی) از این pipeline رد نمی‌شوند (چون command dispatch نیستند، بلکه Hangfire job مستقیم‌اند) — این‌ها مستقیماً `IAuditLogWriter` را صدا می‌زنند، با `ActorUserId = null` و `ActorRoleAtTime = "System"`.

## مدل داده

### `AuditLog` (schema `audit`, فقط append — ADR-009)

```
Id                Guid, PK
CorrelationId     string
ActorUserId       Guid?      (null برای job سیستمی)
OnBehalfOfUserId  Guid?      (رزرو برای آینده؛ در این مرحله همیشه null — هیچ ویژگی on-behalf-of ساخته نمی‌شود)
ActorRoleAtTime   string     ("System" برای jobها)
Action            string     (مقدار از IAuditableCommand.AuditAction، یا "AuditExported"/"AuditPurged")
EntityType        string?
EntityId          string?
BeforeJson        string?    (nvarchar(max))
AfterJson         string?    (nvarchar(max))
ChangedFields     string?    (JSON array از نام فیلدها)
Outcome           enum: Success | Failed | Denied
UpstreamStatus    int?       (رزرو برای وقتی محصولات به این پنل بیایند؛ فعلاً همیشه null)
FailureReason     string?
IpAddress         string?
UserAgent         string?
CreatedAt         DateTime (UTC)
```

هیچ متد `Update`/`Delete` روی این Entity یا DbSet آن در سطح EF Core تعریف نشود؛ فقط `Add`. هیچ endpoint یا Command برای ویرایش/حذف وجود ندارد — نه حتی برای SuperAdmin (ADR-009، ADR-015).

ایندکس‌ها دقیقاً طبق ADR-014:
- `(ActorUserId, CreatedAt DESC)`
- `(EntityType, EntityId, CreatedAt DESC)`

### `ProductRef` (schema `audit` — ADR-009)

```
ExternalProductId  string, PK
LastKnownTitle     string
LastSyncedAt       DateTime (UTC)
```

در این مرحله هیچ کد دیگری این جدول را پر نمی‌کند (چون ماژول محصول هنوز ساخته نشده) — فقط جدول و مهاجرتش ساخته شود تا وقتی محصول به پنل اضافه شد (گام‌های بعدی)، تاریخچهٔ محصول (نمای ۴) بلافاصله کار کند. این را در گزارش پایانی به‌عنوان یک نکته ذکر کن، سؤال نیست.

## Permissionهای این ماژول (`AuditingPermissions`)

- `audit.read.all` — `IsSuperAdminOnly: true`
- `audit.export` — `IsSuperAdminOnly: true`

نقش Admin به هیچ‌کدام دسترسی ندارد؛ `audit.read.own` تعریف نمی‌شود (ADR-014).

## نماها / Endpointها (زیر `/api/v1/audit`)

هر چهار نما endpoint خواندنی ساده‌اند (بدون Command، مثل `Identity/Features/Queries` — مستقیم از `AuditingDbContext`)، همه با `.RequireAuthorization().RequirePermission(AuditingPermissions.ReadAll)`:

1. **`GET /api/v1/audit/logs`** — لاگ سراسری. فیلتر با query string: `actorUserId`, `from`, `to`, `action`, `entityType`, `outcome`. صفحه‌بندی **keyset روی `CreatedAt`** (نه offset) — پارامتر `cursor` (آخرین `CreatedAt` صفحهٔ قبل) و `pageSize` (پیش‌فرض معقول، سقف بالا).
2. **`GET /api/v1/audit/users/{userId}`** — فعالیت یک ادمین: خلاصهٔ شمارشی در بازهٔ `from`/`to` (تعداد به تفکیک `Action` یا حداقل به تفکیک `Outcome`) + timeline صفحه‌بندی‌شدهٔ (keyset) رکوردهای همان `ActorUserId`.
3. **جزئیات یک رکورد** — از همان `GET /api/v1/audit/logs/{id}` یک رکورد را با `BeforeJson`/`AfterJson`/`ChangedFields` کامل برمی‌گرداند؛ فرانت (گام ۴) مسئول رندر «قبل ← بعد» است، این مرحله فقط داده را با شکل خام برمی‌گرداند (رشتهٔ JSON یا Dictionary دیسریالایز شده — هرکدام ساده‌تر است، در گزارش پایانی بگو کدام را انتخاب کردی).
4. **`GET /api/v1/audit/products/{externalProductId}`** — تاریخچهٔ یک محصول: فیلتر `EntityType = "Product"` و `EntityId = externalProductId`، صفحه‌بندی keyset، عنوان از `ProductRef` (اگر رکوردش وجود داشته باشد؛ در غیر این صورت `LastKnownTitle = null` برگردد، نه خطا — طبق ADR-009 دلیل وجود `ProductRef` همین جلوگیری از رکورد بی‌نام است، ولی نبودش نباید ۵۰۰ بدهد).

### خروجی (`Features/ExportAuditLog`) — یک Command واقعی

بر خلاف چهار نمای بالا، این یک `ICommand` است (نه endpoint مستقیم) چون خودش باید طبق ADR-014 با `Action = AuditExported` لاگ شود — یعنی `IAuditableCommand` را پیاده می‌کند و از همان مسیر `AuditBehavior` رد می‌شود.

- `POST /api/v1/audit/export` با همان فیلترهای نمای سراسری در بدنه + `format` (`csv` یا `xlsx`).
- Permission: `audit.export` (`IRequiresPermission`).
- شمارش ردیف‌های منطبق با فیلتر:
  - **≤ ۲۵٬۰۰۰ ردیف:** همان‌جا synchronous فایل تولید و در پاسخ برگردانده می‌شود (`text/csv` یا `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`).
  - **> ۲۵٬۰۰۰ ردیف:** یک Hangfire job صف می‌شود که فایل را در یک پوشهٔ داخلی (مثلاً `backups/audit-exports/`, در `.gitignore`) می‌سازد؛ Command بلافاصله یک `jobId` برمی‌گرداند، نه فایل.
  - `GET /api/v1/audit/export/{jobId}/status` و `GET /api/v1/audit/export/{jobId}/download` برای پیگیری و دانلود بعدی — چون هیچ زیرساخت ایمیل/پیامک در پروژه وجود ندارد (ADR-023)، «اعلام آماده‌شدن» در این مرحله یعنی همین دو endpoint، نه نوتیفیکیشن واقعی؛ این را در گزارش پایانی به‌عنوان یک تصمیم تفسیری ذکر کن.
- `AuditAction = "AuditExported"`, `AuditEntityType = "AuditLog"`; `IAuditContext` را با شرح فیلترهای اعمال‌شده (مثلاً JSON همان query) به‌عنوان `AfterJson` پر کن تا در خودِ لاگ export هم معلوم باشد چه چیزی خروجی گرفته شده.

## پاک‌سازی ۶ماهه (ADR-015) — Hangfire recurring job

- یک recurring job (مثلاً روزانه) که رکوردهای `CreatedAt` قدیمی‌تر از ۶ ماه را:
  1. در یک فایل بایگانی فشرده (`.gz` یا مشابه) در `backups/audit-archive/` (در `.gitignore`) می‌نویسد،
  2. سپس از جدول اصلی حذف می‌کند،
  3. و خودِ اجرای پاک‌سازی را با `Action = "AuditPurged"`, `ActorUserId = null`, `ActorRoleAtTime = "System"` و بازهٔ حذف‌شده (تعداد رکورد و محدودهٔ تاریخ) در `AfterJson` مستقیماً از طریق `IAuditLogWriter` ثبت می‌کند (نه از طریق pipeline — این یک command نیست).
- هیچ endpoint یا permission‌ای برای اجرای دستی این پاک‌سازی از داخل پنل ساخته نشود (ADR-015 صریحاً می‌گوید نه از داخل پنل).
- زمان‌بندی دقیق (مثلاً هر روز ساعت چند) اهمیتی ندارد در این مرحله؛ اگر باید حدس بزنی، یک زمان کم‌ترافیک (مثلاً ۰۳:۰۰ UTC) بگذار و در گزارش پایانی صریح بگو که این عدد قابل تغییر است.

## چیزی که به Identity/Sample module اضافه/عوض نمی‌شود

- هیچ commandی در Identity یا Sample در همین مرحله `IAuditableCommand` نمی‌شود. اتصال commandهای واقعی (`CreateUserCommand`, `SetUserRolesCommand` و…) به این مکانیزم، کار یک مرحلهٔ جدا/بعدی است (تا این پرامپت روی چیزی که از پیش تست‌شده دست نبرد) — فقط زیرساخت اینجا ساخته می‌شود. اگر خواستی برای اثبات end-to-end یک command نمونه را audit-پذیر کنی، **فقط `PingCommand` در ماژول Sample** را برای این منظور استفاده کن (نه چیزی در Identity)، و در گزارش پایانی توضیح بده که این فقط برای اثبات مکانیزم است.

## تست‌های الزامی

- `AuditBehaviorTests` موجود (که فقط pass-through را تست می‌کند) باید همچنان سبز بماند برای commandهای غیر-`IAuditableCommand`؛ تست‌های جدید اضافه کن برای هر سه Outcome (Success/Failed/Denied) با یک command و handler فیک (نه دیتابیس واقعی — از یک `IAuditLogWriter` فیک/mock استفاده کن).
- تست برای `IAuditContext`: محاسبهٔ `ChangedFields` وقتی هر دو Before/After ست شده‌اند، و رفتار وقتی فقط یکی ست شده.
- تست برای `AuthorizationBehavior`: رد یک `IAuditableCommand` بدون permission واقعاً یک رکورد Denied می‌نویسد (با mock `IAuditLogWriter`)، و رد یک command غیر-`IAuditableCommand` هیچ‌چیز نمی‌نویسد.
- تست یکپارچگی (در سطح ماژول Auditing، با DbContext واقعی/InMemory یا SQLite طبق الگوی موجود تست‌های Identity) برای: فیلتر و صفحه‌بندی keyset نمای سراسری، نمای per-product وقتی `ProductRef` وجود ندارد (نباید خطا بدهد)، و اینکه `ExportAuditLog` خودش یک رکورد `AuditExported` تولید می‌کند.
- تست برای مسیر >۲۵٬۰۰۰ ردیف: نیازی به تولید واقعی ۲۵٬۰۰۰+ رکورد نیست؛ آستانه را قابل تزریق/کانفیگ کن (یا حداقل در یک ثابت داخلی جدا) تا تست بتواند آستانه را در سطح پایین‌تر شبیه‌سازی کند.

## معیار پذیرش

1. `dotnet build` و `dotnet test` سبز.
2. هر command که `IAuditableCommand` را پیاده کرده، **بدون آنکه handlerش چیزی به AuditLog بنویسد**، در صورت موفقیت `Outcome = Success` ثبت می‌کند.
3. تلاش رد‌شده (چه به‌خاطر نبود permission، چه command بدون هیچ مارکر دسترسی) روی یک `IAuditableCommand`، با `Outcome = Denied` ثبت می‌شود.
4. خطای غیرمنتظرهٔ handler/تراکنش روی یک `IAuditableCommand`، با `Outcome = Failed` و `FailureReason` ثبت می‌شود و استثنا همچنان به بیرون پرتاب می‌شود (رفتار API عوض نمی‌شود).
5. هیچ مسیری (endpoint، Command، یا متد EF) برای ویرایش یا حذف یک رکورد AuditLog وجود ندارد — نه حتی برای SuperAdmin.
6. کاربر Admin (بدون `audit.read.all`) از هر چهار نما و از export، `403` می‌گیرد.
7. صفحه‌بندی چهار نما واقعاً keyset است (نه `Skip`/`Take` روی offset).
8. `POST /api/v1/audit/export` با فیلتر کم‌ردیف، فایل CSV/xlsx واقعی و قابل بازکردن برمی‌گرداند؛ خودش یک رکورد `AuditExported` تولید می‌کند.
9. Job پاک‌سازی، رکوردهای قدیمی‌تر از ۶ ماه را بایگانی و حذف می‌کند و خودش با `Action = AuditPurged` ثبت می‌شود.
10. تست معماری موجود (رد رفرنس بین ماژول‌ها) همچنان سبز است؛ ماژول Auditing به بخش غیر-Contracts هیچ ماژول دیگری رفرنس نمی‌دهد.

## آنچه در این مرحله **نباید** انجام شود

- هیچ فرانت‌اندی، هیچ صفحهٔ HTML — همه‌چیز از طریق endpoint تست می‌شود.
- هیچ اتصال واقعی command موجود در Identity (`CreateUserCommand` و…) به `IAuditableCommand` — فقط زیرساخت (به‌جز `PingCommand` در Sample، صرفاً برای اثبات، اختیاری).
- هیچ ماژول محصول/کاتالوگ — `ProductRef` فقط جدول خالی است.
- هیچ endpoint حذف/ویرایش دستی روی AuditLog، حتی پشت `IsSuperAdminOnly`.
- هیچ نوتیفیکیشن ایمیل/پیامک واقعی برای آمادگی فایل export.
- هیچ تماسی با API پرتال.
- هیچ کتابخانهٔ اضافه‌ای غیر از `ClosedXML` (که طبق ADR-047 از پیش تأیید شده) بدون بررسی `DECISIONS.md` و پرسیدن.

## خروجی

- کد در همین مخزن، روی برنچ `step-02-auditing`.
- یک کامیت با فرمت Conventional Commits که بدنه‌اش **دلیل** طراحی `IAuditContext`/`IAuditLogWriter` (نه فهرست فایل‌ها) را توضیح دهد.
- به‌روزرسانی کوتاه `README.md` اگر متغیر محیطی یا دستور جدیدی (مثلاً مسیر بایگانی) اضافه شد.
- اگر جایی از `DECISIONS.md` مبهم بود یا با واقعیت پیاده‌سازی نخواند، **حدس نزن** — همان مورد را فهرست کن و بپرس.
