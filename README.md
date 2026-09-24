# NafasLand Admin

پنل مدیریت مستقل برای کارکنان نفس‌لند، روی API پرتال. فرانت فارسی و RTL از
پشت reverse proxy به API متصل می‌شود و محصولات را با حفظ قرارداد پرتال نمایش
و ویرایش می‌کند. برای تصمیم‌های معماری به `DECISIONS.md` و برای ترتیب
کار به `ROADMAP.md` مراجعه کن.

## پیش‌نیازها

- .NET SDK 10 (LTS)
- Node.js 24 و npm 11 (برای اجرای فرانت بدون Docker)
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
# فقط برای محیطی که ایجاد واقعی محصول در آن صریحاً مجاز است:
dotnet user-secrets set "Portal:AllowProductCreation" "true"
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

dotnet ef database update \
  --project src/Modules/Approvals/NafasLand.Admin.Modules.Approvals.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Approvals.Persistence.ApprovalsDbContext
```

۴. اجرا:

```bash
dotnet run --project src/Api/NafasLand.Admin.Api.csproj
```

فرانت برای چک permission سمت سرور باید API را از یک آدرس داخلی در دسترس داشته
باشد. در اجرای بدون Docker، در یک ترمینال دیگر:

```bash
cd src/Web
npm ci
INTERNAL_API_BASE_URL=http://localhost:8080 npm run dev
```

در این حالت برای یکپارچگی کوکی هم‌مبدأ، اجرای کامل با Compose و proxy توصیه
می‌شود؛ اجرای جداگانهٔ بالا بیشتر برای توسعهٔ رابط کاربری است.

اولین بار که برنامه بالا می‌آید، اگر هیچ کاربر SuperAdmin ای وجود نداشته
باشد، حساب آن از روی `Identity:SuperAdmin:Username`/`Password` ساخته می‌شود
(ADR-022)، با `MustChangePassword = true`.

## اجرا با Docker Compose

```bash
cp .env.example .env
# مقدارهای واقعی را در .env پر کن: MSSQL_SA_PASSWORD، DATABASE__CONNECTIONSTRING،
# PORTAL__BASEURL، PORTAL__BEARERTOKEN، PORTAL__TESTPRODUCTID،
# IDENTITY__SUPERADMIN__USERNAME، IDENTITY__SUPERADMIN__PASSWORD

./scripts/dev-up.sh
```

این اسکریپت کل توالی محیط توسعه را با یک دستور انجام می‌دهد: بالا آوردن
`mssql`، صبر برای healthy شدن آن، اجرای مهاجرت هر چهار ماژول از host (نیاز به
`dotnet-ef`؛ `dotnet tool install --global dotnet-ef`)، و بعد بالا آوردن
`api`، `web` و `proxy`. مهاجرت همچنان یک گام صریح می‌ماند (ADR-041) — فقط دیگر
لازم نیست هر بار دستی تایپ شود؛ خودِ اپ هنوز فقط مهاجرت معوق را در استارتاپ
بررسی می‌کند، نه اجرا. برای دیدن لاگ‌ها به‌جای اجرای پس‌زمینه:
`./scripts/dev-up.sh` بدون `-d`؛ برای پاس‌دادن آرگومان اضافه به
`docker compose up` (مثلاً `-d`)، همان‌ها را بعد از اسکریپت بده.

`compose.override.yaml` به‌صورت پیش‌فرض همراه `compose.yaml` خوانده می‌شود و
حالت توسعه (hot reload بک‌اند و فرانت) را فعال می‌کند. پنل را از
`http://localhost:8000` باز کن؛ مرورگر همیشه از همین proxy وارد می‌شود و پورت
خام سرویس `web` عمداً به host باز نشده است. پورت ۸۰۰۰ با
`PROXY_HTTP_PORT` قابل تغییر است.

### استقرار (بدون hot reload)

```bash
docker compose -f compose.yaml -f compose.prod.yaml up -d --build
```

`scripts/dev-up.sh` مخصوص محیط توسعه است (روی پورت ۱۴۳۳ باز‌شده توسط
`compose.override.yaml` تکیه می‌کند). **مهاجرت هنگام بالا آمدن کانتینر اجرا
نمی‌شود.** بعد از بالا آمدن `mssql` (و پیش از آنکه `api` بتواند واقعاً کار
کند، چون بررسی مهاجرت معوق در استارتاپ آن را متوقف می‌کند)، مهاجرت هر چهار
ماژول را از host اجرا کن:

