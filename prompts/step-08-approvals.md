# پرامپت گام ۸ — ماژول Approvals (حذف محصول، انتشار، featured/most، حذف واریانت)

> این متن را به‌عنوان پرامپت به AI کدنویس بده. پیش از اجرا، مطمئن شو `AGENTS.md`، `DECISIONS.md`، `ROADMAP.md` و کد گام‌های ۰ تا ۷ (روی `master`) در دسترس آن هستند.

---

تو روی مخزن `nafasland-service-admin` کار می‌کنی. پیش از هر کاری `AGENTS.md` را بخوان. جزئیات هر تصمیم در `DECISIONS.md` با شمارهٔ ADR آمده — این مرحله مستقیماً روی **ADR-010** (ماژول عمومی Approvals)، **ADR-030** (انتشار محصول)، **ADR-031** (تکلیف عضوهای `status`)، **ADR-032** (دامنهٔ کار روی واریانت‌ها) سوار می‌شود، و به ADR-002 (`IsSuperAdminOnly`)، ADR-006 (ترتیب pipeline)، ADR-009/۰۴۸/۰۴۹ (Auditing)، ADR-014 (صفحه‌بندی keyset)، ADR-017 (نگاشت اندپوینت‌های پرتال)، ADR-019 (schema به‌ازای ماژول) و ADR-036 (CorrelationId) وابسته است. همهٔ این‌ها را کامل بخوان، حدس نزن.

**این اولین ماژول جدید بعد از Identity/Auditing/Catalog است و اولین جایی که یک عملیات پیش از اجرا نیاز به تأیید یک کاربر دوم دارد.** تا الان هر command با یک permission مستقیماً اجرا می‌شد. این مرحله یک مسیر دومرحله‌ای اضافه می‌کند: ثبت درخواست → تأیید/رد سوپرادمین → اجرای واقعی روی پرتال. ماژول `Approvals` هیچ دانشی از Catalog ندارد؛ Catalog فقط چند «مجری» (`IApprovalExecutor`) رجیستر می‌کند.

## قواعد همکاری (الزامی)
## گیت

1. **کامیت و پوش نکن.** تغییرات را فقط در فایل‌ها اعمال کن و همان‌جا رها کن. کاری با `git commit`، `git push`، `git rebase`، `git reset` یا تغییر تاریخچه نداشته باش.
2. **متن کامیت را پیشنهاد بده، نه اجرا.** در پایان کار، متن پیشنهادی کامیت را به‌صورت یک بلوک جدا در پاسخت بنویس. فرمت Conventional Commits با بدنه‌ای که **دلیل** تغییر را توضیح می‌دهد.
3. **هیچ اشاره‌ای به هوش مصنوعی در متن کامیت نباشد.**
4. **هر مرحله روی برنچ جداگانه.** پیش از شروع، برنچ جدید از `master` بساز با نام `step-08-approvals`. روی `master` مستقیم کار نکن.

## مرور و ادغام

5. **برنچ را خودت merge نکن.**
6. در پایان، خلاصه‌ای در قالب توضیحات Pull Request بنویس: چه چیزی ساخته شد، چرا این‌طور، کدام معیارهای پذیرش بررسی و تأیید شدند، و چه چیزی باز مانده.
7. **force push و بازنویسی تاریخچه ممنوع است.**

## دامنهٔ تغییرات

8. **فقط کاری را انجام بده که این پرامپت خواسته.** هیچ refactor بی‌ربط با Identity، Auditing یا فرم‌های موجود ایجاد/ویرایش محصول که گام‌های ۵ تا ۷ ساختند.
9. **`DECISIONS.md`، `AGENTS.md` و `ROADMAP.md` را تغییر نده.** اگر جایی اشتباه یا ناقص بود، در پاسخت گزارش کن.
10. **مهاجرت‌های موجود را ویرایش یا حذف نکن.** این مرحله مهاجرت‌های تازه برای schema جدید `approvals` (و در صورت نیاز افزودن یک permission به `catalog`) اضافه می‌کند.
11. **وابستگی جدید بدون تأیید اضافه نکن.** همهٔ زیرساخت لازم (EF Core، Hangfire، FluentValidation، React Query) از قبل نصب است.
12. نسخهٔ .NET، Next.js، پکیج‌منیجر یا ابزارهای پایه را عوض نکن.

