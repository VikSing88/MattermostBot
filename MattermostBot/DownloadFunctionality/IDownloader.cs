namespace SlackBot.DownloadFunctionality
{
  interface IDownloader
  {
    /// <summary>
    /// Скачивает тред.
    /// </summary>
    /// <param name="thread">Тред, который нужно скачать.</param>
    /// <param name="channelName">Название канала, из которого нужно скачать тред.</param>
    /// <param name="botToken">Токен бота.</param>
    string DownloadThread(ThreadDTO thread, string channelName);
  }
}
