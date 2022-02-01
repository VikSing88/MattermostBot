using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ApiClient;
using MattermostApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiAdapter
{
  /// <summary>
  /// Класс адаптера для Mattermost. 
  /// </summary>
  public class MattermostApiAdapter : IApiClient
  {
    private readonly Api api;

    private Action<MessageEventInfo> EventHandler;

    /// <summary>
    /// Тело запроса добавления эмодзи.
    /// </summary>
    private class AddReactionRequestBody
    {
      public string user_id;
      public string post_id;
      public string emoji_name;
      public Int64 create_at;
    }

    /// <summary>
    /// Сообщение с веб-сокета сервера.
    /// </summary>
    private class ServerWebSocketMessage
    {
      public string @event;
      public MessageData data;
    }

    /// <summary>
    /// Данные сообщения от веб-сокета.
    /// </summary>
    private class MessageData
    {
      public string post;
    }

    /// <summary>
    /// Класс события о сообщении. 
    /// </summary>
    private class PostData
    {
      public string id;
      public string message;
      public string channel_id;
      public string user_id;
      public string root_id;
    }

    public Message[] GetPinnedMessages(string channelID)
    {
      return GetMessagesByRequest(Api.Combine("channels", channelID, "pinned"));
    }

    public void PostMessage(string channelID, string message, string rootId = null)
    {
      Post.Create(api, channelID, message, rootId);
    }

    public Message[] GetThreadMessages(string postID)
    {
      return GetMessagesByRequest(Api.Combine("posts", postID, "thread"));
    }

    public void PinMessage(string messageID)
    {
      var post = Post.GetById(api, messageID).Result;
      post.Pin(api);
    }

    public void UnpinMessage(string messageID)
    {
      var post = Post.GetById(api, messageID).Result;
      post.Unpin(api);
    }

    public void AddReaction(string userId, string messageID, string emodjiName)
    {
      api.PostAsync("reactions", null, 
        new AddReactionRequestBody() { user_id = userId, post_id = messageID, emoji_name = emodjiName, create_at = 0 });
    }

    public void PostEphemeralMessage(string channelID, string userID, string message)
    {
      Post.CreateEphemeral(api, userID, channelID, message);
    }

    public IApiClient RegisterEventHadler(Action<MessageEventInfo> eventHandler)
    {
      EventHandler = eventHandler;
      return this;
    }

    public IApiClient Connect()
    { 
      if (EventHandler != null)
      {
        StartWebSocket(EventHandler);
      }
      return this;
    }

    /// <summary>
    /// Стартовать веб-сокет для получения сообщений о событиях Маттермост.
    /// </summary>
    /// <param name="eventHandler">Обработчик события.</param>
    private async void StartWebSocket(Action<MessageEventInfo> eventHandler)
    {
      using var websocket = new ClientWebSocket();
      websocket.Options.SetRequestHeader("Authorization", $"Bearer {api.Settings.AccessToken}");
      using var cancellationTokenSource = new CancellationTokenSource();      
      await websocket.ConnectAsync(new Uri(GetWebSocketUri()), default);    
      var buffer = WebSocket.CreateClientBuffer(1000, 1000);
      while (!cancellationTokenSource.IsCancellationRequested)
      {
        var responce = await websocket.ReceiveAsync(buffer, cancellationTokenSource.Token);
        var eventMessage = Encoding.UTF8.GetString(buffer.Slice(0, responce.Count));
        ServerWebSocketMessage message = JsonConvert.DeserializeObject<ServerWebSocketMessage>(eventMessage);
        if (message.@event == "posted")
        {
          var postData = JsonConvert.DeserializeObject<PostData>(message.data.post);
          eventHandler(
            new MessageEventInfo() { id = postData.id, message = postData.message, channelID = postData.channel_id, userID = postData.user_id, rootID = postData.root_id});
        }
      }
    }

    /// <summary>
    /// Получить Uri для подключения к веб-сокету Маттермост.
    /// </summary>
    /// <returns>Uri для подключения к веб-сокету Маттермост</returns>
    private string GetWebSocketUri()
    {
      Regex rgx = new Regex(@"https?");
      return rgx.Replace(api.Settings.ServerUri.AbsoluteUri, @"ws") + "api/v4/websocket";
    }

    /// <summary>
    /// Нормализовать дату и время сообщения.
    /// </summary>
    /// <param name="dateTime">Дата и время сообщения.</param>
    /// <returns>Нормализованные дата и время соощения.</returns>
    private DateTime NormilizeDateTime(DateTime? dateTime)
    {
      DateTime result = dateTime?.AddHours(4) ?? DateTime.MinValue;
      return result;
    }

    /// <summary>
    /// Получить список сообщений Mattermost по строке запроса.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <returns>Список сообщений.</returns>
    private Message[] GetMessagesByRequest(string request)
    {
      JObject j = api.GetAsync(request).Result;
      PostList postList = j.ConvertToObject<PostList>();
      postList.List = postList.Convert(j).List;
      return postList.List.Select(p =>
        new Message { messageId = p.id, dateTime = NormilizeDateTime(p.create_at), userId = p.user_id }).ToArray();
    }

    public MattermostApiAdapter(string uri, string token)
    {
      var settings = new Settings()
      {
        ServerUri = new Uri(uri),
        AccessToken = token,
        ApplicationName = "RobertoBot",
        RedirectUri = new Uri("http://localhost/"),
        TokenExpires = DateTime.MaxValue,
      };
      this.api = new Api(settings);
    }
  }
}
