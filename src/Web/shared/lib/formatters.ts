const persianNumber = new Intl.NumberFormat("fa-IR");
const persianDate = new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  timeZone: "Asia/Tehran",
});
const persianDateTime = new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: "Asia/Tehran",
});

export function formatPrice(price: number | string | null | undefined) {
  return price == null ? "قیمت تعریف نشده" : `${persianNumber.format(Number(price))} تومان`;
}

export function formatPersianNumber(value: number | string) {
  return persianNumber.format(Number(value));
}

export function formatPersianDate(utcDate: string | null | undefined) {
  if (!utcDate) {
    return "تاریخ ثبت نشده";
  }

  const date = new Date(utcDate);
  return Number.isNaN(date.getTime()) ? "تاریخ نامعتبر" : persianDate.format(date);
}

export function formatPersianDateTime(utcDate: string | null | undefined) {
  if (!utcDate) {
    return "زمان نامشخص";
  }

  const date = new Date(utcDate);
  return Number.isNaN(date.getTime()) ? "زمان نامعتبر" : persianDateTime.format(date);
}