```bash
dotnet ef database update \
  --project src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Sample.Persistence.SampleDbContext \
  --connection "Server=<هاست mssql>;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"

dotnet ef database update \
  --project src/Modules/Identity/NafasLand.Admin.Modules.Identity.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Identity.Persistence.IdentityDbContext \
  --connection "Server=<هاست mssql>;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"

dotnet ef database update \
  --project src/Modules/Auditing/NafasLand.Admin.Modules.Auditing.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Auditing.Persistence.AuditingDbContext \
  --connection "Server=<هاست mssql>;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"

dotnet ef database update \
  --project src/Modules/Catalog/NafasLand.Admin.Modules.Catalog.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --context NafasLand.Admin.Modules.Catalog.Persistence.CatalogDbContext \
  --connection "Server=<هاست mssql>;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"
```

بررسی سلامت مستقیم API: `curl http://localhost:8080/health`.

### تولید تایپ‌های API برای فرانت

سند OpenAPI فقط وقتی API در محیط Development اجرا می‌شود روی
`/openapi/v1.json` در دسترس است. پس از بالا آمدن API، تایپ‌های TypeScript را
دستی بازتولید کن:

```bash
cd src/Web
npm run generate:api-types
```

اگر API روی آدرس دیگری است، متغیر `OPENAPI_URL` را برای همان دستور تنظیم کن.
این تولید عمداً بخشی از `dev` یا `build` نیست تا بیلد فرانت به API در حال اجرا
وابسته نشود.

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

## مدیریت ادمین‌ها (گام ۵)

مسیر `/admins` برای دارندهٔ `identity.users.manage` فهرست ادمین‌ها، ساخت حساب،
فعال/غیرفعال‌سازی، ریست رمز و ویرایش نقش‌ها را فراهم می‌کند. جزئیات هر حساب در
`/admins/{id}` است؛ Grant/Deny مستقیم permission و مدیریت permissionهای نقش‌ها
فقط برای دارندهٔ `identity.access.manage` نمایش داده می‌شود. فهرست سبک نقش‌ها
برای فرم تغییر نقش از `GET /api/v1/identity/roles/summary` می‌آید و نقشهٔ کامل
permissionهای نقش‌ها را افشا نمی‌کند (ADR-052).

برای ساخت حساب و ریست رمز، فرانت با `crypto.getRandomValues` یک رمز موقت ۱۶
نویسه‌ای شامل حروف کوچک، حروف بزرگ و عدد می‌سازد. این مقدار فقط در state همان
صفحه نگهداری و پس از پاسخ موفق فقط یک‌بار همراه دکمهٔ کپی نمایش داده می‌شود؛ در
URL، storage یا cache کوئری ذخیره نمی‌شود. کاربر در اولین ورود فقط فرم
`/change-password` را می‌بیند و تا تغییر موفق رمز، سایدبار و سایر صفحات پنل
برای او رندر نمی‌شوند.

## گزارش فعالیت (ماژول Auditing، گام ۲)

`AuditBehavior` (جایگاهش از گام ۰ رزرو شده بود) و یک تغییر کوچک در
`AuthorizationBehavior` حالا واقعاً به جدول `AuditLog` می‌نویسند — فقط برای
commandهایی که `IAuditableCommand` را پیاده کرده‌اند (عملیات مدیریت کاربر و
دسترسی در Identity، `ExportAuditLogCommand` در Auditing و، صرفاً برای اثبات
مکانیزم، `PingCommand` در ماژول Sample). سه Outcome:
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
| `GET /users/{userId}` | `audit.read.all` — شمار به‌تفکیک Outcome و به‌تفکیک Action+Outcome (`countsByAction`) + timeline صفحه‌بندی‌شده |
| `GET /products/{externalProductId}` (اختیاری: `variantId` تکرارشونده) | `audit.read.all` — تاریخچهٔ یک محصول با رویدادهای واریانت‌هایش و عنوان از `ProductRef`؛ نبود `ProductRef` خطا نمی‌دهد |
| `GET /actors` | `audit.read.all` — هر کسی که در لاگ عامل بوده، با نام کاربری (گزینه‌های فیلتر «کاربر») |
| `POST /export` (بدنه: فیلترها + `format`: `csv`\|`xlsx`) | `audit.export` — یک `ICommand` واقعی، خودش لاگ می‌شود |
| `GET /export/{jobId}/status`, `GET /export/{jobId}/download` | `audit.export` — فقط برای مسیر >۲۵٬۰۰۰ ردیف (job پس‌زمینه) |

