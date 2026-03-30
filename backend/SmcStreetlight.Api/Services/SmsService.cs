using System.Net.Http.Json;

namespace SmcStreetlight.Api.Services;

public record SmsSendResult(bool Sent, string Message);

public class SmsService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<SmsService> logger)
{
    public async Task<SmsSendResult> SendOtpAsync(string mobile, string otp, CancellationToken cancellationToken = default)
    {
        var provider = config["Sms:Provider"]?.Trim();
        if (string.IsNullOrWhiteSpace(provider))
            return new SmsSendResult(false, "SMS provider is not configured.");

        if (provider.Equals("MSG91", StringComparison.OrdinalIgnoreCase))
        {
            var authKey = config["Sms:AuthKey"];
            var templateId = config["Sms:TemplateId"];
            if (string.IsNullOrWhiteSpace(authKey) || string.IsNullOrWhiteSpace(templateId))
                return new SmsSendResult(false, "MSG91 AuthKey/TemplateId not configured.");

            var url = "https://control.msg91.com/api/v5/otp";
            var payload = new Dictionary<string, string>
            {
                ["mobile"] = $"91{mobile}",
                ["authkey"] = authKey,
                ["otp"] = otp,
                ["template_id"] = templateId
            };

            try
            {
                var client = httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return new SmsSendResult(true, "OTP sent successfully.");

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("MSG91 OTP failed: {Status} {Body}", response.StatusCode, body);
                return new SmsSendResult(false, "OTP API rejected request. Check SMS config.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MSG91 OTP send failed.");
                return new SmsSendResult(false, "OTP send failed due to network/provider error.");
            }
        }

        return new SmsSendResult(false, $"Unsupported SMS provider: {provider}");
    }
}
