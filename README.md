# NafasLand Admin

پنل مدیریت مستقل برای کارکنان نفس‌لند، روی API پرتال. این مخزن در حال حاضر
شامل **اسکلت راه‌رونده** (گام ۰ نقشهٔ راه) است: زیرساخت‌ها وایر شده‌اند، ولی
هیچ منطق محصولی یا تماسی با پرتال هنوز وجود ندارد. برای تصمیم‌های معماری به
`DECISIONS.md` و برای ترتیب کار به `ROADMAP.md` مراجعه کن.

## پیش‌نیازها

- .NET SDK 10 (LTS)
- Docker و Docker Compose (برای اجرای کامل استک با SQL Server)
- ابزار `dotnet-ef` برای اجرای مهاجرت‌ها:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

## اجرای محلی (بدون Docker)

۱. رشتهٔ اتصال و تنظیمات پرتال را با User Secrets تنظیم کن (این‌ها در گیت
نیستند و اجباری‌اند؛ بدون آن‌ها برنامه در استارتاپ بالا نمی‌آید):

```bash
cd src/Api
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Database=NafasLandAdmin;User Id=sa;Password=<رمز>;TrustServerCertificate=True;"
dotnet user-secrets set "Portal:TestProductId" "<شناسهٔ محصول تستی>"
```

`Portal:BaseUrl` و `Portal:RateLimitPerSecond` مقدار پیش‌فرض در
`appsettings.json` دارند و در این گام هیچ تماسی با پرتال زده نمی‌شود؛ فقط
اعتبارسنجی کانفیگ در استارتاپ امتحان می‌شود (ADR-039).

۲. یک SQL Server در دسترس داشته باش (مثلاً همان کانتینر `mssql` از
`compose.yaml`، یا یک نصب محلی).

۳. مهاجرت را اعمال کن (مهاجرت به‌صورت خودکار در استارتاپ اجرا نمی‌شود؛
ADR-041):

```bash
dotnet ef database update \
  --project src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj
```

۴. اجرا:

```bash
dotnet run --project src/Api/NafasLand.Admin.Api.csproj
```

## اجرا با Docker Compose

```bash
cp .env.example .env
# مقدارهای واقعی را در .env پر کن: MSSQL_SA_PASSWORD، DATABASE__CONNECTIONSTRING،
# PORTAL__BASEURL، PORTAL__TESTPRODUCTID

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
استارتاپ آن را متوقف می‌کند)، مهاجرت را از host اجرا کن — با
`compose.override.yaml`، پورت ۱۴۳۳ به host باز است:

```bash
dotnet ef database update \
  --project src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj \
  --startup-project src/Api/NafasLand.Admin.Api.csproj \
  --connection "Server=localhost,1433;Database=NafasLandAdmin;User Id=sa;Password=<همان MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"
```

بررسی سلامت: `curl http://localhost:8080/health`.

## اندپوینت‌های نمونه (ماژول Sample)

این ماژول فقط برای اثبات کارکرد pipeline است و هیچ ربطی به محصولات ندارد؛
در گام‌های بعد حذف یا جایگزین می‌شود.

- `POST /api/v1/sample/ping` با بدنهٔ `{ "message": "..." }` — یک رکورد در
  جدول `sample.PingRecords` می‌نویسد. permission لازم: `sample.ping`.
- `POST /api/v1/sample/unprotected-ping` — همیشه با ۴۰۳ رد می‌شود، چون
  command اش هیچ permission ای تعریف نکرده (پیش‌فرض بسته، ADR-006).

احراز هویت واقعی کار گام ۱ است؛ فعلاً یک کاربر ساختگی همیشه احراز
هویت‌شده وجود دارد که permissionهایش از هدر `X-Test-Permissions` (رشتهٔ
جداشده با کاما) خوانده می‌شود:

```bash
# رد می‌شود: هیچ permission ای پاس داده نشده
curl -i -X POST http://localhost:8080/api/v1/sample/ping \
  -H "Content-Type: application/json" -d '{"message":"سلام"}'

# قبول می‌شود
curl -i -X POST http://localhost:8080/api/v1/sample/ping \
  -H "Content-Type: application/json" -H "X-Test-Permissions: sample.ping" \
  -d '{"message":"سلام"}'

# همیشه ۴۰۳ (بدون permission تعریف‌شده روی command)
curl -i -X POST http://localhost:8080/api/v1/sample/unprotected-ping \
  -H "X-Test-Permissions: sample.ping"
```

## متغیرهای محیطی (`.env`)

کلیدها در `.env.example` مستندند؛ هیچ مقدار واقعی در گیت نیست (ADR-039).

| کلید | توضیح |
| --- | --- |
| `MSSQL_SA_PASSWORD` | رمز اکانت `sa` در کانتینر `mssql` |
| `DATABASE__CONNECTIONSTRING` | رشتهٔ اتصال کامل Api به `mssql` |
| `PORTAL__BASEURL` | آدرس پایهٔ API پرتال (هنوز هیچ تماسی زده نمی‌شود) |
| `PORTAL__TESTPRODUCTID` | شناسهٔ محصول تستی (هنوز ساخته نشده؛ نک. «موارد باز» پایین) |
| `API_HTTP_PORT` | پورت باز شده به host فقط در حالت توسعه |

## Hangfire

Job runner این گام Hangfire است (ADR-012)، فقط وایر شده — هیچ job واقعی‌ای
تعریف نشده. Hangfire هنگام اتصال، schema و جدول‌های داخلی خودش را زیر
schema به نام `hangfire` می‌سازد؛ این رفتار خود کتابخانه است و به قاعدهٔ
«مهاجرت خودکار در استارتاپ اجرا نمی‌شود» (ADR-041) که مخصوص مهاجرت‌های
EF Core ماژول‌هاست مربوط نیست.

## تست

```bash
dotnet build NafasLand.Admin.sln
dotnet test NafasLand.Admin.sln
```

هیچ تستی به SQL Server واقعی وصل نمی‌شود؛ تست‌های handler با یک
`SampleDbContext` پیکربندی‌شده روی رشتهٔ اتصال ساختگی کار می‌کنند (فقط
`Add` به ChangeTracker را امتحان می‌کنند، نه اتصال واقعی)، و تست معماری با
پارس فایل‌های `.csproj` و reflection روی اسمبلی‌های ساخته‌شده کار می‌کند.

## موارد باز (نیاز به تصمیم یا اطلاعات بیرونی)

- ساخت محصول تستی واقعی در پرتال و ثبت شناسه‌اش (`Portal:TestProductId`) —
  کار موازی روی ROADMAP.
- گام ۱ (Identity) باید مشخص کند احراز هویت واقعی چطور جایگزین
  `TestUserAuthenticationHandler` می‌شود؛ فعلاً این هندلر صرفاً یک
  جایگزین موقت برای تست pipeline است.
