# پرامپت گام ۵ — مدیریت ادمین‌ها و دسترسی‌ها

> این متن را به‌عنوان پرامپت به AI کدنویس بده. پیش از اجرا، مطمئن شو `AGENTS.md`، `DECISIONS.md`، `ROADMAP.md` و کد گام‌های ۰ تا ۴ (روی `master`) در دسترس آن هستند.

---

تو روی مخزن `nafasland-service-admin` کار می‌کنی. پیش از هر کاری `AGENTS.md` را بخوان و همهٔ قواعدش را رعایت کن. جزئیات هر تصمیم در `DECISIONS.md` با شمارهٔ ADR آمده است. **بک‌اند این مرحله تقریباً کامل در گام ۱ ساخته شده** — ماژول `Identity` همین حالا کاربر/نقش/permission، ساخت کاربر، ریست رمز، فعال/غیرفعال‌سازی، تغییر نقش و Grant/Deny مستقیم permission را دارد. کار این مرحله عمدتاً **UI** است روی فرانت‌اندی که گام ۴ ساخته (Next.js، سایدبار داده‌محور، الگوی fetch wrapper + antiforgery، React Query)، به‌اضافهٔ یک تغییر دقیق و کوچک در بک‌اند که پایین توضیح داده شده.

## قواعد همکاری (الزامی)
## گیت

1. **کامیت و پوش نکن.** تغییرات را فقط در فایل‌ها اعمال کن و همان‌جا رها کن. کاری با `git commit`، `git push`، `git rebase`، `git reset` یا تغییر تاریخچه نداشته باش.
2. **متن کامیت را پیشنهاد بده، نه اجرا.** در پایان کار، متن پیشنهادی کامیت را به‌صورت یک بلوک جدا در پاسخت بنویس تا بررسی و تأیید شود. فرمت Conventional Commits با بدنه‌ای که **دلیل** تغییر را توضیح می‌دهد، نه فهرست فایل‌ها.
3. **هیچ اشاره‌ای به هوش مصنوعی در متن کامیت نباشد** — نه در عنوان، نه در بدنه، نه به‌صورت `Co-Authored-By` یا هر امضای مشابه. کامیت باید طوری نوشته شود که انگار توسعه‌دهنده نوشته است.
4. **هر مرحله روی برنچ جداگانه.** پیش از شروع، برنچ جدید از `master` بساز با نام `step-05-admin-management`. روی `master` مستقیم کار نکن.

## مرور و ادغام

5. **برنچ را خودت merge نکن.** کار روی برنچ مرحله تمام می‌شود و همان‌جا می‌ماند تا مرور و ادغام شود.
6. در پایان، خلاصه‌ای در قالب توضیحات Pull Request بنویس: چه چیزی ساخته شد، چرا این‌طور، کدام معیارهای پذیرش بررسی و تأیید شدند، و چه چیزی باز مانده.
7. **force push و بازنویسی تاریخچه ممنوع است**، در هیچ شرایطی.

## دامنهٔ تغییرات

8. **فقط کاری را انجام بده که این پرامپت خواسته.** هیچ refactor، تغییر نام، مرتب‌سازی import یا فرمت مجدد فایل‌های بی‌ربط با فهرست محصولات گام ۴، مگر جایی که همین پرامپت صریحاً بگوید.
9. **`DECISIONS.md`، `AGENTS.md` و `ROADMAP.md` را تغییر نده.** اگر جایی از آن‌ها اشتباه یا ناقص بود، در پاسخت گزارش کن تا تصمیم گرفته شود.
10. **مهاجرت‌های موجود را ویرایش یا حذف نکن.** مهاجرت جدید اضافه کن (این مرحله دقیقاً یک مهاجرت کوچک لازم دارد، پایین توضیح داده شده).
11. **وابستگی جدید بدون تأیید اضافه نکن.** فرانت‌اند فعلاً از کامپوننت‌های دستی Tailwind در `shared/ui` استفاده می‌کند (نه shadcn/Radix)؛ همین الگو را ادامه بده — برای فرم مودال یا select هم کامپوننت دستی بساز، کتابخانهٔ UI جدید اضافه نکن. اگر واقعاً لازم شد، اسم بسته و دلیلش را بگو و منتظر بمان.
12. نسخهٔ .NET، Next.js، پکیج‌منیجر یا ابزارهای پایه را عوض نکن.

