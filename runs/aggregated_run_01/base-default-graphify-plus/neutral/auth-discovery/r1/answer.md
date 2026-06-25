## Primary authentication flow

- [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Controllers/CustomerController.cs:442) — main storefront endpoints. Handles login, MFA redirection/session state, password-recovery tokens, registration, logout, and MFA settings. Calls `ValidateCustomerAsync`, `RegisterCustomerAsync`, `ChangePasswordAsync`, and `SignInCustomerAsync`.

- [RouteProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:35) — maps `/login`, `/register`, `/multi-factor-verification`, and password-recovery routes to `CustomerController`.

- [CustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerRegistrationService.cs:138) — core credential logic: validates account state/password, applies lockout, decides whether MFA is required, hashes registration passwords, changes passwords, and signs customers in.

- [ICustomerRegistrationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/ICustomerRegistrationService.cs:1) — contract for the preceding authentication and registration operations.

- [CookieAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/CookieAuthenticationService.cs:45) and [IAuthenticationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/IAuthenticationService.cs:10) — create/remove the ASP.NET authentication cookie and resolve the authenticated customer from claims.

## Password recovery

- [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:1519) — validates the stored recovery token and its configured expiry.

- [WorkflowMessageService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Messages/WorkflowMessageService.cs:319) — sends the recovery email.

- [MessageTokenProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Messages/MessageTokenProvider.cs:1330) — constructs `%Customer.PasswordRecoveryURL%` with the token and customer GUID.

## Multi-factor authentication

- [IMultiFactorAuthenticationMethod.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/IMultiFactorAuthenticationMethod.cs:8) — MFA provider extension point, including configuration and verification components.

- [MultiFactorAuthenticationPluginManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/MultiFactor/MultiFactorAuthenticationPluginManager.cs:10) — loads providers and checks whether the customer’s selected method is active.

- [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Factories/CustomerModelFactory.cs:979) — builds provider lists and selects either configuration or login-verification components.

- [Google Authenticator controller](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Controllers/AuthenticationController.cs:48) — concrete MFA implementation: registers a secret, verifies tokens, clears MFA session state, then completes sign-in.

- [GoogleAuthenticatorService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Plugins/Nop.Plugin.MultiFactorAuth.GoogleAuthenticator/Services/GoogleAuthenticatorService.cs:193) — performs two-factor token validation.

## UI, validation, and middleware

- Views: [Login.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Login.cshtml:1), [Register.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/Register.cshtml:1), [PasswordRecovery.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/PasswordRecovery.cshtml:1), [PasswordRecoveryConfirm.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/PasswordRecoveryConfirm.cshtml:1), and [MultiFactorVerification.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Views/Customer/MultiFactorVerification.cshtml:1).

- Validators: [LoginValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/LoginValidator.cs:9), [RegisterValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/RegisterValidator.cs:12), and [PasswordRecoveryConfirmValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web/Validators/Customer/PasswordRecoveryConfirmValidator.cs:9).

- [AuthenticationStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AuthenticationStartup.cs:18), [AuthenticationMiddleware.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Libraries/Nop.Services/Authentication/AuthenticationMiddleware.cs:36), and [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__auth-discovery__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:210) — register the cookie service and populate `HttpContext.User` through the authentication pipeline.

All listed candidates were verified against source; no graph-only candidates are included.