## ایمنی

13. **هیچ دستور مخربی اجرا نکن.**
14. **هیچ مقدار حساسی را ننویس و چاپ نکن** — توکن پرتال، رمز، رشتهٔ اتصال.
15. **حذف محصول واقعاً روی پرتال اجرا می‌شود اگر تست تا انتها برود.** تست‌های خودکار فقط روی fake پرتال اجرا می‌شوند (طبق گام ۳)؛ اگر برای تست دستی لازم شد چیزی را روی پرتال واقعی حذف/منتشر کنی، **این کار را نکن** — فقط تا مرز آماده‌بودن کد پیش برو و در گزارش پایانی بنویس چه چیزی دستی تست نشد.
16. **محافظ محصول تستی (ADR-029) روی همهٔ عملیات نوشتنی این مرحله هم اعمال می‌شود** — دقیقاً همان الگویی که `UpdateProductCommandHandler` دارد (`src/Modules/Catalog/Features/UpdateProduct/UpdateProductCommandHandler.cs:28-32`): مقایسهٔ شناسهٔ محصول با `PortalOptions.TestProductId` پیش از هر نوشتن واقعی، چه در لحظهٔ ثبت درخواست و چه (حتماً) دوباره در لحظهٔ اجرا — چون بین ثبت و تأیید ممکن است کانفیگ عوض شده باشد.

## کیفیت

17. **اگر جایی مبهم بود، حدس نزن.** فهرست ابهام‌ها را بنویس و بپرس. این پرامپت چند تصمیم نام‌گذاری صریحاً به تو واگذار کرده (پایین) — همان‌جا مشخص شده کدام‌ها.
18. **کد زائد تولید نکن.**
19. **پیش از تحویل، `dotnet build`، `dotnet test`، `npm run build` و `npm run lint` (در `src/Web`) را اجرا کن** و نتیجه را گزارش بده.
20. **در پایان سه چیز گزارش کن:** خلاصهٔ آنچه ساخته شد، هر جایی که از پرامپت منحرف شدی و چرا، و فهرست سؤال‌ها یا موارد باز.

---

## هدف این مرحله

از پنل بتوان چهار عملیات پرریسک روی محصول را از مسیر «درخواست ادمین ← تأیید سوپرادمین ← اجرای واقعی» انجام داد: حذف محصول، انتشار (تغییر `pending`⇄`approved`)، تغییر `featured`/`most`، و حذف واریانت. هیچ‌کدام مستقیماً توسط Admin اجرا نمی‌شود. ماژول `Approvals` عمومی است — دامنه‌ای از هیچ ماژول دیگری نمی‌داند، فقط یک رجیستری از مجری‌ها را دیسپچ می‌کند.

## آنچه از قبل آماده است (فقط رجوع کن)