## ایمنی

13. **هیچ دستور مخربی اجرا نکن**: `rm -rf`، drop کردن دیتابیس، پاک کردن فایل‌های کاربر.
14. **هیچ مقدار حساسی را ننویس و چاپ نکن** — توکن، رمز، رشتهٔ اتصال. حتی رمز موقتی که برای یک ادمین تازه تولید می‌شود، نباید در لاگ یا کامنت افتد؛ فقط یک‌بار در پاسخ HTTP و در UI نمایش داده شود.
15. **به API واقعی پرتال درخواست نزن.** این مرحله اصلاً کاری به پرتال ندارد — دقیقاً همین بی‌ریسک‌بودن دلیل جای گرفتنش پیش از گام ۶ (اولین نوشتن روی محصول واقعی) است.

## کیفیت

16. **اگر جایی مبهم بود، حدس نزن.** فهرست ابهام‌ها را بنویس و بپرس.
17. **کد زائد تولید نکن.**
18. **پیش از تحویل، `dotnet build`، `dotnet test` و `npm run build`/`npm run lint` (در `src/Web`) را اجرا کن** و نتیجه را گزارش بده.
19. **در پایان سه چیز گزارش کن:** خلاصهٔ آنچه ساخته شد، هر جایی که از پرامپت منحرف شدی و چرا، و فهرست سؤال‌ها یا موارد باز.

---

## هدف این مرحله

یک صفحهٔ «مدیریت ادمین‌ها» در فرانت‌اند که رویش دستی بشود: ادمین تازه ساخت، فعال/غیرفعال کرد، رمزش را ریست کرد، نقش‌هایش را عوض کرد و permissionهای مستقیمش را Grant/Deny کرد. به‌علاوه جایگزینی بنر «رمز باید تغییر کند» گام ۴ با یک صفحهٔ واقعی تغییر رمز اجباری. همهٔ این‌ها روی endpointهایی سوار می‌شود که همین حالا در ماژول `Identity` وجود دارند (جدول پایین)؛ فقط یک نقص کوچک در بک‌اند باید پر شود (بخش «تغییر لازم در بک‌اند»).

**این مرحله بخشی از یک تصمیم قدیمی‌تر را هم می‌بندد:** ADR-046 از قبل جلوی گرفتن نقش SuperAdmin توسط یک Admin عادی را گرفته؛ این مرحله فقط UI‌اش را روی این رفتار می‌سازد و تستش می‌کند — منطق امنیتی‌اش را دوباره پیاده نمی‌کند. یک تناقض permission هم حین نوشتن همین پرامپت پیدا و با **ADR-052** بسته شد (فرم تغییر نقش‌ها به فهرست نقش‌ها نیاز دارد ولی فقط `identity.users.manage` دارد، نه `identity.access.manage` که `GET /roles` پشتش است) — جزئیاتش در بخش «تغییر لازم در بک‌اند» و ردیف `GET /roles/summary` در جدول پایین آمده.

## آنچه از قبل آماده است (فقط رجوع کن، دوباره نساز)

همهٔ اینها زیر `/api/v1/identity` و پشت `RequireAuthorization()` هستند (به‌جز login):

