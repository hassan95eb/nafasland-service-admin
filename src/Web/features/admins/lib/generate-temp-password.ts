export const temporaryPasswordLength = 16;

const lower = "abcdefghijkmnopqrstuvwxyz";
const upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
const digits = "23456789";
const alphabet = `${lower}${upper}${digits}`;

function randomIndex(max: number) {
  const limit = 256 - (256 % max);
  const bytes = new Uint8Array(1);
  do {
    crypto.getRandomValues(bytes);
  } while (bytes[0] >= limit);
  return bytes[0] % max;
}

function pick(characters: string) {
  return characters[randomIndex(characters.length)];
}

export function generateTemporaryPassword() {
  const characters = [pick(lower), pick(upper), pick(digits)];
  while (characters.length < temporaryPasswordLength) {
    characters.push(pick(alphabet));
  }

  for (let index = characters.length - 1; index > 0; index--) {
    const swapIndex = randomIndex(index + 1);
    [characters[index], characters[swapIndex]] = [characters[swapIndex], characters[index]];
  }

  return characters.join("");
}