هر دو permission این ماژول `IsSuperAdminOnly` هستند؛ نقش Admin از هیچ‌کدام
این endpointها چیزی نمی‌بیند (۴۰۳). پاسخ‌های خواندن، کنار `actorUserId`،
`actorUsername` را هم دارند (از Identity، از طریق `IUserDirectory` در Kernel).

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

## ایجاد و ویرایش محصول (گام ۷)

دارندهٔ `catalog.products.write` به مسیرهای `/products/new` و
`/products/{id}/edit` دسترسی دارد. قراردادهای داخلی:

| Method و مسیر | رفتار |
| --- | --- |
| `POST /api/v1/catalog/products` | ایجاد محصول؛ هدر GUID به نام `Idempotency-Key` اجباری است |
| `PUT /api/v1/catalog/products/{productId}` | خواندن زنده، مقایسهٔ `LastKnownVersion` و سپس PUT کامل |

`Portal:AllowProductCreation` به‌صورت پیش‌فرض `false` است. ایجاد واقعی فقط وقتی
انجام می‌شود که این گزینه در کانفیگ همان محیط صریحاً `true` باشد. تکرار همان
فرم ایجاد با همان `Idempotency-Key` پاسخ قبلی را برمی‌گرداند و محصول دیگری
نمی‌سازد.

اختلاف نسخه با `409 Conflict` پاسخ داده می‌شود و هیچ PUTای به پرتال نمی‌رود.
تصاویر محصول موجود عیناً حفظ می‌شوند؛ ایجاد محصول با `image/images = null`
است و در این مرحله آپلود، حذف، تغییر ترتیب یا جایگزینی تصویر وجود ندارد.
قیمت و موجودی محصول موجود نیز در فرم محصول تغییر نمی‌کنند و فقط از endpoint
`PATCH /api/v1/catalog/products/variants/{variantId}` به‌روزرسانی می‌شوند.

## تأیید و حذف/انتشار محصول (ماژول Approvals، گام ۸)

چهار عملیات پرریسک روی محصول — حذف، انتشار/لغو انتشار، تغییر `featured`/`most`
و حذف واریانت — مستقیماً توسط Admin اجرا نمی‌شوند. هرکدام از مسیر «ثبت درخواست
← تأیید یا رد سوپرادمین ← اجرای واقعی روی پرتال» عبور می‌کند (ADR-010،
ADR-030، ADR-031، ADR-032). ماژول `Approvals` عمومی است و دانشی از Catalog
ندارد؛ Catalog فقط چهار «مجری» (`IApprovalExecutor`) در `src/Modules/Catalog/Approvals/`
رجیستر می‌کند.

### Permissionهای تازه

ADR-010/ADR-030/ADR-031 کلیدهای permission را بدون پیشوند ماژول نوشته‌اند
(`product.delete.request`، `approval.review`، ...)؛ این پیاده‌سازی، مثل تصحیح
مشابه گام ۶ برای `catalog.products.write`، قرارداد واقعی کد (پیشوند ماژول
کوچک‌حروف) را ادامه می‌دهد:

| Permission | نقش | IsSuperAdminOnly |
| --- | --- | --- |
| `catalog.products.delete.request` | Admin | خیر |
| `catalog.products.delete` | SuperAdmin | بله |
| `catalog.products.publish.request` | Admin | خیر |
| `catalog.products.publish` | SuperAdmin | بله |
| `catalog.products.status.request` (برای `featured`/`most`) | Admin | خیر |
| `catalog.products.status` | SuperAdmin | بله |
| `catalog.variants.delete.request` | Admin | خیر |
| `catalog.variants.delete` | SuperAdmin | بله |
| `approvals.review` (ثبت approve/reject/retry) | SuperAdmin | بله |
| `approvals.read.all` (کارتابل کامل) | SuperAdmin | بله |

`approval.read.own` در ADR برای «همهٔ نقش‌ها» است — چون در مدل default-closed
هیچ permissionی برای «همه» قابل Grant نیست، این عملاً یک permission جداگانه
نیست؛ `GET /api/v1/approvals?mine=true` و `GET /api/v1/approvals/{id}` برای هر
کاربر واردشده باز است و در کد به‌صورت دستی به `RequestedByUserId == کاربر
جاری` محدود می‌شود مگر کاربر `approvals.read.all` داشته باشد.

### جریان از دید کلاینت

