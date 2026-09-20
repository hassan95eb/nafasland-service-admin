import { z } from "zod";

export const createAdminSchema = z.object({
  username: z.string().trim().min(1, "نام کاربری را وارد کنید.").max(100, "نام کاربری نمی‌تواند بیش از ۱۰۰ نویسه باشد."),
});