| Method و مسیر | بدنهٔ درخواست | بدنهٔ پاسخ (فیلدهای اصلی) | دسترسی |
| --- | --- | --- | --- |
| `POST /users` | `{ username, initialPassword }` | `{ id, username }` | `identity.users.manage` |
| `GET /users` | — | آرایه: `{ id, username, isActive, isProtected, mustChangePassword, roles: string[] }` | `identity.users.manage` |
| `GET /users/{id}` | — | `{ id, username, isActive, isProtected, mustChangePassword, createdAt, roles: string[], directPermissions: [{ key, effect: "Grant"\|"Deny" }] }` | `identity.users.manage` |
| `POST /users/{id}/reset-password` | `{ newPassword }` | — | `identity.users.manage` |
| `PUT /users/{id}/roles` | `{ roleIds: guid[] }` (جایگزینی کامل، نه diff) | — | `identity.users.manage` |
| `POST /users/{id}/toggle-active` | — | `{ isActive }` | `identity.users.manage` |
| `PUT /users/{id}/permissions/{permissionKey}` | `{ effect: "Grant"\|"Deny"\|null }` (`null` یعنی حذف override) | — | `identity.access.manage` |
| `GET /permissions` | — | آرایه: `{ key, moduleName, isSuperAdminOnly }` — **بعد از تغییر بک‌اند این مرحله، `displayName` هم اضافه می‌شود** | `identity.access.manage` |
| `GET /roles` | — | آرایه: `{ id, name, isSystemManaged, permissionKeys: string[] }` — شامل نقشهٔ کامل permissionهای هر نقش، عمداً پشت `identity.access.manage` (ADR-052) | `identity.access.manage` |
| `GET /roles/summary` | — | آرایه: `{ id, name, isSystemManaged }`، **بدون `permissionKeys`** — این مرحله می‌سازدش (ADR-052) | `identity.users.manage` |
| `PUT /roles/{roleId}/permissions` | `{ permissionKeys: string[] }` (جایگزینی کامل) | — | `identity.access.manage` |
| `POST /auth/change-password` | `{ currentPassword, newPassword }` | — | هر کاربر واردشده، حتی وقتی `mustChangePassword=true` |

رفتارهای امنیتی که همین حالا در بک‌اند هست و این مرحله فقط رویشان UI می‌سازد:
- کاربر `isProtected` (سوپرادمین seed‌شده) را نمی‌شود غیرفعال کرد یا نقشش را عوض کرد — `403` می‌گیرد. پاسخ خطا را در UI معمولی (پیام از `ProblemDetails`) نشان بده، حدس نزن که یعنی این دکمه باید مخفی شود؛ اگر فکر می‌کنی باید مخفی هم بشود، به‌عنوان تصمیم UX در گزارش پایانی بگو، پیاده نکن مگر مطمئن باشی.
- انتساب نقش `SuperAdmin` (نقش `IsSystemManaged`) به هر کاربری فقط با `identity.access.manage` ممکن است، جدا از `identity.users.manage` (ADR-046). اگر کاربر واردشده این permission را ندارد، فرم انتخاب نقش باید همچنان نقش SuperAdmin را نشان بدهد (برای اینکه بفهمد کاربر چه نقشی دارد) ولی تلاش برای انتخابش باید یا در UI غیرفعال باشد یا بک‌اند رد کند و پیام‌اش نشان داده شود — حدس نزن کدام، اگر ابهام دارد بپرس.
- Grant دادن یک permission با `isSuperAdminOnly=true` (فعلاً فقط `identity.access.manage`) به کاربر/نقش غیر-SuperAdmin رد می‌شود.
- `dotnet run` بدون فرانت‌اند هم قابل تست دستی است (تمام endpointهای بالا از گام ۱ با `curl` تست شده‌اند)؛ رفتار جدید در این مرحله فقط سمت فرانت است، مگر جایی که زیر گفته شده.

## تغییر لازم در بک‌اند (استثنای دقیق قاعدهٔ ۸)

فقط همین دو مورد مجاز است، هیچ‌چیز دیگری:

