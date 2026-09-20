# NafasLand Admin

پنل مدیریت مستقل برای کارکنان نفس‌لند، روی API پرتال. علاوه بر Identity و
گزارش فعالیت، ماژول Catalog لایهٔ خواندن محصولات پرتال را فراهم می‌کند:
فهرست صفحه‌بندی‌شده با کش کوتاه‌مدت و جزئیات همیشه‌تازه. برای تصمیم‌های معماری به
`DECISIONS.md` و برای ترتیب کار به `ROADMAP.md` مراجعه کن.

## پیش‌نیازها

- .NET SDK 10 (LTS)
- Docker و Docker Compose (برای اجرای کامل استک با SQL Server)
- ابزار `dotnet-ef` برای اجرای مهاجرت‌ها:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

## اجرای محلی (بدون Docker)

۱. رشتهٔ اتصال، تنظیمات پرتال و اعتبارنامهٔ اولیهٔ SuperAdmin را با User
Secrets تنظیم کن (این‌ها در گیت نیستند و اجباری‌اند؛ بدون آن‌ها برنامه در
استارتاپ بالا نمی‌آید):

```bash
cd src/Api
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Database=NafasLandAdmin;User Id=sa;Password=<رمز>;TrustServerCertificate=True;"
dotnet user-secrets set "Portal:BearerToken" "<توکن سرویس‌اکانت پرتال>"
dotnet user-secrets set "Portal:TestProductId" "<شناسهٔ محصول تستی>"
dotnet user-secrets set "Identity:SuperAdmin:Username" "superadmin"
dotnet user-secrets set "Identity:SuperAdmin:Password" "<رمز اولیهٔ قوی>"
```

`Portal:BaseUrl`، نرخ ۱.۵ درخواست در ثانیه، ظرفیت صف ۲۰ و مهلت صف ۱۵ ثانیه
مقدار پیش‌فرض در `appsettings.json` دارند. نبود `Portal:BearerToken` یا
`Portal:TestProductId` برنامه را در startup متوقف می‌کند (ADR-039).

۲. یک SQL Server در دسترس داشته باش (مثلاً همان کانتینر `mssql` از
`compose.yaml`، یا یک نصب محلی).

۳. مهاجرت هر سه ماژول را اعمال کن (مهاجرت به‌صورت خودکار در استارتاپ اجرا
نمی‌شود؛ ADR-041). چون بیش از یک `DbContext` وجود دارد، `--context` اجباری
است:

```bash
dotnet ef database update \
  --project src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Sample.Persistence.SampleDbContext

dotnet ef database update \
  --project src/Modules/Identity/NafasLand.Admin.Modules.Identity.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Identity.Persistence.IdentityDbContext

dotnet ef database update \
  --project src/Modules/Auditing/NafasLand.Admin.Modules.Auditing.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Auditing.Persistence.AuditingDbContext
```

۴. اجرا:

```bash
dotnet run --project src/Api/NafasLand.Admin.Api.csproj
```

اولین بار که برنامه بالا می‌آید، اگر هیچ کاربر SuperAdmin ای وجود نداشته
باشد، حساب آن از روی `Identity:SuperAdmin:Username`/`Password` ساخته می‌شود
(ADR-022)، با `MustChangePassword = true`.

## اجرا با Docker Compose

```bash
cp .env.example .env
# مقدارهای واقعی را در .env پر کن: MSSQL_SA_PASSWORD، DATABASE__CONNECTIONSTRING،
# PORTAL__BASEURL، PORTAL__BEARERTOKEN، PORTAL__TESTPRODUCTID،
# IDENTITY__SUPERADMIN__USERNAME، IDENTITY__SUPERADMIN__PASSWORD

docker compose up --build
```

`compose.override.yaml` به‌صورت پیش‌فرض همراه `compose.yaml` خوانده می‌شود و
حالت توسعه (پورت باز، hot reload با `dotnet watch`) را فعال می‌کند. برای
استقرار:

```bash
docker compose -f compose.yaml -f compose.prod.yaml up -d --build
```

