# SlackBot
## Описание

Бот для Slack, выполняющий вспомогательные функции в каналах консультирования отдела поддержки и разработки Directum 5.
Возможности:
- приветственное сообщение в треде для новых участников в канале;
- автоматическое закрепление (pin) новых сообщений в канале;
- уведомление об неактивных консультациях и их автоматическое открепление (unpin);
- скачивание треда с вложениями.

## Настройка Workspace

На сайте конфигурации бота (https://api.slack.com/apps/...):
- создать новое приложение по инструкции https://api.slack.com/authentication/basics;

- на вкладке Socket Mode включить переключатель "Enable Socket Mode".

- создать App-level Token на вкладке Basic Information с разрешением connections:write.  

- на вкладке Interactivity & Shortcuts создать свой Shortcut для сообщений и придумать ему Callback Id.

- на вкладке OAuth & Permissions выдать боту следующие разрешения:
pins:read, pins:write, channels:history, groups:history, im:history, mpim:history, reactions:write, chat:write, users:read, files:read.

- на вкладке Event Subscriptions включить переключатель "Enable Events" и в группе Subscribe to bot events добавить события "message.channels" и "message.groups".

- добавить бота в каналы, в которых он будет использоваться.

## Настройка приложения

В файле appsettings.json заполнить следующие настройки:

- BotToken: токен бота, который можно получить на вкладке OAuth & Permissions;

- BotLevelToken: еще один токен бота, который необходим для работы Socket mod'a. Найти/создать можно на вкладке Basic Information в категории App-Level Tokens;

- BotId: id бота, можно найти в slack'e;

- ShortcutCallbackID: Callback Id, который был создан при настройке Workspace ранее;

- PathToDownloadDirectory: Имя сетевой папки, в которую бот будет скачивать треды;

- Channels: список каналов, в которых бот будет отпинивать неактивные сообщения:
  - ChannelID: Id канала, за сообщениями которого следит бот (можно найти в свойствах канала в Slack); ![image](https://user-images.githubusercontent.com/2363923/126785486-06eef727-65b7-4b21-997c-ad5b4ef3c154.png)

  - DaysBeforeWarning: количество дней после последнего сообщения, через которое бот отправит предупреждение об неактивном треде;

  - DaysBeforeUnpining: количество дней после предупреждения, через которое бот открепит неактивный тред.

  - AutoPinMessage: автоматическое закрепление новых сообщений.
 
  - WelcomeMessage: приветственное сообщение.

## Пример

![image](https://user-images.githubusercontent.com/55059498/126780340-452a4b0a-2d10-4069-8569-81c8c6a12fce.png)