**۱. `DisplayName` روی Permission (بدون ADR جدا، امتداد گام ۱).**
`Permission` هیچ `DisplayName` ذخیره نمی‌کند (فقط `Key`، `ModuleName`، `IsSuperAdminOnly`)، در حالی که `PermissionDefinition` (در `Shared/Kernel/Permissions/PermissionDefinition.cs`، مصرف‌شده در `IdentityModule.cs`/`CatalogModule.cs`/`AuditingModule.cs`/`SampleModule.cs`) از قبل یک `DisplayName` فارسی برای هر permission دارد (مثلاً «مدیریت کاربران»، «مدیریت دسترسی‌ها»). این همان موردی است که `DECISIONS.md` زیر عنوان «موارد باز» ثبت کرده. بدون این، صفحهٔ ویرایش permissionها فقط کلیدهای انگلیسی خام (`identity.users.manage`) را می‌تواند نشان بدهد.

اضافه کن:
- ستون `DisplayName` (nvarchar، not null) به جدول `Permission` — یک مهاجرت جدید.
- `Permission.Create(key, displayName, moduleName, isSuperAdminOnly)` و `Permission.SyncFrom(displayName, moduleName, isSuperAdminOnly)` را به‌روزرسانی کن تا `DisplayName` را هم بگیرند.
- `IdentityBootstrapper.SynchronizePermissionsAsync` را طوری عوض کن که `definition.DisplayName` را هم پاس بدهد (همان `ModulePermissionDefinition.Definition.DisplayName` که همین حالا در دسترس است).
- `ListPermissionsEndpoint` را طوری عوض کن که `DisplayName` را هم در پاسخ برگرداند.

**۲. `GET /roles/summary` تازه، زیر `identity.users.manage` (ADR-052).**
فرم «تغییر نقش‌های کاربر» فقط `identity.users.manage` دارد ولی برای رندرشدنش به فهرست نقش‌ها (id/name/isSystemManaged) نیاز دارد؛ `GET /roles` موجود پشت `identity.access.manage` است چون `permissionKeys` هر نقش را هم برمی‌گرداند — این را شل نکن (به‌طور کامل در ADR-052 توضیح داده شده، بخوانش). به‌جایش:
- یک Query تازه بساز، `GET /api/v1/identity/roles/summary`، زیر permission `identity.users.manage`، که فقط `{ id, name, isSystemManaged }` را از جدول `Role` برمی‌گرداند — بدون `permissionKeys`، بدون join به `RolePermission`.
- هیچ مهاجرتی لازم ندارد؛ `Role` از قبل همهٔ این فیلدها را دارد.
- `GET /roles` کامل دست‌نخورده می‌ماند و فقط برای صفحهٔ ویرایش permissionهای نقش (پایین‌تر، بخش «مدیریت نقش‌ها») مصرف می‌شود.

هیچ تغییر دیگری در ماژول Identity یا هر ماژول دیگر مجاز نیست بدون توقف و پرسیدن.

## تصمیمی که باید بگیری، نه حدس بزنی: رمز موقت

`CreateUserCommand` رمز اولیه را از **درخواست‌دهنده** می‌گیرد (`{ username, initialPassword }`)، بک‌اند رمز تصادفی تولید نمی‌کند. یعنی فرم «ساخت ادمین» باید یک رمز موقت از جایی تهیه کند. رویکرد پیشنهادی (اگر ابهامی نداری همین را پیاده کن، وگرنه بپرس):

- فرانت‌اند یک رمز تصادفی قوی (حداقل ۱۲ نویسه، ترکیب حروف/عدد) سمت کلاینت با `crypto.getRandomValues` تولید می‌کند و همراه با `username` می‌فرستد — نه اینکه از SuperAdmin بخواهد رمز را دستی تایپ کند.
- بعد از موفقیت `POST /users`، رمز تولیدشده **فقط همان یک بار** روی صفحه (نه در toast زودگذر) با دکمهٔ کپی نشان داده شود، با یک هشدار که این رمز دوباره قابل‌بازیابی نیست و باید همین الان به ادمین جدید داده شود.
- رمز تولیدشده در هیچ state ماندگار (URL، localStorage، React Query cache) نگه‌داشته نشود؛ فقط در state موقت خودِ فرم تا وقتی صفحه ترک شود.
- حداقل طول رمز تولیدی باید با `PasswordPolicy.MinLength` بک‌اند (۸) هماهنگ باشد؛ چون فرانت‌اند تولیدش می‌کند، مقدار پیشنهادی (۱۲) از حداقل بیشتر است، مشکلی نیست.

