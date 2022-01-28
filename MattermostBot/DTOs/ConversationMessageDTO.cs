using System.Collections.Generic;

namespace MattermostBot.DTOs
{
  class ConversationMessageDTO
  {
    public bool Ok { get; set; }
    public List<MessageDTO> Messages { get; set; }
  }
  class MessageDTO
  {
    public string Text { get; set; }
    public string Ts { get; set; }
    public string User { get; set; }
    public List<FileDTO> Files { get; set; }
  }
  class FileDTO
  {
    public string Name { get; set; }
    public string UrlPrivateDownload { get; set; }
  }
}
