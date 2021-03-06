using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ApiAdapter;
using ApiClient;
using Microsoft.Extensions.Configuration;

namespace MattermostBot
{
  class MattermostBot
  {
    #region Константы

    /// <summary>
    /// Количество дней до предупреждения по умолчанию.
    /// </summary>
    const int DaysBeforeWarningByDefault = 7;

    /// <summary>
    /// Количество дней до отпинивания сообщения по умолчанию.
    /// </summary>
    const int DaysBeforeUnpiningByDefault = 3;

    ///<summary>
    /// Период проверки канала в минутах по умолчанию.
    ///</summary>
    const int ChannelCheckPeriodInMinutesbyDefault = 60;

    /// <summary>
    /// Текст предупреждения.
    /// </summary>
    const string WarningTextMessage = "Новых сообщений не было уже больше {0} дней. Закрываем консультацию?";

    /// <summary>
    /// Текст при отпинивании сообщения.
    /// </summary>
    const string UnpiningTextMessage = "Консультация закрыта.";

    /// <summary>
    /// Название эмодзи. 
    /// </summary>
    const string EmojiName = "no_entry_sign";    
    
    /// <summary>
    /// Команда скачки треда.
    /// </summary>
    const string downloadCommand = "download";

    #endregion

    #region Вложенные типы   

    /// <summary>
    /// Информация о запиненном сообщении.
    /// </summary>
    private class MessageInfo
    {
      public string id;
      public MessageAction action;
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Uri сервера Mattermost.
    /// </summary>
    private static string mattermostUri;

    /// <summary>
    /// Токен личного доступа бота.
    /// </summary>
    private static string accessToken;

    /// <summary>
    /// Клиент для работы со маттермостом.
    /// </summary>
    private static IApiClient mattermostApi;

    /// <summary>
    /// Информация о всех каналах.
    /// </summary>
    private static readonly List<ChannelInfo> channelsInfo = new List<ChannelInfo>();

    /// <summary>
    /// Список из task`ов, окончание которых нужно дождаться.
    /// </summary>
    private static readonly List<Task> tasks = new List<Task>();

    /// <summary>
    /// ID пользователя бота.
    /// </summary>
    private static string botUserID;

    /// <summary>
    /// Период проверки канала в минутах.
    /// </summary>
    private static int channelCheckPeriodInMinutes;

    /// <summary>
    /// Путь к папке с сохраненными тредами.
    /// </summary>
    private static string pathToDownloadDirectory;

    /// <summary>
    /// Действие, которое надо совершить над запиненным сообщением.
    /// </summary>
    private enum MessageAction
    {
      NeedUnpin,
      NeedUnpinWithMessage,
      NeedWarning,
      DoNothing
    }

    #endregion

    #region Методы

    /// <summary>
    /// Попытаться конвертировать строку в число.
    /// </summary>
    /// <param name="paramName">Имя параметра.</param>
    /// <param name="value">Конвертируемое значение.</param>
    /// <param name="defaultValue">Значение по умолчанию.</param>
    /// <returns>Переданное значение в int, если конвертировать не удалось, возвращается значение int по умолчанию.</returns>
    private static int TryConvertStringToInt(string paramName, string value, int defaultValue)
    {
      int resultValue;
      try
      {
        resultValue = Convert.ToInt32(value);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Произошла ошибка при чтении параметра {paramName}: {ex.Message}. " +
          $"Взято значение по умолчанию {defaultValue}.");
        resultValue = defaultValue;
      }

      return resultValue;
    }