## صفحهٔ تغییر رمز اجباری (جایگزین بنر گام ۴)

`app/(panel)/layout.tsx` الان وقتی `mustChangePassword=true` فقط یک `Alert` نشان می‌دهد و بقیهٔ صفحه هم زیرش قابل‌استفاده می‌ماند — طبق گام ۴ همین‌طور خواسته شده بود، چون فرم واقعی «کار این مرحله» بود. این مرحله باید:

- یک صفحه/مسیر بسازد (مثلاً `app/(panel)/change-password/page.tsx` یا یک overlay در همان layout) که فرم `POST /auth/change-password` (`currentPassword`, `newPassword`) را دارد.
- وقتی `mustChangePassword=true`، کاربر به این مسیر هدایت شود و **هیچ مسیر دیگری در پنل در دسترس نباشد** (نه سایدبار، نه صفحات دیگر) تا وقتی موفق تغییر رمز بدهد — این با رفتار بک‌اند (`IAllowedWhenPasswordChangeRequired` فقط `ChangePasswordCommand`/`LogoutCommand` را قبول می‌کند) هماهنگ می‌شود.
- بعد از موفقیت، کاربر را به همان صفحه‌ای که می‌خواست برود (یا فهرست محصولات) هدایت کن و `router.refresh()` بزن تا `mustChangePassword` تازه از `/auth/me` خوانده شود.
- خطای «رمز فعلی اشتباه است» و «رمز جدید باید متفاوت باشد» را از `ProblemDetails` بخوان و نشان بده (همان الگوی `presentApiError` که هست).

## صفحهٔ مدیریت ادمین‌ها

مسیر پیشنهادی: `app/(panel)/admins/page.tsx`، محافظت‌شده با `identity.users.manage` (رفتار مشابه چک `catalog.products.read` در محل مربوطه — پایین در بخش «سایدبار و مسیر» توضیح داده شده).

بخش‌های لازم:

