using SlackNet;
using SlackNet.Interaction;
using System.Threading.Tasks;

namespace SlackBot.DownloadFunctionality
{
  class DownloadHandler : IMessageShortcutHandler
  {
    private readonly IDownloader downloader;
    private readonly ISlackApiClient slackApi;
    private readonly string notification = "Загрузка началась";
    public DownloadHandler(ISlackApiClient slackApi, IDownloader downloader)
    {
      this.downloader = downloader;
      this.slackApi = slackApi;
    }

    public Task Handle(MessageShortcut request)
    {
      Task.Run(() =>
      {
        if (request.Message.ThreadTs != null)
        {
          slackApi.Chat.PostEphemeralMessageToUser(notification, request.User.Id, request.Channel.Id);
          var thread = slackApi.Conversations.GetThread(request.Message.ThreadTs, request.Channel.Id);
          var pathToThread = downloader.DownloadThread(thread, request.Channel.Name);
          var message = $"Тред находится по ссылке: {pathToThread}";
          slackApi.Chat.PostEphemeralMessageToUser(message, request.User.Id, request.Channel.Id);
        }
      });
      return Task.CompletedTask;
    }
  }
}