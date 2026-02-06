using SuperMemo.Application.DTOs.responses.Otpiq;

namespace SuperMemo.Application.Interfaces;

public interface IOtpiqService
{
    Task<OtpiqResponse> SendVerificationCodeAsync(
        string phoneNumber,
        string verificationCode,
        string smsType = "verification",
        string provider = "whatsapp-sms");
}