- **الگوی خواندن-ادغام-نوشتن روی `PUT` محصول از قبل حل شده.** `UpdateProductCommandHandler` (`src/Modules/Catalog/Features/UpdateProduct/UpdateProductCommandHandler.cs`) دقیقاً همان الگویی است که هر سه مجری «تغییر آرایهٔ محصول» (حذف واریانت، انتشار، featured/most) باید از آن استفاده کنند: `GetProductAsync` → `PortalProductMapper.ToWriteModel(current)` → `preserved with { فقط فیلد موردنظر }` → `UpdateProductAsync`. **کاری با فیلدهای دیگر نکن، فقط همین یک الگو را روی فیلدهای `Status`/`Variants` تکرار کن.**
- `PortalProductWriteModel.Status` آرایهٔ رشته‌ای است (`Status` در write model، `Statuses` در read model — نام فرق دارد، دقت کن؛ `src/Modules/Catalog/Contracts/Models/PortalProductModels.cs:41,111`). برای انتشار: عضو `pending` را بردار، `approved` را اضافه کن (یا برعکس برای لغو انتشار). برای featured/most: عضو `featured`/`most` را toggle کن، بقیهٔ آرایه دست‌نخورده بماند.
- `PortalProductWriteModel.Variants` از همان نوع `PortalProductVariant` تشکیل شده که `PortalProductDetail.Variants` هم دارد (`PortalProductModels.cs:67-86`، `:110`). برای حذف واریانت: از `current.Variants` همان لیست را با فیلتر `variant.Id != command.VariantId` بساز و در write model جایگزین کن. **اگر آخرین واریانت محصول باشد، رد کن** (`BusinessRuleException`، پیام روشن که محصول بی‌قیمت می‌ماند — طبق ADR-032).
- `IPortalProductClient` (`src/Modules/Catalog/Contracts/IPortalProductClient.cs`) فعلاً **متد حذف محصول ندارد**. طبق ADR-017، `DELETE /manage/store/products/:id` در کالکشن مستند است. یک متد تازه اضافه کن: `Task DeleteProductAsync(string externalProductId, CancellationToken cancellationToken)`، با همان الگوی `SendAsync`/تبدیل خطا که بقیهٔ متدهای `PortalProductClient` دارند (`src/Modules/Catalog/Infrastructure/PortalProductClient.cs`).
- `AuditBehavior` (`src/Shared/Infrastructure/Messaging/AuditBehavior.cs:56`) همیشه `OnBehalfOfUserId: null` می‌نویسد — مسیر عمومی `IAuditableCommand` امکان ثبت «به نمایندگی از» را ندارد. `AuditLogPurgeJob` (`src/Modules/Auditing/Jobs/AuditLogPurgeJob.cs`) نمونهٔ نوشتن **مستقیم** از طریق `IAuditLogWriter` (نه دیسپچ command) را نشان می‌دهد — برای رویدادهای اجرای تأیید (`ApprovalExecuted` و بقیهٔ رویدادهای ADR-010 که هم `ActorUserId` هم `OnBehalfOfUserId` لازم دارند) از همین الگو استفاده کن، نه از `IAuditableCommand` عمومی. اگر راه بهتری (مثل افزودن پارامتر اختیاری به `IAuditContext`) می‌بینی، پیاده‌سازی کن و در گزارش پایانی دلیلش را بگو — این یک تصمیم باز است.
- `ConflictException` → `409`، `ResourceNotFoundException` → `404`، `BusinessRuleException` → `422`، `AuthorizationDeniedException` → `403` از قبل در `ProblemDetailsExceptionHandler` map شده‌اند (`src/Shared/Infrastructure/ErrorHandling/ProblemDetailsExceptionHandler.cs`). برای همهٔ حالت‌های این مرحله (تداخل نسخه، درخواست پیدا نشد، تصمیم تکراری روی درخواست ترمینال، دسترسی تأییدکننده عوض‌شده) از همین‌ها استفاده کن؛ اکسپشن جدید لازم نیست.
- `CreateProductCommandHandler` محصول تازه را با `Status: ["pending"]` می‌سازد (`src/Modules/Catalog/Features/CreateProduct/CreateProductCommandHandler.cs:49`) — پس مجری انتشار همیشه با یک محصول `pending` طرف است تا وقتی خودش آن را `approved` نکرده.
- الگوی DbContext هر ماژول (schema `approvals` تازه): مثل `CatalogModule.cs` — `AddDbContext<ApprovalsDbContext>` با `MigrationsHistoryTable("__EFMigrationsHistory", ApprovalsDbContext.SchemaName)`، `AddKeyedScoped<IUnitOfWork>("Approvals", ...)`، `EfCoreMigrationCheck<T>`/`EfCoreDatabaseHealthCheck<T>` (نمونه در `src/Modules/Catalog/CatalogModule.cs:35-50`).
- pipeline (ADR-006) ترتیب ثابت `Logging → Validation → Authorization → Idempotency → Transaction → Audit` دارد (`src/Shared/Infrastructure/Extensions/ServiceCollectionExtensions.cs:46-51`) — چیزی در آن عوض نمی‌کنی.
- الگوی صفحه‌بندی keyset ADR-014 قبلاً برای فهرست AuditLog پیاده شده — همان الگو (نه offset/skip) را برای `GET /api/v1/approvals` هم به کار ببر؛ پیاده‌سازی مرجع را در ماژول Auditing پیدا کن.
- `IModule`، `IRequiresPermission`، `IAuditableCommand`/`IAuditContext`، `IRequiresAuthenticatedUser` در `src/Shared/Kernel/{Modules,Permissions,Auditing}` — همان مارکرها را برای commandهای این مرحله استفاده کن، چیزی در تعریفشان عوض نمی‌شود.
- `<Can permission="...">` (`src/Web/shared/permissions/permission-context.tsx`) و `navItems` (`src/Web/shared/permissions/nav-items.ts`) الگوی موجود فرانت‌اند هستند.

