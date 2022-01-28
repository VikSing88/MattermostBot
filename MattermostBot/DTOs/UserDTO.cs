namespace MattermostBot.DTOs
{
  class UserDTO
  {
    public bool Ok { get; set; }
    public User User { get; set; }
  }
  class User
  {
    public string Name { get; set; }

    public string RealName { get; set; }
  }
}
