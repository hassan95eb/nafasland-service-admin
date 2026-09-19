# پرامپت گام ۱ — Identity و مدل دسترسی

> این متن را به‌عنوان پرامپت به AI کدنویس بده. پیش از اجرا، مطمئن شو `AGENTS.md`، `DECISIONS.md`، `ROADMAP.md` و کد گام ۰ (برنچ `step-00-walking-skeleton`، احتمالاً حالا روی `master`) در دسترس آن هستند.

---

تو روی مخزن `nafasland-service-admin` کار می‌کنی. پیش از هر کاری `AGENTS.md` را بخوان و همهٔ قواعدش را رعایت کن. جزئیات هر تصمیم در `DECISIONS.md` با شمارهٔ ADR آمده است. این مرحله روی زیرساخت گام ۰ سوار می‌شود — آن را دوباره نساز، فقط گسترش بده.

## قواعد همکاری (الزامی)
## گیت

1. **کامیت و پوش نکن.** تغییرات را فقط در فایل‌ها اعمال کن و همان‌جا رها کن. کاری با `git commit`، `git push`، `git rebase`، `git reset` یا تغییر تاریخچه نداشته باش.
2. **متن کامیت را پیشنهاد بده، نه اجرا.** در پایان کار، متن پیشنهادی کامیت را به‌صورت یک بلوک جدا در پاسخت بنویس تا بررسی و تأیید شود. فرمت Conventional Commits با بدنه‌ای که **دلیل** تغییر را توضیح می‌دهد، نه فهرست فایل‌ها.
3. **هیچ اشاره‌ای به هوش مصنوعی در متن کامیت نباشد** — نه در عنوان، نه در بدنه، نه به‌صورت `Co-Authored-By` یا هر امضای مشابه. کامیت باید طوری نوشته شود که انگار توسعه‌دهنده نوشته است.
4. **هر مرحله روی برنچ جداگانه.** پیش از شروع، برنچ جدید از `master` بساز با نام `step-01-identity-and-access`. روی `master` مستقیم کار نکن.

## مرور و ادغام

5. **برنچ را خودت merge نکن.** کار روی برنچ مرحله تمام می‌شود و همان‌جا می‌ماند تا مرور و ادغام شود.
6. در پایان، خلاصه‌ای در قالب توضیحات Pull Request بنویس: چه چیزی ساخته شد، چرا این‌طور، کدام معیارهای پذیرش بررسی و تأیید شدند، و چه چیزی باز مانده.
7. **force push و بازنویسی تاریخچه ممنوع است**، در هیچ شرایطی.

## دامنهٔ تغییرات

8. **فقط کاری را انجام بده که این پرامپت خواسته.** هیچ refactor، تغییر نام، مرتب‌سازی import یا فرمت مجدد فایل‌های بی‌ربط با ماژول Sample یا زیرساخت گام ۰، مگر جایی که همین پرامپت صریحاً بگوید.
9. **`DECISIONS.md`، `AGENTS.md` و `ROADMAP.md` را تغییر نده.** اگر جایی از آن‌ها اشتباه یا ناقص بود، در پاسخت گزارش کن تا تصمیم گرفته شود.
10. **مهاجرت‌های موجود را ویرایش یا حذف نکن.** مهاجرت جدید اضافه کن.
11. **وابستگی جدید بدون تأیید اضافه نکن**، به‌جز `Konscious.Security.Cryptography.Argon2` نسخهٔ `1.3.1` که پایین در بخش «استک» از پیش تأیید شده. اگر به بستهٔ دیگری نیاز شد، اسمش و دلیلش را بگو و منتظر بمان.
12. نسخهٔ .NET، پکیج‌منیجر یا ابزارهای پایه را عوض نکن.

## ایمنی

