import { z } from "zod";

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "رمز عبور فعلی را وارد کنید."),
    newPassword: z.string().min(8, "رمز عبور جدید باید حداقل ۸ نویسه باشد."),
  })
  .refine((value) => value.currentPassword !== value.newPassword, {
    path: ["newPassword"],
    message: "رمز عبور جدید باید با رمز فعلی متفاوت باشد.",
  });
