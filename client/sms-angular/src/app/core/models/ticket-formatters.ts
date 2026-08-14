export function displayStatus(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function formatMinutes(minutes = 0): string {
  if (!minutes) return '0m';
  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  return [hours ? `${hours}h` : '', remainder ? `${remainder}m` : ''].filter(Boolean).join(' ');
}

