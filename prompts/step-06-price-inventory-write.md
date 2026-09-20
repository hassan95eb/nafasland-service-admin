# پرامپت گام ۶ — اولین نوشتن: قیمت و موجودی

> این متن را به‌عنوان پرامپت به AI کدنویس بده. پیش از اجرا، مطمئن شو `AGENTS.md`، `DECISIONS.md`، `ROADMAP.md` و کد گام‌های ۰ تا ۵ (روی `master`) در دسترس آن هستند.

---

تو روی مخزن `nafasland-service-admin` کار می‌کنی. پیش از هر کاری `AGENTS.md` را بخوان. جزئیات هر تصمیم در `DECISIONS.md` با شمارهٔ ADR آمده — این مرحله مستقیماً روی ADR-016، ۰۱۷، ۰۲۹، ۰۳۴، ۰۳۷ و **ADR-053** (تازه، مخصوص همین گام) سوار می‌شود؛ همه را کامل بخوان، حدس نزن.

**این اولین نوشتن واقعی این پروژه روی فروشگاه واقعی است.** ماژول `Catalog` تا الان فقط خواندنی بود (`IPortalProductClient` فقط `ListProductsAsync`/`GetProductAsync` دارد، هیچ DbContext یا schema محلی ندارد). این مرحله برای اولین بار: (۱) یک permission نوشتنی به Catalog اضافه می‌کند، (۲) یک DbContext/schema محلی برای Catalog می‌سازد (فقط برای idempotency، نه برای کش کردن دادهٔ محصول)، (۳) دو مکانیزم ایمنی عمومی به pipeline اضافه می‌کند که گام‌های بعدی (ایجاد/ویرایش کامل محصول، حذف) هم از همین‌ها استفاده خواهند کرد.

## قواعد همکاری (الزامی)
## گیت

1. **کامیت و پوش نکن.** تغییرات را فقط در فایل‌ها اعمال کن و همان‌جا رها کن. کاری با `git commit`، `git push`، `git rebase`، `git reset` یا تغییر تاریخچه نداشته باش.
2. **متن کامیت را پیشنهاد بده، نه اجرا.** در پایان کار، متن پیشنهادی کامیت را به‌صورت یک بلوک جدا در پاسخت بنویس. فرمت Conventional Commits با بدنه‌ای که **دلیل** تغییر را توضیح می‌دهد.
3. **هیچ اشاره‌ای به هوش مصنوعی در متن کامیت نباشد.**
4. **هر مرحله روی برنچ جداگانه.** پیش از شروع، برنچ جدید از `master` بساز با نام `step-06-price-inventory-write`. روی `master` مستقیم کار نکن.

## مرور و ادغام

5. **برنچ را خودت merge نکن.**
6. در پایان، خلاصه‌ای در قالب توضیحات Pull Request بنویس: چه چیزی ساخته شد، چرا این‌طور، کدام معیارهای پذیرش بررسی و تأیید شدند، و چه چیزی باز مانده.
7. **force push و بازنویسی تاریخچه ممنوع است.**

## دامنهٔ تغییرات

8. **فقط کاری را انجام بده که این پرامپت خواسته.** هیچ refactor بی‌ربط با ماژول Identity یا فرانت‌اند `admins`/`login` که گام ۵ ساخته.
9. **`DECISIONS.md`، `AGENTS.md` و `ROADMAP.md` را تغییر نده.** اگر جایی اشتباه یا ناقص بود، در پاسخت گزارش کن.
10. **مهاجرت‌های موجود را ویرایش یا حذف نکن.** این مرحله یک مهاجرت تازه برای schema جدید `catalog` اضافه می‌کند (پایین).
11. **وابستگی جدید بدون تأیید اضافه نکن.** همهٔ زیرساخت لازم (EF Core، Polly/Resilience، React Query) از قبل نصب است.
12. نسخهٔ .NET، Next.js، پکیج‌منیجر یا ابزارهای پایه را عوض نکن.