13. **هیچ دستور مخربی اجرا نکن**: `rm -rf`، drop کردن دیتابیس، پاک کردن فایل‌های کاربر.
14. **هیچ مقدار حساسی را ننویس و چاپ نکن** — توکن، رمز، رشتهٔ اتصال. حتی در لاگ نمونه یا کامنت. رمز اولیهٔ سوپرادمین فقط از متغیر محیطی خوانده می‌شود، هرگز هاردکد نمی‌شود.
15. **به API واقعی پرتال درخواست نزن.** این مرحله اصلاً کاری به پرتال ندارد.

## کیفیت

16. **اگر جایی مبهم بود، حدس نزن.** فهرست ابهام‌ها را بنویس و بپرس.
17. **کد زائد تولید نکن.**
18. **پیش از تحویل، `dotnet build` و `dotnet test` را اجرا کن** و نتیجه را گزارش بده.
19. **در پایان سه چیز گزارش کن:** خلاصهٔ آنچه ساخته شد، هر جایی که از پرامپت منحرف شدی و چرا، و فهرست سؤال‌ها یا موارد باز.

---

## هدف این مرحله

جایگزین کردن کاربر ساختگی گام ۰ (`TestUserAuthenticationHandler` و هدر `X-Test-Permissions`) با یک مدل کاربر/نقش/permission واقعی، ورود واقعی با کوکی، و اولین حساب SuperAdmin. بعد از این مرحله، `POST /api/v1/sample/ping` باید با یک کاربر واقعاً واردشده کار کند، نه با یک هدر تستی.

**این مرحله فرانت‌اند نمی‌سازد.** صفحهٔ «دسترسی‌های کاربر» که در `ROADMAP.md` برای این گام آمده، یعنی این مرحله APIهایی می‌سازد که آن صفحه در گام ۴ (وقتی فرانت‌اند Next.js وجود دارد) مستقیماً از رویشان بالا می‌آید — نه خودِ صفحه. اگر این تعبیر اشتباه است و کارفرما یک رابط کاربری همین حالا می‌خواهد، این را به‌عنوان ابهام گزارش کن، پیاده‌سازی نکن.

## استک (افزودهٔ گام ۱)

- `Konscious.Security.Cryptography.Argon2`، نسخهٔ `1.3.1` — **از پیش تأیید شده**، برای هش Argon2id طبق ADR-023. آن را به `Directory.Packages.props` اضافه کن.
- `Microsoft.AspNetCore.Authentication.Cookies` و `Microsoft.AspNetCore.Antiforgery` — بخشی از shared framework ASP.NET Core هستند؛ نیازی به `PackageVersion` جدا ندارند.
- هیچ کتابخانهٔ دیگری (از جمله `Microsoft.AspNetCore.Identity`) اضافه نشود. مدل داده کاملاً سفارشی است (ADR-001/003/021)؛ Identity framework پیش‌فرض مایکروسافت با این مدل permission-محور و Grant/Deny تطابق ندارد و اضافه‌کردنش یعنی دور ریختن نصفش.

## ماژول جدید: `Modules/Identity`

مثل ماژول Sample در گام ۰، همان ساختار (ADR-004/044):

```
src/Modules/Identity/
├─ Contracts/              فقط چیزی که ماژول‌های دیگر لازم دارند: IEffectivePermissionsProvider (پایین توضیح داده شده)
├─ Features/
│  ├─ Login/               LoginCommand، Handler، Validator، Endpoint
│  ├─ Logout/
│  ├─ ChangePassword/
│  ├─ ResetPassword/       فقط SuperAdmin
│  ├─ CreateUser/          فقط SuperAdmin
│  ├─ SetUserRoles/        فقط SuperAdmin
│  ├─ SetUserPermission/   Grant/Deny/حذف، فقط SuperAdmin
│  ├─ SetRolePermissions/  فقط SuperAdmin، نه روی نقش SuperAdmin (پایین توضیح)
│  ├─ ToggleUserActive/    فعال/غیرفعال‌سازی، فقط SuperAdmin
│  └─ Queries/             GetMe، ListUsers، GetUser، ListPermissions، ListRoles — این‌ها خواندنی‌اند و می‌توانند ساده‌تر از الگوی Command/Handler پیاده شوند (مثلاً یک endpoint که مستقیم از DbContext می‌خواند)، چون چیزی برای Audit یا Authorization پیچیده ندارند بجز چک permission
├─ Persistence/             IdentityDbContext (schema به نام `identity`)، Configurations، Migrations
├─ IdentityPermissions.cs   کلیدهای permission این ماژول
└─ IdentityModule.cs
```

