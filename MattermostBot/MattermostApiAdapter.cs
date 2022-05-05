using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ApiClient;
using MattermostApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiAdapter
{
  /// <summary>
  /// Класс адаптера для api-клиента Mattermost. 
  /// </summary>
  public class MattermostApiAdapter : IApiClient
  {
    /// <summary>
    /// Объект для работы с web Api Mattermost. 
    /// </summary>
    private readonly Api api;

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
      public string create_at;
      public string files;
    }

    public UserInfo GetUserInfoByID(string userID)
    {
      User u = User.GetById(api, userID).Result;
      return new UserInfo {userID = u.id, firstName = u.first_name, lastName = u.last_name, userName = u.username};
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
        new { user_id = userId, post_id = messageID, emoji_name = emodjiName, create_at = 0 });
    }

    public void PostEphemeralMessage(string channelID, string userID, string message)
    {
      Post.CreateEphemeral(api, userID, channelID, message);
    }

    async public void StartReceivingServerMessages(Action<MessageEventInfo> newPostEventHandler, Action<string> errorEventHandler, CancellationToken cancellationToken)
    {
      try
      {
      using var webSocket = new ClientWebSocket();      
      webSocket.Options.SetRequestHeader("Authorization", $"Bearer {api.Settings.AccessToken}");
      await webSocket.ConnectAsync(new Uri(GetWebSocketUri()), default);
      await StartNewPostEventHandling(webSocket, newPostEventHandler, errorEventHandler, cancellationToken);
      }
      catch 
      { 
        Console.WriteLine("Ошибка в StartReceivingServerMessages"); 
      };
    }

    /// <summary>
    /// Начать обработку событий поступления новых сообщений.
    /// </summary>
    /// <param name="webSocket">Веб-сокет.</param>
    /// <param name="newPostEventHandler">Обработчик событий поступления новых сообщений.</param>
    /// <param name="errorEventHandler">Обработчик ошибок.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Таска старта обработки событий поступления новых сообщений.</returns>
    private Task StartNewPostEventHandling(ClientWebSocket webSocket, Action<MessageEventInfo> newPostEventHandler, Action<string> errorEventHandler, 
      CancellationToken cancellationToken)
    {
      return Task.Run(
        async () =>
        {
          const int maxBufferSize = 64000;
          const string errorMessagePattern = "При обработке сообщения возникла ошибка: {0}. Текст сообщения: {1}.";

          var buffer = WebSocket.CreateClientBuffer(maxBufferSize, maxBufferSize);
          while (!cancellationToken.IsCancellationRequested)
          { 
            using var memoryStream = new MemoryStream();
            WebSocketReceiveResult responce;
            do
            {
              responce = await webSocket.ReceiveAsync(buffer, cancellationToken);
              memoryStream.Write(buffer.Array, buffer.Offset, responce.Count);
            }
            while (!responce.EndOfMessage);

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (responce.MessageType == WebSocketMessageType.Text)
            {
              using var streamReader = new StreamReader(memoryStream, Encoding.UTF8);
              var messageText = streamReader.ReadToEnd();
              try
              {
                ServerWebSocketMessage message = JsonConvert.DeserializeObject<ServerWebSocketMessage>(messageText);
                if (message.@event == "posted")
                {
                  var postData = JsonConvert.DeserializeObject<PostData>(message.data.post);
                  newPostEventHandler(
                    new MessageEventInfo() { id = postData.id, message = postData.message, channelID = postData.channel_id, 
                      userID = postData.user_id, rootID = postData.root_id});
                }
              }
              catch (Exception ex)
              {
                errorEventHandler(string.Format(errorMessagePattern, ex.Message, messageText));
              }
            }
          }
        }, cancellationToken);
    }

    /// <summary>
    /// Получить Uri для подключения к веб-сокету Маттермост.
    /// </summary>
    /// <returns>Uri для подключения к веб-сокету Маттермост.</returns>
    private string GetWebSocketUri()
    {
      var pattern = "http";
      return Regex.Replace(api.Settings.ServerUri.AbsoluteUri, pattern, "ws") + "api/v4/websocket";
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
      return postList.List.Select(p => new Message { messageId = p.id, dateTime = p.create_at ??
        DateTime.MinValue, userId = p.user_id, message = p.message, fileIDs = p.file_ids }).ToArray();
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

  /// <summary>
  /// Класс построителя api-клиента Mattermost.
  /// </summary>
  public class MattermostApiClientBuilder : IApiClientBuilder
  {
    /// <summary>
    /// Api-клиент.
    /// </summary>
    private IApiClient apiClient;

    /// <summary>
    /// Обработчик события опубликованного сообщения.
    /// </summary>
    private Action<MessageEventInfo> newPostEventHandler;

    /// <summary>
    /// Обработчик ошибок.
    /// </summary>
    private Action<string> errorEventHandler;

    public IApiClientBuilder RegisterNewPostEventHandler(Action<MessageEventInfo> eventHandler)
    {
      newPostEventHandler = eventHandler;
      return this;
    }

    public IApiClientBuilder RegisterErrorEventHandler(Action<string> eventEventHandler)
    {
      errorEventHandler = eventEventHandler;
      return this;
    }

    public IApiClient Connect(CancellationToken cancellationToken)
    {
      if (newPostEventHandler != null)
      {
        apiClient.StartReceivingServerMessages(newPostEventHandler, errorEventHandler, cancellationToken);
      }
      return apiClient;
    }

    public MattermostApiClientBuilder(string uri, string token)
    {
      apiClient = new MattermostApiAdapter(uri, token);
    }
  }
}