## ایمنی

13. **هیچ دستور مخربی اجرا نکن.**
14. **هیچ مقدار حساسی را ننویس و چاپ نکن** — توکن پرتال، رمز، رشتهٔ اتصال.
15. **این مرحله دقیقاً روی محصول تستی ADR-029 عمل می‌کند، هیچ محصول واقعی دیگری را دست نزن.** محافظش پایین دقیق توضیح داده شده — پیاده‌سازیش الزامی است، نه اختیاری.

## کیفیت

16. **اگر جایی مبهم بود، حدس نزن.** فهرست ابهام‌ها را بنویس و بپرس.
17. **کد زائد تولید نکن.**
18. **پیش از تحویل، `dotnet build`، `dotnet test` و `npm run build`/`npm run lint` (در `src/Web`) را اجرا کن** و نتیجه را گزارش بده.
19. **در پایان سه چیز گزارش کن:** خلاصهٔ آنچه ساخته شد، هر جایی که از پرامپت منحرف شدی و چرا، و فهرست سؤال‌ها یا موارد باز.

---

## هدف این مرحله

از پنل بتوان قیمت و موجودی یک واریانت را روی محصول تستی تغییر داد، با سه محافظ: دو بار ارسال یک فرم دو بار در فروشگاه اثر نگذارد (idempotency، ADR-034)، تغییر هم‌زمان دو ادمین بی‌صدا گم نشود (تشخیص تداخل، ADR-016 + **ADR-053**)، و وقتی پرتال در دسترس نیست پنل کرش نکند بلکه وارد «حالت کاهش‌یافته» شود (ADR-037).

## آنچه از قبل آماده است (فقط رجوع کن)

- `IPortalProductClient` فعلاً فقط `ListProductsAsync`/`GetProductAsync` دارد (`src/Modules/Catalog/Contracts/IPortalProductClient.cs`). `PortalProductDetail.Variants` هر واریانت را با `Id, ProductId, Sku, Price, ComparePrice, Stock, Shipping, Tax` برمی‌گرداند (`PortalProductModels.cs`) — دقیقاً همان چیزی که لازم داری، `ProductId` هم رویش هست.
- endpointهای مدیریتی پرتال طبق ADR-017: `GET /manage/store/products/variants/:id` (یک واریانت) و `PATCH /manage/store/products/variants/:id` (فقط فیلدهای فرستاده‌شده؛ نام فیلدها `price`, `compare_price`, `stock`, `shipping`, `tax`, `sku`, `increase`).
- `PortalProductClient` (پیاده‌سازی) الگوی `SendAsync` را دارد: توکن Bearer، هدر `X-Correlation-Id`، تبدیل خطاهای شبکه/circuit-breaker/timeout به `PortalUnavailableException`. متدهای تازه‌ات را با همین الگو بساز، در همان کلاس.
- `ProblemDetailsExceptionHandler` (`src/Shared/Infrastructure/ErrorHandling/`) از قبل `ConflictException` → `409`، `PortalUnavailableException` → `503`، `PortalBusyException` → `429` را map کرده. برای تداخل نسخهٔ واریانت از همین `ConflictException` موجود استفاده کن؛ اکسپشن جدید لازم نیست.
- pipeline (ADR-006) با ترتیب ثابت `Logging → Validation → Authorization → Transaction → Audit → Handler` در `ServiceCollectionExtensions.cs` ثبت شده.
- الگوی `IAuditableCommand`/`IAuditContext` از گام ۵ روی ماژول Identity پیاده شده (نمونه: `CreateUserCommand`/`CreateUserCommandHandler`) — همان الگو را برای command این مرحله هم پیاده کن؛ چیزی در `AuditBehavior` عوض نمی‌شود.
- الگوی DbContext هر ماژول: `services.AddDbContext<XDbContext>(...)` با `MigrationsHistoryTable("__EFMigrationsHistory", XDbContext.SchemaName)`، به‌اضافهٔ `services.AddKeyedScoped<IUnitOfWork>("Catalog", sp => sp.GetRequiredService<CatalogDbContext>())` (نمونه در `IdentityModule.cs`؛ کلید باید دقیقاً `"Catalog"` باشد چون `ModuleNameResolver` این را از namespace `NafasLand.Admin.Modules.Catalog.*` استخراج می‌کند). `EfCoreMigrationCheck<T>` و `EfCoreDatabaseHealthCheck<T>` هم برای CatalogDbContext ثبت کن، مثل Identity.
- `app/(panel)/layout.tsx`/`requirePermission` (گام ۵) عمومی است؛ صفحهٔ جدید همین الگو را دنبال می‌کند، چیزی در آن‌ها عوض نمی‌شود.