## مدل داده

```
AppUser
  Id (Guid), Username (unique), PasswordHash, PasswordAlgorithm,
  MustChangePassword (bool), IsActive (bool, پیش‌فرض true),
  IsProtected (bool, پیش‌فرض false),
  FailedLoginCount (int), LockedUntil (DateTimeOffset?),
  TwoFactorEnabled (bool, پیش‌فرض false), TwoFactorSecret (string?, رمزنگاری‌نشده فعلاً استفاده نمی‌شود),
  CreatedAt, CreatedByUserId?

Role
  Id (Guid), Name (unique, مثل "SuperAdmin"، "Admin"),
  IsSystemManaged (bool) — true فقط برای نقش SuperAdmin؛ یعنی SetRolePermissions رویش رد می‌شود

Permission
  Id (Guid), Key (unique, "<resource>.<action>"), ModuleName, IsSuperAdminOnly (bool)

UserRole      (UserId, RoleId)                — many-to-many، ADR-003
RolePermission(RoleId, PermissionId)          — many-to-many
UserPermission(UserId, PermissionId, Effect)  — Effect: Grant یا Deny، ADR-021
```

قواعد:
- `Permission` هر بار در استارتاپ از `IModule.Permissions` همهٔ ماژول‌های رجیستر‌شده همگام می‌شود (upsert بر اساس `Key`؛ چیزی حذف نمی‌شود، حتی اگر یک ماژول موقتاً خاموش باشد — همان الگویی که گام ۰ برای seed اولیهٔ permission پایه گذاشت، حالا این‌جا واقعاً در جدول ذخیره می‌شود).
- نقش `SuperAdmin` بعد از هر همگام‌سازی permission، خودش را با **همهٔ** permissionهای موجود پر می‌کند (از جمله `IsSuperAdminOnly`). این تنها راهی است که SuperAdmin بدون دست‌کاری دستی به‌روز می‌ماند وقتی ماژول تازه‌ای permission اضافه می‌کند. به همین دلیل `SetRolePermissions` روی این نقش رد می‌شود (`IsSystemManaged = true`) — دستکاری دستی‌اش بی‌معناست، چون همگام‌سازی بعدی رویش می‌نویسد.
- نقش `Admin` در استارتاپ فقط اگر وجود نداشت ساخته می‌شود، **بدون هیچ permission اولیه**. تخصیص permission به آن کار سوپرادمین از طریق `SetRolePermissions` است (خارج از دامنهٔ این گام چون هنوز هیچ permission غیر از `sample.ping` و کلیدهای همین ماژول Identity وجود ندارد؛ فقط زیرساختش آماده باشد).
- permission مؤثر کاربر = (اتحاد permissionهای نقش‌هایش) + (کلیدهایی با `Effect=Grant` در `UserPermission`) − (کلیدهایی با `Effect=Deny`). `Deny` همیشه برنده است، حتی اگر همان کلید از یک نقش هم بیاید.

## Permissionهای این ماژول (`IdentityPermissions`)

- `identity.users.manage` — ساخت کاربر، تغییر نقش‌ها، فعال/غیرفعال‌سازی، ریست رمز دیگران.
- `identity.access.manage`، **`IsSuperAdminOnly = true`** — دیدن و ویرایش permissionهای نقش‌ها و Grant/Deny مستقیم کاربران. (چون خودِ کنترل دسترسی است؛ اگر Admin هم این را داشته باشد می‌تواند به خودش هر permission دیگری را بدهد.)

## احراز هویت واقعی (جایگزینی گام ۰)

