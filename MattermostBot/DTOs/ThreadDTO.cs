using System.Collections.Generic;

namespace SlackBot
{
  class ThreadDTO
  {
    public bool Ok { get; set; }
    public List<MessageDTO> Messages { get; set; }
  }
}
