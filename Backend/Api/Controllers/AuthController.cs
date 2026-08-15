using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Api.Authentication;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Api.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly GoogleOAuthOptions _googleOAuthOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    private static readonly JsonSerializerOptions PopupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default
    };

    public AuthController(
        IAuthService authService,
        IOptions<GoogleOAuthOptions> googleOAuthOptions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _googleOAuthOptions = googleOAuthOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    // Delegates registration to the auth service and returns JWT credentials.
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return OkResponse(response, "Đăng ký thành công.");
    }

    // Verifies email/username credentials and returns JWT credentials.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return OkResponse(response, "Đăng nhập thành công.");
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult Google([FromQuery] string? returnUrl)
    {
        var frontendOrigin = GetFrontendOrigin(returnUrl);
        if (!_googleOAuthOptions.IsConfigured)
        {
            return GooglePopupResult(
                frontendOrigin,
                success: false,
                message: "Đăng nhập Google chưa được cấu hình trên máy chủ.");
        }

        var callbackUrl = Url.Action(
            nameof(GoogleCallback),
            new { returnUrl = frontendOrigin }) ?? "/api/auth/google/callback";

        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? returnUrl,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var frontendOrigin = GetFrontendOrigin(returnUrl);
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("Google OAuth callback returned an error: {GoogleOAuthError}", error);
            return GooglePopupResult(
                frontendOrigin,
                success: false,
                message: GetGoogleOAuthFailureMessage(error));
        }

        var externalResult = await HttpContext.AuthenticateAsync(GoogleOAuthOptions.ExternalCookieScheme);
        try
        {
            if (!externalResult.Succeeded || externalResult.Principal is null)
            {
                _logger.LogWarning(
                    "Google OAuth external cookie could not be read: {Failure}",
                    externalResult.Failure?.Message);
                return GooglePopupResult(
                    frontendOrigin,
                    success: false,
                    message: "Không thể xác minh tài khoản Google.");
            }

            var subject = externalResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? externalResult.Principal.FindFirstValue("sub");
            var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email)
                ?? externalResult.Principal.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            {
                return GooglePopupResult(
                    frontendOrigin,
                    success: false,
                    message: "Google không trả về đủ thông tin tài khoản.");
            }

            var fullName = externalResult.Principal.FindFirstValue(ClaimTypes.Name)
                ?? externalResult.Principal.FindFirstValue("name");
            var response = await _authService.LoginWithGoogleAsync(
                new GoogleIdentity(subject, email, fullName),
                cancellationToken);

            return GooglePopupResult(frontendOrigin, success: true, authResponse: response);
        }
        catch (AppException ex)
        {
            return GooglePopupResult(frontendOrigin, success: false, message: ex.Message);
        }
        finally
        {
            await HttpContext.SignOutAsync(GoogleOAuthOptions.ExternalCookieScheme);
        }
    }

    // Rotates a valid refresh token and returns a fresh access token pair.
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        return OkResponse(response, "Làm mới phiên đăng nhập thành công.");
    }

    // Revokes the supplied refresh token.
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return OkMessage("Đăng xuất thành công.");
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return OkMessage("Mã OTP đã được gửi tới email của bạn.");
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return OkMessage("Đặt lại mật khẩu thành công.");
    }

    // Reads the current user from the JWT claim and returns profile data.
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetMeAsync(CurrentUserId, cancellationToken);
        return OkResponse(user);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMe(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _authService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
        return OkResponse(user, "Cập nhật hồ sơ thành công.");
    }

    private string GetFrontendOrigin(string? returnUrl)
    {
        if (_googleOAuthOptions.IsAllowedFrontendOrigin(returnUrl, _environment.IsDevelopment()) &&
            Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            return uri.GetLeftPart(UriPartial.Authority);
        }

        return _googleOAuthOptions.GetDefaultFrontendOrigin();
    }

    private static string GetGoogleOAuthFailureMessage(string error)
    {
        if (error.Contains("redirect_uri_mismatch", StringComparison.OrdinalIgnoreCase))
        {
            return "Google chưa nhận đúng địa chỉ trả về. Kiểm tra Redirect URI là http://localhost:5050/signin-google.";
        }

        if (error.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
        {
            return "Client ID hoặc Client Secret Google không hợp lệ. Hãy kiểm tra lại cấu hình OAuth.";
        }

        if (error.Contains("access_denied", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("access denied", StringComparison.OrdinalIgnoreCase))
        {
            return "Google chưa cho phép đăng nhập bằng tài khoản này. Hãy thêm Gmail vào Test users hoặc thử lại và chọn Cho phép.";
        }

        if (error.Contains("correlation", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("state", StringComparison.OrdinalIgnoreCase))
        {
            return "Phiên đăng nhập Google đã hết hạn hoặc trình duyệt chặn cookie. Hãy đóng popup, cho phép cookie rồi thử lại.";
        }

        if (error.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return "Mã xác thực Google đã hết hạn. Hãy bắt đầu đăng nhập lại.";
        }

        return "Google không thể hoàn tất đăng nhập. Vui lòng thử lại.";
    }

    private static ContentResult GooglePopupResult(
        string frontendOrigin,
        bool success,
        string? message = null,
        AuthResponse? authResponse = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            source = "todoflow-google-auth",
            success,
            message,
            data = authResponse
        }, PopupJsonOptions);
        var targetOrigin = JsonSerializer.Serialize(frontendOrigin, PopupJsonOptions);
        var displayMessage = HtmlEncoder.Default.Encode(
            message ?? (success ? "Đăng nhập thành công. Bạn có thể đóng cửa sổ này." : "Đăng nhập không thành công."));

        var html = $$"""
            <!doctype html>
            <html lang="vi">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>TodoFlow</title>
            </head>
            <body>
              <p>{{displayMessage}}</p>
              <script>
                (() => {
                  const message = {{payload}};
                  const targetOrigin = {{targetOrigin}};
                  if (window.opener) {
                    window.opener.postMessage(message, targetOrigin);
                    window.close();
                  }
                })();
              </script>
            </body>
            </html>
            """;

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