## ماژول تازه: `Approvals` (schema `approvals`)

### موجودیت `ApprovalRequest`

دقیقاً طبق ADR-010:

```
ApprovalRequest(Id, RequestType, TargetEntityType, TargetEntityId, PayloadJson,
SnapshotJson, Reason, Status, RequestedByUserId, RequestedAt, ReviewedByUserId?,
ReviewedAt?, ReviewNote?, ExecutedAt?, ExecutionError?, ExpiresAt?, RowVersion)
```

- `Status`: `Pending / Approved / Rejected / Executed / ExecutionFailed / Cancelled / Expired`.
- `RowVersion` روی `rowversion` بومی SQL Server می‌نشیند (طبق ADR-019) — از همان الگویی استفاده کن که EF Core برای `[Timestamp]`/`IsRowVersion()` به کار می‌برد؛ گذار وضعیت باید با این ستون تداخل هم‌زمان را تشخیص دهد (۴۰۹، نه بازنویسی بی‌صدا).
- `PayloadJson` سریالایز شدهٔ همان command اصلی است (مثلاً `DeleteProductCommand`)، نه یک DTO موقت. **نوع از `RequestType` resolve می‌شود، هرگز از نام کلاس داخل JSON.**

### رجیستری `IApprovalExecutor`

```csharp
public interface IApprovalExecutor
{
    string RequestType { get; }          // مثلاً "catalog.product.delete"
    string RequiredPermission { get; }   // permission تأییدکننده، چک‌شده در لحظهٔ اجرا
    Task<ApprovalPreview> PreviewAsync(string payload, CancellationToken ct);
    Task<Result> ExecuteAsync(string payload, ApprovalContext ctx, CancellationToken ct);
}
```

- در `Shared.Kernel` تعریفش کن (ماژول Approvals نباید از Catalog چیزی import کند، برعکسش درست است: Catalog، `IApprovalExecutor` را پیاده می‌کند و در `CatalogModule.RegisterServices` رجیستر می‌کند).
- `Approvals` این اینترفیس‌ها را از DI با `RequestType` کلید می‌کند و resolve می‌کند — نه `switch (type)` دستی روی یک enum بسته؛ ماژول جدید باید بدون دست‌زدن به `Approvals` خودش را اضافه کند (طبق ADR-005/الگوی رجیستری IModule).
- `ApprovalPreview` را خودت طراحی کن (حداقل: یک شیء JSON-پذیر با وضعیت زندهٔ موجودیت هدف) — `PreviewAsync` باید از پرتال زنده بخواند، نه از `SnapshotJson` ذخیره‌شده (قاعدهٔ ۵ ADR-010).

### چهار مجری Catalog

همه در `src/Modules/Catalog/Approvals/` (پوشهٔ تازه، کنار `Features/`):