```bash
# ۱) ادمین درخواست حذف محصول تستی را ثبت می‌کند (نیاز به catalog.products.delete.request)
curl -s -X POST "http://localhost:8080/api/v1/approvals" \
  -b cookies.txt -H "Content-Type: application/json" -H "X-XSRF-TOKEN: $TOKEN" \
  -d '{"requestType":"catalog.product.delete","targetEntityType":"Product","targetEntityId":"<Portal:TestProductId>","reason":"محصول تستی دیگر لازم نیست","payload":{"productId":"<Portal:TestProductId>"}}'

# ۲) سوپرادمین کارتابل را می‌بیند (نیاز به approvals.read.all)
curl -s "http://localhost:8080/api/v1/approvals?status=Pending" -b cookies.txt

# ۳) سوپرادمین جزئیات را با پیش‌نمایش زنده از پرتال می‌بیند
curl -s "http://localhost:8080/api/v1/approvals/<id>" -b cookies.txt

# ۴) تأیید (نیاز به approvals.review) — بلافاصله اجرا هم می‌شود
curl -s -X POST "http://localhost:8080/api/v1/approvals/<id>/approval" \
  -b cookies.txt -H "Content-Type: application/json" -H "X-XSRF-TOKEN: $TOKEN" -d '{"note":null}'
```

هر درخواست `Pending` که ۷ روز بدون تصمیم بماند، با یک Hangfire recurring job
(`approvals-expire-pending`، هر روز ساعت ۰۳:۰۰) به‌صورت خودکار `Expired`
می‌شود.

### نمونهٔ خطای `409` (تصمیم روی درخواست ترمینال یا تداخل هم‌زمان)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "این درخواست دیگر در وضعیت «در انتظار» نیست؛ تصمیمی روی آن قبلاً ثبت شده.",
  "correlationId": "..."
}
```

### نمونهٔ خطای `403` (دسترسی تأییدکننده از زمان ثبت درخواست تغییر کرده)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "detail": "دسترسی catalog.products.delete لازم است؛ دسترسی تأییدکننده از زمان ثبت درخواست تغییر کرده است.",
  "correlationId": "..."
}
```

شکست تماس با پرتال هنگام اجرا خطای HTTP نیست: پاسخ `POST .../approval` همچنان
`200` است و بدنه `status: "ExecutionFailed"` و `executionError` را برمی‌گرداند؛
تصمیم تأیید از دست نمی‌رود و فقط با `POST /api/v1/approvals/{id}/retry`
(دستی، بدون تلاش خودکار) دوباره اجرا می‌شود.

## گزارش فعالیت در پنل (گام ۹)

نماهای ADR-014 روی همان اندپوینت‌های بالا. هیچ اندپوینت ویرایش یا حذف لاگ
وجود ندارد، برای هیچ نقشی.

| مسیر پنل | permission | چه چیزی |
| --- | --- | --- |
| `/admin/audit` | `audit.read.all` | لاگ سراسری با فیلتر کاربر، بازهٔ تاریخ شمسی، عملیات، نوع موجودیت و نتیجه؛ فیلترها در query string می‌مانند (لینک قابل اشتراک). کلیک روی ردیف، جزئیات «قبل ← بعد» را در پنل کناری باز می‌کند. |
| `/admin/audit/users/{userId}` | `audit.read.all` | کارت‌های شمارشی بازهٔ انتخابی (ایجاد، ویرایش، درخواست تأیید، تصمیم، تلاش ردشده، ناموفق) و تایم‌لاین همان کاربر. از روی نام عامل در لاگ سراسری. |
| `/products/{id}` → «تاریخچهٔ تغییرات» | `audit.read.all` | رویدادهای خود محصول، تغییر قیمت/موجودی واریانت‌ها و درخواست‌های تأیید آن، با عنوان از `ProductRef`. بدون این permission بخش رندر نمی‌شود و درخواستش هم فرستاده نمی‌شود. |
| دکمهٔ «خروجی» در `/admin/audit` | `audit.export` | CSV یا Excel دقیقاً با فیلترهای فعال. |

- آیتم منوی «گزارش فعالیت» فقط با `audit.read.all` دیده می‌شود و هر دو صفحه
  در سمت سرور هم با `requirePermission("audit.read.all")` بسته‌اند؛ آدرس مستقیم
  بدون permission به `/forbidden` می‌رود.
