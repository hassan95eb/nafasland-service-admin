export function createIdempotencyKey(randomUuid: () => string = () => crypto.randomUUID()) {
  return randomUuid();
}
