## Primary workflow files

- [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Controllers/CustomerController.cs:442) — central public account controller. Contains `Login`, MFA handoff/verification, logout, password recovery/confirmation, registration, change-password, and MFA configuration actions. Login validates credentials at line 482 and signs in at 491; reset tokens are created at 836–844; registration calls `RegisterCustomerAsync` at 1044.

- [CustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerRegistrationService.cs:138) — core credential and account logic: `ValidateCustomerAsync`, `RegisterCustomerAsync` (238), `ChangePasswordAsync` (364), and `SignInCustomerAsync` (456). Credential validation returns `MultiFactorAuthenticationRequired` when an active selected provider exists (178–181).

- [ICustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/ICustomerRegistrationService.cs:20) — public contract for login validation, registration, password changes, and sign-in.

## Authentication infrastructure

- [CookieAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/CookieAuthenticationService.cs:45) — constructs the customer principal and calls ASP.NET Core `SignInAsync`; clears the authentication cookie in `SignOutAsync` at line 83.

- [IAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/IAuthenticationService.cs:8) — cookie-authentication abstraction consumed by registration and controller code.

- [ServiceCollectionExtensions.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/Extensions/ServiceCollectionExtensions.cs:255) — `AddNopAuthentication` configures the main and external cookie schemes, login path, and external-auth registrars.

- [AuthenticationStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AuthenticationStartup.cs:19) and [AuthenticationMiddleware.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/AuthenticationMiddleware.cs:13) — register/run authentication and populate `HttpContext.User` from the configured scheme.

## Password reset

- [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:1519) — validates password-recovery tokens and their expiration dates.

- [WorkflowMessageService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Messages/WorkflowMessageService.cs:319) — queues the password-recovery email.

- [MessageTokenProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Messages/MessageTokenProvider.cs:1330) — builds the recovery-confirmation URL containing the token and customer GUID.

- [NopCustomerDefaults.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Core/Domain/Customers/NopCustomerDefaults.cs:66) — defines recovery-token, generation-date, selected-MFA-provider, and MFA-session attribute keys.

## Multi-factor authentication

- [MultiFactorAuthenticationPluginManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/MultiFactorAuthenticationPluginManager.cs:10) — discovers active MFA plugins and checks whether a configured provider is active.

- [IMultiFactorAuthenticationMethod.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/IMultiFactorAuthenticationMethod.cs:8) — plugin contract, including enrollment and verification view components.

- [ForceMultiFactorAuthenticationAttribute.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/ForceMultiFactorAuthenticationAttribute.cs:17) — enforces MFA configuration when required and checks the customer’s selected provider.

- [CustomerMultiFactorAuthenticationInfo.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerMultiFactorAuthenticationInfo.cs:6) — session state carrying username, persistence, and return URL between password validation and second-factor verification.

- [GoogleAuthenticatorMethod.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/GoogleAuthenticatorMethod.cs:14) — concrete MFA provider.

- [AuthenticationController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Controllers/AuthenticationController.cs:54) — enrolls Google Authenticator and verifies tokens; successful verification calls `SignInCustomerAsync` at line 97.

## Models, validation, and views

- [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Factories/CustomerModelFactory.cs:406) — prepares registration, login, password-recovery, and MFA models.

- Models: [LoginModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/LoginModel.cs:9), [RegisterModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/RegisterModel.cs:10), [PaswordRecoveryModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/PaswordRecoveryModel.cs:7), [PasswordRecoveryConfirmModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/PasswordRecoveryConfirmModel.cs:8), and [MultiFactorAuthenticationModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Models/Customer/MultiFactorAuthenticationModel.cs:6). Note the existing `PaswordRecoveryModel.cs` filename typo.

- Validators: [LoginValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/LoginValidator.cs:9), [RegisterValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/RegisterValidator.cs:12), and the two `PasswordRecovery*Validator.cs` files in the same directory.

- Views: [Login.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Login.cshtml:1), [Register.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Register.cshtml:1), `PasswordRecovery*.cshtml`, and `MultiFactor*.cshtml` in `Views/Customer`.

For external/social login, the additional central candidate is [ExternalAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/External/ExternalAuthenticationService.cs:270), which authenticates or auto-registers externally identified customers.