- `TestUserAuthenticationHandler` و هر ارجاعی به هدر `X-Test-Permissions` حذف شود.
- Cookie authentication واقعی وایر شود: کوکی `HttpOnly` + `Secure` + `SameSite=Strict` (ADR-013/023). نام کوکی و مدت انقضا را خودت با یک مقدار معقول (مثلاً ۸ ساعت، sliding) انتخاب کن و در README بنویس.
- **`IAllowAnonymousCommand`** یک مارکر تازه در `Shared.Kernel` (کنار `ICommand`/`IRequiresPermission` گام ۰): `AuthorizationBehavior` باید اول چک کند اگر command این مارکر را دارد، بدون هیچ چک permission رد شود (پیش‌نیاز اجرای `LoginCommand` وقتی هنوز هیچ کاربری وارد نشده). فقط `LoginCommand` این مارکر را می‌گیرد؛ هیچ command دیگری، حتی در ماژول Identity.
- Endpoint خود Login هم در سطح ASP.NET Core routing با `.AllowAnonymous()` علامت بخورد؛ همهٔ endpointهای دیگر (شامل logout و بقیهٔ Identity) پشت احراز هویت باشند.
- بعد از ورود موفق، permissionهای مؤثر کاربر به‌صورت `IClaimsTransformation` در **هر درخواست** دوباره از دیتابیس محاسبه و به claims اضافه شوند (نه فقط لحظهٔ ورود) — چون بعد از یک Grant/Deny یا تغییر نقش توسط سوپرادمین، باید بدون نیاز به ورود دوباره اثر کند. `AuthorizationBehavior` دقیقاً مثل گام ۰ از روی claims چک می‌کند؛ فقط منبع claims عوض شده.
- `IEffectivePermissionsProvider` (در `Contracts` ماژول Identity) رابطی است که همین محاسبه را انجام می‌دهد؛ `Shared.Infrastructure` یا `Api` آن را برای `IClaimsTransformation` مصرف می‌کند بدون رفرنس مستقیم به داخل ماژول Identity (فقط `Contracts`).

## قفل حساب (ADR-023)

- ۵ تلاش ناموفق پیاپی → `LockedUntil = اکنون + 15 دقیقه`. تلاش موفق، شمارنده را صفر می‌کند.
- تلاش ورود در بازهٔ قفل، بدون افزایش شمارنده، با پیام روشن رد می‌شود (کد پیشنهادی: `423 Locked`).
- حساب غیرفعال (`IsActive = false`) هم اجازهٔ ورود ندارد؛ پیام آن با پیام قفل یکی نباشد تا قابل تفکیک باشد.

## Seed سوپرادمین (ADR-022)

- در استارتاپ، اگر هیچ کاربری با نقش `SuperAdmin` وجود نداشت، یکی ساخته شود: `Username` و رمز اولیه از متغیر محیطی (مثلاً `IDENTITY__SUPERADMIN__USERNAME`، `IDENTITY__SUPERADMIN__PASSWORD`؛ اگر نبودند، برنامه با پیام روشن بالا نیاید — همان الگوی اعتبارسنجی کانفیگ ADR-039 از گام ۰).
- `IsProtected = true`، `MustChangePassword = true`.
- endpointهای `ToggleUserActive`، `SetUserRoles` و هر چیزی که این حساب را حذف/غیرفعال/تنزل‌نقش کند، اگر هدف `IsProtected` باشد، `403` بدهند — حتی اگر درخواست‌کننده خودش SuperAdmin دیگری باشد.

## اجبار تغییر رمز در اولین ورود

- تا وقتی `MustChangePassword = true`، هر command دیگری غیر از `ChangePasswordCommand` و `LogoutCommand` برای همان کاربر با یک خطای مشخص (نه ۴۰۳ معمولی permission — یک کد/پیام جدا مثل `PASSWORD_CHANGE_REQUIRED`) رد شود. این چک را در `AuthorizationBehavior` یا یک behavior تازهٔ قبل از آن بگذار؛ توضیح بده کدام را انتخاب کردی و چرا.