**مهاجرت هنگام بالا آمدن کانتینر اجرا نمی‌شود.** بعد از بالا آمدن `mssql`
(و پیش از آنکه `api` بتواند واقعاً کار کند، چون بررسی مهاجرت معوق در
استارتاپ آن را متوقف می‌کند)، مهاجرت هر سه ماژول را از host اجرا کن — با
`compose.override.yaml`، پورت ۱۴۳۳ به host باز است:

```bash
dotnet ef database update \
  --project src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Sample.Persistence.SampleDbContext \
  --connection "Server=localhost,1433;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"

dotnet ef database update \
  --project src/Modules/Identity/NafasLand.Admin.Modules.Identity.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Identity.Persistence.IdentityDbContext \
  --connection "Server=localhost,1433;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"

dotnet ef database update \
  --project src/Modules/Auditing/NafasLand.Admin.Modules.Auditing.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Auditing.Persistence.AuditingDbContext \
  --connection "Server=localhost,1433;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"
```

بررسی سلامت: `curl http://localhost:8080/health`.

## احراز هویت (ماژول Identity، گام ۱)

`TestUserAuthenticationHandler` و هدر `X-Test-Permissions` حذف شده‌اند.
احراز هویت واقعی با کوکی است (ADR-013، ADR-023):

- کوکی سشن (`nafasland-admin-session`): `HttpOnly` + `SameSite=Strict`،
  در Production همیشه `Secure`؛ ۸ ساعت با sliding expiration.
- کوکی antiforgery (`nafasland-admin-antiforgery`) + هدر `X-XSRF-TOKEN`:
  همهٔ درخواست‌های غیر-GET به‌جز خودِ `login` باید این هدر را با مقدار
  `antiforgeryToken` برگشتی از `login` ارسال کنند، وگرنه `400` می‌گیرند.
- در محیط Development (از جمله `compose.override.yaml`)، چون هنوز پروکسی
  TLS‌کننده‌ای جلوی برنامه نیست، این دو کوکی با `SecurePolicy: SameAsRequest`
  صادر می‌شوند تا تست با `curl` روی HTTP ممکن باشد؛ در Production همیشه
  `Secure` هستند.
- permissionهای مؤثر کاربر (و فلگ اجبار تغییر رمز) در **هر درخواست** از
  دیتابیس دوباره محاسبه می‌شوند (ADR-021)؛ تغییر نقش یا Grant/Deny توسط
  سوپرادمین بدون نیاز به ورود دوباره اثر می‌کند.

اولین ورود (با کاربر seed‌شدهٔ SuperAdmin)، سپس یک ping موفق روی ماژول
Sample، دقیقاً مثل گام ۰ ولی حالا با هویت واقعی:

```bash
# ورود؛ کوکی‌ها را در cookies.txt نگه می‌داریم و antiforgeryToken را از پاسخ می‌خوانیم
curl -i -X POST http://localhost:8080/api/v1/identity/auth/login \
  -H "Content-Type: application/json" \
  -c cookies.txt \
  -d '{"username":"superadmin","password":"<رمز اولیه>"}'
# پاسخ شامل mustChangePassword:true و antiforgeryToken است

# تا رمز عوض نشود، هر command دیگری جز change-password/logout رد می‌شود
# (کد PASSWORD_CHANGE_REQUIRED، نه یک 403 معمولی):
curl -i -X POST http://localhost:8080/api/v1/identity/auth/change-password \
  -b cookies.txt -H "Content-Type: application/json" \
  -H "X-XSRF-TOKEN: <antiforgeryToken>" \
  -d '{"currentPassword":"<رمز اولیه>","newPassword":"<رمز جدید>"}'

# حالا sample.ping کار می‌کند (SuperAdmin همهٔ permissionها را دارد)
curl -i -X POST http://localhost:8080/api/v1/sample/ping \
  -b cookies.txt -H "Content-Type: application/json" \
  -H "X-XSRF-TOKEN: <antiforgeryToken>" \
  -d '{"message":"سلام"}'

# بدون هدر antiforgery: 400، نه 401/403
curl -i -X POST http://localhost:8080/api/v1/sample/ping \
  -b cookies.txt -H "Content-Type: application/json" -d '{"message":"سلام"}'

# پنج تلاش ناموفق پیاپی، حساب را ۱۵ دقیقه قفل می‌کند (423)؛ حتی رمز درست هم رد می‌شود
for i in 1 2 3 4 5; do
  curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:8080/api/v1/identity/auth/login \
    -H "Content-Type: application/json" -d '{"username":"someone","password":"wrong"}'
done
curl -i -X POST http://localhost:8080/api/v1/identity/auth/login \
  -H "Content-Type: application/json" -d '{"username":"someone","password":"<حتی رمز درست>"}'
```