## permission تازه: `catalog.products.write`

`ADR-021` (سند برنامه‌ریزی قدیمی‌تر، پیش از پیاده‌سازی) از کلید `product.price.update` (بدون پیشوند ماژول) اسم برده بود. ولی قراردادی که واقعاً در کد پیاده شده (`identity.users.manage`، `catalog.products.read`) پیشوند ماژول کوچک‌حروف دارد. **از `catalog.products.write` استفاده کن** (هم‌راستا با `catalog.products.read` موجود)، نه فرم قدیمی ADR-021. این مغایرت را در گزارش پایانی‌ات بنویس تا در `DECISIONS.md` تصحیح شود؛ خودت آن فایل را عوض نکن (قاعدهٔ ۹).

- در `CatalogModule.Permissions` اضافه کن: `new PermissionDefinition(CatalogPermissions.ProductsWrite, "ویرایش قیمت و موجودی محصولات")`.
- `IsSuperAdminOnly` **نه** — طبق حافظهٔ پروژه، Admin باید بتواند قیمت/موجودی را ویرایش کند (فقط حذف محصول مخصوص SuperAdmin است، که گام‌های بعدی‌اند).

## زیرساخت تازه: `CatalogDbContext` (schema `catalog`)

اولین DbContext ماژول Catalog. فقط یک جدول لازم دارد:

```
IdempotencyRecord
  Key (Guid, PK)          — از هدر Idempotency-Key فرانت
  UserId (Guid)
  RequestHash (string)    — هش بدنهٔ درخواست؛ اگر همان Key با بدنهٔ متفاوت دوباره آمد، رد کن (400)، این یعنی سوءاستفاده از کلید
  Status (enum: InProgress, Completed)
  ResponseJson (string?)  — فقط وقتی Completed
  CreatedAt (DateTimeOffset)
```

- محدودیت یکتایی روی `Key` در دیتابیس (نه فقط چک برنامه‌ای) — طبق ADR-034، تا شرایط رقابتی درج همزمان رخ ندهد.
- یک Hangfire job پس‌زمینه که رکوردهای قدیمی‌تر از ۲۴ ساعت را پاک می‌کند (الگوی مشابه job پاک‌سازی ۶ ماههٔ AuditLog در گام ۲، پیدا کن و همان الگو را برای این job هم به کار ببر).

## Idempotency (ADR-034) — پیاده‌سازی به‌عنوان یک لایهٔ pipeline عمومی

این مکانیزم **عمومی** است (طبق ADR-034 روی «همهٔ commandهای ایجاد» تعمیم پیدا می‌کند)، ولی این مرحله فقط یک مصرف‌کننده دارد (command ویرایش واریانت). زیرساخت را عمومی بساز تا گام‌های بعدی (ایجاد محصول) بدون تغییر ازش استفاده کنند.