## Antiforgery

- سرویس `Antiforgery` داخلی ASP.NET Core وایر شود. `LoginCommand` موفق، هم کوکی سشن و هم کوکی/توکن antiforgery را برمی‌گرداند.
- همهٔ endpointهای غیر-GET (به‌جز خودِ Login که هنوز کوکی ندارد) باید توکن antiforgery معتبر بخواهند؛ نبودش یا نامعتبر بودنش باید `400` بدهد، جدا از `401`/`403` هویت/دسترسی.

## Endpointها (زیر `/api/v1/identity`)

| Method & مسیر | Command/Query | دسترسی |
| --- | --- | --- |
| `POST /auth/login` | `LoginCommand` | Anonymous |
| `POST /auth/logout` | `LogoutCommand` | هر کاربر واردشده |
| `GET /auth/me` | Query | هر کاربر واردشده |
| `POST /auth/change-password` | `ChangePasswordCommand` | هر کاربر واردشده |
| `POST /users` | `CreateUserCommand` | `identity.users.manage` |
| `GET /users` | Query | `identity.users.manage` |
| `GET /users/{id}` | Query | `identity.users.manage` |
| `POST /users/{id}/reset-password` | `ResetPasswordCommand` | `identity.users.manage` |
| `PUT /users/{id}/roles` | `SetUserRolesCommand` | `identity.users.manage` |
| `POST /users/{id}/toggle-active` | `ToggleUserActiveCommand` | `identity.users.manage` |
| `PUT /users/{id}/permissions/{permissionKey}` | `SetUserPermissionCommand` (بدنه: `{ effect: "Grant" \| "Deny" \| null }`) | `identity.access.manage` |
| `GET /permissions` | Query | `identity.access.manage` |
| `GET /roles` | Query | `identity.access.manage` |
| `PUT /roles/{roleId}/permissions` | `SetRolePermissionsCommand` | `identity.access.manage`، و رد با ۴۰۹ یا ۴۰۰ روی نقش `IsSystemManaged` |

`SetUserPermissionCommand` و `SetRolePermissionsCommand` باید هر permission ای که `IsSuperAdminOnly = true` دارد را رد کنند (۴۰۰ با پیام روشن) وقتی هدف یک Grant به یک کاربر/نقش غیر-SuperAdmin است.

## چیزی که به Sample module اضافه/عوض نمی‌شود

هیچ کد ماژول Sample تغییر نمی‌کند. رفتار جدیدش خودکار است: چون `AuthorizationBehavior` حالا از احراز هویت واقعی claims می‌خواند، `POST /api/v1/sample/ping` بعد از ورود با کاربری که permission `sample.ping` دارد کار می‌کند و README باید مثال‌های `curl` قدیمی مبتنی بر `X-Test-Permissions` را با نمونهٔ ورود واقعی (login → استفاده از کوکی برگشتی → درخواست بعدی) جایگزین کند.

## AuditLog — عمداً هنوز نه

ADR-023 می‌گوید ورود موفق/ناموفق، قفل شدن و ریست رمز باید لاگ شوند، ولی جدول واقعی `AuditLog` کار گام ۲ است (`AuditBehavior` در گام ۰ فقط یک اسکلت بی‌اثر است). این مرحله جدول AuditLog نمی‌سازد. فقط مطمئن شو همهٔ commandهای این ماژول (`LoginCommand` هم، با وجود `IAllowAnonymousCommand`) از همان شش‌مرحله‌ای pipeline رد می‌شوند، همان‌طور که Sample's PingCommand رد شد — طوری که وقتی گام ۲ `AuditBehavior` را پر کرد، هیچ چیزی در ماژول Identity نیاز به تغییر نداشته باشد.

## تست‌های الزامی