- **خروجی:** تا ۲۵٬۰۰۰ ردیف فایل همان لحظه دانلود می‌شود. بیشتر از آن (سقف با
  `Auditing:Export:SynchronousRowThreshold` قابل تنظیم است)، پاسخ `202` با
  `jobId` است؛ پنل پیام «فایل در حال آماده‌سازی است» نشان می‌دهد، هر ۵ ثانیه
  وضعیت را می‌پرسد و وقتی آماده شد لینک دانلود می‌گذارد. `jobId` در
  `localStorage` مرورگر نگه داشته می‌شود تا با ترک صفحه و برگشت گم نشود. هیچ
  اعلان ایمیل یا پیامکی در کار نیست. خود خروجی با `Action = AuditExported` و
  فیلترهایش ثبت می‌شود.
- **محدودیت ADR-014:** گزارش فقط کارهای انجام‌شده از همین پنل را دارد. تا وقتی
  دسترسی به پنل خود پرتال باز است، نبودن یک رویداد به‌معنای «انجام نشده» نیست؛
  این جمله به‌صورت بنر ثابت بالای هر صفحهٔ گزارش آمده است. رکوردهای قدیمی‌تر از
  ۶ ماه (ADR-015) بایگانی و از نما حذف می‌شوند.
- مقادیر حساس: هر کلیدی در `Before`/`After` که به password، hash، secret یا
  token شبیه باشد، در پاسخ `GET /logs/{id}` پیش از خروج از سرور با `***`
  پوشانده می‌شود.
- مهاجرت تازهٔ این گام (`AddAuditLogParentEntity` در schema `audit`) را مثل بقیه
  با `dotnet ef database update` برای `AuditingDbContext` اجرا کن
  (`scripts/dev-up.sh` این کار را خودش می‌کند).

## متغیرهای محیطی (`.env`)

کلیدها در `.env.example` مستندند؛ هیچ مقدار واقعی در گیت نیست (ADR-039).

| کلید | توضیح |
| --- | --- |
| `MSSQL_SA_PASSWORD` | رمز اکانت `sa` در کانتینر `mssql` |
| `DATABASE__CONNECTIONSTRING` | رشتهٔ اتصال کامل Api به `mssql` |
| `PORTAL__BASEURL` | آدرس پایهٔ API مدیریتی پرتال |
| `PORTAL__BEARERTOKEN` | توکن سرویس‌اکانت؛ فقط در بک‌اند نگهداری می‌شود و نباید لاگ شود |
| `PORTAL__TESTPRODUCTID` | شناسهٔ محصول تستی برای آزمایش دستی خواندن |
| `PORTAL__ALLOWPRODUCTCREATION` | محافظ ایجاد واقعی محصول؛ پیش‌فرض `false` و برای فعال‌سازی باید صریحاً `true` شود |
| `PORTAL__RESTRICTWRITESTOTESTPRODUCT` | محافظ ADR-029؛ پیش‌فرض `true` یعنی هر نوشتن (ویرایش، قیمت/موجودی، حذف، انتشار) فقط روی `PORTAL__TESTPRODUCTID` مجاز است. با `false` نوشتن روی همهٔ محصولات واقعی باز می‌شود |
| `PORTAL__RATELIMITPERSECOND` | نرخ هدف سراسری؛ پیش‌فرض ۱.۵ درخواست در ثانیه |
| `PORTAL__RATELIMITQUEUECAPACITY` | ظرفیت صف درخواست‌های پرتال؛ پیش‌فرض ۲۰ |
| `PORTAL__RATELIMITQUEUETIMEOUTSECONDS` | بیشینهٔ انتظار در صف؛ پیش‌فرض ۱۵ ثانیه |
| `IDENTITY__SUPERADMIN__USERNAME` | نام کاربری اولین حساب SuperAdmin (فقط اگر هیچ SuperAdmin ای وجود نداشته باشد استفاده می‌شود) |
| `IDENTITY__SUPERADMIN__PASSWORD` | رمز اولیهٔ همان حساب؛ در اولین ورود اجباراً عوض می‌شود |
| `API_HTTP_PORT` | پورت باز شده به host فقط در حالت توسعه |
| `PROXY_HTTP_PORT` | ورودی مرورگر به reverse proxy در توسعه؛ پیش‌فرض ۸۰۰۰ |
| `INTERNAL_API_BASE_URL` | آدرس API در شبکهٔ داخلی برای Server Componentهای Next.js؛ پیش‌فرض `http://api:8080` |

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

cd src/Web
npx tsc --noEmit
npm run lint
npm test
npm run build
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