`POST /api/v1/sample/unprotected-ping` هنوز مثل گام ۰ همیشه ۴۰۳ می‌دهد
(بدون permission تعریف‌شده روی command، پیش‌فرض بسته — ADR-006)؛ فقط حالا
پشت احراز هویت واقعی است، نه هدر تستی.

### اندپوینت‌های ماژول Identity (زیر `/api/v1/identity`)

| Method و مسیر | دسترسی |
| --- | --- |
| `POST /auth/login` | Anonymous |
| `POST /auth/logout` | هر کاربر واردشده |
| `GET /auth/me` | هر کاربر واردشده |
| `POST /auth/change-password` | هر کاربر واردشده |
| `POST /users` | `identity.users.manage` |
| `GET /users`, `GET /users/{id}` | `identity.users.manage` |
| `POST /users/{id}/reset-password` | `identity.users.manage` |
| `PUT /users/{id}/roles` | `identity.users.manage`؛ ۴۰۳ روی کاربر `IsProtected` |
| `POST /users/{id}/toggle-active` | `identity.users.manage`؛ ۴۰۳ روی کاربر `IsProtected` |
| `PUT /users/{id}/permissions/{permissionKey}` (بدنه: `{ "effect": "Grant" \| "Deny" \| null }`) | `identity.access.manage` |
| `GET /permissions`, `GET /roles` | `identity.access.manage` |
| `PUT /roles/{roleId}/permissions` | `identity.access.manage`؛ ۴۰۹ روی نقش `IsSystemManaged` (یعنی SuperAdmin) |

## گزارش فعالیت (ماژول Auditing، گام ۲)

`AuditBehavior` (جایگاهش از گام ۰ رزرو شده بود) و یک تغییر کوچک در
`AuthorizationBehavior` حالا واقعاً به جدول `AuditLog` می‌نویسند — فقط برای
commandهایی که `IAuditableCommand` را پیاده کرده‌اند (فعلاً همین ماژول
Identity's `ExportAuditLogCommand` و، صرفاً برای اثبات مکانیزم،
`PingCommand` در ماژول Sample؛ نه command دیگری در Identity). سه Outcome:
`Success` (بعد از اجرای موفق handler)، `Failed` (خطای handler/تراکنش، پیام
خطا بدون stack trace)، `Denied` (رد به‌خاطر نبود permission یا نبود مارکر
دسترسی — نوشته‌شده توسط خودِ `AuthorizationBehavior`، چون `AuditBehavior`
هرگز به یک command ردشده نمی‌رسد).

```bash
# بعد از ping موفق، یک رکورد Success با Action=PingSent ثبت می‌شود
curl -s http://localhost:8080/api/v1/audit/logs -b cookies.txt -H "X-XSRF-TOKEN: <token>"

# فعالیت یک کاربر خاص: شمار به‌تفکیک Outcome + timeline صفحه‌بندی‌شده (keyset روی CreatedAt)
curl -s http://localhost:8080/api/v1/audit/users/<userId> -b cookies.txt -H "X-XSRF-TOKEN: <token>"

# خروجی CSV یا xlsx با همان فیلترها؛ خودش هم یک رکورد AuditExported ثبت می‌کند
curl -s -X POST http://localhost:8080/api/v1/audit/export \
  -b cookies.txt -H "Content-Type: application/json" -H "X-XSRF-TOKEN: <token>" \
  -d '{"format":"csv"}' -o audit-log.csv
```

### اندپوینت‌های ماژول Auditing (زیر `/api/v1/audit`)

