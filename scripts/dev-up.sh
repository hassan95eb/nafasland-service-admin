#!/usr/bin/env bash
# اجرای کل استک (mssql, api, web, proxy) با یک دستور برای محیط توسعه.
#
# مهاجرت دیتابیس همچنان یک گام صریح است (ADR-041) — این اسکریپت فقط آن را
# به‌صورت خودکار در توالی درست (بعد از healthy شدن mssql، پیش از بالا آمدن
# api) اجرا می‌کند؛ چیزی داخل اپ یا Dockerfile به‌صورت خودکار میگریت نمی‌کند.
#
# استفاده: ./scripts/dev-up.sh [آرگومان‌های اضافه برای «docker compose up»]

set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

ENV_FILE=".env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "خطا: فایل .env پیدا نشد." >&2
  echo "اول این را بزن: cp .env.example .env" >&2
  echo "و مقدارهای واقعی (رمز sa، توکن پرتال، اعتبارنامهٔ سوپرادمین و ...) را در آن پر کن." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "خطا: dotnet SDK پیدا نشد؛ برای اجرای مهاجرت‌ها از host لازم است." >&2
  exit 1
fi

if ! dotnet tool list --global | grep -qi dotnet-ef; then
  echo "خطا: ابزار dotnet-ef نصب نیست. این را بزن:" >&2
  echo "  dotnet tool install --global dotnet-ef" >&2
  exit 1
fi

# مقدارهای .env مثل docker compose بدون قواعد quote شل خوانده می‌شوند
# (رشتهٔ اتصال می‌تواند فاصله و «;» داشته باشد)، پس فایل source نمی‌شود؛
# فقط همین یک مقدار لازم را استخراج می‌کنیم.
MSSQL_SA_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' "$ENV_FILE" | tail -n1 | cut -d= -f2-)"

if [[ -z "$MSSQL_SA_PASSWORD" ]]; then
  echo "خطا: MSSQL_SA_PASSWORD در .env تعریف نشده است." >&2
  exit 1
fi

echo "==> بالا آوردن mssql..."
docker compose up -d --build mssql

echo "==> صبر برای healthy شدن mssql..."
timeout_seconds=120
elapsed=0
until [[ "$(docker compose ps mssql --format '{{.Health}}' 2>/dev/null)" == "healthy" ]]; do
  if (( elapsed >= timeout_seconds )); then
    echo "خطا: mssql بعد از ${timeout_seconds} ثانیه healthy نشد." >&2
    docker compose logs mssql >&2
    exit 1
  fi
  sleep 3
  elapsed=$(( elapsed + 3 ))
done
echo "mssql آماده است."

connection="Server=localhost,1433;Database=NafasLandAdmin;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;"

# «dotnet ef» برای پیدا کردن DbContext، اپ را می‌سازد و DatabaseOptions را در
# همین مرحله (نه فقط --connection) می‌خواند؛ چون [Required] است باید چیزی در
# محیط باشد، حتی اگر مقدار واقعی migration از --connection می‌آید.
export Database__ConnectionString="$connection"

# ترتیب مهم نیست، سه ماژول اول schemaهای پایه‌اند و Catalog و Approvals بعداً اضافه شده‌اند.
modules=(
  "src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj|NafasLand.Admin.Modules.Sample.Persistence.SampleDbContext"
  "src/Modules/Identity/NafasLand.Admin.Modules.Identity.csproj|NafasLand.Admin.Modules.Identity.Persistence.IdentityDbContext"
  "src/Modules/Auditing/NafasLand.Admin.Modules.Auditing.csproj|NafasLand.Admin.Modules.Auditing.Persistence.AuditingDbContext"
  "src/Modules/Catalog/NafasLand.Admin.Modules.Catalog.csproj|NafasLand.Admin.Modules.Catalog.Persistence.CatalogDbContext"
  "src/Modules/Approvals/NafasLand.Admin.Modules.Approvals.csproj|NafasLand.Admin.Modules.Approvals.Persistence.ApprovalsDbContext"
)

for entry in "${modules[@]}"; do
  IFS="|" read -r project context <<< "$entry"
  echo "==> اجرای مهاجرت: ${context}"
  dotnet ef database update \
    --project "$project" \
    --startup-project src/Api/NafasLand.Admin.Api.csproj \
    --context "$context" \
    --connection "$connection"
done

echo "==> بالا آوردن بقیهٔ استک (api, web, proxy)..."
exec docker compose up --build "$@"