1. **`catalog.product.delete`** — `ExecuteAsync` محافظ محصول تستی را چک می‌کند، سپس `IPortalProductClient.DeleteProductAsync` را صدا می‌زند. **ریتری خودکار ممنوع** (قاعدهٔ ۴ ADR-010) — شکست یعنی `ExecutionFailed`، نه تلاش دوباره از داخل خود مجری.
2. **`catalog.product.publish`** — الگوی خواندن-ادغام-نوشتن بالا؛ `pending`⇄`approved` را toggle می‌کند. مسیر معکوس (`approved`→`pending`) هم همین مجری را می‌گیرد؛ کدام جهت در `PayloadJson` مشخص می‌شود.
3. **`catalog.product.status`** — همان الگو برای `featured`/`most` (ADR-031). یک درخواست، یک وضعیت را toggle می‌کند (نه هر دو با هم مگر صریحاً خواسته شود).
4. **`catalog.variant.delete`** — الگوی خواندن-ادغام-نوشتن روی `Variants`؛ محافظ «آخرین واریانت» بالا را رعایت کن.

برای هر چهارتا: `PreviewAsync` باید محصول/واریانت هدف را زنده از پرتال بخواند و حداقل عنوان، قیمت‌ها، موجودی، وضعیت فعلی را برای نمایش به سوپرادمین برگرداند (طبق ADR-030 برای publish؛ همین الگو را برای بقیه هم تکرار کن).

### تصمیم‌های نام‌گذاری واگذارشده به تو (الزامی، نه اختیاری، در گزارش پایانی توضیح بده)

ADR-010/۰۳۰ کلیدهای permission را **بدون پیشوند ماژول** نوشته‌اند (`product.delete`، `product.publish.request`, ...) — دقیقاً همان مغایرتی که گام ۶ برای `catalog.products.write` در برابر `product.price.update` قدیمی پیدا کرد و تصحیح کرد (`prompts/step-06-price-inventory-write.md`, بخش «permission تازه»). قرارداد واقعی کد پیشوند ماژول کوچک‌حروف دارد (`catalog.products.read`, `catalog.products.write`). **همان قرارداد را اینجا هم ادامه بده**، نه فرم خام ADR:

```
catalog.products.delete.request    → Admin
catalog.products.delete            → SuperAdmin, IsSuperAdminOnly
catalog.products.publish.request   → Admin
catalog.products.publish           → SuperAdmin, IsSuperAdminOnly
catalog.products.status.request    → Admin   (برای featured/most)
catalog.products.status            → SuperAdmin, IsSuperAdminOnly
catalog.variants.delete.request    → Admin
catalog.variants.delete            → SuperAdmin, IsSuperAdminOnly
```

و برای خودِ ماژول Approvals (بدون پیشوند دیگری چون خودش یک ماژول است):

```
approvals.review      → SuperAdmin, IsSuperAdminOnly   (ثبت approval/rejection/retry)
approvals.read.all     → SuperAdmin, IsSuperAdminOnly   (کارتابل کامل)
```

`approval.read.own` در ADR «همهٔ نقش‌ها» است — این یعنی permission مجزا نیست (چون در مدل default-closed باید صریحاً به هر نقش Grant شود)؛ به‌جایش برای `GET /api/v1/approvals?mine=true` و `GET /api/v1/approvals/{id}` (وقتی خودِ کاربر درخواست‌دهنده است) از مارکر `IRequiresAuthenticatedUser` استفاده کن، با یک چک دستی داخل handler که یا `approvals.read.all` دارد یا `RequestedByUserId == کاربر جاری` — اگر هیچ‌کدام نبود `AuthorizationDeniedException` (۴۰۳). اگر راه‌حل دیگری منطقی‌تر می‌بینی (مثلاً یک policy جدا)، همان را بزن و در گزارش پایانی توضیح بده.

اگر این نام‌گذاری را عوض کردی یا نظر دیگری داری، در گزارش پایانی بنویس تا در `DECISIONS.md` تصحیح شود؛ خودت آن فایل را عوض نکن (قاعدهٔ ۹).

## Commandها و Endpointها

طبق ADR-010:

