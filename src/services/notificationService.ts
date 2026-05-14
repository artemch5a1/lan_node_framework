import { userFacingMessageFromUnknown } from "./notificationFormatting";

export type NotificationSink = {
  /** Показать пользователю сообщение об ошибке (например, тост). */
  showError: (message: string) => void;
};

export type AppNotificationService = {
  showErrorFromUnknown: (error: unknown) => void;
};

export function createNotificationService(sink: NotificationSink): AppNotificationService {
  return {
    showErrorFromUnknown(error: unknown) {
      const msg = userFacingMessageFromUnknown(error).trim();
      if (msg) sink.showError(msg);
    },
  };
}