1. **فهرست ادمین‌ها** (از `GET /users`): نام کاربری، نقش‌ها، وضعیت فعال/غیرفعال (`Badge`، مثل الگوی `ProductsTable`)، و اینکه آیا `mustChangePassword` هنوز باز است.
2. **فرم ساخت ادمین**: `username` + رمز تولیدشده (بالا توضیح داده شد) + نمایش رمز بعد از موفقیت. بعد از ساخت، لیست را invalidate کن (`queryClient.invalidateQueries`) — این اولین mutation واقعی این فرانت‌اند است؛ الگوی query key factory که `productQueryKeys` دارد را برای `adminQueryKeys` هم رعایت کن.
3. **دکمهٔ فعال/غیرفعال** روی هر ردیف (`POST /users/{id}/toggle-active`)؛ برای کاربر `isProtected` غیرفعال (disabled) باشد با یک tooltip/توضیح کوتاه، نه اینکه کاربر با کلیک به یک خطای ۴۰۳ برسد بدون توضیح.
4. **دکمهٔ ریست رمز**: مثل ساخت ادمین، یک رمز تصادفی تولید کن، `POST /users/{id}/reset-password` بزن، رمز را یک‌بار نشان بده.
5. **ویرایش نقش‌ها**: چک‌باکس یا select چندانتخابی از `GET /roles/summary` (نه `GET /roles` کامل — همان دلیلی که در ADR-052 و بخش «تغییر لازم در بک‌اند» آمده: این فرم فقط `identity.users.manage` دارد و نباید `permissionKeys` نقش‌ها را ببیند)؛ ذخیره با `PUT /users/{id}/roles` (جایگزینی کامل). اگر کاربر واردشده `identity.access.manage` ندارد، گزینهٔ نقش `SuperAdmin` (از روی `isSystemManaged` در همان پاسخ) باید غیرفعال/غیرقابل‌انتخاب باشد (بالا توضیح داده شد).
6. **ویرایش permissionهای مستقیم کاربر** (`GET /users/{id}` برای `directPermissions` فعلی + `GET /permissions` برای فهرست کامل): برای هر permission سه حالت — بدون override، Grant، Deny — و ذخیره با `PUT /users/{id}/permissions/{key}` (مقدار `null` برای «بدون override»). permissionهای `isSuperAdminOnly=true` را برای کاربر غیر-SuperAdmin غیرفعال نشان بده.
7. **صفحهٔ جزئیات یک کاربر** (`GET /users/{id}`) می‌تواند بخش‌های ۵ و ۶ را در خودش جای بدهد؛ نیازی نیست همه‌چیز در همان جدول فهرست باشد — یک ردیف قابل‌کلیک به `app/(panel)/admins/[id]/page.tsx` منطقی‌تر است، ولی اگر implementer طرح ساده‌تری (مثلاً پنل کناری) ترجیح می‌دهد و همان قابلیت‌ها را می‌پوشاند، اشکالی ندارد — تصمیمش را در گزارش پایانی بگو.

**نقش `Admin` را که کسی هنوز ویرایش نکرده باشد** (طبق گام ۱، بدون permission اولیه ساخته می‌شود): این صفحه اولین جایی است که SuperAdmin واقعاً می‌تواند به آن نقش permission بدهد (`PUT /roles/{roleId}/permissions`) — یک بخش «مدیریت نقش‌ها» (فهرست `GET /roles` + ویرایش permissionهای هر نقش غیر-سیستمی) هم بخشی از دامنهٔ این صفحه است، نه یک صفحهٔ جدا.

## سایدبار و مسیر — یک نکتهٔ معماری که باید درستش کنی

`app/(panel)/layout.tsx` الان کل پنل را پشت یک permission ثابت و محصول‌محور قفل کرده:

```ts
const productsReadPermission = "catalog.products.read";
...
if (!user.permissions.includes(productsReadPermission)) {
  redirect("/forbidden");
}
```

این با اضافه‌شدن یک صفحهٔ دوم با permission متفاوت (`identity.users.manage`) دیگر درست نیست — فعلاً چون SuperAdmin همهٔ permissionها را دارد مشکلی دیده نمی‌شود، ولی این چک باید عمومی شود: لایهٔ `(panel)` فقط باید «کاربر واردشده است؟» را چک کند (redirect به `/login` اگر نه)، و هر صفحه (`products`، `admins`) خودش چک permission‌اش را با `Can` یا یک گارد سمت سرور مشابه انجام بدهد. صفحهٔ `/forbidden` هم متن ثابتش («اجازهٔ مشاهدهٔ محصولات را ندارید») را باید عمومی کند یا پارامتری بگیرد.

`nav-items.ts` را با آیتم «مدیریت ادمین‌ها» (`href: "/admins"`, `permission: "identity.users.manage"`) گسترش بده؛ `filterNavItems`/`Sidebar` از قبل داده‌محورند، تغییری در منطقشان لازم نیست.

## ساختار فرانت‌اند (فایل‌های جدید پیشنهادی)

