import { z } from "zod";

export const loginSchema = z.object({
  username: z.string().trim().min(1, "نام کاربری را وارد کنید."),
  password: z.string().min(1, "رمز عبور را وارد کنید."),
});
