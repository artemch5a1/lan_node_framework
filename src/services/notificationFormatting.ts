type NetApiErrorBody = {
  error?: { code?: string; message?: string };
};

/** Текст для пользователя из тела ответа API; без акцента на HTTP-код. */
export async function parseNetErrorResponse(response: Response): Promise<string> {
  const raw = await response.text();
  if (raw) {
    try {
      const json = JSON.parse(raw) as NetApiErrorBody;
      const m = json.error?.message?.trim();
      if (m) return m;
    } catch {
      /* не JSON */
    }
  }
  return httpStatusFallbackMessage(response.status);
}

export function userFacingMessageFromUnknown(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

export function httpStatusFallbackMessage(status: number): string {
  switch (status) {
    case 400:
      return "Запрос отклонён: проверьте поля конфигурации.";
    case 403:
      return "Доступ запрещён.";
    case 404:
      return "Запрошенный ресурс не найден.";
    case 409:
      return "Операция конфликтует с текущим состоянием сети.";
    case 502:
    case 503:
      return "Сервис временно недоступен. Попробуйте позже.";
    case 500:
    default:
      return "Не удалось выполнить операцию. Обновите страницу или попробуйте снова.";
  }
}
