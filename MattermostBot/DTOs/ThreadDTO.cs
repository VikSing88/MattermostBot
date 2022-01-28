using System.Collections.Generic;

namespace MattermostBot.DTOs
{
  class ThreadDTO
  {
    public bool Ok { get; set; }
    public List<MessageDTO> Messages { get; set; }
  }
}