- یک مارکر تازه در `Shared.Kernel`، مثلاً `IIdempotentCommand` (کنار `IAuditableCommand`/`IRequiresPermission`): بدون عضو اضافه، فقط علامت.
- یک `IdempotencyBehavior<TCommand, TResponse>` تازه در `Shared.Infrastructure.Messaging`، **بین `AuthorizationBehavior` و `TransactionBehavior`** در `ServiceCollectionExtensions.cs` ثبت شود (این جایگاه را خودت انتخاب کن یا اگر دلیل بهتری برای جای دیگر داری در گزارش پایانی توضیح بده؛ فقط ترتیب پنج حلقهٔ موجود طبق ADR-006 عوض نشود).
- کلید از هدر HTTP `Idempotency-Key` خوانده می‌شود (GUID)؛ نبودش برای یک `IIdempotentCommand` باید `400` بدهد.
- منطق: رکورد `Completed` با همان Key → همان `ResponseJson` قبلی برگردانده شود بدون صدا زدن `next()` (بدون تماس با پرتال). رکورد `InProgress` → `409` با پیام «این درخواست در حال انجام است». رکورد پیدا نشد → با محدودیت یکتایی یک رکورد `InProgress` درج کن (نه «اول بخوان بعد بنویس»)؛ اگر درج به‌خاطر تداخل یکتایی شکست خورد، یعنی یک تلاش موازی همین لحظه شروع کرده — همان مسیر `409` را بگیر.
- بعد از `next()` موفق: رکورد را `Completed` کن، پاسخ را serialize و ذخیره کن. اگر `next()` استثنا پرتاب کرد: رکورد `InProgress` را **حذف کن** (نه علامت‌گذاری «شکست‌خورده» — ADR-034 فقط دو وضعیت دارد)، تا همان Key بشود دوباره امتحان کرد.
- نوشتن/به‌روزرسانی این رکورد از طریق `CatalogDbContext` مستقیم در خودِ behavior انجام می‌شود، با `SaveChangesAsync` خودش — **بیرون از تراکنش اصلی command** (همان الگویی که ADR-048/049 برای AuditLog استفاده کرد: یک نگرانی جدا، یک نوشتن جدا). این یعنی یک محدودیت مشابه ADR-049 هم اینجا هست (نوشتن رکورد idempotency ممکن است commit شود ولی خودِ عملیات portal شکست بخورد یا برعکس)؛ در گزارش پایانی توضیح بده که همین محدودیت را پذیرفتی، دلیلش را تکرار نکن مگر فرقی دیدی.

## تشخیص تداخل برای واریانت (ADR-016 + ADR-053)

واریانت `version` ندارد (ADR-016). طبق **ADR-053** (تازه اضافه‌شده به `DECISIONS.md`، حتماً بخوانش): بک‌اند بلافاصله پیش از `PATCH`، همان واریانت را با `GET /manage/store/products/variants/:id` دوباره می‌خواند و `price`/`stock` فعلی را با `LastKnownPrice`/`LastKnownStock` که فرانت فرستاده مقایسه می‌کند. اختلاف در هر کدام → `ConflictException` (۴۰۹)، پیام باید بگوید کدام فیلد عوض شده (مثلاً «قیمت از زمان بازکردن فرم توسط کاربر دیگری تغییر کرده: X → Y»). **همین خواندن، محافظ محصول تستی (پایین) را هم انجام می‌دهد** — چون `PortalProductVariant.ProductId` همراهش برمی‌گردد، دو بار GET نزن.

## محافظ محصول تستی (ADR-029) — الزامی، نه اختیاری

- `PortalOptions.TestProductId` از قبل در کانفیگ هست (`src/Modules/Catalog/Contracts/Configuration/PortalOptions.cs`).
- Command handler، بعد از خواندن واریانت تازه (بخش بالا) و پیش از `PATCH`، اگر `variant.ProductId != options.TestProductId` بود، رد کن — نه با `ConflictException`، با یک پیام روشن که این محافظ توسعه است (کد پیشنهادی: `403`، یا `CommandValidationException` — خودت تصمیم بگیر و در گزارش پایانی دلیلش را بگو).
- این محافظ باید فعال بماند تا وقتی صراحتاً کسی خاموشش نکند؛ ADR-029 آن را به «محیط توسعه» محدود کرده — بررسی کن آیا در این پروژه اصلاً محیطی غیر از Development مستقر شده (طبق ADR-018 هنوز نه)، و اگر نه، محافظ را بدون شرط محیط فعال کن و در گزارش پایانی بنویس که این تصمیم را گرفتی و چرا؛ اگر یک `IHostEnvironment` چک ساده منطقی‌تر می‌دانی، همان را بزن.