```
POST   /api/v1/approvals                      ثبت درخواست (reason اجباری)
GET    /api/v1/approvals?status=&type=&mine=
GET    /api/v1/approvals/{id}                 جزئیات + preview زنده
POST   /api/v1/approvals/{id}/approval        (approvals.review)
POST   /api/v1/approvals/{id}/rejection       (note اجباری)
POST   /api/v1/approvals/{id}/cancellation    فقط درخواست‌دهنده، فقط Pending
POST   /api/v1/approvals/{id}/retry           پس از ExecutionFailed
```

- `POST /approvals`: بدنه شامل `requestType`، `targetEntityId`، `reason`، و payload خاص هر نوع (مثلاً برای `catalog.product.status` این‌که کدام وضعیت toggle می‌شود). command مربوطه (`CreateApprovalRequestCommand` یا مشابه) باید `IRequiresPermission` بگیرد که مقدارش **ثابت نیست** — از `RequiredPermission`ِ مجری منطبق با `requestType` resolve می‌شود (چون خودِ `product.delete.request` در برابر `product.publish.request` و... فرق دارد؛ یک permission واحد برای «ثبت هر نوع درخواست» غلط است). این یعنی `AuthorizationBehavior` باید بتواند این چک را انجام دهد — اگر با شکل فعلی `IRequiresPermission` (یک رشتهٔ ثابت) جور نمی‌شود، راه‌حلت را در گزارش پایانی توضیح بده؛ حدس نزن، اگر مبهم بود بپرس.
- `POST .../approval`: تصمیم یک‌بار و ترمینال (قاعدهٔ ۳ ADR-010) — گذار وضعیت با `RowVersion` داخل تراکنش. بلافاصله (یا هم‌زمان، تصمیم خودت) اجرا را صدا می‌زند؛ در لحظهٔ اجرا `RequiredPermission` مجری را **دوباره** روی تأییدکننده چک کن (قاعدهٔ ۲ ADR-010: اگر دسترسی‌اش عوض شده، رد شود نه اجرا). شکست تماس پرتال → `ExecutionFailed` با `ExecutionError` ذخیره‌شده، نه پرتاب استثنا به بیرون به‌شکلی که گذار وضعیت `Approved` را هم برگرداند — یعنی `Approved` باید commit شود حتی اگر اجرا شکست بخورد؛ دو نگرانی جدا.
- `POST .../rejection`: `note` اجباری، فقط از `Pending`.
- `POST .../cancellation`: فقط `RequestedByUserId == کاربر جاری`، فقط از `Pending`.
- `POST .../retry`: فقط از `ExecutionFailed`؛ اجرای دوباره از همان `PayloadJson`.
- همهٔ اینها `IAuditableCommand` هستند با `AuditAction`ها: `ApprovalRequested`, `ApprovalApproved`, `ApprovalRejected`, `ApprovalExecuted`, `ApprovalExecutionFailed`, `ApprovalCancelled`. **`ApprovalExpired`** از داخل job زمان‌بندی‌شده پایین می‌آید، نه از یک command — طبق الگوی `AuditLogPurgeJob` مستقیم بنویس.
- `CorrelationId` رویداد اجرا باید `ApprovalRequestId` را حمل کند (قاعدهٔ پایانی ADR-010) — با فرمت/محل دقیقش خودت تصمیم بگیر (مثلاً پیشوند در متن CorrelationId یا یک فیلد اضافه در payload لاگ) و در گزارش پایانی بگو کدام را انتخاب کردی.

## Job زمان‌بندی‌شده: انقضای درخواست‌های Pending

الگوی `AuditLogPurgeJob` (`src/Modules/Auditing/Jobs/AuditLogPurgeJob.cs`) را برای این هم به کار ببر: یک Hangfire recurring job که درخواست‌های `Pending` قدیمی‌تر از ۷ روز را `Expired` می‌کند و رویداد `ApprovalExpired` را مستقیم از طریق `IAuditLogWriter` ثبت می‌کند (نه دیسپچ command، چون کاربری پشتش نیست).

## فرانت‌اند

