using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.requests.Admin;

public class SetAccountStatusRequest
{
    public AccountStatus Status { get; set; }
}