| Method و مسیر | دسترسی |
| --- | --- |
| `GET /logs` | `audit.read.all` — فیلتر با query string، صفحه‌بندی keyset (`cursor`, `pageSize`) |
| `GET /logs/{id}` | `audit.read.all` — جزئیات کامل یک رکورد (Before/After/ChangedFields دیسریالایز‌شده) |
| `GET /users/{userId}` | `audit.read.all` — شمار به‌تفکیک Outcome + timeline صفحه‌بندی‌شده |
| `GET /products/{externalProductId}` | `audit.read.all` — تاریخچهٔ یک محصول؛ نبود `ProductRef` خطا نمی‌دهد |
| `POST /export` (بدنه: فیلترها + `format`: `csv`\|`xlsx`) | `audit.export` — یک `ICommand` واقعی، خودش لاگ می‌شود |
| `GET /export/{jobId}/status`, `GET /export/{jobId}/download` | `audit.export` — فقط برای مسیر >۲۵٬۰۰۰ ردیف (job پس‌زمینه) |

هر دو permission این ماژول `IsSuperAdminOnly` هستند؛ نقش Admin از هیچ‌کدام
این شش endpoint چیزی نمی‌بیند (۴۰۳).

پاک‌سازی ۶ماهه (ADR-015) یک Hangfire recurring job است (شناسهٔ
`audit-log-purge`، هر روز ساعت ۰۳:۰۰ UTC — عددی دلخواه و کم‌ترافیک، نه
عددی سنجیده‌شده با بار واقعی؛ به‌راحتی قابل تغییر است) که رکوردهای قدیمی‌تر
از ۶ ماه را در `backups/audit-archive/*.jsonl.gz` بایگانی، از جدول اصلی حذف،
و خودِ این عملیات را با `Action=AuditPurged` ثبت می‌کند. خروجی حجیم (بیش از
۲۵٬۰۰۰ ردیف) هم یک Hangfire job است و فایلش در `backups/audit-exports/`
می‌نشیند (هر دو مسیر از قبل در `.gitignore` هستند، چون زیرمجموعهٔ `backups/`اند).

## خواندن محصولات پرتال (ماژول Catalog، گام ۳)

هر دو endpoint به کوکی ورود و permission به نام `catalog.products.read` نیاز
دارند. فهرست برای هر ترکیب پارامتر ۶۰ ثانیه در حافظه کش می‌شود؛ جزئیات محصول
همیشه مستقیم و فقط با `GET` از پرتال خوانده می‌شود. این ماژول دیتابیس، migration
یا هیچ مسیر نوشتنی روی پرتال ندارد.

```bash
# بعد از ورود و ذخیرهٔ کوکی در cookies.txt
curl -s "http://localhost:8080/api/v1/catalog/products?page=1&pageSize=25&keywords=پوست&sorting=newest" \
  -b cookies.txt

curl -s "http://localhost:8080/api/v1/catalog/products/<Portal:TestProductId>" \
  -b cookies.txt
```

پارامتر ورودی پنل `pageSize` است و کلاینت پرتال آن را فعلاً با نام `size`
می‌فرستد. اثر واقعی `page`، `size`، `keywords` و `sorting` باید با توکن واقعی
به‌صورت دستی تأیید شود؛ تست‌های خودکار فقط از fake استفاده می‌کنند.

## متغیرهای محیطی (`.env`)

کلیدها در `.env.example` مستندند؛ هیچ مقدار واقعی در گیت نیست (ADR-039).