```
src/Web/
├─ app/(panel)/
│  ├─ admins/page.tsx           فهرست + فرم ساخت
│  ├─ admins/[id]/page.tsx      جزئیات، نقش‌ها، permissionهای مستقیم
│  └─ change-password/page.tsx  فرم تغییر رمز اجباری
├─ features/admins/
│  ├─ api/                      list-users.ts, create-user.ts, reset-password.ts,
│  │                            toggle-active.ts, set-user-roles.ts, set-user-permission.ts,
│  │                            list-roles.ts, set-role-permissions.ts, list-permissions.ts
│  ├─ components/                AdminsTable, CreateAdminForm, GeneratedPasswordReveal,
│  │                            RoleEditor, PermissionEditor
│  ├─ hooks/                     use-admins.ts, use-admin.ts, use-roles.ts, use-permissions.ts + معادل mutation
│  └─ lib/                       generate-temp-password.ts
├─ features/auth/
│  └─ components/ChangePasswordForm.tsx (کنار login-form.tsx موجود)
```

الگوی هر فایل را از معادل موجودش در `features/products` کپی کن (fetch با `apiFetch`، تایپ‌ها از `api-types.generated.ts`، query key factory، `useQuery`/`useMutation`).

## تولید تایپ از OpenAPI

بعد از تغییر بک‌اند (`DisplayName` روی `GET /permissions`)، `npm run generate:api-types` را دوباره اجرا کن تا `shared/lib/api-types.generated.ts` تایپ‌های تازهٔ همهٔ endpointهای بالا (که فعلاً هیچ‌کدام مصرف فرانت نداشتند و ممکن است تایپ نام‌دار نداشته باشند) را بگیرد. اگر پاسخ anonymous بود و تایپ تمیز نساخت، طبق ADR-051 آن Query را در بک‌اند به یک `record` نام‌دار تبدیل کن (دقیقاً همان کاری که `GetMeEndpoint` در گام ۴ با `MeResponse` کرد) — این هم بخشی از همان استثنای بک‌اند بالاست، نه یک مورد جداگانه.

## تست‌های لازم

- فرانت: تست‌های موجود (`node --test tests/*.test.ts`) سبز بمانند؛ برای منطق غیر-UI تازه (مثل `generate-temp-password.ts`، اگر قانونی برای طول/ترکیب دارد) تست اضافه کن.
- بک‌اند: تست واحد برای `Permission.SyncFrom`/`Create` با `DisplayName`، و یک تست که `IdentityBootstrapper` بعد از sync، `DisplayName` صحیح را برای permissionهای شناخته‌شده ذخیره می‌کند.
- دستی/گزارش‌شده: سناریوی کامل «ساخت ادمین تازه → خروج → ورود با او → مجبور به تغییر رمز → بعد از تغییر رمز، دسترسی به فهرست محصولات رد می‌شود چون permission ندارد، مگر نقشش را SuperAdmin از پیش تنظیم کرده باشد».

## معیار پذیرش

1. `dotnet build`، `dotnet test`، `npm run build`، `npm run lint` سبز.
2. کاربر بدون `identity.users.manage` نه آیتم «مدیریت ادمین‌ها» را در سایدبار می‌بیند، نه با وارد کردن مستقیم `/admins` به آن می‌رسد (ریدایرکت یا صفحهٔ ممنوع، نه یک صفحهٔ خالی/کرش).
3. ساخت ادمین تازه از UI یک رمز موقت تولید و یک‌بار نمایش می‌دهد؛ آن ادمین با ورود اول، پیش از هر صفحهٔ دیگر پنل، فقط فرم تغییر رمز را می‌بیند؛ بعد از تغییر رمز موفق، وارد پنل می‌شود.
4. دکمهٔ فعال/غیرفعال، ریست رمز و تغییر نقش روی کاربر `isProtected` (سوپرادمین seed‌شده) کار نمی‌کند و پیام روشن نشان می‌دهد.
5. یک Admin عادی (بدون `identity.access.manage`) نمی‌تواند از UI به خودش یا کس دیگری نقش SuperAdmin بدهد؛ اگر تلاش کند (مثلاً با دستکاری UI)، بک‌اند طبق ADR-046 رد می‌کند و UI پیام خطا را نشان می‌دهد، کرش نمی‌کند.
6. ویرایش permissionهای مستقیم یک کاربر (Grant/Deny/حذف) و ویرایش permissionهای نقش `Admin` از UI کار می‌کند و بلافاصله (بدون نیاز به ورود دوباره، طبق `IClaimsTransformation` موجود) روی همان کاربر اثر می‌کند.
7. `GET /permissions` حالا `displayName` فارسی برمی‌گرداند و صفحهٔ مدیریت دسترسی‌ها از همین برای نمایش استفاده می‌کند، نه کلید خام.
8. هر عملیات این صفحه (ساخت، ریست رمز، فعال/غیرفعال، تغییر نقش، تغییر permission) در گزارش فعالیت گام ۲ با شناسهٔ عامل و هدف دیده می‌شود — این تست باید با AuditLog واقعی (نه فرض) بررسی شود، چون هیچ کدام از commandهای Identity در گام ۱ صریحاً `IAuditableCommand` بودنشان اینجا دوباره تأیید نشده بود؛ اگر معلوم شد یکی از آن‌ها این مارکر را ندارد، این هم باید به‌عنوان بخشی از همان استثنای بک‌اند اضافه شود و در گزارش پایانی توضیح داده شود چرا.
9. `git status` بعد از بیلد تمیز است.