- مسیر تازه: `app/(panel)/approvals/page.tsx` («کارتابل من» — درخواست‌های خودم، `requirePermission` با مارکر «فقط کاربر واردشده»، نه یک permission خاص) و `app/(panel)/approvals/review/page.tsx` (کارتابل سوپرادمین، `requirePermission("approvals.read.all")`).
- `navItems` (`src/Web/shared/permissions/nav-items.ts`) را با دو آیتم تازه گسترش بده. اگر بَج شماره (تعداد Pending برای سوپرادمین، طبق ADR-011) می‌خواهی نشان بدهی، ساختار فعلی `navItems` بَج ندارد — یا یک فیلد اختیاری اضافه کن یا کامپوننت بَج را جدا از آرایه بساز؛ تصمیم خودت، در گزارش پایانی بگو کدام را زدی.
- در `product-details.tsx` (`src/Web/features/products/components/product-details.tsx`): به‌جای دکمهٔ حذف مستقیم (که اصلاً وجود ندارد)، یک دکمهٔ «درخواست حذف» با فرم `reason` اجباری، پشت `<Can permission="catalog.products.delete.request">`. اگر محصول یک `ApprovalRequest` با وضعیت `Pending` روی خودش دارد، یک `Badge` «در انتظار تأیید» نشانش بده (نیاز به یک کوئری سبک که بپرسد آیا `Pending` روی این `targetEntityId` هست — endpoint یا query param تازه‌ای که لازم می‌بینی اضافه کن).
- دکمه‌های مشابه برای «درخواست انتشار/لغو انتشار» و «درخواست featured/most» در همان صفحه، پشت permissionهای `.request` بالا.
- حذف واریانت: در `VariantCard` (همان فایل) یک دکمهٔ «درخواست حذف واریانت» پشت `catalog.variants.delete.request`؛ اگر تنها یک واریانت مانده، دکمه غیرفعال باشد با توضیح چرا (مطابق محافظ بک‌اند).
- فرم‌های تأیید/رد/لغو/retry در صفحهٔ کارتابل سوپرادمین، با نمایش preview زنده (از `GET /approvals/{id}`) پیش از تصمیم.
- الگوی `<Can permission="...">`، `React Query` با `queryClient.invalidateQueries` بعد از هر mutation (بدون optimistic update، طبق ADR-011)، و RTL را از الگوی موجود کپی کن.

## تست‌های لازم

- `dotnet test`:
  - گذار وضعیت `ApprovalRequest` (ماشین وضعیت کامل: از هر `Status` فقط گذارهای مجاز انجام شوند؛ تلاش روی وضعیت ترمینال رد شود).
  - تداخل هم‌زمان روی `RowVersion` (دو تأیید هم‌زمان → یکی `409`).
  - هر چهار مجری روی fake پرتال: حذف محصول، انتشار (هر دو جهت)، featured/most toggle، حذف واریانت (شامل رد حذف آخرین واریانت).
  - محافظ محصول تستی در لحظهٔ اجرا (نه فقط لحظهٔ ثبت درخواست).
  - چک دوبارهٔ permission تأییدکننده در لحظهٔ اجرا (سناریو: دسترسی تأییدکننده بین تأیید و اجرا عوض شده — اگر هم‌زمان انجام می‌شوند این تست شاید بی‌معنا باشد؛ اگر طراحی‌ات این دو را جدا کرد، تست کن).
  - `ExecutionFailed` وقتی fake پرتال خطا می‌دهد؛ `retry` موفق بعدش.
  - انقضای خودکار بعد از ۷ روز (با `TimeProvider` قابل کنترل، مثل الگوی موجود در تست‌های Auditing).
  - `AuditLog` هر رویداد هم `ActorUserId` هم `OnBehalfOfUserId` درست را دارد.
- `npm test`/`node --test`: هر منطق فرانت‌اندی که غیربدیهی است (مثلاً غیرفعال‌کردن دکمهٔ حذف آخرین واریانت).
- دستی/گزارش‌شده: یک سناریوی کامل «ادمین درخواست حذف می‌دهد → سوپرادمین می‌بیند و preview زنده می‌خواند → رد می‌کند با یادداشت → ادمین در کارتابل خودش رد را می‌بیند» روی fake پرتال.

## معیار پذیرش

