Used `graphify query` first, then verified against source. These are the strongest candidate files.

**Primary Public Flows**
- [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Controllers/CustomerController.cs:442)  
  Main public auth controller. Evidence: `Login` validates credentials via `ValidateCustomerAsync`, signs in via `SignInCustomerAsync`, or stores `CustomerMultiFactorAuthenticationInfo` and redirects to MFA verification. Also owns `Logout`, password recovery, registration, change password, phone OTP login, and MFA settings/actions.

- [CustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerRegistrationService.cs:138)  
  Core credential logic. Evidence: `ValidateCustomerAsync` checks existence, active/deleted/registered/lockout, password match, failed attempts, and returns `MultiFactorAuthenticationRequired` if the selected MFA provider is active. Also handles `RegisterCustomerAsync` and `ChangePasswordAsync`.

- [CookieAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/CookieAuthenticationService.cs:45)  
  Cookie sign-in/sign-out implementation. Evidence: builds claims for username/email/phone and calls ASP.NET `SignInAsync`; `SignOutAsync` clears cached customer and signs out of the nop auth scheme.

**Login / Registration / Password Reset UI**
- [Login.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Login.cshtml:66), [Register.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Register.cshtml:18), [PasswordRecovery.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/PasswordRecovery.cshtml:15), [PasswordRecoveryConfirm.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/PasswordRecoveryConfirm.cshtml:26)  
  Public Razor forms. Evidence: route-bound forms for login, register, password recovery email, and reset-password confirmation.

- [LoginModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/LoginModel.cs:9), [RegisterModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/RegisterModel.cs:10), [PaswordRecoveryModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/PaswordRecoveryModel.cs:7), [PasswordRecoveryConfirmModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/PasswordRecoveryConfirmModel.cs:8)  
  Form models. Evidence: contain login email/phone/password, registration email/password/phone, recovery email, and reset password fields.

- [LoginValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/LoginValidator.cs:9), [RegisterValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/RegisterValidator.cs:12), [PasswordRecoveryValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/PasswordRecoveryValidator.cs:8), [PasswordRecoveryConfirmValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/PasswordRecoveryConfirmValidator.cs:9)  
  Input validation for those auth forms.

- [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Factories/CustomerModelFactory.cs:543)  
  Builds auth-related public models. Evidence: methods for login, register, password recovery, MFA settings, and MFA provider models.

**Password Reset / Customer State Support**
- [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:1519)  
  Password recovery token validation/expiry and password retrieval/storage. Evidence: `IsPasswordRecoveryTokenValidAsync`, `IsPasswordRecoveryLinkExpiredAsync`, `GetCurrentPasswordAsync`, `InsertCustomerPasswordAsync`.

- [NopCustomerDefaults.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Core/Domain/Customers/NopCustomerDefaults.cs:64) and [CustomerSettings.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Core/Domain/Customers/CustomerSettings.cs:103)  
  Attribute keys and settings. Evidence: password recovery token/date attributes, selected MFA provider attribute, session key for MFA login info, and password recovery link validity days.

**Multi-Factor Authentication**
- [MultiFactorAuthenticationPluginManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/MultiFactorAuthenticationPluginManager.cs:54)  
  Loads/checks active MFA plugins from settings.

- [IMultiFactorAuthenticationMethod.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/IMultiFactorAuthenticationMethod.cs:15) and [MultiFactorAuthenticationType.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/MultiFactorAuthenticationType.cs:3)  
  MFA plugin contract and type enum. Evidence: plugin provides public setup and verification view components.

- [MultiFactorAuthentication.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/MultiFactorAuthentication.cshtml:42), [MultiFactorVerification.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/MultiFactorVerification.cshtml:1), [ConfigureMultiFactorAuthenticationProvider.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/ConfigureMultiFactorAuthenticationProvider.cshtml:1), [_MultiFactorAuthenticationProviders.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/_MultiFactorAuthenticationProviders.cshtml:1)  
  Public MFA settings, provider selection/configuration, and verification views.

- [GoogleAuthenticatorMethod.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/GoogleAuthenticatorMethod.cs:14), [AuthenticationController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Controllers/AuthenticationController.cs:53), [GoogleAuthenticatorService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Services/GoogleAuthenticatorService.cs:193)  
  Concrete Google Authenticator MFA plugin. Evidence: implements `IMultiFactorAuthenticationMethod`, registers/verifies tokens, and signs in after successful MFA verification.

**Related Authentication Surface**
- [ExternalAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/External/ExternalAuthenticationService.cs:270), [ExternalAuthenticationModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Factories/ExternalAuthenticationModelFactory.cs:42), [Nop.Plugin.ExternalAuth.Facebook](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.ExternalAuth.Facebook/Controllers/FacebookAuthenticationController.cs:128)  
  External login path. Evidence: login/register views render external auth methods; Facebook plugin authenticates and passes claims to `ExternalAuthenticationService.AuthenticateAsync`.

- [Areas/Admin/Controllers/AuthenticationController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/AuthenticationController.cs:115)  
  Admin management for external and MFA auth methods. Evidence: enables/disables active external and MFA plugin system names.
