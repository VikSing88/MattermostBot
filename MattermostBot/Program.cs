using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    const int daysBeforeWarningByDefault = 7;

    /// <summary>
    /// Количество дней до отпинивания сообщения по умолчанию.
    /// </summary>
    const int daysBeforeUnpiningByDefault = 3;

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
    const string emojiName = "no_entry_sign";       

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
    /// Uri маттермоста.
    /// </summary>
    private static string MattermostUri;

    /// <summary>
    /// Токен бота.
    /// </summary>
    private static string botToken;

    /// <summary>
    /// Клиент для работы со маттермостом.
    /// </summary>
    private static IApiClient mattermostApi;

    /// <summary>
    /// Callback id shortcut команды.
    /// </summary>
    private static string shortcutCallbackID;

    /// <summary>
    /// Путь куда будет скачиваться тред.
    /// </summary>
    private static string pathToDownloadDirectory;

    /// <summary>
    /// Информация о всех каналах.
    /// </summary>
    private static readonly List<ChannelInfo> ChannelsInfo = new List<ChannelInfo>();

    /// <summary>
    /// Список из task`ов, окончание которых нужно дождаться
    /// </summary>
    private static readonly List<Task> tasks = new List<Task>();

    /// <summary>
    /// ID бота.
    /// </summary>
    private static string botID;

    /// <summary>
    /// Действие, которое надо совершить над запиненным сообщением.
    /// </summary>
    private enum MessageAction
    {
      NeedUnpin,
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
    /// <returns></returns>
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
            daysBeforeWarningByDefault);
          var daysBeforeUnpining = TryConvertStringToInt("daysBeforeUnpining", config.GetSection($"Channels:{i}:DaysBeforeUnpining").Value,
            daysBeforeUnpiningByDefault);
          var autoPinNewMessage = bool.Parse(config.GetSection($"Channels:{i}:AutoPinNewMessage").Value);
          var welcomeMessage = config.GetSection($"Channels:{i}:WelcomeMessage").Value;

          ChannelsInfo.Add(new ChannelInfo(channelID, daysBeforeWarning, daysBeforeUnpining, autoPinNewMessage, welcomeMessage));
          i++;
        }
        shortcutCallbackID = config["ShortcutCallbackID"];
        MattermostUri = config["MattermostUri"];
        botToken = config["BotToken"];
        pathToDownloadDirectory = config["PathToDownloadDirectory"];
        botID = config["BotID"];
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка чтения конфигурационного файла: {ex.Message}");
        throw;
      }
    }

    private static void ConnectToSlack()
    {
      //var webProxy = new WebProxy();
      mattermostApi = new MattermostApiAdapter(MattermostUri, botToken);
      /*webProxy.UseDefaultCredentials = true;
      try
      {
        client = new HttpClient(new HttpClientHandler() { Proxy = webProxy });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при подключении к slack: {ex.Message}");
        throw;
      }*/
    }

    private static async void ConfigureSlackService()
    {
      /*var httpClient = new HttpClient(new HttpClientHandler
      {
        Proxy = new WebProxy { UseDefaultCredentials = true, },
      });
      var jsonSettings = Default.JsonSettings(Default.SlackTypeResolver(Default.AssembliesContainingSlackTypes));

      slackService = new SlackServiceBuilder()
        .UseHttp(p => Default.Http(jsonSettings, () => httpClient))
        .UseJsonSettings(p => jsonSettings)
        .UseApiToken(botToken)
        .UseAppLevelToken(botLevelToken)
        .RegisterMessageShortcutHandler(shortcutCallbackID, ctx =>
        {
          var slackApi = ctx.ServiceProvider.GetApiClient();
          return new DownloadHandler(slackApi, new LocalDownloader(botToken, slackApi, pathToDownloadDirectory));
        })
        .RegisterEventHandler(p =>
        {
          var slackApi = p.ServiceProvider.GetApiClient();
          return new AutoPinMessageHandler(slackApi, SlackChannelsInfo);
        });
      await slackService.GetSocketModeClient().Connect();*/
    }

    public static void Main()
    {
      try
      {
        Console.WriteLine("Работа бота начата.");

        ReadConfig();
        ConfigureSlackService();
        ConnectToSlack();
        mattermostApi.StartWebSocket(m => EventHandler(m));
        while (true)
        {
          foreach(var channelInfo in ChannelsInfo)
          {
            //mattermostApi.PostMessage(channelInfo.ChannelID, "Hello!");
            tasks.Add(Task.Run(() => ProcessPinsList(channelInfo)));
          }
          Task.WaitAll(tasks.ToArray());
          Thread.Sleep(3600000);
        }
      }
      catch
      {
        Console.WriteLine("Работа бота завершилась из-за ошибок.");
      }
    }

    private static void EventHandler(MessageEvent messageEvent)
    {
      //var result = Task.CompletedTask;
      if (messageEvent.rootID == "")
      {
        foreach (var channelInfo in ChannelsInfo.Where(info => info.ChannelID == messageEvent.channelID))
        {
          if (!string.IsNullOrEmpty(channelInfo.WelcomeMessage))
            /*result = result.ContinueWith(
              (t) => mattermostApi.PostEphemeral(channelInfo.WelcomeMessage, postData.user_id, postData.channel_id));*/
            mattermostApi.PostEphemeralMessage(messageEvent.channelID, messageEvent.userID, channelInfo.WelcomeMessage);

          if (channelInfo.AutoPinNewMessage)
            //result = result.ContinueWith(
            //  (t) => mattermostApi.PinMessage(postData.id));
            mattermostApi.PinMessage(messageEvent.id);

          break;
        }
      }
      else
      if (messageEvent.message.Contains("@roberto get_thread"))
      {
        mattermostApi.PostEphemeralMessage(messageEvent.channelID, messageEvent.userID, "YES!");
      }
      else
      if (messageEvent.message.Contains("@roberto"))
      {
        mattermostApi.PostEphemeralMessage(messageEvent.channelID, messageEvent.userID, "Can i help you?");
      }

    }

      /// <summary>
      /// Обработать список запиненных сообщений.
      /// </summary>
      private static void ProcessPinsList(ChannelInfo channelInfo)
    {
      try
      {
        var pinnedMessages = mattermostApi.GetPinnedMessages(channelInfo.ChannelID);
        var oldMessageTSList = GetOldMessageList(pinnedMessages, channelInfo);
        ReplyMessageInOldThreads(oldMessageTSList, channelInfo);
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
    private static void ReplyMessageInOldThreads(List<MessageInfo> messageInfos, ChannelInfo channelInfo)
    {
      foreach (MessageInfo messageInfo in messageInfos)
      {
        if (messageInfo.action == MessageAction.NeedWarning)
        {
          SendMessage(String.Format(WarningTextMessage, channelInfo.DaysBeforeWarning), messageInfo.id, channelInfo);
        }
        else if (messageInfo.action == MessageAction.NeedUnpin)
        {
          SendMessage(UnpiningTextMessage, messageInfo.id, channelInfo);
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
    /// Получить список старых закрепленных сообщений.
    /// </summary>
    /// <param name="pinedMessages">Полный список закрепленных сообщений.</param>
    /// <returns>Список закрепленных сообщений, с момента создания которых прошло больше DaysCountBeforeWarning дней.</returns>
    private static List<MessageInfo> GetOldMessageList(Message[] pinedMessages, ChannelInfo channelInfo)
    {
      var oldPinedMessageList = new List<MessageInfo>();
      if (pinedMessages != null)
      {
        foreach (var pinedMessage in pinedMessages)
        {
          if (IsOldPinedMessage(pinedMessage.dateTime, Math.Min(channelInfo.DaysBeforeWarning, channelInfo.DaysBeforeUnpining)))
          {
            MessageAction msgAction = Task.Run(() => GetPinedMessageAction(pinedMessage.messageId, channelInfo)).Result;
            oldPinedMessageList.Add(new MessageInfo()
            {
              id = pinedMessage.messageId,
              action = msgAction
            });
          }
        }
      }
      return oldPinedMessageList;
    }

    /// <summary>
    /// Определить действие, которое необходимо с закрепленным сообщением.
    /// </summary>
    /// <param name="messageId">ИД запиненного сообщения.</param>
    /// <returns>Действие, которое необходимо с закрепленным сообщением.</returns>
    private static MessageAction GetPinedMessageAction(string messageId, ChannelInfo channelInfo) 
    {
      var messages = mattermostApi.GetThreadMessages(messageId);
      var sorted_messages = messages.OrderBy(m => m.dateTime).ToArray();
      var latest_message_number = sorted_messages.Count() - 1;
      return DefineActionByDateAndAuthorOfMessage(sorted_messages[latest_message_number].dateTime,
        sorted_messages[latest_message_number].userId, channelInfo );
    }

    /// <summary>
    /// Добавить эмодзи на открепляемое сообщение.
    /// </summary>
    /// <param name="messageTimestamp">Отметка времени открепляемого сообщения.</param>
    private static void AddEmoji(string messageID)
    {
      mattermostApi.AddReaction(botID, messageID, emojiName);
    }

    /// <summary>
    /// Определить действие над закрепленным сообщением по дате и автору последнего ответа.
    /// </summary>
    /// <param name="messageDateTime">Отметка времени последнего сообщения из треда.</param>
    /// <param name="userID">ID автора последнего сообщения из треда. </param>
    /// <param name="channelInfo">Информация о канале, в котором нахоидтся тред.</param>
    /// <returns>Действие над закрепленным сообщением.</returns>
    private static MessageAction DefineActionByDateAndAuthorOfMessage(DateTime messageDateTime, string userID, ChannelInfo channelInfo)
    {
      if (messageDateTime != null)      
      {
        if ((userID != botID) & (IsOldPinedMessage(messageDateTime, channelInfo.DaysBeforeWarning)))
        {
          return MessageAction.NeedWarning;
        }
        else
        if ((userID == botID) & (IsOldPinedMessage(messageDateTime, channelInfo.DaysBeforeUnpining)))
        {
          return MessageAction.NeedUnpin;
        }
      }
      return MessageAction.DoNothing;
    }

    /// <summary>
    /// Определить, является ли закрепленное сообщение старым.
    /// </summary>
    /// <param name="messageDateTime">Отметка времени сообщения.</param>
    /// <param name="DayCount">Период (в днях), по прошествию которого следует считать сообщение старым.</param>
    /// <returns>Признак, является ли закрепленное сообщение старым.</returns>
    private static bool IsOldPinedMessage(DateTime messageDateTime, int DayCount)
    {
      //return messageDateTime.AddDays(DayCount) < DateTime.Now;    // ВНИМАНИЕ!!
      return messageDateTime.AddMinutes(20) < DateTime.Now;
    }

    ///// <summary>
    ///// Конвертировать отметку времени в тип DateTime.
    ///// </summary>
    ///// <param name="unixTimeStamp">Отметка времени.</param>
    ///// <returns>Дата и время.</returns>
    //public static DateTime ConvertUnixTimeStampToDateTime(double unixTimeStamp)
    //{
    //  var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
    //  dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
    //  return dateTime;
    //}

    /// <summary>
    /// Отправить сообщение в тред закрепленного сообщения.
    /// </summary>
    /// <param name="textMessage">Текст отправляемого сообщения.</param>
    /// <param name="messageId">ИД закрепленного сообщения.</param>
    /// <param name="channelInfo">Информация о канале, в котором нахоидтся тред.</param>
    private static void SendMessage(string textMessage, string messageId, ChannelInfo channelInfo)
    {
      mattermostApi.PostMessage(channelInfo.ChannelID, textMessage, messageId);
    }
    #endregion
  }

  /*
  static class GetThreadExtension
  {
    /// <summary>
    /// Получить весь тред.
    /// </summary>
    /// <param name="messageTimestamp">Время первого сообщения треда.</param>
    /// <param name="channel">Канал треда.</param>
    /// <returns>Все сообщения треда.</returns>
    public static ThreadDTO GetThread(this IConversationsApi conversations, string messageTimestamp, string channel)
    {
      var messages = conversations.Replies(channel, messageTimestamp, limit: 50).Result.Messages;
      List<MessageDTO> messageDTO = new List<MessageDTO>(messages.Count);
      ThreadDTO thread = new ThreadDTO();
      foreach (var message in messages)
      {
        messageDTO.Add(new MessageDTO
        {
          Text = message.Text,
          Ts = message.Ts,
          User = message.User,
          Files = GetFiles(new List<FileDTO>())
        });

        List<FileDTO> GetFiles(List<FileDTO> list)
        {
          if (message.Files.Count == 0) return null;
          foreach (var file in message.Files)
          {
            list.Add(new FileDTO
            {
              Name = file.Name,
              UrlPrivateDownload = file.UrlPrivateDownload
            });
          }
          return list;
        }
      }
      thread.Messages = messageDTO;
      return thread;
    }
  }
 

  static class GetUserNameByIdExtension
  {
    /// <summary>
    /// Получить ник пользователя по его id.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>Ник пользователя.</returns>
    public static UserDTO GetUserNameById(this IUsersApi users, string userId)
    {
      var user = users.Info(userId).Result;
      DTOs.User DTOsUser = new DTOs.User
      {
        Name = user.Name,
        RealName = user.RealName
      };
      UserDTO userDTO = new UserDTO
      {
        User = DTOsUser
      };
      return userDTO;
    }
  } */
}