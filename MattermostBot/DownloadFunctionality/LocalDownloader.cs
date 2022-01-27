using System;
using System.IO;
using System.Net;
using System.Text;
using SlackNet;

namespace SlackBot.DownloadFunctionality
{
  class LocalDownloader : IDownloader
  {
    /// <summary>
    /// Полное сообщение из треда.
    /// </summary>
    readonly StringBuilder msg = new StringBuilder();
    readonly string pathToDownloadDirectory;
    private string botToken;
    private ISlackApiClient slackApi;

    public LocalDownloader(string botToken, ISlackApiClient slackApi, string pathToDownloadDirectory)
    {
      this.pathToDownloadDirectory = pathToDownloadDirectory;
      this.botToken = botToken;
      this.slackApi = slackApi;
    }

    /// <summary>
    /// Скачивает весь тред в папку.
    /// </summary>
    /// <param name="thread">Тред, который нужно скачать.</param>
    /// <param name="channelName">Название канала, в котором расположен тред.</param>
    /// <param name="botToken">Токен бота.</param>
    /// <returns>Полный путь до папки с тредом.</returns>
    public string DownloadThread(ThreadDTO thread, string channelName)
    {
      var pathToThreadFolder = CreateThreadFolder(thread.Messages[0], channelName);
      using StreamWriter sw = new StreamWriter(@$"{pathToThreadFolder}\thread.txt");
      foreach (var message in thread.Messages)
      {
        var messageTimestamp = SlackBot.ConvertUnixTimeStampToDateTime(Convert.ToDouble(message.Ts.Substring(0, message.Ts.IndexOf('.'))));
        var userName = slackApi.Users.GetUserNameById(message.User).User.RealName;
        //string text = $"{messageTimestamp}\r\n" +
        //  $"{userName}: {message.Text}\r\n";
        msg.Clear();
        msg.AppendFormat(messageTimestamp + "\r\n" + userName + ": "+ message.Text + "\r\n");
        if (message.Files != null)
        {
          DownloadFiles(message, pathToThreadFolder);
        }
        sw.WriteLine(msg);
      }
      return pathToThreadFolder;
    }

    /// <summary>
    /// Создает папку для скачивания файлов.
    /// </summary>
    /// <param name="firstMessageInThread"></param>
    /// <param name="channelName"></param>
    /// <returns>Путь до папки для скачивания треда.</returns>
    private string CreateThreadFolder(MessageDTO firstMessageInThread, string channelName)
    {
      var threadTS = SlackBot.ConvertUnixTimeStampToDateTime
        (Convert.ToDouble(firstMessageInThread.Ts.Substring(0, firstMessageInThread.Ts.IndexOf('.'))))
        .ToString("yyyy/MM/dd HH-mm");
      var userName = slackApi.Users.GetUserNameById(firstMessageInThread.User).User.RealName;
      var pathToThreadFolder = @$"{pathToDownloadDirectory}{channelName}\{threadTS} {userName}";
      if (Directory.Exists(@$"{pathToThreadFolder}\files"))
      {
        ClearDirectory(@$"{pathToThreadFolder}\files");
        return pathToThreadFolder;
      }
      else
      {
        Directory.CreateDirectory(@$"{pathToThreadFolder}\files");
        return pathToThreadFolder;
      }
    }

    /// <summary>
    /// Удаляет все старые файлы из папки, в которую будут помещаться файлы.
    /// </summary>
    /// <param name="pathToThreadFolder"></param>
    public static void ClearDirectory(string pathToThreadFolder)
    {
      string[] files = Directory.GetFiles(pathToThreadFolder);

      foreach (string file in files)
      {
        System.IO.File.Delete(file);
      }
    }

    /// <summary>
    /// Скачивает все файлы из треда.
    /// </summary>
    /// <param name="message">Сообщение, к которму прикреплены файлы.</param>
    /// <param name="botToken">Токен бота.</param>
    /// <param name="pathToThreadFolder"></param>
    private void DownloadFiles(MessageDTO message, string pathToThreadFolder)
    {
      msg.Append("Вложенные файлы: ");
      using (WebClient client = new WebClient())
      {
        client.Proxy = new WebProxy { UseDefaultCredentials = true, };
        client.Headers.Add($"Authorization: Bearer {botToken}");
        foreach (var file in message.Files)
        {
          var fileName = CreateUniqueFileName(pathToThreadFolder, file.Name);
          msg.Append($"{fileName}; ");
          client.DownloadFile(new Uri(file.UrlPrivateDownload), $@"{pathToThreadFolder}\files\{fileName}");
        }
      }
      msg.Append("\r\n");
    }

    /// <summary>
    /// Создает уникальное название для скачиваемого из треда файла.
    /// </summary>
    /// <param name="pathToThreadFolder">Путь до папки, в которую скачивается тред.</param>
    /// <param name="defaultFileName">Имя, которое файл имеет по умолчанию.</param>
    /// <returns>Уникальное название файла.</returns>
    private string CreateUniqueFileName(string pathToThreadFolder, string defaultFileName)
    {
      var fileName = defaultFileName.Split('.')[0];
      var fileExtension = defaultFileName.Split('.')[1];
      while (System.IO.File.Exists($@"{pathToThreadFolder}\files\{fileName}.{fileExtension}"))
      {
        if (int.TryParse(fileName[^2].ToString(), out int i))
          fileName = fileName.Remove(fileName.Length - 2, 2) + $"{i + 1})";
        else
          fileName += "(1)";
      }
      return $"{fileName}.{fileExtension.ToLower()}";
    }
  }
}