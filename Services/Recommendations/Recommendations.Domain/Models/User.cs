namespace Recommendations.Domain.Models;
public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