1. `dotnet build`، `dotnet test`، `npm run build`، `npm run lint` سبز.
2. Admin نمی‌تواند مستقیماً محصول حذف کند، منتشر کند، یا featured/most را عوض کند — فقط درخواست ثبت می‌کند.
3. تأیید سوپرادمین واقعاً عملیات را روی fake پرتال اجرا می‌کند؛ رد یا لغو هیچ تماسی با پرتال نمی‌زند.
4. تصمیم روی یک درخواست ترمینال (مثلاً `Approved` دوباره) رد می‌شود، بازنویسی نمی‌شود.
5. حذف آخرین واریانت یک محصول رد می‌شود.
6. محافظ محصول تستی (ADR-029) در لحظهٔ اجرا هم فعال است، نه فقط ثبت درخواست.
7. هر رویداد Approvals در AuditLog با `ActorUserId`، `OnBehalfOfUserId` و `ApprovalRequestId` (در قالب انتخابی‌ات) دیده می‌شود.
8. درخواست Pending قدیمی‌تر از ۷ روز خودکار `Expired` می‌شود.
9. کاربر بدون `approvals.read.all` فقط درخواست‌های خودش را می‌بیند، نه همه را.
10. `git status` بعد از بیلد تمیز است.

## آنچه در این مرحله **نباید** انجام شود

- هیچ افزودن واریانت تازه — طبق ROADMAP گام ۸ فقط حذف واریانت است؛ افزودن واریانت در دامنهٔ این پرامپت نیست (اگر لازمش دیدی، در گزارش پایانی به‌عنوان یک سؤال باز مطرح کن، پیاده نکن).
- هیچ پیاده‌سازی `available`/`unavailable` یا `sale` — این‌ها طبق ADR-031 مستقیم توسط Admin انجام می‌شوند (نه از مسیر Approvals) و صریحاً در دامنهٔ ROADMAP گام ۸ نیستند.
- هیچ کانال اعلان بیرونی (ایمیل/پیامک) — طبق ADR-011 فعلاً فقط بَج درون‌پنلی.
- هیچ تغییری در ماژول Identity یا فرم‌های موجود ایجاد/ویرایش محصول.

## موارد باز که باید در گزارش پایانی بیایند، نه حدس زده شوند

- نام‌گذاری نهایی permissionهای بالا (پیشوند `catalog.` در برابر فرم خام ADR-010/۰۳۰) — تناقض را اینجا هم صریح تکرار کن تا در `DECISIONS.md` تصحیح شود.
- چطور `OnBehalfOfUserId` را برای رویدادهای Approvals ثبت کردی: نوشتن مستقیم از طریق `IAuditLogWriter` (مثل `AuditLogPurgeJob`) یا گسترش `IAuditContext`/`AuditBehavior`.
- چطور permission متغیر (بسته به `requestType`) را در `POST /approvals` با شکل فعلی `IRequiresPermission` (رشتهٔ ثابت) جور کردی.
- آیا تأیید (`Approved`) و اجرا (`Executed`) را در یک تراکنش/یک درخواست HTTP جدا کردی یا هم‌زمان انجام دادی؛ چرا.
- محل و قالب دقیق `ApprovalRequestId` در `CorrelationId`.
- تصمیم دربارهٔ بَج شمارهٔ سایدبار (فیلد تازه در `navItems` یا کامپوننت جدا).

## خروجی

- کد در همین مخزن، روی برنچ `step-08-approvals`.
- یک یا چند کامیت Conventional Commits که بدنه‌شان دلیل تصمیم‌های پیاده‌سازی را بگوید.
- بخش «تأیید و حذف/انتشار محصول» در `README.md` اضافه شود: permissionهای تازه، جریان درخواست/تأیید از دید کلاینت، و نمونهٔ خطای ۴۰۹/۴۰۳.
- اگر جایی از این پرامپت یا از ADR-010/۰۳۰/۰۳۱/۰۳۲ با هم یا با کد موجود گام‌های ۰ تا ۷ نمی‌خواند، **حدس نزن** — فهرست کن و بپرس.