    /// <summary>
    /// Прочитать конфиг.
    /// </summary>
    private static void ReadConfig()
    {
      try
      {
        var config = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .Build();

        int i = 0;
        while (config.GetSection($"Channels:{i}:ChannelID").Exists())
        {
          var channelID = config.GetSection($"Channels:{i}:ChannelID").Value;
          var daysBeforeWarning = TryConvertStringToInt("daysBeforeWarning", config.GetSection($"Channels:{i}:DaysBeforeWarning").Value,
            DaysBeforeWarningByDefault);
          var daysBeforeUnpining = TryConvertStringToInt("daysBeforeUnpining", config.GetSection($"Channels:{i}:DaysBeforeUnpining").Value,
            DaysBeforeUnpiningByDefault);
          var autoPinNewMessage = bool.Parse(config.GetSection($"Channels:{i}:AutoPinNewMessage").Value);
          var welcomeThreadMessage = config.GetSection($"Channels:{i}:WelcomeThreadMessage").Value;
          var reactionRequiest = config.GetSection($"Channels:{i}:ReactionRequest").Value;

          channelsInfo.Add(new ChannelInfo(channelID, daysBeforeWarning, daysBeforeUnpining, autoPinNewMessage, reactionRequiest, welcomeThreadMessage));
          i++;
        }
        mattermostUri = config["MattermostUri"];
        accessToken = config["AccessToken"];
        botUserID = config["BotUserID"];
        channelCheckPeriodInMinutes = TryConvertStringToInt("ChannelCheckPeriodInMinutes", config["ChannelCheckPeriodInMinutes"],
            ChannelCheckPeriodInMinutesbyDefault);
        pathToDownloadDirectory = config["PathToDownloadDirectory"];
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка чтения конфигурационного файла: {ex.Message}");
        throw;
      }
    }

    public static void Main()
    {
      Console.WriteLine("Работа бота начата.");
      using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
      try
      {
        ReadConfig();
        mattermostApi = new MattermostApiClientBuilder(mattermostUri, accessToken)
          .RegisterNewPostEventHandler(m => NewPostEventHandler(m))
          .RegisterErrorEventHandler(e => Console.WriteLine(e))
          .Connect(cancellationTokenSource.Token);
        while (true)
        {
          foreach (var channelInfo in channelsInfo)
          {
            tasks.Add(Task.Run(() => ProcessPinsList(channelInfo)));
          }
          Task.WaitAll(tasks.ToArray());
          Thread.Sleep(TimeSpan.FromMinutes(channelCheckPeriodInMinutes));
        }
      }
      catch
      {
        cancellationTokenSource.Cancel();
        Console.WriteLine("Работа бота завершилась из-за ошибок.");
      }
    }