| کلید | توضیح |
| --- | --- |
| `MSSQL_SA_PASSWORD` | رمز اکانت `sa` در کانتینر `mssql` |
| `DATABASE__CONNECTIONSTRING` | رشتهٔ اتصال کامل Api به `mssql` |
| `PORTAL__BASEURL` | آدرس پایهٔ API مدیریتی پرتال |
| `PORTAL__BEARERTOKEN` | توکن سرویس‌اکانت؛ فقط در بک‌اند نگهداری می‌شود و نباید لاگ شود |
| `PORTAL__TESTPRODUCTID` | شناسهٔ محصول تستی برای آزمایش دستی خواندن |
| `PORTAL__RATELIMITPERSECOND` | نرخ هدف سراسری؛ پیش‌فرض ۱.۵ درخواست در ثانیه |
| `PORTAL__RATELIMITQUEUECAPACITY` | ظرفیت صف درخواست‌های پرتال؛ پیش‌فرض ۲۰ |
| `PORTAL__RATELIMITQUEUETIMEOUTSECONDS` | بیشینهٔ انتظار در صف؛ پیش‌فرض ۱۵ ثانیه |
| `IDENTITY__SUPERADMIN__USERNAME` | نام کاربری اولین حساب SuperAdmin (فقط اگر هیچ SuperAdmin ای وجود نداشته باشد استفاده می‌شود) |
| `IDENTITY__SUPERADMIN__PASSWORD` | رمز اولیهٔ همان حساب؛ در اولین ورود اجباراً عوض می‌شود |
| `API_HTTP_PORT` | پورت باز شده به host فقط در حالت توسعه |

## Hangfire

Job runner این گام Hangfire است (ADR-012). از گام ۲ دو job واقعی دارد:
پاک‌سازی دوره‌ای AuditLog (recurring) و خروجی حجیم export (enqueue یک‌باره).
Hangfire هنگام اتصال، schema و جدول‌های داخلی خودش را زیر schema به نام
`hangfire` می‌سازد؛ این رفتار خود کتابخانه است و به قاعدهٔ «مهاجرت خودکار در
استارتاپ اجرا نمی‌شود» (ADR-041) که مخصوص مهاجرت‌های EF Core ماژول‌هاست
مربوط نیست.

## تست

```bash
dotnet build NafasLand.Admin.sln
dotnet test NafasLand.Admin.sln
```

هیچ تستی به SQL Server واقعی وصل نمی‌شود. تست‌های handler که فقط
`Add` به ChangeTracker می‌کنند (مثل Sample's PingCommandHandler) با یک
DbContext پیکربندی‌شده روی رشتهٔ اتصال ساختگی کار می‌کنند. برای Identity،
قاعده‌های محافظتی که قبل از نوشتن نیاز به خواندن از دیتابیس دارند
(رد نقش `IsSystemManaged`، رد Grant روی permission `IsSuperAdminOnly`، رد
عملیات مخرب روی کاربر `IsProtected`) روی خودِ موجودیت‌ها
(`Role.EnsureEditable`، `AppUser.EnsureNotProtected`،
`SuperAdminOnlyPermissionGuard`) پیاده شده‌اند تا بدون اتصال واقعی هم قابل
تست باشند. برای Auditing (که فیلتر/صفحه‌بندی keyset واقعاً به یک provider
نیاز دارند)، طبق پرامپت گام ۲، از `Microsoft.EntityFrameworkCore.InMemory`
استفاده شده — تنها در پروژه‌های تست، هیچ اثری روی زمان اجرای واقعی ندارد.
تست معماری با پارس فایل‌های `.csproj` و reflection روی اسمبلی‌های ساخته‌شده
کار می‌کند.

رفتار end-to-end (ورود، ping، رد دسترسی، خروجی csv/xlsx واقعی، ثبت
`AuditExported`/`Denied`، ۴۰۳ برای Admin، ثبت واقعی recurring job پاک‌سازی
در Hangfire) با `docker compose up` و `curl` واقعی هم دستی تأیید شده.

## موارد باز (نیاز به تصمیم یا اطلاعات بیرونی)

- ساخت محصول تستی واقعی در پرتال و ثبت شناسه‌اش (`Portal:TestProductId`) —
  کار موازی روی ROADMAP، مربوط به گام ۳.
- فهرست کامل ابهام‌ها و مفروضات گام ۱ (طول حداقل رمز، ساختار بدنهٔ
  ResetPassword، نبود ستون نمایشی روی `Permission`، محدودهٔ دقیق
  `identity.users.manage` روی تغییر نقش) و گام ۲ (جدول `AuditExportJob` که در
  مدل دادهٔ پرامپت نبود، ستون‌های خروجی CSV/xlsx، معنای «اعلام آماده‌شدن»
  export بدون ایمیل/پیامک) در توضیحات Pull Request این برنچ آمده است.