- **محاسبهٔ permission مؤثر**: تست واحد که (نقش‌ها + Grant − Deny) را با چند سناریو (شامل تداخل Grant/Deny روی یک کلید) می‌سنجد — این دقیقاً معیار پذیرش ROADMAP است.
- قفل بعد از ۵ تلاش، و باز شدنش بعد از گذشت زمان (با یک ساعت تزریقی/قابل‌موکاپ، نه `Thread.Sleep`).
- رد `SetRolePermissions` روی نقش `IsSystemManaged`.
- رد Grant/Deny روی permission با `IsSuperAdminOnly = true`.
- رد عملیات مخرب روی کاربر `IsProtected`.
- `LoginCommand` با `IAllowAnonymousCommand` واقعاً از `AuthorizationBehavior` بدون چک رد می‌شود؛ یک command ساختگی دیگر بدون این مارکر و بدون permission، طبق رفتار گام ۰، رد می‌شود (رگرسیون).

## معیار پذیرش

1. `dotnet build` و `dotnet test` سبز.
2. ورود با رمز درست، کوکی سشن + توکن antiforgery برمی‌گرداند.
3. ورود با رمز غلط پنج بار پیاپی، حساب را ۱۵ دقیقه قفل می‌کند؛ تلاش ششم حتی با رمز درست رد می‌شود.
4. `POST /api/v1/sample/ping` با کوکی کاربری که permission `sample.ping` دارد کار می‌کند؛ با کاربری که ندارد، `403` می‌دهد.
5. permission با `IsSuperAdminOnly=true` از `PUT /roles/{roleId}/permissions` یا `PUT /users/{id}/permissions/{key}` به نقش/کاربر غیر-SuperAdmin قابل‌اعطا نیست.
6. محاسبهٔ permission مؤثر (نقش‌ها + Grant − Deny) تست واحد دارد و سبز است.
7. حساب seed‌شدهٔ SuperAdmin با `MustChangePassword=true` بالا می‌آید؛ قبل از تغییر رمز، هیچ command دیگری (به‌جز change-password و logout) از آن پذیرفته نمی‌شود.
8. عملیات غیرفعال‌سازی/تغییر نقش/حذف روی کاربر `IsProtected` همیشه `403` می‌دهد، حتی برای سوپرادمین دیگر.
9. درخواست تغییردهنده بدون توکن antiforgery معتبر، `400` می‌دهد.
10. `git status` بعد از بیلد تمیز است.

## آنچه در این مرحله **نباید** انجام شود

- هیچ فرانت‌اندی، حتی یک صفحهٔ HTML ساده برای تست دستی. تست از طریق `curl`/تست خودکار.
- هیچ جدول یا نوشتن واقعی `AuditLog` (بالا توضیح داده شد).
- هیچ پیاده‌سازی واقعی TOTP/دومرحله‌ای — فقط فیلدهای رزروشده در مدل.
- هیچ permission یا نقشی مخصوص Catalog/محصول (آن گام‌های بعدی‌اند).
- هیچ ایمیل یا پیامک برای ریست رمز — همان‌طور که ADR-023 گفته، ریست فقط دستی توسط سوپرادمین است.
- هیچ کتابخانهٔ اضافه‌ای غیر از موردی که در «استک» تأیید شد.

## خروجی

- کد در همین مخزن، روی برنچ `step-01-identity-and-access`.
- یک یا چند کامیت Conventional Commits که بدنه‌شان دلیل تصمیم‌های پیاده‌سازی (نه فهرست فایل) را بگوید.
- بخش «احراز هویت» `README.md` به‌روز شود: نحوهٔ ورود واقعی، متغیرهای محیطی جدید سوپرادمین، و نمونهٔ `curl` جدید برای ping (جایگزین `X-Test-Permissions`).
- اگر جایی از این پرامپت یا از ADRهای مرتبط (۰۰۱، ۰۰۲، ۰۰۳، ۰۲۱، ۰۲۲، ۰۲۳) با هم یا با گام ۰ نمی‌خواند، **حدس نزن** — فهرست کن و بپرس.