    /// <summary>
    /// Обработчик события появления на канале нового сообщения.
    /// </summary>
    /// <param name="messageEventInfo">Информация о событии.</param>
    private static void NewPostEventHandler(MessageEventInfo messageEventInfo)
    {
      if (messageEventInfo.rootID == "")
      {
        foreach (var channelInfo in channelsInfo.Where(info => info.ChannelID == messageEventInfo.channelID))
        {
          if (channelInfo.AutoPinNewMessage)
          {
            mattermostApi.PinMessage(messageEventInfo.id);
            if (!string.IsNullOrEmpty(channelInfo.WelcomeThreadMessage))
            {
              SendMessageToThread(channelInfo.WelcomeThreadMessage, messageEventInfo.id, channelInfo);
            }
          }
          break;
        }
      }
      else 
      {
        var botName = mattermostApi.GetUserInfoByID(botUserID).userName;
        if (Regex.IsMatch(messageEventInfo.message, string.Format("(?:@{0})", botName), RegexOptions.IgnoreCase))
        {
          var rgx = new Regex(@"[-.?!)(,:\s]+");
          var strs = rgx.Split(messageEventInfo.message.ToLower()).Where(s => !string.IsNullOrEmpty(s) && 
            !s.Equals(string.Format("@{0}", botName))).ToList();
          if (string.Join(' ', strs) == downloadCommand)
          {
            mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, messageEventInfo.userID, "Скачивание треда началось.");
            DownloadThread(messageEventInfo);
          }
          else
            mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, messageEventInfo.userID, string.Format(
              "Я не знаю такой команды.\nДоступные команды: {0}", downloadCommand));
        }
      }
    }

    /// <summary>
    /// Скачать тред.
    /// </summary>
    /// <param name="messageEventInfo">Информация о событии.</param>
    private static void DownloadThread(MessageEventInfo messageEventInfo)
    {
      Message[] messages = mattermostApi.GetThreadMessages(messageEventInfo.rootID);
      var pathToThread = GetPathToSaveThread(pathToDownloadDirectory, messages[0].userId, messages[0].dateTime.ToLocalTime());
      try 
      {
        Directory.CreateDirectory(pathToThread);
        var pathToFilesFolder = Path.Combine(pathToThread, "files");
        if (messages.Where(m => m.fileIDs != null).Count() > 0)
          Directory.CreateDirectory(pathToFilesFolder);
        var pathToThreadFile = Path.Combine(pathToThread, "thread.txt");
        StreamWriter thread = File.CreateText(pathToThreadFile);
        foreach (Message message in messages)
        {
          thread.WriteLine(message.dateTime.ToLocalTime() + "\n" + GetUserName(message.userId) + ": " + message.message + "\n");
          if (message.fileIDs != null)
          {
            message.fileIDs
              .ToList()
              .ForEach(fileId =>
              {
                var fileName = mattermostApi.GetFileById(message.messageId, fileId, pathToFilesFolder).Result;
                thread.WriteLine("Добавлен файл - {0}\n", fileName);
              });
          }
        }
        thread.Close();
        mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, messageEventInfo.userID, "Тред скачан и находится здесь: " + pathToThread);
      }

      catch (DirectoryNotFoundException dirEx)
      {
        mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, messageEventInfo.userID, "Произошла ошибка при скачивании треда. Обратитесь к администратору.");
        Console.WriteLine("Путь не найден: " + dirEx.Message);
      }
    }

    /// <summary>
    /// Получить имя, фамилию пользователя. Если их нет, получить никнейм.
    /// </summary>
    /// <param name="userID">ИД пользователя.</param>
    /// <returns>Строка с полным именем пользователя. Если нет имени или фамилии, возвращаетя никнейм.</returns>
    private static string GetUserName(string userID)
    {
      var userInfo = mattermostApi.GetUserInfoByID(userID);
      if (!string.IsNullOrEmpty(userInfo.firstName) && !string.IsNullOrEmpty(userInfo.lastName))
      {
        return string.Format("{0} {1}", userInfo.firstName, userInfo.lastName);
      }
      else
      {
        return userInfo.userName;
      }
    }

    /// <summary>
    /// Создать путь для скачивания треда.
    /// </summary>
    /// <param name="pathToDownloadDirectory">Путь до папки со всеми скачанными тредами.</param>
    /// <param name="rootDateTime">Дата создания корневого сообщения треда. </param>
    /// <param name="rootUserID">ИД создателя корневого сообщения треда.</param>
    private static string GetPathToSaveThread(string pathToDownloadDirectory, string rootUserID, DateTime rootDateTime)
    {
      var rootDateTimeString = rootDateTime.ToString("yyyy.MM.dd HH-mm");
      var path = Path.GetFullPath(Path.Combine(pathToDownloadDirectory, string.Format("{0} {1}", rootDateTimeString, GetUserName(rootUserID))));
      return path;
    }

    /// <summary>
    /// Обработать список запиненных сообщений.
    /// </summary>
    private static void ProcessPinsList(ChannelInfo channelInfo)
    {
      try
      {
        var pinnedMessages = mattermostApi.GetPinnedMessages(channelInfo.ChannelID);
        var messageActionsList = GetMessageActionsList(pinnedMessages, channelInfo);
        ProcessPinnedMessage(messageActionsList, channelInfo);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Обработка сообщений завершилась с ошибкой: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Отправить сообщение в тред.
    /// </summary>
    /// <param name="messageInfos">Список запиненных сообщений.</param>
    private static void ProcessPinnedMessage(List<MessageInfo> messageInfos, ChannelInfo channelInfo)
    {
      foreach (MessageInfo messageInfo in messageInfos)
      {
        if (messageInfo.action == MessageAction.NeedUnpin)
        {
          UnpinMessage(messageInfo.id);
        }
        else if (messageInfo.action == MessageAction.NeedWarning)
        {
          SendMessageToThread(String.Format(WarningTextMessage, channelInfo.DaysBeforeWarning), messageInfo.id, channelInfo);
        }
        else if (messageInfo.action == MessageAction.NeedUnpinWithMessage)
        {
          SendMessageToThread(UnpiningTextMessage, messageInfo.id, channelInfo);
          AddEmoji(messageInfo.id);
          UnpinMessage(messageInfo.id);
        }
      }
    }

    /// <summary>
    /// Открепить сообщение.
    /// </summary>
    /// <param name="messageID">ИД закрепленного сообщения.</param>
    private static void UnpinMessage(string messageID)
    {
      mattermostApi.UnpinMessage(messageID);
    }

    /// <summary>
    /// Получить список действий для закрепленных сообщений.
    /// </summary>
    /// <param name="pinedMessages">Полный список закрепленных сообщений.</param>
    /// <returns>Список закрепленных сообщений, с момента создания которых прошло больше DaysCountBeforeWarning дней.</returns>
    private static List<MessageInfo> GetMessageActionsList(Message[] pinedMessages, ChannelInfo channelInfo)
    {
      var pinedMessageActionsList = new List<MessageInfo>();
      if (pinedMessages != null)
      {
        foreach (var pinedMessage in pinedMessages)
        {
          MessageAction msgAction = Task.Run(() => GetPinedMessageAction(pinedMessage, channelInfo)).Result;
          pinedMessageActionsList.Add(new MessageInfo()
          {
            id = pinedMessage.messageId,
            action = msgAction
          });
        }
      }
      return pinedMessageActionsList;
    }

    /// <summary>
    /// Определить действие, которое необходимо с закрепленным сообщением.
    /// </summary>
    /// <param name="message">Закрепленное сообщение.</param>
    /// <param name="channelInfo">Информация о канале.</param>
    /// <returns>Действие, которое необходимо с закрепленным сообщением.</returns>
    private static MessageAction GetPinedMessageAction(Message message, ChannelInfo channelInfo)
    {
      var messages = mattermostApi.GetThreadMessages(message.messageId);
      var latest_message = messages.OrderBy(m => m.dateTime).Last();

      if (latest_message.dateTime != null)
      {
        if (message.reactions != null && message.reactions.Any(e => e == channelInfo.ReactionRequest))
        {
          return MessageAction.NeedUnpin;
        }
        else
        if ((latest_message.userId != botUserID) & (IsOldPinedMessage(latest_message.dateTime, channelInfo.DaysBeforeWarning)))
        {
          return MessageAction.NeedWarning;
        }
        else
        if ((latest_message.userId == botUserID) & (IsOldPinedMessage(latest_message.dateTime, channelInfo.DaysBeforeUnpining)))
        {
          return MessageAction.NeedUnpinWithMessage;
        }
      }
      return MessageAction.DoNothing;
    }

    /// <summary>
    /// Добавить эмодзи на открепляемое сообщение.
    /// </summary>
    /// <param name="messageID">ИД сообщения.</param>
    private static void AddEmoji(string messageID)
    {
      mattermostApi.AddReaction(botUserID, messageID, EmojiName);
    }

    /// <summary>
    /// Определить, является ли закрепленное сообщение старым.
    /// </summary>
    /// <param name="messageDateTime">Отметка времени сообщения.</param>
    /// <param name="DayCount">Период (в днях), по прошествию которого следует считать сообщение старым.</param>
    /// <returns>Признак, является ли закрепленное сообщение старым.</returns>
    private static bool IsOldPinedMessage(DateTime messageDateTime, int DayCount)
    {
      return messageDateTime.AddDays(DayCount) < DateTime.UtcNow;
    }

    /// <summary>
    /// Отправить сообщение в тред закрепленного сообщения.
    /// </summary>
    /// <param name="textMessage">Текст отправляемого сообщения.</param>
    /// <param name="messageId">ИД закрепленного сообщения.</param>
    /// <param name="channelInfo">Информация о канале, в котором нахоидтся тред.</param>
    private static void SendMessageToThread(string textMessage, string messageId, ChannelInfo channelInfo)
    {
      mattermostApi.PostMessage(channelInfo.ChannelID, textMessage, messageId);
    }
    #endregion
  }

}