## Endpoint و Command تازه

`PATCH /api/v1/catalog/products/variants/{variantId}` → `UpdateVariantPriceAndInventoryCommand(VariantId, NewPrice, NewStock, LastKnownPrice, LastKnownStock)`:
- `IRequiresPermission` → `CatalogPermissions.ProductsWrite`.
- `IIdempotentCommand`.
- `IAuditableCommand` → `AuditAction = "VariantPriceInventoryUpdated"`, `AuditEntityType = "ProductVariant"`؛ handler باید `IAuditContext.SetEntityId(variantId)`، `SetBefore(new { price = فعلی, stock = فعلی })` (از همان GET تازه)، `SetAfter(new { price = جدید, stock = جدید })` را پر کند.
- ترتیب داخل handler: GET تازهٔ واریانت → چک محصول تستی → چک تداخل (`price`/`stock`) → پر کردن AuditContext با مقادیر قبل → `PATCH` به پرتال با `IPortalProductClient.UpdateVariantAsync(variantId, new PortalVariantPatch(NewPrice, NewStock), ct)` → پر کردن AuditContext با مقادیر بعد.
- `IPortalProductClient` را با این دو متد تازه گسترش بده: `GetVariantAsync(string externalVariantId, CancellationToken)` و `UpdateVariantAsync(string externalVariantId, PortalVariantPatch patch, CancellationToken)` (`PortalVariantPatch` یک record با `decimal? Price`, `int? Stock` — نال یعنی آن فیلد فرستاده نمی‌شود، طبق معنای `PATCH` در ADR-017).
- اعتبارسنجی (`FluentValidation`): `NewPrice >= 0`، `NewStock >= 0`.

## حالت کاهش‌یافته (ADR-037)

فعلاً `ProductListCache` (`src/Modules/Catalog/Infrastructure/ProductListCache.cs`) فقط یک کش ۶۰ثانیه‌ای برای تازگی است — اگر `client.ListProductsAsync` استثنا بدهد، استثنا مستقیم بالا می‌رود، هیچ fallback به دادهٔ کهنه نیست. این همان چیزی است که ADR-037 برای «خواندن» می‌خواهد و تا الان ساخته نشده؛ این مرحله باید کامل‌اش کند، هم برای فهرست هم برای جزئیات محصول (که این مرحله برای اولین بار در فرانت مصرف می‌شود):

- کش (list و detail) را طوری عوض کن که وقتی `factory()` با `PortalUnavailableException` شکست می‌خورد، اگر یک مقدار قبلاً کش‌شده (حتی منقضی‌شده) برای همان کلید موجود است، همان را با یک پرچم `IsStale = true` و `AsOfUtc` (زمان کش‌شدنش) برگرداند؛ اگر هیچ مقدار قبلی‌ای نیست، استثنا همچنان بالا برود (چیزی برای نشان‌دادن نیست).
- این یعنی `PortalProductListResult`/`PortalProductDetail` (یا یک پوشش دورشان، اگر تغییر مستقیم رکوردهای موجود دردسر بیشتری دارد) باید `IsStale`/`AsOfUtc` را حمل کنند تا فرانت بتواند نوار هشدار «ارتباط با فروشگاه برقرار نیست؛ داده‌های نمایش‌داده‌شده مربوط به … است» را نشان بدهد.
- **نوشتن:** دکمهٔ ذخیرهٔ قیمت/موجودی باید وقتی می‌داند پرتال در دسترس نیست غیرفعال باشد، نه اینکه کاربر کلیک کند و بعد `503` بگیرد. راه پیشنهادی: همان `IsStale` که از GET جزئیات محصول می‌آید را در فرانت برای غیرفعال‌کردن فرم استفاده کن — اگر جزئیات محصول کهنه است، یعنی همین الان پرتال در دسترس نیست، پس فرم ویرایش را غیرفعال کن با همان پیام ADR-037.
- **هیچ صفی برای نوشتن معلق نگه نمی‌داری** — دقیقاً طبق ADR-037، اگر نوشتن رد شد، رد شد؛ کاربر خودش دوباره تلاش می‌کند.
- health check را دست نزن مگر لازم شد؛ اگر برای این مرحله افزودن وضعیت circuit-breaker به `/health` را لازم می‌بینی، در گزارش پایانی بگو چرا، پیاده نکن مگر مطمئن باشی چون خارج از دامنهٔ صریح این پرامپت است.