## آنچه در این مرحله **نباید** انجام شود

- هیچ صفحهٔ ثبت‌نام عمومی یا فراموشی رمز با ایمیل/پیامک — طبق ADR-023 ریست فقط دستی توسط SuperAdmin است.
- هیچ نقش یا permission تازهٔ محصولی/کاتالوگ — آن‌ها را دست نزن.
- هیچ تغییری در `AuditBehavior`/`AuditingModule` (گام ۲) به‌جز آنچه در معیار پذیرش ۸ گفته شد اگر لازم افتاد.
- هیچ کتابخانهٔ UI/فرم جدید (react-hook-form، shadcn، و مشابه) بدون تأیید.
- هیچ تماس مستقیم فرانت با پرتال یا تغییری در `features/products`.

## موارد باز که باید در گزارش پایانی بیایند، نه حدس زده شوند

- آیا صفحهٔ جزئیات یک ادمین باید مسیر جدا (`admins/[id]`) باشد یا پنل کناری در همان فهرست — تصمیمت را با دلیل بگو.
- رفتار دقیق UI وقتی SuperAdmin تلاش می‌کند نقش SuperAdmin را از کاربر seed‌شده بگیرد (که `isProtected` هم هست، پس دوبار رد می‌شود) — پیام باید کدام دلیل را نشان بدهد؟
- آیا هر کدام از commandهای Identity گام ۱ (`CreateUserCommand`، `ResetPasswordCommand`، …) از قبل `IAuditableCommand` را پیاده کرده‌اند یا این مرحله باید اضافه‌اش کند (معیار پذیرش ۸).
- اگر یکی از Queryهای بالا (`GET /users`، `GET /roles`، …) بعد از `generate:api-types` تایپ anonymous/تمیز نساخت، کدام‌ها را به رکورد نام‌دار تبدیل کردی.

## خروجی

- کد در همین مخزن، روی برنچ `step-05-admin-management`.
- یک یا چند کامیت Conventional Commits که بدنه‌شان دلیل تصمیم‌های پیاده‌سازی (نه فهرست فایل) را بگوید.
- بخش مربوط به مدیریت ادمین‌ها در `README.md` اضافه شود: مسیرهای جدید، و اینکه رمز موقت چگونه تولید و نمایش داده می‌شود.
- اگر جایی از این پرامپت یا از ADRهای مرتبط (۰۰۱، ۰۰۲، ۰۰۳، ۰۲۱، ۰۲۲، ۰۲۳، ۰۴۶، ۰۵۲) با هم یا با کد موجود گام‌های ۱ و ۴ نمی‌خواند، **حدس نزن** — فهرست کن و بپرس.