## فرانت‌اند

مسیر تازه: `app/(panel)/products/[id]/page.tsx` (تا الان هیچ صفحهٔ جزئیات محصولی نبود؛ گام ۴ فقط فهرست را ساخت). `requirePermission("catalog.products.read")` برای دیدن صفحه؛ فرم ویرایش قیمت/موجودی فقط وقتی نمایش داده می‌شود که کاربر `catalog.products.write` هم داشته باشد (`<Can permission="catalog.products.write">` از `shared/permissions/permission-context.tsx`).

- `features/products/api/get-product.ts`، `features/products/hooks/use-product.ts` (الگوی `use-products.ts` را کپی کن).
- `features/products/api/update-variant.ts`: یک بار در باز شدن فرم (نه در هر submit) یک `Idempotency-Key` تازه (`crypto.randomUUID()`) بساز و در state فرم نگه دار؛ در `apiFetch` با `headers: { "Idempotency-Key": key }` بفرست. بستن/بازکردن دوبارهٔ فرم، کلید تازه می‌سازد.
- فرم باید `LastKnownPrice`/`LastKnownStock` را از همان دادهٔ لود‌شده (نه دوباره از کاربر) بفرستد — کاربر این‌ها را نمی‌بیند، فقط مقدار جدید را وارد می‌کند.
- خطای `409` (تداخل) باید پیام دقیق بک‌اند را نشان دهد و به کاربر بگوید صفحه را رفرش کند (`queryClient.invalidateQueries` + پیام)، نه فقط یک toast عمومی.
- بنر «ارتباط با فروشگاه برقرار نیست» طبق `IsStale`/`AsOfUtc` بالا.

## تست‌های لازم

- `dotnet test`: idempotency (کلید تکراری با پاسخ `Completed` → بدون تماس دوم با fake پرتال؛ کلید `InProgress` هم‌زمان → `409`؛ محدودیت یکتایی دیتابیس واقعاً چک شود، نه فقط منطق برنامه). تداخل قیمت/موجودی → `409` با تشخیص کدام فیلد. محافظ محصول تستی → رد روی هر شناسهٔ دیگر. `AuditContext` پر می‌شود (مشابه `IdentityAuditMetadataTests` که گام ۵ ساخت، برایش الگو بگیر).
- `npm test`/`node --test`: تولید `Idempotency-Key` (اگر منطقی غیر از `crypto.randomUUID()` خام دارد).
- دستی/گزارش‌شده: یک سناریوی کامل «باز کردن فرم → تغییر قیمت → ارسال دوبار پیاپی (دابل‌کلیک) → فقط یک تغییر واقعی» روی fake پرتال.

## معیار پذیرش

1. `dotnet build`، `dotnet test`، `npm run build`، `npm run lint` سبز.
2. ارسال دوبارهٔ همان فرم (همان `Idempotency-Key`) یک نتیجه می‌دهد؛ پرتال (fake) فقط یک‌بار صدا زده می‌شود.
3. اگر قیمت یا موجودی واریانت بین لود فرم و ارسال توسط یک منبع دیگر (تست، fake را دستکاری کن) عوض شده باشد، ذخیره `409` می‌دهد و پرتال اصلاً صدا زده نمی‌شود؛ بازنویسی رخ نمی‌دهد.
4. تلاش نوشتن روی هر `productId`ای غیر از `TestProductId` رد می‌شود.
5. وقتی circuit breaker باز است (شبیه‌سازی در تست fake)، صفحهٔ جزئیات محصول دادهٔ کش‌شده را با برچسب «داده‌های کهنه» نشان می‌دهد و فرم ویرایش غیرفعال است؛ هیچ کرش یا صفحهٔ خطای خام دیده نمی‌شود.
6. تغییر موفق قیمت/موجودی در گزارش فعالیت (AuditLog) با شناسهٔ عامل، شناسهٔ واریانت، مقدار قبل و بعد دیده می‌شود.
7. کاربر بدون `catalog.products.write` فرم ویرایش را در صفحهٔ جزئیات محصول نمی‌بیند (فقط حالت خواندنی).
8. `git status` بعد از بیلد تمیز است.

## آنچه در این مرحله **نباید** انجام شود

- هیچ ویرایش کامل محصول (`PUT`) — آن گام ۷ است، منتظر تست رفت‌وبرگشت `PUT` می‌ماند (طبق ROADMAP).
- هیچ ایجاد یا حذف محصول/واریانت.
- فیلد `increase` (تغییر نسبی موجودی) — طبق ADR-017 برای ماژول انبار در آینده رزرو شده؛ این مرحله فقط مقدار مطلق `stock` می‌فرستد.
- هیچ کش‌کردن دائمی دادهٔ محصول در دیتابیس محلی؛ `CatalogDbContext` فقط `IdempotencyRecord` دارد.
- هیچ افزودن وضعیت circuit-breaker به `/health` مگر توضیح داده شود چرا لازم بود.
- هیچ تغییری در ماژول Identity یا فرانت‌اند `admins`.

## موارد باز که باید در گزارش پایانی بیایند، نه حدس زده شوند

- کد HTTP دقیق محافظ محصول تستی (۴۰۳ یا خطای اعتبارسنجی ۴۰۰) — تصمیمت و دلیلش.
- آیا محافظ محصول تستی را مشروط به محیط (`IHostEnvironment.IsDevelopment()`) کردی یا بدون‌قید‌وشرط؛ چرا.
- جای دقیق `IdempotencyBehavior` در pipeline اگر غیر از پیشنهاد این پرامپت (بین Authorization و Transaction) گذاشتیش، و چرا.
- آیا شکل فعلی `PortalProductListResult`/`PortalProductDetail` را برای `IsStale`/`AsOfUtc` مستقیم عوض کردی یا یک پوشش (wrapper) دورشان ساختی؛ کدام و چرا.
- نام‌گذاری `catalog.products.write` در برابر `product.price.update` قدیمی در ADR-021 — تناقض را اینجا هم صریح تکرار کن تا در `DECISIONS.md` تصحیح شود.

## خروجی

- کد در همین مخزن، روی برنچ `step-06-price-inventory-write`.
- یک یا چند کامیت Conventional Commits که بدنه‌شان دلیل تصمیم‌های پیاده‌سازی را بگوید.
- بخش «ویرایش قیمت و موجودی» در `README.md` اضافه شود: permission تازه، رفتار idempotency از دید کلاینت (هدر لازم)، و نمونهٔ `409` تداخل.
- اگر جایی از این پرامپت یا از ADR-016/۰۱۷/۰۲۹/۰۳۴/۰۳۷/۰۵۳ با هم یا با کد موجود گام‌های ۱ تا ۵ نمی‌خواند، **حدس نزن** — فهرست کن و بپرس.
